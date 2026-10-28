# -*- coding: utf-8 -*-
# Shared copy — canonical implementation lives in EskaCMS.WebHost/Script/ocr_normalization.py

import re
import unicodedata

import cv2
import numpy as np


def normalize_field(value):
    if isinstance(value, dict):
        inner = value.get("value")
        while isinstance(inner, dict):
            inner = inner.get("value")
        return inner or ""
    return value or ""


def normalize_response_fields(data: dict) -> dict:
    if not isinstance(data, dict):
        return {}
    clean = {}
    for key, value in data.items():
        if key.startswith("_conf_"):
            continue
        clean[key] = normalize_field(value)
    clean["full_name_ar"] = normalize_field(data.get("full_name_ar"))
    clean["full_name_en"] = normalize_field(data.get("full_name_en"))
    return clean


def normalize_arabic_text(text: str) -> str:
    if not text:
        return ""
    text = unicodedata.normalize("NFKC", text)
    text = text.replace("ـ", "")
    text = re.sub(r"[^\u0600-\u06FF\s]", " ", text)
    text = re.sub(r"[أإآٱ]", "ا", text)
    text = re.sub(r"\s+", " ", text).strip()
    return text


def enhance_arabic_crop(image):
    if image is None or image.size == 0:
        return image
    if len(image.shape) == 2:
        bgr = cv2.cvtColor(image, cv2.COLOR_GRAY2BGR)
    else:
        bgr = image
    bgr = cv2.resize(bgr, None, fx=3, fy=3, interpolation=cv2.INTER_LANCZOS4)
    gray = cv2.cvtColor(bgr, cv2.COLOR_BGR2GRAY)
    clahe = cv2.createCLAHE(clipLimit=2.5, tileGridSize=(8, 8))
    gray = clahe.apply(gray)
    gray = cv2.fastNlMeansDenoising(gray, None, h=10, templateWindowSize=7, searchWindowSize=21)
    kernel = np.array([[0, -1, 0], [-1, 5, -1], [0, -1, 0]])
    return cv2.filter2D(gray, -1, kernel)


def enhance_id_image(image):
    return enhance_arabic_crop(image)


def generate_ocr_variants(image):
    variants = [("original", image)]
    enhanced = enhance_arabic_crop(image)
    variants.append(("enhanced", enhanced))
    variants.append(("high_contrast", cv2.convertScaleAbs(enhanced, alpha=1.5, beta=0)))
    variants.append((
        "adaptive_threshold",
        cv2.adaptiveThreshold(enhanced, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY, 31, 11),
    ))
    return variants


def arabic_ratio(text: str) -> float:
    if not text:
        return 0.0
    arabic_chars = len(re.findall(r"[\u0600-\u06FF]", text))
    total_chars = len(text.replace(" ", ""))
    return arabic_chars / total_chars if total_chars else 0.0


def score_arabic_candidate(text, confidence=0, expected_word_count=None):
    text = normalize_arabic_text(text)
    if not text:
        return 0.0
    words = text.split()
    score = float(confidence or 0) * 40 + arabic_ratio(text) * 30 + min(len(text), 60) * 0.5 + len(words) * 5
    score -= sum(1 for w in words if len(w) == 1) * 8
    if expected_word_count:
        diff = abs(len(words) - expected_word_count)
        score += 20 if diff == 0 else 10 if diff == 1 else -diff * 5
    if re.search(r"[A-Za-z0-9@#$%^&*_+=<>]", text):
        score -= 20
    return score


def select_best_arabic_candidate(candidates, expected_word_count=None):
    best, best_score = "", -1.0
    for candidate in candidates or []:
        text = normalize_arabic_text(candidate.get("text", ""))
        score = score_arabic_candidate(text, candidate.get("confidence", 0) or 0, expected_word_count)
        if score > best_score:
            best_score, best = score, text
    return best
