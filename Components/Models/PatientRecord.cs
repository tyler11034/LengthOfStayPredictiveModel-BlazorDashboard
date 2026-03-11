using Microsoft.ML.Data;

namespace LengthOfStayPredictor.Models;

/// <summary>
/// Historical patient record used for training the ML model.
/// </summary>
public class PatientRecord
{
    [LoadColumn(0)]
    public float Id { get; set; }

    [LoadColumn(1)]
    public string? DateOfBirth { get; set; }

    [LoadColumn(2)]
    public string? DateOfAdmission { get; set; }

    [LoadColumn(3)]
    public string? DateOfDischarge { get; set; }

    [LoadColumn(4)]
    public string? Diagnosis { get; set; }

    [LoadColumn(5)]
    public string? ReferralDiagnosis { get; set; }

    [LoadColumn(6)]
    public string? LegalStatus { get; set; }

    [LoadColumn(7)]
    public float Sex { get; set; }

    [LoadColumn(8)]
    public float FacilityBeds { get; set; }

    [LoadColumn(9)]
    public string? Barriers { get; set; }

    [LoadColumn(10)]
    public string? Charges { get; set; }

    [LoadColumn(11)]
    public string? Religion { get; set; }

    [LoadColumn(12)]
    public float Education { get; set; }

    [LoadColumn(13)]
    public string? Ethnicity { get; set; }

    [LoadColumn(14)]
    public float MaritalStatus { get; set; }

    [LoadColumn(15)]
    public string? County { get; set; }

    [LoadColumn(16)]
    public float VeteranStatus { get; set; }
}