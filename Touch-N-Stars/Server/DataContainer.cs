using System;
using TouchNStars.Server;

internal static class DataContainer {
    internal static object lockObj = new object();

    internal static GuiderData guiderData = new GuiderData();
    internal static bool afRun = false;
    internal static bool afError = false;
    internal static string afErrorText = string.Empty;
    internal static bool newAfGraph = false;
    internal static DateTime lastAfTimestamp = DateTime.MinValue;
    internal static bool wshvActive = false;
    internal static int wshvPort = 80;
}