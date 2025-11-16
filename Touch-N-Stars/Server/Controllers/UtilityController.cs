using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TouchNStars.Utility;

namespace TouchNStars.Server.Controllers;

public class UtilityController : WebApiController
{
    private static readonly List<string> excluded_members = new List<string>() { "GetEquipment", "RequestAll", "LoadPlugin" };

    [Route(HttpVerbs.Get, "/logs")]
    public List<Hashtable> GetRecentLogs([QueryField(true)] int count, [QueryField] string level)
    {
        List<Hashtable> logs = new List<Hashtable>();

        if (string.IsNullOrEmpty(level))
        {
            level = string.Empty;
        }

        if (level.Equals("ERROR") || level.Equals("WARNING") || level.Equals("INFO") || level.Equals("DEBUG") || string.IsNullOrEmpty(level))
        {
            string currentLogFile = Directory.GetFiles(CoreUtility.LogPath).OrderByDescending(File.GetCreationTime).First();

            string[] logLines = [];

            using (var stream = new FileStream(currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    string content = reader.ReadToEnd();
                    logLines = content.Split('\n');
                }
            }

            List<string> filteredLogLines = new List<string>();
            foreach (string line in logLines)
            {
                bool valid = true;

                if (!line.Contains('|' + level + '|') && !string.IsNullOrEmpty(level))
                {
                    valid = false;
                }
                if (line.Contains("DATE|LEVEL|SOURCE|MEMBER|LINE|MESSAGE"))
                {
                    valid = false;
                }
                foreach (string excluded_member in excluded_members)
                {
                    if (line.Contains(excluded_member))
                    {
                        valid = false;
                    }
                }
                if (valid)
                {
                    filteredLogLines.Add(line);
                }
            }
            IEnumerable<string> lines = filteredLogLines.TakeLast(count);
            foreach (string line in lines)
            {
                string[] parts = line.Split('|');
                if (parts.Length >= 6)
                {
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

    [Route(HttpVerbs.Get, "/get-api-port")]
    public async Task<int> GetApiPort()
    {
        return await TouchNStars.Communicator.GetPort(true);
    }

    [Route(HttpVerbs.Get, "/version")]
    public object GetAssemblyVersion()
    {
        try
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString();

            return new Dictionary<string, object>
            {
                { "success", true },
                { "version", version }
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new Dictionary<string, object>
            {
                { "success", false },
                { "error", ex.Message }
            };
        }
    }
}
