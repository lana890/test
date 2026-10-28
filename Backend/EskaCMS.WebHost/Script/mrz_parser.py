# -*- coding: utf-8 -*-

import re
from datetime import date, datetime

# ── Character value table ────────────────────────────────────────────────────

_CHAR_VALUES: dict[str, int] = {c: i + 10 for i, c in enumerate("ABCDEFGHIJKLMNOPQRSTUVWXYZ")}
_CHAR_VALUES.update({str(i): i for i in range(10)})
_CHAR_VALUES["<"] = 0

_WEIGHTS = [7, 3, 1]

# ── Regex ────────────────────────────────────────────────────────────────────

MRZ_CHAR_RE = re.compile(r"^[A-Z0-9<]+$")

# ── Exceptions ───────────────────────────────────────────────────────────────

class TD3ParseError(Exception):
    pass

# ══════════════════════════════════════════════════════════════════════════════
#  CHECK DIGIT
# ══════════════════════════════════════════════════════════════════════════════

def compute_check_digit(field: str) -> int:
    """
    ICAO check digit: weighted sum of char values (weights 7,3,1 cycling) mod 10.
    """
    return sum(
        _CHAR_VALUES.get(c, 0) * _WEIGHTS[i % 3]
        for i, c in enumerate(field)
    ) % 10


def verify_check(field: str, expected_digit: str) -> bool:
    try:
        return compute_check_digit(field) == int(expected_digit)
    except (ValueError, TypeError):
        return False

# ══════════════════════════════════════════════════════════════════════════════
#  DATE PARSING
# ══════════════════════════════════════════════════════════════════════════════

def _parse_mrz_date(yymmdd: str, *, future_bias: bool = False) -> str | None:
    """
    Convert YYMMDD → ISO date string YYYY-MM-DD.

    future_bias=False  (date of birth):   YY > current year-mod-100 → 1900s
    future_bias=True   (expiry date):     YY ≤ current year+10-mod-100 → 2000s
    """
    if not yymmdd or len(yymmdd) != 6 or not yymmdd.isdigit():
        return None
    yy, mm, dd = int(yymmdd[:2]), int(yymmdd[2:4]), int(yymmdd[4:6])
    now_yy = datetime.now().year % 100
    if future_bias:
        year = 2000 + yy if yy <= now_yy + 15 else 1900 + yy
    else:
        year = 1900 + yy if yy > now_yy else 2000 + yy
    try:
        return date(year, mm, dd).strftime("%Y-%m-%d")
    except ValueError:
        return None

# ══════════════════════════════════════════════════════════════════════════════
#  LOOKUP TABLES
# ══════════════════════════════════════════════════════════════════════════════

