using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static EskaCMS.Search.Model.PassportData;

namespace EskaCMS.Search.Model
{
    public enum OcrDocumentType
    {
        Passport = 1,
        NationalId = 2,
        Vehicle = 3
    }

    public class OcrDocumentScanRequest
    {
        public OcrDocumentType DocumentType { get; set; }
        public IFormFile? File { get; set; }
        public IFormFile? Front { get; set; }
        public IFormFile? Back { get; set; }
    }

    //    public class OCRResult
    //    {
    //        [JsonPropertyName("success")]
    //        public bool Success { get; set; }

    //        [JsonPropertyName("data")]
    //        public IDCardData? Data { get; set; }

    //        [JsonPropertyName("error")]
    //        public string? Error { get; set; }
    //    }

    //    public class IDCardData
    //    {
    //        [JsonPropertyName("national_number")]
    //        public string? NationalNumber { get; set; }

    //        [JsonPropertyName("full_name_ar")]
    //        public string? FullNameAr { get; set; }

    //        [JsonPropertyName("full_name_en")]
    //        public string? FullNameEn { get; set; }

    //        [JsonPropertyName("date_of_birth")]
    //        public string? DateOfBirth { get; set; }

    //        [JsonPropertyName("gender")]
    //        public string? Gender { get; set; }

    //        [JsonPropertyName("expiry_date")]
    //        public string? ExpiryDate { get; set; }

    //        [JsonPropertyName("place_of_birth")]
    //        public string? PlaceOfBirth { get; set; }

    //        [JsonPropertyName("mothers_name")]
    //        public string? MothersName { get; set; }

    //        [JsonPropertyName("blood_type")]
    //        public string? BloodType { get; set; }

    //        [JsonPropertyName("issue_place")]
    //        public string? IssuePlace { get; set; }

    //        [JsonPropertyName("residence_place")]
    //        public string? ResidencePlace { get; set; }

    //        [JsonPropertyName("registration_place")]
    //        public string? RegistrationPlace { get; set; }

    //        [JsonPropertyName("document_number")]
    //        public string? DocumentNumber { get; set; }

    //        [JsonPropertyName("nationality")]
    //        public string? Nationality { get; set; }
    //    }
    //    public class OCR_Request
    //    {
    //        public IFormFile frontImage { get; set; }
    //        public IFormFile backImage { get; set; }

    //    }
    //}
    public class MrzData
    {
        [JsonPropertyName("mrz_raw")]
        public List<string>? RawLines { get; set; }

        [JsonPropertyName("mrz_lines_detected")]
        public int LinesDetected { get; set; }

        [JsonPropertyName("mrz_l1")]
        public string? Line1 { get; set; }

        [JsonPropertyName("mrz_l2")]
        public string? Line2 { get; set; }

        [JsonPropertyName("mrz_l3")]           // TD1 (national ID) only
        public string? Line3 { get; set; }

        [JsonPropertyName("mrz_doc_type")]
        public string? DocType { get; set; }

        [JsonPropertyName("mrz_country")]
        public string? Country { get; set; }

        [JsonPropertyName("mrz_nationality")]
        public string? Nationality { get; set; }

        [JsonPropertyName("mrz_passport_no")]  // TD3 (passport)
        public string? PassportNo { get; set; }

        [JsonPropertyName("mrz_doc_number")]   // TD1 (national ID)
        public string? DocNumber { get; set; }

        [JsonPropertyName("mrz_national_id")]
        public string? NationalId { get; set; }

        [JsonPropertyName("mrz_surname")]
        public string? Surname { get; set; }

        [JsonPropertyName("mrz_given_names")]
        public string? GivenNames { get; set; }

        [JsonPropertyName("mrz_name_full")]
        public string? FullName { get; set; }

        [JsonPropertyName("mrz_dob")]
        public string? DobRaw { get; set; }

        [JsonPropertyName("mrz_dob_parsed")]
        public string? DobParsed { get; set; }

        [JsonPropertyName("mrz_sex")]
        public string? Sex { get; set; }

        [JsonPropertyName("mrz_expiry")]
        public string? ExpiryRaw { get; set; }

