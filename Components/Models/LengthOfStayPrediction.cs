using Microsoft.ML.Data;

namespace LengthOfStayPredictor.Models;

/// <summary>
/// ML.NET prediction output — predicted length of stay in days.
/// </summary>
public class LengthOfStayPrediction
{
    [ColumnName("Score")]
    public float PredictedLengthOfStayDays { get; set; }
}