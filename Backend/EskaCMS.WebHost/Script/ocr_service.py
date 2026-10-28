
# -*- coding: utf-8 -*-

import json
import os
import re
import shutil
import sys
import tempfile
import traceback
import threading
import time
import concurrent.futures
from email import policy as email_policy
from email.parser import BytesParser
from http.server import BaseHTTPRequestHandler, HTTPServer

# Ensure UTF-8 output on Windows
os.environ["PYTHONIOENCODING"] = "utf-8"
os.environ["PYTHONUTF8"]       = "1"
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

# ── Import EasyOCR parsers (loaded once, heavy) ───────────────────────────────
from passport_parser         import (
    extract_passport         as easyocr_passport,
    _arabic_name_too_short_for_latin,
    _compact_latin_name_key,
    _space_visual_name_with_mrz,
    _split_name_parts_from_full,
    _NAT,
)
from national_id_hybrid_engine import extract_jordan_id      as hybrid_jordan_id
from vehicle_license_parser  import extract_vehicle_license  as easyocr_vehicle
from ocr_engine              import GPU_INFO, repair_arabic_mojibake

# ── Import Ollama path (lightweight — only activates if Ollama is running) ────
import ollama_ocr

# FIX: Probe Ollama in a background thread so a slow/unreachable Ollama server
# does not delay the first request. _OLLAMA_READY is False until the probe
# completes. Any request that arrives before the probe finishes will use EasyOCR.
_OLLAMA_MODEL: str | None = None
_OLLAMA_READY: bool       = False
_OLLAMA_PROBE_DONE        = threading.Event()
_OLLAMA_PROBE_LOCK        = threading.Lock()
_OLLAMA_LAST_PROBE_AT     = 0.0

def _probe_ollama():
    global _OLLAMA_MODEL, _OLLAMA_READY
    try:
        with _OLLAMA_PROBE_LOCK:
            model = ollama_ocr.get_vision_model()
            _OLLAMA_MODEL = model
            _OLLAMA_READY = model is not None
    except Exception as exc:
        print(f"  ⚠️  Ollama probe failed: {exc}", flush=True)
        _OLLAMA_READY = False
    finally:
        _OLLAMA_PROBE_DONE.set()
        status = f"🦙 Ollama ready ({_OLLAMA_MODEL})" if _OLLAMA_READY else "❌ Ollama not available — using EasyOCR"
        print(f"  {status}", flush=True)

threading.Thread(target=_probe_ollama, daemon=True, name="ollama-probe").start()

# OCR requests run in a thread pool so a slow Ollama call never blocks other
# requests. Size = 4 means up to 4 concurrent OCR jobs (adjust to your CPU/GPU).
_OCR_EXECUTOR = concurrent.futures.ThreadPoolExecutor(max_workers=4, thread_name_prefix="ocr")

# Per-request timeout in seconds. If Ollama doesn't respond within this time,
# the request fails with a clear error instead of hanging forever.
_OCR_TIMEOUT_SECS = int(os.environ.get("OCR_TIMEOUT_SECS", "60"))

_TRUE_VALUES = {"1", "true", "yes", "on", "y"}
_FALSE_VALUES = {"0", "false", "no", "off", "n"}


def _truthy(value) -> bool:
    if isinstance(value, bool):
        return value
    if value is None:
        return False
    return str(value).strip().lower() in _TRUE_VALUES


def _env_truthy(*names: str) -> bool:
    for name in names:
        value = os.environ.get(name)
        if value is None:
            continue
        text = value.strip().lower()
        if text in _TRUE_VALUES:
            return True
        if text in _FALSE_VALUES:
            return False
    return False


def _ensure_ollama_ready(wait: bool = True) -> bool:
    """Return true when a supported vision model is available."""
    global _OLLAMA_MODEL, _OLLAMA_READY, _OLLAMA_LAST_PROBE_AT

    if wait and not _OLLAMA_PROBE_DONE.is_set():
        timeout = float(os.environ.get("OCR_OLLAMA_PROBE_WAIT_SECS", "2.5"))
        _OLLAMA_PROBE_DONE.wait(timeout=max(0.0, timeout))

    if _OLLAMA_READY:
        return True

    now = time.monotonic()
    if now - _OLLAMA_LAST_PROBE_AT < 3.0:
        return False

    with _OLLAMA_PROBE_LOCK:
        if _OLLAMA_READY:
            return True
        now = time.monotonic()
        if now - _OLLAMA_LAST_PROBE_AT < 3.0:
            return False
        _OLLAMA_LAST_PROBE_AT = now
        try:
            # get_vision_model caches negative startup probes. Clear that cache
            # before an on-demand retry so Ollama can recover after restart/load.
            ollama_ocr.reset_cache()
            model = ollama_ocr.get_vision_model()
            _OLLAMA_MODEL = model
            _OLLAMA_READY = model is not None
            if _OLLAMA_READY:
                print(f"  🦙 Ollama became ready ({_OLLAMA_MODEL})", flush=True)
        except Exception as exc:
            print(f"  ⚠️  Ollama re-probe failed: {exc}", flush=True)
            _OLLAMA_READY = False
    return _OLLAMA_READY


def _json_default(value):
    if hasattr(value, "item"):
        try:
            return value.item()
        except Exception:
            pass
    if hasattr(value, "tolist"):
        try:
            return value.tolist()
        except Exception:
            pass
    return str(value)


# ── Doc-type normalisation ────────────────────────────────────────────────────

