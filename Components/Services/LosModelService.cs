using LengthOfStayPredictor.Models;
using Microsoft.ML;
using System.Data;
using System.Globalization;

namespace LengthOfStayPredictor.Services;

/// <summary>
/// Trains a regression model to predict Length of Stay (days)
/// and provides prediction + waitlist simulation capabilities.
/// </summary>
public class LosModelService
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private PredictionEngine<ModelInput, LengthOfStayPrediction>? _predictionEngine;

    public LosModelService()
    {
        _mlContext = new MLContext(seed: 42);
    }

    /// <summary>
    /// Model evaluation results returned after training.
    /// </summary>
    public record ModelMetrics(double RSquared, double MeanAbsoluteError, double RootMeanSquaredError);

    /// <summary>
    /// A single month's snapshot for the velocity chart.
    /// </summary>
    public record MonthlySnapshot(string Month, decimal Admitted, decimal Occupancy, decimal WaitlistRemaining);

    /// <summary>
    /// Loads historical CSV, transforms raw records into features, trains the model,
    /// and returns evaluation metrics.
    /// </summary>
    public ModelMetrics TrainModel(string csvPath)
    {
        // Load raw CSV
        IDataView rawData = _mlContext.Data.LoadFromTextFile<PatientRecord>(
            csvPath, hasHeader: true, separatorChar: ',');

        // Transform raw records → ModelInput (compute age, LOS)
        var records = _mlContext.Data.CreateEnumerable<PatientRecord>(rawData, reuseRowObject: false);
        var inputs = records.Select(ToModelInput).Where(m => m.LengthOfStayDays > 0).ToList();

        IDataView trainingData = _mlContext.Data.LoadFromEnumerable(inputs);

        // Train/test split
        var split = _mlContext.Data.TrainTestSplit(trainingData, testFraction: 0.2);

        // Build pipeline
        var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding(
                outputColumnName: "DiagnosisEncoded", inputColumnName: nameof(ModelInput.Diagnosis))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                outputColumnName: "ReferralDiagEncoded", inputColumnName: nameof(ModelInput.ReferralDiagnosis)))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                outputColumnName: "LegalStatusEncoded", inputColumnName: nameof(ModelInput.LegalStatus)))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                outputColumnName: "BarriersEncoded", inputColumnName: nameof(ModelInput.Barriers)))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                outputColumnName: "ChargesEncoded", inputColumnName: nameof(ModelInput.Charges)))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                outputColumnName: "ReligionEncoded", inputColumnName: nameof(ModelInput.Religion)))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                outputColumnName: "EthnicityEncoded", inputColumnName: nameof(ModelInput.Ethnicity)))
            .Append(_mlContext.Transforms.Categorical.OneHotEncoding(
                outputColumnName: "CountyEncoded", inputColumnName: nameof(ModelInput.County)))
            .Append(_mlContext.Transforms.Concatenate("Features",
                nameof(ModelInput.AgeAtAdmission),
                nameof(ModelInput.Sex),
                nameof(ModelInput.FacilityBeds),
                nameof(ModelInput.Education),
                nameof(ModelInput.MaritalStatus),
                nameof(ModelInput.VeteranStatus),
                "DiagnosisEncoded",
                "ReferralDiagEncoded",
                "LegalStatusEncoded",
                "BarriersEncoded",
                "ChargesEncoded",
                "ReligionEncoded",
                "EthnicityEncoded",
                "CountyEncoded"))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 20,
                numberOfTrees: 200,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.1));

        _model = pipeline.Fit(split.TrainSet);

        // Evaluate
        var predictions = _model.Transform(split.TestSet);
        var metrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: "Label");

        _predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, LengthOfStayPrediction>(_model);

        return new ModelMetrics(metrics.RSquared, metrics.MeanAbsoluteError, metrics.RootMeanSquaredError);
    }

    /// <summary>
    /// Predicts the length of stay for each person on the waitlist.
    /// </summary>
    public void PredictWaitlist(List<WaitlistPerson> waitlist, int facilityBeds)
    {
        if (_predictionEngine is null)
            throw new InvalidOperationException("Model must be trained before making predictions.");

        foreach (var person in waitlist)
        {
            var input = new ModelInput
            {
                AgeAtAdmission = (float)(DateTime.Today - person.DateOfBirth).TotalDays / 365.25f,
                Diagnosis = person.Diagnosis,
                ReferralDiagnosis = person.ReferralDiagnosis,
                LegalStatus = person.LegalStatus,
                Sex = person.Sex,
                FacilityBeds = facilityBeds,
                Barriers = person.Barriers,
                Charges = person.Charges,
                Religion = person.Religion,
                Education = person.Education,
                Ethnicity = person.Ethnicity,
                MaritalStatus = person.MaritalStatus,
                County = person.County,
                VeteranStatus = person.VeteranStatus
            };

            var prediction = _predictionEngine.Predict(input);
            person.PredictedLengthOfStayDays = Math.Max(prediction.PredictedLengthOfStayDays, 14);
        }
    }

    /// <summary>
    /// Simulates the waitlist month-by-month and returns structured data for charting.
    /// </summary>
    public static List<MonthlySnapshot> SimulateWaitlistVelocityData(
        List<WaitlistPerson> waitlist,
        int totalBeds,
        int currentOccupancy,
        int monthsToSimulate = 36)
    {
        var results = new List<MonthlySnapshot>();
        var inFacility = new List<float>();

        float avgLos = waitlist.Count > 0
            ? waitlist.Average(w => w.PredictedLengthOfStayDays)
            : 180;

        var rng = new Random(7);
        for (int i = 0; i < currentOccupancy; i++)
            inFacility.Add(rng.Next(1, (int)avgLos));

        var waitQueue = new Queue<WaitlistPerson>(waitlist);
        var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        for (int month = 0; month < monthsToSimulate; month++)
        {
            var currentMonth = startDate.AddMonths(month);
            int daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);

            int discharged = inFacility.RemoveAll(d => d <= daysInMonth);

            for (int i = 0; i < inFacility.Count; i++)
                inFacility[i] -= daysInMonth;

            int openBeds = totalBeds - inFacility.Count;
            int admitted = 0;
            while (openBeds > 0 && waitQueue.Count > 0)
            {
                var person = waitQueue.Dequeue();
                inFacility.Add(person.PredictedLengthOfStayDays);
                openBeds--;
                admitted++;
            }

            results.Add(new MonthlySnapshot(
                currentMonth.ToString("MMM yyyy"),
                admitted,
                inFacility.Count,
                waitQueue.Count));

            if (waitQueue.Count == 0 && month > 0)
                break;
        }

        return results;
    }

    /// <summary>Converts a raw CSV record to a feature-engineered ModelInput.</summary>
    private static ModelInput ToModelInput(PatientRecord record)
    {
        var dob = DateTime.ParseExact(record.DateOfBirth!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var admission = DateTime.ParseExact(record.DateOfAdmission!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var discharge = DateTime.ParseExact(record.DateOfDischarge!, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        return new ModelInput
        {
            LengthOfStayDays = (float)(discharge - admission).TotalDays,
            AgeAtAdmission = (float)(admission - dob).TotalDays / 365.25f,
            Diagnosis = record.Diagnosis,
            ReferralDiagnosis = record.ReferralDiagnosis,
            LegalStatus = record.LegalStatus,
            Sex = record.Sex,
            FacilityBeds = record.FacilityBeds,
            Barriers = record.Barriers,
            Charges = record.Charges,
            Religion = record.Religion,
            Education = record.Education,
            Ethnicity = record.Ethnicity,
            MaritalStatus = record.MaritalStatus,
            County = record.County,
            VeteranStatus = record.VeteranStatus
        };
    }
}