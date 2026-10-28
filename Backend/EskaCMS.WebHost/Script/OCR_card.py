# -*- coding: utf-8 -*-
from http.server import BaseHTTPRequestHandler, HTTPServer
import sys
import os
import io
import contextlib
import warnings

os.environ["PYTHONIOENCODING"] = "utf-8"
os.environ["PYTHONUTF8"]       = "1"

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding="utf-8", errors="replace")


@contextlib.contextmanager
def suppress_stdout():
    old_stdout = sys.stdout
    try:
        sys.stdout = open(os.devnull, "w", encoding="utf-8")
        yield
    finally:
        sys.stdout.close()
        sys.stdout = old_stdout


with suppress_stdout():
    warnings.filterwarnings("ignore")
    import re
    import json
    import cv2
    import numpy as np
    import easyocr

with suppress_stdout():
    _reader = easyocr.Reader(["ar", "en"], gpu=False, verbose=False)


# ─────────────────────────────────────────────────────────────────────────────
#  PREPROCESSING  – 7 strategies targeting unclear/low-quality phone photos
# ─────────────────────────────────────────────────────────────────────────────
# def preprocess_image(image_path):
#     img = cv2.imread(image_path)
#     if img is None:
#         raise ValueError(f"Could not load image: {image_path}")

#     h, w = img.shape[:2]
#     if w > 1200:
#         scale = 1200 / w
#         img = cv2.resize(img, (int(w * scale), int(h * scale)),
#                          interpolation=cv2.INTER_AREA)
#     elif w < 800:
#         scale = 800 / w
#         img = cv2.resize(img, (int(w * scale), int(h * scale)),
#                          interpolation=cv2.INTER_CUBIC)

#     gray     = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
#     clahe    = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
#     enhanced = clahe.apply(gray)

#     return [enhanced]
def preprocess_image(image_path):
    img = cv2.imread(image_path)

    # resize
    h, w = img.shape[:2]
    if w > 1200:
        scale = 1200 / w
        img = cv2.resize(img, (int(w * scale), int(h * scale)),
                         interpolation=cv2.INTER_AREA)

    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)  # NO denoising!
    clahe = cv2.createCLAHE(clipLimit=3.0, tileGridSize=(8, 8))
    enhanced = clahe.apply(gray)

    s1 = enhanced
    s2 = cv2.bilateralFilter(enhanced, d=9, sigmaColor=75, sigmaSpace=75)
    s3 = cv2.adaptiveThreshold(enhanced, 255,cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY, 31, 10)
    kernel = np.array([[0, -1, 0], [-1, 5, -1], [0, -1, 0]])
    s4 = cv2.filter2D(enhanced, -1, kernel)

    return [s1, s2, s3,s4]
# ─────────────────────────────────────────────────────────────────────────────
#  OCR – run all strategies, deduplicate, merge
#  Confidence threshold 0.2 — lower than default to catch faint text
#  in low-quality images that EasyOCR reads with low confidence
# ─────────────────────────────────────────────────────────────────────────────
def run_ocr_multi(image_path):
    variants = preprocess_image(image_path)
    seen, merged = set(), []

    for variant in variants:
        with suppress_stdout():
            results = _reader.readtext(
                variant,
                detail=1,
                paragraph=False,       # keep individual word boxes
                width_ths=0.7,         # merge horizontally close boxes
                ycenter_ths=0.5,       # merge boxes on same line
                height_ths=0.5,
                text_threshold=0.5,    # min text confidence
                low_text=0.3,          # catches faint/unclear text
                link_threshold=0.4,
                canvas_size=1280,     #  limits internal processing size
                mag_ratio=1.0,        #  no magnification
            )

        for (_, text, conf) in results:
            t = text.strip()
            if not t or conf < 0.2:    # skip very low confidence noise
                continue
            key = re.sub(r"\s+", "", t).lower()
            if key not in seen:
                seen.add(key)
                merged.append({"text": t, "confidence": round(conf, 2)})

    return merged


# ─────────────────────────────────────────────────────────────────────────────
#  HELPERS
# ─────────────────────────────────────────────────────────────────────────────
def digits_only(text):
    return re.sub(r"[^\d]", "", text)


def is_date(text):
    return bool(re.fullmatch(r"\d{2}/\d{2}/\d{4}", text) or
                re.fullmatch(r"\d{4}/\d{2}/\d{2}", text))