def _normalise_doc_type(value: str | None) -> str:
    value = (value or "").strip().lower().replace("-", "_").replace(" ", "_")
    aliases = {
        "passport":           "passport",
        "passports":          "passport",
        "td3":                "passport",
        "national_id":        "national_id",
        "nationalid":         "national_id",
        "national_id_card":   "national_id",
        "nationalidcard":     "national_id",
        "id":                 "national_id",
        "id_card":            "national_id",
        "identity":           "national_id",
        "jordan_id":          "national_id",
        "jordanian_id":       "national_id",
        "td1":                "national_id",
        "vehicle_license":    "vehicle_license",
        "vehicle":            "vehicle_license",
        "car_license":        "vehicle_license",
        "carlicense":         "vehicle_license",
        "license":            "vehicle_license",
    }
    return aliases.get(value, value)


# ══════════════════════════════════════════════════════════════════════════════
#  RESPONSE HELPERS
# ══════════════════════════════════════════════════════════════════════════════

_PUBLIC_DROP_FIELDS = {
    "name_en_mrz", "name_en_conflict",
    "full_name_en_mrz", "full_name_en_conflict",
    "front_text_ar", "front_text_en",
    "back_text_ar", "back_text_en",
    "front_raw_tokens", "back_raw_tokens",
    "is_expired", "expiry_date_time", "birth_date_time",
}

_ARABIC_SCRIPT_COUNTRY_CODES = {
    "DZA", "BHR", "COM", "DJI", "EGY", "IRQ", "JOR", "KWT",
    "LBN", "LBY", "MRT", "MAR", "OMN", "PSE", "QAT", "SAU",
    "SOM", "SDN", "SYR", "TUN", "ARE", "YEM",
}

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


def normalize_field(value):
    if isinstance(value, dict):
        inner = value.get("value", "")
        while isinstance(inner, dict):
            inner = inner.get("value", "")
        return inner or ""
    return value or ""


def _repair_mrz_name_word(word: str) -> list[str]:
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


def _clean_public_data(data: dict | None) -> dict:
    if not isinstance(data, dict):
        return {}
    clean = {}
    for key, value in data.items():
        if key in _PUBLIC_DROP_FIELDS or key == "conf" or key.startswith("_"):
            continue
        if key in ("full_name_ar", "full_name_en", "name_ar", "name_en") or (isinstance(value, dict) and "value" in value):
            value = normalize_field(value)
        if value is None or value == "" or value == []:
            continue
        if isinstance(value, str):
            value = value.replace(" (MRZ)", "").strip()
            if not value:
                continue
        clean[key] = value
    return clean


_PLATE_CODE_DIGITS = str.maketrans(
    "\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669"
    "\u06F0\u06F1\u06F2\u06F3\u06F4\u06F5\u06F6\u06F7\u06F8\u06F9",
    "01234567890123456789",
)
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
    text = str(plate_number or "").strip().translate(_PLATE_CODE_DIGITS)
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


def _insert_after(data: dict, anchor: str, key: str, value: object) -> None:
    if key in data:
        data[key] = value
        return
    rebuilt = {}
    inserted = False
    for existing_key, existing_value in data.items():
        rebuilt[existing_key] = existing_value
        if existing_key == anchor:
            rebuilt[key] = value
            inserted = True
    if not inserted:
        rebuilt[key] = value
    data.clear()
    data.update(rebuilt)


def _normalize_vehicle_license_public_fields(clean: dict) -> None:
    derived_coding, derived_number_only, normalized_plate = _split_plate_number(clean.get("plate_number"))
    if normalized_plate:
        clean["plate_number"] = normalized_plate

    car_coding = normalize_field(clean.get("car_coding") or clean.get("car_code"))
    if isinstance(car_coding, str):
        car_coding = re.sub(r"[^A-Z0-9]", "", car_coding.translate(_PLATE_CODE_DIGITS).upper())
    if not car_coding:
        car_coding = derived_coding

    plate_number_only = normalize_field(
        clean.get("plate_number_only")
        or clean.get("plate_no_without_code")
        or clean.get("plate_number_without_coding")
    )
    if isinstance(plate_number_only, str):
        plate_number_only = re.sub(r"\D", "", plate_number_only.translate(_PLATE_CODE_DIGITS))
    if not plate_number_only:
        plate_number_only = derived_number_only

    if car_coding:
        clean.pop("car_code", None)
        _insert_after(clean, "plate_number", "car_coding", car_coding)
    if plate_number_only:
        clean.pop("plate_no_without_code", None)
        clean.pop("plate_number_without_coding", None)
        _insert_after(clean, "car_coding" if "car_coding" in clean else "plate_number",
                      "plate_number_only", plate_number_only)


def _passport_mrz_name_parts(mrz: dict | None) -> tuple[str | None, str | None, str | None]:
    line1 = ""
    if isinstance(mrz, dict):
        if mrz.get("name_trustworthy") is False:
            return None, None, None
        line1 = (mrz.get("line1") or mrz.get("mrz_l1") or "").strip()
    if len(line1) < 44 or not line1.startswith("P"):
        return None, None, None

    field = line1[5:44].rstrip("<")
    if not field:
        return None, None, None
    if "<<" not in field:
        return None, None, None
    surname_raw, given_raw = field.split("<<", 1)

    surname = _repair_mrz_name_part(surname_raw)
    given = _repair_mrz_name_part(given_raw)
    full = " ".join(part for part in (given, surname) if part)
    return (full or None), (surname or None), (given or None)


def _passport_has_arabic_name_context(clean: dict) -> bool:
    for key in ("issuing_state", "nationality_code"):
        value = clean.get(key)
        if isinstance(value, str) and value.strip().upper() in _ARABIC_SCRIPT_COUNTRY_CODES:
            return True
    return bool(clean.get("name_ar"))


def _normalize_passport_date(value: str | None) -> str | None:
    if not value:
        return None
    text = re.sub(r"[\u0600-\u06FF]+", " ", str(value)).upper().strip()
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


