# # -*- coding: utf-8 -*-
# """
# passport_parser.py — International passport OCR (any ICAO TD3 country).

# Pipeline:
#   load_and_resize → build_variants → select_source(mode="mrz") for MRZ strip
#   → ocr_to_tokens (READER_EN + MRZ_ALLOWLIST) → extract_mrz_from_tokens
#   → normalize → _correct_passport_no → validate_td3 → output
# """

# from ocr_engine import (
#     READER_MIX,
#     load_and_resize,
#     build_variants,
#     select_source,
#     compute_quality,
#     ocr_to_tokens,
#     extract_mrz_from_tokens,
#     normalize_mrz_l1_td3,
#     normalize_mrz_l2_td3,
#     validate_td3,
#     split_mrz_name,
#     mrz_date,
#     GPU_INFO,
#     READER_EN,
#     MRZ_ALLOWLIST,
# )

# # ── Nationality code → label ──────────────────────────────────────────────────
# _NAT: dict[str, str] = {
#     "AFG": "Afghan",       "ALB": "Albanian",     "DZA": "Algerian",
#     "ARG": "Argentine",    "ARM": "Armenian",      "AUS": "Australian",
#     "AUT": "Austrian",     "AZE": "Azerbaijani",   "BHR": "Bahraini",
#     "BGD": "Bangladeshi",  "BLR": "Belarusian",    "BEL": "Belgian",
#     "BRA": "Brazilian",    "BGR": "Bulgarian",     "CAN": "Canadian",
#     "CHL": "Chilean",      "CHN": "Chinese",       "COL": "Colombian",
#     "HRV": "Croatian",     "CUB": "Cuban",         "CYP": "Cypriot",
#     "CZE": "Czech",        "DNK": "Danish",        "EGY": "Egyptian",
#     "EST": "Estonian",     "ETH": "Ethiopian",     "FIN": "Finnish",
#     "FRA": "French",       "DEU": "German",        "GHA": "Ghanaian",
#     "GRC": "Greek",        "HUN": "Hungarian",     "ISL": "Icelandic",
#     "IND": "Indian",       "IDN": "Indonesian",    "IRN": "Iranian",
#     "IRQ": "Iraqi",        "IRL": "Irish",         "ISR": "Israeli",
#     "ITA": "Italian",      "JPN": "Japanese",      "JOR": "Jordanian",
#     "KAZ": "Kazakhstani",  "KEN": "Kenyan",        "KOR": "South Korean",
#     "KWT": "Kuwaiti",      "LBN": "Lebanese",      "LBY": "Libyan",
#     "LTU": "Lithuanian",   "LUX": "Luxembourgish", "MYS": "Malaysian",
#     "MLT": "Maltese",      "MAR": "Moroccan",      "MEX": "Mexican",
#     "MDA": "Moldovan",     "MNG": "Mongolian",     "MNE": "Montenegrin",
#     "NPL": "Nepali",       "NLD": "Dutch",         "NZL": "New Zealander",
#     "NGA": "Nigerian",     "NOR": "Norwegian",     "OMN": "Omani",
#     "PAK": "Pakistani",    "PAN": "Panamanian",    "PER": "Peruvian",
#     "PHL": "Filipino",     "POL": "Polish",        "PRT": "Portuguese",
#     "PSE": "Palestinian",  "QAT": "Qatari",        "ROU": "Romanian",
#     "RUS": "Russian",      "SAU": "Saudi",         "SRB": "Serbian",
#     "SGP": "Singaporean",  "SVK": "Slovak",        "SVN": "Slovenian",
#     "SOM": "Somali",       "ZAF": "South African", "ESP": "Spanish",
#     "LKA": "Sri Lankan",   "SDN": "Sudanese",      "SWE": "Swedish",
#     "CHE": "Swiss",        "SYR": "Syrian",        "THA": "Thai",
#     "TUN": "Tunisian",     "TUR": "Turkish",       "UGA": "Ugandan",
#     "UKR": "Ukrainian",    "ARE": "Emirati",       "GBR": "British",
#     "USA": "American",     "VEN": "Venezuelan",    "VNM": "Vietnamese",
#     "YEM": "Yemeni",       "ZMB": "Zambian",       "ZWE": "Zimbabwean",
#     "UNO": "UN Organisation", "XXA": "Stateless",
#     "XXB": "Refugee",     "XXC": "Refugee (Other)", "XXX": "Unspecified",
# }

# _DOC_TYPE: dict[str, str] = {
#     "P":  "Passport",          "PA": "Passport",
#     "PC": "Passport Card",     "PD": "Diplomatic Passport",
#     "PS": "Service Passport",  "PO": "Official Passport",
#     "PN": "Non-national Passport", "PE": "Emergency Passport",
# }

# _SEX: dict[str, str] = {"M": "Male", "F": "Female", "<": "Unspecified"}


# # ══════════════════════════════════════════════════════════════════════════════
# #  PUBLIC ENTRY POINT
# # ══════════════════════════════════════════════════════════════════════════════

# def extract_passport(image_path: str) -> dict:
#     """
#     Extract fields from any ICAO TD3 passport using READER_EN + MRZ_ALLOWLIST.

#     Key design decisions:
#     - Use READER_EN (not READER_MIX) so the model is tuned for Latin/MRZ chars
#     - Use MRZ_ALLOWLIST to constrain OCR output to valid MRZ characters
#     - Use select_source(mode="mrz") which picks the best preprocessed variant
#       for reading the MRZ zone specifically
#     - After normalization, apply check-digit-guided passport_no correction
#       to fix any residual single-char errors (e.g. Z→2)
#     """
#     # ── 1. Load ───────────────────────────────────────────────────────────────
#     try:
#         img = load_and_resize(image_path)
#     except Exception as exc:
#         return _err(f"Cannot load image: {exc}")

#     variants = build_variants(img)
#     quality  = compute_quality(variants["enhanced"])

#     # ── 2. OCR — use MRZ-tuned source + English-only reader + allowlist ──────
#     # select_source(mode="mrz") picks thresholded variant for low-contrast MRZ zones
#     # READER_EN avoids Arabic model interference on Latin MRZ characters
#     # MRZ_ALLOWLIST restricts output to A-Z 0-9 < only
#     src_mrz = select_source(variants, mode="mrz")
#     try:
#         tokens = ocr_to_tokens(src_mrz, reader=READER_MIX, allowlist=MRZ_ALLOWLIST)
#     except Exception as exc:
#         return _err(f"OCR failed: {exc}")

#     if not tokens:
#         return _err("No OCR tokens — image may be blank or unreadable")

#     # ── 3. Locate MRZ lines ───────────────────────────────────────────────────
#     img_h      = src_mrz.shape[0]
#     mrz_tokens = extract_mrz_from_tokens(tokens, img_h, doc_type="passport")

#     if not mrz_tokens or len(mrz_tokens) < 2:
#         return {
#             "passport": {},
#             "mrz":      {"mrz_valid": False, "error": "MRZ lines not detected"},
#             "quality":  quality,
#         }

#     raw_line1 = mrz_tokens[0].get("text", "")
#     raw_line2 = mrz_tokens[1].get("text", "")

#     # ── 4. Normalize ──────────────────────────────────────────────────────────
#     line1 = normalize_mrz_l1_td3(raw_line1)
#     line2 = normalize_mrz_l2_td3(raw_line2)

#     if len(line1) != 44 or len(line2) != 44:
#         return {
#             "passport": {},
#             "mrz": {
#                 "mrz_valid": False,
#                 "error":  f"Bad MRZ length: L1={len(line1)} L2={len(line2)}",
#                 "line1":  line1,
#                 "line2":  line2,
#             },
#             "quality": quality,
#         }

#     # ── 5. Passport number correction (check-digit guided) ────────────────────
#     # normalize_mrz_l2_td3 applies global letter→digit corrections which can
#     # corrupt alphanumeric passport numbers (e.g. Z→2). This reverses that
#     # using the ICAO check digit as ground truth.
#     line2 = _correct_passport_no(line2)

#     # ── 6. Validate ───────────────────────────────────────────────────────────
#     checks    = validate_td3(line2)
#     mrz_valid = all(checks.values())

#     # ── 7. Build output ───────────────────────────────────────────────────────
#     passport = _build_passport_fields(line1, line2)

#     return {
#         "passport": {k: v for k, v in passport.items() if v},
#         "mrz": {
#             "mrz_valid":  mrz_valid,
#             "mrz_checks": checks,
#             "line1":      line1,
#             "line2":      line2,
#         },
#         "quality": quality,
#     }


# # ══════════════════════════════════════════════════════════════════════════════
# #  FIELD EXTRACTION
# # ══════════════════════════════════════════════════════════════════════════════

