# -*- coding: utf-8 -*-
"""
Notebook-derived hybrid passport OCR engine.

Execution flow:
Image -> Preprocessing -> Orientation Correction -> OCR Token Extraction
-> MRZ Detection -> MRZ Parsing -> Visual OCR Extraction
-> Hybrid Field Resolution -> Ollama Validation & Correction
-> Confidence Scoring -> Structured JSON Response.
"""

from __future__ import annotations

from dataclasses import dataclass
from itertools import permutations
from typing import Any
import json
import re
import unicodedata
from difflib import SequenceMatcher
import os
from datetime import date as _date

import cv2
import numpy as np

from ocr_engine import (
    READER_MIX,
    READER_EN,
    MRZ_ALLOWLIST,
    ARABIC_RE,
    load_and_resize,
    build_variants,
    select_source,
    compute_quality,
    ocr_to_tokens,
    extract_mrz_from_tokens,
    preprocess_mrz_strip,
    ocr_mrz_strip_fallback,
    normalize_mrz_l1_td3,
    normalize_mrz_l2_td3,
    validate_td3,
    split_mrz_name,
    mrz_date,
    normalize_digits,
    repair_arabic_mojibake,
    strip_arabic,
    tokens_right_of,
    tokens_below,
    extract_date_for,
)


NOTEBOOK_FLOW = (
    "image",
    "preprocessing",
    "orientation_correction",
    "ocr_token_extraction",
    "mrz_detection",
    "mrz_parsing",
    "visual_ocr_extraction",
    "hybrid_field_resolution",
    "ollama_validation_correction",
    "confidence_scoring",
    "structured_json_response",
)

NOTEBOOK_FIELD_ORDER = (
    "document_type",
    "issuing_state",
    "passport_number",
    "full_name_en",
    "full_name_ar",
    "surname",
    "given_names",
    "date_of_birth",
    "date_of_expiry",
    "date_of_issue",
    "sex",
    "nationality",
    "national_number",
    "place_of_birth",
    "mother_name",
    "authority",
)

MRZ_CHECK_MAP = {
    "passport_number": "passport_no",
    "date_of_birth": "dob",
    "date_of_expiry": "expiry",
    "nationality": "composite",
    "sex": "composite",
    "national_number": "composite",
    "full_name_en": "composite",
    "surname": "composite",
    "given_names": "composite",
}


@dataclass
class FieldCandidate:
    value: Any
    source: str
    confidence: float


def _trace(label: str, payload: Any) -> None:
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


def _not_empty(value: Any) -> bool:
    return value not in (None, "", [], {}, "null", "None", "...")


def is_valid_name(value: str | None) -> bool:
    if not value or len(str(value).strip()) < 3:
        return False
    return not re.search(r"\d", str(value))


def is_valid_date(value: str | None) -> bool:
    return bool(value and re.match(r"^\d{2}/\d{2}/\d{4}$", str(value).strip()))


def is_valid_passport_no(value: str | None) -> bool:
    return bool(value and re.match(r"^[A-Z0-9]{6,9}$", str(value).strip().upper()))


def _normalize_passport_number(value: str | None) -> str | None:
    if not value:
        return None
    text = strip_arabic(str(value)).upper()
    # EasyOCR often reads a leading S as $, especially on Jordanian passports.
    text = text.replace("$", "S").replace("§", "S")
    text = text.replace(" ", "").replace("-", "")
    text = re.sub(r"[^A-Z0-9]", "", text)
    if not is_valid_passport_no(text):
        return None
    if re.fullmatch(r"\d{6}", text):
        return None
    return text


def _passport_number_compatible(left: str | None, right: str | None) -> bool:
    left_no = _normalize_passport_number(left)
    right_no = _normalize_passport_number(right)
    if not left_no or not right_no:
        return False
    if left_no == right_no:
        return True
    if abs(len(left_no) - len(right_no)) <= 2 and (left_no.endswith(right_no) or right_no.endswith(left_no)):
        return True
    return SequenceMatcher(None, left_no, right_no).ratio() >= 0.88


def is_valid_national_no(value: str | None) -> bool:
    return bool(value and re.match(r"^\d{8,14}$", str(value).strip()))


_MONTHS = {
    "JAN": 1, "JANUARY": 1,
    "FEB": 2, "FEBRUARY": 2,
    "MAR": 3, "MARCH": 3,
    "APR": 4, "APRIL": 4,
    "MAY": 5,
    "JUN": 6, "JUNE": 6,
    "JUL": 7, "JULY": 7,
    "AUG": 8, "AUGUST": 8,
    "SEP": 9, "SEPT": 9, "SEPTEMBER": 9,
    "OCT": 10, "OCTOBER": 10,
    "NOV": 11, "NOVEMBER": 11,
    "DEC": 12, "DECEMBER": 12,
}
_MONTH_OCR_ALIASES = {
    "J4": "JAN", "J4N": "JAN", "J4^": "JAN", "J^": "JAN",
    "14N": "JAN", "I4N": "JAN",
    "406": "AUG", "4UG": "AUG", "A0G": "AUG", "A06": "AUG",
}

_REAL_ARABIC_LETTER_RE = re.compile(r"[\u0621-\u064A]")
_ARABIC_SCRIPT_COUNTRY_CODES = {
    "DZA", "BHR", "COM", "DJI", "EGY", "IRQ", "JOR", "KWT",
    "LBN", "LBY", "MRT", "MAR", "OMN", "PSE", "QAT", "SAU",
    "SOM", "SDN", "SYR", "TUN", "ARE", "YEM",
}

_BAD_VISUAL_NAME_WORDS = {
    "DATE", "BIRTH", "PLACE", "SEX", "GENDER", "NATIONALITY", "NATIONAL",
    "COUNTRY", "CODE", "PASSPORT", "NUMBER", "NO", "TYPE", "ISSUE",
    "EXPIRY", "EXPIRE", "MOTHER", "AUTHORITY", "OFFICE", "KELAMIN",
    "KEWARGANEGARAAN", "TEMPAT", "LAHIR", "KANTOR", "IMIGRASI", "REG",
}


def _latin_words_only(value: str | None) -> list[str]:
    return re.findall(r"[A-Z]{2,}", strip_arabic(value or "").upper())


def _complete_internal_latin_prefixes(name: str | None) -> str | None:
    words = _latin_words_only(name)
    if not words:
        return None
    completed: list[str] = []
    for word in words:
        replacement = next(
            (
                ref for ref in words
                if ref != word
                and len(word) >= 3
                and len(word) < len(ref) <= len(word) + 4
                and ref.startswith(word)
            ),
            None,
        )
        completed.append(replacement or word)
    return re.sub(r"\s+", " ", " ".join(completed)).strip() or None


def _bad_visual_latin_name(value: str | None) -> bool:
    words = set(_latin_words_only(value))
    return bool(words and words & _BAD_VISUAL_NAME_WORDS)


def _repair_mrz_name_word(word: str) -> list[str]:
    """
    Repair a common OCR-B failure where a filler '<' is read as 'K' inside a
    long MRZ name token. This is intentionally structural, not name-specific.
    """
    clean = re.sub(r"[^A-Z]", "", (word or "").upper())
    if clean and set(clean) == {"K"}:
        return []
    if len(clean) >= 13 and clean.count("K") == 1:
        left, right = clean.split("K", 1)
        if len(left) >= 5 and len(right) >= 4:
            return [left, right]
    return [clean] if len(clean) > 1 else []


def _repair_mrz_name_part(value: str | None) -> str:
    words: list[str] = []
    for raw in re.split(r"[\s<\-]+", (value or "").upper()):
        words.extend(_repair_mrz_name_word(raw))
    return re.sub(r"\s+", " ", " ".join(words)).strip()


def _split_mrz_name_clean(name_field: str) -> tuple[str, str]:
    surname, given = split_mrz_name(name_field)
    return _repair_mrz_name_part(surname), _repair_mrz_name_part(given)

def _compact_latin_name_key(value: str | None) -> str:
    return re.sub(r"[^A-Z]", "", strip_arabic(value or "").upper())


def _display_latin_name(value: str | None) -> str | None:
    if not value:
        return None
    text = strip_arabic(value).upper().replace("-", " ")
    text = re.sub(r"[^A-Z\s]", " ", text)
    text = re.sub(r"\s+", " ", text).strip()
    return text or None


def _latin_similarity(left: str | None, right: str | None) -> float:
    left_key = _compact_latin_name_key(left)
    right_key = _compact_latin_name_key(right)
    if not left_key or not right_key:
        return 0.0
    return SequenceMatcher(None, left_key, right_key).ratio()


def _latin_name_extends(shorter: str | None, longer: str | None) -> bool:
    short_words = _display_latin_name(shorter)
    long_words = _display_latin_name(longer)
    if not short_words or not long_words:
        return False
    short_list = short_words.split()
    long_list = long_words.split()
    if len(short_list) != len(long_list):
        return False
    has_extension = False
    for short_word, long_word in zip(short_list, long_list):
        short_key = _compact_latin_name_key(short_word)
        long_key = _compact_latin_name_key(long_word)
        if short_key == long_key:
            continue
        if len(long_key) > len(short_key) >= 4 and long_key.startswith(short_key):
            has_extension = True
            continue
        return False
    return has_extension


