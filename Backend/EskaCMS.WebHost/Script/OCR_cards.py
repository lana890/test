
# -*- coding: utf-8 -*-

import contextlib
import io
import json
import os
import re
import sys
import warnings
from http.server import BaseHTTPRequestHandler, HTTPServer

# ── Encoding setup (must happen before any print) ─────────────────────────────
os.environ["PYTHONIOENCODING"] = "utf-8"
os.environ["PYTHONUTF8"] = "1"
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", errors="replace")


@contextlib.contextmanager
def _silence():
    """Suppress stdout from noisy libraries (EasyOCR, torch, etc.)."""
    old = sys.stdout
    try:
        sys.stdout = open(os.devnull, "w", encoding="utf-8")
        yield
    finally:
        sys.stdout.close()
        sys.stdout = old


# ── Heavy imports (suppressed) ────────────────────────────────────────────────
with _silence():
    warnings.filterwarnings("ignore")
    import cv2
    import numpy as np
    import easyocr

print("🔄  Loading EasyOCR (ar + en) …", flush=True)
with _silence():
    READER_MIX = easyocr.Reader(["ar", "en"], gpu=True, verbose=False)
    READER_EN  = easyocr.Reader(["en"],        gpu=True, verbose=False)
print("✅  EasyOCR ready.", flush=True)

# ── Constants ───────────────────────────────────────────────────────
# ──────────
MRZ_ALLOWLIST = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<"
ARABIC_RE     = re.compile(r"[\u0600-\u06FF]")
SNAP          = 12   # pixel grid for token deduplication


# ══════════════════════════════════════════════════════════════════════════════
#  1. IMAGE LOADING & QUALITY
# ══════════════════════════════════════════════════════════════════════════════

def load_and_resize(path: str, max_width: int = 1400, min_width: int = 900):
    img = cv2.imread(path)
    if img is None:
        raise ValueError(f"Cannot load image: {path!r}")
    h, w = img.shape[:2]
    if w > max_width:
        img = cv2.resize(img, (max_width, int(h * max_width / w)),
                         interpolation=cv2.INTER_AREA)
    elif w < min_width:
        img = cv2.resize(img, (min_width, int(h * min_width / w)),
                         interpolation=cv2.INTER_CUBIC)
    return img


def compute_quality(gray) -> dict:
    blur       = float(cv2.Laplacian(gray, cv2.CV_64F).var())
    brightness = float(np.mean(gray) / 255.0)
    contrast   = float(np.std(gray)  / 255.0)
    return {
        "blur":       round(blur, 2),
        "brightness": round(brightness, 3),
        "contrast":   round(contrast, 3),
        "usable":     blur >= 40.0 and 0.10 <= brightness <= 0.95 and contrast >= 0.08,
    }


# ══════════════════════════════════════════════════════════════════════════════
#  2. PREPROCESSING VARIANTS
# ══════════════════════════════════════════════════════════════════════════════

def build_variants(img) -> dict:
    """Return dict of preprocessed grayscale images for the same source image."""
    gray     = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    clahe    = cv2.createCLAHE(clipLimit=1.2, tileGridSize=(16, 16))
    enhanced = clahe.apply(gray)

    thresholded = cv2.adaptiveThreshold(
        enhanced, 255,
        cv2.ADAPTIVE_THRESH_GAUSSIAN_C,
        cv2.THRESH_BINARY, 31, 10,
    )
    kernel    = np.array([[0, -1, 0], [-1, 5, -1], [0, -1, 0]])
    sharpened = cv2.filter2D(enhanced, -1, kernel)

    return {
        "gray":        gray,
        "enhanced":    enhanced,
        "thresholded": thresholded,
        "sharpened":   sharpened,
    }


def _region_metrics(source, x_min, y_min, x_max, y_max):
    h, w = source.shape[:2]
    crop = source[int(h * y_min):int(h * y_max),
                  int(w * x_min):int(w * x_max)]
    if crop.size == 0:
        return 1.0, 1.0, 1000.0
    return (float(np.mean(crop) / 255.0),
            float(np.std(crop)  / 255.0),
            float(cv2.Laplacian(crop, cv2.CV_64F).var()))