# ISO 3166-1 alpha-3 → demonym (extend as needed)
NATIONALITY_LABELS: dict[str, str] = {
    "AFG": "Afghan",       "ALB": "Albanian",     "DZA": "Algerian",
    "AND": "Andorran",     "AGO": "Angolan",       "ARG": "Argentine",
    "ARM": "Armenian",     "AUS": "Australian",    "AUT": "Austrian",
    "AZE": "Azerbaijani",  "BHR": "Bahraini",      "BGD": "Bangladeshi",
    "BLR": "Belarusian",   "BEL": "Belgian",       "BLZ": "Belizean",
    "BEN": "Beninese",     "BTN": "Bhutanese",     "BOL": "Bolivian",
    "BIH": "Bosnian",      "BWA": "Motswana",      "BRA": "Brazilian",
    "BRN": "Bruneian",     "BGR": "Bulgarian",     "BFA": "Burkinabe",
    "BDI": "Burundian",    "KHM": "Cambodian",     "CMR": "Cameroonian",
    "CAN": "Canadian",     "CAF": "Central African","CHL": "Chilean",
    "CHN": "Chinese",      "COL": "Colombian",     "COD": "Congolese",
    "CRI": "Costa Rican",  "HRV": "Croatian",      "CUB": "Cuban",
    "CYP": "Cypriot",      "CZE": "Czech",         "DNK": "Danish",
    "DJI": "Djiboutian",   "DOM": "Dominican",     "ECU": "Ecuadorian",
    "EGY": "Egyptian",     "SLV": "Salvadoran",    "EST": "Estonian",
    "ETH": "Ethiopian",    "FIN": "Finnish",       "FRA": "French",
    "GAB": "Gabonese",     "GMB": "Gambian",       "GEO": "Georgian",
    "DEU": "German",       "GHA": "Ghanaian",      "GRC": "Greek",
    "GTM": "Guatemalan",   "GIN": "Guinean",       "GUY": "Guyanese",
    "HTI": "Haitian",      "HND": "Honduran",      "HUN": "Hungarian",
    "ISL": "Icelandic",    "IND": "Indian",        "IDN": "Indonesian",
    "IRN": "Iranian",      "IRQ": "Iraqi",         "IRL": "Irish",
    "ISR": "Israeli",      "ITA": "Italian",       "JAM": "Jamaican",
    "JPN": "Japanese",     "JOR": "Jordanian",     "KAZ": "Kazakhstani",
    "KEN": "Kenyan",       "PRK": "North Korean",  "KOR": "South Korean",
    "KWT": "Kuwaiti",      "KGZ": "Kyrgyz",        "LAO": "Laotian",
    "LVA": "Latvian",      "LBN": "Lebanese",      "LBR": "Liberian",
    "LBY": "Libyan",       "LIE": "Liechtenstein", "LTU": "Lithuanian",
    "LUX": "Luxembourgish","MKD": "Macedonian",    "MDG": "Malagasy",
    "MWI": "Malawian",     "MYS": "Malaysian",     "MDV": "Maldivian",
    "MLI": "Malian",       "MLT": "Maltese",       "MRT": "Mauritanian",
    "MEX": "Mexican",      "MDA": "Moldovan",      "MCO": "Monégasque",
    "MNG": "Mongolian",    "MNE": "Montenegrin",   "MAR": "Moroccan",
    "MOZ": "Mozambican",   "MMR": "Burmese",       "NAM": "Namibian",
    "NPL": "Nepali",       "NLD": "Dutch",         "NZL": "New Zealander",
    "NIC": "Nicaraguan",   "NER": "Nigerien",      "NGA": "Nigerian",
    "NOR": "Norwegian",    "OMN": "Omani",         "PAK": "Pakistani",
    "PAN": "Panamanian",   "PNG": "Papua New Guinean", "PRY": "Paraguayan",
    "PER": "Peruvian",     "PHL": "Filipino",      "POL": "Polish",
    "PRT": "Portuguese",   "PSE": "Palestinian",   "QAT": "Qatari",
    "ROU": "Romanian",     "RUS": "Russian",       "RWA": "Rwandan",
    "SAU": "Saudi",        "SEN": "Senegalese",    "SRB": "Serbian",
    "SLE": "Sierra Leonean","SGP": "Singaporean",  "SVK": "Slovak",
    "SVN": "Slovenian",    "SOM": "Somali",        "ZAF": "South African",
    "ESP": "Spanish",      "LKA": "Sri Lankan",    "SDN": "Sudanese",
    "SWE": "Swedish",      "CHE": "Swiss",         "SYR": "Syrian",
    "TWN": "Taiwanese",    "TJK": "Tajik",         "TZA": "Tanzanian",
    "THA": "Thai",         "TLS": "Timorese",      "TGO": "Togolese",
    "TTO": "Trinidadian",  "TUN": "Tunisian",      "TUR": "Turkish",
    "TKM": "Turkmen",      "UGA": "Ugandan",       "UKR": "Ukrainian",
    "ARE": "Emirati",      "GBR": "British",       "USA": "American",
    "URY": "Uruguayan",    "UZB": "Uzbek",         "VEN": "Venezuelan",
    "VNM": "Vietnamese",   "YEM": "Yemeni",        "ZMB": "Zambian",
    "ZWE": "Zimbabwean",
    # Special ICAO codes
    "UNO": "UN Organisation", "UNA": "UN Agency", "UNK": "Kosovo",
    "XXA": "Stateless",       "XXB": "Refugee",   "XXC": "Refugee (Other)",
    "XXX": "Unspecified",
}