# def _build_passport_fields(line1: str, line2: str) -> dict:
#     doc_type_raw  = line1[0:2].rstrip("<").strip()
#     issuing_state = line1[2:5].replace("<", "").strip()
#     surname, given_names = split_mrz_name(line1[5:44])

#     passport_no  = line2[0:9].rstrip("<")
#     nat_code     = line2[10:13].replace("<", "").strip()
#     dob_raw      = line2[13:19]
#     sex_raw      = line2[20]
#     expiry_raw   = line2[21:27]
#     personal_raw = line2[28:42].replace("<", "").strip()

#     full_name   = f"{given_names} {surname}".strip() if given_names else surname
#     nationality = _NAT.get(nat_code, nat_code)
#     doc_type    = _DOC_TYPE.get(doc_type_raw, "Passport")
#     gender      = _SEX.get(sex_raw, sex_raw)
#     dob         = mrz_date(dob_raw,    kind="birth")
#     expiry      = mrz_date(expiry_raw, kind="expiry")

#     return {
#         "document_type":    doc_type,
#         "issuing_state":    issuing_state,
#         "passport_no":      passport_no,
#         "name_en":          full_name,
#         "surname":          surname,
#         "given_names":      given_names,
#         "nationality":      nationality,
#         "nationality_code": nat_code,
#         "gender":           gender,
#         "date_of_birth":    dob,
#         "expiry_date":      expiry,
#         "personal_no":      personal_raw or None,
#     }


# # ══════════════════════════════════════════════════════════════════════════════
# #  PASSPORT NUMBER CORRECTION  (check-digit guided, single-char errors only)
# # ══════════════════════════════════════════════════════════════════════════════

# # Common OCR-B confusions: what normalize_mrz_l2_td3 may have wrongly changed
# _CORRECTIONS: dict[str, list[str]] = {
#     # Digit introduced by normalizer, but original was a letter
#     "2": ["Z"],
#     "5": ["S"],
#     "8": ["B"],
#     "6": ["G"],
#     "1": ["I", "L"],
#     # Letter that OCR misread as another letter (normalizer didn't touch these)
#     "R": ["K"],
#     "K": ["R"],
#     "U": ["V"],
#     "V": ["U"],
#     "N": ["M"],
#     "M": ["N"],
#     "D": ["O"],
#     # Letter that should have been a digit
#     "I": ["1"],
#     "Z": ["2"],
#     "S": ["5"],
#     "B": ["8"],
#     "G": ["6"],
# }


# def _correct_passport_no(line2: str) -> str:
#     """
#     If the passport_no check digit (line2[9]) fails, try single-character
#     substitutions from _CORRECTIONS at each position of line2[0:9].
#     Only accepts a correction when the check digit passes.
#     Does NOT attempt multi-character corrections to avoid false positives.
#     """
#     doc_no = line2[0:9]
#     chk    = line2[9]

#     if _chk_ok(doc_no, chk):
#         return line2   # already valid

#     chars = list(doc_no)
#     for pos, ch in enumerate(chars):
#         for alt in _CORRECTIONS.get(ch, []):
#             candidate = "".join(chars[:pos] + [alt] + chars[pos + 1:])
#             if _chk_ok(candidate, chk):
#                 print(f"  [MRZ] passport_no fix pos {pos}: {ch!r}→{alt!r}  "
#                       f"({doc_no} → {candidate})")
#                 return candidate + line2[9:]

#     print(f"  [MRZ] passport_no: no single-char fix found for {doc_no!r}")
#     return line2


# # ══════════════════════════════════════════════════════════════════════════════
# #  ICAO CHECK DIGIT  (self-contained, no ocr_engine dependency)
# # ══════════════════════════════════════════════════════════════════════════════

# _ICAO_V: dict[str, int] = {c: i + 10 for i, c in enumerate("ABCDEFGHIJKLMNOPQRSTUVWXYZ")}
# _ICAO_V.update({str(i): i for i in range(10)})
# _ICAO_V["<"] = 0
# _ICAO_W = [7, 3, 1]


# def _icao_check(field: str) -> int:
#     return sum(_ICAO_V.get(c, 0) * _ICAO_W[i % 3] for i, c in enumerate(field)) % 10


# def _chk_ok(field: str, expected: str) -> bool:
#     expected = expected.replace("I", "1")
#     try:
#         return _icao_check(field) == int(expected)
#     except (ValueError, TypeError):
#         return False


# # ── Helpers ───────────────────────────────────────────────────────────────────

# def _err(message: str) -> dict:
#     return {
#         "passport": {},
#         "mrz":      {"mrz_valid": False, "error": message},
#         "quality":  {"usable": False},
#     }

# -*- coding: utf-8 -*-
"""
passport_parser.py — International passport OCR (any ICAO TD3 country).

Pipeline:
  load_and_resize → build_variants → select_source(mode="mrz") for MRZ strip
  → ocr_to_tokens (READER_MIX + MRZ_ALLOWLIST) → extract_mrz_from_tokens
  → normalize → _correct_passport_no → validate_td3
  → visual pass (READER_MIX, no allowlist) for Arabic name + issue date
  → output with per-field confidence
"""

import re
import unicodedata
from difflib import SequenceMatcher
from datetime import date as _date

from ocr_engine import (
    READER_MIX,
    READER_EN,
    load_and_resize,
    build_variants,
    select_source,
    compute_quality,
    ocr_to_tokens,
    extract_mrz_from_tokens,
    normalize_mrz_l1_td3,
    normalize_mrz_l2_td3,
    split_mrz_name,
    mrz_date,
    GPU_INFO,
    MRZ_ALLOWLIST,
    ARABIC_RE,
    repair_arabic_mojibake,
    strip_arabic,
)

# ── Nationality code → label ──────────────────────────────────────────────────
_NAT: dict[str, str] = {
    "AFG": "Afghan",       "ALB": "Albanian",     "DZA": "Algerian",
    "ARG": "Argentine",    "ARM": "Armenian",      "AUS": "Australian",
    "AUT": "Austrian",     "AZE": "Azerbaijani",   "BHR": "Bahraini",
    "BGD": "Bangladeshi",  "BLR": "Belarusian",    "BEL": "Belgian",
    "BRA": "Brazilian",    "BGR": "Bulgarian",     "CAN": "Canadian",
    "CHL": "Chilean",      "CHN": "Chinese",       "COL": "Colombian",
    "HRV": "Croatian",     "CUB": "Cuban",         "CYP": "Cypriot",
    "CZE": "Czech",        "DNK": "Danish",        "EGY": "Egyptian",
    "EST": "Estonian",     "ETH": "Ethiopian",     "FIN": "Finnish",
    "FRA": "French",       "DEU": "German",        "GHA": "Ghanaian",
    "GRC": "Greek",        "HUN": "Hungarian",     "ISL": "Icelandic",
    "IND": "Indian",       "IDN": "Indonesian",    "IRN": "Iranian",
    "IRQ": "Iraqi",        "IRL": "Irish",         "ISR": "Israeli",
    "ITA": "Italian",      "JPN": "Japanese",      "JOR": "Jordanian",
    "KAZ": "Kazakhstani",  "KEN": "Kenyan",        "KOR": "South Korean",
    "KWT": "Kuwaiti",      "LBN": "Lebanese",      "LBY": "Libyan",
    "LTU": "Lithuanian",   "LUX": "Luxembourgish", "MYS": "Malaysian",
    "MLT": "Maltese",      "MAR": "Moroccan",      "MEX": "Mexican",
    "MDA": "Moldovan",     "MNG": "Mongolian",     "MNE": "Montenegrin",
    "NPL": "Nepali",       "NLD": "Dutch",         "NZL": "New Zealander",
    "NGA": "Nigerian",     "NOR": "Norwegian",     "OMN": "Omani",
    "PAK": "Pakistani",    "PAN": "Panamanian",    "PER": "Peruvian",
    "PHL": "Filipino",     "POL": "Polish",        "PRT": "Portuguese",
    "PSE": "Palestinian",  "QAT": "Qatari",        "ROU": "Romanian",
    "RUS": "Russian",      "SAU": "Saudi",         "SRB": "Serbian",
    "SGP": "Singaporean",  "SVK": "Slovak",        "SVN": "Slovenian",
    "SOM": "Somali",       "ZAF": "South African", "ESP": "Spanish",
    "LKA": "Sri Lankan",   "SDN": "Sudanese",      "SWE": "Swedish",
    "CHE": "Swiss",        "SYR": "Syrian",        "THA": "Thai",
    "TUN": "Tunisian",     "TUR": "Turkish",       "UGA": "Ugandan",
    "UKR": "Ukrainian",    "ARE": "Emirati",       "GBR": "British",
    "USA": "American",     "VEN": "Venezuelan",    "VNM": "Vietnamese",
    "YEM": "Yemeni",       "ZMB": "Zambian",       "ZWE": "Zimbabwean",
    "UNO": "UN Organisation", "XXA": "Stateless",
    "XXB": "Refugee",     "XXC": "Refugee (Other)", "XXX": "Unspecified",
}

