namespace LengthOfStayPredictor.Models;

/// <summary>
/// A person currently on the facility waitlist awaiting admission.
/// </summary>
public class WaitlistPerson
{
    public int Id { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Diagnosis { get; set; }
    public string? ReferralDiagnosis { get; set; }
    public string? LegalStatus { get; set; }
    public int Sex { get; set; }
    public string? Barriers { get; set; }
    public string? Charges { get; set; }
    public string? Religion { get; set; }
    public int Education { get; set; }
    public string? Ethnicity { get; set; }
    public int MaritalStatus { get; set; }
    public string? County { get; set; }
    public int VeteranStatus { get; set; }

    /// <summary>Predicted length of stay in days (populated after prediction).</summary>
    public float PredictedLengthOfStayDays { get; set; }
}