DOCUMENT_TYPE_LABELS: dict[str, str] = {
    "P":  "Passport",
    "PA": "Passport",
    "PC": "Passport Card",
    "PD": "Diplomatic Passport",
    "PS": "Service Passport",
    "PO": "Official Passport",
    "PN": "Non-national Passport",
    "PE": "Emergency Passport",
    "PF": "Facilitation Document",
    "PI": "Identity Passport",
    "PB": "Border Crossing Passport",
}

SEX_LABELS: dict[str, str] = {
    "M": "Male",
    "F": "Female",
    "<": "Unspecified",
}

# ══════════════════════════════════════════════════════════════════════════════
#  NAME PARSING
# ══════════════════════════════════════════════════════════════════════════════

def _parse_name_field(name_field: str) -> tuple[str, str]:
    """
    Parse 39-char name field from line 1 positions [5:44].
    Returns (surname, given_names).
    '<<' separates surname from given name(s); '<' is a space.
    """
    if "<<" in name_field:
        surname_raw, given_raw = name_field.split("<<", 1)
    else:
        surname_raw, given_raw = name_field, ""

    surname     = surname_raw.replace("<", " ").strip()
    given_names = given_raw.replace("<", " ").strip()
    return surname, given_names

# ══════════════════════════════════════════════════════════════════════════════
#  CORE TD3 PARSER
# ══════════════════════════════════════════════════════════════════════════════

def parse_td3(line1: str, line2: str) -> dict:
    """
    Parse a complete TD3 MRZ (2 × 44 characters).

    Returns a dict with:
      - All extracted fields (human-readable)
      - mrz_valid (bool)
      - mrz_checks (per-field check digit results)
      - mrz_lines (raw, for debugging)

    Raises TD3ParseError on structural problems.
    """
    line1 = line1.upper().strip()
    line2 = line2.upper().strip()

    # ── Structural validation ────────────────────────────────────────────────
    if len(line1) != 44:
        raise TD3ParseError(f"Line 1 must be 44 chars, got {len(line1)}: {line1!r}")
    if len(line2) != 44:
        raise TD3ParseError(f"Line 2 must be 44 chars, got {len(line2)}: {line2!r}")
    if not MRZ_CHAR_RE.match(line1):
        raise TD3ParseError(f"Line 1 contains invalid MRZ characters: {line1!r}")
    if not MRZ_CHAR_RE.match(line2):
        raise TD3ParseError(f"Line 2 contains invalid MRZ characters: {line2!r}")

    # ── Line 1 extraction ────────────────────────────────────────────────────
    #  [0]     document type (e.g. 'P')
    #  [1]     document subtype or '<'
    #  [2:5]   issuing state / organisation (3 chars)
    #  [5:44]  name field (39 chars)

    doc_type_raw  = line1[0:2].rstrip("<").strip()
    issuing_state = line1[2:5].replace("<", "").strip()
    name_field    = line1[5:44]
    surname, given_names = _parse_name_field(name_field)

    # ── Line 2 extraction ────────────────────────────────────────────────────
    #  [0:9]   document number (9 chars, padded with <)
    #  [9]     check digit — document number
    #  [10:13] nationality (3 chars)
    #  [13:19] date of birth YYMMDD
    #  [19]    check digit — DOB
    #  [20]    sex  M / F / <
    #  [21:27] expiry date YYMMDD
    #  [27]    check digit — expiry
    #  [28:42] optional data / personal number (14 chars)
    #  [42]    check digit — optional data
    #  [43]    composite check digit

    doc_no_raw      = line2[0:9]
    doc_no_chk      = line2[9]
    nationality_raw = line2[10:13]
    dob_raw         = line2[13:19]
    dob_chk         = line2[19]
    sex_raw         = line2[20]
    expiry_raw      = line2[21:27]
    expiry_chk      = line2[27]
    optional_raw    = line2[28:42]
    optional_chk    = line2[42]
    composite_chk   = line2[43]

    # Strip trailing filler
    passport_no  = doc_no_raw.rstrip("<")
    nationality  = nationality_raw.replace("<", "").strip()
    personal_no  = optional_raw.replace("<", "").strip() or None

    # ── Check digits ─────────────────────────────────────────────────────────
    chk_passport_no = verify_check(doc_no_raw, doc_no_chk)
    chk_dob         = verify_check(dob_raw, dob_chk)
    chk_expiry      = verify_check(expiry_raw, expiry_chk)
    # Optional data check: only validate if field is not all fillers
    chk_optional    = verify_check(optional_raw, optional_chk) if personal_no else True
    # Composite check: doc_no+chk, dob+chk, sex, expiry+chk, optional+chk
    composite_field = line2[0:10] + line2[13:20] + line2[21:43]
    chk_composite   = verify_check(composite_field, composite_chk)

    mrz_valid = all([chk_passport_no, chk_dob, chk_expiry, chk_composite])

    # ── Human-readable values ─────────────────────────────────────────────────
    dob_iso        = _parse_mrz_date(dob_raw, future_bias=False)
    expiry_iso     = _parse_mrz_date(expiry_raw, future_bias=True)
    sex_label      = SEX_LABELS.get(sex_raw, sex_raw)
    nat_label      = NATIONALITY_LABELS.get(nationality, nationality)
    doc_type_label = DOCUMENT_TYPE_LABELS.get(doc_type_raw, "Passport")
    full_name      = f"{given_names} {surname}".strip() if given_names else surname

    return {
        # ── Document identification ───────────────────────────────────────────
        "document_type":     doc_type_label,
        "doc_type_raw":      doc_type_raw,
        "issuing_state":     issuing_state,
        "passport_no":       passport_no,

        # ── Holder ───────────────────────────────────────────────────────────
        "surname":           surname,
        "given_names":       given_names,
        "name_en":           full_name,
        "nationality":       nat_label,
        "nationality_code":  nationality,
        "gender":            sex_label,
        "date_of_birth":     dob_iso,
        "expiry_date":       expiry_iso,
        "personal_no":       personal_no,

        # ── Validity ──────────────────────────────────────────────────────────
        "mrz_valid": mrz_valid,
        "mrz_checks": {
            "passport_no": chk_passport_no,
            "dob":         chk_dob,
            "expiry":      chk_expiry,
            "optional":    chk_optional,
            "composite":   chk_composite,
        },

        # ── Raw data (for debugging / audit) ──────────────────────────────────
        "mrz_lines": {
            "line1": line1,
            "line2": line2,
        },
    }