def _normalize_passport_gender(value: str | None) -> str | None:
    text = str(value or "").strip().upper()
    if text in {"M", "MALE", "MALE/ذكر", "ذكر"}:
        return "Male/ذكر"
    if text in {"F", "FEMALE", "FEMALE/انثى", "FEMALE/أنثى", "انثى", "أنثى"}:
        return "Female/انثى"
    return None if text in {"", "<", "UNSPECIFIED", "NONE", "NULL"} else str(value).strip()


def _clean_passport_place(value: str | None) -> str | None:
    if not value:
        return None
    text = re.sub(r"\s+", " ", str(value)).strip()
    if re.search(r"[A-Za-z]", text):
        parts = []
        for part in text.split():
            parts.append(part.upper() if re.search(r"[A-Za-z]", part) else part)
        return " ".join(parts)
    return text


def _clean_public_arabic_text(value: str | None, *, min_words: int = 1) -> str | None:
    if not value:
        return None
    text = re.sub(r"[A-Za-z0-9_]+", " ", str(value))
    text = re.sub(r"[^\u0600-\u06FF\s]", " ", text)
    text = re.sub(r"[\u064B-\u065F\u0670\u0640]", "", text)
    text = re.sub(r"[\u0623\u0625\u0622\u0671]", "ا", text)
    text = text.replace("ى", "ي")
    words = [word for word in re.split(r"\s+", text.strip()) if len(word) >= 2]
    if len(words) < min_words:
        return None
    return re.sub(r"\s+", " ", " ".join(words)).strip() or None



def _clean_public_latin_name(value: str | None) -> str | None:
    if not value:
        return None
    text = re.sub(r"[\u0600-\u06FF]+", " ", str(value).upper())
    text = re.sub(r"[^A-Z\s'-]", " ", text)
    text = re.sub(r"\s+", " ", text).strip()
    return text or None


def _clean_public_arabic_text_legacy(value: str | None, *, min_words: int = 1) -> str | None:
    if not value:
        return None
    text = repair_arabic_mojibake(str(value))
    text = re.sub(r"[A-Za-z0-9_]+", " ", text)
    text = re.sub(r"[^\u0600-\u06FF\s]", " ", text)
    text = re.sub(r"[\u064B-\u065F\u0670\u0640]", "", text)
    text = re.sub(r"[\u0623\u0625\u0622\u0671]", "\u0627", text)
    text = text.replace("\u0649", "\u064A")
    words = [word for word in re.split(r"\s+", text.strip()) if len(word) >= 2]
    if len(words) < min_words:
        return None
    return re.sub(r"\s+", " ", " ".join(words)).strip() or None


def _complete_truncated_public_words(words: list[str], references: list[str]) -> list[str]:
    out = []
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
        out.append(replacement or word)
    return out


def _words_contained(needles: list[str], haystack: list[str]) -> bool:
    remaining = list(haystack)
    for needle in needles:
        if needle not in remaining:
            return False
        remaining.remove(needle)
    return True


def _national_id_mrz_public_name(mrz: dict | None, visual_name: str | None = None) -> str | None:
    mrz = mrz or {}
    existing = _clean_public_latin_name(mrz.get("mrz_name_full"))
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
    refs = (_clean_public_latin_name(visual_name) or "").split()
    words = _complete_truncated_public_words(given_words + surname_words, refs)
    if len(words) < 2 or any(len(w) > 24 for w in words):
        return None
    return " ".join(words)

def _normalize_national_id_public_fields(clean: dict, mrz: dict | None) -> None:
    if clean.get("national_number"):
        digits = re.sub(r"\D", "", str(clean.get("national_number")))
        if len(digits) > 10:
            digits = digits[-10:]
        if len(digits) == 10:
            clean["national_number"] = digits
        else:
            clean.pop("national_number", None)

    if clean.get("document_number"):
        doc_number = re.sub(r"[^A-Z0-9]", "", str(clean.get("document_number")).upper())
        if re.match(r"^[A-Z0-9]{5,12}$", doc_number):
            clean["document_number"] = doc_number
        else:
            clean.pop("document_number", None)

    if clean.get("full_name_ar"):
        cleaned_ar = _clean_public_arabic_text(clean.get("full_name_ar"), min_words=2)
        if cleaned_ar:
            clean["full_name_ar"] = cleaned_ar
        else:
            clean.pop("full_name_ar", None)
    # Keep the public Arabic name when the parser produced a clean Arabic value.
    # Completeness repair belongs in the OCR/hybrid layer; the API layer should
    # not silently remove Arabic names from otherwise valid national-ID responses.

    if clean.get("full_name_en"):
        latin = re.sub(r"[^A-Za-z\s'-]", " ", str(clean.get("full_name_en"))).strip()
        latin = re.sub(r"\s+", " ", latin).strip().upper()
        if latin:
            clean["full_name_en"] = latin
        else:
            clean.pop("full_name_en", None)

    mrz_name = _national_id_mrz_public_name(mrz, clean.get("full_name_en"))
    if mrz_name:
        current_words = (_clean_public_latin_name(clean.get("full_name_en")) or "").split()
        mrz_words = mrz_name.split()
        if not current_words or (len(mrz_words) > len(current_words) and _words_contained(current_words, mrz_words)):
            clean["full_name_en"] = mrz_name

    if clean.get("gender"):
        gender = _normalize_passport_gender(clean.get("gender"))
        if gender:
            clean["gender"] = gender

    for key in ("date_of_birth", "expiry_date"):
        normalized = _normalize_passport_date(clean.get(key))
        if normalized:
            clean[key] = normalized


