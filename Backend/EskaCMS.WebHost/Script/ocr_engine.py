
# -*- coding: utf-8 -*-

import os
import contextlib
import io
import re
import sys
import unicodedata
from datetime import date
from typing import Any

import cv2
import numpy as np
import easyocr
import torch
from contextlib import contextmanager

@contextmanager
def _silence():
    old_stdout = sys.stdout
    old_stderr = sys.stderr
    try:
        sys.stdout = open(os.devnull, "w", encoding="utf-8")
        sys.stderr = open(os.devnull, "w", encoding="utf-8")
        yield
    finally: 
        sys.stdout = old_stdout
        sys.stderr = old_stderr

_AR_INDIC = str.maketrans(
    "\u0660\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669",
    "0123456789",
)


def _normalize_digits(text: str) -> str:
    return text.translate(_AR_INDIC)


def normalize_digits(text: str) -> str:
    """Notebook-compatible public alias for Arabic-Indic digit normalization."""
    return _normalize_digits(text or "")

def _env_flag(name: str, default: bool = True) -> bool:
    value = os.getenv(name)
    if value is None:
        return default
    return value.strip().lower() not in ("0", "false", "no", "off")


def _detect_gpu() -> dict:
    requested = _env_flag("OCR_USE_GPU", True)
    cuda_available = bool(torch.cuda.is_available())
    device_count = int(torch.cuda.device_count()) if cuda_available else 0
    device_name = torch.cuda.get_device_name(0) if cuda_available and device_count else None
    return {
        "requested": requested,
        "enabled": bool(requested and cuda_available and device_count > 0),
        "cuda_available": cuda_available,
        "cuda_version": torch.version.cuda,
        "device_count": device_count,
        "device_name": device_name,
        "torch_version": torch.__version__,
    }


GPU_INFO = _detect_gpu()


print(f"  Loading EasyOCR readers (gpu={GPU_INFO['enabled']})...", flush=True)
with _silence():
    READER_MIX = easyocr.Reader(["ar", "en"], gpu=GPU_INFO["enabled"], verbose=False)
    READER_EN  = easyocr.Reader(["en"],        gpu=GPU_INFO["enabled"], verbose=False)
print(f"  EasyOCR ready. GPU: {GPU_INFO}", flush=True)

MRZ_ALLOWLIST = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<"
ARABIC_RE     = re.compile(r"[\u0600-\u06FF]")
SNAP          = 12   # pixel grid for deduplication


def repair_arabic_mojibake(text: str | None) -> str:
    """
    Repair UTF-8 Arabic that was accidentally decoded as Latin-1/CP1252.
    Normal Arabic and English are returned unchanged.
    """
    if text is None:
        return ""
    value = str(text)
    if not re.search(r"[\u00D8\u00D9\u00DA\u00DB]", value):
        return value

    best = value
    best_arabic = len(ARABIC_RE.findall(best))
    for encoding in ("latin1", "cp1252"):
        try:
            repaired = value.encode(encoding).decode("utf-8")
        except UnicodeError:
            continue
        arabic_count = len(ARABIC_RE.findall(repaired))
        if arabic_count > best_arabic:
            best = repaired
            best_arabic = arabic_count
    return best


#  IMAGE UTILITIES

def load_and_resize(path: str, max_width: int = 1400, min_width: int = 900) -> np.ndarray:
    img = cv2.imread(path)
    if img is None:
        raise ValueError(f"Cannot load image: {path!r}")
    h, w = img.shape[:2]
    if w > max_width:
        img = cv2.resize(img, (max_width, int(h * max_width / w)), interpolation=cv2.INTER_AREA)
    elif w < min_width:
        img = cv2.resize(img, (min_width, int(h * min_width / w)), interpolation=cv2.INTER_CUBIC)
    return img


def compute_quality(gray: np.ndarray) -> dict:
    if len(gray.shape) == 3:
        gray = cv2.cvtColor(gray, cv2.COLOR_BGR2GRAY)
    blur       = float(cv2.Laplacian(gray, cv2.CV_64F).var())
    brightness = float(np.mean(gray) / 255.0)
    contrast   = float(np.std(gray)  / 255.0)
    return {
        "blur":       round(blur, 2),
        "brightness": round(brightness, 3),
        "contrast":   round(contrast, 3),
        "usable":     blur >= 40.0 and 0.10 <= brightness <= 0.95 and contrast >= 0.08,
    }


def _ensure_bgr(image: np.ndarray) -> np.ndarray:
    if image is None or image.size == 0:
        return image
    if len(image.shape) == 2:
        return cv2.cvtColor(image, cv2.COLOR_GRAY2BGR)
    if image.shape[2] == 4:
        return cv2.cvtColor(image, cv2.COLOR_BGRA2BGR)
    return image