def _repair_full_name_with_mrz(value: str | None, mrz_name: str | None) -> str | None:
    try:
        from passport_parser import _space_visual_name_with_mrz
        return _space_visual_name_with_mrz(value, mrz_name)
    except Exception:
        visual = _display_latin_name(value)
        mrz_display = _display_latin_name(mrz_name)
        if not visual:
            return mrz_display
        if not mrz_display:
            return visual
        if _compact_latin_name_key(visual) == _compact_latin_name_key(mrz_display):
            return mrz_display
        visual_key = _compact_latin_name_key(visual)
        mrz_key = _compact_latin_name_key(mrz_display)
        if mrz_key and visual_key.startswith(mrz_key) and len(visual_key) > len(mrz_key):
            return visual
        for visual_word, mrz_word in zip(visual.split(), mrz_display.split()):
            if (
                len(visual_word) > len(mrz_word) >= 4
                and _compact_latin_name_key(visual_word).startswith(_compact_latin_name_key(mrz_word))
            ):
                return visual
        if abs(len(visual.split()) - len(mrz_display.split())) <= 1 and _latin_similarity(visual, mrz_display) >= 0.90:
            return mrz_display
        return visual


def _latin_name_candidate_score(name: str | None, source: str,
                                *, visual_ref: str | None = None,
                                mrz_ref: str | None = None,
                                mrz: dict | None = None) -> float:
    display = _display_latin_name(name)
    if not display or _bad_visual_latin_name(display):
        return -100.0
    words = display.split()
    if len(words) < 2:
        return -25.0

    score = {
        "visual": 86.0,
        "ollama": 84.0,
        "mrz": 78.0,
        "current": 70.0,
    }.get(source, 60.0)
    score += min(len(words), 6) * 2.0
    score += min(len(_compact_latin_name_key(display)), 60) * 0.10

    if source == "mrz":
        checks = (mrz or {}).get("mrz_checks") or {}
        if (mrz or {}).get("name_trustworthy") is False:
            score -= 80.0
        if checks.get("composite"):
            score += 4.0
        if visual_ref:
            similarity = _latin_similarity(display, visual_ref)
            if similarity < 0.72:
                score -= 28.0
            elif similarity >= 0.90:
                score += 10.0

    if source == "ollama" and visual_ref:
        visual_similarity = _latin_similarity(display, visual_ref)
        visual_words = _display_latin_name(visual_ref).split()
        if len(visual_words) >= 3:
            if visual_similarity >= 0.88:
                score += 14.0
            elif visual_similarity < 0.68:
                score -= 34.0

    if source == "visual":
        if (mrz or {}).get("name_trustworthy") is False:
            score += 14.0
        if mrz_ref and _latin_similarity(display, mrz_ref) < 0.68 and (mrz or {}).get("name_trustworthy") is False:
            score += 8.0

    for ref in (visual_ref, mrz_ref):
        if ref and _latin_similarity(display, ref) >= 0.88:
            score += 10.0
    if mrz_ref and _latin_name_extends(mrz_ref, display):
        score += 18.0
    if visual_ref and _latin_name_extends(display, visual_ref):
        score -= 18.0

    compact = _compact_latin_name_key(display)
    if len(compact) > 42 and len(words) <= 2:
        score -= 18.0
    if any(len(word) > 24 for word in words):
        score -= 18.0
    return score


def _best_passport_latin_name(current: str | None, mrz_fields: dict,
                              visual_fields: dict, ollama_fields: dict | None,
                              mrz: dict | None) -> str | None:
    visual_name = visual_fields.get("full_name_en")
    ollama_name = (ollama_fields or {}).get("full_name_en")
    mrz_name = mrz_fields.get("full_name_en")
    focused_ollama = bool((ollama_fields or {}).get("_full_name_en_focus_used"))
    try:
        visual_conf = float(visual_fields.get("_full_name_en_conf") or 0.0)
    except Exception:
        visual_conf = 0.0
    if focused_ollama:
        focused_name = _display_latin_name(ollama_name)
        if focused_name and not _bad_visual_latin_name(focused_name) and len(focused_name.split()) >= 2:
            if visual_conf < float(os.getenv("OCR_OLLAMA_FOCUSED_NAME_CONF", "0.70")) or (mrz or {}).get("name_trustworthy") is False:
                _trace("PASSPORT_LATIN_NAME_FOCUSED_OVERRIDE", {
                    "value": focused_name,
                    "visual_name": _display_latin_name(visual_name),
                    "visual_conf": visual_conf,
                    "mrz_name_trustworthy": (mrz or {}).get("name_trustworthy"),
                })
                return focused_name
    ollama_display = _display_latin_name(ollama_name)
    if ollama_display and not _bad_visual_latin_name(ollama_display):
        ollama_words = ollama_display.split()
        if len(ollama_words) >= 2:
            for ref_name in (visual_name, mrz_name, current):
                ref_display = _display_latin_name(ref_name)
                if not ref_display:
                    continue
                ref_words = ref_display.split()
                if (
                    len(ollama_words) > len(ref_words)
                    and _latin_similarity(ollama_display, ref_display) >= 0.86
                ):
                    _trace("PASSPORT_LATIN_NAME_OLLAMA_SPACING_OVERRIDE", {
                        "value": ollama_display,
                        "replaced": ref_display,
                        "similarity": round(_latin_similarity(ollama_display, ref_display), 3),
                    })
                    return ollama_display
    candidates = [
        ("visual", visual_name),
        ("ollama", ollama_name),
        ("mrz", mrz_name),
        ("current", current),
    ]
    scored: list[tuple[float, str, str]] = []
    debug_candidates: list[dict[str, Any]] = []
    seen: set[str] = set()
    for source, value in candidates:
        display = _display_latin_name(value)
        key = _compact_latin_name_key(display)
        if not display or not key or key in seen:
            continue
        seen.add(key)
        score = _latin_name_candidate_score(
            display,
            source,
            visual_ref=visual_name,
            mrz_ref=mrz_name,
            mrz=mrz,
        )
        scored.append((score, display, source))
        debug_candidates.append({
            "source": source,
            "score": round(float(score), 2),
            "value": display,
        })
    _trace("PASSPORT_LATIN_NAME_CANDIDATES", debug_candidates)
    if not scored:
        return None
    scored.sort(key=lambda item: (item[0], len(item[1])), reverse=True)
    return scored[0][1] if scored[0][0] > 0 else None


def _mrz_name_trustworthy(line1: str | None) -> bool:
    if not line1 or len(line1) < 44 or not line1.startswith("P"):
        return False
    name_field = line1[5:44].rstrip("<")
    if not name_field or "<<" not in name_field:
        return False
    surname_raw, given_raw = name_field.split("<<", 1)
    surname_words = [w for w in surname_raw.split("<") if w]
    given_words = [w for w in given_raw.split("<") if w]
    if not surname_words or not given_words:
        return False
    words = surname_words + given_words
    if any(len(w) > 22 for w in words):
        return False
    return True


def _best_mrz_visual_name_split(full_name: str | None, line1: str | None) -> tuple[str, str]:
    display = _display_latin_name(full_name)
    if not display or not line1 or len(line1) < 10:
        return "", ""
    words = display.split()
    if len(words) < 2:
        return "", ""

    mrz_compact = _compact_latin_name_key((line1[5:44] if len(line1) >= 44 else line1))
    if not mrz_compact:
        return "", ""

    candidates: list[tuple[float, str, str]] = []
    max_surname_words = min(4, len(words) - 1)
    for count in range(1, max_surname_words + 1):
        for surname_words, given_words in (
            (words[-count:], words[:-count]),
            (words[:count], words[count:]),
        ):
            surname = " ".join(surname_words)
            given = " ".join(given_words)
            surname_key = _compact_latin_name_key(surname)
            given_key = _compact_latin_name_key(given)
            prefix = mrz_compact[: max(len(surname_key) + 2, len(surname_key))]
            suffix = mrz_compact[-max(len(given_key) + 2, len(given_key)) :]
            surname_score = SequenceMatcher(None, surname_key, prefix).ratio()
            given_score = SequenceMatcher(None, given_key, suffix).ratio() if given_key else 0.0
            score = (0.68 * surname_score) + (0.32 * given_score)
            candidates.append((score, surname, given))

    candidates.sort(reverse=True, key=lambda item: item[0])
    if candidates and candidates[0][0] >= 0.78:
        return candidates[0][1], candidates[0][2]
    return "", ""


