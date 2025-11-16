using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;

namespace TouchNStars.Server.Controllers;

/// <summary>
/// API Controller for system control (shutdown/restart)
/// </summary>
public class SystemController : WebApiController
{
    /// <summary>
    /// GET /api/system/shutdown - Shutdown the system
    /// </summary>
    [Route(HttpVerbs.Get, "/system/shutdown")]
    public object SystemShutdown()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-Command \"Stop-Computer -Force\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();

            return new Dictionary<string, object>()
            {
                { "success", true },
                { "message", "System shutdown initiated" }
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new Dictionary<string, object>()
            {
                { "success", false },
                { "error", ex.Message }
            };
        }
    }

    /// <summary>
    /// GET /api/system/restart - Restart the system
    /// </summary>
    [Route(HttpVerbs.Get, "/system/restart")]
    public object SystemRestart()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-Command \"Restart-Computer -Force\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();

            return new Dictionary<string, object>()
            {
                { "success", true },
                { "message", "System restart initiated" }
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new Dictionary<string, object>()
            {
                { "success", false },
                { "error", ex.Message }
            };
        }
    }
}