def _resize_lanczos(image: np.ndarray, scale: int = 2) -> np.ndarray:
    scale = max(1, int(scale or 1))
    if scale == 1 or image is None or image.size == 0:
        return image
    return cv2.resize(image, None, fx=scale, fy=scale, interpolation=cv2.INTER_LANCZOS4)


def _denoise_gray(gray: np.ndarray, h: int = 10) -> np.ndarray:
    return cv2.fastNlMeansDenoising(gray, None, h=h, templateWindowSize=7, searchWindowSize=21)


def _clahe_gray(gray: np.ndarray, clip: float = 1.8, tile: tuple[int, int] = (8, 8)) -> np.ndarray:
    clahe = cv2.createCLAHE(clipLimit=float(clip), tileGridSize=tile)
    return clahe.apply(gray)


def _clahe_luminance(image: np.ndarray, clip: float = 1.8, tile: tuple[int, int] = (8, 8)) -> np.ndarray:
    bgr = _ensure_bgr(image)
    lab = cv2.cvtColor(bgr, cv2.COLOR_BGR2LAB)
    l, a, b = cv2.split(lab)
    l = _clahe_gray(l, clip=clip, tile=tile)
    return cv2.cvtColor(cv2.merge((l, a, b)), cv2.COLOR_LAB2BGR)


def _unsharp(gray: np.ndarray, amount: float = 1.55, sigma: float = 1.2) -> np.ndarray:
    blur = cv2.GaussianBlur(gray, (0, 0), sigmaX=sigma)
    sharp = cv2.addWeighted(gray, amount, blur, 1.0 - amount, 0)
    return np.clip(sharp, 0, 255).astype(np.uint8)


def _adaptive_threshold(gray: np.ndarray, block: int = 35, c: int = 11) -> np.ndarray:
    block = max(15, int(block) | 1)
    return cv2.adaptiveThreshold(
        gray, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY, block, c
    )


def _mrz_safe_variant(image: np.ndarray) -> np.ndarray:
    bgr = _ensure_bgr(image)
    gray = cv2.cvtColor(bgr, cv2.COLOR_BGR2GRAY)
    gray = _resize_lanczos(gray, scale=2)
    denoised = _denoise_gray(gray, h=8)
    enhanced = _clahe_gray(denoised, clip=1.6, tile=(16, 16))
    return _unsharp(enhanced, amount=1.35, sigma=1.0)


def build_variants(img: np.ndarray) -> dict[str, np.ndarray]:
    """Build non-destructive OCR variants for text, Arabic, and MRZ."""
    img = _ensure_bgr(img)
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

    denoised = _denoise_gray(gray, h=10)
    enhanced = _clahe_gray(denoised, clip=1.2, tile=(16, 16))
    sharpened = _unsharp(enhanced, amount=1.45, sigma=1.0)
    high_contrast = cv2.convertScaleAbs(enhanced, alpha=1.22, beta=0)
    thresholded = _adaptive_threshold(enhanced, block=31, c=10)

    color_clahe = _clahe_luminance(img, clip=1.5, tile=(8, 8))
    color_denoised = cv2.fastNlMeansDenoisingColored(
        color_clahe, None, h=4, hColor=4, templateWindowSize=7, searchWindowSize=21
    )

    variants = {
        "original": img,
        "gray": gray,
        "denoised": denoised,
        "enhanced": enhanced,
        "thresholded": thresholded,
        "sharpened": sharpened,
        "high_contrast": high_contrast,
        "color_clahe": color_clahe,
        "color_denoised": color_denoised,
        "mrz": _mrz_safe_variant(img),
    }
    if img.shape[0] * img.shape[1] <= 700_000:
        variants["arabic"] = enhance_id_image(img, scale=3, threshold=False)
        variants["arabic_threshold"] = enhance_id_image(img, scale=3, threshold=True)
    return variants


def normalize_field(value):
    if isinstance(value, dict):
        inner = value.get("value", "")
        while isinstance(inner, dict):
            inner = inner.get("value", "")
        return inner or ""
    return value or ""


def normalize_arabic_text(text: str | None) -> str:
    if not text:
        return ""
    text = unicodedata.normalize("NFKC", repair_arabic_mojibake(str(text)))
    text = re.sub(r"[\u064B-\u065F\u0670\u0640]", "", text)
    text = re.sub(r"[\u0623\u0625\u0622\u0671]", "\u0627", text)
    text = text.replace("\u0649", "\u064a")
    text = re.sub(r"[أإآٱ]", "ا", text)
    text = text.replace("ى", "ي")
    text = re.sub(r"\s+", " ", text).strip()
    return text