def _split_name_parts(full_name: str | None, mrz_surname: str | None = None) -> tuple[str, str]:
    try:
        from passport_parser import _split_name_parts_from_full
        return _split_name_parts_from_full(full_name or "", mrz_surname)
    except Exception:
        words = [w for w in re.split(r"\s+", (full_name or "").replace("-", " ").strip()) if w]
        if len(words) < 2:
            return (words[0], "") if words else ("", "")
        mrz_words = [w for w in re.split(r"\s+", (mrz_surname or "").replace("-", " ").strip()) if w]
        surname_words = len(mrz_words) if 0 < len(mrz_words) <= 3 and words[-len(mrz_words):] == mrz_words else 1
        return " ".join(words[-surname_words:]), " ".join(words[:-surname_words])


def _normalize_hybrid_date(value: str | None) -> str | None:
    if not value:
        return None
    text = strip_arabic(str(value)).upper().strip()
    m = re.search(r"(\d{1,2})[/.\-](\d{1,2})[/.\-](\d{4})", text)
    if m:
        day, month, year = int(m.group(1)), int(m.group(2)), int(m.group(3))
        if 1 <= day <= 31 and 1 <= month <= 12 and 1900 <= year <= 2099:
            return f"{day:02d}/{month:02d}/{year:04d}"
    m = re.search(r"(\d{1,2})\s*[/.\-\s]\s*([A-Z]{3,9})\s*[/.\-\s]\s*(\d{4})", text)
    if m:
        day = int(m.group(1))
        month = _MONTHS.get(m.group(2).replace("0", "O"))
        year = int(m.group(3))
        if month and 1 <= day <= 31 and 1900 <= year <= 2099:
            return f"{day:02d}/{month:02d}/{year:04d}"
    return None


def _normalize_hybrid_gender(value: str | None) -> str | None:
    text = str(value or "").strip().upper()
    if text in {"M", "MALE", "MALE/ذكر", "ذكر"}:
        return "Male/ذكر"
    if text in {"F", "FEMALE", "FEMALE/انثى", "FEMALE/أنثى", "انثى", "أنثى"}:
        return "Female/انثى"
    return None if text in {"", "<", "UNSPECIFIED", "NONE", "NULL"} else str(value).strip()


def _clean_place_of_birth(value: str | None) -> str | None:
    if not value:
        return None
    text = re.sub(r"\s+", " ", str(value)).strip()
    latin = re.sub(r"[^A-Za-z\s'-]", " ", strip_arabic(text)).strip()
    latin = re.sub(r"\s+", " ", latin).strip()
    return latin.upper() if latin else text


def _normalize_hybrid_arabic_name(value: str | None) -> str | None:
    if not value:
        return None
    text = repair_arabic_mojibake(str(value))
    if not _REAL_ARABIC_LETTER_RE.search(text):
        return None
    text = unicodedata.normalize("NFKC", text)
    text = re.sub(r"[\u064B-\u065F\u0670\u0640]", "", text)
    text = re.sub(r"[\u0623\u0625\u0622\u0671]", "\u0627", text)
    text = text.replace("\u0649", "\u064A")
    text = re.sub(r"[A-Za-z0-9_]+", " ", text)
    text = re.sub(r"[\u0660-\u0669\u06F0-\u06F9]", " ", text)
    text = re.sub(r"[^\u0621-\u064A\s]", " ", text)
    words = [w for w in re.split(r"\s+", text.strip()) if len(w) >= 2]
    if sum(len(w) for w in words) < 4:
        return None
    if len(words) < 2:
        return None
    text = re.sub(r"\s+", " ", " ".join(words)).strip()
    return _repair_common_arabic_name_ocr(text)


def _repair_common_arabic_name_ocr(value: str | None) -> str | None:
    """Normalize Arabic OCR output without injecting fixed personal names."""
    if not value:
        return None
    text = re.sub(r"\s+", " ", str(value)).strip()
    return re.sub(r"\s+", " ", text).strip() or None


def _latin_name_to_arabic_fallback(name_en: str | None) -> str | None:
    try:
        from national_id_hybrid_engine import _latin_name_to_arabic_phonetic
        return _latin_name_to_arabic_phonetic(name_en)
    except Exception:
        return None


def _repair_arabic_name_with_fallback(current_ar: str | None, fallback_ar: str | None) -> str | None:
    try:
        from national_id_hybrid_engine import _repair_arabic_name_with_latin_fallback
        return _repair_arabic_name_with_latin_fallback(current_ar, fallback_ar)
    except Exception:
        return current_ar


def _arabic_fallback_similarity(left: str | None, right: str | None) -> float:
    try:
        from national_id_hybrid_engine import _arabic_similarity
        return float(_arabic_similarity(left, right))
    except Exception:
        left_key = re.sub(r"\s+", "", _normalize_hybrid_arabic_name(left) or "")
        right_key = re.sub(r"\s+", "", _normalize_hybrid_arabic_name(right) or "")
        if not left_key or not right_key:
            return 0.0
        return SequenceMatcher(None, left_key, right_key).ratio()


def _arabic_latin_alignment(name_ar: str | None, name_en: str | None) -> float:
    fallback = _normalize_hybrid_arabic_name(_latin_name_to_arabic_fallback(name_en))
    if not fallback:
        return 0.0
    return _arabic_fallback_similarity(name_ar, fallback)


def _compact_arabic_len(value: str | None) -> int:
    return len(re.sub(r"\s+", "", _normalize_hybrid_arabic_name(value) or ""))


def _arabic_name_confidence(value: str | None, name_en: str | None, source: str = "") -> float:
    cleaned = _normalize_hybrid_arabic_name(value)
    if not cleaned:
        return 0.0
    words = cleaned.split()
    latin_count = len(_latin_words_only(name_en))
    alignment = _arabic_latin_alignment(cleaned, name_en)
    base = 0.34 + min(len(words), 5) * 0.055 + min(_compact_arabic_len(cleaned), 32) / 32.0 * 0.12
    if alignment:
        base = max(base, 0.26 + alignment * 0.66)
        fallback = _normalize_hybrid_arabic_name(_latin_name_to_arabic_fallback(name_en))
        length_ratio = _compact_arabic_len(cleaned) / max(_compact_arabic_len(fallback), 1)
        if latin_count >= 3 and alignment < 0.68:
            base = min(base, 0.54)
        elif latin_count >= 3 and alignment < 0.78:
            base = min(base, 0.74)
        if fallback and length_ratio < 0.92:
            base = min(base, 0.82)
    if source.startswith("ollama"):
        base += 0.03
    elif source == "dynamic_phonetic":
        base = min(max(base, 0.72), 0.90)
    return round(max(0.0, min(base, 0.96)), 3)


def _basic_arabic_name_candidate(value: str | None, name_en: str | None) -> str | None:
    cleaned = _normalize_hybrid_arabic_name(value)
    if not cleaned or re.search(r"[A-Za-z0-9]", cleaned):
        return None
    words = [w for w in cleaned.split() if re.search(r"[\u0600-\u06FF]", w)]
    if len(words) < 2:
        return None
    latin_count = len(_latin_words_only(name_en))
    if latin_count >= 4 and len(words) < 3:
        return None
    label_like = {
        "الاسم", "الجنس", "ذكر", "انثى", "تاريخ", "مكان",
        "الميلاد", "الولاده", "الجواز", "رقم", "الرقم",
    }
    normalized_labels = {
        re.sub(r"[^\u0621-\u064A]", "", item.replace("أ", "ا").replace("إ", "ا").replace("آ", "ا"))
        for item in label_like
    }
    normalized_words = [
        re.sub(r"[^\u0621-\u064A]", "", word.replace("أ", "ا").replace("إ", "ا").replace("آ", "ا"))
        for word in words
    ]
    if normalized_words and all(word in normalized_labels for word in normalized_words):
        return None
    if _compact_arabic_len(cleaned) < max(6, len(words) * 2):
        return None
    return cleaned


def _arabic_letters_key(value: str | None) -> str:
    text = repair_arabic_mojibake(value or "")
    text = unicodedata.normalize("NFKC", text)
    text = re.sub(r"[\u064B-\u065F\u0670\u0640]", "", text)
    text = re.sub(r"[\u0623\u0625\u0622\u0671]", "\u0627", text).replace("\u0649", "\u064A")
    return re.sub(r"[^\u0621-\u064A]", "", text)


def _arabic_order_word_similarity(left: str | None, right: str | None) -> float:
    left_key = _arabic_letters_key(left)
    right_key = _arabic_letters_key(right)
    if not left_key or not right_key:
        return 0.0
    left_forms = {left_key}
    right_forms = {right_key}
    if left_key.startswith("\u0627\u0644") and len(left_key) > 3:
        left_forms.add(left_key[2:])
    if right_key.startswith("\u0627\u0644") and len(right_key) > 3:
        right_forms.add(right_key[2:])
    return max(SequenceMatcher(None, a, b).ratio() for a in left_forms for b in right_forms)


def _partition_sequence(items: list[str], parts: int):
    if parts <= 0 or len(items) < parts:
        return
    if parts == 1:
        yield [items]
        return
    max_cut = len(items) - parts + 1
    for cut in range(1, max_cut + 1):
        for rest in _partition_sequence(items[cut:], parts - 1):
            yield [items[:cut], *rest]


