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
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TouchNStars.Utility;
using TouchNStars.PHD2;

namespace TouchNStars.Server;

public class FavoriteTarget {
    public Guid Id { get; set; } = Guid.NewGuid(); // Wird automatisch gesetzt
    public string Name { get; set; }
    public double Ra { get; set; }
    public double Dec { get; set; }
    public string RaString { get; set; }
    public string DecString { get; set; }
    public string Rotation { get; set; }
}

public class NGCSearchResult {
    public string Name { get; set; }
    public double RA { get; set; }
    public double Dec { get; set; }
}

public class ApiResponse {
    public bool Success { get; set; }
    public object Response { get; set; }
    public string Error { get; set; }
    public int StatusCode { get; set; }
    public string Type { get; set; }


}

public class Controller : WebApiController {

    private static readonly List<string> excluded_members = new List<string>() { "GetEquipment", "RequestAll", "LoadPlugin" };
    private static readonly string FavoritesFilePath = Path.Combine(
     Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
     "NINA", "TnsCache", "favorites.json"
    );
    private static readonly object _fileLock = new();
    private static PHD2Service phd2Service = new PHD2Service();



    [Route(HttpVerbs.Post, "/favorites")]
    public async Task<ApiResponse> AddFavoriteTarget() {
        try {
            var favorite = await HttpContext.GetRequestDataAsync<FavoriteTarget>();

            if (favorite.Id == Guid.Empty)
                favorite.Id = Guid.NewGuid();

            List<FavoriteTarget> currentFavorites = new();
            if (File.Exists(FavoritesFilePath)) {
                var json = await File.ReadAllTextAsync(FavoritesFilePath);
                currentFavorites = System.Text.Json.JsonSerializer.Deserialize<List<FavoriteTarget>>(json);
            }

            currentFavorites.Add(favorite);
            lock (_fileLock) {
                Directory.CreateDirectory(Path.GetDirectoryName(FavoritesFilePath));
                var updatedJson = System.Text.Json.JsonSerializer.Serialize(
                    currentFavorites,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(FavoritesFilePath, updatedJson);
            }

            return new ApiResponse {
                Success = true,
                Response = favorite,
                StatusCode = 200,
                Type = "FavoriteTarget"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Get, "/favorites")]
    public async Task<List<FavoriteTarget>> GetFavoriteTargets() {
        if (!File.Exists(FavoritesFilePath)) return new List<FavoriteTarget>();

        var json = await File.ReadAllTextAsync(FavoritesFilePath);
        return System.Text.Json.JsonSerializer.Deserialize<List<FavoriteTarget>>(json);
    }

    [Route(HttpVerbs.Delete, "/favorites/{id}")]
    public async Task<ApiResponse> DeleteFavoriteTarget(Guid id) {
        try {
            if (!File.Exists(FavoritesFilePath)) {
                return new ApiResponse { Success = false, Error = "Keine Daten vorhanden", StatusCode = 404 };
            }

            var json = await File.ReadAllTextAsync(FavoritesFilePath);
            var favorites = System.Text.Json.JsonSerializer.Deserialize<List<FavoriteTarget>>(json);

            var toRemove = favorites.FirstOrDefault(f => f.Id == id);
            if (toRemove == null) {
                return new ApiResponse { Success = false, Error = "Eintrag nicht gefunden", StatusCode = 404 };
            }

            favorites.Remove(toRemove);
            await File.WriteAllTextAsync(FavoritesFilePath, System.Text.Json.JsonSerializer.Serialize(favorites, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            return new ApiResponse { Success = true, Response = $"Favorit mit Id '{id}' gelöscht", StatusCode = 200 };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse { Success = false, Error = ex.Message, StatusCode = 500 };
        }
    }

    [Route(HttpVerbs.Put, "/favorites/{id}")]
    public async Task<ApiResponse> UpdateFavoriteTarget(Guid id) {
        try {
            var updatedTarget = await HttpContext.GetRequestDataAsync<FavoriteTarget>();

            if (!File.Exists(FavoritesFilePath)) {
                return new ApiResponse { Success = false, Error = "Keine Daten vorhanden", StatusCode = 404 };
            }

            var json = await File.ReadAllTextAsync(FavoritesFilePath);
            var favorites = System.Text.Json.JsonSerializer.Deserialize<List<FavoriteTarget>>(json);

            var existing = favorites.FirstOrDefault(f => f.Id == id);
            if (existing == null) {
                return new ApiResponse { Success = false, Error = "Eintrag nicht gefunden", StatusCode = 404 };
            }

            existing.Name = updatedTarget.Name;
            existing.Ra = updatedTarget.Ra;
            existing.Dec = updatedTarget.Dec;
            existing.DecString = updatedTarget.DecString;
            existing.RaString = updatedTarget.RaString;
            existing.Rotation = updatedTarget.Rotation;


            await File.WriteAllTextAsync(FavoritesFilePath, System.Text.Json.JsonSerializer.Serialize(favorites, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            return new ApiResponse { Success = true, Response = existing, StatusCode = 200 };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse { Success = false, Error = ex.Message, StatusCode = 500 };
        }
    }




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

    [Route(HttpVerbs.Get, "/ngc/search")]
    public async Task<object> SearcgNGC([QueryField(true)] string query, [QueryField] int limit) {

        TouchNStars.Mediators.DeepSkyObjectSearchVM.Limit = limit;
        TouchNStars.Mediators.DeepSkyObjectSearchVM.TargetName = query; // Setting the target name automatically starts the search

        await TouchNStars.Mediators.DeepSkyObjectSearchVM.TargetSearchResult.Task; // Wait for the search to finsish
        List<NGCSearchResult> results = new List<NGCSearchResult>();

        foreach (var result in TouchNStars.Mediators.DeepSkyObjectSearchVM.TargetSearchResult.Result) { // bring the results in a better format
            results.Add(new NGCSearchResult() {
                Name = result.Column1,
                RA = CoreUtility.HmsToDegrees(result.Column2),
                Dec = CoreUtility.DmsToDegrees(result.Column3.Replace(" ", "").Replace('°', ':').Replace('\'', ':').Replace("\"", "")) // maybe use reflection to directly get the coordinates
            });
        }

        return results;
    }

    [Route(HttpVerbs.Get, "/system/shutdown")]
    public object SystemShutdown() {
        try {
            var process = new System.Diagnostics.Process {
                StartInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "powershell",
                    Arguments = "-Command \"Stop-Computer -Force\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();

            return new Dictionary<string, object>() {
                { "success", true },
                { "message", "System shutdown initiated" }
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new Dictionary<string, object>() {
                { "success", false },
                { "error", ex.Message }
            };
        }
    }

    [Route(HttpVerbs.Get, "/system/restart")]
    public object SystemRestart() {
        try {
            var process = new System.Diagnostics.Process {
                StartInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "powershell",
                    Arguments = "-Command \"Restart-Computer -Force\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();

            return new Dictionary<string, object>() {
                { "success", true },
                { "message", "System restart initiated" }
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new Dictionary<string, object>() {
                { "success", false },
                { "error", ex.Message }
            };
        }
    }

    [Route(HttpVerbs.Get, "/targetpic")]
    public async Task FetchTargetPicture([QueryField(true)] int width, [QueryField(true)] int height, [QueryField(true)] double fov, [QueryField(true)] double ra, [QueryField(true)] double dec, [QueryField] bool useCache) {
        try {
            HttpContext.Response.ContentType = "image/jpeg";
            if (useCache) {
                string framingCache = TouchNStars.Mediators.Profile.ActiveProfile.ApplicationSettings.SkySurveyCacheDirectory;
                CacheSkySurveyImageFactory factory = new CacheSkySurveyImageFactory(width, height, new CacheSkySurvey(framingCache));
                BitmapSource source = factory.Render(new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(dec), Epoch.J2000), fov, 0);

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 100;
                using (MemoryStream stream = new MemoryStream()) {
                    encoder.Frames.Add(BitmapFrame.Create(source));
                    encoder.Save(stream);
                    stream.Position = 0;
                    Response.OutputStream.Write(stream.ToArray(), 0, (int)stream.Length);
                }
            } else {
                HttpClient client = new HttpClient();
                byte[] image = await client.GetByteArrayAsync($"{CoreUtility.Hips2FitsUrl}?width={width}&height={height}&fov={fov}&ra={ra}&dec={dec}&hips=CDS/P/DSS2/color&projection=STG&format=jpg");
                Response.OutputStream.Write(image, 0, image.Length);

                client.Dispose();
            }

        } catch (Exception ex) {
            Logger.Error(ex);
            throw;
        }
    }

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

    // PHD2 API Endpoints

    public static void CleanupPHD2Service() {
        phd2Service?.Dispose();
    }

    [Route(HttpVerbs.Get, "/phd2/status")]
    public async Task<ApiResponse> GetPHD2Status() {
        try {
            var status = await phd2Service.GetStatusAsync();
            return new ApiResponse {
                Success = true,
                Response = status,
                StatusCode = 200,
                Type = "PHD2Status"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Post, "/phd2/connect")]
    public async Task<ApiResponse> ConnectPHD2() {
        try {
            var requestData = await HttpContext.GetRequestDataAsync<dynamic>();
            string hostname = requestData?.hostname ?? "localhost";
            uint instance = requestData?.instance ?? 1;

            bool result = await phd2Service.ConnectAsync(hostname, instance);
            
            return new ApiResponse {
                Success = result,
                Response = new { Connected = result, Error = phd2Service.LastError },
                StatusCode = result ? 200 : 400,
                Type = "PHD2Connection"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Post, "/phd2/disconnect")]
    public async Task<ApiResponse> DisconnectPHD2() {
        try {
            await phd2Service.DisconnectAsync();
            
            return new ApiResponse {
                Success = true,
                Response = new { Connected = false },
                StatusCode = 200,
                Type = "PHD2Connection"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Get, "/phd2/profiles")]
    public async Task<ApiResponse> GetPHD2Profiles() {
        try {
            var profiles = await phd2Service.GetEquipmentProfilesAsync();
            
            return new ApiResponse {
                Success = true,
                Response = profiles,
                StatusCode = 200,
                Type = "PHD2Profiles"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Post, "/phd2/connect-equipment")]
    public async Task<ApiResponse> ConnectPHD2Equipment() {
        try {
            var requestData = await HttpContext.GetRequestDataAsync<dynamic>();
            string profileName = requestData?.profileName;

            if (string.IsNullOrEmpty(profileName)) {
                HttpContext.Response.StatusCode = 400;
                return new ApiResponse {
                    Success = false,
                    Error = "Profile name is required",
                    StatusCode = 400,
                    Type = "Error"
                };
            }

            bool result = await phd2Service.ConnectEquipmentAsync(profileName);
            
            return new ApiResponse {
                Success = result,
                Response = new { EquipmentConnected = result, Error = phd2Service.LastError },
                StatusCode = result ? 200 : 400,
                Type = "PHD2Equipment"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Post, "/phd2/start-guiding")]
    public async Task<ApiResponse> StartPHD2Guiding() {
        try {
            var requestData = await HttpContext.GetRequestDataAsync<dynamic>();
            double settlePixels = requestData?.settlePixels ?? 2.0;
            double settleTime = requestData?.settleTime ?? 10.0;
            double settleTimeout = requestData?.settleTimeout ?? 100.0;

            bool result = await phd2Service.StartGuidingAsync(settlePixels, settleTime, settleTimeout);
            
            return new ApiResponse {
                Success = result,
                Response = new { GuidingStarted = result, Error = phd2Service.LastError },
                StatusCode = result ? 200 : 400,
                Type = "PHD2Guiding"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Post, "/phd2/stop-guiding")]
    public async Task<ApiResponse> StopPHD2Guiding() {
        try {
            bool result = await phd2Service.StopGuidingAsync();
            
            return new ApiResponse {
                Success = result,
                Response = new { GuidingStopped = result, Error = phd2Service.LastError },
                StatusCode = result ? 200 : 400,
                Type = "PHD2Guiding"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Post, "/phd2/dither")]
    public async Task<ApiResponse> DitherPHD2() {
        try {
            var requestData = await HttpContext.GetRequestDataAsync<dynamic>();
            double ditherPixels = requestData?.ditherPixels ?? 3.0;
            double settlePixels = requestData?.settlePixels ?? 2.0;
            double settleTime = requestData?.settleTime ?? 10.0;
            double settleTimeout = requestData?.settleTimeout ?? 100.0;

            bool result = await phd2Service.DitherAsync(ditherPixels, settlePixels, settleTime, settleTimeout);
            
            return new ApiResponse {
                Success = result,
                Response = new { DitherStarted = result, Error = phd2Service.LastError },
                StatusCode = result ? 200 : 400,
                Type = "PHD2Dither"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Post, "/phd2/pause")]
    public async Task<ApiResponse> PausePHD2() {
        try {
            bool result = await phd2Service.PauseGuidingAsync();
            
            return new ApiResponse {
                Success = result,
                Response = new { Paused = result, Error = phd2Service.LastError },
                StatusCode = result ? 200 : 400,
                Type = "PHD2Pause"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Post, "/phd2/unpause")]
    public async Task<ApiResponse> UnpausePHD2() {
        try {
            bool result = await phd2Service.UnpauseGuidingAsync();
            
            return new ApiResponse {
                Success = result,
                Response = new { Unpaused = result, Error = phd2Service.LastError },
                StatusCode = result ? 200 : 400,
                Type = "PHD2Pause"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Post, "/phd2/start-looping")]
    public async Task<ApiResponse> StartPHD2Looping() {
        try {
            bool result = await phd2Service.StartLoopingAsync();
            
            return new ApiResponse {
                Success = result,
                Response = new { LoopingStarted = result, Error = phd2Service.LastError },
                StatusCode = result ? 200 : 400,
                Type = "PHD2Looping"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Get, "/phd2/settling")]
    public async Task<ApiResponse> GetPHD2Settling() {
        try {
            var settling = await phd2Service.CheckSettlingAsync();
            
            return new ApiResponse {
                Success = true,
                Response = settling,
                StatusCode = 200,
                Type = "PHD2Settling"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Get, "/phd2/pixel-scale")]
    public async Task<ApiResponse> GetPHD2PixelScale() {
        try {
            var pixelScale = await phd2Service.GetPixelScaleAsync();
            
            return new ApiResponse {
                Success = true,
                Response = new { PixelScale = pixelScale },
                StatusCode = 200,
                Type = "PHD2PixelScale"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    [Route(HttpVerbs.Get, "/phd2/all-info")]
    public async Task<ApiResponse> GetAllPHD2Info() {
        try {
            // Get all available PHD2 information in parallel
            var statusTask = phd2Service.GetStatusAsync();
            var profilesTask = phd2Service.GetEquipmentProfilesAsync();
            var settlingTask = phd2Service.CheckSettlingAsync();
            var pixelScaleTask = phd2Service.GetPixelScaleAsync();

            await Task.WhenAll(statusTask, profilesTask, settlingTask, pixelScaleTask);

            var status = await statusTask;
            var profiles = await profilesTask;
            var settling = await settlingTask;
            var pixelScale = await pixelScaleTask;

            var allInfo = new {
                Connection = new {
                    IsConnected = phd2Service.IsConnected,
                    LastError = phd2Service.LastError
                },
                Status = status,
                EquipmentProfiles = profiles,
                Settling = settling,
                PixelScale = pixelScale,
                Capabilities = new {
                    CanGuide = phd2Service.IsConnected && (status?.AppState == "Guiding" || status?.AppState == "Looping" || status?.AppState == "Stopped"),
                    CanDither = phd2Service.IsConnected && status?.AppState == "Guiding",
                    CanPause = phd2Service.IsConnected && status?.AppState == "Guiding",
                    CanLoop = phd2Service.IsConnected && status?.AppState == "Stopped"
                },
                GuideStats = status?.Stats != null ? new {
                    RmsTotal = status.Stats.RmsTotal,
                    RmsRA = status.Stats.RmsRA,
                    RmsDec = status.Stats.RmsDec,
                    PeakRA = status.Stats.PeakRA,
                    PeakDec = status.Stats.PeakDec,
                    AvgDistance = status.AvgDist
                } : null,
                ServerInfo = new {
                    PHD2Version = status?.Version,
                    PHD2Subversion = status?.PHDSubver,
                    AppState = status?.AppState,
                    IsGuiding = status?.IsGuiding ?? false,
                    IsSettling = status?.IsSettling ?? false
                }
            };
            
            return new ApiResponse {
                Success = true,
                Response = allInfo,
                StatusCode = 200,
                Type = "PHD2AllInfo"
            };
        } catch (Exception ex) {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }
}
