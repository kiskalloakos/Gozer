using System.Globalization;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Fast expedition test setup. Use RPG > Tests > Set Village Time to 6 PM.
/// </summary>
public static class ExpeditionTimeTest
{
    const string MinutesKey = "Village.TotalMinutes";
    const int SixPmMinute = 18 * 60;

    [MenuItem("RPG/Tests/Set Village Time to 6 PM")]
    public static void SetVillageTimeToSixPm()
    {
        if (VillageTime.Instance)
        {
            VillageTime.Instance.SetForTesting(SixPmMinute);
        }
        else
        {
            string saved = PlayerPrefs.GetString(MinutesKey, VillageTime.MorningMinute.ToString(CultureInfo.InvariantCulture));
            double totalMinutes = double.TryParse(saved, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                && !double.IsNaN(parsed) && !double.IsInfinity(parsed) && parsed >= 0d
                ? parsed
                : VillageTime.MorningMinute;
            double dayStart = System.Math.Floor(totalMinutes / 1440d) * 1440d;
            PlayerPrefs.SetString(MinutesKey, (dayStart + SixPmMinute).ToString("R", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        Debug.Log("RPG test clock set to 6:00 PM. Expedition entry will unlock at 7:00 PM.");
    }
}
