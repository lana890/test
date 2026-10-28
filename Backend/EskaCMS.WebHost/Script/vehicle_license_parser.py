# vehicle_license_parser.py
# -*- coding: utf-8 -*-

import re
import cv2
import numpy as np
from collections import Counter

from ocr_engine import (
    _normalize_digits as _normalize_digits_engine,
    strip_arabic,
    tokens_right_of, tokens_below,
    READER_MIX, READER_EN, SNAP,
    build_variants, ocr_to_tokens, load_and_resize, select_source,
)

# ── Constants ─────────────────────────────────────────────────────────────────
_AR_INDIC_CHARS = "٠١٢٣٤٥٦٧٨٩"
_AMBIGUOUS      = set('260')   # digits EasyOCR misreads in Arabic context
_LOW_CONF       = 0.85


# ── Helpers ───────────────────────────────────────────────────────────────────
def _has_arabic_indic(text: str) -> bool:
    return any(c in _AR_INDIC_CHARS for c in text)

def _digits_only(text: str) -> str:
    return re.sub(r"\D", "", _normalize_digits_engine(text))

def _clean_alphanumeric(text: str) -> str:
    return re.sub(r"[^A-Z0-9]", "", text.upper().replace("O", "0").replace("I", "1"))

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
    text = _normalize_digits_engine(str(plate_number or "").strip())
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


def _normalize_plate_number_candidate(text: object) -> str | None:
    _, _, normalized = _split_plate_number(text)
    return normalized


def _is_valid_registration(number: str) -> bool:
    """Jordan DVLD: 10 digits, starts with 1."""
    return bool(number) and len(number) == 10 and number.startswith('1')

def _is_mrz_token(text: str) -> bool:
    """MRZ lines contain '<' and mix Latin letters with Arabic-Indic digits."""
    return '<' in text or (
        len(re.sub(r"[^A-Za-z]", "", text)) > 3
        and _has_arabic_indic(text)
    )

def _is_pure_arabic_indic_number(text: str) -> bool:
    """Token should be mostly Arabic-Indic digits, minimal noise chars."""
    d         = _digits_only(text)
    non_digit = re.sub(r"[٠-٩0-9]", "", text).strip()
    return len(d) >= 6 and len(non_digit) <= 2

def _auto_rotate(img: np.ndarray) -> np.ndarray:
    h, w = img.shape[:2]
    if h > w * 1.2:
        return cv2.rotate(img, cv2.ROTATE_90_CLOCKWISE)
    return img

def _is_label(text: str) -> bool:
    arabic_chars = sum(1 for c in text if "\u0600" <= c <= "\u06FF")
    return arabic_chars > len(text) * 0.35


# ── Re-read token at full resolution (majority vote) ─────────────────────────
def _reread_arabic_token(raw_img: np.ndarray, token: dict,
                         resized_shape: tuple, reader=None) -> str | None:
    """
    Crop the token from the FULL RESOLUTION original image.
    Scales bbox coords from resized → original, then runs 9 variants
    (3 scales × 3 preprocessings) and returns the majority-vote result.
    """
    r = reader or READER_MIX

    res_h,  res_w  = resized_shape[:2]
    orig_h, orig_w = raw_img.shape[:2]
    scale_x = orig_w / res_w
    scale_y = orig_h / res_h

    x1 = max(0,      int(token["x"]  * scale_x) - 15)
    y1 = max(0,      int(token["y"]  * scale_y) - 15)
    x2 = min(orig_w, int(token["x2"] * scale_x) + 15)
    y2 = min(orig_h, int(token["y2"] * scale_y) + 15)

    crop = raw_img[y1:y2, x1:x2]
    if crop.size == 0:
        return None

    gray = cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY) \
           if len(crop.shape) == 3 else crop.copy()

    results = []
    for scale in [3, 4, 5]:
        up = cv2.resize(gray, None, fx=scale, fy=scale,
                        interpolation=cv2.INTER_CUBIC)

        # Variant 1: CLAHE + Otsu
        clahe = cv2.createCLAHE(clipLimit=3.0, tileGridSize=(4, 4))
        cl    = clahe.apply(up)
        _, v1 = cv2.threshold(cl, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)

        # Variant 2: unsharp mask + Otsu
        blurred = cv2.GaussianBlur(up, (0, 0), sigmaX=2)
        sharp   = np.clip(
            cv2.addWeighted(up, 1.8, blurred, -0.8, 0), 0, 255
        ).astype(np.uint8)
        _, v2   = cv2.threshold(sharp, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)

        # Variant 3: raw upscale — preserves digit curves, no binarization
        v3 = up

        for variant in [v1, v2, v3]:
            for txt in r.readtext(variant, detail=0, paragraph=False):
                if _has_arabic_indic(txt):
                    n = _digits_only(txt)
                    if len(n) >= 6:
                        results.append(n)

    if not results:
        return None

    counts = Counter(results)
    return counts.most_common(1)[0][0]   # majority vote