_DOC_TYPE: dict[str, str] = {
    "P":  "Passport",          "PA": "Passport",
    "PC": "Passport Card",     "PD": "Diplomatic Passport",
    "PS": "Service Passport",  "PO": "Official Passport",
    "PN": "Non-national Passport", "PE": "Emergency Passport",
}

_SEX: dict[str, str] = {"M": "Male/ذكر", "F": "Female/انثى", "<": "Unspecified"}

MIN_FIELD_CONF = 0.75

_PASSPORT_NAME_SKIP = {
    "THE", "HASHMITE", "HASHEMITE", "KINGDOM", "JORDAN", "PASSPORT",
    "TYPE", "COUNTRY", "CODE", "NO", "NUMBER", "DATE", "BIRTH", "PLACE",
    "SEX", "GENDER", "NATIONAL", "MOTHER", "AUTHORITY", "ISSUE", "EXPIRY",
    "EXPIRYDATE", "ISSUEDATE", "NAME",
}

_LATIN_NAME_LABEL_RE = re.compile(
    r"\b(?:NAME|NAMES|FULL\s*NAME|FULLNAME|NAMA|LENGKAP|SURNAME|GIVEN\s*NAMES?)\b",
    re.I,
)
_LATIN_HEADER_WORDS = {
    "PASSPORT", "PASSPOR", "PASSPON", "PSSPORT", "PSSPORTNO", "PASSPORTNO",
    "TYPE", "JENIS", "COUNTRY", "CODE", "KODE", "NEGARA", "NATIONALITY",
    "KEWARGANEGARAAN", "SEX", "KELAMIN", "DATE", "TGL", "BIRTH", "LAHIR",
    "EXPIRY", "EXPIRE", "EXPIRYDATE", "ISSUE", "ISSUING", "OFFICE", "KANTOR",
    "IMIGRASI", "PLACE", "TEMPAT", "REG", "REPUBLIC", "REPUBLIK", "KINGDOM",
    "HASHEMITE", "HASHMITE", "MINISTRY", "AUTHORITY", "MOTHER", "NUMBER",
}
_PASSPORT_AR_LABEL_WORDS = {
    "الاسم", "الإسم", "اسم", "لاسم", "لإسم", "جواز", "سفر",
    "الجنس", "تاريخ", "الميلاد", "الاصدار", "الإصدار", "الانتهاء",
    "الإنتهاء", "مكان", "الولادة", "الميلاد", "الام", "الأم",
    "الرقم", "الوطني", "الوطنية", "المملكة", "الاردنية", "الأردنية",
    "الهاشمية", "رمز", "الدولة", "نوع", "الجواز", "سلطة",
    "رفم", "رمر", "النرع", "اسبراز", "المرلة", "دلم", "ارضي",
}
_ARABIC_LABEL_HINTS = (
    "تاريخ", "ناريخ", "ميلاد", "مبلاد", "متلام", "ولادة",
    "صلاحية", "انتهاء", "اصدار", "مكان", "الجنس", "الرقم",
    "الوطني", "الوطنية", "الام", "جواز", "سفر", "رمز",
    "الدولة", "سلطة", "نوع",
)
_ARABIC_SCRIPT_COUNTRY_CODES = {
    "DZA", "BHR", "COM", "DJI", "EGY", "IRQ", "JOR", "KWT",
    "LBN", "LBY", "MRT", "MAR", "OMN", "PSE", "QAT", "SAU",
    "SOM", "SDN", "SYR", "TUN", "ARE", "YEM",
}
_MONTHS = {
    "JAN": 1, "JANUARY": 1,
    "FEB": 2, "FEBRUARY": 2,
    "MAR": 3, "MARCH": 3,
    "APR": 4, "APRI": 4, "APRIL": 4,
    "MAY": 5,
    "JUN": 6, "JUNE": 6,
    "JUL": 7, "JULY": 7,
    "AUG": 8, "AUGUST": 8,
    "SEP": 9, "SEPT": 9, "SEPTEMBER": 9,
    "OCT": 10, "OCTOBER": 10, "0CT": 10,
    "NOV": 11, "NOVEMBER": 11,
    "DEC": 12, "DECEMBER": 12,
}
# ══════════════════════════════════════════════════════════════════════════════
#  PUBLIC ENTRY POINT
# ══════════════════════════════════════════════════════════════════════════════

def extract_passport(image_path: str) -> dict:
    """
    Extract fields from any ICAO TD3 passport.

    Returns:
        passport     — extracted fields (value is None if conf < 0.75)
        field_confs  — per-field confidence scores for frontend use
        mrz          — raw MRZ data + validation checks
        quality      — image quality metrics
    """
    # ── 1. Load ───────────────────────────────────────────────────────────────
    try:
        img = load_and_resize(image_path)
    except Exception as exc:
        return _err(f"Cannot load image: {exc}")

    variants = build_variants(img)
    quality  = compute_quality(variants["enhanced"])

    # ── 2. MRZ OCR ────────────────────────────────────────────────────────────
    src_mrz = select_source(variants, mode="mrz")
    try:
        tokens = ocr_to_tokens(src_mrz, reader=READER_MIX, allowlist=MRZ_ALLOWLIST)
    except Exception as exc:
        return _err(f"OCR failed: {exc}")

    if not tokens:
        return _err("No OCR tokens — image may be blank or unreadable")

    # ── 3. Visual OCR (Arabic name + issue date) ──────────────────────────────
    text_tokens, latin_tokens = _collect_passport_visual_tokens(variants)

    # ── 4. Locate MRZ lines ───────────────────────────────────────────────────
    img_h      = src_mrz.shape[0]
    mrz_tokens = extract_mrz_from_tokens(tokens, img_h, doc_type="passport")

    if not mrz_tokens or len(mrz_tokens) < 2:
        return {
            "passport":    {},
            "field_confs": {},
            "mrz":         {"mrz_valid": False, "error": "MRZ lines not detected"},
            "quality":     quality,
        }

    raw_line1 = mrz_tokens[0].get("text", "")
    raw_line2 = mrz_tokens[1].get("text", "")

    # Raw OCR token confidence — used ONLY as a quality signal, not as field confidence.
    # EasyOCR reports per-character confidence which is naturally lower for MRZ OCR-B font.
    # A successfully located and parsed MRZ line is structurally reliable regardless of
    # the raw OCR score — the ICAO check digits are the real validity measure.
    mrz_token_conf = round(
        sum(t.get("conf", 0) for t in mrz_tokens) / len(mrz_tokens), 3
    )

    # Structural MRZ confidence: if MRZ lines were found and are the right length,
    # treat the parse as reliable. Scale from 0.80 (no checks) to 1.0 (checks pass).
    # This is what gets used for field_confs and _gate — not the raw token noise.
    mrz_conf = 0.82 if mrz_token_conf >= 0.35 else max(0.5, mrz_token_conf * 2.0)

    # ── 5. Normalize ──────────────────────────────────────────────────────────
    line1 = normalize_mrz_l1_td3(raw_line1)
    line2 = normalize_mrz_l2_td3(raw_line2)

    if len(line1) != 44 or len(line2) != 44:
        return {
            "passport":    {},
            "field_confs": {},
            "mrz": {
                "mrz_valid": False,
                "error":  f"Bad MRZ length: L1={len(line1)} L2={len(line2)}",
                "line1":  line1,
                "line2":  line2,
            },
            "quality": quality,
        }

    # ── 6. Passport number correction (check-digit guided) ────────────────────
    line2 = _correct_passport_no(line2)
    line2 = _correct_jordan_passport_prefix(line1, line2)
    line2 = line2[:10] + _normalize_country_code(line2[10:13]).ljust(3, "<")[:3] + line2[13:]

    # ── 7. Validate ───────────────────────────────────────────────────────────
    checks    = _validate_td3_local(line2)
    mrz_valid = all(checks.values())

    # ── 8. Build output ───────────────────────────────────────────────────────
    passport, field_confs = _build_passport_fields(
        line1, line2, text_tokens, latin_tokens, mrz_conf, checks, variants
    )

    return {
        "passport":    passport,
        "field_confs": field_confs,
        "mrz": {
            "mrz_valid":       mrz_valid,
            "mrz_checks":      checks,
            "mrz_token_conf":  mrz_token_conf,
            "line1":           line1,
            "line2":           line2,
        },
        "quality": quality,
    }


