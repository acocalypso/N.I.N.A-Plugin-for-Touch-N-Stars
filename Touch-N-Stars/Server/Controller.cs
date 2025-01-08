using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TouchNStars.Helper;

namespace TouchNStars.Server;

public class GuiderData {
    public List<double> RADistanceRaw { get; set; }
    public List<double> DECDistanceRaw { get; set; }
}

public class NGCSearchResult {
    public string Name { get; set; }
    public double RA { get; set; }
    public double Dec { get; set; }
}

public class Controller : WebApiController {

    private object lockObj = new object();

    private object searchResultsCache = null; // unsure which type this will be
    private GuiderData guiderData = new GuiderData();
    private bool afRun = false;
    private bool afError = false;
    private string afErrorText = string.Empty;
    private bool newAfGraph = false;
    private DateTime lastAfTimestamp = DateTime.MinValue;
    private bool wshvActive = false;
    private int wshvPort = 80;


    [Route(HttpVerbs.Get, "/api/logs")]
    public string GetRecentLogs([QueryField] int count, [QueryField] string level) {
        return "Hello World";
    }

    [Route(HttpVerbs.Get, "/api/wshv")]
    public object GetWshvData() {
        lock (lockObj) {
            return new Dictionary<string, object>() { { "wshvActive", wshvActive }, { "wshvPort", wshvPort } };
        }
    }

    [Route(HttpVerbs.Get, "/api/autofocus")]
    public string ControlAutofocus() {
        return "Hello World Autofocus";
    }

    [Route(HttpVerbs.Get, "/api/guider-data")]
    public object GetGuiderData() {
        lock (lockObj) {
            return guiderData;
        }
    }

    [Route(HttpVerbs.Get, "/api/ngc/search")]
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

    [Route(HttpVerbs.Get, "/api/ngc/cache")] // can we omit the following two methods?
    public string GetCachedNGCResults() {
        return "Hello World NGC Cache";
    }

    [Route(HttpVerbs.Post, "/api/ngc/cache")]
    public string UpdateCachedNGCResults() {
        // HttpContext.GetRequestBodyAsStringAsync()
        return "Hello World NGC Cache";
    }

    [Route(HttpVerbs.Get, "/api/targetpic")]
    public string FetchTargetPicture([QueryField(true)] int width, [QueryField(true)] int height, [QueryField(true)] float fov, [QueryField(true)] string ra, [QueryField(true)] string dec) {
        return "Hello World Target Picture";
    }
}