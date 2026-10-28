# ollama_ocr.py
# -*- coding: utf-8 -*-
"""
Ollama-based OCR for Jordanian identity documents.

Strategy per document type:
  - Passport      → Ollama (vision LLMs handle structured Latin+Arabic well)
  - National ID   → Ollama (mixed Arabic/English fields, structured layout)
  - Vehicle Lic.  → Ollama with EasyOCR fallback for registration number
                    (Arabic-Indic digits on busy background are hard for LLMs)

Supported models (auto-detected, highest priority first):
  llama3.2-vision, qwen2.5vl, qwen3-vl, minicpm-v, llava:13b, llava:7b

Usage:
    import ollama_ocr
    if ollama_ocr.is_available():
        result = ollama_ocr.extract_passport("passport.jpg")
        result = ollama_ocr.extract_jordan_id("front.jpg", "back.jpg")
        result = ollama_ocr.extract_vehicle_license("front.jpg", "back.jpg")
"""

import base64
import json
import os
import re
import tempfile
import urllib.error
import urllib.request
from difflib import SequenceMatcher

import cv2
import numpy as np

# ── Configuration ─────────────────────────────────────────────────────────────
OLLAMA_BASE     = os.environ.get("OLLAMA_BASE", "http://127.0.0.1:11434").rstrip("/")
REQUEST_TIMEOUT = int(os.environ.get("OLLAMA_REQUEST_TIMEOUT", "45"))
OLLAMA_NUM_CTX = int(os.environ.get("OLLAMA_NUM_CTX", "4096"))
OLLAMA_NUM_PREDICT = int(os.environ.get("OLLAMA_NUM_PREDICT", "768"))
OLLAMA_NUM_PREDICT_RETRY = int(os.environ.get("OLLAMA_NUM_PREDICT_RETRY", "1024"))
OLLAMA_KEEP_ALIVE = os.environ.get("OLLAMA_KEEP_ALIVE", "10m")
OLLAMA_IMAGE_MAX_SIDE = int(os.environ.get("OLLAMA_IMAGE_MAX_SIDE", "1600"))
OLLAMA_IMAGE_JPEG_QUALITY = int(os.environ.get("OLLAMA_IMAGE_JPEG_QUALITY", "90"))

# Vision models in preference order. First match found in Ollama wins.
#
# ORDERING RULES:
#   1. Non-thinking models first — output JSON directly, no token waste.
#   2. Thinking models (qwen3-vl, qwen2-vl) are LAST — they burn the entire
#      token budget on reasoning prose even with think=false, so only used
#      if nothing better is installed.
#   3. llama3 (text-only) excluded — hangs forever on image input.
#   4. qwen3-coder / deepseek-coder excluded — text/code only, no vision.
_VISION_PRIORITY = [
    "qwen2.5vl:7b",            # strong vision, no thinking, clean JSON output
    "gemma3",            # good fallback, also no thinking
    "llama3.2-vision",   # best Arabic + MRZ accuracy when available
    "minicpm-v",
    "qwen2.5vl",         # good but thinking-capable — only if above missing
    "llava:13b",
    "llava:7b",
    "llava",
    "moondream",
    "qwen3-vl",          # LAST: thinking model, unreliable JSON output
    "qwen2-vl",          # LAST: same issue
]

# Models that emit <think>...</think> reasoning blocks.
# These are routed through /api/chat with think=false.
# Even so, qwen3-vl frequently ignores the flag and burns the token budget
# on reasoning — prefer any non-thinking model above these.
_THINKING_MODEL_PREFIXES = {
    "qwen3-vl",
    "qwen3",
    "qwen2-vl",
}

# Weak models — used as last resort with a warning
_WEAK_MODELS = {"llava:7b", "moondream", "llava"}

# Arabic-Indic + Extended Arabic-Indic -> western digit translation table.
# Use escapes so a bad deployment encoding cannot corrupt this string.
_AR_INDIC = str.maketrans(
    "\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669"
    "\u06F0\u06F1\u06F2\u06F3\u06F4\u06F5\u06F6\u06F7\u06F8\u06F9",
    "01234567890123456789",
)

import os as _os
_FORCED_MODEL = _os.environ.get("OLLAMA_VISION_MODEL", "").strip() or None


# ── Prompts ───────────────────────────────────────────────────────────────────

_PROMPT_PASSPORT = """You are an identity document reader.
This is a passport image. Extract the following fields exactly as printed:
- full_name_en (English/Latin name as printed)
- full_name_ar (must contain Arabic script only. If the document does not contain an Arabic name, return null. Do NOT place Chinese, Japanese, Korean, Cyrillic, Hindi, Thai, or any other script in full_name_ar.)
- passport_number
- date_of_birth (DD/MM/YYYY)
- date_of_expiry (DD/MM/YYYY)
- date_of_issue (DD/MM/YYYY)
- sex (M or F)
- nationality
- place_of_birth
- national_number (10-digit ID number if present)
- mother_name
- authority
- mrz_l1 (first MRZ line, exactly 44 characters if visible)
- mrz_l2 (second MRZ line, exactly 44 characters if visible)

Return ONLY a JSON object. No markdown, no explanation.
Example:
{"full_name_en": "NAME IN ENGLISH", "full_name_ar": null, "passport_number": null, "date_of_birth": "DD/MM/YYYY", "date_of_expiry": null}

Important name rules:
- Prefer the printed visual Names/Name field over the MRZ for full_name_en.
- MRZ names may be shortened because the MRZ line is fixed width; do not shorten a visible printed name to match MRZ.
- For full_name_ar, read only the printed Arabic name. Never transliterate the English name into Arabic.

If a field is not visible, not present on the document, or unclear, set its value to null."""

_PROMPT_NATIONAL_ID_FRONT = """You are a JSON-only OCR engine. Output nothing except a single JSON object.
Do not explain. Do not use markdown. Do not add any text before or after the JSON.
CRITICAL: Output all values in their original language (Arabic in Arabic, English in English). Never output Chinese.

Task: Extract every visible field from this Jordanian national ID card FRONT image.
Use null for fields you cannot read. Dates in DD/MM/YYYY format.

Field locations on the FRONT side:
- national_number: next to label "الرقم الوطني" — 10 digits
- full_name_ar: next to label "الاسم" — full Arabic name (all parts)
- full_name_en: next to label "Name:" — full English name (all parts)
- date_of_birth: next to label "تاريخ الولادة"
- gender: next to label "الجنس" — return M or F only
- place_of_birth: next to label "مكان الولادة" — city name only
- mother_name: next to label "اسم الام" — mother first name in Arabic

Output exactly this structure:
{"national_number": "10-digit number", "full_name_ar": "...", "full_name_en": "...", "date_of_birth": "DD/MM/YYYY", "gender": "M or F", "place_of_birth": "...", "mother_name": "..."}"""

_PROMPT_NATIONAL_ID_BACK = """You are a JSON-only OCR engine. Output nothing except a single JSON object.
Do not explain. Do not use markdown. Do not add any text before or after the JSON.
CRITICAL: Output all values in their original language. Never output Chinese.

Task: Extract every visible field from this Jordanian national ID card BACK image.
MRZ lines must be exactly 30 characters using only A-Z, 0-9, and <.
Dates in DD/MM/YYYY format.

Field locations on the BACK side:
- document_number: next to label "ID no." — alphanumeric e.g. FDN46372
- expiry_date: next to label "Expiry / الصلاحية"
- blood_type: next to label "فصيلة الدم" — one of A+/A-/B+/B-/O+/O-/AB+/AB-
- mrz_l1: first MRZ line at the bottom (30 chars, starts with IDJOR)
- mrz_l2: second MRZ line at the bottom (30 chars, digits and letters)
- mrz_l3: third MRZ line at the bottom (30 chars, name with << separator)

Output exactly this structure:
{"document_number": "...", "expiry_date": "DD/MM/YYYY", "blood_type": "...", "mrz_l1": "30-char line 1", "mrz_l2": "30-char line 2", "mrz_l3": "30-char name line"}"""

