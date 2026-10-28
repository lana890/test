# -*- coding: utf-8 -*-

import re
import os
import json
from datetime import date
from ocr_engine import (
    load_and_resize, build_variants, build_arabic_ocr_variants,
    compute_quality, normalize_arabic_text, normalize_field,
    select_source,
    ocr_to_tokens, READER_MIX, READER_EN,
    extract_mrz_from_tokens, preprocess_mrz_strip, ocr_mrz_strip_fallback,
    normalize_mrz_l1_td1, normalize_mrz_l2_td1, normalize_mrz_l3_td1,
    icao_check, validate_td1,
    split_mrz_name, build_full_name, mrz_date,
    strip_arabic, is_label_token, same_row,
    tokens_right_of, tokens_below,
    MRZ_ALLOWLIST, SNAP, ARABIC_RE,
) 


# ══════════════════════════════════════════════════════════════════════════════
#  MRZ PARSING  — TD1 (3 × 30 chars)
# ══════════════════════════════════════════════════════════════════════════════

def _trace(label: str, payload) -> None:
    try:
        text = json.dumps(payload, ensure_ascii=False, sort_keys=True, default=str)
    except Exception:
        text = str(payload)
    print(f"[OCR_TRACE] {label}: {text}", flush=True)


def _token_preview(tokens: list[dict], limit: int = 120) -> list[dict]:
    preview = []
    for token in (tokens or [])[:limit]:
        preview.append({
            "text": token.get("text"),
            "conf": round(float(token.get("conf", 0.0)), 3),
            "x": int(token.get("x", 0) or 0),
            "y": int(token.get("y", 0) or 0),
        })
    return preview


def parse_mrz_td1(raw_lines: list[dict]) -> dict:
    return parse_jordan_id_mrz(raw_lines)


def build_td1_l1_from_fields(front: dict, back: dict) -> dict | None:
    doc_no = re.sub(r"[^A-Z0-9<]", "", (back.get("document_number") or "").upper())
    national_no = re.sub(r"\D", "", front.get("national_number") or "")
    if not doc_no or not national_no:
        return None

    doc_field = doc_no.ljust(9, "<")[:9]
    check = str(icao_check(doc_field))
    optional = national_no.ljust(15, "<")[:15]
    return {"text": f"IDJOR{doc_field}{check}{optional}", "y": -1, "conf": 1.0}


def repair_td1_mrz_lines(raw_lines: list[dict], front: dict, back: dict) -> list[dict]:
    if len(raw_lines) >= 3:
        return raw_lines[:3]

    synthetic_l1 = build_td1_l1_from_fields(front, back)
    if synthetic_l1 is None:
        return raw_lines

    cleaned = []
    for r in raw_lines:
        text = re.sub(r"[^A-Z0-9<]", "", r.get("text", "").upper())
        if re.match(r"^I?D[A-Z0-9<]{3}", text):
            continue
        cleaned.append(r)
    return [synthetic_l1] + cleaned[:2]


def _mrz_clean(text: str) -> str:
    text = text.upper()
    text = text.replace(" ", "").replace("|", "<").replace("/", "<")
    text = text.replace("[", "<").replace("]", "<").replace("{", "<").replace("}", "<")
    return re.sub(r"[^A-Z0-9<]", "<", text)


def _mrz_digits(text: str) -> str:
    return text.translate(str.maketrans("OoQqDdIiLlSsGgBbZz", "000000111155668822"))


def _valid_mrz_date(raw: str, kind: str = "birth") -> str | None:
    raw = _mrz_digits(raw)
    if len(raw) != 6 or not raw.isdigit():
        return None
    yy, mm, dd = int(raw[:2]), int(raw[2:4]), int(raw[4:6])
    if not (1 <= mm <= 12 and 1 <= dd <= 31):
        return None
    current_year = date.today().year
    if kind == "expiry":
        # Jordanian ID expiry dates are modern/future dates. YY=32 is 2032,
        # never 1932; expired modern cards still stay in the 2000s.
        yyyy = 2000 + yy
    else:
        yyyy = 2000 + yy if 2000 + yy <= current_year else 1900 + yy
    return f"{dd:02d}/{mm:02d}/{yyyy}"


def _looks_like_td1_l2(text: str) -> bool:
    compact = _mrz_digits(_mrz_clean(text))
    return bool(re.search(r"\d{6}\d?[MF]\d{6}\d?", compact))


def _looks_like_td1_l3(text: str) -> bool:
    compact = _mrz_clean(text)
    letters = sum(1 for c in compact if c.isalpha())
    return "<<" in compact and letters >= 8 and not compact.startswith("ID")


def _choose_jordan_mrz_lines(raw_lines: list[dict], front: dict | None, back: dict | None) -> tuple[str, str, str]:
    cleaned = [_mrz_clean(r.get("text", "")) for r in raw_lines if r.get("text")]
    line1 = next((x for x in cleaned if re.match(r"^I?D[A-Z0-9<]{3}", x)), "")
    line2 = next((x for x in cleaned if _looks_like_td1_l2(x)), "")
    line3 = next((x for x in cleaned if _looks_like_td1_l3(x)), "")

    if not line1 and front is not None and back is not None:
        synthetic = build_td1_l1_from_fields(front, back)
        line1 = _mrz_clean(synthetic["text"]) if synthetic else ""

    remaining = [x for x in cleaned if x not in (line1, line2, line3)]
    if not line2 and remaining:
        line2 = remaining[0]
    if not line3 and len(remaining) > 1:
        line3 = remaining[-1]

    return line1, line2, line3


def _parse_jordan_mrz_l2(line2: str) -> dict:
    cleaned = _mrz_clean(line2)
    positioned = normalize_mrz_l2_td1(cleaned)
    if (
        positioned[:6].isdigit()
        and positioned[7] in ("M", "F", "<")
        and positioned[8:14].isdigit()
    ):
        return {
            "dob": positioned[0:6],
            "dob_check": positioned[6] if positioned[6].isdigit() else "",
            "sex": positioned[7],
            "expiry": positioned[8:14],
            "expiry_check": positioned[14] if positioned[14].isdigit() else "",
            "nationality": "JOR",
        }

    compact = _mrz_digits(cleaned)
    match = re.search(
        r"(?P<dob>\d{6})(?P<dob_check>[0-9<])?(?P<sex>[MF<])"
        r"(?P<exp>\d{6})(?P<exp_check>[0-9<])?",
        compact,
    )
    if not match:
        return {}
    return {
        "dob": match.group("dob"),
        "dob_check": "" if match.group("dob_check") == "<" else (match.group("dob_check") or ""),
        "sex": match.group("sex"),
        "expiry": match.group("exp"),
        "expiry_check": "" if match.group("exp_check") == "<" else (match.group("exp_check") or ""),
        "nationality": "JOR",
    }


def _name_from_mrz_l3(line3: str) -> tuple[str, str, str]:
    normalized = normalize_mrz_l3_td1(line3)
    surname, given = split_mrz_name(normalized)
    surname = _clean_latin_name(surname)
    given = _clean_latin_name(given)
    full_name = _clean_latin_name(build_full_name(surname, given))
    return normalized, surname, given, full_name


def parse_jordan_id_mrz(raw_lines: list[dict], front: dict | None = None, back: dict | None = None) -> dict:
    texts = [_mrz_clean(r.get("text", "")) for r in raw_lines if r.get("text")]
    result = {
        "mrz_raw": texts,
        "mrz_lines_detected": len(texts),
        "mrz_doc_type": "ID",
        "mrz_country": "JOR",
        "mrz_nationality": "JOR",
    }

    line1, line2, line3 = _choose_jordan_mrz_lines(raw_lines, front, back)
    result["mrz_l1"] = line1 or None
    result["mrz_l2"] = line2 or None
    result["mrz_l3"] = line3 or None

    if line1:
        normalized_l1 = normalize_mrz_l1_td1(line1)
        result["mrz_l1"] = normalized_l1
        result["mrz_doc_number"] = normalized_l1[5:14].replace("<", "") or None
        optional = normalized_l1[15:30].replace("<", "")
        nid_match = re.search(r"\d{8,12}", optional) or re.search(r"\d{8,12}", normalized_l1)
        result["mrz_national_id"] = nid_match.group(0) if nid_match else None

    l2_fields = _parse_jordan_mrz_l2(line2)
    if l2_fields:
        result["mrz_dob"] = l2_fields["dob"]
        result["mrz_check_dob"] = l2_fields["dob_check"]
        result["mrz_sex"] = l2_fields["sex"]
        result["mrz_expiry"] = l2_fields["expiry"]
        result["mrz_check_expiry"] = l2_fields["expiry_check"]
        result["mrz_nationality"] = "JOR"
        result["mrz_dob_parsed"] = _valid_mrz_date(l2_fields["dob"], "birth")
        result["mrz_expiry_parsed"] = _valid_mrz_date(l2_fields["expiry"], "expiry")
    else:
        result["mrz_dob_parsed"] = None
        result["mrz_expiry_parsed"] = None

    if line3:
        normalized_l3, surname, given, full_name = _name_from_mrz_l3(line3)
        full_name = _complete_english_name(full_name, front.get("full_name_en") if front else None) or full_name
        result["mrz_l3"] = normalized_l3
        result["mrz_surname"] = surname
        result["mrz_given_names"] = given
        result["mrz_name_full"] = full_name

    checks = {
        "doc_number": False,
        "dob": False,
        "expiry": False,
        "composite": False,
    }
    try:
        l1 = result.get("mrz_l1") or ""
        if len(l1) >= 15 and l1[14].isdigit():
            checks["doc_number"] = icao_check(l1[5:14]) == int(l1[14])
        if l2_fields.get("dob_check", "").isdigit():
            checks["dob"] = icao_check(l2_fields["dob"]) == int(l2_fields["dob_check"])
        if l2_fields.get("expiry_check", "").isdigit():
            checks["expiry"] = icao_check(l2_fields["expiry"]) == int(l2_fields["expiry_check"])
    except Exception:
        pass
    result["mrz_checks"] = checks
    result["mrz_valid"] = bool(
        l2_fields
        and result.get("mrz_dob_parsed")
        and result.get("mrz_expiry_parsed")
        and (not l2_fields.get("dob_check") or checks["dob"])
        and (not l2_fields.get("expiry_check") or checks["expiry"])
    )
    return result