def select_source(variants: dict, x_min, y_min, x_max, y_max, mode="text"):
    """Pick the best preprocessed variant for a specific region."""
    brightness, contrast, blur = _region_metrics(
        variants["enhanced"], x_min, y_min, x_max, y_max)

    if mode == "mrz"   and (contrast < 0.10 or brightness < 0.18):
        return variants["thresholded"]
    if mode == "arabic" and blur < 50.0:
        return variants["sharpened"]
    if contrast < 0.08:
        return variants["thresholded"]
    if blur < 42.0:
        return variants["sharpened"]
    return variants["enhanced"]


# ══════════════════════════════════════════════════════════════════════════════
#  3. MRZ STRIP PREPROCESSING
# ══════════════════════════════════════════════════════════════════════════════

def preprocess_mrz_strip(img):
    """Isolate and binarise the MRZ zone from the back of the ID."""
    h, w   = img.shape[:2]
    img3x  = cv2.resize(img, (w * 3, h * 3), interpolation=cv2.INTER_CUBIC)
    gray3x = cv2.cvtColor(img3x, cv2.COLOR_BGR2GRAY)
    H, W   = gray3x.shape

    denoised = cv2.fastNlMeansDenoising(gray3x, h=15)
    _, binary = cv2.threshold(denoised, 0, 255,
                              cv2.THRESH_BINARY + cv2.THRESH_OTSU)

    col_proj  = np.sum(binary == 0, axis=1)
    min_cols  = W * 0.08
    start_y   = int(H * 0.60)
    mrz_rows  = [y for y in range(start_y, H) if col_proj[y] > min_cols]

    strip = binary[max(0, min(mrz_rows) - 30):] if mrz_rows else binary[int(H * 0.70):]

    # Drop leading rows that are >75 % black (stamp / hologram bleed)
    sh, sw     = strip.shape
    clean_start = 0
    for y in range(sh):
        if np.sum(strip[y] == 0) / sw < 0.75:
            clean_start = y
            break
    strip = strip[clean_start:]

    kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (2, 2))
    return cv2.morphologyEx(strip, cv2.MORPH_CLOSE, kernel)


# ══════════════════════════════════════════════════════════════════════════════
#  4. OCR HELPERS
# ══════════════════════════════════════════════════════════════════════════════

def _readtext(img, reader, allowlist=None, **kw):
    with _silence():
        return reader.readtext(img, detail=1, paragraph=False,
                               allowlist=allowlist, **kw)


