using System;
using System.Text.Json.Serialization;

namespace TouchNStars.Server.Models;

public class FavoriteTarget
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Wird automatisch gesetzt
    public string Name { get; set; }
    public double Ra { get; set; }
    public double Dec { get; set; }
    public string RaString { get; set; }
    public string DecString { get; set; }
    [JsonConverter(typeof(NullableDoubleConverter))]
    public double? Rotation { get; set; }
}