def enhance_id_image(image: np.ndarray, scale: int = 3, threshold: bool = False) -> np.ndarray:
    if image is None or image.size == 0:
        return image

    bgr = _ensure_bgr(image)
    upscaled = _resize_lanczos(bgr, scale=scale)
    gray = cv2.cvtColor(upscaled, cv2.COLOR_BGR2GRAY)

    denoised = _denoise_gray(gray, h=9)
    enhanced = _clahe_gray(denoised, clip=2.0, tile=(8, 8))
    sharpened = _unsharp(enhanced, amount=1.55, sigma=1.1)

    if threshold:
        return _adaptive_threshold(sharpened, block=35, c=11)
    return sharpened


def build_arabic_ocr_variants(image: np.ndarray) -> dict[str, np.ndarray]:
    enhanced = enhance_id_image(image, scale=3, threshold=False)
    sharpened = _unsharp(enhanced, amount=1.7, sigma=1.0)
    high_contrast = cv2.convertScaleAbs(enhanced, alpha=1.28, beta=0)
    adaptive_threshold = enhance_id_image(image, scale=3, threshold=True)
    gray = cv2.cvtColor(_ensure_bgr(image), cv2.COLOR_BGR2GRAY)
    original_upscaled = _resize_lanczos(gray, scale=3)
    return {
        "original": original_upscaled,
        "enhanced": enhanced,
        "sharpened": sharpened,
        "high_contrast": high_contrast,
        "adaptive_threshold": adaptive_threshold,
    }


def _region_metrics(source: np.ndarray, x0: float, y0: float,
                    x1: float, y1: float) -> tuple[float, float, float]:
    h, w = source.shape[:2]
    crop = source[int(h*y0):int(h*y1), int(w*x0):int(w*x1)]
    if crop.size == 0:
        return 1.0, 1.0, 1000.0
    return (float(np.mean(crop)/255), float(np.std(crop)/255),
            float(cv2.Laplacian(crop, cv2.CV_64F).var()))


def select_source(variants: dict, x0=0.0, y0=0.0, x1=1.0, y1=1.0,
                  mode: str = "text") -> np.ndarray:
    """Pick the best preprocessed variant for a given region and task."""
    brightness, contrast, blur = _region_metrics(variants["enhanced"], x0, y0, x1, y1)
    if mode == "mrz":
        return variants["mrz"] if "mrz" in variants else variants["enhanced"]
    if mode == "arabic":
        if blur < 65.0 or contrast < 0.10:
            return variants["arabic"] if "arabic" in variants else variants["sharpened"]
        return variants["sharpened"] if "sharpened" in variants else variants["enhanced"]
    if contrast < 0.08:
        # Keep normal visual OCR on grayscale/color-enhanced sources. Adaptive
        # thresholding is useful for MRZ but can erase faint blue/green security
        # text and Arabic dots on many IDs/passports.
        return variants.get("color_denoised") or variants.get("high_contrast") or variants["enhanced"]
    if blur < 42.0:
        return variants["sharpened"]
    return variants["enhanced"]