# FIX: Replaced "..." placeholder with explicit null instruction so _str() filter
# isn't fighting against model output that the prompt itself invited.
_PROMPT_VEHICLE_FRONT = """You are a JSON-only OCR engine. Output nothing except a single JSON object.
Do not explain. Do not use markdown. Do not add any text before or after the JSON.

Task: Read the plate number from this Jordanian vehicle license FRONT image.

CRITICAL — the plate number is:
- In the LARGE HIGHLIGHTED BOX at the very BOTTOM of the card
- Next to the label "رقم اللوحة"
- Formatted like "37-30732", "7-30732", "37-7", or "7-7"
- The part before the dash has 1 or 2 digits
- The part after the dash has 1 to 5 digits

Also return car_coding:
- If plate_number has a dash, car_coding is the part before the first dash.
- Example: plate_number "37-30732" means car_coding "37".
- If plate_number has no dash, use null.

Also return plate_number_only:
- If plate_number has a dash, plate_number_only is the part after the first dash.
- Example: plate_number "37-30732" means plate_number_only "30732".
- If plate_number has no dash, use null.

Do NOT return the alphanumeric code printed at the TOP of the card (e.g. a pink/coloured box).
That top code is a document ID, not the plate number.
If you cannot read the field clearly, use null.

Output exactly this structure:
{"plate_number": null, "car_coding": null, "plate_number_only": null}"""


_PROMPT_VEHICLE_BACK = """You are a JSON-only OCR engine. Output nothing except a single JSON object.
Do not explain. Do not use markdown. Do not add any text before or after the JSON.
If you cannot read a field clearly, use null (not "..." or empty string).

Task: Read THREE specific fields from this Jordanian vehicle license BACK image.

FIELD 1 — registration_number (رقم التسجيل):
- Find the label "رقم التسجيل" — it is on the FIRST data row, near the TOP-RIGHT
- The value is printed in Arabic-Indic digits: ٠=0 ١=1 ٢=2 ٣=3 ٤=4 ٥=5 ٦=6 ٧=7 ٨=8 ٩=9
- Convert every digit to western. Result is exactly 10 digits starting with 1.
- Example: ١٣١٩٣٠٥٤٩٧ → "1319305497"
- WARNING: Do NOT return the value next to "رقم البوليصة" (near the BOTTOM)
- WARNING: Do NOT return the value next to "رقم المحرك" (engine number, shorter)

FIELD 2 — chassis_number (رقم الشاصي):
- Next to label "رقم الشاصي" — exactly 17 characters, letters A-Z and digits 0-9
- Example: "KMHDN41AP3U589471"
- Do NOT return the engine number (رقم المحرك) — that is a different, shorter field

FIELD 3 — policy_number (رقم البوليصة):
- Find label "رقم البوليصة" near the BOTTOM of the card
- Western digits only, typically 10 digits, e.g. "1011191737"
- Must be DIFFERENT from registration_number

Output exactly this structure:
{"registration_number": null, "chassis_number": null, "policy_number": null}"""

# Focused retry prompts
_PROMPT_VEHICLE_BACK_REG = """You are a JSON-only OCR engine. Output nothing except a single JSON object.
If you cannot read the field, use null (not "..." or empty string).

Task: Find ONLY the registration number (رقم التسجيل) in this Jordanian vehicle license image.
- Find label "رقم التسجيل" on the FIRST data row (near the TOP of the card)
- Printed in Arabic-Indic digits: ٠=0 ١=1 ٢=2 ٣=3 ٤=4 ٥=5 ٦=6 ٧=7 ٨=8 ٩=9
- Convert every digit to western equivalent
- Result must be exactly 10 digits starting with 1
- Example: ١٣٣١٢١٤٣٠٣ → "1331214303"
- DO NOT return value next to "رقم البوليصة" (BOTTOM) or "رقم المحرك" (engine)

Output: {"registration_number": null}"""

_PROMPT_VEHICLE_BACK_REG_RAW = """You are a JSON-only OCR engine. Output nothing except a single JSON object.
If you cannot read the field, use null (not "..." or empty string).

Task: Find ONLY the registration number (رقم التسجيل) in this Jordanian vehicle license image.
- Find label "رقم التسجيل" on the FIRST data row (near the TOP of the card)
- Copy the digits next to it EXACTLY as they appear — Arabic-Indic numerals
- Do NOT convert them. Copy each character exactly as printed.
- Do NOT use any value from the bottom of the card

Output: {"registration_number_raw": null}"""

_PROMPT_VEHICLE_BACK_CHASSIS = """You are a JSON-only OCR engine. Output nothing except a single JSON object.
If you cannot read the field, use null.

Task: Find the chassis number (رقم الشاصي) in this Jordanian vehicle license image.
- Exactly 17 characters: only letters A-Z and digits 0-9
- Example: "KMHDN41AP3U589471"
- Do NOT return the engine number (رقم المحرك) — it is shorter and starts with letters like G4EC

Output: {"chassis_number": null}"""

_PROMPT_VEHICLE_BACK_POLICY = """You are a JSON-only OCR engine. Output nothing except a single JSON object.
If you cannot read the field, use null (not "..." or empty string).

Task: Find the insurance policy number (رقم البوليصة) in this Jordanian vehicle license image.
- Near the BOTTOM of the card next to label "رقم البوليصة"
- Western digits only, e.g. "1010875804"

Output: {"policy_number": null}"""


# ── Image preprocessing ───────────────────────────────────────────────────────

def _fix_arabic_zero_dot(value: str) -> str:
    """
    Models often render ٠ (Arabic-Indic zero) as a literal dot.
    Fix sequences like "13193.5497" → "1319305497"
    """
    return re.sub(r"(\d)\.(\d)", r"\g<1>0\g<2>", value)


def normalize_field(value):
    if isinstance(value, dict):
        inner = value.get("value", "")
        while isinstance(inner, dict):
            inner = inner.get("value", "")
        return inner or ""
    return value or ""


def _trace(label: str, payload) -> None:
    try:
        text = json.dumps(payload, ensure_ascii=False, sort_keys=True, default=str)
    except Exception:
        text = str(payload)
    print(f"[OCR_TRACE] {label}: {text}", flush=True)


def _str_field(data: dict | None, key: str) -> str | None:
    value = normalize_field(data.get(key) if isinstance(data, dict) else None)
    if value in (None, "", "null", "None", "..."):
        return None
    value = str(value).strip()
    return value if value and value not in ("null", "None", "...") else None


def _first_str_field(data: dict | None, *keys: str) -> str | None:
    for key in keys:
        value = _str_field(data, key)
        if value:
            return value
    return None