def _normalize_passport_public_names(clean: dict, mrz: dict | None) -> None:
    mrz_full, mrz_surname, mrz_given = _passport_mrz_name_parts(mrz)
    internal_name = _clean_public_latin_name(clean.get("full_name_en"))
    if internal_name:
        clean["name_en"] = internal_name

    if clean.get("name_en"):
        fixed_name = _space_visual_name_with_mrz(clean.get("name_en"), mrz_full)
        current_key = _compact_latin_name_key(clean.get("name_en"))
        fixed_key = _compact_latin_name_key(fixed_name)
        # MRZ is excellent for spacing, but a noisy MRZ must not change letters
        # already read correctly from visual OCR/Ollama, e.g. AHMAD -> KAHMAD.
        if fixed_name and fixed_key == current_key:
            clean["name_en"] = fixed_name
    elif mrz_full:
        clean["name_en"] = _space_visual_name_with_mrz(mrz_full, mrz_full) or mrz_full

    name_en = clean.get("name_en")
    mrz_matches_name = bool(
        name_en
        and mrz_full
        and _compact_latin_name_key(name_en) == _compact_latin_name_key(mrz_full)
    )
    if name_en:
        if (
            clean.get("surname")
            and clean.get("given_names")
            and _compact_latin_name_key(f"{clean.get('given_names')} {clean.get('surname')}")
               == _compact_latin_name_key(name_en)
        ):
            surname = clean.get("surname") or ""
            given_names = clean.get("given_names") or ""
        elif mrz_matches_name:
            surname = _space_visual_name_with_mrz(mrz_surname, mrz_surname) or mrz_surname or ""
            given_names = _space_visual_name_with_mrz(mrz_given, mrz_given) or mrz_given or ""
        else:
            surname, given_names = _split_name_parts_from_full(name_en)
        if surname:
            clean["surname"] = surname
        if given_names:
            clean["given_names"] = given_names
    else:
        if mrz_surname:
            clean["surname"] = mrz_surname
        if mrz_given:
            clean["given_names"] = mrz_given

    if clean.get("name_ar") and _arabic_name_too_short_for_latin(clean.get("name_ar"), clean.get("name_en")):
        clean.pop("name_ar", None)

    for key in ("date_of_birth", "issue_date", "expiry_date"):
        normalized = _normalize_passport_date(clean.get(key))
        if normalized:
            clean[key] = normalized
    gender = _normalize_passport_gender(clean.get("gender"))
    if gender:
        clean["gender"] = gender
    elif "gender" in clean:
        clean.pop("gender", None)
    place = _clean_passport_place(clean.get("place_of_birth"))
    if place:
        clean["place_of_birth"] = place


def _normalize_passport_public_aliases(clean: dict) -> None:
    alias_map = {
        "passport_number": "passport_no",
        "full_name_en": "name_en",
        "full_name_ar": "name_ar",
        "date_of_expiry": "expiry_date",
        "date_of_issue": "issue_date",
        "sex": "gender",
        "national_number": "national_no",
    }
    force_aliases = {"full_name_en", "full_name_ar", "date_of_expiry", "date_of_issue"}
    for internal, public in alias_map.items():
        if clean.get(internal) and (internal in force_aliases or not clean.get(public)):
            clean[public] = clean[internal]
    for internal in alias_map:
        clean.pop(internal, None)
    nat = str(clean.get("nationality") or "").strip().upper()
    if re.fullmatch(r"[A-Z]{3}", nat):
        clean.setdefault("nationality_code", nat)
        clean["nationality"] = _NAT.get(nat, nat)
    if clean.get("national_no"):
        digits = re.sub(r"\D", "", str(clean.get("national_no")))
        code = str(clean.get("nationality_code") or clean.get("issuing_state") or "").strip().upper()
        if code == "JOR":
            if len(digits) == 10:
                clean["national_no"] = digits
            else:
                clean.pop("national_no", None)
        elif 6 <= len(digits) <= 14:
            clean["national_no"] = digits
        else:
            clean.pop("national_no", None)


def _filled_ratio(data: dict, keys: tuple[str, ...]) -> float:
    return sum(1 for k in keys if data.get(k)) / len(keys) if keys else 0.0


def _check_ratio(mrz: dict | None, keys: tuple[str, ...]) -> float:
    checks = (mrz or {}).get("mrz_checks") or {}
    values = [bool(checks.get(k)) for k in keys if k in checks]
    if not values:
        return 0.5 if (mrz or {}).get("mrz_valid") else 0.0
    return sum(values) / len(values)


def _quality_ratio(quality: dict | None) -> float:
    if not isinstance(quality, dict):
        return 0.0
    if "front" in quality or "back" in quality:
        sides  = [quality.get("front"), quality.get("back")]
        usable = [1.0 if isinstance(s, dict) and s.get("usable") else 0.0
                  for s in sides]
        return sum(usable) / len(usable)
    return 1.0 if quality.get("usable") else 0.0


def _has_check_signal(mrz: dict | None, keys: tuple[str, ...]) -> bool:
    if not isinstance(mrz, dict):
        return False
    checks = mrz.get("mrz_checks") or {}
    return bool(mrz.get("mrz_valid") or any(k in checks for k in keys))


def _has_quality_signal(quality: dict | None) -> bool:
    if not isinstance(quality, dict):
        return False
    if "front" in quality or "back" in quality:
        return any(isinstance(quality.get(side), dict) for side in ("front", "back"))
    return "usable" in quality