# ── Registration extractor (full Colab logic) ─────────────────────────────────
def _extract_registration_rtl(tokens: list,
                               raw_img: np.ndarray,
                               resized_shape: tuple,
                               reader=None) -> str | None:
    """
    Step 1 — Filter to clean Arabic-Indic number tokens (exclude MRZ/garbage)
    Step 2 — Group into top row, sort RTL, quick join
    Step 3 — If valid + high confidence → done
    Step 4 — Re-read low-confidence ambiguous tokens at full resolution
    Step 5 — Sliding window fallback
    """
    r = reader or READER_MIX

    # Filter: only clean Arabic-Indic digit tokens, exclude MRZ lines
    ar_tokens = [
        t for t in tokens
        if _has_arabic_indic(t["text"])
        and not _is_mrz_token(t["text"])
        and _is_pure_arabic_indic_number(t["text"])
    ]

    print(f"  🔍 Arabic-Indic tokens (filtered): {len(ar_tokens)}")
    for t in ar_tokens:
        print(f"     conf={t['conf']:.2f}  '{t['text']}'  "
              f"→  '{_digits_only(t['text'])}'  "
              f"x={t['x']:.0f}  cy={t['cy']:.0f}")

    if not ar_tokens:
        print("  ⚠️  No clean Arabic-Indic tokens found")
        return None

    # Group into top row (registration is always row 1 on back card)
    ar_tokens.sort(key=lambda t: t["cy"])
    best_row   = ar_tokens[0]["cy"]
    row_tokens = [t for t in ar_tokens if abs(t["cy"] - best_row) <= 30]
    row_tokens.sort(key=lambda t: -t["x"])  # RTL: rightmost first

    print(f"  📋 Row tokens RTL: {[t['text'] for t in row_tokens]}")

    # Quick join
    joined = "".join(_digits_only(t["text"]) for t in row_tokens)
    print(f"  📌 Quick join: '{joined}' (len={len(joined)})")

    needs_reread = any(
        t["conf"] < _LOW_CONF
        and any(c in _AMBIGUOUS for c in _digits_only(t["text"]))
        for t in row_tokens
    )

    if _is_valid_registration(joined) and not needs_reread:
        print("  ✅ Quick join valid + high confidence")
        return joined

    if needs_reread:
        print("  ⚠️  Low confidence on ambiguous digits — re-reading at full resolution...")
    else:
        print("  ⚠️  Invalid length — re-reading...")

    # Per-token re-read
    improved = []
    for tok in row_tokens:
        d    = _digits_only(tok["text"])
        conf = tok["conf"]

        if conf < _LOW_CONF and any(c in _AMBIGUOUS for c in d):
            print(f"     🔁 Re-reading: '{tok['text']}'  conf={conf:.2f}  digits='{d}'")
            reread = _reread_arabic_token(raw_img, tok, resized_shape, r)
            if reread:
                print(f"        ✅ Re-read: '{reread}'")
                improved.append(reread)
            else:
                print(f"        📌 Re-read failed, keeping: '{d}'")
                improved.append(d)
        else:
            print(f"     ✓  Keeping '{d}'  (conf={conf:.2f})")
            improved.append(d)

    joined2 = "".join(improved)
    print(f"  📌 After re-read: '{joined2}' (len={len(joined2)})")

    if _is_valid_registration(joined2):
        print("  ✅ Re-read result valid")
        return joined2

    # Sliding window fallback
    combined = joined2 if len(joined2) >= len(joined) else joined
    for start in range(len(combined)):
        chunk = combined[start:start+10]
        if _is_valid_registration(chunk):
            print(f"  ✅ Window match: '{chunk}'")
            return chunk

    best = joined2 if len(joined2) >= len(joined) else joined
    print(f"  ⚠️  Returning best effort: '{best}'")
    return best if len(best) >= 6 else None


# ── Label/value helpers ───────────────────────────────────────────────────────
def _find_label_fuzzy(tokens: list, *fragments) -> dict | None:
    for t in tokens:
        for frag in fragments:
            if frag in t["text"]:
                return t
    for i in range(len(tokens) - 1):
        joined = tokens[i]["text"] + tokens[i+1]["text"]
        for frag in fragments:
            if frag in joined:
                return tokens[i]
    return None

def _value_left_of(tokens: list, label_tok: dict, max_dy: int = 35) -> list:
    return sorted(
        [t for t in tokens
         if t is not label_tok
         and abs(t["cy"] - label_tok["cy"]) <= max_dy
         and t["x2"] < label_tok["x"]
         and not _is_label(t["text"])],
        key=lambda t: -t["x"]
    )