_PLATE_NUMBER_RE = re.compile(
    r"(?<![0-9\u0660-\u0669\u06F0-\u06F9-\u2010-\u2015\u2212/])"
    r"([0-9\u0660-\u0669\u06F0-\u06F9]{1,2})\s*[-\u2010-\u2015\u2212/]\s*"
    r"([0-9\u0660-\u0669\u06F0-\u06F9]{1,5})"
    r"(?!\s*[-\u2010-\u2015\u2212/]\s*[0-9\u0660-\u0669\u06F0-\u06F9])"
    r"(?![0-9\u0660-\u0669\u06F0-\u06F9])"
)
_DATE_LIKE_RE = re.compile(
    r"(?<![0-9])"
    r"[0-9]{1,2}\s*[-\u2010-\u2015\u2212/]\s*"
    r"[0-9]{1,2}\s*[-\u2010-\u2015\u2212/]\s*"
    r"[0-9]{2,4}"
    r"(?![0-9])"
)


def _inside_date_like_span(text: str, start: int, end: int) -> bool:
    return any(start >= m.start() and end <= m.end() for m in _DATE_LIKE_RE.finditer(text))


def _split_plate_number(plate_number: object) -> tuple[str | None, str | None, str | None]:
    text = str(plate_number or "").strip().translate(_AR_INDIC)
    if not text:
        return None, None, None
    for match in _PLATE_NUMBER_RE.finditer(text):
        if _inside_date_like_span(text, match.start(), match.end()):
            continue
        car_coding = re.sub(r"\D", "", match.group(1))
        plate_number_only = re.sub(r"\D", "", match.group(2))
        if car_coding and plate_number_only:
            return car_coding, plate_number_only, f"{car_coding}-{plate_number_only}"
    return None, None, None


# ── EasyOCR targeted fallback ─────────────────────────────────────────────────

def _easyocr_extract_registration(back_path: str) -> str | None:
    """
    Targeted EasyOCR fallback for registration_number only.
    Finds 10-digit Arabic-Indic/western numbers starting with 1,
    and picks the one spatially closest to the رقم التسجيل label.
    """
    try:
        from ocr_engine import load_and_resize, ocr_to_tokens, READER_MIX
    except ImportError as exc:
        print(f"  ⚠️  EasyOCR fallback unavailable: {exc}")
        return None

    try:
        img    = load_and_resize(back_path)
        tokens = ocr_to_tokens(img, READER_MIX)
    except Exception as exc:
        print(f"  ⚠️  EasyOCR read failed: {exc}")
        return None

    def _normalise(text: str) -> str:
        """Arabic-Indic + dot-zero fix → pure western digits."""
        western = text.translate(_AR_INDIC)
        western = _fix_arabic_zero_dot(western)
        return re.sub(r"\D", "", western)

    # Locate the label row so we can rank candidates by proximity
    label_y: int | None = None
    for tok in tokens:
        if "التسجيل" in tok["text"] or "تسجيل" in tok["text"]:
            label_y = tok["y"]
            print(f"  📍 EasyOCR found رقم التسجيل label at y={label_y}")
            break

    if label_y is None:
        print("  ⚠️  EasyOCR: label رقم التسجيل not found — scanning all tokens")

    candidates: list[tuple[int, str]] = []  # (y-distance from label, value)

    for tok in tokens:
        normalised = _normalise(tok["text"])
        if len(normalised) == 10 and normalised.startswith("1"):
            dist = abs(tok["y"] - label_y) if label_y is not None else 9999
            candidates.append((dist, normalised))
            print(f"      candidate: {normalised!r}  y={tok['y']}  dist={dist}px")

    if not candidates:
        print("  ⚠️  EasyOCR: no 10-digit candidate found")
        return None

    candidates.sort(key=lambda x: x[0])
    best_dist, best = candidates[0]
    print(f"  ✅ EasyOCR registration: {best}  (row_dist={best_dist}px)")
    return best

def _crop_registration_row(image_path: str) -> str:
    """
    Dynamically find the رقم التسجيل label using EasyOCR, then crop that row.
    Falls back to a fixed top-right crop if the label is not found.

    Sending only the registration row means the model cannot see رقم البوليصة
    or رقم المحرك — eliminating field confusion.
    """
    try:
        from ocr_engine import load_and_resize, ocr_to_tokens, READER_MIX
        img    = load_and_resize(image_path)
        tokens = ocr_to_tokens(img, READER_MIX)
        h, w   = img.shape[:2]

        label_tok = None
        for t in tokens:
            if "التسجيل" in t["text"] or "تسجيل" in t["text"]:
                label_tok = t
                break

        if label_tok:
            pad = 20
            y1  = max(0, label_tok["y"] - pad)
            y2  = min(h, label_tok["y2"] + pad)
            x1, x2 = 0, w
            print(f"  📍 Found رقم التسجيل at y={label_tok['y']}, cropping row...")
        else:
            print("  ⚠️  Label not found — using fixed top-right crop fallback")
            y1, y2 = 0, int(h * 0.25)
            x1, x2 = int(w * 0.45), w

        crop = img[y1:y2, x1:x2]
        if crop.size == 0:
            return image_path

    except Exception as exc:
        print(f"  ⚠️  EasyOCR crop failed ({exc}) — using fixed crop")
        img = cv2.imread(image_path)
        if img is None:
            return image_path
        h, w = img.shape[:2]
        crop = img[0:int(h * 0.25), int(w * 0.45):w]

    # Upscale + enhance so Arabic-Indic digits are clear to the vision model
    crop  = cv2.resize(crop, None, fx=2.5, fy=2.5, interpolation=cv2.INTER_CUBIC)

    # FIX: Explicitly check channel count before grayscale conversion to avoid
    # passing a 3-channel image to fastNlMeansDenoising (which expects single-channel).
    if len(crop.shape) == 3 and crop.shape[2] == 3:
        gray = cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY)
    else:
        gray = crop

    gray  = cv2.convertScaleAbs(gray, alpha=1.5, beta=20)
    gray  = cv2.fastNlMeansDenoising(gray, h=10)
    _, thresh = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)

    fd, tmp = tempfile.mkstemp(suffix=".jpg", prefix="ocr_reg_crop_")
    os.close(fd)
    cv2.imwrite(tmp, thresh)
    return tmp


# ── HTTP helpers ──────────────────────────────────────────────────────────────

def _get(url: str, timeout: int = 5) -> dict:
    with urllib.request.urlopen(url, timeout=timeout) as r:
        return json.loads(r.read())


