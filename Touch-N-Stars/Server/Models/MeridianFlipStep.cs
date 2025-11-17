namespace TouchNStars.Server.Models;

public class MeridianFlipStep
{
    /// <summary>
    /// Eindeutige ID des Steps (z.B. "PassMeridian", "Flip", "Autofocus", "Recenter", "Settle")
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Bezeichnung des Steps (lokalisierter Text)
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Ist dieser Step bereits abgeschlossen?
    /// </summary>
    public bool Finished { get; set; }
}
