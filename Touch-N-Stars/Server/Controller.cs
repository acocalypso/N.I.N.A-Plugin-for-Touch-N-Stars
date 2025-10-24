using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.WPF.Base.SkySurvey;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TouchNStars.Utility;
using TouchNStars.PHD2;
using TouchNStars.SequenceItems;

namespace TouchNStars.Server;

public class NullableDoubleConverter : JsonConverter<double?> {
    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType == JsonTokenType.Null) {
            return null;
        }
        if (reader.TokenType == JsonTokenType.String) {
            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue)) {
                return null;
            }
            if (double.TryParse(stringValue, out double result)) {
                return result;
            }
            return null;
        }
        if (reader.TokenType == JsonTokenType.Number) {
            return reader.GetDouble();
        }
        return null;
    }

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options) {
        if (value.HasValue) {
            writer.WriteNumberValue(value.Value);
        } else {
            writer.WriteNullValue();
        }
    }
}

public class FavoriteTarget {
    public Guid Id { get; set; } = Guid.NewGuid(); // Wird automatisch gesetzt
    public string Name { get; set; }
    public double Ra { get; set; }
    public double Dec { get; set; }
    public string RaString { get; set; }
    public string DecString { get; set; }
    [JsonConverter(typeof(NullableDoubleConverter))]
    public double? Rotation { get; set; }
}

public class Setting {
    public string Key { get; set; }
    public string Value { get; set; }
}

// NGCSearchResult moved to Controllers/TargetSearchController.cs

public class ApiResponse {
    public bool Success { get; set; }
    public object Response { get; set; }
    public string Error { get; set; }
    public int StatusCode { get; set; }
    public string Type { get; set; }


}

public class Controller : WebApiController {

    // This controller is now empty - all endpoints have been moved to specialized controllers:
    // - Autofocus endpoint moved to Controllers/AutofocusController.cs
    // - Logs, get-api-port, and version endpoints moved to Controllers/UtilityController.cs
    // - NGC Search and Target Picture endpoints moved to Controllers/TargetSearchController.cs
    // - Favorites endpoints moved to Controllers/FavoritesController.cs
    // - Settings endpoints moved to Controllers/SettingsController.cs
    // - System endpoints moved to Controllers/SystemController.cs
    // - MessageBox endpoints moved to Controllers/MessageBoxController.cs
    // - Telescopius proxy endpoints moved to Controllers/TelescopiusController.cs
    // - PHD2 API endpoints moved to Controllers/PHD2Controller.cs
    // - NINA Dialog Control endpoints moved to Controllers/DialogController.cs

}