# ══════════════════════════════════════════════════════════════════════════════
#  FRONT-SIDE FIELD PARSER
# ══════════════════════════════════════════════════════════════════════════════

_SKIP_WORDS = {
    "KINGDOM", "MINISTRY", "MINISURY", "CIVIL", "PASSPORT", "STATUS",
    "CARD", "HASHEMITE", "INTERIOR", "THE", "OF", "JORDAN", "DEPT",
    "DEPARTMENT", "ID",
}


def _token_text_all(tokens: list) -> str:
    return " ".join(t["text"] for t in tokens)


def _token_text_by_lang(tokens: list, lang: str) -> str:
    parts = []
    for t in tokens:
        text = t["text"].strip()
        has_ar = bool(ARABIC_RE.search(text))
        has_en = bool(re.search(r"[A-Za-z]", text))
        if lang == "ar" and has_ar:
            parts.append(text)
        elif lang == "en" and has_en:
            parts.append(strip_arabic(text))
    return re.sub(r"\s+", " ", " ".join(p for p in parts if p)).strip()


def _token_text_list(tokens: list) -> list[str]:
    return [t["text"] for t in tokens if t.get("text")]


_DIGIT_TRANS = str.maketrans("٠١٢٣٤٥٦٧٨٩۰۱۲۳۴۵۶۷۸۹", "01234567890123456789")
_AR_DIACRITICS_RE = re.compile(r"[\u064B-\u065F\u0670\u0640]")
_AR_LABEL_RE = re.compile(
    r"(?:الإسم|الاسم|لإسم|اسم|الرقم\s*الوطني|الوطني|الوطنى|"
    r"الجنس|تاريخ\s*الولادة|مكان\s*الولادة|الولادة|"
    r"اسم\s*الأم|اسم\s*الام|الأم|الام|"
    r"مكان\s*الإصدار|مكان\s*الاصدار|الإصدار|الاصدار|"
    r"مكان\s*الإقامة|مكان\s*الاقامة|الإقامة|الاقامة|"
    r"فصيلة\s*الدم|الدم|وزارة|دائرة|بطاقة|شخصية|المملكة|الهاشمية)"
)
_AR_STOP_WORDS = {
    "الإسم", "الاسم", "لإسم", "اسم", "الرقم", "الوطني", "الوطنى",
    "الجنس", "تاريخ", "مكان", "الولادة", "الأم", "الام", "الإصدار",
    "الاصدار", "الإقامة", "الاقامة", "فصيلة", "الدم", "وزارة", "دائرة",
    "بطاقة", "شخصية", "المملكة", "الهاشمية",
}
_LATIN_LABEL_WORDS = {
    "NAME", "ID", "NO", "DATE", "BIRTH", "PLACE", "SEX", "GENDER",
    "NATIONAL", "NUMBER", "EXPIRY", "EXP", "ISSUE", "REGISTRY", "BLOOD",
    "TYPE", "MOTHER", "CARD",
}
_JORDAN_PLACE_WORDS = {
    "عمان", "اربد", "إربد", "الزرقاء", "العقبة", "الكرك", "السلط",
    "مادبا", "جرش", "عجلون", "المفرق", "الطفيلة", "معان", "البلقاء",
    "الكويت",
}
def _normalize_digits(text: str | None) -> str:
    return (text or "").translate(_DIGIT_TRANS)