def _post(url: str, payload: dict, timeout: int = REQUEST_TIMEOUT) -> dict:
    data = json.dumps(payload).encode()
    req  = urllib.request.Request(
        url, data=data,
        headers={"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return json.loads(r.read())


def _prepared_image_bytes(path: str) -> bytes | None:
    if str(os.environ.get("OLLAMA_PREPARE_IMAGE", "1")).strip().lower() in {"0", "false", "no", "off"}:
        return None
    img = cv2.imread(path, cv2.IMREAD_COLOR)
    if img is None or img.size == 0:
        return None

    h, w = img.shape[:2]
    if h > w * 1.20:
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        upper_dark = float(np.mean(gray[: int(h * 0.45), :] < 115))
        lower_dark = float(np.mean(gray[int(h * 0.45):, :] < 115))
        # Tall passport booklet photos often include a mostly blank signature
        # page above the data page. Crop only when the lower half is clearly
        # denser, so normal portrait data-page photos are preserved.
        if lower_dark > max(0.018, upper_dark * 1.45):
            img = img[int(h * 0.42):, :]
            h, w = img.shape[:2]

    max_side = max(600, OLLAMA_IMAGE_MAX_SIDE)
    side = max(h, w)
    if side > max_side:
        scale = max_side / float(side)
        img = cv2.resize(img, (int(w * scale), int(h * scale)), interpolation=cv2.INTER_AREA)

    quality = min(96, max(75, OLLAMA_IMAGE_JPEG_QUALITY))
    ok, encoded = cv2.imencode(".jpg", img, [int(cv2.IMWRITE_JPEG_QUALITY), quality])
    return bytes(encoded) if ok else None


def _encode_image(path: str) -> str:
    data = _prepared_image_bytes(path)
    if data is None:
        with open(path, "rb") as f:
            data = f.read()
    return base64.b64encode(data).decode()


def _first_json_object(text: str) -> str | None:
    """Return the first balanced { } block in a string."""
    start = text.find("{")
    if start < 0:
        return None
    depth, in_string, escape = 0, False, False
    for i in range(start, len(text)):
        ch = text[i]
        if in_string:
            if escape:
                escape = False
            elif ch == "\\":
                escape = True
            elif ch == '"':
                in_string = False
            continue
        if ch == '"':
            in_string = True
        elif ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return text[start:i + 1]
    return None


def _parse_json_response(text: str) -> dict:
    """
    Robustly extract a JSON object from a model response.
    Handles: pure JSON, markdown fences, <think> blocks, prose wrapping.
    """
    original = text.strip()

    # Strip <think>...</think> blocks (qwen3-vl thinking mode)
    text = re.sub(r"<think>[\s\S]*?</think>", "", original, flags=re.IGNORECASE).strip()

    # Strip markdown fences
    text = re.sub(r"```(?:json)?\s*", "", text)
    text = re.sub(r"```", "", text)

    # Extract first balanced JSON object
    obj = _first_json_object(text)
    if obj:
        try:
            return json.loads(obj)
        except json.JSONDecodeError:
            pass

    # Last resort: recover key:value pairs from prose
    recovered = {}
    for line in original.splitlines():
        line = line.strip()
        kv = re.match(r'^["\']?(\w+)["\']?\s*:\s*["\']?([^"\'{\[\n]+)["\']?$', line)
        if kv:
            recovered[kv.group(1)] = kv.group(2).strip().rstrip('",')
    if recovered:
        print(f"  ⚠️  JSON parse failed — recovered {len(recovered)} key(s) from prose")
        return recovered

    if original:
        print(f"  ⚠️  JSON parse failed completely. Preview: {original[:200]!r}")
    return {}


def _is_thinking_model(model: str) -> bool:
    base = model.split(":")[0].lower()
    return any(base == p for p in _THINKING_MODEL_PREFIXES)


def _ask(model: str, prompt: str, image_path: str, _retry: bool = False) -> dict:
    """
    Send one image + prompt to Ollama and return the parsed JSON dict.

    Routing logic:
      - Thinking models (qwen3-vl, qwen2-vl, etc.) → /api/chat with think=false.
        These models ignore /no_think and the options.think flag on /api/generate,
        causing all output to go into the 'thinking' field as prose reasoning and
        leaving 'response' empty. The /api/chat endpoint with think=false in the
        message body correctly suppresses the thinking loop.
      - All other models → /api/generate (simpler, no overhead).
    """
    is_thinking = _is_thinking_model(model)

    if is_thinking:
        return _ask_chat(model, prompt, image_path, _retry=_retry)
    else:
        return _ask_generate(model, prompt, image_path, _retry=_retry)


def _ask_generate(model: str, prompt: str, image_path: str, _retry: bool = False) -> dict:
    """/api/generate path — for non-thinking models (llama3.2-vision, llava, minicpm-v, etc.)."""
    # FIX: Use 2048 as the base budget. 768 is too small for passport/ID JSON
    # which can be 600-900 tokens. llama3.2-vision also sometimes outputs extra
    # whitespace/commentary before the JSON object, eating into the budget.
    num_predict = OLLAMA_NUM_PREDICT if not _retry else OLLAMA_NUM_PREDICT_RETRY

    payload = {
        "model":  model,
        "prompt": prompt,
        "images": [_encode_image(image_path)],
        "stream": False,
        "format": "json",
        "keep_alive": OLLAMA_KEEP_ALIVE,
        "options": {
            "temperature": 0.0,
            "num_ctx": OLLAMA_NUM_CTX,
            "num_predict": num_predict,
        },
    }
    resp        = _post(f"{OLLAMA_BASE}/api/generate", payload)
    raw         = resp.get("response", "").strip()
    done_reason = resp.get("done_reason", "")

    print(f"      raw preview: {raw[:300].strip()!r}  [done={done_reason!r}]")
    _trace("OLLAMA_RAW_GENERATE", {
        "model": model,
        "done_reason": done_reason,
        "retry": _retry,
        "raw": raw,
    })

    # FIX: Truncation detection — done_reason can be "length", empty string "",
    # or absent entirely depending on the Ollama version and model. Detect
    # truncation by also checking whether the raw text ends without a closing }.
    is_truncated = (
        done_reason == "length"
        or (raw and not raw.rstrip().endswith("}"))
    )
    if is_truncated:
        print("      ⚠️  Response appears truncated (no closing })")

    result = _parse_json_response(raw)

    # Retry with larger budget if we got nothing and response looks truncated
    if not result and is_truncated and not _retry:
        print("      🔁 Retrying with larger token budget...")
        return _ask_generate(model, prompt, image_path, _retry=True)

    if not result:
        print(f"      ❌ No JSON extracted. Full raw: {raw!r}")

    _trace("OLLAMA_PARSED_GENERATE", result)
    return result


def _ask_chat(model: str, prompt: str, image_path: str, _retry: bool = False) -> dict:
    """
    /api/chat path — for thinking models (qwen3-vl, qwen2-vl).

    Thinking is ALWAYS disabled. Three mechanisms are used together because
    qwen3-vl in Ollama requires all three to reliably suppress the think loop:
      1. think=false at the top-level payload  (Ollama API flag)
      2. /no_think prefix in the prompt        (model-level instruction)
      3. num_ctx set to 8192 for image+compact JSON evidence.

    We NEVER retry with think=true — that makes things worse by burning the
    entire token budget on reasoning prose instead of JSON output.

    On retry (_retry=True) we only bump num_predict in case the first attempt
    was truncated before the closing } of the JSON object.
    """
    image_b64   = _encode_image(image_path)
    num_predict = OLLAMA_NUM_PREDICT if not _retry else OLLAMA_NUM_PREDICT_RETRY

    # /no_think prefix is the most reliable per-request suppression for qwen3-vl
    no_think_prompt = "/no_think\n" + prompt

    payload = {
        "model":  model,
        "stream": False,
        "format": "json",
        "keep_alive": OLLAMA_KEEP_ALIVE,
        "think":  False,          # top-level Ollama flag — suppresses thinking field
        "options": {
            "temperature": 0.0,
            "num_predict": num_predict,
            "num_ctx":     OLLAMA_NUM_CTX,
        },
        "messages": [
            {
                "role":    "user",
                "content": no_think_prompt,
                "images":  [image_b64],
            }
        ],
    }

    resp        = _post(f"{OLLAMA_BASE}/api/chat", payload)
    msg         = resp.get("message", {})
    raw         = msg.get("content", "").strip()
    done_reason = resp.get("done_reason", "")

    # If thinking still leaked through, strip it before parsing
    thinking_leaked = msg.get("thinking", "").strip()
    if thinking_leaked:
        print(f"  ⚠️  Thinking leaked despite think=false ({len(thinking_leaked)} chars) — stripping")

    # Strip any <think>...</think> that ended up in content
    raw = re.sub(r"<think>[\s\S]*?</think>", "", raw, flags=re.IGNORECASE).strip()

    print(f"      raw preview: {raw[:300].strip()!r}  [done={done_reason}]")
    _trace("OLLAMA_RAW_CHAT", {
        "model": model,
        "done_reason": done_reason,
        "retry": _retry,
        "raw": raw,
    })
    if done_reason == "length":
        print("      ⚠️  Response truncated — hit num_predict limit")

    result = _parse_json_response(raw)

    # Only retry if truncated (not if thinking — we never want thinking)
    if not result and done_reason == "length" and not _retry:
        print("      🔁 Truncated — retrying with larger num_predict...")
        return _ask_chat(model, prompt, image_path, _retry=True)

    if not result:
        print(f"      ❌ No JSON extracted. Full raw: {raw!r}")

    _trace("OLLAMA_PARSED_CHAT", result)
    return result


# ── Model detection ───────────────────────────────────────────────────────────

_cached_model: str | None = None
_detection_done: bool      = False


def get_vision_model() -> str | None:
    global _cached_model, _detection_done
    if _detection_done and _cached_model:
        return _cached_model

    if _FORCED_MODEL:
        _cached_model = _FORCED_MODEL
        _detection_done = True
        print(f"  🦙 Ollama model forced via env: {_cached_model}")
        return _cached_model

    try:
        data       = _get(f"{OLLAMA_BASE}/api/tags", timeout=3)
        full_names = [m["name"] for m in data.get("models", [])]
    except Exception as exc:
        print(f"  ℹ️  Ollama not reachable ({exc}) — falling back to EasyOCR.")
        _cached_model = None
        _detection_done = False
        return None

    if not full_names:
        print("  ℹ️  Ollama running but no models installed.")
        _cached_model = None
        _detection_done = False
        return None

    print(f"  ℹ️  Ollama models available: {full_names}")

    for preferred in _VISION_PRIORITY:
        pref_base = preferred.split(":")[0].lower()
        for full_name in full_names:
            installed_base = full_name.split(":")[0].lower()
            if installed_base == pref_base or installed_base.startswith(pref_base):
                _cached_model = full_name
                if any(full_name.startswith(w) for w in _WEAK_MODELS):
                    print(f"  ⚠️  Weak model selected: {_cached_model}")
                    print("      Arabic accuracy limited. Consider: ollama pull llama3.2-vision")
                else:
                    print(f"  🦙 Ollama model selected: {_cached_model}")
                _detection_done = True
                return _cached_model

    print("  ℹ️  Ollama is running, but no supported vision model is installed.")
    print("      Install qwen2.5vl:7b or another vision model; text-only models are skipped for OCR.")
    _cached_model = None
    _detection_done = False
    return None


def is_available() -> bool:
    return get_vision_model() is not None


def reset_cache() -> None:
    global _cached_model, _detection_done
    _cached_model   = None
    _detection_done = False


# ── Logging helper ────────────────────────────────────────────────────────────

def _log_missing(doc_type: str, data: dict) -> None:
    """Print any fields that came back null/empty without hardcoding names."""
    missing = [k for k, v in data.items()
               if v is None or str(v).strip() in ("", "null", "None")]
    if missing:
        print(f"  ⚠️  Missing fields ({doc_type}): {', '.join(missing)}")
    else:
        print(f"  ✅ All fields extracted ({doc_type})")


# ── Document extractors ───────────────────────────────────────────────────────

def extract_passport(image_path: str) -> dict:
    """Extract passport fields. Returns same shape as passport_parser.extract_passport()."""
    model = get_vision_model()
    if not model:
        raise RuntimeError("No Ollama vision model available")

    print(f"  🦙 Ollama passport OCR ({model})...")
    raw  = _ask(model, _PROMPT_PASSPORT, image_path)

    passport = {
        "passport_no":    _first_str_field(raw, "passport_no", "passport_number"),
        "national_no":    _first_str_field(raw, "national_no", "national_number", "national_id_number"),
        "name_en":        _first_str_field(raw, "name_en", "full_name_en"),
        "name_ar":        _first_str_field(raw, "name_ar", "full_name_ar"),
        "date_of_birth":  _str_field(raw, "date_of_birth"),
        "expiry_date":    _first_str_field(raw, "expiry_date", "date_of_expiry"),
        "issue_date":     _first_str_field(raw, "issue_date", "date_of_issue"),
        "gender":         _first_str_field(raw, "gender", "sex"),
        "nationality":    _str_field(raw, "nationality"),
        "place_of_birth": _str_field(raw, "place_of_birth"),
        "mother_name":    _str_field(raw, "mother_name"),
        "authority":      _str_field(raw, "authority"),
    }
    mrz_l1 = _str_field(raw, "mrz_l1")
    mrz_l2 = _str_field(raw, "mrz_l2")
    checks = {}
    if mrz_l1 and mrz_l2:
        try:
            from ocr_engine import normalize_mrz_l1_td3, normalize_mrz_l2_td3, validate_td3
            mrz_l1 = normalize_mrz_l1_td3(mrz_l1)
            mrz_l2 = normalize_mrz_l2_td3(mrz_l2)
            checks = validate_td3(mrz_l2)
        except Exception as exc:
            print(f"  ⚠️  Ollama MRZ normalization failed: {exc}")

    mrz = {
        "mrz_l1":     mrz_l1,
        "mrz_l2":     mrz_l2,
        "line1":      mrz_l1,
        "line2":      mrz_l2,
        "mrz_valid":  bool(checks) and all(checks.values()),
        "mrz_checks": checks,
    }
    _log_missing("passport", passport)
    _trace("OLLAMA_PASSPORT_FIELDS", passport)
    _trace("OLLAMA_PASSPORT_MRZ", mrz)
    return {
        "engine":   "ollama",
        "model":    model,
        "quality":  None,
        "passport": passport,
        "mrz":      mrz,
    }


def extract_jordan_id(front_path: str, back_path: str | None = None) -> dict:
    """Extract national ID fields. Returns same shape as national_id_parser.extract_jordan_id()."""
    model = get_vision_model()
    if not model:
        raise RuntimeError("No Ollama vision model available")

    print(f"  🦙 Ollama national ID OCR ({model})...")
    front = _ask(model, _PROMPT_NATIONAL_ID_FRONT, front_path)
    back  = _ask(model, _PROMPT_NATIONAL_ID_BACK,  back_path) if back_path else {}

    data = {
        # From FRONT image
        "national_number": _str_field(front, "national_number"),
        "full_name_ar":    _str_field(front, "full_name_ar"),
        "full_name_en":    _str_field(front, "full_name_en"),
        "date_of_birth":   _str_field(front, "date_of_birth"),
        "gender":          _str_field(front, "gender"),
        "place_of_birth":  _str_field(front, "place_of_birth"),
        "mother_name":     _str_field(front, "mother_name"),
        # From BACK image
        "blood_type":      _str_field(back,  "blood_type"),
        "document_number": _str_field(back,  "document_number"),
        "expiry_date":     _str_field(back,  "expiry_date"),
    }
    mrz = {
        "mrz_l1":     _str_field(back, "mrz_l1"),
        "mrz_l2":     _str_field(back, "mrz_l2"),
        "mrz_l3":     _str_field(back, "mrz_l3"),
        "mrz_valid":  False,
        "mrz_checks": {},
    }
    _log_missing("national_id", data)
    _trace("OLLAMA_NATIONAL_ID_FIELDS", data)
    _trace("OLLAMA_NATIONAL_ID_MRZ", mrz)
    return {
        "engine":  "ollama",
        "model":   model,
        "quality": None,
        "data":    data,
        "mrz":     mrz,
    }


def extract_vehicle_license(front_path: str, back_path: str) -> dict:
    model = get_vision_model()
    if not model:
        raise RuntimeError("No Ollama vision model available")

    print(f"  🦙 Ollama vehicle license OCR ({model})...")

    front_raw = _ask(model, _PROMPT_VEHICLE_FRONT, front_path)
    back_raw  = _ask(model, _PROMPT_VEHICLE_BACK,  back_path)

    print(f"      front: {front_raw}")
    print(f"      back:  {back_raw}")

    plate   = _str_field(front_raw, "plate_number")
    derived_coding, derived_number_only, normalized_plate = _split_plate_number(plate)
    if normalized_plate:
        plate = normalized_plate
    car_coding = _str_field(front_raw, "car_coding") or derived_coding
    plate_number_only = _str_field(front_raw, "plate_number_only") or derived_number_only
    reg     = _str_field(back_raw,  "registration_number")
    chassis = _str_field(back_raw,  "chassis_number")
    policy  = _str_field(back_raw,  "policy_number")

    if reg:
        reg = _fix_arabic_zero_dot(reg)

    if reg and policy and reg == policy:
        print("  ⚠️  registration == policy — model confused fields, clearing reg")
        reg = None

    # Attempt 2: focused reg prompt on full image
    if not reg:
        print("  🔁 Attempt 2: registration (focused prompt, full image)...")
        r   = _ask(model, _PROMPT_VEHICLE_BACK_REG, back_path)
        reg = _str_field(r, "registration_number")
        if reg:
            reg = _fix_arabic_zero_dot(reg)
        if reg and policy and reg == policy:
            reg = None

    # Attempt 3: crop the registration row to isolate it
    cropped_path = None
    if not reg:
        print("  🔁 Attempt 3: registration (cropped row)...")
        cropped_path = _crop_registration_row(back_path)
        r   = _ask(model, _PROMPT_VEHICLE_BACK_REG, cropped_path)
        reg = _str_field(r, "registration_number")
        if reg:
            reg = _fix_arabic_zero_dot(reg)
        if reg and policy and reg == policy:
            reg = None

    # Attempt 4: ask model to copy raw Arabic digits → Python converts
    if not reg:
        print("  🔁 Attempt 4: registration (raw Arabic copy)...")
        src        = cropped_path or back_path
        r          = _ask(model, _PROMPT_VEHICLE_BACK_REG_RAW, src)
        raw_arabic = _str_field(r, "registration_number_raw")
        if raw_arabic:
            converted = raw_arabic.translate(_AR_INDIC)
            converted = _fix_arabic_zero_dot(converted)
            converted = re.sub(r"\D", "", converted)
            if len(converted) == 10 and converted.startswith("1"):
                reg = converted
                print(f"  ✅ Raw Arabic → {reg}")
            else:
                print(f"  ⚠️  Raw Arabic failed: {raw_arabic!r} → {converted!r}")

    # Attempt 5: targeted EasyOCR — best at Arabic-Indic digit recognition
    if not reg:
        print("  🔁 Attempt 5: registration (targeted EasyOCR fallback)...")
        reg = _easyocr_extract_registration(back_path)
        if reg:
            reg = _fix_arabic_zero_dot(reg)
            if policy and reg == policy:
                print("  ⚠️  EasyOCR result matches policy — discarding")
                reg = None
        else:
            print("  ⚠️  EasyOCR fallback also returned null")

    # Clean up temp crop file
    if cropped_path and os.path.exists(cropped_path):
        try:
            os.remove(cropped_path)
        except OSError:
            pass

    # Retry chassis and policy if missing from first pass
    if not chassis:
        print("  🔁 Retrying: chassis_number...")
        r       = _ask(model, _PROMPT_VEHICLE_BACK_CHASSIS, back_path)
        chassis = _str_field(r, "chassis_number")

    if not policy:
        print("  🔁 Retrying: policy_number...")
        r      = _ask(model, _PROMPT_VEHICLE_BACK_POLICY, back_path)
        policy = _str_field(r, "policy_number")

    # Final format validation for registration
    if reg:
        digits = re.sub(r"\D", "", str(reg).translate(_AR_INDIC))
        if len(digits) == 10 and digits.startswith("1"):
            reg = digits
        else:
            print(f"  ⚠️  Registration '{reg}' failed format validation — setting null")
            reg = None

    data = {
        "plate_number":        plate,
        "car_coding":          car_coding,
        "plate_number_only":   plate_number_only,
        "registration_number": reg,
        "chassis_number":      chassis,
        "policy_number":       policy,
    }
    _log_missing("vehicle_license", data)

    return {
        "success":  True,
        "engine":   "ollama",
        "model":    model,
        "doc_type": "vehicle_license",
        "quality":  None,
        "data":     data,
    }


# Notebook hybrid validation/correction prompt
# --------------------------------------------
# This is intentionally separate from extract_passport(). The notebook-based
# engine calls it after MRZ and visual OCR have already produced candidates.
# Ollama may resolve ambiguity, normalize, and fill low-confidence gaps; the
# hybrid arbitration layer protects valid MRZ values from being overwritten.

_PROMPT_PASSPORT_HYBRID_VALIDATE = """You are an identity document reader.
Read the passport image directly and extract fields exactly as printed.
Use the OCR/MRZ evidence below only as hints; if the image clearly shows a different value, copy the image text.

Rules:
- Return ONLY a JSON object. No markdown, no explanation.
- Do not invent values. Use null for invisible or unclear fields.
- Dates must be DD/MM/YYYY.
- full_name_en must be Latin/English text only, exactly as printed.
- full_name_ar must contain Arabic script only, exactly as printed next to the Arabic name label.
- Do NOT translate, transliterate, or infer Arabic names from English names.
- If the passport has no Arabic name, set full_name_ar to null.
- sex must be M or F.
- Prefer the printed visual Names/Name field over MRZ for full_name_en.
- MRZ names can be truncated by fixed width; never shorten a visible printed name to match MRZ.
- Use MRZ as a checksum/identity hint, not as the source for a longer printed visual name.

Return keys: passport_number, full_name_en, full_name_ar, date_of_birth,
date_of_expiry, date_of_issue, sex, nationality, national_number, place_of_birth,
mother_name, authority."""

_PROMPT_PASSPORT_ARABIC_NAME_ONLY = """You are reading only the Arabic full name on this passport image.
Return ONLY a JSON object. No markdown, no explanation.

Task:
- Find the Arabic name printed near the Arabic label "\u0627\u0644\u0627\u0633\u0645".
- Copy the entire Arabic name row exactly as printed, in normal Arabic reading order.
- The first returned word should be the rightmost name word closest to the Arabic name label.
- Do not omit the first Arabic word beside/after the label.
- If the Arabic row has multiple words on the same baseline, include all of them.
- Preserve the exact visible spelling and letters. Do not normalize, correct, expand, or add letters.
- Do not read MRZ.
- Do not translate or transliterate from English.
- Do not include labels, gender, nationality, dates, authority, or place names.
- If no Arabic name is printed or it is unclear, return {"full_name_ar": null}.

Return keys: full_name_ar."""

_PROMPT_PASSPORT_ENGLISH_NAME_ONLY = """You are reading only the Latin/English full name on this passport image.
Return ONLY a JSON object. No markdown, no explanation.

Task:
- Find the printed Latin/English holder name near labels like Name, Names, Full Name, or NAMA LENGKAP.
- Copy all visible Latin name words exactly as printed, preserving the printed order.
- Do not read or use the MRZ. MRZ contains < separators and may be truncated or OCR-corrupted.
- Do not include labels, passport number, country, nationality, dates, place names, or authority.
- If the printed Latin/English name is unclear, return {"full_name_en": null}.

Return keys: full_name_en."""

_PROMPT_NATIONAL_ID_HYBRID_VALIDATE_FRONT = """You are an identity document reader.
Read this Jordanian national ID FRONT image directly and extract fields exactly as printed.
Use the OCR/MRZ evidence below only as hints; if the image clearly shows a different value, copy the image text.

Rules:
- Return ONLY a JSON object. No markdown, no explanation.
- Do not invent values. Use null for invisible or unclear fields.
- Dates must be DD/MM/YYYY.
- full_name_ar must contain Arabic script only, exactly as printed next to the Arabic name label.
- full_name_en must contain Latin/English text only, exactly as printed next to the Name label.
- Do NOT translate, transliterate, or infer Arabic names from English names.
- gender must be M or F.
- For Arabic names, preserve dots and leading letters; do not drop initial alef in names.
- If Arabic name letters are unclear, return the visible Arabic text rather than guessing from English.

Return keys: national_number, full_name_ar, full_name_en, date_of_birth,
gender, place_of_birth, mother_name."""

_PROMPT_NATIONAL_ID_ARABIC_FRONT_ONLY = """You are reading only Arabic personal-name fields on this Jordanian national ID front image.
Return ONLY a JSON object. No markdown, no explanation.

Task:
- full_name_ar: copy the Arabic full name printed next to the Arabic label "\u0627\u0644\u0627\u0633\u0645".
- mother_name: copy the Arabic mother name printed next to the Arabic label "\u0627\u0633\u0645 \u0627\u0644\u0627\u0645".
- Do not translate or transliterate from English.
- Do not read MRZ.
- Do not include labels, gender, dates, national number, place of birth, or other fields.
- Use null for unclear fields.

Return keys: full_name_ar, mother_name."""


_PROMPT_NATIONAL_ID_HYBRID_VALIDATE_BACK = """You are an identity document reader.
Read this Jordanian national ID BACK image directly and extract fields exactly as printed.
Use the OCR/MRZ evidence below only as hints; if the image clearly shows a different value, copy the image text.

Rules:
- Return ONLY a JSON object. No markdown, no explanation.
- Do not invent values. Use null for invisible or unclear fields.
- Dates must be DD/MM/YYYY.
- MRZ lines must use only A-Z, 0-9, and < and should be exactly 30 characters if visible.

Return keys: document_number, expiry_date, blood_type, mrz_l1, mrz_l2, mrz_l3."""


def _json_compact(value) -> str:
    try:
        return json.dumps(value or {}, ensure_ascii=False, sort_keys=True)
    except TypeError:
        return "{}"


_ARABIC_SCRIPT_COUNTRY_CODES = {
    "DZA", "BHR", "COM", "DJI", "EGY", "IRQ", "JOR", "KWT",
    "LBN", "LBY", "MRT", "MAR", "OMN", "PSE", "QAT", "SAU",
    "SOM", "SDN", "SYR", "TUN", "ARE", "YEM", "IRN",
}
_ARABIC_SCRIPT_COUNTRY_WORDS = (
    "ARAB", "EMIRAT", "JORDAN", "SAUDI", "KUWAIT", "QATAR",
    "BAHRAIN", "OMAN", "EGYPT", "IRAQ", "SYRIA", "LEBANON",
    "PALEST", "YEMEN", "SUDAN", "LIBYA", "ALGERIA", "TUNISIA",
    "MOROCCO", "IRAN",
)


def _passport_has_arabic_name_context(context: dict | None) -> bool:
    context = context or {}
    for bucket in ("fields", "visual"):
        data = context.get(bucket) or {}
        for key in ("issuing_state", "nationality", "nationality_code"):
            value = str(data.get(key) or "").strip().upper()
            if value in _ARABIC_SCRIPT_COUNTRY_CODES:
                return True
            if any(word in value for word in _ARABIC_SCRIPT_COUNTRY_WORDS):
                return True
        if _str_field(data, "full_name_ar") or _str_field(data, "name_ar"):
            return True
    mrz_l1 = str(context.get("mrz_l1") or "").upper()
    if len(mrz_l1) >= 5 and mrz_l1[2:5] in _ARABIC_SCRIPT_COUNTRY_CODES:
        return True
    return False


def _latin_name_key(value: str | None) -> str:
    return re.sub(r"[^A-Z]", "", str(value or "").upper())


def _latin_name_words(value: str | None) -> list[str]:
    return re.findall(r"[A-Z]{2,}", str(value or "").upper())


def _latin_name_similarity(left: str | None, right: str | None) -> float:
    left_key = _latin_name_key(left)
    right_key = _latin_name_key(right)
    if not left_key or not right_key:
        return 0.0
    return SequenceMatcher(None, left_key, right_key).ratio()


def _looks_like_complete_latin_name(value: str | None) -> bool:
    words = _latin_name_words(value)
    if len(words) < 2:
        return False
    if any(len(word) > 24 for word in words):
        return False
    joined = " ".join(words)
    if any(label in joined for label in ("PASSPORT", "COUNTRY", "NATIONALITY", "AUTHORITY", "ISSUING")):
        return False
    return True


def _passport_needs_focused_english_name(context: dict | None, clean: dict | None) -> bool:
    context = context or {}
    clean = clean or {}
    fields = context.get("fields") or {}
    visual = context.get("visual") or {}
    visual_name = _first_str_field(visual, "full_name_en", "name_en")
    mrz_name = _first_str_field(fields, "full_name_en", "name_en")
    current_name = _first_str_field(clean, "full_name_en", "name_en")
    try:
        visual_conf = float(visual.get("_full_name_en_conf") or 0.0)
    except Exception:
        visual_conf = 0.0

    if not _looks_like_complete_latin_name(current_name) and not _looks_like_complete_latin_name(visual_name):
        return True
    if visual_name and visual_conf and visual_conf < float(os.environ.get("OCR_OLLAMA_FOCUSED_NAME_CONF", "0.70")):
        return True
    if visual_name and mrz_name and _latin_name_similarity(visual_name, mrz_name) < 0.86:
        return True
    if current_name and visual_name and _latin_name_similarity(current_name, visual_name) < 0.86:
        return True
    if current_name and mrz_name and _latin_name_similarity(current_name, mrz_name) < 0.78 and not visual_name:
        return True
    if (
        visual_name
        and current_name
        and len(_latin_name_words(visual_name)) >= 4
        and len(_latin_name_words(current_name)) < len(_latin_name_words(visual_name))
    ):
        return True
    return False


def _arabic_name_key(value: str | None) -> str:
    text = str(value or "")
    text = re.sub(r"[\u064B-\u065F\u0670\u0640]", "", text)
    return re.sub(r"[^\u0600-\u06FF]", "", text)


def _arabic_word_count(value: str | None) -> int:
    return len(re.findall(r"[\u0600-\u06FF]{2,}", str(value or "")))


def _arabic_similarity(left: str | None, right: str | None) -> float:
    left_key = _arabic_name_key(left)
    right_key = _arabic_name_key(right)
    if not left_key or not right_key:
        return 0.0
    return SequenceMatcher(None, left_key, right_key).ratio()


def _national_id_needs_focused_arabic(context: dict | None, clean: dict | None) -> bool:
    context = context or {}
    clean = clean or {}
    fields = context.get("fields") or {}
    visual_ar = _first_str_field(fields, "full_name_ar")
    current_ar = _first_str_field(clean, "full_name_ar")
    name_en = _first_str_field(clean, "full_name_en") or _first_str_field(fields, "full_name_en")
    latin_count = len(_latin_name_words(name_en))

    if _arabic_word_count(current_ar) < 2:
        return True
    if latin_count >= 3 and _arabic_word_count(current_ar) < max(2, latin_count - 1):
        return True
    if visual_ar and current_ar and _arabic_similarity(visual_ar, current_ar) < 0.66:
        return True
    mother = _first_str_field(clean, "mother_name") or _first_str_field(fields, "mother_name")
    if mother and re.search(r"[A-Za-z0-9]", mother):
        return True
    if not mother:
        return True
    return False


def validate_passport_hybrid(image_path: str,
                             extracted: dict | None = None,
                             mrz: dict | None = None,
                             visual: dict | None = None,
                             quality: dict | None = None) -> dict:
    """Notebook-style Ollama pass for ambiguity resolution and correction."""
    model = get_vision_model()
    if not model:
        return {}

    context = {
        "fields": extracted or {},
        "visual": visual or {},
        "mrz_valid": (mrz or {}).get("mrz_valid"),
        "mrz_checks": (mrz or {}).get("mrz_checks"),
        "mrz_l1": (mrz or {}).get("line1"),
        "mrz_l2": (mrz or {}).get("line2"),
    }
    prompt = (
        _PROMPT_PASSPORT_HYBRID_VALIDATE
        + "\n\nExisting OCR/MRZ evidence JSON:\n"
        + _json_compact(context)
    )
    data = _ask(model, prompt, image_path)
    _trace("OLLAMA_PASSPORT_VALIDATE_RAW_FIELDS", data)
    allowed = {
        "passport_number", "full_name_en", "full_name_ar", "date_of_birth",
        "date_of_expiry", "date_of_issue", "sex", "nationality",
        "national_number", "place_of_birth", "mother_name", "authority",
        "validation_notes",
    }
    clean = {k: v for k, v in (data or {}).items() if k in allowed}
    if _env_flag("OCR_OLLAMA_FOCUSED_PASSPORT_ENGLISH", "1") and _passport_needs_focused_english_name(context, clean):
        focused = _ask(model, _PROMPT_PASSPORT_ENGLISH_NAME_ONLY, image_path)
        _trace("OLLAMA_PASSPORT_ENGLISH_FOCUS_RAW_FIELDS", focused)
        focused_en = _first_str_field(focused, "full_name_en", "name_en")
        if focused_en:
            clean["full_name_en"] = focused_en
            clean["_full_name_en_focus_used"] = "1"
    if _env_flag("OCR_OLLAMA_FOCUSED_PASSPORT_ARABIC", "1") and _passport_has_arabic_name_context(context):
        focused = _ask(model, _PROMPT_PASSPORT_ARABIC_NAME_ONLY, image_path)
        _trace("OLLAMA_PASSPORT_ARABIC_FOCUS_RAW_FIELDS", focused)
        focused_ar = _first_str_field(focused, "full_name_ar", "name_ar")
        if focused_ar:
            clean["full_name_ar"] = focused_ar
            clean["_full_name_ar_focus_used"] = "1"
    clean["model"] = model
    _trace("OLLAMA_PASSPORT_VALIDATE_CLEAN", clean)
    return clean

def _env_flag(name: str, default: str = "0") -> bool:
    return str(os.environ.get(name, default)).strip().lower() in {"1", "true", "yes", "on"}


def _clean_allowed(raw: dict | None, allowed: set[str]) -> dict:
    clean = {}
    for key in allowed:
        value = _str_field(raw, key)
        if value:
            clean[key] = value
    return clean


def _needs_id_back_ollama(extracted: dict | None, mrz: dict | None) -> bool:
    if _env_flag("OCR_OLLAMA_VALIDATE_ID_BACK_ALWAYS", "0"):
        return True
    extracted = extracted or {}
    if any(not _str_field(extracted, key) for key in ("document_number", "expiry_date", "blood_type")):
        return True
    checks = (mrz or {}).get("mrz_checks") or {}
    return not bool((mrz or {}).get("mrz_valid") or checks.get("doc_number") or checks.get("expiry"))


def validate_jordan_id_hybrid(front_path: str,
                              back_path: str | None = None,
                              extracted: dict | None = None,
                              mrz: dict | None = None,
                              quality: dict | None = None) -> dict:
    """Fast Ollama validation pass for the notebook-style ID pipeline."""
    model = get_vision_model()
    if not model:
        return {}

    context = {
        "fields": extracted or {},
        "mrz_valid": (mrz or {}).get("mrz_valid"),
        "mrz_checks": (mrz or {}).get("mrz_checks"),
        "mrz_l1": (mrz or {}).get("mrz_l1") or (mrz or {}).get("line1"),
        "mrz_l2": (mrz or {}).get("mrz_l2") or (mrz or {}).get("line2"),
        "mrz_l3": (mrz or {}).get("mrz_l3") or (mrz or {}).get("line3"),
        "quality": quality or {},
    }
    front_prompt = (
        _PROMPT_NATIONAL_ID_HYBRID_VALIDATE_FRONT
        + "\n\nExisting OCR/MRZ evidence JSON:\n"
        + _json_compact(context)
    )
    front_raw = _ask(model, front_prompt, front_path)
    _trace("OLLAMA_NID_FRONT_VALIDATE_RAW_FIELDS", front_raw)
    clean = _clean_allowed(front_raw, {
        "national_number", "full_name_ar", "full_name_en", "date_of_birth",
        "gender", "place_of_birth", "mother_name",
    })

    if _env_flag("OCR_OLLAMA_FOCUSED_ID_ARABIC", "1") and _national_id_needs_focused_arabic(context, clean):
        focused = _ask(model, _PROMPT_NATIONAL_ID_ARABIC_FRONT_ONLY, front_path)
        _trace("OLLAMA_NID_ARABIC_FOCUS_RAW_FIELDS", focused)
        focused_ar = _first_str_field(focused, "full_name_ar")
        focused_mother = _first_str_field(focused, "mother_name")
        if focused_ar:
            clean["full_name_ar"] = focused_ar
            clean["_full_name_ar_focus_used"] = "1"
        if focused_mother:
            current_mother = _first_str_field(clean, "mother_name")
            if (
                not current_mother
                or re.search(r"[A-Za-z0-9]", current_mother)
                or _arabic_similarity(current_mother, focused_mother) < 0.55
            ):
                clean["mother_name"] = focused_mother
                clean["_mother_name_focus_used"] = "1"

    if back_path and _needs_id_back_ollama(extracted, mrz):
        back_prompt = (
            _PROMPT_NATIONAL_ID_HYBRID_VALIDATE_BACK
            + "\n\nExisting OCR/MRZ evidence JSON:\n"
            + _json_compact(context)
        )
        back_raw = _ask(model, back_prompt, back_path)
        _trace("OLLAMA_NID_BACK_VALIDATE_RAW_FIELDS", back_raw)
        clean.update(_clean_allowed(back_raw, {
            "document_number", "expiry_date", "blood_type",
            "mrz_l1", "mrz_l2", "mrz_l3",
        }))

    clean["model"] = model
    _trace("OLLAMA_NID_VALIDATE_CLEAN", clean)
    return clean