def ocr_to_tokens(img, reader=None, allowlist=None, min_conf=0.05) -> list:
    """Run EasyOCR and return a deduplicated, sorted token list."""
    r      = reader or READER_MIX
    raw    = _readtext(img, r, allowlist=allowlist)
    deduped = {}
    for (bbox, text, conf) in raw:
        text = text.strip()
        if not text or conf < min_conf:
            continue
        xs  = [p[0] for p in bbox]
        ys  = [p[1] for p in bbox]
        cx  = int(round(((min(xs) + max(xs)) / 2) / SNAP) * SNAP)
        cy  = int(round(((min(ys) + max(ys)) / 2) / SNAP) * SNAP)
        key = (text.lower(), cx, cy)
        tok = {
            "text": text, "conf": round(conf, 3),
            "x":  min(xs), "y":  min(ys),
            "x2": max(xs), "y2": max(ys),
            "cx": (min(xs) + max(xs)) // 2,
            "cy": (min(ys) + max(ys)) // 2,
        }
        if key not in deduped or deduped[key]["conf"] < conf:
            deduped[key] = tok

    tokens = list(deduped.values())
    tokens.sort(key=lambda t: (t["y"] // 30, -t["x"]))
    return tokens


def ocr_mrz(mrz_strip) -> list:
    """OCR the pre-cropped MRZ strip (English-only + allowlist)."""
    raw    = _readtext(mrz_strip, READER_EN, allowlist=MRZ_ALLOWLIST)
    tokens = []
    for (bbox, text, conf) in raw:
        text = text.strip()
        if not text or conf < 0.05:
            continue
        ys = [p[1] for p in bbox]
        tokens.append({"text": text, "conf": round(conf, 3), "y": min(ys)})
    tokens.sort(key=lambda t: t["y"])
    return tokens


# ══════════════════════════════════════════════════════════════════════════════
#  5. FIELD PARSING — FRONT
# ══════════════════════════════════════════════════════════════════════════════

def _token_text_all(tokens: list) -> str:
    return " ".join(t["text"] for t in tokens)


def _extract_inline(text: str, patterns: list):
    for pat in patterns:
        m = re.search(pat, text)
        if m:
            return m.group(1).strip()
    return None


SKIP_WORDS = {
    "KINGDOM", "MINISTRY", "CIVIL", "PASSPORT",
    "STATUS", "CARD", "HASHEMITE", "INTERIOR",
}


def parse_front(tokens: list) -> dict:
    fields = {}
    full   = _token_text_all(tokens)

    # ── National Number ───────────────────────────────────────────────────────
    for t in tokens:
        m = re.search(r"(\d{8,12})", t["text"])
        if m:
            fields["national_number"] = m.group(1)
            break
    if "national_number" not in fields:
        m = re.search(r"(\d{8,12})", full)
        if m:
            fields["national_number"] = m.group(1)

    # ── Date of Birth ─────────────────────────────────────────────────────────
    dob = _extract_inline(full, [r"(\d{2}/\d{2}/\d{4})"])
    if dob:
        fields["date_of_birth"] = dob

    # ── Gender ────────────────────────────────────────────────────────────────
    for t in tokens:
        txt = t["text"]
        if "الجنس" in txt or "جنس" in txt:
            m = re.search(r"[:\s]+(.+)", txt)
            if m:
                val = re.split(r"\s{2,}|الولادة", m.group(1).strip())[0].strip()
                fields["gender_ar"] = val
                gm = re.search(r"\b([FM])\b", val)
                if gm:
                    fields["gender"] = "Male" if gm.group(1) == "M" else "Female"
            break
        if re.search(r"\b([FM])\b", txt) and re.search(r"(أنثى|أنثي|ذكر)", txt):
            gm = re.search(r"\b([FM])\b", txt)
            if gm:
                fields["gender"] = "Male" if gm.group(1) == "M" else "Female"
            am = re.search(r"(أنثى|أنثي|ذكر)", txt)
            if am:
                fields["gender_ar"] = am.group(1)

    # ── English Name ──────────────────────────────────────────────────────────
    for t in tokens:
        txt = t["text"]
        m = re.search(r"Name[a-zA-Z:/\s]{0,3}\s+([A-Z]{3,}.*)", txt)
        if m:
            raw = re.sub(r"[^A-Z\s]", "", m.group(1).upper()).strip()
            row = [r for r in tokens
                   if r is not t
                   and abs(r["cy"] - t["cy"]) <= 35
                   and r["x"] >= t["x2"]
                   and re.search(r"[A-Z]{2,}", r["text"])
                   and not any(w in r["text"] for w in SKIP_WORDS)]
            row.sort(key=lambda r: r["x"])
            parts = [raw] + [re.sub(r"[^A-Z\s]", "", r["text"].upper()).strip()
                             for r in row]
            fields["full_name_en"] = " ".join(p for p in parts if p)
            break
        if re.match(r"^[A-Z][A-Z\s]{10,}$", txt) and t["conf"] > 0.3:
            if not any(w in txt for w in SKIP_WORDS):
                fields["full_name_en"] = txt.strip()
                break

    # ── Arabic Name ───────────────────────────────────────────────────────────
    for t in tokens:
        txt = t["text"]
        if "الإسم" in txt or "الاسم" in txt:
            m      = re.search(r"(?:الإسم|الاسم)\s*[:\s]+(.+)", txt)
            inline = m.group(1).strip() if m else ""

            min_y = t["y"] - 30
            max_y = t["y"] + 80
            row_tokens = [r for r in tokens
                          if r is not t
                          and min_y <= r["y"] <= max_y
                          and ARABIC_RE.search(r["text"])
                          and not any(lbl in r["text"] for lbl in
                                      ["الرقم", "الجنس", "تاريخ", "مكان", "اسم",
                                       "وزارة", "دائرة", "بطاقة", "المملكة"])]
            row_tokens.sort(key=lambda r: -r["x"])
            parts  = ([inline] if inline else []) + [r["text"] for r in row_tokens]
            name_ar = " ".join(p.strip() for p in parts if p.strip())
            if name_ar:
                fields["full_name_ar"] = name_ar
            break

    # ── Place of Birth ────────────────────────────────────────────────────────
    for t in tokens:
        if "مكان" in t["text"] and "الولادة" in t["text"]:
            m = re.search(r"الولادة\s*[:\s]+(.+)", t["text"])
            if m:
                fields["place_of_birth"] = m.group(1).strip()
            break
    if "place_of_birth" not in fields:
        for t in tokens:
            if "الولادة" in t["text"] and ":" in t["text"] and not re.search(r"\d", t["text"]):
                m = re.search(r"الولادة\s*[:\s]+(.+)", t["text"])
                if m:
                    fields["place_of_birth"] = m.group(1).strip()

    # ── Mother's Name ─────────────────────────────────────────────────────────
    for t in tokens:
        if "الام" in t["text"] or "الأم" in t["text"]:
            m = re.search(r"(?:الام|الأم)\s*[:\s]+(.+)", t["text"])
            if m and m.group(1).strip() and m.group(1).strip() != ":":
                fields["mothers_name"] = m.group(1).strip()
            else:
                nearby = [r for r in tokens
                          if r is not t
                          and abs(r["cy"] - t["cy"]) <= 30
                          and ARABIC_RE.search(r["text"])
                          and r["text"].strip() not in ["اسم", "الام", "الأم"]]
                nearby.sort(key=lambda r: r["x"])
                if nearby:
                    fields["mothers_name"] = nearby[0]["text"].strip()
            break

    return fields


# ══════════════════════════════════════════════════════════════════════════════
#  6. FIELD PARSING — BACK
# ══════════════════════════════════════════════════════════════════════════════

def parse_back(tokens: list) -> dict:
    fields = {}

    # ── Expiry Date ───────────────────────────────────────────────────────────
    for t in tokens:
        txt = t["text"].replace("Z", "2").replace("O", "0").replace("I", "1")
        m = re.search(r"(\d{2}/\d{2}/\d{4})", txt)
        if m:
            fields["expiry_date"] = m.group(1)
            break

    # ── Issue Place ───────────────────────────────────────────────────────────
    for t in tokens:
        if "الاصدار" in t["text"] or "الإصدار" in t["text"]:
            m = re.search(r"(?:الاصدار|الإصدار)\s*[:\s]+(.+)", t["text"])
            if m:
                fields["issue_place"] = m.group(1).strip()
            break

    # ── Residence Place ───────────────────────────────────────────────────────
    for t in tokens:
        if "الاقامة" in t["text"] or "الإقامة" in t["text"]:
            m = re.search(r"(?:الاقامة|الإقامة)\s*[:\s]+(.+)", t["text"])
            if m:
                fields["residence_place"] = m.group(1).strip()
            break

    # ── Blood Type ────────────────────────────────────────────────────────────
    full  = _token_text_all(tokens)
    blood = _extract_inline(full, [r"\b([ABO]{1,2}[+-])\b"])
    if blood:
        fields["blood_type"] = blood

    # ── Registry Number ───────────────────────────────────────────────────────
    for t in tokens:
        txt = re.sub(r"[{\[\(]", "1", t["text"])
        m = re.search(r"(\d{3}/\d{3})", txt)
        if m:
            fields["registry_no"] = m.group(1)
            break

    # ── Document Number ───────────────────────────────────────────────────────
    doc_no = None
    for t in tokens:
        txt = (t["text"]
               .replace("[D", "ID").replace("[d", "ID")
               .replace("|0", "ID").replace("|O", "ID")
               .replace("l0", "ID").replace("lO", "ID"))
        if re.search(r"\bno\b|\bID No\b", txt, re.IGNORECASE):
            m = re.search(
                r"(?:n\s*[o0]?|no\.?|n°|ne|mo|wo|no)\s*[:.\-]?\s*(\d{5,})", txt)
            if m:
                doc_no = m.group(1)
            else:
                below = [b for b in tokens
                         if b is not t
                         and b["y"] > t["y2"]
                         and b["y"] - t["y2"] <= 60
                         and re.match(r"^[A-Z0-9]{4,}$", b["text"].strip())]
                below.sort(key=lambda b: b["y"])
                if below:
                    doc_no = below[0]["text"].strip()
            break

    if not doc_no:
        for t in tokens:
            m = re.match(r"^([A-Z]{2,4}\d{4,})$", t["text"].strip())
            if m:
                doc_no = m.group(1)
                break

    if doc_no:
        fields["document_number"] = doc_no

    return fields


# ══════════════════════════════════════════════════════════════════════════════
#  7. MRZ EXTRACTION & PARSING
# ══════════════════════════════════════════════════════════════════════════════

_MRZ_LINE_RE = re.compile(r"^[A-Z0-9<]{15,}$")


def _fix_mrz_token(text: str) -> str:
    return re.sub(r"[^A-Z0-9<]", "<", text.upper().replace(" ", ""))


def extract_mrz_lines(mrz_tokens: list) -> list:
    valid = []
    for t in mrz_tokens:
        cleaned = _fix_mrz_token(t["text"])
        if len(cleaned) >= 10 and _MRZ_LINE_RE.match(cleaned):
            valid.append((t["y"], cleaned))
    if not valid:
        return []

    valid.sort(key=lambda x: x[0])
    clustered, current_y, current_parts = [], valid[0][0], [valid[0][1]]
    for y, text in valid[1:]:
        if abs(y - current_y) < 30:
            current_parts.append(text)
        else:
            clustered.append("".join(current_parts))
            current_y, current_parts = y, [text]
    clustered.append("".join(current_parts))
    clustered = [ln.ljust(30, "<")[:30] for ln in clustered]

    for i, line in enumerate(clustered):
        if re.match(r"^ID[A-Z0-9]{1,3}", line) and i + 2 < len(clustered):
            return clustered[i:i + 3]
    for i, line in enumerate(clustered):
        if re.match(r"^\d{6}[0-9<][FM<]", line):
            return clustered[max(0, i - 1):max(0, i - 1) + 3]
    return clustered


def parse_mrz(lines: list) -> dict:
    if not lines:
        return {"error": "No MRZ lines detected"}

    result  = {"mrz_raw": lines, "mrz_lines_detected": len(lines)}
    padded  = [ln.ljust(30, "<")[:30] for ln in lines]

    if len(padded) >= 3:
        l1, l2, l3 = padded[0], padded[1], padded[2]
    elif len(padded) == 2:
        la, lb = padded
        if re.match(r"^\d{7}", la):
            l1, l2, l3 = "IDJOR" + "<" * 25, la, lb
        else:
            l1, l2, l3 = la, lb, "<" * 30
    else:
        l1 = padded[0]; l2 = l3 = "<" * 30

    result["mrz_doc_type"]    = l1[0:2].replace("<", "")
    result["mrz_country"]     = l1[2:5].replace("<", "")
    result["mrz_doc_number"]  = l1[5:14].replace("<", "")
    result["mrz_national_id"] = l1[15:28].replace("<", "").strip()
    result["mrz_dob"]         = l2[0:6]
    result["mrz_sex"]         = l2[7] if len(l2) > 7 else "?"
    result["mrz_expiry"]      = l2[8:14]
    result["mrz_nationality"] = l2[15:18].replace("<", "")

    # Name from line 3
    name_field = l3
    sep = re.search(r"<<+", name_field)
    if sep:
        surname   = name_field[:sep.start()].replace("<", " ").strip()
        given_raw = name_field[sep.end():]
        given     = re.sub(r"<+", " ", given_raw).strip()
    else:
        runs = [(m.start(), len(m.group()))
                for m in re.finditer(r"<+", name_field)]
        if runs:
            longest = max(runs, key=lambda r: r[1])
            surname = name_field[:longest[0]].replace("<", " ").strip()
            given   = name_field[longest[0] + longest[1]:].replace("<", " ").strip()
        else:
            surname = name_field.replace("<", " ").strip()
            given   = ""

    result["mrz_surname"]     = surname
    result["mrz_given_names"] = given
    result["full_name_en_mrz"] = (given + " " + surname).strip()

    # Optional checksum validation
    try:
        from mrz.checker.td1 import TD1CodeChecker
        result["mrz_valid"] = TD1CodeChecker(l1, l2, l3).valid()
    except Exception as exc:
        result["mrz_valid"] = f"check_skipped: {exc}"

    return result


# ══════════════════════════════════════════════════════════════════════════════
#  8. CROSS-VALIDATION & MERGE
# ══════════════════════════════════════════════════════════════════════════════

def _normalise_en(s: str) -> str:
    return re.sub(r"\s+", " ", re.sub(r"[^A-Z\s]", "", s.upper())).strip()


def _validate_name_en(field_ocr: str, mrz_surname: str, mrz_given: str):
    """Return best English name + source label."""
    sur      = mrz_surname.replace("<", " ").strip()
    gvn      = mrz_given.replace("<", " ").strip()
    mrz_name = (gvn + " " + sur).strip()

    if not mrz_name:
        return field_ocr, "field_ocr"
    if not field_ocr:
        return mrz_name, "mrz"

    mrz_words   = set(_normalise_en(mrz_name).split())
    field_words = set(_normalise_en(field_ocr).split())
    overlap     = len(mrz_words & field_words)
    similarity  = overlap / max(len(mrz_words), 1)
    surname_match = sur.upper() in _normalise_en(field_ocr) if sur else False

    if surname_match or similarity >= 0.4:
        return mrz_name, "mrz"
    elif similarity >= 0.2:
        return mrz_name, "mrz_low_confidence"
    else:
        return {"mrz": mrz_name, "field_ocr": field_ocr, "conflict": True}, "conflict"


def cross_validate(front: dict, back: dict, mrz: dict) -> tuple:
    # National number
    if "national_number" not in front and mrz.get("mrz_national_id"):
        front["national_number"] = mrz["mrz_national_id"]

    # Date of birth
    if "date_of_birth" not in front and mrz.get("mrz_dob") and len(mrz["mrz_dob"]) == 6:
        d = mrz["mrz_dob"]
        yy, mm, dd = d[0:2], d[2:4], d[4:6]
        yyyy = "20" + yy if int(yy) <= 30 else "19" + yy
        front["date_of_birth"] = f"{dd}/{mm}/{yyyy}"

    # English name
    sur  = mrz.get("mrz_surname", "")
    gvn  = mrz.get("mrz_given_names", "")
    name_result, source = _validate_name_en(front.get("full_name_en", ""), sur, gvn)
    if isinstance(name_result, dict):
        front["full_name_en"]          = name_result["field_ocr"]
        front["full_name_en_mrz"]      = name_result["mrz"]
        front["full_name_en_conflict"] = True
    else:
        front["full_name_en"] = name_result
        if source == "mrz_low_confidence":
            front["full_name_en_source"] = "mrz_low_confidence"

    # Document number
    if "document_number" not in back and mrz.get("mrz_doc_number"):
        back["document_number"] = mrz["mrz_doc_number"]

    # Expiry date
    if "expiry_date" not in back and mrz.get("mrz_expiry") and len(mrz["mrz_expiry"]) == 6:
        d = mrz["mrz_expiry"]
        yy, mm, dd = d[0:2], d[2:4], d[4:6]
        back["expiry_date"] = f"{dd}/{mm}/20{yy}"

    # Gender from MRZ sex field
    if "gender" not in front:
        sex = mrz.get("mrz_sex", "")
        if sex == "M":
            front["gender"] = "Male"
        elif sex == "F":
            front["gender"] = "Female"

    return front, back


# ══════════════════════════════════════════════════════════════════════════════
#  9. MAIN EXTRACTION PIPELINE
# ══════════════════════════════════════════════════════════════════════════════

def extract_jordan_id(front_path: str, back_path: str) -> dict:
    """
    Full pipeline: load → preprocess → OCR → parse → cross-validate.
    Returns a dict with keys: quality, data, mrz.
    """
    front_img = load_and_resize(front_path)
    back_img  = load_and_resize(back_path)

    front_variants = build_variants(front_img)
    back_variants  = build_variants(back_img)

    front_quality = compute_quality(front_variants["enhanced"])
    back_quality  = compute_quality(back_variants["enhanced"])

    # ── Front OCR ─────────────────────────────────────────────────────────────
    front_src    = select_source(front_variants, 0, 0, 1, 1, mode="text")
    front_tokens = ocr_to_tokens(front_src, READER_MIX)

    # ── Back OCR (two passes: mixed + MRZ-only) ───────────────────────────────
    back_src          = select_source(back_variants, 0, 0, 1, 0.65, mode="text")
    back_tokens_dict  = {}

    for tok in ocr_to_tokens(back_src, READER_MIX):
        cx  = int(round(tok["cx"] / SNAP) * SNAP)
        cy  = int(round(tok["cy"] / SNAP) * SNAP)
        key = (tok["text"].lower(), cx, cy)
        if key not in back_tokens_dict or back_tokens_dict[key]["conf"] < tok["conf"]:
            back_tokens_dict[key] = tok

    for tok in ocr_to_tokens(back_variants["thresholded"], READER_EN,
                              allowlist=MRZ_ALLOWLIST):
        cx  = int(round(tok["cx"] / SNAP) * SNAP)
        cy  = int(round(tok["cy"] / SNAP) * SNAP)
        key = (tok["text"].lower(), cx, cy)
        if key not in back_tokens_dict or back_tokens_dict[key]["conf"] < tok["conf"]:
            back_tokens_dict[key] = tok

    back_tokens = sorted(back_tokens_dict.values(),
                         key=lambda t: (t["y"] // 30, -t["x"]))

    # ── MRZ strip OCR ─────────────────────────────────────────────────────────
    mrz_strip  = preprocess_mrz_strip(back_img)
    mrz_tokens = ocr_mrz(mrz_strip)

    # ── Parse & cross-validate ────────────────────────────────────────────────
    front_data = parse_front(front_tokens)
    back_data  = parse_back(back_tokens)
    mrz_lines  = extract_mrz_lines(mrz_tokens)
    mrz_data   = parse_mrz(mrz_lines)

    front_data, back_data = cross_validate(front_data, back_data, mrz_data)

    # ── Flatten to canonical output schema ────────────────────────────────────
    data = {
        "national_number":    front_data.get("national_number"),
        "full_name_ar":       front_data.get("full_name_ar"),
        "full_name_en":       front_data.get("full_name_en"),
        "date_of_birth":      front_data.get("date_of_birth"),
        "gender":             front_data.get("gender"),
        "place_of_birth":     front_data.get("place_of_birth"),
        "mothers_name":       front_data.get("mothers_name"),
        "blood_type":         back_data.get("blood_type"),
        "expiry_date":        back_data.get("expiry_date"),
        "issue_place":        back_data.get("issue_place"),
        "residence_place":    back_data.get("residence_place"),
        "document_number":    back_data.get("document_number"),
        "nationality":        "Jordanian",
        # Extra detail flags (kept for debugging, safe to ignore in C#)
        "full_name_en_mrz":       front_data.get("full_name_en_mrz"),
        "full_name_en_conflict":  front_data.get("full_name_en_conflict", False),
        "full_name_en_source":    front_data.get("full_name_en_source"),
    }

    return {
        "success": True,
        "data":    data,
        "mrz":     mrz_data,
        "quality": {
            "front": front_quality,
            "back":  back_quality,
        },
    }


# ══════════════════════════════════════════════════════════════════════════════
#  10. HTTP SERVER
# ══════════════════════════════════════════════════════════════════════════════

class OCRHandler(BaseHTTPRequestHandler):

    # ── GET /health ───────────────────────────────────────────────────────────
    def do_GET(self):
        if self.path == "/health":
            self._send_json({"status": "ok"}, 200)
        else:
            self._send_json({"error": "Not found"}, 404)

    # ── POST /ocr ─────────────────────────────────────────────────────────────
    def do_POST(self):
        if self.path != "/ocr":
            self._send_json({"error": "Not found"}, 404)
            return

        try:
            length = int(self.headers.get("Content-Length", 0))
            body   = json.loads(self.rfile.read(length))
            paths  = body.get("image_paths", [])

            if len(paths) < 2:
                self._send_json(
                    {"success": False, "error": "image_paths must contain [front, back]"},
                    400,
                )
                return

            result = extract_jordan_id(paths[0], paths[1])

        except Exception as exc:
            result = {"success": False, "error": str(exc), "data": {}}

        self._send_json(result, 200)

    # ── Helper ────────────────────────────────────────────────────────────────
    def _send_json(self, payload: dict, status: int):
        body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, fmt, *args):
        # Replace built-in logging with a timestamped print so it goes to stdout
        print(f"[HTTP] {self.address_string()} {fmt % args}", flush=True)


# ══════════════════════════════════════════════════════════════════════════════
#  ENTRY POINT
# ══════════════════════════════════════════════════════════════════════════════

if __name__ == "__main__":
    HOST, PORT = "127.0.0.1", 5050
    server = HTTPServer((HOST, PORT), OCRHandler)
    print(f"🚀  OCR server listening on http://{HOST}:{PORT}", flush=True)
    print(f"    POST /ocr    — extract ID fields", flush=True)
    print(f"    GET  /health — liveness probe",    flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n🛑  Server stopped.", flush=True)
