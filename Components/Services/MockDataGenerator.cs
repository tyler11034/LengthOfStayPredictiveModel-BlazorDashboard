using System.Globalization;
using System.Text;

namespace LengthOfStayPredictor.Services;

/// <summary>
/// Generates a synthetic historical CSV dataset for model training.
/// </summary>
public static class MockDataGenerator
{
    private static readonly string[] Diagnoses =
        ["Schizophrenia", "Schizoaffective Disorder", "Bipolar I", "Major Depressive Disorder", "PTSD", "Antisocial PD", "Substance Use Disorder"];

    private static readonly string[] LegalStatuses =
        ["IST", "NGRI", "Civil Commitment", "Guilty But Mentally Ill", "Competency Restoration"];

    private static readonly string[] BarrierOptions =
        ["None", "Housing", "Insurance", "Medication Non-compliance", "Co-occurring SUD", "Guardianship Pending", "Immigration Hold"];

    private static readonly string[] ChargeOptions =
        ["Assault", "Arson", "Burglary", "Theft", "DUI", "Homicide", "Robbery", "Trespassing", "Drug Possession", "Vandalism"];

    private static readonly string[] Religions =
        ["Christian", "Catholic", "Muslim", "Jewish", "Buddhist", "None", "Other"];

    private static readonly string[] Ethnicities =
        ["White", "Black", "Hispanic", "Asian", "Native American", "Pacific Islander", "Other"];

    private static readonly string[] Counties =
        ["Franklin", "Cuyahoga", "Hamilton", "Montgomery", "Summit", "Lucas", "Stark", "Butler", "Lorain", "Mahoning"];

    public static void GenerateCsv(string filePath, int recordCount = 1500)
    {
        var rng = new Random(42);
        var sb = new StringBuilder();

        sb.AppendLine("Id,DateOfBirth,DateOfAdmission,DateOfDischarge,Diagnosis,ReferralDiagnosis,LegalStatus,Sex,FacilityBeds,Barriers,Charges,Religion,Education,Ethnicity,MaritalStatus,County,VeteranStatus");

        for (int i = 1; i <= recordCount; i++)
        {
            var dob = new DateTime(1960, 1, 1).AddDays(rng.Next(0, 365 * 40));
            var admission = new DateTime(2010, 1, 1).AddDays(rng.Next(0, 365 * 13));

            // Simulate realistic LOS: base + modifiers
            int baseLos = rng.Next(30, 365);
            string diagnosis = Diagnoses[rng.Next(Diagnoses.Length)];
            string referralDiagnosis = Diagnoses[rng.Next(Diagnoses.Length)];
            string legalStatus = LegalStatuses[rng.Next(LegalStatuses.Length)];
            int sex = rng.Next(0, 2);
            int beds = rng.Next(50, 301);
            string barriers = BarrierOptions[rng.Next(BarrierOptions.Length)];
            string charges = ChargeOptions[rng.Next(ChargeOptions.Length)];
            string religion = Religions[rng.Next(Religions.Length)];
            int education = rng.Next(8, 21);
            string ethnicity = Ethnicities[rng.Next(Ethnicities.Length)];
            int maritalStatus = rng.Next(0, 5);
            string county = Counties[rng.Next(Counties.Length)];
            int veteranStatus = rng.NextDouble() < 0.15 ? 1 : 0;

            // Add realistic correlations to LOS
            if (legalStatus is "NGRI" or "Guilty But Mentally Ill") baseLos += rng.Next(180, 730);
            if (diagnosis == "Schizophrenia") baseLos += rng.Next(60, 200);
            if (barriers != "None") baseLos += rng.Next(30, 120);
            if (charges is "Homicide" or "Arson") baseLos += rng.Next(180, 540);

            var discharge = admission.AddDays(baseLos);
            string format = "yyyy-MM-dd";

            sb.AppendLine(string.Join(",",
                i,
                dob.ToString(format, CultureInfo.InvariantCulture),
                admission.ToString(format, CultureInfo.InvariantCulture),
                discharge.ToString(format, CultureInfo.InvariantCulture),
                diagnosis,
                referralDiagnosis,
                legalStatus,
                sex,
                beds,
                barriers,
                charges,
                religion,
                education,
                ethnicity,
                maritalStatus,
                county,
                veteranStatus));
        }

        File.WriteAllText(filePath, sb.ToString());
    }

    public static List<Models.WaitlistPerson> GenerateMockWaitlist(int count = 250)
    {
        var rng = new Random(99);
        var waitlist = new List<Models.WaitlistPerson>();

        for (int i = 1; i <= count; i++)
        {
            waitlist.Add(new Models.WaitlistPerson
            {
                Id = 10_000 + i,
                DateOfBirth = new DateTime(1965, 1, 1).AddDays(rng.Next(0, 365 * 40)),
                Diagnosis = Diagnoses[rng.Next(Diagnoses.Length)],
                ReferralDiagnosis = Diagnoses[rng.Next(Diagnoses.Length)],
                LegalStatus = LegalStatuses[rng.Next(LegalStatuses.Length)],
                Sex = rng.Next(0, 2),
                Barriers = BarrierOptions[rng.Next(BarrierOptions.Length)],
                Charges = ChargeOptions[rng.Next(ChargeOptions.Length)],
                Religion = Religions[rng.Next(Religions.Length)],
                Education = rng.Next(8, 21),
                Ethnicity = Ethnicities[rng.Next(Ethnicities.Length)],
                MaritalStatus = rng.Next(0, 5),
                County = Counties[rng.Next(Counties.Length)],
                VeteranStatus = rng.NextDouble() < 0.15 ? 1 : 0
            });
        }

        return waitlist;
    }
}