def _compact_spaces(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()


def _arabic_key(text: str | None) -> str:
    text = normalize_arabic_text(_normalize_digits(text))
    text = _AR_DIACRITICS_RE.sub("", text)
    return text.replace("أ", "ا").replace("إ", "ا").replace("آ", "ا")


def _strip_arabic_labels(text: str | None) -> str:
    text = _arabic_key(text)
    text = re.sub(r"\bName\b\s*:?", " ", text, flags=re.I)
    text = _AR_LABEL_RE.sub(" ", text)
    return text


def _clean_arabic_field(value: str | None, keep_slash: bool = False, min_words: int = 1) -> str | None:
    if not value:
        return None
    value = _strip_arabic_labels(value)
    value = re.sub(r"[A-Za-z0-9_]+", " ", value)
    allowed = r"^\u0600-\u06FF\s/" if keep_slash else r"^\u0600-\u06FF\s"
    value = re.sub(f"[{allowed}]", " ", value)
    if keep_slash:
        value = re.sub(r"\s*/\s*", " / ", value)
    words = []
    for word in re.split(r"\s+", value):
        if keep_slash and word in {"/", "|"}:
            if words and words[-1] != "/":
                words.append("/")
            continue
        strip_chars = " :؛،,.-_|\\"
        if not keep_slash:
            strip_chars += "/"
        word = word.strip(strip_chars)
        if not word or word in _AR_STOP_WORDS:
            continue
        if len(word) == 1:
            continue
        if word == "أبو":
            word = "ابو"
        words.append(word)
    deduped = []
    for word in words:
        if not deduped or deduped[-1] != word:
            deduped.append(word)
    if len(deduped) < min_words:
        return None
    return _compact_spaces(" ".join(deduped)) or None


def _clean_arabic_name(value: str | None) -> str | None:
    cleaned = _clean_arabic_field(value, min_words=2)
    return cleaned


def _fix_name_ocr_token(token: str) -> list[str]:
    token = token.strip("'")
    if not token:
        return []
    if any(ch.isdigit() for ch in token) and re.search(r"[A-Z]", token):
        digit_count = sum(ch.isdigit() for ch in token)
        if digit_count <= 2:
            token = token.translate(str.maketrans({"0": "O", "1": "I", "5": "S", "8": "B"}))
    token = re.sub(r"[^A-Z']", "", token)
    if not token or token in _SKIP_WORDS or token in _LATIN_LABEL_WORDS:
        return []
    return [token]


def sanitize_id_data(data: dict) -> dict:
    for name_key in ("full_name_ar", "full_name_en"):
        raw = normalize_field(data.get(name_key))
        if name_key == "full_name_ar":
            data[name_key] = _clean_arabic_name(raw)
        else:
            data[name_key] = _clean_latin_name(raw) or None

    for key in ("mother_name", "mothers_name_ar", "place_of_birth", "issue_place", "residence_place"):
        if data.get(key):
            data[key] = _clean_arabic_field(normalize_field(data[key]), keep_slash=(key == "residence_place"))
    if data.get("issue_place"):
        data["issue_place"] = _clean_jordan_place(data["issue_place"], issue_place=True)
    if data.get("residence_place"):
        data["residence_place"] = _clean_jordan_place(data["residence_place"])

    for key in list(data.keys()):
        if key == "conf" or key.startswith("_conf_"):
            data.pop(key, None)
        elif isinstance(data.get(key), dict) and "value" in data[key]:
            data[key] = normalize_field(data[key])

    return data


def _clean_latin_name(value: str | None) -> str:
    if not value:
        return ""
    value = strip_arabic(_normalize_digits(value)).upper()
    value = re.sub(r"\bNAME\b\s*:?", " ", value)
    value = value.replace("<<", " ")
    value = re.sub(r"[<\-_/|:]+", " ", value)
    value = re.sub(r"[^A-Z0-9\s']", " ", value)
    raw_tokens = [t for t in re.split(r"\s+", value) if t]
    fixed: list[str] = []
    for token in raw_tokens:
        fixed.extend(_fix_name_ocr_token(token))
    deduped = []
    for token in fixed:
        if not deduped or deduped[-1] != token:
            deduped.append(token)
    return _compact_spaces(" ".join(deduped))


def _latin_name_score(value: str | None) -> int:
    cleaned = _clean_latin_name(value)
    if not cleaned:
        return 0
    tokens = cleaned.split()
    return sum(len(t) for t in tokens) + 4 * len(tokens)


def _merge_mrz_and_visual_name(mrz_name: str | None, viz_name: str | None) -> str | None:
    mrz_tokens = _clean_latin_name(mrz_name).split()
    viz_tokens = _clean_latin_name(viz_name).split()
    if not mrz_tokens:
        return " ".join(viz_tokens) or None
    if not viz_tokens:
        return " ".join(mrz_tokens)

    merged = []
    for token in mrz_tokens:
        replacement = next(
            (
                v for v in viz_tokens
                if len(token) >= 3 and len(token) < len(v) and v.startswith(token)
            ),
            None,
        )
        if not replacement and len(token) <= 5:
            for visual in viz_tokens:
                common = 0
                for a, b in zip(token, visual):
                    if a != b:
                        break
                    common += 1
                if common >= 3 and len(visual) > len(token):
                    replacement = token + visual[common:]
                    break
        merged.append(replacement or token)

    return _compact_spaces(" ".join(merged)) or None


def _complete_english_name(mrz_name: str | None, viz_name: str | None) -> str | None:
    if _latin_name_score(mrz_name):
        return _merge_mrz_and_visual_name(mrz_name, viz_name)
    if _latin_name_score(viz_name):
        return _clean_latin_name(viz_name)
    return None


def _arabic_name_is_suspicious(name_ar: str | None) -> bool:
    if not name_ar:
        return True
    if re.search(r"[A-Za-z0-9_:]", name_ar):
        return True
    words = [w for w in re.split(r"\s+", name_ar) if w]
    return len(words) < 3 or any(len(w) <= 1 for w in words)


def _arabic_name_conf(name_ar: str | None) -> float:
    """
    Score an Arabic name string on a 0–1 scale based on strict content quality checks.
    Conservative: start at 0.0 and only add points for verified quality indicators.
    
    Quality penalties:
      - Contains Latin/digits/symbols:              max 0.25 (likely OCR error)
      - Suspicious patterns (diacritics heavy):     reduce by 0.20
      - Very short total length (< 8 chars):        max 0.50
      - Single word only:                           max 0.60
    Quality bonuses (cumulative, starting from 0.0):
      - Only Arabic + spaces:                       +0.25
      - Has 2+ words:                               +0.20
      - All words >= 2 chars:                       +0.20
      - All words >= 3 chars:                       +0.15
      - Realistic length (>= 8 chars):              +0.20
    """
    if not name_ar:
        return 0.0
    
    # Detect OCR artefacts — these are red flags
    if re.search(r"[A-Za-z0-9_:؛،]", name_ar):
        return 0.25  # Mixed scripts = high error likelihood
    
    # Check for excessive diacritics (sign of garbled OCR)
    diacritics = len(re.findall(r"[\u064B-\u0652]", name_ar))
    if diacritics > len(name_ar) * 0.3:  # > 30% diacritics = suspicious
        return 0.20
    
    words = [w for w in re.split(r"\s+", name_ar.strip()) if w]
    if not words or not name_ar.strip():
        return 0.0
    
    # Start conservative
    score = 0.0
    
    # Bonus: pure Arabic text
    if not re.search(r"[^ء-ي\s]", name_ar):
        score += 0.25
    
    # Bonus: has 2+ words
    if len(words) >= 2:
        score += 0.20
    else:
        # Penalty: single word only
        score = min(score, 0.60)
    
    # Bonus: all words >= 2 chars
    min_word_len = min(len(w) for w in words)
    if min_word_len >= 2:
        score += 0.20
    
    # Bonus: all words >= 3 chars
    if min_word_len >= 3:
        score += 0.15
    
    # Bonus: realistic total length
    total_len = sum(len(w) for w in words)
    if total_len >= 8:
        score += 0.20
    else:
        # Penalty: too short = likely corrupted
        score = min(score, 0.50)
    
    return round(min(max(score, 0.0), 1.0), 4)


def _latin_name_conf(name_en: str | None) -> float:
    """
    Score an English/Latin name string on a 0–1 scale based on strict quality.
    Conservative approach: start at 0.0 and reward verified quality indicators.
    
    Quality penalties:
      - Contains digits/special chars (except apostrophe/hyphen): max 0.25
      - Single word only:                                       max 0.60
      - Very short total length (< 6 chars):                   max 0.50
      - Contains skip words/labels:                            reduce by 0.20
    
    Quality bonuses (cumulative, from 0.0):
      - Only Latin letters, spaces, apostrophe, hyphen:       +0.25
      - Has 2+ words:                                         +0.20
      - All words >= 2 chars:                                 +0.20
      - All words >= 3 chars:                                 +0.15
      - Realistic length (>= 6 chars):                        +0.20
    """
    if not name_en:
        return 0.0
    
    # Detect suspicious patterns
    if re.search(r"[^A-Za-z\s'\-]", name_en):
        return 0.25  # Contains digits/symbols = likely error
    
    words = [w for w in re.split(r"\s+", name_en.strip()) if w]
    if not words or not name_en.strip():
        return 0.0
    
    # Start conservative
    score = 0.0
    
    # Bonus: only valid Latin characters
    if not re.search(r"[^A-Za-z\s'\-]", name_en):
        score += 0.25
    
    # Bonus: has 2+ words
    if len(words) >= 2:
        score += 0.20
    else:
        # Penalty: single word only
        score = min(score, 0.60)
    
    # Bonus: all words >= 2 chars
    min_word_len = min(len(w) for w in words)
    if min_word_len >= 2:
        score += 0.20
    
    # Bonus: all words >= 3 chars
    if min_word_len >= 3:
        score += 0.15
    
    # Bonus: realistic total length
    total_len = sum(len(w) for w in words)
    if total_len >= 6:
        score += 0.20
    else:
        # Penalty: too short
        score = min(score, 0.50)
    
    # Penalty: contains skip words or labels
    if any(w in _SKIP_WORDS or w in _LATIN_LABEL_WORDS for w in words):
        score = max(0.0, score - 0.20)
    
    return round(min(max(score, 0.0), 1.0), 4)


def _complete_arabic_name(name_ar: str | None, name_en: str | None) -> str | None:
    return _clean_arabic_name(name_ar)


def _quality_with_readability(quality: dict, tokens: list[dict], fields: dict) -> dict:
    q = dict(quality)
    has_text = len(tokens) >= 6
    has_fields = sum(1 for v in fields.values() if v) >= 3
    readable = has_text and has_fields and 0.06 <= q.get("brightness", 0) <= 0.98 and q.get("contrast", 0) >= 0.025
    q["usable"] = bool(q.get("usable") or readable)
    return q


def _aggregate_quality(front_quality: dict, back_quality: dict) -> dict:
    return {
        "blur": round(min(front_quality.get("blur", 0), back_quality.get("blur", 0)), 2),
        "brightness": round((front_quality.get("brightness", 0) + back_quality.get("brightness", 0)) / 2, 3),
        "contrast": round((front_quality.get("contrast", 0) + back_quality.get("contrast", 0)) / 2, 3),
        "usable": bool(front_quality.get("usable")) if not back_quality.get("usable") else bool(front_quality.get("usable") and back_quality.get("usable")),
        "front": front_quality,
        "back": back_quality,
    }


def _has_arabic_label(text: str, keys: tuple[str, ...]) -> bool:
    key = _arabic_key(text)
    return any(k in key for k in keys)


def _same_row_tokens(tokens: list[dict], anchor: dict, dy: int = 36) -> list[dict]:
    return [t for t in tokens if t is not anchor and abs(t["cy"] - anchor["cy"]) <= dy]


def _numeric_context(text: str | None) -> str:
    return _normalize_digits(text).translate(str.maketrans({
        "O": "0", "o": "0", "Q": "0", "q": "0", "D": "0",
        "I": "1", "i": "1", "L": "1", "l": "1",
        "S": "5", "s": "5", "B": "8", "b": "8",
        "Z": "2", "z": "2", "G": "6", "g": "6",
    }))


def _extract_national_number(tokens: list[dict]) -> str | None:
    text = _numeric_context(_token_text_all(tokens))
    matches = re.findall(r"(?<!\d)(\d{10})(?!\d)", text)
    if matches:
        return matches[0]
    match = re.search(r"(?<!\d)(\d{8,12})(?!\d)", text)
    return match.group(1) if match else None


def _parse_visual_date(value: str | None, kind: str = "any") -> str | None:
    if not value:
        return None
    text = _numeric_context(value)
    for match in re.finditer(r"(?<!\d)(\d{1,2})\s*[/.\-|]\s*(\d{1,2})\s*[/.\-|]\s*(\d{2,4})(?!\d)", text):
        day, month, year = int(match.group(1)), int(match.group(2)), match.group(3)
        if len(year) == 2:
            year = str(2000 + int(year) if kind == "expiry" else (2000 + int(year) if 2000 + int(year) <= date.today().year else 1900 + int(year)))
        yyyy = int(year)
        if 1 <= day <= 31 and 1 <= month <= 12 and 1900 <= yyyy <= 2099:
            return f"{day:02d}/{month:02d}/{yyyy:04d}"
    return None


def _extract_arabic_value_near_label(
    tokens: list[dict],
    keys: tuple[str, ...],
    *,
    min_words: int = 1,
    keep_slash: bool = False,
    prefer_inline: bool = False,
) -> str | None:
    candidates: list[tuple[int, str]] = []
    for anchor in tokens:
        if not _has_arabic_label(anchor["text"], keys):
            continue

        inline = None
        if ":" in anchor["text"] or "：" in anchor["text"]:
            inline = _clean_arabic_field(anchor["text"], keep_slash=keep_slash, min_words=min_words)
        if inline:
            candidates.append((len(inline.split()) * 10 + (30 if prefer_inline else 0), inline))
            if prefer_inline:
                continue

        row = [
            t for t in _same_row_tokens(tokens, anchor)
            if ARABIC_RE.search(t["text"])
            and not _has_arabic_label(t["text"], keys)
            and not _has_arabic_label(t["text"], ("الرقم", "الوطني", "الجنس", "تاريخ", "مكان", "وزارة", "دائرة", "بطاقة"))
        ]
        row.sort(key=lambda t: -t["x"])
        row_value = _clean_arabic_field(" ".join(t["text"] for t in row), keep_slash=keep_slash, min_words=min_words)
        if row_value:
            candidates.append((len(row_value.split()) * 5 + len(row), row_value))

        below = [
            t for t in tokens
            if t is not anchor
            and 0 < t["y"] - anchor["y2"] <= 95
            and ARABIC_RE.search(t["text"])
            and not is_label_token(t["text"])
        ]
        if below:
            first_y = min(t["cy"] for t in below)
            below_row = sorted([t for t in below if abs(t["cy"] - first_y) <= 35], key=lambda t: -t["x"])
            below_value = _clean_arabic_field(" ".join(t["text"] for t in below_row), keep_slash=keep_slash, min_words=min_words)
            if below_value:
                candidates.append((len(below_value.split()) * 3, below_value))

    if not candidates:
        return None
    candidates.sort(key=lambda item: item[0], reverse=True)
    return candidates[0][1]


def _extract_arabic_name_from_rows(tokens: list[dict]) -> str | None:
    reject_keys = (
        "الرقم", "الوطني", "الجنس", "تاريخ", "مكان", "وزارة", "دائرة",
        "بطاقة", "المملكة", "الهاشمية", "الاحوال", "الجوازات", "شخصية",
        "الولادة", "الام", "فصيلة", "الدم",
    )
    row_groups: dict[int, list[dict]] = {}
    for token in tokens:
        text = token.get("text", "")
        if not ARABIC_RE.search(text):
            continue
        if _has_arabic_label(text, reject_keys) and not _has_arabic_label(text, _AR_NAME_KEYS):
            continue
        bucket = int(round(token["cy"] / 35) * 35)
        row_groups.setdefault(bucket, []).append(token)

    candidates: list[tuple[float, str]] = []
    for row in row_groups.values():
        row.sort(key=lambda t: -t["x"])
        value = _clean_arabic_name(" ".join(t["text"] for t in row))
        if not value:
            continue
        words = value.split()
        if len(words) < 2 or len(words) > 6:
            continue
        avg_conf = _avg_conf(row)
        score = _arabic_candidate_score(value, avg_conf)
        candidates.append((score, value))

    if not candidates:
        return None
    candidates.sort(key=lambda item: (item[0], len(item[1])), reverse=True)
    return candidates[0][1]


def _extract_latin_visual_name(tokens: list[dict]) -> str | None:
    candidates: list[tuple[int, str]] = []
    for token in tokens:
        text = strip_arabic(token["text"])
        if re.search(r"NAME\s*[:.\-]?", text, re.I):
            inline = re.sub(r".*?NAME\s*[:.\-]?", " ", text, flags=re.I)
            row = [
                t for t in _same_row_tokens(tokens, token)
                if re.search(r"[A-Za-z]{2,}", strip_arabic(t["text"]))
                and not is_label_token(t["text"])
            ]
            row.sort(key=lambda t: t["x"])
            value = _clean_latin_name(" ".join([inline] + [t["text"] for t in row]))
            if value:
                candidates.append((_latin_name_score(value) + 10, value))

    row_groups: dict[int, list[dict]] = {}
    for token in tokens:
        text = strip_arabic(token["text"])
        if not re.search(r"[A-Za-z]{3,}", text) or is_label_token(token["text"]):
            continue
        bucket = int(round(token["cy"] / 35) * 35)
        row_groups.setdefault(bucket, []).append(token)

    for row in row_groups.values():
        row.sort(key=lambda t: t["x"])
        value = _clean_latin_name(" ".join(t["text"] for t in row))
        if len(value.split()) >= 2:
            candidates.append((_latin_name_score(value), value))

    if not candidates:
        return None
    candidates.sort(key=lambda item: item[0], reverse=True)
    return candidates[0][1]


def _extract_gender(tokens: list[dict]) -> str | None:
    full = _token_text_all(tokens)
    arabic_full = _arabic_key(full)
    if re.search(r"(?:^|[\s:/])M\s*/|/\s*M(?:[\s:/]|$)", full, re.I) or "ذكر" in arabic_full:
        return "Male"
    if re.search(r"(?:^|[\s:/])F\s*/|/\s*F(?:[\s:/]|$)", full, re.I) or "انث" in arabic_full:
        return "Female"
    for token in tokens:
        if _has_arabic_label(token["text"], ("الجنس", "جنس")):
            row_text = " ".join(t["text"] for t in [token] + _same_row_tokens(tokens, token))
            if re.search(r"\bM\b", row_text, re.I) or "ذكر" in _arabic_key(row_text):
                return "Male"
            if re.search(r"\bF\b", row_text, re.I) or "انث" in _arabic_key(row_text):
                return "Female"
    return None


def _extract_blood_type(tokens: list[dict]) -> str | None:
    texts = [_token_text_all(tokens)]
    for token in tokens:
        if _has_arabic_label(token["text"], ("فصيلة", "الدم")) or re.search(r"\bBLOOD\b", token["text"], re.I):
            row = [token] + _same_row_tokens(tokens, token, dy=45)
            texts.insert(0, " ".join(t["text"] for t in row))

    for text in texts:
        s = strip_arabic(_normalize_digits(text)).upper()
        s = s.replace("＋", "+").replace("﹢", "+").replace("–", "-")
        match = re.search(r"(?<![A-Z0-9])((?:A|4)\s*(?:B|8)|A|4|B|8|O|0)\s*([+\-])", s)
        if not match:
            continue
        group = re.sub(r"\s+", "", match.group(1))
        group = group.replace("4", "A").replace("8", "B").replace("0", "O")
        if group in {"A", "B", "AB", "O"}:
            return f"{group}{match.group(2)}"
    return None


def _extract_registry_no(tokens: list[dict]) -> str | None:
    text = _numeric_context(_token_text_all(tokens)).replace("|", "/").replace("\\", "/")
    for match in re.finditer(r"(?<!\d)(\d{2,4})\s*[/\-]\s*(\d{2,4})(?!\d)", text):
        left, right = match.group(1), match.group(2)
        if len(left) <= 2 and len(right) <= 2:
            continue
        return f"{left}/{right}"
    return None


def _extract_document_number(tokens: list[dict]) -> str | None:
    texts = []
    for token in tokens:
        if re.search(r"\bID\s*NO\b", strip_arabic(token["text"]), re.I):
            row = [token] + _same_row_tokens(tokens, token, dy=45)
            texts.append(" ".join(t["text"] for t in row))
            below = [
                t for t in tokens
                if t["y"] > token["y2"]
                and t["y"] - token["y2"] <= 70
                and re.search(r"[A-Za-z0-9]", t["text"])
            ]
            if below:
                texts.append(" ".join(t["text"] for t in sorted(below, key=lambda t: (t["y"], t["x"]))))
    texts.append(_token_text_all(tokens))

    for text in texts:
        s = strip_arabic(_normalize_digits(text)).upper()
        s = re.sub(r"\bID\s*NO\.?\b", " ", s)
        for match in re.finditer(r"(?<![A-Z0-9])([A-Z]{2,4})\s*[-_ ]?\s*([0-9OQDISBLZG]{4,8})(?![A-Z0-9])", s):
            prefix = match.group(1)
            digits = _numeric_context(match.group(2))
            if digits.isdigit():
                return f"{prefix}{digits}"
    return None


def _clean_jordan_place(value: str | None, *, issue_place: bool = False) -> str | None:
    cleaned = _clean_arabic_field(value, keep_slash=not issue_place)
    if not cleaned:
        return None
    words = cleaned.split()
    city_indexes = [i for i, w in enumerate(words) if w in _JORDAN_PLACE_WORDS]
    if not city_indexes:
        return cleaned
    if issue_place:
        return words[city_indexes[-1]]
    last_city = city_indexes[-1]
    return _compact_spaces(" ".join(words[:last_city + 1])) or cleaned


_AR_NAME_KEYS = ("الاسم", "لاسم", "الإسم", "لإسم")


def _arabic_candidate_score(value: str | None, ocr_conf: float = 0.0, source: str = "") -> float:
    cleaned = _clean_arabic_name(value)
    if not cleaned:
        return 0.0
    words = cleaned.split()
    score = _arabic_name_conf(cleaned) * 100.0
    score += min(len(cleaned), 60) * 0.8
    score += min(len(words), 5) * 8.0
    score += max(0.0, min(float(ocr_conf or 0.0), 1.0)) * 45.0
    if source == "standard":
        score += 12.0
    elif source == "original":
        score += 6.0
    if len(words) < 3:
        score -= 18.0
    if _arabic_name_is_suspicious(cleaned):
        score -= 22.0
    return round(score, 3)


def _arabic_candidate_from_tokens(tokens: list[dict], source: str) -> dict | None:
    raw = (
        _extract_arabic_value_near_label(tokens, _AR_NAME_KEYS, min_words=2)
        or _extract_arabic_name_from_rows(tokens)
    )
    value = _clean_arabic_name(normalize_arabic_text(raw))
    if not value:
        return None
    arabic_tokens = [t for t in tokens if ARABIC_RE.search(t.get("text", ""))]
    ocr_conf = _avg_conf(arabic_tokens)
    conf = _arabic_name_conf(value)
    return {
        "source": source,
        "value": value,
        "conf": conf,
        "ocr_conf": ocr_conf,
        "score": _arabic_candidate_score(value, ocr_conf, source),
    }


def _select_best_arabic_candidate(candidates: list[dict]) -> dict | None:
    if not candidates:
        return None
    return sorted(
        candidates,
        key=lambda c: (
            c.get("score", 0.0),
            c.get("conf", 0.0),
            len(c.get("value", "")),
            len(c.get("value", "").split()),
        ),
        reverse=True,
    )[0]


def _log_arabic_candidate(label: str, candidate: dict | None) -> None:
    if candidate:
        print(
            f"   Arabic OCR {label}: {candidate['value']!r} "
            f"(conf={candidate['conf']}, ocr={candidate['ocr_conf']}, score={candidate['score']})"
        )
    else:
        print(f"   Arabic OCR {label}: <none>")


def _crop_arabic_name_zone(front_img):
    h, w = front_img.shape[:2]
    y0, y1 = int(h * 0.15), int(h * 0.40)
    x0, x1 = int(w * 0.30), w
    crop = front_img[y0:y1, x0:x1]
    return crop if crop.size else front_img


def _extract_best_arabic_name(front_img, base_tokens: list[dict] | None = None) -> dict | None:
    name_zone = _crop_arabic_name_zone(front_img)
    variants = build_arabic_ocr_variants(name_zone)
    candidates: list[dict] = []

    if base_tokens:
        standard = _arabic_candidate_from_tokens(base_tokens, "standard")
        if standard:
            candidates.append(standard)
        _log_arabic_candidate("standard", standard)

    original_tokens = ocr_to_tokens(variants["original"], READER_MIX, min_conf=0.02)
    original = _arabic_candidate_from_tokens(original_tokens, "original")
    if original:
        candidates.append(original)
    _log_arabic_candidate("original", original)

    enhanced_tokens = ocr_to_tokens(variants["enhanced"], READER_MIX, min_conf=0.02)
    enhanced = _arabic_candidate_from_tokens(enhanced_tokens, "enhanced")
    if enhanced:
        candidates.append(enhanced)
    _log_arabic_candidate("enhanced", enhanced)

    best = _select_best_arabic_candidate(candidates)
    low_confidence = (
        best is None
        or best.get("conf", 0.0) < CONF_THRESHOLD
        or len(best.get("value", "").split()) < 3
    )

    if low_confidence:
        for name in ("sharpened", "high_contrast", "adaptive_threshold"):
            tokens = ocr_to_tokens(variants[name], READER_MIX, min_conf=0.02)
            candidate = _arabic_candidate_from_tokens(tokens, name)
            if candidate:
                candidates.append(candidate)
            _log_arabic_candidate(name, candidate)
        best = _select_best_arabic_candidate(candidates)

    _log_arabic_candidate("selected", best)
    return best


def _apply_arabic_name_candidate(front_data: dict, candidate: dict | None) -> None:
    if not candidate:
        return
    current = _clean_arabic_name(normalize_field(front_data.get("full_name_ar")))
    current_conf = _arabic_name_conf(current)
    current_score = _arabic_candidate_score(current, current_conf)
    candidate_score = candidate.get("score", 0.0)
    candidate_value = candidate.get("value")

    is_better = (
        not current
        or candidate_score > current_score + 5.0
        or (
            len(candidate_value or "") > len(current) + 2
            and candidate.get("conf", 0.0) >= max(0.0, current_conf - 0.1)
        )
    )
    if is_better and candidate_value:
        front_data["full_name_ar"] = candidate_value
        front_data["_conf_full_name_ar"] = candidate.get("conf", 0.0)


_MOTHER_LABEL_KEYS = ("\u0627\u0633\u0645 \u0627\u0644\u0627\u0645", "\u0627\u0633\u0645 \u0627\u0644\u0623\u0645", "\u0627\u0644\u0627\u0645", "\u0627\u0644\u0623\u0645")


def _clean_mother_candidate(value: str | None) -> str | None:
    cleaned = _clean_arabic_field(value, min_words=1)
    if not cleaned:
        return None
    words = [w for w in re.split(r"\s+", cleaned) if w]
    if not words or len(words) > 3:
        return None
    if any(_has_arabic_label(w, _AR_NAME_KEYS + _MOTHER_LABEL_KEYS) for w in words):
        return None
    if any(w in _AR_STOP_WORDS for w in words):
        return None
    if re.search(r"[A-Za-z0-9_:]", cleaned):
        return None
    if sum(len(w) for w in words) < 3:
        return None
    return cleaned


def _mother_candidate_score(value: str | None, conf: float = 0.0, source: str = "") -> float:
    cleaned = _clean_mother_candidate(value)
    if not cleaned:
        return -100.0
    words = cleaned.split()
    score = 20.0 + min(len(cleaned), 20) * 2.0 + max(0.0, conf) * 40.0
    if len(words) == 1:
        score += 12.0
    elif len(words) == 2:
        score += 5.0
    else:
        score -= 10.0
    if source in {"crop", "enhanced", "sharpened", "high_contrast"}:
        score += 8.0
    return score


def _mother_candidate_from_tokens(tokens: list[dict], source: str = "tokens") -> dict | None:
    value = _clean_mother_candidate(
        _extract_arabic_value_near_label(tokens, _MOTHER_LABEL_KEYS, min_words=1, prefer_inline=True)
    )
    if not value:
        return None
    conf = _avg_conf([t for t in tokens if ARABIC_RE.search(t.get("text", ""))])
    compact = re.sub(r"\s+", "", value)
    if source != "front" and len(compact) <= 3 and conf < 0.65:
        return None
    return {
        "value": value,
        "conf": conf,
        "source": source,
        "score": _mother_candidate_score(value, conf, source),
    }


def _crop_mother_name_zone(front_img):
    h, w = front_img.shape[:2]
    y0, y1 = int(h * 0.76), int(h * 0.98)
    x0, x1 = int(w * 0.58), w
    crop = front_img[y0:y1, x0:x1]
    return crop if crop.size else front_img


def _extract_best_mother_name(front_img, base_tokens: list[dict] | None = None) -> dict | None:
    candidates: list[dict] = []
    if base_tokens:
        candidate = _mother_candidate_from_tokens(base_tokens, "front")
        if candidate:
            candidates.append(candidate)

    crop = _crop_mother_name_zone(front_img)
    variants = build_arabic_ocr_variants(crop)
    variant_names = ("original", "enhanced", "sharpened")
    if os.environ.get("OCR_EXTRA_MOTHER_VARIANTS") == "1":
        variant_names = ("original", "enhanced", "sharpened", "high_contrast")
    for variant_name in variant_names:
        tokens = ocr_to_tokens(variants[variant_name], READER_MIX, min_conf=0.02)
        candidate = _mother_candidate_from_tokens(tokens, variant_name)
        if candidate:
            candidates.append(candidate)

    best = max(candidates, key=lambda c: (c.get("score", 0.0), len(c.get("value", ""))), default=None)
    if best and best.get("score", 0.0) > 0:
        print(f"   mother candidate [{best.get('source')}]: {best.get('value')} ({best.get('conf', 0.0):.2f})")
        return best
    return None


def _apply_mother_name_candidate(front_data: dict, candidate: dict | None) -> None:
    if not candidate:
        return
    current = _clean_mother_candidate(normalize_field(front_data.get("mother_name") or front_data.get("mothers_name_ar")))
    current_conf = float(front_data.get("_conf_mother_name") or 0.0)
    current_score = _mother_candidate_score(current, current_conf, "current")
    candidate_value = candidate.get("value")
    candidate_score = float(candidate.get("score") or 0.0)
    if candidate_value and (not current or candidate_score >= current_score + 4.0 or len(candidate_value) > len(current)):
        front_data["mother_name"] = candidate_value
        front_data["mothers_name_ar"] = candidate_value
        front_data["_conf_mother_name"] = candidate.get("conf", 0.0)


CONF_THRESHOLD = 0.75


def _avg_conf(tokens: list[dict]) -> float:
    """Return average OCR confidence of a token list (0.0–1.0).
    WARNING: This is unreliable on document fonts. Use specific validators instead."""
    confs = [t.get("conf", 1.0) for t in tokens if t.get("text")]
    return round(sum(confs) / len(confs), 4) if confs else 0.0


def _national_number_conf(value: str | None) -> float:
    """
    Validate and score a national number (should be 10-12 digits for Jordan).
    Conservative scoring based on content validation, not OCR scores.
    """
    if not value:
        return 0.0
    
    # Clean: should only contain digits
    cleaned = re.sub(r"\D", "", value)
    
    # Too short or too long = corrupted
    if len(cleaned) < 10 or len(cleaned) > 12:
        return 0.20
    
    # All digits = valid pattern
    if len(cleaned) == len(value):
        return 0.85  # High confidence for clean all-digit number
    
    # Has some non-digits mixed in = OCR error
    noise_ratio = 1.0 - (len(cleaned) / len(value))
    if noise_ratio > 0.2:
        return 0.30  # Too much noise
    
    return 0.65  # Some noise but mostly valid


def _date_of_birth_conf(value: str | None) -> float:
    """
    Validate and score a date of birth (DD/MM/YYYY format).
    High confidence only for realistic dates.
    """
    if not value:
        return 0.0
    
    # Should be DD/MM/YYYY or similar separators
    match = re.match(r"^(\d{1,2})[-/\.\\](\d{1,2})[-/\.\\](\d{4})$", value)
    if not match:
        return 0.20  # Not even close to date format
    
    try:
        day, month, year = int(match.group(1)), int(match.group(2)), int(match.group(3))
        
        # Validate ranges
        if not (1 <= month <= 12):
            return 0.25  # Invalid month
        if not (1 <= day <= 31):
            return 0.25  # Invalid day
        
        # Year should be 1920-2020 (reasonable birth year range)
        if not (1920 <= year <= 2020):
            return 0.30
        
        # Valid realistic date
        return 0.90
    except (ValueError, TypeError):
        return 0.20


def _document_number_conf(value: str | None) -> float:
    """
    Validate and score a document number (typically AA123456 format).
    """
    if not value:
        return 0.0
    
    # Should be 2-4 letters + 4-8 digits
    match = re.match(r"^([A-Z]{2,4})(\d{4,8})$", value.upper())
    if not match:
        return 0.25  # Doesn't match expected format
    
    prefix, digits = match.group(1), match.group(2)
    
    # Check for unrealistic patterns
    # Repeated same digit (like 1111) = likely OCR error
    if len(set(digits)) == 1:
        return 0.40
    
    # All different digits = normal
    if len(set(digits)) >= len(digits) - 1:
        return 0.85
    
    return 0.70


def _gender_conf(value: str | None) -> float:
    """
    Validate and score gender field (M, F, Male, Female, etc).
    """
    if not value:
        return 0.0
    
    normalized = value.upper().strip()
    
    # Valid gender values
    if normalized in ("M", "F", "MALE", "FEMALE", "MALE", "FEMALE"):
        return 0.95
    
    # Close but not quite
    if normalized in ("MA", "FE", "MR", "MS"):
        return 0.60
    
    # Doesn't match at all
    return 0.10


def _levenshtein_distance(s1: str, s2: str) -> int:
    """
    Calculate Levenshtein distance between two strings.
    Measures minimum number of single-character edits (insert, delete, substitute)
    needed to change s1 into s2.
    
    Examples:
      - "1234567890" vs "1234567891" = 1 (one digit different)
      - "12345" vs "12345" = 0 (identical)
      - "ABCD" vs "ACBD" = 2 (two substitutions)
    
    This is useful for detecting OCR errors where 1-2 digits are wrong.
    """
    if len(s1) == 0:
        return len(s2)
    if len(s2) == 0:
        return len(s1)
    
    # Create matrix to store distances
    rows = len(s1) + 1
    cols = len(s2) + 1
    dist = [[0] * cols for _ in range(rows)]
    
    # Initialize first column and row
    for i in range(1, rows):
        dist[i][0] = i
    for j in range(1, cols):
        dist[0][j] = j
    
    # Calculate distances
    for i in range(1, rows):
        for j in range(1, cols):
            if s1[i - 1] == s2[j - 1]:
                cost = 0
            else:
                cost = 1
            
            dist[i][j] = min(
                dist[i - 1][j] + 1,      # deletion
                dist[i][j - 1] + 1,      # insertion
                dist[i - 1][j - 1] + cost  # substitution
            )
    
    return dist[rows - 1][cols - 1]


def _validate_national_number_with_mrz(extracted: str, mrz_national_id: str) -> float:
    """
    Validate extracted national number by comparing with MRZ number.
    Returns confidence based on how well they match.
    
    Returns:
      - 0.98: Exact match → number is definitely correct
      - 0.70: 1 digit different → likely OCR error on one digit
      - 0.40: 2 digits different → significant error
      - 0.15: Multiple digits different → one source is wrong
      - Falls back to format check if no MRZ available
    """
    if not mrz_national_id:
        return _national_number_conf(extracted)  # Fall back to format check
    
    # Normalize both to remove non-digits for comparison
    extracted_digits = re.sub(r"\D", "", extracted or "")
    mrz_digits = re.sub(r"\D", "", mrz_national_id or "")
    
    if not extracted_digits or not mrz_digits:
        return _national_number_conf(extracted)
    
    # Calculate how many digits are different using Levenshtein distance
    distance = _levenshtein_distance(extracted_digits, mrz_digits)
    
    # Exact match
    if distance == 0:
        return 0.98  # ✅ Matches MRZ perfectly - definitely correct!
    
    # Single digit difference (common OCR error)
    elif distance == 1:
        return 0.70  # ⚠️ 1 digit different - likely OCR misread 1 digit
    
    # Two digits different (less likely but possible)
    elif distance == 2:
        return 0.40  # ⚠️ 2 digits different - significant error
    
    # Multiple differences
    else:
        return 0.15  # ❌ Numbers are very different - one is wrong


def _conf_or_null(value, conf: float):
    """Return value if conf >= threshold, else None. Always returns (value, conf)."""
    if conf < CONF_THRESHOLD:
        return None, conf
    return value, conf


def parse_front(tokens: list[dict]) -> dict:
    fields: dict = {}

    national_number = _extract_national_number(tokens)
    if national_number:
        fields["national_number"] = national_number
        fields["_conf_national_number"] = _national_number_conf(national_number)

    dob = None
    dob_tokens: list[dict] = []
    for token in tokens:
        if _has_arabic_label(token["text"], ("تاريخ", "الولادة")) or re.search(r"\bBIRTH\b", token["text"], re.I):
            row_tokens = [token] + _same_row_tokens(tokens, token)
            row = " ".join(t["text"] for t in row_tokens)
            dob = _parse_visual_date(row, "birth")
            if dob:
                dob_tokens = row_tokens
                break
    if not dob:
        dob = _parse_visual_date(_token_text_all(tokens), "birth")
        dob_tokens = tokens
    fields["date_of_birth"] = dob
    fields["_conf_date_of_birth"] = _date_of_birth_conf(dob) if dob else 0.0

    gender = _extract_gender(tokens)
    if gender:
        fields["gender"] = gender
        fields["_conf_gender"] = _gender_conf(gender)

    name_en = _extract_latin_visual_name(tokens)
    if name_en:
        fields["full_name_en"] = name_en
        fields["_conf_full_name_en"] = _latin_name_conf(name_en)

    name_ar = (
        _extract_arabic_value_near_label(tokens, _AR_NAME_KEYS, min_words=2)
        or _extract_arabic_name_from_rows(tokens)
    )
    if name_ar:
        fields["full_name_ar"] = name_ar
        fields["_conf_full_name_ar"] = _arabic_name_conf(name_ar)

    place = _extract_arabic_value_near_label(tokens, ("مكان الولادة", "الولادة"), min_words=1)
    if place:
        fields["place_of_birth"] = place

    mother = _clean_mother_candidate(_extract_arabic_value_near_label(tokens, _MOTHER_LABEL_KEYS, min_words=1, prefer_inline=True))
    if mother:
        fields["mothers_name_ar"] = mother
        fields["_conf_mother_name"] = _mother_candidate_score(mother, 0.0, "front") / 100.0

    return fields


# ══════════════════════════════════════════════════════════════════════════════
#  BACK-SIDE FIELD PARSER
# ══════════════════════════════════════════════════════════════════════════════

def parse_back(tokens: list[dict]) -> dict:
    fields: dict = {}

    # ── Expiry date ───────────────────────────────────────────────────────────
    expiry = None
    for token in tokens:
        if (
            _has_arabic_label(token["text"], ("الصلاحية", "انتهاء"))
            or re.search(r"\bEXPIRY\b|\bEXP\b", token["text"], re.I)
        ):
            row = " ".join(t["text"] for t in [token] + _same_row_tokens(tokens, token))
            expiry = _parse_visual_date(row, "expiry")
            if expiry:
                break
    expiry = expiry or _parse_visual_date(_token_text_all(tokens), "expiry")
    if expiry:
        fields["expiry_date"] = expiry

    # ── Issue place ───────────────────────────────────────────────────────────
    issue = _extract_arabic_value_near_label(tokens, ("مكان الاصدار", "الاصدار"), min_words=1)
    if issue:
        fields["issue_place"] = _clean_jordan_place(issue, issue_place=True) or issue

    # ── Residence place ───────────────────────────────────────────────────────
    residence = _extract_arabic_value_near_label(tokens, ("مكان الاقامة", "الاقامة"), min_words=1, keep_slash=True)
    if residence:
        fields["residence_place"] = _clean_jordan_place(residence) or residence

    # ── Blood type ────────────────────────────────────────────────────────────
    blood = _extract_blood_type(tokens)
    if blood:
        fields["blood_type"] = blood

    # ── Registry number ───────────────────────────────────────────────────────
    registry_no = _extract_registry_no(tokens)
    if registry_no:
        fields["registry_no"] = registry_no

    # ── Document number ───────────────────────────────────────────────────────
    doc_no = _extract_document_number(tokens)
    if doc_no:
        fields["document_number"] = doc_no
        fields["_conf_document_number"] = _document_number_conf(doc_no)

    return fields


# ══════════════════════════════════════════════════════════════════════════════
#  CROSS-VALIDATION
# ══════════════════════════════════════════════════════════════════════════════

def _normalise_name(s: str) -> str:
    return re.sub(r"\s+", " ", re.sub(r"[^A-Z\s]", "", s.upper())).strip()


def cross_validate_id(front: dict, back: dict, mrz: dict) -> tuple[dict, dict]:
    checks = mrz.get("mrz_checks", {})

    # National number
    if not front.get("national_number") and mrz.get("mrz_national_id"):
        front["national_number"] = mrz["mrz_national_id"]

    # DOB
    def _is_valid_date(d: str) -> bool:
        try:
            parts = d.strip().split("/")
            if len(parts) != 3:
                return False
            day, month, year = int(parts[0]), int(parts[1]), int(parts[2])
            return 1 <= day <= 31 and 1 <= month <= 12 and 1900 <= year <= 2100
        except (ValueError, AttributeError):
            return False

    mrz_dob = mrz.get("mrz_dob_parsed")
    viz_dob = front.get("date_of_birth")

    if checks.get("dob") and mrz_dob:
        front["date_of_birth"] = mrz_dob
    elif viz_dob and _is_valid_date(viz_dob):
        front["date_of_birth"] = viz_dob
    elif mrz_dob:
        front["date_of_birth"] = mrz_dob

    # Expiry
    mrz_expiry = mrz.get("mrz_expiry_parsed")
    viz_expiry = back.get("expiry_date")
    if checks.get("expiry") and mrz_expiry:
        back["expiry_date"] = mrz_expiry
    elif mrz_expiry and (not viz_expiry or not _is_valid_date(viz_expiry)):
        back["expiry_date"] = mrz_expiry
    elif viz_expiry and _is_valid_date(viz_expiry):
        back["expiry_date"] = viz_expiry
    elif mrz_expiry:
        back["expiry_date"] = mrz["mrz_expiry_parsed"]

    # English name — unwrap from dict if needed
    def _unwrap_name(src: dict, key: str) -> str:
        v = src.get(key)
        if isinstance(v, dict):
            return (v.get("value") or "").strip()
        return (v or "").strip()

    def _wrap_name(src: dict, key: str, new_value: str | None):
        """Store a name field as {"value": str, "conf": float}.
        Conf is computed from the content quality of new_value, not inherited —
        because the old conf may have been computed on different/garbled text."""
        if not new_value:
            src[key] = None
            return
        if key == "full_name_ar":
            conf = _arabic_name_conf(new_value)
        else:
            conf = _latin_name_conf(new_value)
        src[key] = {"value": new_value, "conf": conf}
        src[f"_conf_{key}"] = conf

    mrz_name = mrz.get("mrz_name_full", "").strip()
    viz_name = _unwrap_name(front, "full_name_en")

    if mrz_name:
        completed_name = _complete_english_name(mrz_name, viz_name)
        if completed_name:
            _wrap_name(front, "full_name_en", completed_name)
    elif viz_name:
        _wrap_name(front, "full_name_en", _clean_latin_name(viz_name))

    viz_name_ar = _unwrap_name(front, "full_name_ar")
    completed_ar = _complete_arabic_name(viz_name_ar, _unwrap_name(front, "full_name_en"))
    if completed_ar:
        _wrap_name(front, "full_name_ar", completed_ar)

    # Document number
    visual_doc = re.sub(r"[^A-Z0-9]", "", str(back.get("document_number") or "").upper())
    mrz_doc = re.sub(r"[^A-Z0-9]", "", str(mrz.get("mrz_doc_number") or "").upper())
    visual_doc_valid = _document_number_conf(visual_doc) >= 0.70
    mrz_doc_valid = _document_number_conf(mrz_doc) >= 0.70
    if visual_doc_valid:
        back["document_number"] = mrz_doc if mrz_doc_valid and visual_doc.endswith(mrz_doc) else visual_doc
    elif mrz_doc_valid:
        back["document_number"] = mrz_doc
        back["_conf_document_number"] = max(float(back.get("_conf_document_number") or 0.0), _document_number_conf(mrz_doc))
    elif visual_doc:
        back["document_number"] = visual_doc

    # Gender
    if not front.get("gender") and mrz.get("mrz_sex") in ("M","F"):
        front["gender"] = "Male" if mrz["mrz_sex"] == "M" else "Female"

    if back.get("residence_place") and back.get("issue_place") and back["issue_place"] not in back["residence_place"]:
        back["residence_place"] = f"{back['residence_place']} {back['issue_place']}".strip()

    return front, back


# ══════════════════════════════════════════════════════════════════════════════
#  MAIN PIPELINE
# ══════════════════════════════════════════════════════════════════════════════
def extract_jordan_id(front_path: str, back_path: str | None = None) -> dict:
    print("\n🔍 Loading ID images…")
    front_img = load_and_resize(front_path)

    # ── Guard: skip back processing if no back image provided ────────────────
    has_back = back_path is not None and back_path != front_path
    back_img = load_and_resize(back_path) if has_back else None

    front_variants = build_variants(front_img)
    back_variants  = build_variants(back_img) if has_back else None

    front_quality = compute_quality(front_variants["enhanced"])
    back_quality  = compute_quality(back_variants["enhanced"]) if has_back else {"blur": 0, "brightness": 0, "contrast": 0, "usable": False}

    # ── Front OCR ─────────────────────────────────────────────────────────────
    print("🔍 OCR: front (ar+en)…")
    front_src    = select_source(front_variants, 0, 0, 1, 1, mode="text")
    front_tokens = ocr_to_tokens(front_src, READER_MIX)
    _trace("NID_EASYOCR_FRONT_TOKENS", _token_preview(front_tokens))
    print("🔍 OCR: front Arabic name variants…")
    best_arabic_name = _extract_best_arabic_name(front_img, front_tokens)
    best_mother_name = _mother_candidate_from_tokens(front_tokens, "front")

    # ── Back OCR — skip entirely if no back image ─────────────────────────────
    back_tokens = []
    mrz_raw     = []

    if has_back:
        print("🔍 OCR: back (two-pass)…")
        back_src  = select_source(back_variants, 0, 0, 1, 0.65, mode="text")
        tok_dict: dict = {}
        for tok in ocr_to_tokens(back_src, READER_MIX):
            cx  = int(round(tok["cx"]/SNAP)*SNAP)
            cy  = int(round(tok["cy"]/SNAP)*SNAP)
            key = (tok["text"].lower(), cx, cy)
            if key not in tok_dict or tok_dict[key]["conf"] < tok["conf"]:
                tok_dict[key] = tok
        for tok in ocr_to_tokens(back_variants["thresholded"], READER_EN, allowlist=MRZ_ALLOWLIST):
            cx  = int(round(tok["cx"]/SNAP)*SNAP)
            cy  = int(round(tok["cy"]/SNAP)*SNAP)
            key = (tok["text"].lower(), cx, cy)
            if key not in tok_dict or tok_dict[key]["conf"] < tok["conf"]:
                tok_dict[key] = tok
        back_tokens = sorted(tok_dict.values(), key=lambda t: (t["y"]//30, t["x"]))
        _trace("NID_EASYOCR_BACK_TOKENS", _token_preview(back_tokens))

        print("📡 Extracting MRZ from back tokens…")
        mrz_raw = extract_mrz_from_tokens(back_tokens, back_img.shape[0], doc_type="national_id")
        if len(mrz_raw) < 3:
            print("   ⚠️  MRZ not in tokens — falling back to strip OCR")
            strip = preprocess_mrz_strip(back_img)
            fallback_lines = ocr_mrz_strip_fallback(strip, lines_count=3)
            seen = {_mrz_clean(r.get("text", "")) for r in mrz_raw}
            mrz_raw = mrz_raw + [r for r in fallback_lines if _mrz_clean(r.get("text", "")) not in seen]
    else:
        print("⚠️  No back image — skipping MRZ and back-side fields")

    # ── Parse and cross-validate ───────────────────────────────────────────────
    print("🧩 Parsing fields…")
    front_data = parse_front(front_tokens)
    _apply_arabic_name_candidate(front_data, best_arabic_name)
    _apply_mother_name_candidate(front_data, best_mother_name)
    current_mother = _clean_mother_candidate(normalize_field(front_data.get("mother_name") or front_data.get("mothers_name_ar")))
    current_mother_conf = float(front_data.get("_conf_mother_name") or 0.0)
    if os.environ.get("OCR_FOCUSED_MOTHER", "1") != "0" and _mother_candidate_score(current_mother, current_mother_conf, "current") < 65.0:
        focused_mother = _extract_best_mother_name(front_img, front_tokens)
        _apply_mother_name_candidate(front_data, focused_mother)
    back_data  = parse_back(back_tokens)   # returns empty dict when back_tokens=[]
    _trace("NID_EASYOCR_FRONT_FIELDS", front_data)
    _trace("NID_EASYOCR_BACK_FIELDS", back_data)

    if has_back:
        mrz_raw = repair_td1_mrz_lines(mrz_raw, front_data, back_data)

    print("📡 Parsing MRZ (TD1)…")
    mrz_data = parse_jordan_id_mrz(mrz_raw, front_data, back_data) if mrz_raw else {}

    print(f"   L1: {mrz_data.get('mrz_l1','N/A')}")
    print(f"   L2: {mrz_data.get('mrz_l2','N/A')}")
    print(f"   L3: {mrz_data.get('mrz_l3','N/A')}")
    print(f"   Checks: {mrz_data.get('mrz_checks')}")
    _trace("NID_EASYOCR_MRZ", mrz_data)

    # MRZ is a strong validator, but the public response must not collapse to
    # empty when the back image is cropped, glossy, or partly occluded. Keep the
    # visual fields and let the hybrid layer/Ollama fill or down-rank uncertain
    # back-side fields.
    if has_back and len(mrz_raw) < 3:
        print("⚠️  Back image provided but MRZ not detected — continuing with visual/Ollama evidence")

    print("🔗 Cross-validating…")
    front_data, back_data = cross_validate_id(front_data, back_data, mrz_data)
    front_quality = _quality_with_readability(front_quality, front_tokens, front_data)
    back_quality = _quality_with_readability(back_quality, back_tokens, back_data)

    # ── Validate national number against MRZ for better confidence ───────────────
    if front_data.get("national_number") and mrz_data.get("mrz_national_id"):
        mrz_validated_conf = _validate_national_number_with_mrz(
            front_data.get("national_number"),
            mrz_data.get("mrz_national_id")
        )
        front_data["_conf_national_number"] = mrz_validated_conf
        print(f"   🔄 National number validated against MRZ: {mrz_validated_conf}")

    # ── Canonical output schema ────────────────────────────────────────────────
    # DESIGN: EasyOCR confidence scores are unreliable on document fonts.
    # - MRZ-derived fields (dob, doc_number, national_number from MRZ): returned unconditionally
    # - Purely visual fields (national_number from front scan, gender, dob from front):
    #   use conf threshold as a sanity filter, but fall back to MRZ value when available
    # - Name fields: returned as plain strings without confidence

    # ── Calculate overall accuracy percentage ────────────────────────────────────
    def _calculate_accuracy(front_data: dict, back_data: dict, mrz_data: dict) -> float:
        """Calculate overall accuracy from all field confidences."""
        confidences = []
        
        # Extract confidence from wrapped name fields if they exist
        for key in ["_conf_full_name_ar", "_conf_full_name_en", "_conf_national_number", "_conf_date_of_birth"]:
            conf = front_data.get(key)
            if isinstance(conf, (int, float)):
                confidences.append(float(conf))
        
        # Add back data confidence if available
        for key in ["_conf_document_number"]:
            conf = back_data.get(key)
            if isinstance(conf, (int, float)):
                confidences.append(float(conf))
        
        # If no specific confidences found, use default values based on data quality
        if not confidences:
            # Base accuracy on whether key fields are present
            fields_present = sum([
                bool(front_data.get("full_name_ar")),
                bool(front_data.get("full_name_en")),
                bool(front_data.get("national_number")),
                bool(front_data.get("date_of_birth")),
                bool(back_data.get("document_number")),
            ])
            # Each present field contributes 20% (5 fields × 20% = 100%)
            base_accuracy = (fields_present / 5.0) * 100
            confidences = [base_accuracy / 100]
        
        # Average all confidences and convert to percentage
        avg_conf = sum(confidences) / len(confidences) if confidences else 0.5
        return round(avg_conf * 100, 1)

    def _name_field(value, conf_key: str, src: dict):
        """Extract name value without confidence score.
        Handles both plain strings and already-wrapped dicts from cross_validate_id."""
        if isinstance(value, dict):
            # Already wrapped by _wrap_name in cross_validate_id — extract value
            inner = value.get("value")
            return inner if inner else None
        # Plain string set by parse_front
        return value if value else None

    data = {
        "national_number": normalize_field(front_data.get("national_number")) or None,
        "full_name_ar":    normalize_field(front_data.get("full_name_ar")) or None,
        "full_name_en":    normalize_field(front_data.get("full_name_en")) or None,
        "date_of_birth":   normalize_field(front_data.get("date_of_birth")) or None,
        "nationality":     "Jordanian",
        "gender":          normalize_field(front_data.get("gender")) or None,
        "place_of_birth":  normalize_field(front_data.get("place_of_birth")) or None,
        "mother_name":     normalize_field(front_data.get("mother_name") or front_data.get("mothers_name_ar")) or None,
        "blood_type":      normalize_field(back_data.get("blood_type")) or None,
        "document_number": normalize_field(back_data.get("document_number")) or None,
        "expiry_date":     normalize_field(back_data.get("expiry_date")) or None,
    }
    data = sanitize_id_data(data)
    _trace("NID_EASYOCR_FINAL_DATA", data)

    # Calculate overall accuracy from all field confidences
    accuracy = _calculate_accuracy(front_data, back_data, mrz_data)

    return {
        "success": True,
        "content": {
            "success": True,
            "accuracy_percentage": accuracy,
            "data": data,
        },
        "data":    data,
        "accuracy_percentage": accuracy,
        "mrz":     mrz_data,
        "quality": _aggregate_quality(front_quality, back_quality),
    }