def _accuracy_percentage(data: dict, mrz: dict | None, quality: dict | None, *,
                          required_fields: tuple[str, ...],
                          check_fields:    tuple[str, ...],
                          field_confs: dict | None = None) -> float:
    filled = _filled_ratio(data, required_fields)
    has_checks = _has_check_signal(mrz, check_fields)
    has_quality = _has_quality_signal(quality)
    check_score = _check_ratio(mrz, check_fields) if has_checks else filled
    quality_score = _quality_ratio(quality) if has_quality else filled
    field_confs = field_confs or {}
    conf_values = []
    for field in required_fields:
        if field in field_confs and isinstance(field_confs.get(field), (int, float)):
            conf_values.append(float(field_confs[field]))
        elif data.get(field):
            conf_values.append(0.70)
        else:
            conf_values.append(0.0)
    confidence_score = sum(conf_values) / len(conf_values) if conf_values else filled

    score = (
        0.45 * confidence_score
        + 0.30 * filled
        + 0.15 * check_score
        + 0.10 * quality_score
    ) * 100
    cap = 100.0
    if not has_checks:
        cap = min(cap, 95.0)
    if not has_quality:
        cap = min(cap, 95.0)
    if not has_checks and not has_quality:
        cap = min(cap, 92.0)
    critical_fields = [
        f for f in (
            "national_number", "full_name_ar", "full_name_en",
            "passport_no", "name_en", "name_ar",
            "date_of_birth", "expiry_date",
        )
        if f in required_fields and data.get(f)
    ]
    critical_confs = [
        float(field_confs[f])
        for f in critical_fields
        if isinstance(field_confs.get(f), (int, float))
    ]
    if critical_confs:
        weakest = min(critical_confs)
        if weakest < 0.55:
            cap = min(cap, 72.0)
        elif weakest < 0.70:
            cap = min(cap, 84.0)
        elif weakest < 0.80:
            cap = min(cap, 89.0)
    return round(max(0.0, min(cap, score)), 2)

def _looks_like_date(value: str | None) -> bool:
    return bool(value and re.match(r"^\d{2}/\d{2}/\d{4}$", str(value).strip()))


def _looks_like_passport_no(value: str | None) -> bool:
    return bool(value and re.match(r"^[A-Z0-9]{6,9}$", str(value).strip().upper()))


def _passport_source_score(field: str, value, source: str,
                           easy: dict | None, ollama: dict | None,
                           quality: dict | None) -> float:
    if value in (None, "", [], {}, "null", "None", "..."):
        return 0.0

    checks = ((easy or {}).get("mrz") or {}).get("mrz_checks") or {}
    easy_confs = (easy or {}).get("field_confs") or {}
    blur = (quality or {}).get("blur", 999)
    usable = (quality or {}).get("usable", True)

    if source == "mrz_visual":
        score = float(easy_confs.get(field, 0.0) or 0.0)
        check_map = {
            "passport_no": "passport_no",
            "date_of_birth": "dob",
            "expiry_date": "expiry",
            "name_en": "composite",
            "surname": "composite",
            "given_names": "composite",
            "nationality": "composite",
            "nationality_code": "composite",
            "national_no": "composite",
            "gender": "composite",
        }
        check_key = check_map.get(field)
        if check_key and checks.get(check_key):
            score = max(score, 0.97)
        elif check_key:
            score = max(score, 0.20)
        if field == "passport_no" and _looks_like_passport_no(str(value)):
            score = max(score, 0.72)
        if field in ("date_of_birth", "expiry_date", "issue_date") and _looks_like_date(str(value)):
            score = max(score, 0.65)
        return min(score, 1.0)

    if source == "ollama":
        score = 0.75
        if field in ("name_en", "name_ar", "place_of_birth", "nationality"):
            score = 0.82
        if field == "passport_no" and not _looks_like_passport_no(str(value)):
            score *= 0.20
        if field in ("date_of_birth", "expiry_date", "issue_date") and not _looks_like_date(str(value)):
            score *= 0.25
        if not usable:
            score *= 0.70
        elif blur < 100:
            score *= 0.80
        return min(score, 1.0)

    return 0.0


def _merge_passport_sources(easy: dict, ollama: dict | None = None) -> dict:
    """
    Notebook-style passport merge: EasyOCR supplies visual+MRZ, Ollama supplies
    vision semantics, and each public field picks the strongest source.
    """
    easy_passport = (easy or {}).get("passport") or {}
    ollama_passport = (ollama or {}).get("passport") or {}
    quality = (easy or {}).get("quality") or (ollama or {}).get("quality")

    fields = (
        "document_type", "issuing_state", "passport_no", "name_en", "name_ar",
        "surname", "given_names", "nationality", "nationality_code",
        "national_no", "gender", "date_of_birth", "issue_date",
        "expiry_date", "place_of_birth",
    )

    merged = {}
    field_sources = {}
    for field in fields:
        candidates = []
        easy_value = normalize_field(easy_passport.get(field))
        ollama_value = normalize_field(ollama_passport.get(field))
        if easy_value:
            candidates.append((
                _passport_source_score(field, easy_value, "mrz_visual", easy, ollama, quality),
                "mrz_visual",
                easy_value,
            ))
        if ollama_value:
            candidates.append((
                _passport_source_score(field, ollama_value, "ollama", easy, ollama, quality),
                "ollama",
                ollama_value,
            ))
        if not candidates:
            continue
        score, source, value = max(candidates, key=lambda item: item[0])
        if score <= 0:
            continue
        merged[field] = value
        field_sources[field] = {"source": source, "confidence": round(score, 2)}

    # Keep MRZ-derived decomposition stable only when the MRZ name structure is trustworthy.
    mrz = (easy or {}).get("mrz") or (ollama or {}).get("mrz")
    mrz_name_trustworthy = not isinstance(mrz, dict) or mrz.get("name_trustworthy") is not False
    for key in ("surname", "given_names"):
        if mrz_name_trustworthy and easy_passport.get(key):
            merged[key] = easy_passport[key]
            field_sources[key] = {"source": "mrz_visual", "confidence": 0.97}

    return {
        "passport": merged,
        "field_confs": {k: v["confidence"] for k, v in field_sources.items()},
        "field_sources": field_sources,
        "mrz": mrz,
        "quality": quality,
        "model": (ollama or {}).get("model"),
    }


def _has_national_id_back_signal(clean: dict, mrz: dict | None) -> bool:
    if any(clean.get(k) for k in ("document_number", "expiry_date", "blood_type")):
        return True
    mrz = mrz or {}
    if any(mrz.get(k) for k in ("mrz_l1", "mrz_l2", "mrz_l3", "line1", "line2", "line3")):
        return True
    checks = mrz.get("mrz_checks") or {}
    return bool(checks)