def arabic_words_only(text):
    return re.sub(r"[^\u0600-\u06FF\s]", "", text).strip()


def is_arabic_word(text):
    ar = len(re.findall(r"[\u0600-\u06FF]", text))
    return ar > 0 and ar / max(len(text), 1) > 0.5


def normalise_ar(text):
    t = re.sub(r"[\u064B-\u065F]", "", text)
    t = re.sub(r"[إأآ]", "ا", t)
    t = re.sub(r"ى", "ي", t)
    t = re.sub(r"ة", "ه", t)
    return t


# ─────────────────────────────────────────────────────────────────────────────
#  MRZ PARSER
# ─────────────────────────────────────────────────────────────────────────────
def parse_mrz(texts):
    mrz_data, mrz_lines = {}, []
    for t in texts:
        clean = t.replace(" ", "").upper()
        if re.fullmatch(r"[A-Z0-9<]{15,}", clean):
            mrz_lines.append(clean)

    if len(mrz_lines) >= 1:
        line1 = mrz_lines[0]
        if line1.startswith("IDDOM") and len(line1) > 5:
            doc_raw = line1[5:].split("<")[0]
            if doc_raw:
                mrz_data["document_number"] = doc_raw

    if len(mrz_lines) >= 2:
        line2 = mrz_lines[1]
        if len(line2) >= 10:
            nat = line2[:10]
            if re.fullmatch(r"\d{10}", nat):
                mrz_data["national_number"] = nat
        if len(line2) >= 16:
            dob = line2[10:16]
            if re.fullmatch(r"\d{6}", dob):
                yy, mm, dd = int(dob[:2]), dob[2:4], dob[4:6]
                yyyy = 1900 + yy if yy > 30 else 2000 + yy
                mrz_data["date_of_birth"] = f"{dd}/{mm}/{yyyy}"
        if len(line2) >= 17 and line2[16] in ["M", "F"]:
            mrz_data["gender"] = "Male" if line2[16] == "M" else "Female"
        if len(line2) >= 23:
            exp = line2[17:23]
            if re.fullmatch(r"\d{6}", exp):
                yy2, mm2, dd2 = int(exp[:2]), exp[2:4], exp[4:6]
                mrz_data["expiry_date"] = f"{dd2}/{mm2}/{2000 + yy2}"

    if len(mrz_lines) >= 3:
        line3 = mrz_lines[2]
        parts = line3.split("<<")
        if len(parts) >= 2:
            surname = parts[0].replace("<", " ").strip()
            given   = parts[1].replace("<", " ").strip()
            mrz_data["full_name_en"] = f"{given} {surname}".strip()

    return mrz_data


# ─────────────────────────────────────────────────────────────────────────────
#  LABEL TABLE  (normalised for fuzzy matching)
# ─────────────────────────────────────────────────────────────────────────────
LABEL_TABLE = [
    ("الرقم الوطني",   "national_number"),
    ("الرقم الوطنى",   "national_number"),
    ("تاريخ الولادة",  "date_of_birth"),
    ("تاريخ الولاده",  "date_of_birth"),
    ("تاريخ الميلاد",  "date_of_birth"),
    ("مكان الولادة",   "place_of_birth"),   # BEFORE issue_place
    ("مكان الولاده",   "place_of_birth"),
    ("مكان الأصدار",   "issue_place"),
    ("مكان الاصدار",   "issue_place"),
    ("مكان الاقامة",   "residence_place"),
    ("مكان الاقامه",   "residence_place"),
    ("مكان القيد",     "registration_place"),
    ("اسم الأم",       "mothers_name"),
    ("اسم الام",       "mothers_name"),
    ("الجنس",          "gender"),
    ("الصلاحية",       "expiry_date"),
    ("الصلاحيه",       "expiry_date"),
    ("فصيلة الدم",     "blood_type"),
    ("فصيله الدم",     "blood_type"),
]

_LABEL_TABLE_NORM = [(normalise_ar(lbl), lbl, fkey) for lbl, fkey in LABEL_TABLE]


def find_label(text):
    nt = normalise_ar(text)
    for norm_lbl, canon_lbl, fkey in _LABEL_TABLE_NORM:
        if norm_lbl in nt:
            return canon_lbl, fkey
    return None


