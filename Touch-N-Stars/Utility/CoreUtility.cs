using System;
using System.IO;

namespace TouchNStars.Utility;

public static class CoreUtility {
    public const string BASE_API_URL = "http://localhost:1888/v2/api";

    public static readonly string CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NINA", "FramingAssistantCache");
    public static readonly string LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NINA", "Logs");
    public static readonly string AfPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NINA", "AutoFocus");
    public static readonly string Hips2FitsUrl = "http://alaskybis.u-strasbg.fr/hips-image-services/hips2fits";

    public static double HmsToDegrees(string hms) {
        string[] hmsParts = hms.Split(':');
        if (hmsParts.Length == 3) {
            return (double.Parse(hmsParts[0]) * 15) + (double.Parse(hmsParts[1]) * (15.0 / 60)) + (double.Parse(hmsParts[2]) * (15.0 / 3600));
        } else {
            throw new ArgumentException("HMS string must be in the format HH:MM:SS, was: " + hms);
        }
    }

    public static double DmsToDegrees(string dms) {
        int sign = dms.StartsWith('-') ? -1 : 1;
        string stripped = dms.Remove(0, 1);

        string[] dmsParts = stripped.Split(':');

        if (dmsParts.Length == 3) {
            return (double.Parse(dmsParts[0]) + (double.Parse(dmsParts[1]) / 60) + (double.Parse(dmsParts[2]) / 3600)) * sign;
        } else {
            throw new ArgumentException("DMS string must be in the format DD:MM:SS.s, was: " + dms);
        }
    }
}