# ══════════════════════════════════════════════════════════════════════════════
#  OCR CORRECTION HELPERS  (exported for use in passport_parser)
# ══════════════════════════════════════════════════════════════════════════════

def fix_numeric_field(s: str) -> str:
    """
    Correct common OCR errors in purely-numeric MRZ fields
    (dates, check digits).
    O→0, I→1, S→5, B→8, Z→2, G→6
    """
    return s.translate(str.maketrans("OILSBZG", "0115826"))


def fix_mixed_field(s: str, *, alpha_positions: set[int] | None = None) -> str:
    """
    Correct OCR errors in alphanumeric fields (passport number).
    Only applies numeric corrections to positions known to be digits.
    """
    if not alpha_positions:
        return s   # Without position hints, leave alphanumeric as-is
    result = list(s)
    for i, c in enumerate(result):
        if i not in alpha_positions:
            result[i] = c.translate(str.maketrans("OILSBZG", "0115826"))
    return "".join(result)


def try_correct_line2(line2: str) -> str:
    """
    Apply targeted numeric corrections to the numeric-only zones of line 2:
    - DOB [13:19], DOB check [19]
    - Expiry [21:27], Expiry check [27]
    - Composite check [43]
    Passport number [0:9] is left unchanged (alphanumeric).
    """
    chars = list(line2)
    num_positions = list(range(13, 20)) + list(range(21, 28)) + [43]
    for i in num_positions:
        if i < len(chars):
            chars[i] = fix_numeric_field(chars[i])
    return "".join(chars)