def _passport_required_fields(clean: dict) -> tuple[str, ...]:
    required = ["passport_no", "name_en", "date_of_birth", "expiry_date", "gender", "nationality"]
    issuing_state = str(clean.get("issuing_state") or "").strip().upper()
    nationality_code = str(clean.get("nationality_code") or "").strip().upper()
    if clean.get("national_no") or issuing_state == "JOR" or nationality_code == "JOR":
        required.append("national_no")
    if _passport_has_arabic_name_context(clean):
        required.append("name_ar")
    return tuple(required)


def _national_id_required_and_checks(clean: dict, mrz: dict | None) -> tuple[tuple[str, ...], tuple[str, ...]]:
    required = [
        "national_number", "full_name_ar", "full_name_en",
        "date_of_birth", "gender", "nationality",
        "place_of_birth", "mother_name",
    ]
    if _has_national_id_back_signal(clean, mrz):
        required.extend(["expiry_date", "document_number", "blood_type"])
        return tuple(required), ("doc_number", "dob", "expiry", "composite")
    return tuple(required), ()


def _stable_image_copy(image_path: str) -> str:
    """
    Copy caller-owned temp images before running multi-pass OCR.
    Some host apps clean OCRTemp files while the request is still being parsed;
    EasyOCR may have already loaded the file, but Ollama reopens it later.
    """
    ext = os.path.splitext(image_path)[1] or ".jpg"
    handle = tempfile.NamedTemporaryFile(prefix="passport_ocr_", suffix=ext, delete=False)
    stable_path = handle.name
    handle.close()
    shutil.copy2(image_path, stable_path)
    return stable_path


def _public_response(doc_type: str, data: dict,
                     mrz: dict | None, quality: dict | None,
                     engine: str = "easyocr",
                     model:  str | None = None,
                     field_confs: dict | None = None,
                     accuracy_percentage: float | int | None = None) -> dict:
    clean = _clean_public_data(data)

    if doc_type == "passport":
        _normalize_passport_public_aliases(clean)
        _normalize_passport_public_names(clean, mrz)
        if not _passport_has_arabic_name_context(clean):
            clean.pop("name_ar", None)
        required = _passport_required_fields(clean)
        checks   = ("passport_no", "dob", "expiry", "composite")
    elif doc_type == "vehicle_license":
        _normalize_vehicle_license_public_fields(clean)
        required = ("plate_number", "registration_number", "chassis_number")
        checks   = ()
    else:  # national_id
        _normalize_national_id_public_fields(clean, mrz)
        required, checks = _national_id_required_and_checks(clean, mrz)

    computed_accuracy = _accuracy_percentage(clean, mrz, quality,
                                             required_fields=required,
                                             check_fields=checks,
                                             field_confs=field_confs)
    try:
        accuracy = round(float(accuracy_percentage), 2) if accuracy_percentage is not None else computed_accuracy
    except (TypeError, ValueError):
        accuracy = computed_accuracy
    resp = {
        "success":             bool(clean),
        "doc_type":            doc_type,
        "engine":              engine,
        "accuracy_percentage": accuracy,
        "data":                clean,
    }
    if model:
        resp["model"] = model

    # FIX: Include a debug warning in the response when Ollama returned no data,
    # instead of silently returning {"data": {}, "accuracy_percentage": 0}.
    # This makes it immediately obvious that extraction failed vs. returned empty fields.
    if engine == "ollama" and not clean:
        resp["warning"] = (
            "Ollama returned no extractable fields. "
            "Check ollama_ocr logs. The model may have failed to produce JSON output."
        )

    try:
        print(
            "[OCR_TRACE] API_RESPONSE: "
            + json.dumps(
                {
                    "doc_type": doc_type,
                    "engine": engine,
                    "accuracy_percentage": accuracy,
                    "model": model,
                    "data": clean,
                },
                ensure_ascii=False,
                sort_keys=True,
                default=str,
            ),
            flush=True,
        )
    except Exception:
        print(f"[OCR_TRACE] API_RESPONSE: {resp}", flush=True)

    return resp


# ══════════════════════════════════════════════════════════════════════════════
#  REQUEST HANDLER
# ══════════════════════════════════════════════════════════════════════════════