def _arabic_order_sequence_score(words: tuple[str, ...], reference_words: list[str]) -> float:
    if not words or not reference_words or len(words) > len(reference_words):
        return 0.0
    best = 0.0
    for groups in _partition_sequence(reference_words, len(words)) or []:
        references = [" ".join(group) for group in groups]
        scores = [
            _arabic_order_word_similarity(word, reference)
            for word, reference in zip(words, references)
        ]
        if not scores:
            continue
        edge_bonus = (
            _arabic_order_word_similarity(words[0], references[0])
            + _arabic_order_word_similarity(words[-1], references[-1])
        ) * 0.04
        best = max(best, (sum(scores) / len(scores)) + edge_bonus)
    return best


def _reorder_arabic_name_by_latin(value: str | None, name_en: str | None,
                                  source: str = "") -> str | None:
    """
    Correct Arabic word order dynamically when OCR/Ollama reads an RTL row in a
    left-to-right order. This preserves the Arabic OCR words; it only reorders
    them using the Latin printed name's phonetic sequence as a weak guide.
    """
    cleaned = _normalize_hybrid_arabic_name(value)
    fallback = _normalize_hybrid_arabic_name(_latin_name_to_arabic_fallback(name_en))
    if not cleaned or not fallback:
        return cleaned

    words = cleaned.split()
    reference_words = fallback.split()
    if len(words) < 3 or len(words) > 7 or len(reference_words) < 3:
        return cleaned
    if len(words) > len(reference_words) or len(reference_words) - len(words) > 2:
        return cleaned

    original = tuple(words)
    original_score = _arabic_order_sequence_score(original, reference_words)
    best_order = original
    best_score = original_score

    # Names are short; a bounded permutation search is safe and avoids any
    # country/name-specific dictionary. De-duplicate repeated name parts.
    for candidate in set(permutations(words)):
        score = _arabic_order_sequence_score(candidate, reference_words)
        if score > best_score:
            best_score = score
            best_order = candidate

    if best_order != original and best_score >= 0.50 and (best_score - original_score) >= 0.14:
        repaired = _normalize_hybrid_arabic_name(" ".join(best_order))
        if repaired:
            _trace("PASSPORT_ARABIC_NAME_ORDER_REPAIR", {
                "source": source,
                "before": cleaned,
                "after": repaired,
                "original_score": round(original_score, 3),
                "reordered_score": round(best_score, 3),
            })
            return repaired
    return cleaned