# ─────────────────────────────────────────────────────────────────────────────
#  FIELD MAPPER
# ─────────────────────────────────────────────────────────────────────────────
def map_jordan_id_fields(extracted_texts):
    fields = {
        "national_number":    None,
        "full_name_ar":       None,
        "full_name_en":       None,
        "date_of_birth":      None,
        "gender":             None,
        "expiry_date":        None,
        "place_of_birth":     None,
        "mothers_name":       None,
        "blood_type":         None,
        "issue_place":        None,
        "residence_place":    None,
        "registration_place": None,
        "document_number":    None,
        "nationality":        "Jordanian",
    }

    texts = [item["text"] for item in extracted_texts]
    n     = len(texts)

    fields.update({k: v for k, v in parse_mrz(texts).items() if v is not None})

    arabic_only_fields = {
        "place_of_birth", "mothers_name", "issue_place",
        "residence_place", "registration_place",
    }

    def resolve(raw, fkey):
        if not raw:
            return None
        raw = raw.strip()
        if fkey == "national_number":
            d = digits_only(raw)
            return d if len(d) == 10 else None
        if fkey in ("date_of_birth", "expiry_date"):
            dm = re.search(r"\d{2}/\d{2}/\d{4}|\d{4}/\d{2}/\d{2}", raw)
            return dm.group(0) if dm else None
        if fkey == "gender":
            if re.search(r"أنث|انث|female", raw, re.IGNORECASE): return "Female"
            if re.search(r"ذكر|male",raw, re.IGNORECASE): return "Male"
            return None
        if fkey == "blood_type":
            m = re.search(r"([ABO]{1,2}[+-])", raw, re.IGNORECASE)
            return m.group(0).upper() if m else None
        if fkey in arabic_only_fields:
            ar = arabic_words_only(raw)
            return ar if ar else None
        return raw

    for i, text in enumerate(texts):
        t = text.strip()

        # ── Arabic full name ─────────────────────────────────────────────────
        if fields["full_name_ar"] is None and re.search(r"الاسم|الإسم|الاصم", t):
            val = re.split(r"الاسم|الإسم|الاصم", t, maxsplit=1)[-1]
            val = re.sub(r"^[\s:：]+", "", val).strip()
            j = i + 1
            while j < n and j <= i + 6:
                nxt = texts[j].strip()
                if is_arabic_word(nxt) and not find_label(nxt) and len(nxt) <= 40:
                    val = (val + " " + nxt).strip()
                    j += 1
                else:
                    break
            fields["full_name_ar"] = val if len(val) > 2 else (
                texts[i + 1].strip() if i + 1 < n else None
            )

        # ── English full name ────────────────────────────────────────────────
        if fields["full_name_en"] is None:
            m = re.match(r"(?:Name\s*[:/]?\s*)([A-Z][A-Z\s]{4,})", t, re.IGNORECASE)
            if m:
                fields["full_name_en"] = m.group(1).strip()
            elif re.fullmatch(r"[A-Z]{2,}(?:\s+[A-Z]{2,}){2,}", t):
                fields["full_name_en"] = t

        # ── Label matching ───────────────────────────────────────────────────
        match = find_label(t)
        if match:
            canon_label, fkey = match
            if fields[fkey] is None:
                remainder = t.replace(canon_label, "").replace(":", "").strip()
                val = resolve(remainder, fkey)
                if val:
                    fields[fkey] = val
                elif i + 1 < n:
                    cand = texts[i + 1].strip()
                    if cand and cand != t:
                        fields[fkey] = resolve(cand, fkey)

        # ── Blood type standalone ────────────────────────────────────────────
        if fields["blood_type"] is None:
            m = re.search(r"(?:الدم|blood)\s*[:/]?\s*([ABO]{1,2}[+-])", t, re.IGNORECASE)
            if m:
                fields["blood_type"] = m.group(1).upper()
            elif re.fullmatch(r"[ABO]{1,2}[+-]", t.strip(), re.IGNORECASE):
                fields["blood_type"] = t.strip().upper()


        if fields["document_number"] is None:
            m = re.search(r"(?:[A-Z]{3}\d{5})", t, re.IGNORECASE)
            if m:
                fields["document_number"] = m.group(1).upper()
          
        if fields["mothers_name"] is None:
            m = re.search(r"(?:الام|الأم)\s*([\u0600\u06FF\s]+)", t, re.IGNORECASE)
            if m:
                fields["mothers_name"] = m.group(1).upper()
        # ── Standalone national number ───────────────────────────────────────
        if fields["national_number"] is None:
            d = digits_only(t)
            if len(d) == 10:
                fields["national_number"] = d

        # ── Dates ────────────────────────────────────────────────────────────
        if is_date(t):
            if   fields["date_of_birth"] is None: fields["date_of_birth"] = t
            elif fields["expiry_date"]   is None: fields["expiry_date"]   = t

        dm = re.search(r"(\d{2}/\d{2}/\d{4}|\d{4}/\d{2}/\d{2})", t)
        if dm:
            d = dm.group(1)
            if   fields["date_of_birth"] is None: fields["date_of_birth"] = d
            elif fields["expiry_date"]   is None and d != fields["date_of_birth"]:
                fields["expiry_date"] = d

        # ── Gender fallback ──────────────────────────────────────────────────
        if fields["gender"] is None:
            if re.search(r"أنث|انث|female", t, re.IGNORECASE): fields["gender"] = "Female"
            elif re.search(r"ذكر|male",     t, re.IGNORECASE): fields["gender"] = "Male"

    # ── Post-processing ──────────────────────────────────────────────────────
    g = fields.get("gender") or ""
    if g not in ("Male", "Female"):
        if   re.search(r"أنث|انث|female", g, re.IGNORECASE): fields["gender"] = "Female"
        elif re.search(r"ذكر|male",        g, re.IGNORECASE): fields["gender"] = "Male"
        else: fields["gender"] = None

    if fields["national_number"]:
        d = digits_only(str(fields["national_number"]))
        fields["national_number"] = d if len(d) == 10 else None

    if fields["place_of_birth"] is None and fields["issue_place"] is not None:
        val = fields["issue_place"]
        if is_arabic_word(val) and len(val.split()) <= 3:
            fields["place_of_birth"] = val
            fields["issue_place"]    = None

    return fields


