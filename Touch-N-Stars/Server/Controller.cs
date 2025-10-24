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

    private static readonly List<string> excluded_members = new List<string>() { "GetEquipment", "RequestAll", "LoadPlugin" };
    // FavoritesFilePath and _fileLock moved to Controllers/FavoritesController.cs
    // SettingsFilePath and _fileLock moved to Controllers/SettingsController.cs
    // PHD2 services moved to Controllers/PHD2Controller.cs

    [Route(HttpVerbs.Get, "/logs")]
    public List<Hashtable> GetRecentLogs([QueryField(true)] int count, [QueryField] string level) {
        List<Hashtable> logs = new List<Hashtable>();

        if (string.IsNullOrEmpty(level)) {
            level = string.Empty;
        }

        if (level.Equals("ERROR") || level.Equals("WARNING") || level.Equals("INFO") || level.Equals("DEBUG") || string.IsNullOrEmpty(level)) {
            string currentLogFile = Directory.GetFiles(CoreUtility.LogPath).OrderByDescending(File.GetCreationTime).First();

            string[] logLines = [];

            using (var stream = new FileStream(currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (var reader = new StreamReader(stream)) {
                    string content = reader.ReadToEnd();
                    logLines = content.Split('\n');
                }
            }

            List<string> filteredLogLines = new List<string>();
            foreach (string line in logLines) {
                bool valid = true;

                if (!line.Contains('|' + level + '|') && !string.IsNullOrEmpty(level)) {
                    valid = false;
                }
                if (line.Contains("DATE|LEVEL|SOURCE|MEMBER|LINE|MESSAGE")) {
                    valid = false;
                }
                foreach (string excluded_member in excluded_members) {
                    if (line.Contains(excluded_member)) {
                        valid = false;
                    }
                }
                if (valid) {
                    filteredLogLines.Add(line);
                }
            }
            IEnumerable<string> lines = filteredLogLines.TakeLast(count);
            foreach (string line in lines) {
                string[] parts = line.Split('|');
                if (parts.Length >= 6) {
                    logs.Add(new Hashtable() {
                        { "timestamp", parts[0] },
                        { "level", parts[1] },
                        { "source", parts[2] },
                        { "member", parts[3] },
                        { "line", parts[4] },
                        { "message", string.Join('|', parts.Skip(5)).Trim() }
                    });
                }
            }
        }
        logs.Reverse();
        return logs;
    }

    [Route(HttpVerbs.Get, "/autofocus/{action}")]
    public async Task<object> ControlAutofocus(string action) {
        string targetUrl = $"{await CoreUtility.GetApiUrl()}/equipment/focuser/auto-focus";
        bool info = action.Equals("info");
        bool start = action.Equals("start");
        bool stop = action.Equals("stopp");

        if (info) {
            return new Dictionary<string, object>() {
                { "Success", true },
                { "autofocus_running", DataContainer.afRun },
                { "newAfGraph", DataContainer.newAfGraph },
                { "afError", DataContainer.afError },
                { "afErrorText", DataContainer.afErrorText },
            };
        }
        if (start) {
            DataContainer.afRun = true;
            DataContainer.newAfGraph = false;
            DataContainer.afError = false;
            DataContainer.afErrorText = string.Empty;

            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(targetUrl);

                if (response.IsSuccessStatusCode) {
                    ApiResponse apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
                    if (apiResponse.Success) {
                        return new Dictionary<string, object>() { { "message", "Autofokus gestartet" } };
                    } else {
                        return new Dictionary<string, object>() { { "message", $"Fehler beim Starten des Autofokus: {apiResponse.Error}" } };
                    }
                } else {
                    return new Dictionary<string, object>() { { "message", $"Fehler beim Starten des Autofokus: {response.StatusCode}" } };
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                HttpContext.Response.StatusCode = 500;
                return new Dictionary<string, object>() { { "error", "Interner Fehler beim Starten des Autofokus" } };
            }
        }

        if (stop) {
            DataContainer.afRun = false;
            DataContainer.newAfGraph = false;

            try {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(targetUrl + "?cancel=true");

                if (response.IsSuccessStatusCode) {
                    ApiResponse apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
                    if (apiResponse.Success) {
                        return new Dictionary<string, object>() { { "message", "Autofokus gestoppt" } };
                    } else {
                        return new Dictionary<string, object>() { { "message", $"Fehler beim Stoppen des Autofokus: {apiResponse.Error}" } };
                    }
                } else {
                    return new Dictionary<string, object>() { { "message", $"Fehler beim Stoppen des Autofokus: {response.StatusCode}" } };
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                HttpContext.Response.StatusCode = 500;
                return new Dictionary<string, object>() { { "error", "Interner Fehler beim Stopoen des Autofokus" } };
            }
        }

        return new Dictionary<string, object>() { { "error", "Ungültige Anfrage" } };
    }

    // NGC Search and Target Picture endpoints moved to Controllers/TargetSearchController.cs

    [Route(HttpVerbs.Get, "/get-api-port")]
    public async Task<int> GetApiPort() {
        return await TouchNStars.Communicator.GetPort(true);
    }

    [Route(HttpVerbs.Get, "/version")]
    public object GetAssemblyVersion() {
        try {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString();

            return new Dictionary<string, object>
            {
            { "success", true },
            { "version", version }
        };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new Dictionary<string, object>
            {
            { "success", false },
            { "error", ex.Message }
        };
        }
    }

    // PHD2 API Endpoints moved to Controllers/PHD2Controller.cs

    // NINA Dialog Control Endpoints moved to Controllers/DialogController.cs

}