def ocr_to_tokens(img: np.ndarray, reader=None, allowlist: str | None = None,
                  min_conf: float = 0.05) -> list[dict]:
    r = reader or READER_MIX
    canvas_size = int(os.environ.get("OCR_EASYOCR_CANVAS_SIZE", "4096"))
    mag_ratio = float(os.environ.get("OCR_EASYOCR_MAG_RATIO", "1.2"))
    with _silence():
        # raw = r.readtext(img, detail=1, paragraph=False, allowlist=allowlist)
        raw = r.readtext(
            img,
            detail=1,
            paragraph=False,
            allowlist=allowlist,
            canvas_size=canvas_size,
            mag_ratio=mag_ratio,
        )
    deduped: dict = {}
    for bbox, text, conf in raw:
        text = repair_arabic_mojibake(text).strip()
        if not text or conf < min_conf:
            continue
        xs = [p[0] for p in bbox]; ys = [p[1] for p in bbox]
        cx = int(round(((min(xs)+max(xs))/2)/SNAP)*SNAP)
        cy = int(round(((min(ys)+max(ys))/2)/SNAP)*SNAP)
        key = (text.lower(), cx, cy)
        tok = {
            "text": text, "conf": round(conf, 3),
            "x": min(xs), "y": min(ys), "x2": max(xs), "y2": max(ys),
            "cx": (min(xs)+max(xs))//2, "cy": (min(ys)+max(ys))//2,
        }
        if key not in deduped or deduped[key]["conf"] < conf:
            deduped[key] = tok
    tokens = list(deduped.values())
    tokens.sort(key=lambda t: (t["y"]//30, t["x"]))
    return tokens


#  MRZ CHARACTER NORMALIZATION

_SYM_TO_LETTER = {
    "$":"S", "Â§":"S", "|":"I", "!":"I", "@":"A", "&":"R",
    '"':"M", "\u201c":"M", "\u201d":"M", "#":"H",
    "'":"<", "`":"<", "[":"<", "]":"<", "{":"<", "}":"<",
    "(":"<", ")":"<", "/":"<", "\\":"<", " ":"<",
}
_DIGIT_TO_LETTER = {"0":"O","1":"I","2":"Z","4":"A","5":"S","6":"G","7":"T","8":"B","9":"P"}
_LETTER_TO_DIGIT = {"O":"0","Q":"0","D":"0","I":"1","L":"1","J":"0","Z":"2",
                    "S":"5","G":"6","T":"7","B":"8","A":"4"}
_VALID_MRZ = set("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<")

# TD3 line-2 positional mask (passport, 44 chars)
_TD3_L2_MASK = (
    "XXXXXXXXX"   # 0-8:  passport number
    "D"           # 9:    pp check digit
    "LLL"         # 10-12: nationality
    "DDDDDD"      # 13-18: DOB YYMMDD
    "D"           # 19:   DOB check
    "S"           # 20:   sex
    "DDDDDD"      # 21-26: expiry YYMMDD
    "D"           # 27:   expiry check
    "XXXXXXXXXXXXXX"  # 28-41: optional/national ID
    "D"           # 42:   optional check
    "D"           # 43:   composite check
)

# TD1 line-2 positional mask (national ID, 30 chars)
_TD1_L2_MASK = (
    "DDDDDD"      # 0-5:  DOB YYMMDD
    "D"           # 6:    DOB check
    "S"           # 7:    sex
    "DDDDDD"      # 8-13: expiry YYMMDD
    "D"           # 14:   expiry check
    "LLL"         # 15-17: nationality
    "XXXXXXXXXXX" # 18-28: optional
    "D"           # 29:   composite check
)


def _fix_char(ch: str) -> str:
    if ch in _VALID_MRZ:
        return ch
    return _SYM_TO_LETTER.get(ch, "<")


def normalize_mrz_l1_td3(raw: str) -> str:
    """TD3 line 1: all 44 chars must be A-Z or <. Digits â†’ likely letters."""
    chars = [_fix_char(c) for c in raw.upper()]
    result = []
    for ch in chars:
        if ch in "ABCDEFGHIJKLMNOPQRSTUVWXYZ<":
            result.append(ch)
        elif ch.isdigit():
            result.append(_DIGIT_TO_LETTER.get(ch, "<"))
        else:
            result.append("<")
    line = list(("".join(result)).ljust(44, "<")[:44])
    # Enforce: pos 0 = 'P', pos 1 = '<' for standard passports
    if line[0] == "P":
        line[1] = "<"
    return "".join(line)


def normalize_mrz_l2_td3(raw: str) -> str:
    """TD3 line 2: position-aware correction per ICAO 9303."""
    chars = list(("".join(_fix_char(c.upper()) for c in raw.strip())).ljust(44, "<")[:44])
    for i, mask in enumerate(_TD3_L2_MASK):
        ch = chars[i]
        if mask == "D":
            if not ch.isdigit():
                chars[i] = _LETTER_TO_DIGIT.get(ch, "0")
        elif mask == "L":
            if not (ch.isalpha() and ch in "ABCDEFGHIJKLMNOPQRSTUVWXYZ"):
                chars[i] = _DIGIT_TO_LETTER.get(ch, "<")
        elif mask == "S":
            if ch not in ("M", "F", "<"):
                chars[i] = "M" if ch in ('"', "'", "`") else "<"
        else:
            if ch not in _VALID_MRZ:
                chars[i] = "<"
    return "".join(chars)


def normalize_mrz_l1_td1(raw: str) -> str:
    """TD1 line 1 (30 chars): letters only in name zone, digits in numeric zone."""
    chars = list(("".join(_fix_char(c.upper()) for c in raw.strip())).ljust(30, "<")[:30])
    # pos 0-1: doc type, 2-4: country, 5-13: doc number, 14: doc check,
    # 15-29: optional data. TD1 line 1 does not have a check digit at pos 29.
    for i in range(30):
        ch = chars[i]
        if i == 14:
            if not ch.isdigit():
                chars[i] = _LETTER_TO_DIGIT.get(ch, "0")
        elif i < 5:         # doc type + country = letters only
            if ch.isdigit():
                chars[i] = _DIGIT_TO_LETTER.get(ch, "<")
        else:
            if ch not in _VALID_MRZ:
                chars[i] = "<"
    return "".join(chars)


def _td1_l2_score(chars: list) -> int:
    """Score a candidate TD1 L2 alignment â€” higher is more plausible."""
    score = 0
    if chars[7] in ("M", "F"):                             score += 3  # sex
    if all(chars[i].isdigit() for i in range(0, 6)):       score += 3  # DOB digits
    if all(chars[i].isdigit() for i in range(8, 14)):      score += 3  # expiry digits
    if all(chars[i].isalpha() for i in range(15, 18)):     score += 2  # nationality
    if chars[6].isdigit():                                  score += 1  # DOB check digit
    if chars[14].isdigit():                                 score += 1  # expiry check digit
    return score


def _apply_td1_l2_mask(chars: list) -> list:
    """Apply positional mask corrections to a 30-char TD1 L2 list."""
    out = chars[:]
    for i, mask in enumerate(_TD1_L2_MASK):
        ch = out[i]
        if mask == "D":
            if not ch.isdigit():
                out[i] = _LETTER_TO_DIGIT.get(ch, "0")
        elif mask == "L":
            if not (ch.isalpha() and ch in "ABCDEFGHIJKLMNOPQRSTUVWXYZ"):
                out[i] = _DIGIT_TO_LETTER.get(ch, "<")
        elif mask == "S":
            if ch not in ("M", "F", "<"):
                out[i] = "M" if ch in ('"', "'", "`") else "<"
        else:
            if ch not in _VALID_MRZ:
                out[i] = "<"
    return out


def normalize_mrz_l2_td1(raw: str) -> str:
    """TD1 line 2 (30 chars): position-aware with alignment recovery.

    Same principle as TD3: a single OCR misread near pos 6 (DOB check digit)
    or pos 7 (sex) shifts all subsequent fields. We try dropping a spurious
    char in that zone and keep the best-scoring alignment.
    """
    raw_clean = "".join(_fix_char(c.upper()) for c in raw.strip())
    base = list(raw_clean.ljust(30, "<")[:30])
    best = _apply_td1_l2_mask(base[:])
    best_score = _td1_l2_score(best)

    # Try dropping a spurious char near the DOB check / sex zone (pos 5-10)
    for drop_pos in range(max(0, 5), min(len(raw_clean), 11)):
        trial_raw   = raw_clean[:drop_pos] + raw_clean[drop_pos + 1:]
        trial       = list(trial_raw.ljust(30, "<")[:30])
        trial_norm  = _apply_td1_l2_mask(trial)
        trial_score = _td1_l2_score(trial_norm)
        if trial_score > best_score:
            best, best_score = trial_norm, trial_score

    return "".join(best)


def normalize_mrz_l3_td1(raw: str) -> str:
    """TD1 line 3 (30 chars): name field â€” letters and < only."""
    chars = [_fix_char(c.upper()) for c in raw.strip()]
    result = []
    for ch in chars:
        if ch in "ABCDEFGHIJKLMNOPQRSTUVWXYZ<":
            result.append(ch)
        elif ch.isdigit():
            result.append(_DIGIT_TO_LETTER.get(ch, "<"))
        else:
            result.append("<")
    return ("".join(result)).ljust(30, "<")[:30]

_ICAO_WEIGHTS = [7, 3, 1]
_ICAO_VALS    = {c: i for i, c in enumerate("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ<")}


def icao_check(field: str) -> int:
    return sum(_ICAO_VALS.get(c, 0) * _ICAO_WEIGHTS[i % 3]
               for i, c in enumerate(field)) % 10


def validate_td3(l2: str) -> dict:
    try:
        return {
            "passport_no": icao_check(l2[0:9])  == int(l2[9]),
            "dob":         icao_check(l2[13:19]) == int(l2[19]),
            "expiry":      icao_check(l2[21:27]) == int(l2[27]),
            "composite":   icao_check(l2[0:10] + l2[13:20] + l2[21:43]) == int(l2[43]),
        }
    except (ValueError, IndexError) as e:
        return {"passport_no": False, "dob": False, "expiry": False,
                "composite": False, "error": str(e)}


def validate_td1(l1: str, l2: str) -> dict:
    try:
        doc_no_field = l1[5:14]
        doc_no_check = l1[14]
        national_id  = l1[15:28]
        composite_field = (l1[0:10] + l1[10:30] + l2[0:7] + l2[8:15] + l2[18:29])
        return {
            "doc_number": icao_check(doc_no_field) == int(doc_no_check),
            "dob":        icao_check(l2[0:6])       == int(l2[6]),
            "expiry":     icao_check(l2[8:14])      == int(l2[14]),
            "composite":  icao_check(composite_field) == int(l2[29]),
        }
    except (ValueError, IndexError) as e:
        return {"doc_number": False, "dob": False, "expiry": False,
                "composite": False, "error": str(e)}


_RAW_MRZ_COMPAT = set(
    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
    "0123456789<$|\"&@!#^*()' "
)


def _mrz_compat_ratio(text: str) -> float:
    return sum(1 for c in text if c in _RAW_MRZ_COMPAT) / max(len(text), 1)


def extract_mrz_from_tokens(tokens: list[dict], img_h: int,
                             doc_type: str = "passport") -> list[dict]:
    """
    Find TD3 (passport, 44-char) or TD1 (ID card, 30-char) MRZ lines
    among the full-image OCR tokens.

    Returns up to 2 (TD3) or 3 (TD1) dicts: {text, y, conf}, topâ†’bottom.
    """
    min_len        = 22   # minimum raw token length to be considered MRZ
    bottom_thresh  = img_h * 0.68   # MRZ is always in the bottom third
    expected_width = 44 if doc_type == "passport" else 30

    candidates = []
    for t in tokens:
        text = t["text"].strip()
        if len(text) < min_len:
            continue
        if t["y"] < bottom_thresh:
            continue
        if _mrz_compat_ratio(text) < 0.85:
            continue
        candidates.append({"text": text, "y": t["y"], "conf": t["conf"]})

    if not candidates:
        return []

    # Cluster by y-proximity (merge fragments on the same physical row)
    candidates.sort(key=lambda c: c["y"])
    clusters: list[dict] = []
    for c in candidates:
        if clusters and abs(c["y"] - clusters[-1]["y"]) < 40:
            # Keep the longer fragment
            if len(c["text"]) > len(clusters[-1]["text"]):
                clusters[-1] = c
        else:
            clusters.append(c)

    # Return the bottom-most 2 (TD3) or 3 (TD1) clusters
    n = 2 if doc_type == "passport" else 3
    return clusters[-n:] if len(clusters) >= n else clusters


#  MRZ STRIP FALLBACK  (used only when token extraction finds nothing)

def _find_mrz_zone_y(gray: np.ndarray) -> int:
    h, w = gray.shape
    _, binary = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
    density = np.array([np.sum(binary[y] == 0) / w for y in range(h)])
    dense = np.where(density > 0.22)[0]
    if len(dense) == 0:
        return int(h * 0.78)
    bands, start, prev = [], dense[0], dense[0]
    for y in dense[1:]:
        if y - prev > 20:
            bands.append((start, prev, prev - start))
            start = y
        prev = y
    bands.append((start, prev, prev - start))
    bottom = [b for b in bands if b[0] > h * 0.60 and b[2] > 10]
    if len(bottom) >= 2:
        return max(0, bottom[-2][0] - 15)
    if bottom:
        return max(0, bottom[0][0] - 15)
    return max(0, int(bands[-1][0]) - 15)


def preprocess_mrz_strip(img: np.ndarray, doc_type: str = "id") -> np.ndarray:
    h, w   = img.shape[:2]
    img3x  = cv2.resize(_ensure_bgr(img), (w*3, h*3), interpolation=cv2.INTER_LANCZOS4)
    gray3x = cv2.cvtColor(img3x, cv2.COLOR_BGR2GRAY)
    H, W   = gray3x.shape

    if doc_type == "passport":
        # Stronger denoising + fixed threshold for holographic backgrounds
        denoised = _denoise_gray(gray3x, h=10)
        enhanced = _clahe_gray(denoised, clip=1.8, tile=(16, 16))
        sharpened = _unsharp(enhanced, amount=1.35, sigma=1.0)
        _, binary = cv2.threshold(sharpened, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
    else:
        # Original â€” works well on clean ID card backgrounds
        denoised = _denoise_gray(gray3x, h=14)
        enhanced = _clahe_gray(denoised, clip=1.8, tile=(16, 16))
        sharpened = _unsharp(enhanced, amount=1.35, sigma=1.0)
        _, binary = cv2.threshold(sharpened, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)

    col_proj = np.sum(binary == 0, axis=1)
    start_y  = int(H * 0.60)
    mrz_rows = [y for y in range(start_y, H) if col_proj[y] > W * 0.08]
    strip    = binary[max(0, min(mrz_rows)-30):] if mrz_rows else binary[int(H*0.70):]
    sh, sw   = strip.shape
    clean    = next((y for y in range(sh) if np.sum(strip[y]==0)/sw < 0.75), 0)
    strip    = strip[clean:]
    k = cv2.getStructuringElement(cv2.MORPH_RECT, (2, 2))
    return cv2.morphologyEx(strip, cv2.MORPH_CLOSE, k)


def ocr_mrz_strip_fallback(mrz_strip: np.ndarray, lines_count: int = 2) -> list[dict]:
    """Band-split strip OCR. Use only when token extraction fails."""
    h, _ = mrz_strip.shape[:2]
    lines_count = max(2, min(lines_count, 3))
    lines = []
    for idx in range(lines_count):
        start = int(h * idx / lines_count)
        end = int(h * (idx + 1) / lines_count)
        band = mrz_strip[start:end]
        with _silence():
            raw = READER_EN.readtext(
                band, detail=1, paragraph=False,
                allowlist=MRZ_ALLOWLIST, width_ths=0.9, height_ths=0.5)
        raw.sort(key=lambda r: r[0][0][0])
        joined = "".join(
            re.sub(r"[^A-Z0-9<]", "<", t.upper())
            for _, t, c in raw if c > 0.05
        )
        if len(joined.replace("<", "")) >= 6:
            lines.append({"text": joined, "y": start,
                           "conf": round(sum(c for _,_,c in raw)/max(len(raw),1), 3)})
    return lines


#  SHARED FIELD UTILITIES

def strip_arabic(text: str) -> str:
    return re.sub(r"\s+", " ",
           re.sub(r"[\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF]+",
                  " ", text)).strip()


def is_label_token(text: str) -> bool:
    """True if this token looks like a bilingual field label rather than a value."""
    t = strip_arabic(text)
    kws = ("Date","Name","Sex","Place","Birth","Issue","Expiry","National",
           "Passport","Mother","Authority","Type","Country","Code","Amhority")
    if any(k in t for k in kws):
        return True
    arabic = sum(1 for c in text if "\u0600" <= c <= "\u06FF")
    return arabic > len(text) * 0.4


def same_row(a: dict, b: dict, tol: int = 35) -> bool:
    return abs(a["cy"] - b["cy"]) <= tol


def tokens_right_of(tokens: list, anchor: dict, max_dy: int = 35) -> list:
    return sorted(
        [t for t in tokens
         if t is not anchor and same_row(t, anchor, max_dy)
         and t["x"] > anchor["x2"] and not is_label_token(t["text"])],
        key=lambda t: t["x"])


def tokens_below(tokens: list, anchor: dict, max_dy: int = 90,
                 x_lo: float | None = None, x_hi: float | None = None) -> list:
    xl = x_lo if x_lo is not None else anchor["x"] - 50
    xh = x_hi if x_hi is not None else anchor["x2"] + 300
    return sorted(
        [t for t in tokens
         if t is not anchor
         and t["y"] > anchor["y2"]
         and t["y"] - anchor["y2"] <= max_dy
         and t["x"] >= xl and t["x2"] <= xh
         and not is_label_token(t["text"])],
        key=lambda t: t["y"])


#  DATE PARSING  always DD/MM/YYYY

MONTH_MAP = {
    "JAN":"01","FEB":"02","MAR":"03","APR":"04","MAY":"05","JUN":"06",
    "JUL":"07","AUG":"08","SEP":"09","OCT":"10","NOV":"11","DEC":"12",
    "0CT":"10","OC7":"10","0C7":"10",
}

_MONTH_FIXES = {
    "0CT":"OCT","OC7":"OCT","0C7":"OCT","JUl":"JUL","JUI":"JUL",
    "5EP":"SEP","N0V":"NOV","0EC":"DEC","MA8":"MAR","FE8":"FEB",
    "^4":"14",
}


def parse_date(raw: str) -> str:
    """Parse any date representation to DD/MM/YYYY. Arabic stripped first."""
    if not raw:
        return ""
    s = strip_arabic(raw).upper()
    for bad, good in _MONTH_FIXES.items():
        s = s.replace(bad, good)
    # DD/MM/YYYY or variants
    m = re.search(r"(\d{1,2})[/.\-](\d{2})[/.\-](\d{4})", s)
    if m:
        return f"{int(m.group(1)):02d}/{int(m.group(2)):02d}/{m.group(3)}"
    # DD MON YYYY
    d  = re.search(r"\b(\d{1,2})\b", s)
    y  = re.search(r"\b(\d{4})\b", s)
    mo = re.search(r"\b([A-Z]{3})\b", s)
    if d and y and mo:
        mon = MONTH_MAP.get(mo.group(1))
        if mon:
            return f"{int(d.group(1)):02d}/{mon}/{y.group(1)}"
    return s.strip()


def extract_date_for(tokens: list, label_tok: dict) -> str | None:
    """Find and parse the date value associated with a label token."""
    right = tokens_right_of(tokens, label_tok)
    right_text = " ".join(t["text"] for t in right)
    if re.search(r"\d{4}", strip_arabic(right_text)):
        return parse_date(right_text)
    x_lo  = label_tok["x"] - 20
    x_hi  = label_tok["x2"] + 350
    below = tokens_below(tokens, label_tok, max_dy=110, x_lo=x_lo, x_hi=x_hi)
    if not below:
        return None
    fy   = below[0]["cy"]
    row  = sorted([t for t in below if abs(t["cy"] - fy) <= 35], key=lambda t: t["x"])
    raw  = " ".join(strip_arabic(t["text"]) for t in row).strip()
    if re.search(r"\d{4}|[A-Z]{3}", raw):
        return parse_date(raw)
    return None



def split_mrz_name(name_field: str) -> tuple[str, str]:
    field = name_field.rstrip("<")
    if "<<" in field:
        idx     = field.index("<<")
        surname = field[:idx].replace("<", "-").strip("-")
        given   = re.sub(r"<+", " ", field[idx:].lstrip("<")).strip()
        return surname, given
    runs = [(m.start(), len(m.group())) for m in re.finditer(r"<+", field)]
    if runs:
        longest = max(runs, key=lambda r: r[1])
        return (field[:longest[0]].replace("<","-").strip("-"),
                re.sub(r"<+", " ", field[longest[0]+longest[1]:]).strip())
    return field.replace("<", " ").strip(), ""


def build_full_name(surname: str, given: str) -> str:
    """Given-names first, surname last â€” matches how it's printed on Jordanian docs."""
    return " ".join(p.strip() for p in [given, surname] if p.strip())

def _normalize_digits(text: str) -> str:
    return text.translate(_AR_INDIC)

def _reread_number_token(src_img, token: dict, reader=None) -> str:
    """
    Crop the bounding box of a token, upscale + sharpen it, and re-OCR.
    Fixes EasyOCR confusion between Arabic-Indic Ù¢ and Ù£ at normal resolution.
 
    Returns the re-read text (normalized to western digits), or falls back
    to the original token text if re-read produces nothing useful.
    """
    r=reader or READER_MIX
    x, y, x2, y2 = token["x"], token["y"], token["x2"], token["y2"]
    pad = 6
    h, w = src_img.shape[:2]
    x  = max(0, x  - pad)
    y  = max(0, y  - pad)
    x2 = min(w, x2 + pad)
    y2 = min(h, y2 + pad)
 
    crop = src_img[y:y2, x:x2]
    if crop.size == 0:
        return _normalize_digits(token["text"])
 
    # Upscale so digit strokes are thick enough for OCR to distinguish glyphs
    crop = cv2.resize(crop, None, fx=3, fy=3, interpolation=cv2.INTER_CUBIC)
 
    # src_img from select_source() is already grayscale work directly on it.
    # Unsharp mask: amplify edges so Ù£ curved tail is distinct from Ù¢ flat base
    blur   = cv2.GaussianBlur(crop, (0, 0), sigmaX=2)
    sharp  = cv2.addWeighted(crop, 1.8, blur, -0.8, 0)
    sharp = np.clip(sharp, 0, 255).astype(np.uint8)  

    # CLAHE  same params as ocr_engine.build_variants but tighter tile for a
    # small crop so local contrast is boosted without over-brightening thin strokes
    clahe  = cv2.createCLAHE(clipLimit=3.0, tileGridSize=(4, 4))
    sharp  = clahe.apply(sharp)
 
    # Otsu threshold  clean binary (same pattern as preprocess_mrz_strip)
    _, binary = cv2.threshold(sharp, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
 
    results = r.readtext(binary, detail=0, paragraph=False)
    if results:
        return _normalize_digits(" ".join(results))
    return _normalize_digits(token["text"])

def mrz_date(d: str, kind: str = "birth") -> str:
    d = d.translate(str.maketrans("OoIlSsBb", "00115588")) 
    if len(d) != 6 or not d.isdigit():
        return d
    yy, mm, dd = d[:2], d[2:4], d[4:6]
    year = int(yy)
    if kind == "expiry":
        yyyy = str(2000 + year)
    else:
        yyyy = str(2000 + year if 2000 + year <= date.today().year else 1900 + year)
    return f"{dd}/{mm}/{yyyy}"


NOTEBOOK_PASSPORT_FLOW = (
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


def prepare_passport_ocr_inputs(image_path: str) -> dict:
    """
    Build notebook-compatible OCR-ready inputs for passport processing.

    The helper reuses the notebook preprocessing and token model already
    present in this module: load_and_resize, build_variants, select_source,
    compute_quality, and ocr_to_tokens.
    """
    img = load_and_resize(image_path)
    variants = build_variants(img)
    quality = compute_quality(img)
    visual_source = select_source(variants, mode="text")
    mrz_source = select_source(variants, mode="mrz")
    visual_tokens = ocr_to_tokens(visual_source, reader=READER_MIX)
    mrz_tokens = ocr_to_tokens(
        mrz_source,
        reader=READER_EN,
        allowlist=MRZ_ALLOWLIST,
        min_conf=0.01,
    )
    return {
        "flow": NOTEBOOK_PASSPORT_FLOW,
        "image": img,
        "variants": variants,
        "quality": quality,
        "orientation": {"angle": 0, "method": "notebook_default_upright"},
        "visual_source": visual_source,
        "mrz_source": mrz_source,
        "visual_tokens": visual_tokens,
        "mrz_tokens": mrz_tokens,
    }