class OCRHandler(BaseHTTPRequestHandler):

    # ── GET /health ───────────────────────────────────────────────────────────
    def do_GET(self):
        if self.path == "/health":
            self._send({
                "status":  "ok",
                "readers": "loaded",
                "gpu":     GPU_INFO,
                "ollama": {
                    "available":   _OLLAMA_READY,
                    "model":       _OLLAMA_MODEL,
                    "probe_done":  _OLLAMA_PROBE_DONE.is_set(),
                },
            }, 200)
        else:
            self._send({"error": "Not found"}, 404)

    # ── POST /ocr  /ocr/passport  /ocr/id  /ocr/vehicle ──────────────────────
    def do_POST(self):
        temp_files = []
        try:
            body, temp_files = self._read_body()
        except ValueError as e:
            self._send({"success": False, "error": str(e)}, 400)
            return

        path = self.path.rstrip("/")

        if path == "/health":
            self._send({"status": "ok", "gpu": GPU_INFO}, 200)
            return

        valid_paths = ("/ocr", "/ocr/passport", "/ocr/id", "/ocr/vehicle")
        if path not in valid_paths:
            self._send({"error": f"Unknown endpoint: {path}"}, 404)
            return

        # FIX: Run OCR in the thread pool with a hard timeout.
        # This prevents a hung Ollama call from blocking the HTTP server thread
        # and making ALL subsequent requests time out on the client side.
        future = _OCR_EXECUTOR.submit(self._dispatch, path, body)
        try:
            result = future.result(timeout=_OCR_TIMEOUT_SECS)
        except concurrent.futures.TimeoutError:
            future.cancel()
            result = {
                "success": False,
                "error":   f"OCR timed out after {_OCR_TIMEOUT_SECS}s. "
                           "Ollama may be loading the model or is overloaded.",
            }
        except FileNotFoundError as e:
            result = {"success": False, "error": str(e)}
        except Exception as e:
            traceback.print_exc()
            result = {"success": False, "error": str(e)}
        finally:
            for p in temp_files:
                try:
                    if os.path.exists(p):
                        os.remove(p)
                except OSError:
                    pass

        try:
            self._send(result, 200)
        except (BrokenPipeError, ConnectionResetError, OSError):
            print("[WARN] Client disconnected before response could be sent.")

    # ── Body parsing ──────────────────────────────────────────────────────────
    def _read_body(self) -> tuple[dict, list[str]]:
        length       = int(self.headers.get("Content-Length", 0))
        raw          = self.rfile.read(length) if length else b""
        content_type = self.headers.get("Content-Type", "")

        if "multipart/form-data" in content_type.lower():
            return self._read_multipart_body(raw, content_type)

        try:
            return json.loads(raw or b"{}"), []
        except (json.JSONDecodeError, ValueError) as e:
            raise ValueError(f"Bad JSON: {e}") from e

    def _read_multipart_body(self, raw: bytes,
                             content_type: str) -> tuple[dict, list[str]]:
        header = (
            f"Content-Type: {content_type}\r\n"
            "MIME-Version: 1.0\r\n\r\n"
        ).encode("utf-8")
        msg             = BytesParser(policy=email_policy.default).parsebytes(header + raw)
        body: dict      = {}
        temp_files:     list[str] = []
        positional_files: list[str] = []

        for part in msg.iter_parts():
            disposition = part.get("Content-Disposition", "")
            if "form-data" not in disposition:
                continue
            name     = part.get_param("name", header="content-disposition")
            filename = part.get_filename()
            payload  = part.get_payload(decode=True) or b""
            if not name:
                continue

            if filename:
                if not payload:
                    continue
                saved = self._save_uploaded_file(filename, payload)
                temp_files.append(saved)
                positional_files.append(saved)
                self._assign_uploaded_file(body, name, saved)
            else:
                charset = part.get_content_charset() or "utf-8"
                text    = payload.decode(charset, errors="ignore").strip()
                if text:
                    body[name] = text

        if "image_paths" not in body and positional_files:
            body["image_paths"] = positional_files
        if "front_path" not in body and len(positional_files) >= 1:
            body["front_path"] = positional_files[0]
        if "back_path" not in body and len(positional_files) >= 2:
            body["back_path"] = positional_files[1]
        if "image_path" not in body and len(positional_files) == 1:
            body["image_path"] = positional_files[0]

        return body, temp_files

    @staticmethod
    def _save_uploaded_file(filename: str, payload: bytes) -> str:
        _, ext = os.path.splitext(filename)
        if not ext or len(ext) > 8:
            ext = ".jpg"
        fd, path = tempfile.mkstemp(prefix="ocr_upload_", suffix=ext)
        with os.fdopen(fd, "wb") as f:
            f.write(payload)
        return path

    @staticmethod
    def _assign_uploaded_file(body: dict, name: str, path: str) -> None:
        key = name.strip().lower().replace("-", "_")
        if key in {"front", "frontfile", "front_file", "frontimage", "front_image"}:
            body["front_path"] = path
        elif key in {"back", "backfile", "back_file", "backimage", "back_image"}:
            body["back_path"] = path
        elif key in {"file", "image", "passport", "passportimage", "passport_image"}:
            body["image_path"] = path
        else:
            body.setdefault("image_paths", []).append(path)

    # ── Engine selection ──────────────────────────────────────────────────────
    @staticmethod
    def _use_ollama(body: dict) -> bool:
        """
        Use Ollama when the caller/appsetting requests it and a vision model is
        reachable. This keeps appsetting control, while avoiding a startup race
        where the first request arrives before the background probe finishes.

        Opt-out options (any truthy value):
            { "force_easyocr": true }   — always use EasyOCR
            { "engine": "easyocr" }     — explicit engine selection
        """
        if _truthy(body.get("force_easyocr")):
            return False

        engine = str(body.get("engine", "")).strip().lower()
        if engine in {"easyocr", "mrz", "visual", "mrz+visual", "fast"}:
            return False

        explicit_engine = engine in {"ollama", "mrz+visual+ollama", "hybrid_ollama", "vision"}
        explicit_flag = any(
            _truthy(body.get(key))
            for key in ("use_ollama", "ollama", "ollama_enabled", "useOllama", "OllamaEngine")
        )
        env_default = _env_truthy(
            "OCR_USE_OLLAMA_DEFAULT",
            "OCR_USE_OLLAMA",
            "OCR_OLLAMA_ENABLED",
            "OLLAMA_ENABLED",
        )

        if not (explicit_engine or explicit_flag or env_default):
            return False

        if _ensure_ollama_ready(wait=True):
            return True

        print(
            "  ⚠️  Ollama was requested but no supported vision model is ready; "
            "falling back to MRZ+visual.",
            flush=True,
        )
        return False
    # ── Routing ───────────────────────────────────────────────────────────────
    def _dispatch(self, path: str, body: dict) -> dict:

        # Determine document type
        if path == "/ocr/passport":
            doc_type = "passport"
        elif path == "/ocr/id":
            doc_type = "national_id"
        elif path == "/ocr/vehicle":
            doc_type = "vehicle_license"
        else:
            doc_type = _normalise_doc_type(
                body.get("doc_type")
                or body.get("document_type")
                or body.get("type")
                or body.get("documentType")
            )
            if doc_type not in ("passport", "national_id", "vehicle_license"):
                # Heuristic auto-detect from payload shape
                if "front_path" in body and "back_path" in body:
                    doc_type = "national_id"
                elif "image_path" in body:
                    doc_type = "passport"
                else:
                    return {
                        "success": False,
                        "error": (
                            "Specify doc_type ('passport', 'national_id', or 'vehicle_license') "
                            "or use /ocr/passport, /ocr/id, or /ocr/vehicle endpoint."
                        ),
                    }

        use_ollama = self._use_ollama(body)
        engine_tag = "mrz+visual+ollama" if use_ollama else "mrz+visual"
        print(f"  ⚙️  Engine: {engine_tag} | doc_type: {doc_type}")

        # ── Passport ──────────────────────────────────────────────────────────
        if doc_type == "passport":
            paths      = body.get("image_paths") or []
            image_path = (
                body.get("image_path") or body.get("imagePath")
                or body.get("ImagePath") or body.get("file_path")
                or (paths[0] if paths else None)
            )
            if not image_path:
                return {"success": False, "error": "Missing image_path"}
            if not os.path.exists(image_path):
                raise FileNotFoundError(f"Image not found: {image_path!r}")

            stable_path = _stable_image_copy(image_path)
            result = easyocr_passport(stable_path, use_ollama=use_ollama)
            return _public_response(
                "passport",
                result.get("data") or result.get("passport") or {},
                result.get("mrz"),
                result.get("quality"),
                engine=result.get("_source") or ("mrz+visual+ollama" if use_ollama else "mrz+visual"),
                model=result.get("model") or (_OLLAMA_MODEL if use_ollama else None),
                field_confs=result.get("field_confs") or {},
                accuracy_percentage=result.get("accuracy_percentage")
                    or (result.get("confidence") or {}).get("overall_document_confidence"),
            )

        # ── National ID ───────────────────────────────────────────────────────
        elif doc_type == "national_id":
            paths      = body.get("image_paths") or []
            front_path = (
                body.get("front_path") or body.get("frontPath")
                or body.get("FrontPath") or body.get("front")
                or (paths[0] if len(paths) > 0 else None)
            )
            back_path  = (
                body.get("back_path") or body.get("backPath")
                or body.get("BackPath") or body.get("back")
                or (paths[1] if len(paths) > 1 else None)
            )

            if not front_path:
                return {"success": False, "error": "National ID requires front_path"}

            for p in filter(None, (front_path, back_path)):
                if not os.path.exists(p):
                    raise FileNotFoundError(f"Image not found: {p!r}")

            r = hybrid_jordan_id(front_path, back_path, use_ollama=use_ollama)
            return _public_response("national_id",
                                    r.get("data") or {},
                                    r.get("mrz"),
                                    r.get("quality"),
                                    engine=r.get("_source") or ("mrz+visual+ollama" if use_ollama else "mrz+visual"),
                                    model=r.get("model") or (_OLLAMA_MODEL if use_ollama else None),
                                    field_confs=r.get("field_confs") or {},
                                    accuracy_percentage=r.get("accuracy_percentage")
                                        or (r.get("confidence") or {}).get("overall_document_confidence"))

        # ── Vehicle License ───────────────────────────────────────────────────
        else:
            paths      = body.get("image_paths") or []
            front_path = (
                body.get("front_path") or body.get("frontPath")
                or body.get("FrontPath") or body.get("front")
                or (paths[0] if len(paths) > 0 else None)
            )
            back_path  = (
                body.get("back_path") or body.get("backPath")
                or body.get("BackPath") or body.get("back")
                or (paths[1] if len(paths) > 1 else None)
            )
            if not front_path or not back_path:
                return {"success": False,
                        "error": "Vehicle license requires front_path and back_path"}
            for p in (front_path, back_path):
                if not os.path.exists(p):
                    raise FileNotFoundError(f"Image not found: {p!r}")

            if use_ollama:
                r = ollama_ocr.extract_vehicle_license(front_path, back_path)
                return _public_response("vehicle_license",
                                        r.get("data") or {},
                                        None,
                                        r.get("quality"),
                                        engine="ollama",
                                        model=r.get("model"))
            else:
                r = easyocr_vehicle(front_path, back_path)
                return _public_response("vehicle_license",
                                        r.get("data") or {},
                                        None,
                                        r.get("quality"),
                                        engine="easyocr")

    # ── HTTP helpers ──────────────────────────────────────────────────────────
    def _send(self, payload: dict, status: int):
        body = json.dumps(payload, ensure_ascii=False, indent=2, default=_json_default).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)
        self.wfile.flush()

    def log_message(self, fmt, *args):
        print(f"[HTTP] {self.address_string()} — {fmt % args}", flush=True)


# ══════════════════════════════════════════════════════════════════════════════
#  ENTRY POINT
# ══════════════════════════════════════════════════════════════════════════════

if __name__ == "__main__":
    HOST, PORT = "127.0.0.1", 5050
    server = HTTPServer((HOST, PORT), OCRHandler)
    print(f"🚀  OCR service listening on http://{HOST}:{PORT}", flush=True)
    print(f"    POST /ocr/passport  — passport (TD3 / ICAO MRZ)", flush=True)
    print(f"    POST /ocr/id        — Jordanian national ID (TD1)", flush=True)
    print(f"    POST /ocr/vehicle   — vehicle license card", flush=True)
    print(f"    POST /ocr           — auto-detect (pass doc_type)", flush=True)
    print(f"    GET  /health        — liveness probe", flush=True)
    print(f"", flush=True)
    print(f"    Active engine: probing Ollama in background...", flush=True)
    print(f"    (pass force_easyocr=true in body to override per-request)", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n🛑  Server stopped.", flush=True)
    finally:
        _OCR_EXECUTOR.shutdown(wait=False)