        [JsonPropertyName("mrz_expiry_parsed")]
        public string? ExpiryParsed { get; set; }

        [JsonPropertyName("mrz_checks")]
        public Dictionary<string, bool>? CheckDigits { get; set; }

        [JsonPropertyName("mrz_valid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }



    public class PassportData
    {
        [JsonPropertyName("passport_no")]
        public string? PassportNo { get; set; }

        [JsonPropertyName("national_no")]
        public string? NationalNo { get; set; }

        [JsonPropertyName("name_en")]
        public string? NameEn { get; set; }

        [JsonPropertyName("name_ar")]
        public string? NameAr { get; set; }

        [JsonPropertyName("date_of_birth")]
        public string? DateOfBirth { get; set; }

        [JsonPropertyName("issue_date")]
        public string? IssueDate { get; set; }

        [JsonPropertyName("expiry_date")]
        public string? ExpiryDate { get; set; }

        [JsonPropertyName("gender")]
        public string? Gender { get; set; }

        [JsonPropertyName("place_of_birth")]
        public string? PlaceOfBirth { get; set; }

        [JsonPropertyName("mothers_name")]
        public string? MothersName { get; set; }

        [JsonPropertyName("authority")]
        public string? Authority { get; set; }

        [JsonPropertyName("nationality")]
        public string? Nationality { get; set; }

        // Conflict flags (set when MRZ and VIZ disagree)
        [JsonPropertyName("name_en_mrz")]
        public string? NameEnMrz { get; set; }

        [JsonPropertyName("name_en_conflict")]
        public bool NameConflict { get; set; }

        // Convenience helpers
        public bool IsExpired =>
            TryParseDate(ExpiryDate, out var d) && d < DateTime.Today;

        public DateTime? ExpiryDateTime =>
            TryParseDate(ExpiryDate, out var d) ? d : null;

        public DateTime? BirthDateTime =>
            TryParseDate(DateOfBirth, out var d) ? d : null;

        private static bool TryParseDate(string? raw, out DateTime result)
        {
            result = default;
            return raw != null &&
                   DateTime.TryParseExact(raw.Replace(" (MRZ)", ""),
                       "dd/MM/yyyy",
                       System.Globalization.CultureInfo.InvariantCulture,
                       System.Globalization.DateTimeStyles.None,
                       out result);
        }


        public class NationalIdData
        {
            [JsonPropertyName("national_number")]
            public string? NationalNumber { get; set; }

            [JsonPropertyName("full_name_ar")]
            public string? FullNameAr { get; set; }

            [JsonPropertyName("full_name_en")]
            public string? FullNameEn { get; set; }

            [JsonPropertyName("date_of_birth")]
            public string? DateOfBirth { get; set; }

            [JsonPropertyName("gender")]
            public string? Gender { get; set; }

            [JsonPropertyName("place_of_birth")]
            public string? PlaceOfBirth { get; set; }

            [JsonPropertyName("mothers_name_ar")]
            public string? MothersNameAr { get; set; }

            [JsonPropertyName("blood_type")]
            public string? BloodType { get; set; }

            [JsonPropertyName("expiry_date")]
            public string? ExpiryDate { get; set; }

            [JsonPropertyName("issue_place")]
            public string? IssuePlace { get; set; }

            [JsonPropertyName("residence_place")]
            public string? ResidencePlace { get; set; }

            [JsonPropertyName("document_number")]
            public string? DocumentNumber { get; set; }

            [JsonPropertyName("registry_no")]
            public string? RegistryNo { get; set; }

            [JsonPropertyName("nationality")]
            public string? Nationality { get; set; }

            // Conflict flags
            [JsonPropertyName("full_name_en_mrz")]
            public string? FullNameEnMrz { get; set; }

            [JsonPropertyName("full_name_en_conflict")]
            public bool NameConflict { get; set; }

            [JsonPropertyName("front_text_ar")]
            public string? FrontTextAr { get; set; }

            [JsonPropertyName("front_text_en")]
            public string? FrontTextEn { get; set; }

            [JsonPropertyName("back_text_ar")]
            public string? BackTextAr { get; set; }

            [JsonPropertyName("back_text_en")]
            public string? BackTextEn { get; set; }

            [JsonPropertyName("front_raw_tokens")]
            public List<string>? FrontRawTokens { get; set; }

            [JsonPropertyName("back_raw_tokens")]
            public List<string>? BackRawTokens { get; set; }

            public bool IsExpired =>
                TryParseDate(ExpiryDate, out var d) && d < DateTime.Today;

            public DateTime? ExpiryDateTime =>
                TryParseDate(ExpiryDate, out var d) ? d : null;

            public DateTime? BirthDateTime =>
                TryParseDate(DateOfBirth, out var d) ? d : null;

            private static bool TryParseDate(string? raw, out DateTime result)
            {
                result = default;
                return raw != null &&
                       DateTime.TryParseExact(raw.Replace(" (MRZ)", ""),
                           "dd/MM/yyyy",
                           System.Globalization.CultureInfo.InvariantCulture,
                           System.Globalization.DateTimeStyles.None,
                           out result);
            }
        }
        public class QualityInfo
        {
            [JsonPropertyName("blur")] public double Blur { get; set; }
            [JsonPropertyName("brightness")] public double Brightness { get; set; }
            [JsonPropertyName("contrast")] public double Contrast { get; set; }
            [JsonPropertyName("usable")] public bool Usable { get; set; }
        }

        public class NationalIdQualityInfo
        {
            [JsonPropertyName("blur")] public double Blur { get; set; }
            [JsonPropertyName("brightness")] public double Brightness { get; set; }
            [JsonPropertyName("contrast")] public double Contrast { get; set; }
            [JsonPropertyName("usable")] public bool Usable { get; set; }
            [JsonPropertyName("front")] public QualityInfo? Front { get; set; }
            [JsonPropertyName("back")] public QualityInfo? Back { get; set; }
        }

        public class VehicleLicenseData
        {
            public bool Success { get; set; }
            public string DocType { get; set; }
            public double AccuracyPercentage { get; set; }
            public VehicleLicenseRespnse Data { get; set; }
            public VehicleLicenseQuality Quality { get; set; }
        }

        public class VehicleLicenseRespnse
        {
            // Front fields
            public string LicenseNumber { get; set; }
            public string OwnerNameAr { get; set; }
            public string IssueDate { get; set; }
            public string ExpiryDate { get; set; }
            public string VehicleType { get; set; }
            public string UsageType { get; set; }
            public string RegistrationPlace { get; set; }

            // Back fields
            public string PlateNumber { get; set; }
            public string ChassisNumber { get; set; }
            public string PolicyNumber { get; set; }
            public string RegistrationNumber { get; set; }
            public string EngineNumber { get; set; }
            public string Make { get; set; }
            public string Model { get; set; }
            public string Year { get; set; }
            public string Color { get; set; }
            public string Seats { get; set; }
            public string EmptyWeightKg { get; set; }
            public string PayloadKg { get; set; }
        }

        public class VehicleLicenseQuality
        {
            public VehicleLicenseSideQuality Front { get; set; }
            public VehicleLicenseSideQuality Back { get; set; }
        }

        public class VehicleLicenseSideQuality
        {
            public double Blur { get; set; }
            public double Brightness { get; set; }
            public double Contrast { get; set; }
            public bool Usable { get; set; }
            public int FieldsFound { get; set; }
            public int FieldsRequired { get; set; }
            public bool Readable { get; set; }
        }

        public class OcrResponse<TData> : OcrResponse<TData, QualityInfo> { }

        public class OcrResponse<TData, TQuality>
        {
            [JsonPropertyName("success")] public bool Success { get; set; }
            [JsonPropertyName("doc_type")] public string? DocType { get; set; }
            [JsonPropertyName("accuracy_percentage")] public double? AccuracyPercentage { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("error")] public string? Error { get; set; }
            [JsonPropertyName("data")] public TData? Data { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("mrz")] public MrzData? Mrz { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            [JsonPropertyName("quality")] public TQuality? Quality { get; set; }
        }
    }
    public class PassportResponse : OcrResponse<PassportData> { }
    public class VehicleResponse : OcrResponse<VehicleLicenseData> { }

    public class NationalIdResponse : OcrResponse<NationalIdData, NationalIdQualityInfo> { }
}