# ─────────────────────────────────────────────────────────────────────────────
#  ENTRY POINT
# ─────────────────────────────────────────────────────────────────────────────
def extract_jordan_id(image_paths: list):
    try:
        all_texts = []
        for path in image_paths:
            all_texts.extend(run_ocr_multi(path))

        seen, unique = set(), []
        for item in all_texts:
            key = re.sub(r"\s+", "", item["text"]).lower()
            if key not in seen:
                seen.add(key)
                unique.append(item)

        output = {
            "success": True,
            "data":    map_jordan_id_fields(unique),
        }
    except Exception as e:
        output = {
            "success": False,
            "error":   str(e),
            "data":    {},
        }

    print(json.dumps(output, ensure_ascii=False))
    sys.stdout.flush()

#HTTP Handler
class OCRHandler(BaseHTTPRequestHandler):

    def do_GET(self):
        if self.path == "/health":
            self.send_response(200)
            self.send_header("Content-Type", "text/plain")
            self.end_headers()
            self.wfile.write(b"OK")

    # OCR endpoint C# calls this to scan ID
    def do_POST(self):
        try:
            length = int(self.headers["Content-Length"])
            body   = json.loads(self.rfile.read(length))
            paths  = body.get("image_paths", [])

            all_texts = []
            for path in paths:
                all_texts.extend(run_ocr_multi(path))

            seen, unique = set(), []
            for item in all_texts:
                key = re.sub(r"\s+", "", item["text"]).lower()
                if key not in seen:
                    seen.add(key)
                    unique.append(item)
                    
            response = json.dumps(result, ensure_ascii=False).encode("utf-8")
            self.send_response(200) 
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.send_header("Content-Length", len(response))
            self.end_headers()
            self.wfile.write(response)
            result = {
                "success": True,
                "data": map_jordan_id_fields(unique)
            }
        except Exception as e:
            result = {
                "success": False,
                "error": str(e),
                "data": {}
            }


    # Silence request logs
    def log_message(self, format, *args):
        pass
if __name__ == "__main__":
    server = HTTPServer(("127.0.0.1", 5050), OCRHandler)
    print("Server listening on http://127.0.0.1:5050", flush=True)
    server.serve_forever()