# ══════════════════════════════════════════════════════════════════════════════
#  FIELD EXTRACTION
# ══════════════════════════════════════════════════════════════════════════════

def _dedupe_visual_tokens(tokens: list[dict]) -> list[dict]:
    """Merge OCR tokens produced from multiple variants of the same image."""
    deduped: dict[tuple[str, int, int], dict] = {}
    for tok in tokens:
        text = (tok.get("text") or "").strip()
        if not text:
            continue
        x = int(round(float(tok.get("x", 0.0)) / 12.0) * 12)
        y = int(round(float(tok.get("cy", tok.get("y", 0.0))) / 12.0) * 12)
        key = (text.lower(), x, y)
        if key not in deduped or deduped[key].get("conf", 0.0) < tok.get("conf", 0.0):
            deduped[key] = tok
    result = list(deduped.values())
    result.sort(key=lambda t: (t.get("y", 0) // 30, t.get("x", 0)))
    return result


def _collect_passport_visual_tokens(variants: dict) -> tuple[list[dict], list[dict]]:
    """
    Read visual fields with purpose-specific OCR passes.

    Mixed Arabic/English is used once on the selected visual source for Arabic
    text. English-only is used for Latin visual fields so headers and MRZ-like
    text are less likely to pollute the name candidate list.
    """
    src_text = select_source(variants, mode="text")
    try:
        mixed_tokens = ocr_to_tokens(src_text, reader=READER_MIX)
    except Exception as exc:
        print(f"  [passport] mixed visual OCR failed: {exc}")
        mixed_tokens = []
    ar_preview = " | ".join(t.get("text", "") for t in mixed_tokens if ARABIC_RE.search(t.get("text", "")))
    if ar_preview:
        print(f"  [passport] Arabic OCR selected: {ar_preview[:220]}")

    try:
        latin_tokens = ocr_to_tokens(src_text, reader=READER_EN, min_conf=0.02)
    except Exception as exc:
        print(f"  [passport] English visual OCR failed: {exc}")
        latin_tokens = []

    return mixed_tokens, _dedupe_visual_tokens(latin_tokens)


def _passport_no_conf(value: str | None, check_passed: bool) -> float:
    """
    Confidence for passport number using content validation (like national_id).
    ICAO check digit passing is the strongest signal; format is secondary.
    """
    if not value:
        return 0.0
    if check_passed:
        return 1.0  # cryptographically verified
    # Format check: 2-4 letters + 4-8 digits (most common patterns)
    m = re.match(r"^([A-Z]{1,4})([0-9]{4,9})$", value)
    if not m:
        return 0.30
    digits = m.group(2)
    if len(set(digits)) == 1:
        return 0.40  # all same digit = OCR artefact
    return 0.70


def _dob_conf(value: str | None, check_passed: bool) -> float:
    """
    Confidence for date of birth using content validation (mirrors _date_of_birth_conf
    from national_id_parser but also honours the ICAO check digit result).
    """
    if not value:
        return 0.0
    if check_passed:
        return 1.0
    m = re.match(r"^(\d{1,2})[/.\-](\d{1,2})[/.\-](\d{4})$", value)
    if not m:
        return 0.20
    try:
        day, month, year = int(m.group(1)), int(m.group(2)), int(m.group(3))
        if not (1 <= month <= 12 and 1 <= day <= 31 and 1920 <= year <= 2020):
            return 0.25
        return 0.90
    except ValueError:
        return 0.20


def _expiry_conf(value: str | None, check_passed: bool) -> float:
    """
    Confidence for expiry date.  ICAO check digit is the primary signal.
    Without it, a future/recent date format is still high confidence.
    """
    if not value:
        return 0.0
    if check_passed:
        return 1.0
    m = re.match(r"^(\d{1,2})[/.\-](\d{1,2})[/.\-](\d{4})$", value)
    if not m:
        return 0.20
    try:
        day, month, year = int(m.group(1)), int(m.group(2)), int(m.group(3))
        if not (1 <= month <= 12 and 1 <= day <= 31 and 2000 <= year <= 2060):
            return 0.25
        return 0.90
    except ValueError:
        return 0.20


def _name_en_conf(value: str | None) -> float:
    """
    Content-based English name confidence (ported from national_id _latin_name_conf).
    MRZ name has no check digit, so we use word quality signals instead.
    """
    if not value:
        return 0.0
    if re.search(r"[^A-Za-z\s'\-]", value):
        return 0.25
    words = [w for w in value.strip().split() if w]
    if not words:
        return 0.0
    score = 0.0
    score += 0.25  # pure Latin
    if len(words) >= 2:
        score += 0.20
    min_len = min(len(w) for w in words)
    if min_len >= 2:
        score += 0.20
    if min_len >= 3:
        score += 0.15
    if sum(len(w) for w in words) >= 6:
        score += 0.20
    _SKIP = {"NAME","ID","NO","DATE","BIRTH","PLACE","SEX","GENDER","NATIONAL",
             "NUMBER","EXPIRY","EXP","ISSUE","REGISTRY","BLOOD","TYPE","CARD"}
    if any(w.upper() in _SKIP for w in words):
        score = max(0.0, score - 0.20)
    return round(min(max(score, 0.0), 1.0), 3)


def _name_ar_conf(value: str | None) -> float:
    """
    Content-based Arabic name confidence (ported from national_id _arabic_name_conf).
    """
    if not value:
        return 0.0
    if re.search(r"[A-Za-z0-9_:؛،]", value):
        return 0.25
    diacritics = len(re.findall(r"[\u064B-\u0652]", value))
    if diacritics > len(value) * 0.3:
        return 0.20
    words = [w for w in value.strip().split() if w]
    if not words:
        return 0.0
    score = 0.0
    if not re.search(r"[^ء-ي\s]", value):
        score += 0.25
    if len(words) >= 2:
        score += 0.20
    min_len = min(len(w) for w in words)
    if min_len >= 2:
        score += 0.20
    if min_len >= 3:
        score += 0.15
    if sum(len(w) for w in words) >= 8:
        score += 0.20
    return round(min(max(score, 0.0), 1.0), 3)


def _name_ar_conf(value: str | None) -> float:
    """Arabic name confidence using real Unicode Arabic letter ranges."""
    if not value:
        return 0.0
    if re.search(r"[A-Za-z0-9_:\u061B\u060C]", value):
        return 0.25
    diacritics = len(re.findall(r"[\u064B-\u0652]", value))
    if diacritics > len(value) * 0.3:
        return 0.20
    words = [w for w in value.strip().split() if w]
    if not words:
        return 0.0
    score = 0.0
    if not re.search(r"[^\u0621-\u064A\s]", value):
        score += 0.25
    if len(words) >= 2:
        score += 0.20
    min_len = min(len(w) for w in words)
    if min_len >= 2:
        score += 0.20
    if min_len >= 3:
        score += 0.15
    if sum(len(w) for w in words) >= 8:
        score += 0.20
    return round(min(max(score, 0.0), 1.0), 3)


def _nationality_conf(nat_code: str | None, nat_label: str | None) -> float:
    """3-letter ISO code that resolved to a known nationality → high confidence."""
    if not nat_code:
        return 0.0
    if nat_label and nat_label != nat_code:
        return 0.95  # resolved from _NAT table → known valid code
    if re.match(r"^[A-Z]{3}$", nat_code or ""):
        return 0.70  # valid format but unknown code
    return 0.30


def _gender_conf(value: str | None) -> float:
    """Valid bilingual gender string → high confidence."""
    if not value:
        return 0.0
    if value in ("Male/ذكر", "Female/انثى", "Unspecified"):
        return 0.95
    if value in ("Male", "Female", "M", "F"):
        return 0.85
    return 0.20


def _normalize_country_code(code: str | None) -> str:
    code = re.sub(r"[^A-Z0-9]", "", (code or "").upper())
    if code in {"JOR", "J0R", "10R", "1OR", "IOR"}:
        return "JOR"
    return code.replace("0", "O")


def _latin_ocr_text(text: str | None) -> str:
    text = strip_arabic(text or "").upper()
    text = text.replace("/", " ")
    text = re.sub(r"[^A-Z0-9\s]", " ", text)
    return re.sub(r"\s+", " ", text).strip()


def _latin_words(text: str | None) -> list[str]:
    return re.findall(r"[A-Z]{2,}", _latin_ocr_text(text))


def _has_latin_name_label(text: str | None) -> bool:
    return bool(_LATIN_NAME_LABEL_RE.search(_latin_ocr_text(text)))


def _is_latin_name_label_only_row(text: str | None) -> bool:
    words = _latin_words(text)
    if not words or not _has_latin_name_label(text):
        return False
    label_words = {"NAME", "NAMES", "FULL", "NAMA", "LENGKAP", "SURNAME", "GIVEN"}
    return all(word in label_words for word in words)


def _is_latin_passport_header_row(text: str | None) -> bool:
    norm = _latin_ocr_text(text)
    if not norm:
        return False
    words = _latin_words(norm)
    if not words:
        return False
    header_hits = 0
    for word in words:
        if word in _LATIN_HEADER_WORDS:
            header_hits += 1
            continue
        if word.startswith(("PASSP", "EXPIR", "ISSU", "NATION", "AUTH", "COUNTR", "REPUBLIC")):
            header_hits += 1
    if header_hits >= 2:
        return True
    return header_hits == 1 and len(words) <= 2 and not _has_latin_name_label(norm)


def _clean_visual_latin_name(text: str | None) -> str | None:
    if not text:
        return None
    text = strip_arabic(text).upper()
    text = text.replace("-", " ")
    text = re.sub(r"[^A-Z0-9\s]", " ", text)
    text = re.sub(r"\bNAME\b", " ", text)
    text = re.sub(r"\s+", " ", text).strip()
    if not text:
        return None

    words: list[str] = []
    for word in text.split():
        if word in _PASSPORT_NAME_SKIP or len(word) <= 1:
            continue
        if any(ch.isdigit() for ch in word):
            word = word.translate(str.maketrans({"0": "O", "1": "I", "5": "S", "8": "B"}))
        words.append(word)

    if len(words) < 2:
        return None
    return " ".join(words)


def _extract_visual_english_name_passport(tokens: list) -> tuple[str | None, float, float | None]:
    row_groups: dict[int, list] = {}
    for tok in tokens:
        text = tok.get("text", "")
        if "<" in text:
            continue
        if not re.search(r"[A-Za-z]{2,}", strip_arabic(text)):
            continue
        bucket = int(round(tok["cy"] / 24)) * 24
        row_groups.setdefault(bucket, []).append(tok)

    label_ys: list[float] = []
    for row in row_groups.values():
        row_sorted = sorted(row, key=lambda t: t["x"])
        merged = " ".join(t["text"] for t in row_sorted)
        if _has_latin_name_label(merged):
            label_ys.append(sum(t.get("cy", 0.0) for t in row) / len(row))

    candidates: list[tuple[float, str, float, float]] = []
    for row in row_groups.values():
        row_sorted = sorted(row, key=lambda t: t["x"])
        merged = " ".join(t["text"] for t in row_sorted)
        row_norm = _latin_ocr_text(merged)
        if re.search(r"\d", merged):
            continue
        if min(t["cy"] for t in row) < 110:
            continue
        cleaned = _clean_visual_latin_name(merged)
        if not cleaned:
            continue
        words = cleaned.split()
        upper_words = {w.upper() for w in words}

        avg_cy = sum(t.get("cy", 0.0) for t in row) / len(row)
        anchored_below_name_label = any(8 <= avg_cy - label_y <= 150 for label_y in label_ys)
        same_row_name_label = (
            any(abs(avg_cy - label_y) <= 22 for label_y in label_ys)
            and not _is_latin_name_label_only_row(row_norm)
        )

        if _is_latin_passport_header_row(row_norm):
            continue
        if _is_latin_name_label_only_row(row_norm) and not anchored_below_name_label:
            continue
        if not anchored_below_name_label and not same_row_name_label and len(words) < 3:
            continue
        if upper_words & {"HASHMITE", "HASHEMITE", "KINGDOM", "PASSPORT", "COUNTRY", "NATIONAL"}:
            continue
        if any(
            w.startswith(("DAT", "EXP", "ISS", "BIR", "MOTH", "AUTH", "PLACE"))
            or "EXPIR" in w
            or "ERPIR" in w
            for w in upper_words
        ):
            continue
        avg_conf = sum(t.get("conf", 0.0) for t in row) / len(row)
        y_bonus = max(0.0, 1.0 - (min(t["cy"] for t in row) / 700.0))
        score = len(words) * 12.0 + avg_conf * 30.0 + y_bonus * 8.0
        if anchored_below_name_label:
            score += 45.0
        elif same_row_name_label:
            score += 30.0
        if _is_latin_name_label_only_row(row_norm):
            score -= 12.0
        candidates.append((score, cleaned, avg_conf, avg_cy))

    if not candidates:
        return None, 0.0, None
    candidates.sort(key=lambda item: (item[0], len(item[1])), reverse=True)
    _, name, conf, avg_cy = candidates[0]
    return name, round(conf, 3), avg_cy


def _split_name_parts_from_full(full_name: str, mrz_surname: str | None = None) -> tuple[str, str]:
    words = [w for w in re.split(r"\s+", (full_name or "").replace("-", " ").strip()) if w]
    if not words:
        return "", ""
    if len(words) == 1:
        return words[0], ""

    surname_words = 1
    mrz_words = [w for w in re.split(r"\s+", (mrz_surname or "").replace("-", " ").strip()) if w]
    if 0 < len(mrz_words) <= 3 and words[-len(mrz_words):] == mrz_words:
        surname_words = len(mrz_words)

    surname = " ".join(words[-surname_words:])
    given = " ".join(words[:-surname_words])
    return surname, given


def _has_arabic_passport_context(issuing_state: str | None, nat_code: str | None, tokens: list | None) -> bool:
    if issuing_state in _ARABIC_SCRIPT_COUNTRY_CODES or nat_code in _ARABIC_SCRIPT_COUNTRY_CODES:
        return True
    return any(ARABIC_RE.search(t.get("text", "")) for t in (tokens or []))


def _compact_latin_name_key(value: str | None) -> str:
    return re.sub(r"[^A-Z]", "", strip_arabic(value or "").upper())


def _latin_name_similarity(left: str | None, right: str | None) -> float:
    left_key = _compact_latin_name_key(left)
    right_key = _compact_latin_name_key(right)
    if not left_key or not right_key:
        return 0.0
    return SequenceMatcher(None, left_key, right_key).ratio()


def _display_mrz_name(value: str | None) -> str | None:
    if not value:
        return None
    value = strip_arabic(value).upper().replace("-", " ")
    value = re.sub(r"[^A-Z\s]", " ", value)
    value = re.sub(r"\s+", " ", value).strip()
    return value or None


def _split_glued_visual_name_with_mrz(visual_name: str, mrz_name: str | None) -> str:
    mrz_display = _display_mrz_name(mrz_name)
    if not mrz_display:
        return visual_name

    visual_words = [w for w in re.split(r"\s+", visual_name.strip()) if w]
    mrz_words = [w for w in re.split(r"\s+", mrz_display.strip()) if w]
    if not visual_words or not mrz_words:
        return visual_name

    output: list[str] = []
    mrz_pos = 0
    changed = False
    for word in visual_words:
        compact_word = _compact_latin_name_key(word)
        if not compact_word:
            continue

        matched = False
        for end in range(mrz_pos + 1, min(len(mrz_words), mrz_pos + 5) + 1):
            chunk = mrz_words[mrz_pos:end]
            if "".join(_compact_latin_name_key(part) for part in chunk) == compact_word:
                output.extend(chunk)
                changed = changed or len(chunk) > 1 or chunk[0] != word
                mrz_pos = end
                matched = True
                break

        if matched:
            continue

        output.append(word)
        for i in range(mrz_pos, len(mrz_words)):
            if _compact_latin_name_key(mrz_words[i]) == compact_word:
                mrz_pos = i + 1
                break

    return " ".join(output) if changed else visual_name


def _space_visual_name_with_mrz(visual_name: str | None, mrz_name: str | None) -> str | None:
    """
    Fix OCR-glued visual names using MRZ spacing only when both contain the
    same Latin letters. This does not translate or substitute names.
    """
    if not visual_name:
        return _display_mrz_name(mrz_name)
    visual_name = _display_mrz_name(visual_name) or visual_name
    mrz_display = _display_mrz_name(mrz_name)
    if not mrz_display:
        return visual_name
    if _compact_latin_name_key(visual_name) == _compact_latin_name_key(mrz_display):
        return mrz_display
    visual_key = _compact_latin_name_key(visual_name)
    mrz_key = _compact_latin_name_key(mrz_display)
    if mrz_key and visual_key.startswith(mrz_key) and len(visual_key) > len(mrz_key):
        return visual_name
    for visual_word, mrz_word in zip(visual_name.split(), mrz_display.split()):
        if (
            len(visual_word) > len(mrz_word) >= 4
            and _compact_latin_name_key(visual_word).startswith(_compact_latin_name_key(mrz_word))
        ):
            return visual_name
    visual_words = visual_name.split()
    mrz_words = mrz_display.split()
    if (
        abs(len(visual_words) - len(mrz_words)) <= 1
        and _latin_name_similarity(visual_name, mrz_display) >= 0.90
    ):
        return mrz_display
    return _split_glued_visual_name_with_mrz(visual_name, mrz_display)


def _arabic_name_too_short_for_latin(name_ar: str | None, name_en: str | None) -> bool:
    if not name_ar or not name_en:
        return False
    ar_words = [w for w in re.split(r"\s+", name_ar.strip()) if w]
    en_words = [w for w in re.split(r"\s+", _display_mrz_name(name_en) or "") if w]
    if len(en_words) >= 4 and len(ar_words) <= 3 and sum(len(w) for w in en_words) >= 18:
        return True
    return len(en_words) >= 5 and len(ar_words) <= len(en_words) - 2


def _extract_arabic_name_from_name_band(
    variants: dict | None,
    latin_name_y: float | None,
) -> tuple[str | None, float]:
    """
    Retry Arabic OCR on the right-side name band. This is image OCR only; it
    does not translate or map the English name to Arabic.
    """
    if not variants or latin_name_y is None:
        return None, 0.0
    try:
        import cv2
    except Exception:
        return None, 0.0

    candidates: list[tuple[float, str, float]] = []
    for label in ("enhanced", "sharpened", "gray"):
        source = variants.get(label)
        if source is None:
            continue
        h, w = source.shape[:2]
        y0 = max(0, int(latin_name_y - 55))
        y1 = min(h, int(latin_name_y + 65))
        x0 = max(0, int(w * 0.42))
        crop = source[y0:y1, x0:w]
        if crop is None or crop.size == 0:
            continue

        if len(crop.shape) == 3:
            gray = cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY)
        else:
            gray = crop
        gray = cv2.resize(gray, None, fx=3, fy=3, interpolation=cv2.INTER_LANCZOS4)
        blur = cv2.GaussianBlur(gray, (0, 0), sigmaX=1.2)
        sharp = cv2.addWeighted(gray, 1.75, blur, -0.75, 0)
        clahe = cv2.createCLAHE(clipLimit=2.6, tileGridSize=(4, 4))
        enhanced = clahe.apply(sharp)

        for variant in (enhanced, sharp):
            try:
                tokens = ocr_to_tokens(variant, reader=READER_MIX, min_conf=0.01)
            except Exception:
                continue
            name, raw_conf = _extract_arabic_name_passport(tokens, None)
            cleaned = _clean_visual_arabic_name(name)
            if not cleaned:
                continue
            conf = max(_name_ar_conf(cleaned), raw_conf)
            candidates.append((conf + min(len(cleaned.split()), 6) * 0.03, cleaned, conf))

    if not candidates:
        return None, 0.0
    candidates.sort(key=lambda item: (item[0], len(item[1])), reverse=True)
    _, name, conf = candidates[0]
    return name, round(min(conf, 1.0), 3)


def _clean_visual_arabic_name(text: str | None) -> str | None:
    text = repair_arabic_mojibake(str(text or ""))
    if not text or not ARABIC_RE.search(text):
        return None
    text = unicodedata.normalize("NFKC", text)
    text = re.sub(r"[\u064B-\u065F\u0670\u0640]", "", text)
    text = re.sub(r"[\u0623\u0625\u0622\u0671]", "\u0627", text)
    text = text.replace("\u0649", "\u064A")
    text = re.sub(r"[A-Za-z0-9_]+", " ", text)
    text = re.sub(r"[^\u0600-\u06FF\s]", " ", text)
    words = []
    for word in re.split(r"\s+", text.strip()):
        word = word.strip(" :;,.-_/\\|\u061B\u060C")
        if len(word) < 2 or word in _PASSPORT_AR_LABEL_WORDS:
            continue
        words.append(word)
    if len(words) < 2:
        return None
    return re.sub(r"\s+", " ", " ".join(words)).strip() or None


def _arabic_label_hint_count(text: str | None) -> int:
    if not text:
        return 0
    norm = unicodedata.normalize("NFKC", repair_arabic_mojibake(str(text)))
    norm = re.sub(r"[\u064B-\u065F\u0670\u0640]", "", norm)
    norm = re.sub(r"[\u0623\u0625\u0622\u0671]", "\u0627", norm)
    norm = norm.replace("\u0649", "\u064A")
    return sum(1 for hint in _ARABIC_LABEL_HINTS if hint in norm)


def _extract_visual_date_near_label(tokens: list, label_kind: str) -> tuple[str | None, float]:
    if label_kind == "birth":
        label_re = re.compile(r"\bBIRTH\b|\bBINTH\b|\bBJRTH\b|الميلاد|المبلاد", re.I)
    elif label_kind == "expiry":
        label_re = re.compile(r"\bEXPIRY\b|\bEXPIRY\b|\bEXPIY\b|الانتهاء|الإنتهاء", re.I)
    else:
        label_re = re.compile(r"\bISSUE\b|\bISSUED\b|\bISSUC\b|الإصدار|اصدار|الزمدار", re.I)

    for tok in tokens:
        if not label_re.search(tok.get("text", "")):
            continue
        anchor_cy = tok["cy"]
        anchor_y2 = tok.get("y2", tok["cy"])
        nearby = [
            t for t in tokens
            if abs(t["cy"] - anchor_cy) <= 45
            or (0 < t["y"] - anchor_y2 <= 90)
        ]
        nearby.sort(key=lambda t: (abs(t["cy"] - anchor_cy), t["x"]))
        for candidate in nearby:
            d = _parse_passport_date(candidate.get("text", ""))
            if d:
                return d, round(candidate.get("conf", 0.0), 3)
        merged = " ".join(t.get("text", "") for t in nearby)
        d = _parse_passport_date(merged)
        if d:
            confs = [t.get("conf", 0.0) for t in nearby if t.get("text")]
            return d, round(sum(confs) / len(confs), 3) if confs else 0.5
    return None, 0.0


def _calculate_accuracy_passport(passport: dict, checks: dict) -> float:
    """
    Content-validator-based accuracy for passport (mirrors _calculate_accuracy
    in national_id_parser).

    Collects per-field confidence from content validators (NOT raw OCR scores),
    then averages them.  ICAO check digits are the primary trust signal.

    Core fields (always expected from MRZ):
        passport_no, name_en, date_of_birth, expiry_date, gender, nationality
    Bonus fields (visual-only, frequently absent):
        name_ar, national_no, issue_date
    """
    # ── Core field confs (content-validated) ──────────────────────────────────
    core = {
        "passport_no":   passport.get("_conf_passport_no",   0.0),
        "name_en":       passport.get("_conf_name_en",       0.0),
        "date_of_birth": passport.get("_conf_date_of_birth", 0.0),
        "expiry_date":   passport.get("_conf_expiry_date",   0.0),
        "gender":        passport.get("_conf_gender",        0.0),
        "nationality":   passport.get("_conf_nationality",   0.0),
    }
    # ── Bonus field confs ─────────────────────────────────────────────────────
    bonus = {
        "name_ar":    passport.get("_conf_name_ar",    0.0),
        "national_no":passport.get("_conf_national_no",0.0),
        "issue_date": passport.get("_conf_issue_date", 0.0),
    }

    core_avg  = sum(core.values())  / len(core)
    # Bonus: only count fields that are actually present (don't penalise absence)
    bonus_present = [v for v in bonus.values() if v > 0.0]
    bonus_avg = sum(bonus_present) / len(bonus_present) if bonus_present else 0.0

    # MRZ check ratio (same as before)
    all_checks = list(checks.values())
    check_avg  = sum(1 for v in all_checks if v) / len(all_checks) if all_checks else 0.0

    # Weights mirror national_id's spirit: core content quality is primary
    score = (0.65 * core_avg + 0.15 * bonus_avg + 0.20 * check_avg) * 100
    return round(max(0.0, min(100.0, score)), 2)


def _build_passport_fields(
    line1: str,
    line2: str,
    text_tokens: list,
    latin_tokens: list,
    mrz_conf: float,
    checks: dict,
    variants: dict | None = None,
) -> tuple[dict, dict]:
    """
    Build passport field dict and per-field confidence scores.

    Confidence is now derived from CONTENT VALIDATORS (same approach as
    national_id_parser), not raw EasyOCR OCR scores:
      - ICAO check digit passes    → conf = 1.0  (cryptographic proof)
      - Format + content quality   → conf from _passport_no_conf / _dob_conf etc.
      - Visual-OCR fields          → conf from _name_ar_conf / token quality
    """
    # ── MRZ-derived fields ────────────────────────────────────────────────────
    doc_type_raw  = line1[0:2].rstrip("<").strip()
    issuing_state = _normalize_country_code(line1[2:5].replace("<", "").strip())
    mrz_surname, mrz_given_names = split_mrz_name(line1[5:44])

    passport_no  = line2[0:9].rstrip("<")
    nat_code     = _normalize_country_code(line2[10:13].replace("<", "").strip())
    if nat_code not in _NAT and issuing_state in _NAT:
        nat_code = issuing_state
    dob_raw      = line2[13:19]
    sex_raw      = line2[20]
    expiry_raw   = line2[21:27]
    national_no  = line2[28:42].replace("<", "").strip() or None

    mrz_full_name_en = f"{mrz_given_names} {mrz_surname}".strip() if mrz_given_names else mrz_surname
    visual_tokens = latin_tokens or text_tokens
    date_tokens = _dedupe_visual_tokens((latin_tokens or []) + (text_tokens or []))

    visual_name_en, visual_name_conf, visual_name_y = _extract_visual_english_name_passport(visual_tokens)
    full_name_en = _space_visual_name_with_mrz(visual_name_en, mrz_full_name_en)
    if full_name_en:
        surname, given_names = _split_name_parts_from_full(full_name_en, mrz_surname)
    else:
        surname, given_names = mrz_surname, mrz_given_names

    nationality  = _NAT.get(nat_code, nat_code)
    doc_type     = _DOC_TYPE.get(doc_type_raw, "Passport")
    gender       = _SEX.get(sex_raw, "Unspecified")
    if gender == "Male":
        gender = "Male/ذكر"
    elif gender == "Female":
        gender = "Female/انثى"
    dob    = mrz_date(dob_raw,    kind="birth")
    expiry = mrz_date(expiry_raw, kind="expiry")

    visual_dob, visual_dob_conf = _extract_visual_date_near_label(date_tokens, "birth")
    visual_expiry, visual_expiry_conf = _extract_visual_date_near_label(date_tokens, "expiry")
    if visual_dob and (not checks.get("dob", False) or _dob_conf(dob, False) < MIN_FIELD_CONF):
        dob = visual_dob
    if visual_expiry and (not checks.get("expiry", False) or _expiry_conf(expiry, False) < MIN_FIELD_CONF):
        expiry = visual_expiry

    # ── Content-validator confidence (same philosophy as national_id) ─────────
    conf_passport_no = _passport_no_conf(passport_no, checks.get("passport_no", False))
    conf_dob         = max(_dob_conf(dob, checks.get("dob", False)), visual_dob_conf if dob == visual_dob else 0.0)
    conf_expiry      = max(_expiry_conf(expiry, checks.get("expiry", False)), visual_expiry_conf if expiry == visual_expiry else 0.0)
    conf_name_en     = max(_name_en_conf(full_name_en), min(0.95, visual_name_conf + 0.15) if visual_name_en else 0.0)
    conf_nationality = _nationality_conf(nat_code, nationality)
    conf_gender      = _gender_conf(gender)
    conf_national_no = _passport_no_conf(national_no, False) if national_no else 0.0

    # ── Visual tokens: Arabic name + issue date ───────────────────────────────
    visual_name_ar, _name_ar_raw_conf  = _extract_arabic_name_passport(text_tokens, visual_name_y)
    name_ar = _clean_visual_arabic_name(visual_name_ar)
    focused_name_ar, _focused_name_ar_conf = _extract_arabic_name_from_name_band(variants, visual_name_y)
    focused_name_ar = _clean_visual_arabic_name(focused_name_ar)
    if (
        focused_name_ar
        and not _arabic_name_too_short_for_latin(focused_name_ar, full_name_en)
        and (
            not name_ar
            or _arabic_name_too_short_for_latin(name_ar, full_name_en)
            or len(focused_name_ar.split()) > len(name_ar.split())
        )
    ):
        name_ar = focused_name_ar
    if _arabic_name_too_short_for_latin(name_ar, full_name_en):
        name_ar = None
    if name_ar:
        print(f"  [passport] Final Arabic name: {name_ar}")
    issue_date, _issue_raw_conf = _extract_issue_date_passport(date_tokens)

    # Content validators override raw token conf for visual fields too
    conf_name_ar   = _name_ar_conf(name_ar)
    conf_issue     = _expiry_conf(issue_date, False) if issue_date else 0.0

    # Gate visual fields only (MRZ fields always returned)
    def _gate_visual(value, conf):
        return value if conf >= MIN_FIELD_CONF else None

    passport = {
        "document_type":    doc_type,
        "issuing_state":    issuing_state,
        "passport_no":      passport_no,
        "name_en":          full_name_en,
        "name_ar":          _gate_visual(name_ar,    conf_name_ar),
        "surname":          surname,
        "given_names":      given_names,
        "nationality":      nationality,
        "nationality_code": nat_code,
        "national_no":      national_no,
        "gender":           gender,
        "date_of_birth":    dob,
        "issue_date":       _gate_visual(issue_date, conf_issue),
        "expiry_date":      expiry,
        # Internal conf keys used by _calculate_accuracy_passport
        "_conf_passport_no":   conf_passport_no,
        "_conf_name_en":       conf_name_en,
        "_conf_name_ar":       conf_name_ar,
        "_conf_date_of_birth": conf_dob,
        "_conf_expiry_date":   conf_expiry,
        "_conf_gender":        conf_gender,
        "_conf_nationality":   conf_nationality,
        "_conf_national_no":   conf_national_no,
        "_conf_issue_date":    conf_issue,
    }

    # Public conf map (no underscore prefix — used by _public_response)
    field_confs = {
        "passport_no":   round(conf_passport_no, 3),
        "name_en":       round(conf_name_en,     3),
        "name_ar":       round(conf_name_ar,     3),
        "national_no":   round(conf_national_no, 3),
        "nationality":   round(conf_nationality, 3),
        "gender":        round(conf_gender,      3),
        "date_of_birth": round(conf_dob,         3),
        "issue_date":    round(conf_issue,        3),
        "expiry_date":   round(conf_expiry,       3),
    }

    return passport, field_confs


# ══════════════════════════════════════════════════════════════════════════════
#  VISUAL FIELD EXTRACTORS
# ══════════════════════════════════════════════════════════════════════════════

def _extract_arabic_name_passport(tokens: list, latin_name_y: float | None = None) -> tuple[str | None, float]:
    """
    Extract Arabic name from visual tokens.
    Groups tokens on the same row and picks the row with the most Arabic words.
    Returns (name, confidence) or (None, 0.0).
    """
    # Group tokens by approximate row (bucket by cy)
    row_groups: dict[int, list] = {}
    for tok in tokens:
        if not ARABIC_RE.search(tok["text"]):
            continue
        bucket = int(round(tok["cy"] / 30)) * 30
        row_groups.setdefault(bucket, []).append(tok)

    candidates: list[tuple[float, int, str, float]] = []
    seen_candidates: set[str] = set()

    def _add_arabic_candidate(text: str, toks: list[dict], row_y: float) -> None:
        cleaned = _clean_visual_arabic_name(text)
        if not cleaned:
            return
        ar_words = cleaned.split()
        if len(ar_words) < 2 or len(ar_words) > 7:
            return
        label_hints = _arabic_label_hint_count(cleaned)
        if label_hints >= 2 or (label_hints and len(ar_words) > 4):
            return
        if cleaned in seen_candidates:
            return
        seen_candidates.add(cleaned)

        avg_c = sum(t.get("conf", 0.0) for t in toks) / max(len(toks), 1)
        proximity = 1.0
        if latin_name_y is not None:
            proximity = max(0.0, 1.0 - abs(row_y - latin_name_y) / 85.0)
        word_score = min(len(ar_words), 5) * 0.08 - max(0, len(ar_words) - 5) * 0.18
        score = avg_c * 0.75 + proximity * 0.55 + word_score - label_hints * 0.35
        candidates.append((score, len(ar_words), cleaned, avg_c))


    for bucket, row_toks in row_groups.items():
        row_toks_sorted = sorted(row_toks, key=lambda t: -t["x"])  # RTL: right first
        row_y = sum(t.get("cy", 0.0) for t in row_toks) / len(row_toks)
        if latin_name_y is not None and abs(row_y - latin_name_y) > 85:
            continue
        merged_text = " ".join(t["text"] for t in row_toks_sorted).strip()
        _add_arabic_candidate(merged_text, row_toks_sorted, row_y)
        for tok in row_toks_sorted:
            _add_arabic_candidate(tok.get("text", ""), [tok], tok.get("cy", row_y))

    if not candidates:
        return None, 0.0

    # Pick best: confidence/proximity first; do not let long label rows win.
    candidates.sort(key=lambda x: (x[0], min(x[1], 5)), reverse=True)
    best_score, _, best_text, avg_conf = candidates[0]
    return best_text.strip(), round(min(1.0, max(avg_conf, best_score)), 3)


def _extract_issue_date_passport(tokens: list) -> tuple[str | None, float]:
    """
    Extract issue date from visual tokens.
    Strategy 1: look for a token with an issue label then read nearby date.
    Strategy 2: fallback — scan all tokens for a date in the issue year range.
    Returns (date_str DD/MM/YYYY, confidence) or (None, 0.0).
    """
    # Strategy 1: label-anchored search
    for tok in tokens:
        text = tok["text"]
        is_issue_label = (
            re.search(r"\bISSUE\b|\bISSUED\b|\bDATE\s*OF\s*ISSUE\b", text, re.I)
            or "اصدار" in text
            or "إصدار" in text
            or "الإصدار" in text
        )
        if not is_issue_label:
            continue

        # Search same row and just below for a date
        anchor_cy = tok["cy"]
        anchor_y2 = tok.get("y2", tok["cy"])
        nearby = [
            t for t in tokens
            if (abs(t["cy"] - anchor_cy) <= 40)
            or (0 < t["y"] - anchor_y2 <= 80)
        ]
        nearby.sort(key=lambda t: abs(t["cy"] - anchor_cy))
        for candidate in nearby:
            d = _parse_passport_date(candidate["text"])
            if d:
                return d, round(candidate["conf"], 3)

    # Strategy 2: collect all dates in a plausible issue year range
    today         = _date.today()
    date_candidates: list[tuple[int, str, float]] = []

    for tok in tokens:
        d = _parse_passport_date(tok["text"])
        if not d:
            continue
        try:
            parts = d.split("/")
            y = int(parts[2])
            # Issue dates are recent (2000–today), not future expiry dates
            if 2000 <= y <= today.year:
                date_candidates.append((y, d, tok["conf"]))
        except (IndexError, ValueError):
            pass

    if date_candidates:
        # Most recent non-expiry date is most likely the issue date
        date_candidates.sort(key=lambda x: x[0], reverse=True)
        _, best_date, best_conf = date_candidates[0]
        return best_date, round(best_conf, 3)

    return None, 0.0


def _parse_passport_date(text: str) -> str | None:
    """Parse DD/MM/YYYY or DD-MM-YYYY or DD.MM.YYYY from a token string."""
    text = strip_arabic(text).upper().strip()
    m = re.search(r"(\d{1,2})[/.\-](\d{1,2})[/.\-](\d{4})", text)
    if m:
        d, mo, y = int(m.group(1)), int(m.group(2)), int(m.group(3))
        if 1 <= d <= 31 and 1 <= mo <= 12 and 1900 <= y <= 2099:
            return f"{d:02d}/{mo:02d}/{y:04d}"

    m = re.search(r"(\d{1,2})\s*([A-Z0-9]{3,9})[A-Z0-9/.\-]*\s*(\d{4})", text)
    if m:
        d = int(m.group(1))
        month_key = m.group(2).replace("0", "O")
        y = int(m.group(3))
        mo = _MONTHS.get(month_key)
        if mo and 1 <= d <= 31 and 1900 <= y <= 2099:
            return f"{d:02d}/{mo:02d}/{y:04d}"

    m = re.search(r"(\d{4})\D{0,12}(\d{1,2})", text)
    if m:
        y = int(m.group(1))
        d = int(m.group(2))
        month_hint = next((num for key, num in _MONTHS.items() if key in text), None)
        if month_hint and 1 <= d <= 31 and 1900 <= y <= 2099:
            return f"{d:02d}/{month_hint:02d}/{y:04d}"
    return None


# ══════════════════════════════════════════════════════════════════════════════
#  PASSPORT NUMBER CORRECTION  (check-digit guided, single-char errors only)
# ══════════════════════════════════════════════════════════════════════════════

# Common OCR-B confusions: what normalize_mrz_l2_td3 may have wrongly changed
_CORRECTIONS: dict[str, list[str]] = {
    "2": ["Z"],
    "5": ["S"],
    "8": ["B"],
    "6": ["G"],
    "1": ["I", "L"],
    "R": ["K"],
    "K": ["R"],
    "U": ["V"],
    "V": ["U"],
    "N": ["M"],
    "M": ["N"],
    "D": ["O"],
    "I": ["1"],
    "Z": ["2"],
    "S": ["5"],
    "B": ["8"],
    "G": ["6"],
}


def _correct_passport_no(line2: str) -> str:
    """
    If the passport_no check digit (line2[9]) fails, try single-character
    substitutions from _CORRECTIONS at each position of line2[0:9].
    Only accepts a correction when the check digit passes.
    """
    doc_no = line2[0:9]
    chk    = line2[9]

    if _chk_ok(doc_no, chk):
        return line2

    chars = list(doc_no)
    for pos, ch in enumerate(chars):
        for alt in _CORRECTIONS.get(ch, []):
            candidate = "".join(chars[:pos] + [alt] + chars[pos + 1:])
            if _chk_ok(candidate, chk):
                print(f"  [MRZ] passport_no fix pos {pos}: {ch!r}→{alt!r}  "
                      f"({doc_no} → {candidate})")
                return candidate + line2[9:]

    print(f"  [MRZ] passport_no: no single-char fix found for {doc_no!r}")
    return line2


def _correct_jordan_passport_prefix(line1: str, line2: str) -> str:
    issuing_state = _normalize_country_code(line1[2:5].replace("<", "").strip())
    if issuing_state != "JOR" or len(line2) < 10:
        return line2

    doc_no = line2[0:9]
    if not doc_no or not doc_no[0].isdigit():
        return line2

    prefix_candidates = {
        "0": ["O"],
        "8": ["S", "B"],
        "1": ["I", "L"],
        "5": ["S"],
    }
    for alt in prefix_candidates.get(doc_no[0], []):
        candidate = alt + doc_no[1:]
        if _chk_ok(candidate, line2[9]):
            print(f"  [MRZ] Jordan passport prefix fix: {doc_no!r} → {candidate!r}")
            return candidate + line2[9:]
    return line2


# ══════════════════════════════════════════════════════════════════════════════
#  ICAO CHECK DIGIT  (self-contained)
# ══════════════════════════════════════════════════════════════════════════════

_ICAO_V: dict[str, int] = {c: i + 10 for i, c in enumerate("ABCDEFGHIJKLMNOPQRSTUVWXYZ")}
_ICAO_V.update({str(i): i for i in range(10)})
_ICAO_V["<"] = 0
_ICAO_W = [7, 3, 1]


def _icao_check(field: str) -> int:
    return sum(_ICAO_V.get(c, 0) * _ICAO_W[i % 3] for i, c in enumerate(field)) % 10


def _chk_ok(field: str, expected: str) -> bool:
    expected = expected.replace("I", "1")
    try:
        return _icao_check(field) == int(expected)
    except (ValueError, TypeError):
        return False


def _validate_td3_local(l2: str) -> dict:
    try:
        return {
            "passport_no": _chk_ok(l2[0:9], l2[9]),
            "dob":         _chk_ok(l2[13:19], l2[19]),
            "expiry":      _chk_ok(l2[21:27], l2[27]),
            "composite":   _chk_ok(l2[0:10] + l2[13:20] + l2[21:43], l2[43]),
        }
    except (IndexError, TypeError):
        return {"passport_no": False, "dob": False, "expiry": False, "composite": False}


# ── Helpers ───────────────────────────────────────────────────────────────────

def _err(message: str) -> dict:
    return {
        "passport":    {},
        "field_confs": {},
        "mrz":         {"mrz_valid": False, "error": message},
        "quality":     {"usable": False},
    }


# Notebook-based hybrid passport engine
# -------------------------------------
# Keep this file's historical helper functions importable, but route the public
# entry point through the shared hybrid engine built from passport_ocr_pipeline.
# That gives callers the notebook execution flow while preserving old imports
# such as _space_visual_name_with_mrz used by ocr_service.py.
try:
    from passport_hybrid_engine import extract_passport as _notebook_extract_passport

    def extract_passport(image_path: str, use_ollama: bool = True, debug: bool = False) -> dict:
        return _notebook_extract_passport(
            image_path=image_path,
            use_ollama=use_ollama,
            debug=debug,
        )
except Exception as _hybrid_import_error:
    _HYBRID_IMPORT_ERROR = str(_hybrid_import_error)
    _ORIGINAL_EXTRACT_PASSPORT = extract_passport

    def extract_passport(image_path: str, use_ollama: bool = True, debug: bool = False) -> dict:
        result = _ORIGINAL_EXTRACT_PASSPORT(image_path)
        result.setdefault("warning", f"Notebook hybrid engine unavailable: {_HYBRID_IMPORT_ERROR}")
        return result
