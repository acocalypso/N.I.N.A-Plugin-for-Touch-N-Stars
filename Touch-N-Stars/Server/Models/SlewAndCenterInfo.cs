using System.Collections.Generic;

namespace TouchNStars.Server.Models;

public class SlewAndCenterInfo
{
    /// <summary>
    /// Ist Slew and Center aktiv?
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Status des aktuellen Vorgangs (z.B. "Exposing", "Solving", "Centering")
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Aktuelle Messung
    /// </summary>
    public SlewAndCenterMeasurement CurrentMeasurement { get; set; }

    /// <summary>
    /// Rotation in Grad
    /// </summary>
    public string Rotation { get; set; }

    /// <summary>
    /// Anzahl der durchgeführten Messungen
    /// </summary>
    public int MeasurementCount { get; set; }

    /// <summary>
    /// Alle durchgeführten Messungen (komplette Historie)
    /// </summary>
    public List<SlewAndCenterMeasurement> Measurements { get; set; } = new List<SlewAndCenterMeasurement>();
}

public class SlewAndCenterMeasurement
{
    /// <summary>
    /// Zeitstempel der Messung
    /// </summary>
    public string Time { get; set; }

    /// <summary>
    /// War die Messung erfolgreich?
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Fehlerabstand in Arcsekunden
    /// </summary>
    public string ErrorDistance { get; set; }

    /// <summary>
    /// Rotation in Grad (Position Angle)
    /// </summary>
    public string Rotation { get; set; }
}
