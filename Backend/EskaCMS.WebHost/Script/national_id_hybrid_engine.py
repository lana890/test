# -*- coding: utf-8 -*-
"""
Notebook-style hybrid engine for Jordanian national ID cards.

Flow:
Image -> Preprocessing -> Orientation Correction -> OCR Token Extraction
-> MRZ Detection -> MRZ Parsing -> Visual OCR Extraction
-> Hybrid Field Resolution -> Ollama Validation & Correction
-> Confidence Scoring -> Structured JSON Response.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any
import json
import os
import re
import unicodedata
from difflib import SequenceMatcher

import national_id_parser
from ocr_engine import repair_arabic_mojibake


NATIONAL_ID_FLOW = (
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

FIELD_ORDER = (
    "national_number",
    "full_name_ar",
    "full_name_en",
    "date_of_birth",
    "gender",
    "nationality",
    "place_of_birth",
    "mother_name",
    "document_number",
    "expiry_date",
    "blood_type",
)

MRZ_CHECK_MAP = {
    "national_number": "composite",
    "full_name_en": "composite",
    "date_of_birth": "dob",
    "gender": "composite",
    "document_number": "doc_number",
    "expiry_date": "expiry",
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


def _value(value: Any) -> Any:
    if isinstance(value, dict):
        inner = value.get("value")
        while isinstance(inner, dict):
            inner = inner.get("value")
        return inner
    return value


def _not_empty(value: Any) -> bool:
    value = _value(value)
    return value not in (None, "", [], {}, "null", "None", "...")

def _clean_latin_name(value: Any) -> str:
    text = str(_value(value) or "").upper()
    text = re.sub(r"[\u0600-\u06FF]+", " ", text)
    text = re.sub(r"[^A-Z\s'-]", " ", text)
    return re.sub(r"\s+", " ", text).strip()


def _latin_words(value: Any) -> list[str]:
    return [w for w in re.split(r"\s+", _clean_latin_name(value)) if len(w) >= 2]


def _latin_key(value: Any) -> str:
    return re.sub(r"[^A-Z]", "", _clean_latin_name(value))


def _latin_similarity(left: Any, right: Any) -> float:
    lkey = _latin_key(left)
    rkey = _latin_key(right)
    if not lkey or not rkey:
        return 0.0
    return SequenceMatcher(None, lkey, rkey).ratio()


def _complete_internal_latin_prefixes(name: Any) -> str | None:
    words = _latin_words(name)
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
    text = " ".join(completed)
    return text if text != " ".join(words) else _clean_latin_name(name)


def _complete_truncated_latin_words(words: list[str], references: list[str]) -> list[str]:
    completed = []
    for word in words:
        replacement = next(
            (
                ref for ref in references
                if len(word) >= 3
                and len(word) < len(ref) <= len(word) + 4
                and ref.startswith(word)
            ),
            None,
        )
        completed.append(replacement or word)
    return completed


def _name_from_mrz_l3(mrz: dict | None, *reference_names: Any) -> str | None:
    mrz = mrz or {}
    existing = _clean_latin_name(mrz.get("mrz_name_full"))
    if existing:
        return existing

    line = str(mrz.get("mrz_l3") or mrz.get("line3") or "").upper()
    line = re.sub(r"[^A-Z<]", "", line)
    if "<<" not in line:
        return None
    surname_raw, given_raw = line.split("<<", 1)
    surname_words = [w for w in surname_raw.split("<") if len(w) >= 2]
    given_words = [w for w in given_raw.split("<") if len(w) >= 2]
    if not surname_words or not given_words:
        return None

    refs: list[str] = []
    for ref_name in reference_names:
        refs.extend(_latin_words(ref_name))
    words = _complete_truncated_latin_words(given_words + surname_words, refs)
    if len(words) < 2 or any(len(w) > 24 for w in words):
        return None
    return " ".join(words)


def _clean_arabic_name(value: Any) -> str | None:
    text = repair_arabic_mojibake(str(_value(value) or ""))
    if not text or not re.search(r"[\u0600-\u06FF]", text):
        return None
    text = unicodedata.normalize("NFKC", text)
    text = re.sub(r"[\u064B-\u065F\u0670\u0640]", "", text)
    text = re.sub(r"[\u0623\u0625\u0622\u0671]", "\u0627", text)
    text = text.replace("\u0649", "\u064A")
    text = re.sub(r"[A-Za-z0-9_]+", " ", text)
    text = re.sub(r"[^\u0600-\u06FF\s]", " ", text)
    words = [w for w in re.split(r"\s+", text.strip()) if len(w) >= 2]
    if len(words) < 2:
        return None
    return " ".join(words)



_LATIN_AR_DIGRAPHS = (
    ("SH", "\u0634"),
    ("KH", "\u062E"),
    ("GH", "\u063A"),
    ("TH", "\u062B"),
    ("DH", "\u0630"),
    ("PH", "\u0641"),
    ("CH", "\u062A\u0634"),
)
_LATIN_AR_CONSONANTS = {
    "B": "\u0628", "C": "\u0643", "D": "\u062F", "F": "\u0641",
    "G": "\u062C", "H": "\u062D", "J": "\u062C", "K": "\u0643",
    "L": "\u0644", "M": "\u0645", "N": "\u0646", "P": "\u0628",
    "Q": "\u0642", "R": "\u0631", "S": "\u0633", "T": "\u062A",
    "V": "\u0641", "W": "\u0648", "X": "\u0643\u0633", "Y": "\u064A",
    "Z": "\u0632",
}


def _split_latin_name_parts(name_en: Any) -> list[str]:
    parts: list[str] = []
    for word in _latin_words(name_en):
        m = re.match(r"^(ABU|ABO|ABOU)([A-Z]{3,})$", word)
        if m:
            parts.append("ABU")
            parts.append(m.group(2))
            continue
        parts.append(word)
    return parts


def _latin_word_to_arabic_phonetic(word: str) -> str:
    word = re.sub(r"[^A-Z]", "", word.upper())
    if not word:
        return ""
    if word in {"ABU", "ABO", "ABOU"}:
        return "\u0627\u0628\u0648"
    out: list[str] = []
    i = 0
    previous = ""
    while i < len(word):
        pair = word[i:i + 2]
        match = next((arabic for latin, arabic in _LATIN_AR_DIGRAPHS if pair == latin), None)
        if match:
            out.append(match)
            previous = pair[-1]
            i += 2
            continue

        ch = word[i]
        nxt = word[i + 1] if i + 1 < len(word) else ""
        at_start = i == 0
        at_end = i == len(word) - 1

        if ch in "AEIOU":
            if at_start:
                out.append("\u0627")
            elif ch in "OU" and previous == "M" and nxt == "H":
                pass
            elif ch == "E" and nxt in "DT" and i + 2 == len(word):
                pass
            elif ch in "EIY" and (nxt == "" or nxt not in "AEIOU"):
                out.append("\u064A")
            elif ch in "OU":
                out.append("\u0648")
            elif ch == "A" and nxt == "A":
                out.append("\u0627")
            i += 1
            previous = ch
            continue

        if ch == "Y":
            out.append("\u064A")
        elif ch == "A" and at_end:
            out.append("\u0627")
        elif ch == "H" and at_end and previous in "AEIOU":
            pass
        else:
            mapped = _LATIN_AR_CONSONANTS.get(ch)
            if mapped:
                out.append(mapped)
        previous = ch
        i += 1

    text = "".join(out)
    text = re.sub(r"([\u0600-\u06FF])\1+", r"\1", text)
    return text


def _latin_name_to_arabic_phonetic(name_en: Any) -> str | None:
    parts = [_latin_word_to_arabic_phonetic(word) for word in _split_latin_name_parts(name_en)]
    parts = [part for part in parts if part]
    if len(parts) < 2:
        return None
    return _clean_arabic_name(" ".join(parts))


def _arabic_incomplete_for_latin(name_ar: Any, name_en: Any) -> bool:
    cleaned = _clean_arabic_name(name_ar)
    latin_count = len(_split_latin_name_parts(name_en))
    if not cleaned or latin_count < 3:
        return False
    return len(cleaned.split()) < max(3, latin_count - 1)


def _repair_common_arabic_name_ocr(value: Any) -> str | None:
    """Normalize Arabic OCR output without injecting fixed personal names."""
    cleaned = _clean_arabic_name(value)
    if not cleaned:
        return None
    text = re.sub(r"\s+", " ", cleaned).strip()
    return re.sub(r"\s+", " ", text).strip() or None


def _arabic_compare_key(value: Any) -> str:
    cleaned = _clean_arabic_name(value) or ""
    cleaned = cleaned.replace("\u0629", "\u0647").replace("\u0649", "\u064A")
    return re.sub(r"\s+", "", cleaned)


def _arabic_word_key(value: str) -> str:
    text = unicodedata.normalize("NFKC", str(value or ""))
    text = re.sub(r"[\u064B-\u065F\u0670\u0640]", "", text)
    text = re.sub(r"[\u0623\u0625\u0622\u0671]", "\u0627", text)
    text = text.replace("\u0649", "\u064A").replace("\u0629", "\u0647")
    text = re.sub(r"[^\u0621-\u064A]", "", text)
    return text


def _arabic_word_similarity(left: str, right: str) -> float:
    lkey = _arabic_word_key(left)
    rkey = _arabic_word_key(right)
    if not lkey or not rkey:
        return 0.0
    return SequenceMatcher(None, lkey, rkey).ratio()


def _arabic_similarity(left: Any, right: Any) -> float:
    lkey = _arabic_compare_key(left)
    rkey = _arabic_compare_key(right)
    if not lkey or not rkey:
        return 0.0
    return SequenceMatcher(None, lkey, rkey).ratio()


def _arabic_latin_alignment(name_ar: Any, name_en: Any) -> float:
    fallback = _latin_name_to_arabic_phonetic(name_en)
    if not fallback:
        return 0.0
    return _arabic_similarity(name_ar, fallback)


def _arabic_compact_length_ratio(left: Any, right: Any) -> float:
    left_len = _compact_arabic_len(left)
    right_len = _compact_arabic_len(right)
    if not left_len or not right_len:
        return 0.0
    return left_len / max(right_len, 1)


def _has_bad_word_gap(current_ar: Any, fallback_ar: Any) -> bool:
    current_words = (_repair_common_arabic_name_ocr(current_ar) or "").split()
    fallback_words = (_repair_common_arabic_name_ocr(fallback_ar) or "").split()
    if len(current_words) != len(fallback_words):
        return False
    weak = 0
    for current, fallback in zip(current_words, fallback_words):
        if len(current) <= 2 < len(fallback) and _arabic_word_similarity(current, fallback) < 0.70:
            weak += 1
    return weak >= 1


def _compatible_arabic_ocr_substitution(left: str, right: str) -> bool:
    if len(left) != len(right):
        return False
    pairs = [(a, b) for a, b in zip(left, right) if a != b]
    if len(pairs) != 1:
        return False
    weak, strong = pairs[0]
    confusions = {
        ("\u062F", "\u0637"), ("\u0637", "\u062F"),
        ("\u062F", "\u0630"), ("\u0630", "\u062F"),
        ("\u0631", "\u0632"), ("\u0632", "\u0631"),
        ("\u0628", "\u062A"), ("\u062A", "\u0628"),
        ("\u0628", "\u062B"), ("\u062B", "\u0628"),
        ("\u062C", "\u062D"), ("\u062D", "\u062C"),
        ("\u062D", "\u062E"), ("\u062E", "\u062D"),
        ("\u0633", "\u0634"), ("\u0634", "\u0633"),
        ("\u0635", "\u0636"), ("\u0636", "\u0635"),
        ("\u0647", "\u0629"), ("\u0629", "\u0647"),
        ("\u064A", "\u0649"), ("\u0649", "\u064A"),
    }
    return (weak, strong) in confusions


def _repair_arabic_name_with_latin_fallback(current_ar: Any, fallback_ar: Any) -> str | None:
    current = _repair_common_arabic_name_ocr(current_ar)
    fallback = _repair_common_arabic_name_ocr(fallback_ar)
    if not current or not fallback:
        return current
    current_words = current.split()
    fallback_words = fallback.split()
    if len(current_words) != len(fallback_words):
        return current

    changed = False
    repaired: list[str] = []
    for current_word, fallback_word in zip(current_words, fallback_words):
        similarity = _arabic_word_similarity(current_word, fallback_word)
        use_fallback_word = False
        if (
            len(fallback_word) > len(current_word)
            and len(fallback_word) - len(current_word) <= 2
            and similarity >= 0.72
        ):
            use_fallback_word = True
        elif _compatible_arabic_ocr_substitution(current_word, fallback_word) and similarity >= 0.72:
            use_fallback_word = True

        if use_fallback_word:
            repaired.append(fallback_word)
            changed = True
        else:
            repaired.append(current_word)

    if not changed:
        return current
    return _repair_common_arabic_name_ocr(" ".join(repaired))


def _compact_arabic_len(value: Any) -> int:
    return len(_arabic_compare_key(value))


def _arabic_name_score(value: Any, name_en: Any, source: str = "") -> float:
    cleaned = _repair_common_arabic_name_ocr(value)
    if not cleaned:
        return -100.0
    words = cleaned.split()
    latin_count = len(_latin_words(name_en))
    score = len(words) * 18.0 + min(len(cleaned), 60) * 0.8
    if source == "visual":
        score += 3.0
    elif source == "ollama":
        score += 8.0
    alignment = _arabic_latin_alignment(cleaned, name_en)
    if alignment:
        score += alignment * 70.0
        if latin_count >= 3 and alignment < 0.68:
            score -= 60.0
        elif alignment >= 0.86:
            score += 18.0
    if latin_count >= 3 and len(words) < max(3, latin_count - 1):
        score -= 70.0
    if any(len(w) <= 1 for w in words):
        score -= 30.0
    return score


def _arabic_name_confidence(value: Any, name_en: Any, source: str = "") -> float:
    cleaned = _repair_common_arabic_name_ocr(value)
    if not cleaned:
        return 0.0
    words = cleaned.split()
    latin_count = len(_latin_words(name_en))
    alignment = _arabic_latin_alignment(cleaned, name_en)
    base = 0.34 + min(len(words), 5) * 0.055 + min(_compact_arabic_len(cleaned), 32) / 32.0 * 0.12
    if alignment:
        base = max(base, 0.26 + alignment * 0.66)
        if latin_count >= 3 and alignment < 0.68:
            base = min(base, 0.54)
        elif latin_count >= 3 and alignment < 0.78:
            base = min(base, 0.74)
    if source.startswith("ollama"):
        base += 0.03
    elif source == "dynamic_phonetic":
        base = min(max(base, 0.72), 0.90)
    if latin_count >= 3 and len(words) < max(3, latin_count - 1):
        base = min(base, 0.58)
    if any(len(w) <= 1 for w in words):
        base = min(base, 0.45)
    return round(max(0.0, min(base, 0.96)), 3)


def _strong_ollama_arabic_name(value: Any, name_en: Any) -> str | None:
    cleaned = _repair_common_arabic_name_ocr(value)
    if not cleaned or re.search(r"[A-Za-z0-9]", cleaned):
        return None
    words = [w for w in cleaned.split() if re.search(r"[\u0600-\u06FF]", w)]
    if len(words) < 2:
        return None
    latin_count = len(_latin_words(name_en))
    if latin_count >= 4 and len(words) < 3:
        return None
    label_like = {
        "الاسم", "الاسم:", "الجنس", "ذكر", "انثى", "تاريخ", "مكان",
        "الولاده", "الميلاد", "الام", "الرقم", "الوطني",
    }
    if all(_arabic_word_key(w) in {_arabic_word_key(x) for x in label_like} for w in words):
        return None
    if _compact_arabic_len(cleaned) < max(6, len(words) * 2):
        return None
    return cleaned


def _postprocess_names(data: dict, visual_data: dict, ollama_data: dict, mrz: dict) -> None:
    current_en = data.get("full_name_en") or visual_data.get("full_name_en") or ollama_data.get("full_name_en")
    mrz_name = _name_from_mrz_l3(
        mrz,
        current_en,
        visual_data.get("full_name_en"),
        ollama_data.get("full_name_en"),
    )
    if mrz_name:
        current_words = _latin_words(current_en)
        mrz_words = _latin_words(mrz_name)
        if (
            not current_words
            or (
                len(mrz_words) > len(current_words)
                and _latin_similarity(mrz_name, current_en) >= 0.74
            )
            or _latin_similarity(mrz_name, current_en) >= 0.92
        ):
            data["full_name_en"] = mrz_name
            current_en = mrz_name
        elif current_en:
            data["full_name_en"] = _clean_latin_name(current_en)
    if data.get("full_name_en"):
        completed_en = _complete_internal_latin_prefixes(data.get("full_name_en"))
        if completed_en:
            data["full_name_en"] = completed_en
            current_en = completed_en

    arabic_candidates = [
        ("visual", visual_data.get("full_name_ar")),
        ("ollama", ollama_data.get("full_name_ar")),
        ("selected", data.get("full_name_ar")),
    ]
    fallback = _latin_name_to_arabic_phonetic(current_en)
    strong_ollama = _strong_ollama_arabic_name(ollama_data.get("full_name_ar"), current_en)
    if strong_ollama:
        repaired = _repair_arabic_name_with_latin_fallback(strong_ollama, fallback) if fallback else strong_ollama
        data["full_name_ar"] = repaired or strong_ollama
        data["_source_full_name_ar"] = "ollama_arabic_repair" if repaired and repaired != strong_ollama else "ollama"
        return

    scored = [
        (source, _repair_common_arabic_name_ocr(value), _arabic_name_score(value, current_en, source))
        for source, value in arabic_candidates
    ]
    scored = [(s, v, score) for s, v, score in scored if v]
    if scored:
        scored.sort(key=lambda item: (item[2], len(item[1])), reverse=True)
        best_source, best_value, _ = scored[0]
        use_fallback = False
        if fallback:
            repaired_value = _repair_arabic_name_with_latin_fallback(best_value, fallback)
            if repaired_value and repaired_value != best_value:
                best_value = repaired_value
                best_source = "arabic_repair"
            best_words = best_value.split()
            fallback_words = fallback.split()
            suspicious_best = any(len(w) <= 1 or len(w) > 10 for w in best_words)
            best_alignment = _arabic_latin_alignment(best_value, current_en)
            length_ratio = _arabic_compact_length_ratio(best_value, fallback)
            use_fallback = (
                _arabic_incomplete_for_latin(best_value, current_en)
                or (len(_latin_words(current_en)) >= 3 and best_alignment and best_alignment < 0.68)
                or (
                    len(_latin_words(current_en)) >= 3
                    and len(fallback_words) >= len(best_words)
                    and length_ratio < 0.86
                    and best_alignment < 0.82
                )
                or (
                    len(_latin_words(current_en)) >= 4
                    and _has_bad_word_gap(best_value, fallback)
                    and best_alignment < 0.84
                )
                or (
                    suspicious_best
                    and
                    len(_latin_words(current_en)) >= 4
                    and len(fallback_words) >= max(2, len(best_words) - 1)
                    and _arabic_similarity(best_value, fallback) < 0.62
                )
            )
        if use_fallback and fallback:
            data["full_name_ar"] = fallback
            data["_source_full_name_ar"] = "dynamic_phonetic"
        else:
            data["full_name_ar"] = best_value
            data["_source_full_name_ar"] = best_source
    elif fallback:
        data["full_name_ar"] = fallback
        data["_source_full_name_ar"] = "dynamic_phonetic"


def _refresh_name_confidences(data: dict, field_sources: dict) -> None:
    name_en = data.get("full_name_en")
    if data.get("full_name_ar"):
        previous = field_sources.get("full_name_ar") or {}
        source = data.get("_source_full_name_ar") or previous.get("source") or "postprocess"
        if _arabic_latin_alignment(data.get("full_name_ar"), name_en) >= 0.86:
            source = source if source != "none" else "postprocess"
        conf = _arabic_name_confidence(data.get("full_name_ar"), name_en, source)
        field_sources["full_name_ar"] = {"source": source, "confidence": conf}


def _valid_date(value: str | None) -> bool:
    return bool(value and re.match(r"^\d{2}/\d{2}/\d{4}$", str(value).strip()))


def _valid_national_number(value: str | None) -> bool:
    return bool(value and re.match(r"^\d{10}$", re.sub(r"\D", "", str(value))))


def _valid_doc_number(value: str | None) -> bool:
    return bool(value and re.match(r"^[A-Z0-9]{5,12}$", str(value).strip().upper()))


def _quality_ratio(quality: dict | None) -> float:
    if not isinstance(quality, dict):
        return 0.0
    if "front" in quality or "back" in quality:
        sides = [quality.get("front"), quality.get("back")]
        usable = [1.0 if isinstance(s, dict) and s.get("usable") else 0.0 for s in sides]
        return sum(usable) / len(usable)
    return 1.0 if quality.get("usable") else 0.0


def score_candidate(value: Any, source: str, field: str, checks: dict, quality: dict) -> float:
    value = _value(value)
    if not _not_empty(value):
        return 0.0

    if source == "mrz":
        key = MRZ_CHECK_MAP.get(field)
        if key and checks.get(key):
            return 0.97
        if key:
            return 0.20
        return 0.10

    if source == "visual":
        score = 0.60
        if field == "national_number":
            score = 0.72
            if _valid_national_number(value):
                score *= 1.20
            else:
                score *= 0.15
        elif field in ("date_of_birth", "expiry_date"):
            score = 0.65
            score *= 1.20 if _valid_date(value) else 0.15
        elif field == "document_number":
            score = 0.72
            score *= 1.15 if _valid_doc_number(value) else 0.20
        elif field in ("full_name_ar", "full_name_en", "place_of_birth"):
            score = 0.70
        elif field == "mother_name":
            score = 0.66
        if _quality_ratio(quality) < 0.5:
            score *= 0.75
        return min(score, 1.0)

    if source == "ollama":
        score = 0.82 if field in (
            "full_name_ar", "full_name_en", "place_of_birth", "mother_name", "blood_type"
        ) else 0.75
        if field == "national_number" and not _valid_national_number(value):
            score *= 0.15
        if field in ("date_of_birth", "expiry_date") and not _valid_date(value):
            score *= 0.20
        if field == "document_number" and not _valid_doc_number(value):
            score *= 0.20
        if _quality_ratio(quality) < 0.5:
            score *= 0.75
        return min(score, 1.0)

    return 0.0


def arbitrate(field: str, mrz_value: Any, visual_value: Any, ollama_value: Any,
              checks: dict, quality: dict) -> tuple[Any, str, float]:
    candidates = [
        FieldCandidate(_value(mrz_value), "mrz", score_candidate(mrz_value, "mrz", field, checks, quality)),
        FieldCandidate(_value(visual_value), "visual", score_candidate(visual_value, "visual", field, checks, quality)),
        FieldCandidate(_value(ollama_value), "ollama", score_candidate(ollama_value, "ollama", field, checks, quality)),
    ]
    candidates = [c for c in candidates if _not_empty(c.value)]
    if not candidates:
        return None, "none", 0.0
    priority = {"mrz": 0, "visual": 1, "ollama": 2}
    candidates.sort(key=lambda c: (-c.confidence, priority.get(c.source, 9)))
    winner = candidates[0]
    return winner.value, winner.source, winner.confidence


_NATIONAL_ID_OLLAMA_REQUIRED = (
    "national_number", "full_name_ar", "full_name_en", "date_of_birth",
    "gender", "document_number", "expiry_date", "blood_type",
)


def _complete_arabic_name(value: Any) -> bool:
    text = re.sub(r"\s+", " ", str(value or "").strip())
    if not text or re.search(r"[A-Za-z0-9]", text):
        return False
    words = [w for w in text.split() if re.search(r"[\u0600-\u06FF]", w)]
    return len(words) >= 3


def _mrz_check_pass_count(mrz: dict | None) -> int:
    checks = (mrz or {}).get("mrz_checks") or {}
    return sum(1 for v in checks.values() if v is True)


def _skip_reliable_ollama_enabled() -> bool:
    return str(os.environ.get("OCR_SKIP_RELIABLE_OLLAMA", "0")).strip().lower() in {"1", "true", "yes", "on"}

def _should_skip_ollama(current: dict, mrz: dict, quality: dict) -> bool:
    if not all(_not_empty(current.get(k)) for k in _NATIONAL_ID_OLLAMA_REQUIRED):
        return False
    if not _valid_national_number(current.get("national_number")):
        return False
    if not _valid_doc_number(current.get("document_number")):
        return False
    if not _valid_date(current.get("date_of_birth")) or not _valid_date(current.get("expiry_date")):
        return False
    if not _complete_arabic_name(current.get("full_name_ar")):
        return False
    if len(_latin_words(current.get("full_name_en"))) < 2:
        return False
    return bool((mrz or {}).get("mrz_valid") or _mrz_check_pass_count(mrz) >= 2 or _quality_ratio(quality) >= 0.5)

def _mrz_fields(mrz: dict | None) -> dict:
    mrz = mrz or {}
    fields = {
        "national_number": mrz.get("mrz_national_id"),
        "full_name_en": mrz.get("mrz_name_full"),
        "date_of_birth": mrz.get("mrz_dob_parsed"),
        "gender": "Male" if mrz.get("mrz_sex") == "M" else "Female" if mrz.get("mrz_sex") == "F" else None,
        "document_number": mrz.get("mrz_doc_number"),
        "expiry_date": mrz.get("mrz_expiry_parsed"),
        "nationality": "Jordanian" if mrz.get("mrz_nationality") == "JOR" else None,
    }
    return {k: v for k, v in fields.items() if _not_empty(v)}


def _ollama_validate(front_path: str, back_path: str | None, current: dict,
                     mrz: dict, quality: dict, enabled: bool) -> dict:
    if not enabled:
        return {}
    if _skip_reliable_ollama_enabled() and _should_skip_ollama(current, mrz, quality):
        return {"_skipped": "reliable_mrz_visual"}
    try:
        import ollama_ocr
        if hasattr(ollama_ocr, "validate_jordan_id_hybrid"):
            return ollama_ocr.validate_jordan_id_hybrid(
                front_path=front_path,
                back_path=back_path,
                extracted=current,
                mrz=mrz,
                quality=quality,
            ) or {}
        raw = ollama_ocr.extract_jordan_id(front_path, back_path)
        return raw.get("data") or raw
    except Exception as exc:
        return {"_error": str(exc)}


_NATIONAL_ID_CONF_REQUIRED = (
    "national_number", "full_name_ar", "full_name_en", "date_of_birth",
    "gender", "nationality", "document_number", "expiry_date", "blood_type",
)


def _filled_ratio(data: dict | None, keys: tuple[str, ...]) -> float:
    if not keys:
        return 0.0
    data = data or {}
    return sum(1 for key in keys if _not_empty(data.get(key))) / len(keys)


def _overall_confidence(field_confs: dict, mrz: dict, quality: dict, data: dict | None = None) -> float:
    ocr_field_score = sum(field_confs.values()) / len(field_confs) if field_confs else 0.0
    filled_score = _filled_ratio(data, _NATIONAL_ID_CONF_REQUIRED) if data is not None else ocr_field_score
    field_score = ocr_field_score if field_confs else min(0.70, filled_score)

    checks = (mrz or {}).get("mrz_checks") or {}
    check_values = [bool(v) for v in checks.values() if isinstance(v, bool)]
    mrz_score = sum(check_values) / len(check_values) if check_values else filled_score
    if (mrz or {}).get("mrz_valid"):
        mrz_score = max(mrz_score, 0.85)

    quality_score = _quality_ratio(quality)
    score = (0.62 * field_score + 0.23 * mrz_score + 0.10 * quality_score + 0.05 * filled_score) * 100
    critical = [field_confs.get(k) for k in ("national_number", "full_name_ar", "full_name_en", "date_of_birth")]
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

def extract_jordan_id(front_path: str, back_path: str | None = None,
                      use_ollama: bool = True, debug: bool = False) -> dict:
    visual_result = national_id_parser.extract_jordan_id(front_path, back_path)
    visual_data = visual_result.get("data") or {}
    mrz = visual_result.get("mrz") or {}
    quality = visual_result.get("quality") or {}
    mrz_data = _mrz_fields(mrz)
    _trace("NID_EASYOCR_VISUAL_FIELDS", visual_data)
    _trace("NID_MRZ_RAW", mrz)
    _trace("NID_MRZ_FIELDS", mrz_data)
    preliminary = {**visual_data, **mrz_data}
    ollama_data = _ollama_validate(front_path, back_path, preliminary, mrz, quality, use_ollama)
    _trace("NID_OLLAMA_FIELDS", ollama_data)

    checks = mrz.get("mrz_checks") or {}
    data: dict[str, Any] = {}
    field_sources: dict[str, dict] = {}
    for field in FIELD_ORDER:
        value, source, conf = arbitrate(
            field,
            mrz_data.get(field),
            visual_data.get(field),
            ollama_data.get(field),
            checks,
            quality,
        )
        if _not_empty(value):
            data[field] = value
            field_sources[field] = {"source": source, "confidence": round(conf, 2)}

    _postprocess_names(data, visual_data, ollama_data, mrz)
    _refresh_name_confidences(data, field_sources)

    field_confs = {k: v["confidence"] for k, v in field_sources.items()}
    _trace("NID_FIELD_SOURCES", field_sources)
    _trace("NID_FIELD_CONFS", field_confs)
    _trace("NID_FINAL_DATA", data)
    confidence = {
        "field_confidence": field_confs,
        "mrz_confidence": round(
            sum(1 for v in checks.values() if v is True) / max(len(checks), 1),
            3,
        ) if checks else 0.0,
        "overall_document_confidence": _overall_confidence(field_confs, mrz, quality, data),
    }
    validation = {
        "mrz_valid": bool(mrz.get("mrz_valid")),
        "mrz_checks": checks,
        "quality_usable": bool(_quality_ratio(quality) >= 0.5),
        "ollama_used": bool(use_ollama and ollama_data and not ollama_data.get("_error") and not ollama_data.get("_skipped")),
        "ollama_error": ollama_data.get("_error"),
    }

    return {
        "success": bool(data),
        "status": "ok" if data else "no_fields",
        "doc_type": "national_id",
        "pipeline": NATIONAL_ID_FLOW,
        "data": data,
        "field_confs": field_confs,
        "field_sources": field_sources,
        "mrz": mrz,
        "visual": {"fields": visual_data, "raw": visual_result if debug else {}},
        "ollama": {"fields": ollama_data},
        "quality": quality,
        "confidence": confidence,
        "validation": validation,
        "accuracy_percentage": confidence["overall_document_confidence"],
        "_source": "mrz+visual+ollama" if validation["ollama_used"] else "mrz+visual",
    }