def _best_value(tokens: list, label_tok: dict) -> str | None:
    left = _value_left_of(tokens, label_tok)
    if left:
        return " ".join(t["text"] for t in left).strip()
    right = tokens_right_of(tokens, label_tok)
    if right:
        return " ".join(t["text"] for t in right).strip()
    below = tokens_below(tokens, label_tok, max_dy=80,
                         x_lo=label_tok["x"] - 20,
                         x_hi=label_tok["x2"] + 500)
    if below:
        return below[0]["text"].strip()
    return None


# ── Token merge helper ────────────────────────────────────────────────────────
def _merge_token_lists(tokens_ar: list, tokens_en: list) -> list:
    token_dict = {
        (t["text"].lower(),
         int(round(t["cx"] / SNAP) * SNAP),
         int(round(t["cy"] / SNAP) * SNAP)): t
        for t in tokens_ar
    }
    for t in tokens_en:
        key = (t["text"].lower(),
               int(round(t["cx"] / SNAP) * SNAP),
               int(round(t["cy"] / SNAP) * SNAP))
        if key not in token_dict or token_dict[key]["conf"] < t["conf"]:
            token_dict[key] = t
    return sorted(token_dict.values(), key=lambda t: (t["y"] // 30, t["x"]))


# ── Field parsers ─────────────────────────────────────────────────────────────
def parse_front(tokens: list) -> dict:
    fields = {}
    full   = " ".join(t["text"] for t in tokens)

    for t in tokens:
        plate = _normalize_plate_number_candidate(t["text"])
        if plate:
            fields["plate_number"] = plate
            return fields
        m = re.search(r"\b([A-Z]{1,3}\d{5,8})\b", t["text"].upper())
        if m:
            fields["plate_number"] = m.group(1)
            return fields

    plate = _normalize_plate_number_candidate(full)
    if plate:
        fields["plate_number"] = plate
        return fields

    m = re.search(r"\b([A-Z]{1,3}\d{5,8})\b", full.upper())
    if m:
        fields["plate_number"] = m.group(1)

    return fields


def parse_back(tokens_raw: list,
               tokens_pre: list,
               raw_img: np.ndarray,
               resized_shape: tuple) -> dict:
    """
    tokens_raw  — from raw (unprocessed) image  → used for registration
    tokens_pre  — from preprocessed image       → used for chassis / policy
    raw_img     — full resolution original      → used for re-read crops
    resized_shape — shape of the resized image  → for coordinate scaling
    """
    fields = {}

    # ── Registration: raw tokens + full-res re-reads ──────────────────────────
    print("\n── Registration extraction ──")
    reg = _extract_registration_rtl(
        tokens_raw, raw_img, resized_shape, READER_MIX
    )
    if reg:
        fields["registration_number"] = reg

    # ── Chassis: preprocessed tokens, strict VIN pattern ─────────────────────
    VIN_PATTERN  = re.compile(r'\b[A-HJ-NPR-Z0-9]{17}\b')
    VIN_EXCLUDES = ['EXPIRY', 'DATE', 'EXPIRE', 'LICENSE']

    chassis_label = _find_label_fuzzy(
        tokens_pre, "رقم الهيكل", "هيكل", "الشاصي", "رقم الشاصي"
    )
    if chassis_label:
        raw_val = _best_value(tokens_pre, chassis_label)
        if raw_val:
          cleaned = _clean_alphanumeric(strip_arabic(raw_val))
    # Extract first 17-char VIN if present, else first ≥10-char chunk
          vin_match = VIN_PATTERN.search(cleaned)
        if vin_match:
          fields["chassis_number"] = vin_match.group(0)
        elif len(cleaned) >= 10:
        # Take only the first whitespace-separated alphanumeric chunk
          first_chunk = strip_arabic(raw_val).strip().upper().split()[0]
          fields["chassis_number"] = _clean_alphanumeric(first_chunk)

    if "chassis_number" not in fields:
        # Pass 1: strict 17-char VIN
        for t in tokens_pre:
            text = _clean_alphanumeric(strip_arabic(t["text"]))
            m    = VIN_PATTERN.search(text)
            if m:
                fields["chassis_number"] = m.group(0)
                break

    if "chassis_number" not in fields:
        # Pass 2: relaxed 15-18 chars, exclude date/expiry tokens
        for t in tokens_pre:
            upper = t["text"].upper()
            if any(kw in upper for kw in VIN_EXCLUDES):
                continue
            cleaned = _clean_alphanumeric(strip_arabic(t["text"]))
            if 15 <= len(cleaned) <= 18:
                fields["chassis_number"] = strip_arabic(t["text"]).strip().upper()
                break

    # ── Policy: search both raw and preprocessed ──────────────────────────────
    all_tokens = tokens_raw + tokens_pre
    reg_val    = fields.get("registration_number")

    pol_label = _find_label_fuzzy(
        all_tokens, "البوليصة", "رقم البوليصة", "البوليصه"
    )
    if pol_label:
        raw_val = _best_value(all_tokens, pol_label)
        if raw_val:
            n = _normalize_digits(raw_val)
            m = re.search(r"\d{6,15}", n)
            if m:
                fields["policy_number"] = m.group(0)

    if "policy_number" not in fields:
        # Standalone western-digit token, prefer longer
        candidates = []
        seen       = set()
        for t in all_tokens:
            raw = t["text"].strip()
            if re.fullmatch(r"[0-9]{7,15}", raw) and raw != reg_val and raw not in seen:
                candidates.append((len(raw), raw))
                seen.add(raw)
        if candidates:
            candidates.sort(reverse=True)
            fields["policy_number"] = candidates[0][1]

    return fields


def _normalize_digits(text: str) -> str:
    return _normalize_digits_engine(text)


# ── Main entry point ──────────────────────────────────────────────────────────
def extract_vehicle_license(front_path: str, back_path: str) -> dict:
    print("\n🔍 Loading vehicle license images...")

    # Load raw originals BEFORE any resize (needed for full-res re-reads)
    front_raw = _auto_rotate(cv2.imread(front_path))
    back_raw  = _auto_rotate(cv2.imread(back_path))

    if front_raw is None:
        raise ValueError(f"Cannot load front image: {front_path!r}")
    if back_raw is None:
        raise ValueError(f"Cannot load back image: {back_path!r}")

    # Resized versions for OCR token detection
    front_img = load_and_resize(front_path)
    back_img  = load_and_resize(back_path)
    back_img  = _auto_rotate(back_img)
    front_img = _auto_rotate(front_img)

    front_variants = build_variants(front_img)
    back_variants  = build_variants(back_img)

    # ── Front OCR ─────────────────────────────────────────────────────────────
    print("OCR: front (ar+en)...")
    front_tokens_ar = ocr_to_tokens(
        select_source(front_variants, 0, 0, 1, 1, mode="arabic"), READER_MIX)
    front_tokens_en = ocr_to_tokens(
        select_source(front_variants, 0, 0, 1, 1, mode="text"), READER_EN)
    front_tokens = _merge_token_lists(front_tokens_ar, front_tokens_en)

    # ── Back OCR — TWO separate passes ───────────────────────────────────────
    #
    # RAW back_img  → for registration (Arabic-Indic digits clearer without preprocessing)
    # PREPROCESSED  → for chassis VIN and policy (Latin chars need sharpening)
    #
    print("OCR: back raw (ar+en) — for registration...")
    back_tokens_raw_ar = ocr_to_tokens(back_img, READER_MIX)
    back_tokens_raw_en = ocr_to_tokens(back_img, READER_EN)
    back_tokens_raw    = _merge_token_lists(back_tokens_raw_ar, back_tokens_raw_en)

    print("OCR: back preprocessed (ar+en) — for chassis/policy...")
    back_tokens_pre_ar = ocr_to_tokens(
        select_source(back_variants, 0, 0, 1, 1, mode="arabic"), READER_MIX)
    back_tokens_pre_en = ocr_to_tokens(
        select_source(back_variants, 0, 0, 1, 1, mode="text"), READER_EN)
    back_tokens_pre    = _merge_token_lists(back_tokens_pre_ar, back_tokens_pre_en)

    # ── Parse ─────────────────────────────────────────────────────────────────
    print("🧩 Parsing fields...")
    front_fields = parse_front(front_tokens)
    back_fields  = parse_back(
        back_tokens_raw,   # raw tokens for registration
        back_tokens_pre,   # preprocessed tokens for chassis/policy
        back_raw,          # full resolution original for re-read crops
        back_img.shape     # resized shape for coordinate scaling
    )

    plate_number = front_fields.get("plate_number")
    car_coding, plate_number_only, normalized_plate = _split_plate_number(plate_number)
    plate_number = normalized_plate or plate_number
    result = {
        "plate_number":        plate_number,
        "car_coding":          car_coding,
        "plate_number_only":   plate_number_only,
        "registration_number": back_fields.get("registration_number"),
        "chassis_number":      back_fields.get("chassis_number"),
        "policy_number":       back_fields.get("policy_number"),
    }

    print("✅ Done:", result)
    return {
        "success":  True,
        "doc_type": "vehicle_license",
        "data":     result,
    }