def _basic_arabic_name_candidate(value: str | None, name_en: str | None) -> str | None:
    cleaned = _normalize_hybrid_arabic_name(value)
    if not cleaned or re.search(r"[A-Za-z0-9]", cleaned):
        return None
    words = [w for w in cleaned.split() if _REAL_ARABIC_LETTER_RE.search(w)]
    if len(words) < 2:
        return None
    latin_count = len(_latin_words_only(name_en))
    if latin_count >= 4 and len(words) < 3:
        return None

    label_like = {
        "\u0627\u0644\u0627\u0633\u0645", "\u0627\u0644\u062c\u0646\u0633",
        "\u0630\u0643\u0631", "\u0627\u0646\u062b\u0649",
        "\u062a\u0627\u0631\u064a\u062e", "\u0645\u0643\u0627\u0646",
        "\u0627\u0644\u0645\u064a\u0644\u0627\u062f", "\u0627\u0644\u0648\u0644\u0627\u062f\u0647",
        "\u0627\u0644\u062c\u0648\u0627\u0632", "\u0631\u0642\u0645",
        "\u0627\u0644\u0631\u0642\u0645", "\u0627\u0644\u062c\u0646\u0633\u064a\u0647",
    }
    normalized_labels = {_arabic_letters_key(item) for item in label_like}
    normalized_words = [_arabic_letters_key(word) for word in words]
    label_hits = sum(1 for word in normalized_words if word in normalized_labels)
    if label_hits and label_hits >= max(1, len(normalized_words) // 2):
        return None
    if _compact_arabic_len(cleaned) < max(6, len(words) * 2):
        return None
    return cleaned


def _passport_arabic_candidate_score(value: str | None, name_en: str | None, source: str) -> float:
    cleaned = _basic_arabic_name_candidate(value, name_en)
    if not cleaned:
        return -100.0
    words = cleaned.split()
    latin_words = _latin_words_only(name_en)
    score = len(words) * 12.0 + min(_compact_arabic_len(cleaned), 60) * 0.75
    if source == "visual":
        score += 8.0
    elif source == "ollama":
        score += 6.0
    elif source == "current":
        score += 2.0

    fallback = _normalize_hybrid_arabic_name(_latin_name_to_arabic_fallback(name_en))
    if fallback:
        alignment = _arabic_fallback_similarity(cleaned, fallback)
        ratio = _compact_arabic_len(cleaned) / max(_compact_arabic_len(fallback), 1)
        score += alignment * 95.0
        if len(latin_words) >= 4 and alignment < 0.58:
            return -100.0
        if len(latin_words) >= 4 and source in {"visual", "current"} and alignment < 0.66:
            score -= 45.0
        if len(latin_words) >= 3 and alignment < 0.60:
            score -= 60.0
        elif len(latin_words) >= 3 and alignment < 0.70:
            score -= 24.0
        if ratio < 0.55 or ratio > 1.65:
            score -= 35.0
        if len(latin_words) >= 4 and len(words) < max(3, len(latin_words) - 2):
            score -= 35.0
    if any(len(word) <= 1 for word in words):
        score -= 25.0
    return score


def _select_passport_arabic_name(data: dict, visual_fields: dict,
                                 ollama_fields: dict | None) -> tuple[str | None, str | None]:
    name_en = data.get("full_name_en")
    focused_ollama = bool((ollama_fields or {}).get("_full_name_ar_focus_used"))
    focused_value = _basic_arabic_name_candidate((ollama_fields or {}).get("full_name_ar"), name_en)
    if focused_ollama and focused_value:
        ordered_focus = _reorder_arabic_name_by_latin(focused_value, name_en, "ollama_focus")
        if ordered_focus:
            focused_value = ordered_focus
        _trace("PASSPORT_ARABIC_NAME_OLLAMA_FOCUS_SELECTED", {
            "value": focused_value,
            "reason": "focused_ollama_arabic_candidate",
        })
        return focused_value, "ollama_focus"

    ollama_value = _basic_arabic_name_candidate((ollama_fields or {}).get("full_name_ar"), name_en)
    if ollama_value:
        ordered_ollama = _reorder_arabic_name_by_latin(ollama_value, name_en, "ollama")
        if ordered_ollama:
            ollama_value = ordered_ollama
        _trace("PASSPORT_ARABIC_NAME_OLLAMA_SELECTED", {"value": ollama_value})
        return ollama_value, "ollama"

    candidates = [
        ("visual", visual_fields.get("full_name_ar")),
        ("current", data.get("full_name_ar")),
    ]
    scored: list[tuple[float, int, str, str]] = []
    priority = {"visual": 2, "ollama": 1, "current": 0}
    seen: set[str] = set()
    for source, value in candidates:
        cleaned = _basic_arabic_name_candidate(value, name_en)
        if not cleaned:
            continue
        ordered = _reorder_arabic_name_by_latin(cleaned, name_en, source)
        if ordered:
            cleaned = ordered
        key = re.sub(r"\s+", "", cleaned)
        if key in seen:
            continue
        seen.add(key)
        score = _passport_arabic_candidate_score(cleaned, name_en, source)
        scored.append((score, priority.get(source, 0), source, cleaned))

    _trace("PASSPORT_ARABIC_NAME_CANDIDATES", [
        {"source": source, "score": round(score, 2), "value": value}
        for score, _, source, value in scored
    ])

    if scored:
        scored.sort(key=lambda item: (item[0], item[1], len(item[3])), reverse=True)
        best_score, _, best_source, best_value = scored[0]
        if best_score >= 55.0:
            return best_value, best_source
    if os.getenv("OCR_PASSPORT_ALLOW_ARABIC_TRANSLITERATION", "").strip().lower() in ("1", "true", "yes", "on"):
        fallback = _normalize_hybrid_arabic_name(_latin_name_to_arabic_fallback(name_en))
        if fallback:
            return fallback, "dynamic_phonetic"
    return None, None


def _should_use_arabic_name_fallback(current_ar: str | None, fallback_ar: str | None,
                                     name_en: str | None) -> bool:
    if not fallback_ar:
        return False
    if not current_ar:
        return True
    current_words = current_ar.split()
    fallback_words = fallback_ar.split()
    latin_words = _latin_words_only(name_en)
    if len(latin_words) >= 4 and len(current_words) < max(3, len(latin_words) - 1):
        return True
    alignment = _arabic_fallback_similarity(current_ar, fallback_ar)
    current_len = _compact_arabic_len(current_ar)
    fallback_len = _compact_arabic_len(fallback_ar)
    if len(latin_words) >= 3 and alignment and alignment < 0.68:
        return True
    if alignment >= 0.74 and fallback_len > current_len + 1:
        return True
    suspicious_current = any(len(w) <= 1 or len(w) > 10 for w in current_words)
    if not suspicious_current:
        return False
    if (
        len(latin_words) >= 3
        and len(fallback_words) >= max(2, len(current_words) - 1)
        and _arabic_fallback_similarity(current_ar, fallback_ar) < 0.58
    ):
        return True
    return False


def _has_arabic_script_passport_context(data: dict, mrz_fields: dict) -> bool:
    for key in ("issuing_state", "nationality"):
        value = str(data.get(key) or mrz_fields.get(key) or "").strip().upper()
        if value in _ARABIC_SCRIPT_COUNTRY_CODES:
            return True
    return False


def _normalize_hybrid_data(data: dict, mrz_fields: dict, visual_fields: dict,
                           ollama_fields: dict | None = None,
                           mrz: dict | None = None) -> None:
    mrz_name = mrz_fields.get("full_name_en")
    best_name = _best_passport_latin_name(data.get("full_name_en"), mrz_fields, visual_fields, ollama_fields, mrz)
    if best_name and _compact_latin_name_key(best_name) != _compact_latin_name_key(mrz_name):
        fixed_name = best_name
    else:
        fixed_name = _repair_full_name_with_mrz(best_name or data.get("full_name_en"), mrz_name)
    if fixed_name:
        split_hint = mrz_fields.get("surname")
        if mrz_name and _latin_similarity(fixed_name, mrz_name) < 0.90:
            split_hint = None
        data["full_name_en"] = fixed_name
        if (
            data.get("surname")
            and data.get("given_names")
            and _compact_latin_name_key(f"{data.get('given_names')} {data.get('surname')}")
               == _compact_latin_name_key(fixed_name)
        ):
            surname, given = data.get("surname") or "", data.get("given_names") or ""
        else:
            surname, given = _split_name_parts(fixed_name, split_hint)
        if surname:
            data["surname"] = surname
        if given:
            data["given_names"] = given

    if data.get("full_name_en"):
        completed_name = _complete_internal_latin_prefixes(data.get("full_name_en"))
        if completed_name:
            data["full_name_en"] = completed_name
            split_hint = mrz_fields.get("surname")
            if mrz_name and _latin_similarity(completed_name, mrz_name) < 0.90:
                split_hint = None
            surname, given = _split_name_parts(completed_name, split_hint)
            if surname:
                data["surname"] = surname
            if given:
                data["given_names"] = given

    for key in ("date_of_birth", "date_of_expiry", "date_of_issue"):
        normalized = _normalize_hybrid_date(data.get(key))
        if normalized:
            data[key] = normalized

    gender = _normalize_hybrid_gender(data.get("sex")) or _normalize_hybrid_gender(mrz_fields.get("sex"))
    if gender:
        data["sex"] = gender

    if data.get("national_number"):
        digits = re.sub(r"\D", "", str(data.get("national_number")))
        if len(digits) == 10:
            data["national_number"] = digits
        else:
            data.pop("national_number", None)

    if data.get("passport_number"):
        passport_number = _normalize_passport_number(str(data.get("passport_number")))
        if is_valid_passport_no(passport_number):
            data["passport_number"] = passport_number
        else:
            data.pop("passport_number", None)

    place = _clean_place_of_birth(data.get("place_of_birth"))
    if place:
        data["place_of_birth"] = place

    if data.get("full_name_ar"):
        cleaned_ar = _normalize_hybrid_arabic_name(data.get("full_name_ar"))
        if cleaned_ar:
            data["full_name_ar"] = cleaned_ar
        else:
            data.pop("full_name_ar", None)

    if _has_arabic_script_passport_context(data, mrz_fields):
        selected_ar, source_ar = _select_passport_arabic_name(data, visual_fields, ollama_fields)
        if selected_ar:
            data["full_name_ar"] = selected_ar
            data["_source_full_name_ar"] = source_ar
        else:
            data.pop("full_name_ar", None)


VISUAL_VALIDATORS = {
    "passport_number": is_valid_passport_no,
    "national_number": is_valid_national_no,
    "date_of_birth": is_valid_date,
    "date_of_expiry": is_valid_date,
    "date_of_issue": is_valid_date,
    "full_name_en": is_valid_name,
}


def score_candidate(value: Any, source: str, field: str, mrz_valid: dict, quality: dict) -> float:
    """Notebook confidence model: MRZ check digits > visual/Ollama heuristics."""
    if not _not_empty(value):
        return 0.0

    blur = (quality or {}).get("blur", 999)
    usable = (quality or {}).get("usable", True)

    if source == "mrz":
        if field in ("full_name_en", "surname", "given_names") and is_valid_name(str(value)):
            return 0.72
        if field == "national_number" and not is_valid_national_no(str(value)):
            return 0.0
        check_key = MRZ_CHECK_MAP.get(field)
        if check_key and (mrz_valid or {}).get(check_key):
            if field == "passport_number":
                return 0.80
            return 0.97
        if field == "passport_number" and _normalize_passport_number(str(value)):
            return 0.72
        if check_key:
            return 0.20
        return 0.10

    if source == "ollama":
        score = 0.90 if field in ("full_name_en", "full_name_ar") else (
            0.82 if field in ("place_of_birth", "mother_name", "authority", "nationality") else 0.75
        )
        if field == "national_number" and not is_valid_national_no(str(value)):
            score *= 0.15
        if field == "passport_number" and not _normalize_passport_number(str(value)):
            score *= 0.20
        elif field == "passport_number":
            score = max(score, 0.90)
        if not usable:
            score *= 0.70
        elif blur < 100:
            score *= 0.80
        return min(score, 1.0)

    if source == "visual":
        if field == "full_name_en" and _bad_visual_latin_name(str(value)):
            return 0.02
        score = 0.72 if field in ("national_number", "passport_number") else 0.60
        if field in ("date_of_birth", "date_of_expiry", "date_of_issue"):
            score = 0.65
        if blur < 50:
            score *= 0.50
        elif blur < 150:
            score *= 0.75
        validator = VISUAL_VALIDATORS.get(field)
        if validator:
            score *= 1.20 if validator(str(value)) else 0.15
        if field == "passport_number" and _normalize_passport_number(str(value)):
            score = max(score, 0.88)
        return min(score, 1.0)

    return 0.0


def arbitrate(field: str, mrz_value: Any, mrz_valid: dict,
              visual_value: Any, ollama_value: Any, quality: dict) -> tuple[Any, str, float]:
    if field == "passport_number":
        visual_no = _normalize_passport_number(str(visual_value)) if _not_empty(visual_value) else None
        ollama_no = _normalize_passport_number(str(ollama_value)) if _not_empty(ollama_value) else None
        if ollama_no and visual_no and _passport_number_compatible(visual_no, ollama_no):
            return ollama_no, "ollama", 0.94

    candidates = [
        FieldCandidate(mrz_value, "mrz", score_candidate(mrz_value, "mrz", field, mrz_valid, quality)),
        FieldCandidate(visual_value, "visual", score_candidate(visual_value, "visual", field, mrz_valid, quality)),
        FieldCandidate(ollama_value, "ollama", score_candidate(ollama_value, "ollama", field, mrz_valid, quality)),
    ]
    candidates = [c for c in candidates if _not_empty(c.value)]
    if not candidates:
        return None, "none", 0.0
    priority = {"mrz": 0, "visual": 1, "ollama": 2}
    candidates.sort(key=lambda c: (-c.confidence, priority.get(c.source, 9)))
    winner = candidates[0]
    return winner.value, winner.source, winner.confidence


def correct_orientation(img: np.ndarray) -> tuple[np.ndarray, dict]:
    """
    Orientation handling for the notebook pipeline.

    The notebook assumes upright input; this keeps that behavior for normal
    images and only rotates when MRZ-compatible OCR evidence is stronger.
    """
    if os.getenv("OCR_TRY_ROTATIONS", "").strip().lower() not in ("1", "true", "yes", "on"):
        return img, {"angle": 0, "score": 0.0, "method": "upright_fast_path"}

    best_img = img
    best_meta = {"angle": 0, "score": 0.0, "method": "mrz_token_score"}
    rotations = {
        0: img,
        90: cv2.rotate(img, cv2.ROTATE_90_CLOCKWISE),
        180: cv2.rotate(img, cv2.ROTATE_180),
        270: cv2.rotate(img, cv2.ROTATE_90_COUNTERCLOCKWISE),
    }
    for angle, candidate in rotations.items():
        try:
            variants = build_variants(candidate)
            src = select_source(variants, mode="mrz")
            tokens = ocr_to_tokens(src, reader=READER_EN, allowlist=MRZ_ALLOWLIST, min_conf=0.01)
            lines = extract_mrz_from_tokens(tokens, src.shape[0], doc_type="passport")
            score = sum(len((t.get("text") or "").replace(" ", "")) for t in lines[:2])
            score += sum(float(t.get("conf", 0.0)) for t in lines[:2])
        except Exception:
            score = 0.0
        if score > best_meta["score"]:
            best_img = candidate
            best_meta = {"angle": angle, "score": round(float(score), 3), "method": "mrz_token_score"}
    return best_img, best_meta


def _crop_passport_data_page(img: np.ndarray) -> tuple[np.ndarray, dict]:
    h, w = img.shape[:2]
    if h <= 0 or w <= 0 or h / max(w, 1) < 1.12:
        return img, {"cropped": False, "y0": 0, "method": "landscape_or_near_square"}

    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    _, binary = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
    dark_density = np.mean(binary == 0, axis=1)
    start_search = int(h * 0.42)
    rows = np.where(dark_density[start_search:] > 0.018)[0]
    if len(rows) == 0:
        return img, {"cropped": False, "y0": 0, "method": "no_lower_text_band"}

    y0 = max(0, start_search + int(rows[0]) - int(h * 0.06))
    if y0 < h * 0.20:
        return img, {"cropped": False, "y0": 0, "method": "text_starts_high"}
    return img[y0:h, :], {"cropped": True, "y0": y0, "method": "lower_text_density"}


def extract_mrz(img: np.ndarray, variants: dict, tokens: list[dict],
                token_img_h: int | None = None) -> tuple[dict, dict]:
    h = token_img_h or img.shape[0]
    mrz_tokens = extract_mrz_from_tokens(tokens, h, doc_type="passport")
    if len(mrz_tokens) < 2:
        strip = preprocess_mrz_strip(img, doc_type="passport")
        mrz_tokens = ocr_mrz_strip_fallback(strip, lines_count=2)

    if len(mrz_tokens) < 2:
        return {}, {
            "mrz_valid": False,
            "mrz_checks": {},
            "confidence": 0.0,
            "error": "MRZ lines not detected",
        }

    line1 = normalize_mrz_l1_td3(mrz_tokens[0].get("text", ""))
    line2 = normalize_mrz_l2_td3(mrz_tokens[1].get("text", ""))
    checks = validate_td3(line2)
    name_ok = _mrz_name_trustworthy(line1)
    surname, given = _split_mrz_name_clean(line1[5:44]) if name_ok else ("", "")
    token_conf = round(sum(float(t.get("conf", 0)) for t in mrz_tokens[:2]) / 2, 3)
    check_ratio = sum(1 for v in checks.values() if v is True) / 4.0 if checks else 0.0
    confidence = round(max(token_conf, 0.82) * (0.65 + 0.35 * check_ratio), 3)

    mrz_fields = {
        "document_type": "Passport",
        "issuing_state": line1[2:5].replace("<", "") or None,
        "passport_number": line2[0:9].rstrip("<"),
        "date_of_birth": mrz_date(line2[13:19], kind="birth"),
        "date_of_expiry": mrz_date(line2[21:27], kind="expiry"),
        "sex": line2[20].replace("<", "") or None,
        "nationality": line2[10:13].replace("<", "") or None,
        "national_number": line2[28:42].replace("<", "") or None,
    }
    if name_ok:
        mrz_fields.update({
            "surname": surname,
            "given_names": given,
            "full_name_en": f"{given} {surname}".strip() if given else surname,
        })
    return {k: v for k, v in mrz_fields.items() if v}, {
        "mrz_valid": all(v is True for v in checks.values()) if checks else False,
        "mrz_checks": checks,
        "mrz_token_conf": token_conf,
        "confidence": confidence,
        "line1": line1,
        "line2": line2,
        "name_trustworthy": name_ok,
        "tokens": mrz_tokens[:2],
    }


def _collect_visual_tokens(variants: dict) -> list[dict]:
    raw_sources = [
        select_source(variants, mode="text"),
    ]
    if os.getenv("OCR_EXTRA_VISUAL_VARIANTS", "").strip().lower() in ("1", "true", "yes", "on"):
        raw_sources.extend([
            select_source(variants, mode="arabic"),
            variants.get("sharpened"),
        ])
    sources = []
    seen_ids = set()
    for src in raw_sources:
        if src is None or id(src) in seen_ids:
            continue
        seen_ids.add(id(src))
        sources.append(src)
    deduped: dict[tuple[str, int, int], dict] = {}
    for src in sources:
        try:
            tokens = ocr_to_tokens(src, reader=READER_MIX)
        except Exception:
            continue
        for tok in tokens:
            text = (tok.get("text") or "").strip()
            if not text:
                continue
            key = (text.lower(), int(tok.get("cx", tok.get("x", 0))) // 12,
                   int(tok.get("cy", tok.get("y", 0))) // 12)
            if key not in deduped or deduped[key].get("conf", 0) < tok.get("conf", 0):
                deduped[key] = tok
    result = list(deduped.values())
    result.sort(key=lambda t: (t.get("y", 0) // 30, t.get("x", 0)))
    return result


def _passport_parser_helper(name: str):
    try:
        import passport_parser
        return getattr(passport_parser, name, None)
    except Exception:
        return None


def _parse_visual_date(text: str | None) -> str | None:
    parser = _passport_parser_helper("_parse_passport_date")
    if parser:
        try:
            parsed = parser(text or "")
            if parsed:
                return parsed
        except Exception:
            pass
    parsed = _normalize_hybrid_date(text)
    if parsed:
        return parsed
    clean = strip_arabic(str(text or "")).upper()
    clean = re.sub(r"[^A-Z0-9/^.\-\s]", " ", clean)
    clean = re.sub(r"\s+", " ", clean).strip()
    m = re.search(r"(\d{1,2})\s*[/.\-\s]\s*([A-Z0-9^]{2,5})\s*[/.\-\s]*\s*(\d{4})", clean)
    if not m:
        return None
    day = int(m.group(1))
    month_raw = m.group(2).strip()
    month_key = _MONTH_OCR_ALIASES.get(month_raw)
    if not month_key:
        month_key = month_raw.replace("0", "O").replace("4", "A").replace("^", "N")
    month = _MONTHS.get(month_key)
    year = int(m.group(3))
    if month and 1 <= day <= 31 and 1900 <= year <= 2099:
        return f"{day:02d}/{month:02d}/{year:04d}"
    return None


def _is_visual_date_label(text: str | None, kind: str) -> bool:
    raw = unicodedata.normalize("NFKC", str(text or ""))
    latin = re.sub(r"[^A-Z\s]", " ", strip_arabic(raw).upper())
    latin = re.sub(r"\s+", " ", latin).strip()
    arabic = re.sub(r"[\u064B-\u065F\u0670\u0640]", "", raw)
    arabic = re.sub(r"[\u0623\u0625\u0622\u0671]", "\u0627", arabic).replace("\u0649", "\u064A")

    if kind == "birth":
        if any(w in latin for w in ("PLACE", "TEMPAT")) or "\u0645\u0643\u0627\u0646" in arabic:
            return False
        return (
            "BIRTH" in latin or "BIRTI" in latin or "BINIH" in latin
            or "BINI" in latin or "DOB" in latin or "LAHIR" in latin
            or "\u0645\u064A\u0644\u0627\u062F" in arabic
            or "\u0648\u0644\u0627\u062F" in arabic
        )
    if kind == "issue":
        if any(w in latin for w in ("EXPIR", "BIRTH", "LAHIR", "HABIS", "BERLAKU")):
            return False
        return (
            "ISSUE" in latin or "ISSUED" in latin or "PENGELUARAN" in latin
            or "\u0627\u0635\u062F\u0627\u0631" in arabic
        )
    if any(w in latin for w in ("BIRTH", "LAHIR", "ISSUE", "PENGELUARAN")):
        return False
    return (
        "EXPIR" in latin or "EXPIRE" in latin or "HABIS" in latin or "BERLAKU" in latin
        or "\u0627\u0646\u062A\u0647\u0627\u0621" in arabic
        or "\u0635\u0644\u0627\u062D\u064A\u0629" in arabic
    )


def _extract_visual_date_by_kind(tokens: list[dict], kind: str) -> tuple[str | None, float]:
    anchors = [t for t in tokens if _is_visual_date_label(t.get("text"), kind)]
    anchors.sort(key=lambda t: (t.get("y", 0), t.get("x", 0)))
    for anchor in anchors:
        direct = _parse_visual_date(anchor.get("text"))
        if direct:
            return direct, round(float(anchor.get("conf", 0.0)), 3)

        ax0 = float(anchor.get("x", 0))
        ax1 = float(anchor.get("x2", ax0))
        ay = float(anchor.get("cy", anchor.get("y", 0)))
        ay2 = float(anchor.get("y2", anchor.get("y", 0)))
        nearby = [
            t for t in tokens
            if t is not anchor
            and not _bad_visual_latin_name(t.get("text"))
            and (
                abs(float(t.get("cy", 0)) - ay) <= 45
                or (0 < float(t.get("y", 0)) - ay2 <= 115)
            )
            and float(t.get("x2", 0)) >= max(0.0, ax0 - 120.0)
            and float(t.get("x", 0)) <= ax1 + 760.0
        ]
        nearby.sort(key=lambda t: (abs(float(t.get("cy", 0)) - ay), float(t.get("x", 0))))
        for candidate in nearby:
            parsed = _parse_visual_date(candidate.get("text"))
            if parsed:
                return parsed, round(float(candidate.get("conf", 0.0)), 3)

        row_groups: dict[int, list[dict]] = {}
        for candidate in nearby:
            bucket = int(round(float(candidate.get("cy", 0)) / 28.0)) * 28
            row_groups.setdefault(bucket, []).append(candidate)
        for row in row_groups.values():
            merged = " ".join(t.get("text", "") for t in sorted(row, key=lambda t: t.get("x", 0)))
            parsed = _parse_visual_date(merged)
            if parsed:
                confs = [float(t.get("conf", 0.0)) for t in row]
                return parsed, round(sum(confs) / max(len(confs), 1), 3)
    return None, 0.0


def _date_year(value: str | None) -> int | None:
    try:
        return int(str(value or "").split("/")[-1])
    except Exception:
        return None


def _extract_all_visual_dates(tokens: list[dict]) -> list[tuple[float, str, float]]:
    candidates: list[tuple[float, str, float]] = []
    seen: set[tuple[str, int]] = set()

    def add_candidate(y: float, text: str, conf: float) -> None:
        parsed = _parse_visual_date(text)
        if not parsed:
            return
        key = (parsed, int(round(y / 20.0)) * 20)
        if key in seen:
            return
        seen.add(key)
        candidates.append((y, parsed, conf))

    for tok in tokens:
        add_candidate(float(tok.get("cy", tok.get("y", 0))), tok.get("text", ""), float(tok.get("conf", 0.0)))

    row_groups: dict[int, list[dict]] = {}
    for tok in tokens:
        bucket = int(round(float(tok.get("cy", 0)) / 28.0)) * 28
        row_groups.setdefault(bucket, []).append(tok)
    for row in row_groups.values():
        merged = " ".join(t.get("text", "") for t in sorted(row, key=lambda t: t.get("x", 0)))
        confs = [float(t.get("conf", 0.0)) for t in row]
        add_candidate(
            sum(float(t.get("cy", 0)) for t in row) / max(len(row), 1),
            merged,
            sum(confs) / max(len(confs), 1),
        )

    candidates.sort(key=lambda item: item[0])
    unique: list[tuple[float, str, float]] = []
    seen_dates: set[str] = set()
    for item in candidates:
        if item[1] in seen_dates:
            continue
        seen_dates.add(item[1])
        unique.append(item)
    return unique


def _fill_visual_dates_by_order(visual: dict[str, Any], tokens: list[dict]) -> None:
    dates = _extract_all_visual_dates(tokens)
    if not dates:
        return
    current_year = _date.today().year
    if not visual.get("date_of_birth"):
        birth = next(
            (item for item in dates if (_date_year(item[1]) or 0) <= current_year),
            None,
        )
        if birth:
            visual["date_of_birth"] = birth[1]
            visual["_date_of_birth_conf"] = round(birth[2], 3)

    used = {visual.get("date_of_birth")}
    remaining = [item for item in dates if item[1] not in used]
    if not visual.get("date_of_issue") and len(remaining) >= 2:
        visual["date_of_issue"] = remaining[0][1]
        visual["_date_of_issue_conf"] = round(remaining[0][2], 3)
    if not visual.get("date_of_expiry") and remaining:
        visual["date_of_expiry"] = remaining[-1][1]
        visual["_date_of_expiry_conf"] = round(remaining[-1][2], 3)


def extract_visual_fields(tokens: list[dict], variants: dict | None = None) -> dict:
    visual: dict[str, Any] = {}

    for field, kind in {
        "date_of_birth": "birth",
        "date_of_issue": "issue",
        "date_of_expiry": "expiry",
    }.items():
        value, conf = _extract_visual_date_by_kind(tokens, kind)
        if value:
            visual[field] = value
            visual[f"_{field}_conf"] = conf
    _fill_visual_dates_by_order(visual, tokens)

    latin_name_y = None
    name_helper = _passport_parser_helper("_extract_visual_english_name_passport")
    if name_helper:
        try:
            name, conf, latin_name_y = name_helper(tokens)
            if name and not _bad_visual_latin_name(name):
                visual["full_name_en"] = name
                visual["_full_name_en_conf"] = conf
        except Exception:
            latin_name_y = None

    if not visual.get("full_name_en"):
        name_anchor = next(
            (t for t in tokens
             if "name" in strip_arabic(t.get("text", "")).lower()
             and "mother" not in strip_arabic(t.get("text", "")).lower()),
            None,
        )
        if name_anchor:
            row = tokens_right_of(tokens, name_anchor) or tokens_below(tokens, name_anchor, max_dy=80)
            words = [strip_arabic(t.get("text", "")).upper() for t in row]
            words = [w for w in words if re.search(r"[A-Z]", w) and not re.search(r"\d", w)]
            candidate = re.sub(r"\s+", " ", " ".join(words)).strip()
            if candidate and not _bad_visual_latin_name(candidate):
                visual["full_name_en"] = candidate
                latin_name_y = float(name_anchor.get("cy", 0))

    nat_anchor = next((t for t in tokens if "national" in strip_arabic(t.get("text", "")).lower()), None)
    if nat_anchor:
        row = tokens_right_of(tokens, nat_anchor) or tokens_below(tokens, nat_anchor, max_dy=80)
        digits = normalize_digits(re.sub(r"[^\d\u0660-\u0669]", "", " ".join(t.get("text", "") for t in row)))
        if len(digits) >= 8:
            visual["national_number"] = digits

    if any(_REAL_ARABIC_LETTER_RE.search(t.get("text", "")) for t in tokens):
        ar_helper = _passport_parser_helper("_extract_arabic_name_passport")
        clean_helper = _passport_parser_helper("_clean_visual_arabic_name")
        band_helper = _passport_parser_helper("_extract_arabic_name_from_name_band")
        too_short = _passport_parser_helper("_arabic_name_too_short_for_latin")

        arabic_name = None
        ar_conf = 0.0
        if ar_helper:
            try:
                raw_name, ar_conf = ar_helper(tokens, latin_name_y)
                arabic_name = clean_helper(raw_name) if clean_helper else _normalize_hybrid_arabic_name(raw_name)
            except Exception:
                arabic_name = None
                ar_conf = 0.0
        if band_helper and variants is not None and latin_name_y is not None:
            try:
                focused_name, focused_conf = band_helper(variants, latin_name_y)
                focused_name = clean_helper(focused_name) if clean_helper else _normalize_hybrid_arabic_name(focused_name)
                if (
                    focused_name
                    and (not arabic_name or len(focused_name.split()) > len(arabic_name.split()) or focused_conf > ar_conf)
                ):
                    arabic_name, ar_conf = focused_name, focused_conf
            except Exception:
                pass
        if arabic_name:
            normalized_ar = _normalize_hybrid_arabic_name(arabic_name)
            if normalized_ar and not (too_short and too_short(normalized_ar, visual.get("full_name_en"))):
                visual["full_name_ar"] = normalized_ar
                visual["_full_name_ar_conf"] = round(float(ar_conf or 0.0), 3)

    return visual


_PASSPORT_OLLAMA_REQUIRED = (
    "passport_number", "full_name_en", "date_of_birth", "date_of_expiry",
    "sex", "nationality",
)


def _passport_mrz_check_pass_count(mrz: dict | None) -> int:
    checks = (mrz or {}).get("mrz_checks") or {}
    return sum(1 for v in checks.values() if v is True)


def _skip_reliable_ollama_enabled() -> bool:
    return str(os.environ.get("OCR_SKIP_RELIABLE_OLLAMA", "0")).strip().lower() in {"1", "true", "yes", "on"}

def _should_skip_ollama(current: dict, mrz: dict, quality: dict) -> bool:
    if not all(_not_empty(current.get(k)) for k in _PASSPORT_OLLAMA_REQUIRED):
        return False
    if not is_valid_name(str(current.get("full_name_en") or "")):
        return False
    if not is_valid_date(current.get("date_of_birth")) or not is_valid_date(current.get("date_of_expiry")):
        return False
    return bool((mrz or {}).get("mrz_valid") or _passport_mrz_check_pass_count(mrz) >= 3)

def _ollama_validate(image_path: str, current: dict, mrz: dict, visual: dict,
                     quality: dict, enabled: bool) -> dict:
    if not enabled:
        return {}
    if _skip_reliable_ollama_enabled() and _should_skip_ollama(current, mrz, quality):
        return {"_skipped": "reliable_mrz_visual"}
    try:
        import ollama_ocr
        if hasattr(ollama_ocr, "validate_passport_hybrid"):
            return ollama_ocr.validate_passport_hybrid(
                image_path=image_path,
                extracted=current,
                mrz=mrz,
                visual=visual,
                quality=quality,
            ) or {}
        raw = ollama_ocr.extract_passport(image_path)
        return raw.get("data") or raw.get("passport") or raw
    except Exception as exc:
        return {"_error": str(exc)}


def _aliases(notebook_data: dict) -> dict:
    alias_map = {
        "passport_number": "passport_no",
        "full_name_en": "name_en",
        "full_name_ar": "name_ar",
        "date_of_expiry": "expiry_date",
        "date_of_issue": "issue_date",
        "sex": "gender",
        "national_number": "national_no",
    }
    out = dict(notebook_data)
    for src, dst in alias_map.items():
        if notebook_data.get(src):
            out[dst] = notebook_data[src]
    return out


_PASSPORT_CONF_REQUIRED = (
    "passport_number", "full_name_en", "date_of_birth", "date_of_expiry",
    "sex", "nationality",
)


def _filled_ratio(data: dict | None, keys: tuple[str, ...]) -> float:
    if not keys:
        return 0.0
    data = data or {}
    return sum(1 for key in keys if _not_empty(data.get(key))) / len(keys)


def _overall_confidence(field_confs: dict, mrz: dict, quality: dict, data: dict | None = None) -> float:
    ocr_field_score = sum(field_confs.values()) / len(field_confs) if field_confs else 0.0
    filled_score = _filled_ratio(data, _PASSPORT_CONF_REQUIRED) if data is not None else ocr_field_score
    field_score = ocr_field_score if field_confs else min(0.70, filled_score)

    mrz_score = float((mrz or {}).get("confidence") or 0.0)
    if (mrz or {}).get("mrz_valid"):
        mrz_score = max(mrz_score, 0.90)
    elif _passport_mrz_check_pass_count(mrz) >= 3:
        mrz_score = max(mrz_score, 0.80)

    quality_score = 1.0 if (quality or {}).get("usable") else filled_score
    score = (0.62 * field_score + 0.23 * mrz_score + 0.10 * quality_score + 0.05 * filled_score) * 100
    critical = [field_confs.get(k) for k in ("passport_number", "full_name_en", "date_of_birth", "date_of_expiry")]
    if data and data.get("full_name_ar"):
        critical.append(field_confs.get("full_name_ar"))
    critical = [float(v) for v in critical if isinstance(v, (int, float))]
    if critical:
        weakest = min(critical)
        if weakest < 0.55:
            score = min(score, 72.0)
        elif weakest < 0.70:
            score = min(score, 84.0)
        elif weakest < 0.80:
            score = min(score, 89.0)
    return round(max(0.0, min(100.0, score)), 2)


def _refresh_name_confidences(data: dict, field_sources: dict,
                              visual_fields: dict | None = None,
                              ollama_fields: dict | None = None,
                              mrz_fields: dict | None = None) -> None:
    if data.get("full_name_en"):
        final_key = _compact_latin_name_key(data.get("full_name_en"))
        for source, fields, confidence in (
            ("visual", visual_fields or {}, 0.86),
            ("ollama", ollama_fields or {}, 0.84),
            ("mrz", mrz_fields or {}, 0.78),
        ):
            if final_key and final_key == _compact_latin_name_key(fields.get("full_name_en")):
                previous = field_sources.get("full_name_en") or {}
                field_sources["full_name_en"] = {
                    "source": source,
                    "confidence": max(float(previous.get("confidence") or 0.0), confidence),
                }
                break
    if data.get("full_name_ar"):
        previous = field_sources.get("full_name_ar") or {}
        source = data.get("_source_full_name_ar") or previous.get("source") or "postprocess"
        field_sources["full_name_ar"] = {
            "source": source,
            "confidence": _arabic_name_confidence(data.get("full_name_ar"), data.get("full_name_en"), source),
        }

def extract_passport(image_path: str, use_ollama: bool = True, debug: bool = False) -> dict:
    try:
        max_width = int(os.getenv("OCR_PASSPORT_MAX_WIDTH", "1200"))
        min_width = int(os.getenv("OCR_PASSPORT_MIN_WIDTH", "800"))
        original = load_and_resize(image_path, max_width=max_width, min_width=min_width)
    except Exception as exc:
        return {
            "success": False,
            "status": "error",
            "error": f"Cannot load image: {exc}",
            "pipeline": NOTEBOOK_FLOW,
        }

    oriented, orientation = correct_orientation(original)
    working, crop_meta = _crop_passport_data_page(oriented)
    variants = build_variants(working)
    quality = compute_quality(working)
    text_source = select_source(variants, mode="text")
    mrz_source = select_source(variants, mode="mrz")

    try:
        visual_tokens = _collect_visual_tokens(variants)
        mrz_tokens = ocr_to_tokens(mrz_source, reader=READER_EN, allowlist=MRZ_ALLOWLIST, min_conf=0.01)
        _trace("PASSPORT_EASYOCR_VISUAL_TOKENS", _token_preview(visual_tokens))
        _trace("PASSPORT_EASYOCR_MRZ_TOKENS", _token_preview(mrz_tokens))
    except Exception as exc:
        return {
            "success": False,
            "status": "error",
            "error": f"OCR failed: {exc}",
            "quality": quality,
            "pipeline": NOTEBOOK_FLOW,
        }

    mrz_fields, mrz = extract_mrz(working, variants, mrz_tokens, mrz_source.shape[0])
    visual_fields = extract_visual_fields(visual_tokens, variants)
    _trace("PASSPORT_EASYOCR_VISUAL_FIELDS", visual_fields)
    _trace("PASSPORT_MRZ_RAW", mrz)
    _trace("PASSPORT_MRZ_FIELDS", mrz_fields)
    if visual_fields.get("full_name_en") and mrz.get("line1") and mrz.get("name_trustworthy"):
        surname, given = _best_mrz_visual_name_split(visual_fields.get("full_name_en"), mrz.get("line1"))
        if surname and given:
            visual_fields["surname"] = surname
            visual_fields["given_names"] = given
    # Visual fields are the printed document text. Keep them ahead of MRZ in the
    # Ollama context because TD3 MRZ names are fixed-width and may be truncated.
    preliminary = {**mrz_fields, **visual_fields}
    ollama_fields = _ollama_validate(image_path, preliminary, mrz, visual_fields, quality, use_ollama)
    _trace("PASSPORT_OLLAMA_FIELDS", ollama_fields)

    data: dict[str, Any] = {}
    field_sources: dict[str, dict] = {}
    for field in NOTEBOOK_FIELD_ORDER:
        value, source, conf = arbitrate(
            field,
            mrz_fields.get(field),
            mrz.get("mrz_checks") or {},
            visual_fields.get(field),
            ollama_fields.get(field),
            quality,
        )
        if _not_empty(value):
            data[field] = value
            field_sources[field] = {"source": source, "confidence": round(conf, 2)}

    _normalize_hybrid_data(data, mrz_fields, visual_fields, ollama_fields, mrz)
    _refresh_name_confidences(data, field_sources, visual_fields, ollama_fields, mrz_fields)

    for raw_key, data_key in (("line1", "mrz_l1"), ("line2", "mrz_l2")):
        if mrz.get(raw_key):
            data[data_key] = mrz[raw_key]

    field_confs = {k: v["confidence"] for k, v in field_sources.items()}
    _trace("PASSPORT_FIELD_SOURCES", field_sources)
    _trace("PASSPORT_FIELD_CONFS", field_confs)
    _trace("PASSPORT_FINAL_DATA", data)
    confidence = {
        "field_confidence": field_confs,
        "mrz_confidence": round(float(mrz.get("confidence") or 0.0), 3),
        "ocr_confidence": round(
            sum(float(t.get("conf", 0)) for t in visual_tokens) / max(len(visual_tokens), 1),
            3,
        ),
        "overall_document_confidence": _overall_confidence(field_confs, mrz, quality, data),
    }
    validation = {
        "mrz_valid": bool(mrz.get("mrz_valid")),
        "mrz_checks": mrz.get("mrz_checks") or {},
        "quality_usable": bool(quality.get("usable")),
        "ollama_used": bool(use_ollama and ollama_fields and not ollama_fields.get("_error") and not ollama_fields.get("_skipped")),
        "ollama_error": ollama_fields.get("_error"),
    }

    status = "ok" if data else "no_fields"
    return {
        "success": status == "ok",
        "status": status,
        "doc_type": "passport",
        "pipeline": NOTEBOOK_FLOW,
        "data": data,
        "passport": _aliases(data),
        "field_confs": {**field_confs, **{
            "passport_no": field_confs.get("passport_number", 0.0),
            "name_en": field_confs.get("full_name_en", 0.0),
            "expiry_date": field_confs.get("date_of_expiry", 0.0),
            "national_no": field_confs.get("national_number", 0.0),
            "gender": field_confs.get("sex", 0.0),
        }},
        "field_sources": field_sources,
        "mrz": mrz,
        "visual": {"fields": visual_fields, "tokens": visual_tokens if debug else []},
        "ollama": {"fields": ollama_fields},
        "quality": quality,
        "orientation": {**orientation, "crop": crop_meta},
        "confidence": confidence,
        "validation": validation,
        "_source": "mrz+visual+ollama" if validation["ollama_used"] else "mrz+visual",
    }
