using Microsoft.ML.Data;

namespace LengthOfStayPredictor.Models;

/// <summary>
/// Transformed feature set fed into the ML pipeline.
/// Age at admission is derived from DateOfBirth and DateOfAdmission.
/// LengthOfStayDays (the label) is derived from admission/discharge dates.
/// </summary>
public class ModelInput
{
    [ColumnName("Label")]
    public float LengthOfStayDays { get; set; }

    public float AgeAtAdmission { get; set; }
    public string? Diagnosis { get; set; }
    public string? ReferralDiagnosis { get; set; }
    public string? LegalStatus { get; set; }
    public float Sex { get; set; }
    public float FacilityBeds { get; set; }
    public string? Barriers { get; set; }
    public string? Charges { get; set; }
    public string? Religion { get; set; }
    public float Education { get; set; }
    public string? Ethnicity { get; set; }
    public float MaritalStatus { get; set; }
    public string? County { get; set; }
    public float VeteranStatus { get; set; }
}