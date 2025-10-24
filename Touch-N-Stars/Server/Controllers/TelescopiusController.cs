using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace TouchNStars.Server.Controllers;

/// <summary>
/// API Controller for Telescopius PIAAPI proxy
/// </summary>
public class TelescopiusController : WebApiController
{
    /// <summary>
    /// GET /api/proxy/telescopius - Proxy GET requests to Telescopius PIAAPI
    /// </summary>
    [Route(HttpVerbs.Get, "/proxy/telescopius")]
    public async Task ProxyTelescopiusGet()
    {
        await ProxyTelescopiusRequest("GET");
    }

    /// <summary>
    /// POST /api/proxy/telescopius - Proxy POST requests to Telescopius PIAAPI
    /// </summary>
    [Route(HttpVerbs.Post, "/proxy/telescopius")]
    public async Task ProxyTelescopiusPost()
    {
        await ProxyTelescopiusRequest("POST");
    }

    /// <summary>
    /// PUT /api/proxy/telescopius - Proxy PUT requests to Telescopius PIAAPI
    /// </summary>
    [Route(HttpVerbs.Put, "/proxy/telescopius")]
    public async Task ProxyTelescopiusPut()
    {
        await ProxyTelescopiusRequest("PUT");
    }

    /// <summary>
    /// DELETE /api/proxy/telescopius - Proxy DELETE requests to Telescopius PIAAPI
    /// </summary>
    [Route(HttpVerbs.Delete, "/proxy/telescopius")]
    public async Task ProxyTelescopiusDelete()
    {
        await ProxyTelescopiusRequest("DELETE");
    }

    /// <summary>
    /// OPTIONS /api/proxy/telescopius - Handle CORS preflight requests
    /// </summary>
    [Route(HttpVerbs.Options, "/proxy/telescopius")]
    public Task ProxyTelescopiusOptions()
    {
        // Handle CORS preflight requests - headers are already set by CustomHeaderModule
        HttpContext.Response.StatusCode = 200;
        HttpContext.Response.Headers.Add("Access-Control-Max-Age", "86400"); // 24 hours

        // Empty response body for preflight
        Response.OutputStream.Write(new byte[0], 0, 0);

        return Task.CompletedTask;
    }

    private async Task ProxyTelescopiusRequest(string httpMethod)
    {
        try
        {
            // Get the target URL from query parameters
            string targetUrl = HttpContext.Request.QueryString.Get("url");
            if (string.IsNullOrEmpty(targetUrl))
            {
                HttpContext.Response.StatusCode = 400;
                HttpContext.Response.ContentType = "application/json";
                var errorResponse = System.Text.Json.JsonSerializer.Serialize(new ApiResponse
                {
                    Success = false,
                    Error = "Missing 'url' query parameter",
                    StatusCode = 400,
                    Type = "MissingParameter"
                });
                var errorBytes = System.Text.Encoding.UTF8.GetBytes(errorResponse);
                Response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
                return;
            }

            // Validate that the URL is for Telescopius PIAAPI
            if (!targetUrl.Contains("telescopius.com") && !targetUrl.Contains("piaapi"))
            {
                HttpContext.Response.StatusCode = 403;
                HttpContext.Response.ContentType = "application/json";
                var errorResponse = System.Text.Json.JsonSerializer.Serialize(new ApiResponse
                {
                    Success = false,
                    Error = "Proxy only allows Telescopius PIAAPI requests",
                    StatusCode = 403,
                    Type = "ProxyForbidden"
                });
                var errorBytes = System.Text.Encoding.UTF8.GetBytes(errorResponse);
                Response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
                return;
            }

            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage request = new HttpRequestMessage();
                request.Method = new HttpMethod(httpMethod);
                request.RequestUri = new Uri(targetUrl);

                // Add essential headers
                string authHeader = HttpContext.Request.Headers["Authorization"];
                if (!string.IsNullOrEmpty(authHeader))
                {
                    request.Headers.Add("Authorization", authHeader);
                }
                request.Headers.Add("Accept", "application/json, */*");

                // Handle request body for POST/PUT requests
                if (httpMethod == "POST" || httpMethod == "PUT")
                {
                    if (HttpContext.Request.HasEntityBody)
                    {
                        using (var reader = new StreamReader(HttpContext.Request.InputStream))
                        {
                            string body = await reader.ReadToEndAsync();
                            if (!string.IsNullOrEmpty(body))
                            {
                                request.Content = new StringContent(body);

                                if (!string.IsNullOrEmpty(HttpContext.Request.ContentType))
                                {
                                    request.Content.Headers.Remove("Content-Type");
                                    request.Content.Headers.Add("Content-Type", HttpContext.Request.ContentType);
                                }
                            }
                        }
                    }
                }

                // Send the request
                HttpResponseMessage response = await client.SendAsync(request);

                // Copy response status
                HttpContext.Response.StatusCode = (int)response.StatusCode;

                // Copy response headers
                foreach (var header in response.Headers)
                {
                    try
                    {
                        HttpContext.Response.Headers.Add(header.Key, string.Join(",", header.Value));
                    }
                    catch
                    {
                        // Ignore problematic headers
                    }
                }

                // Copy content headers
                if (response.Content != null)
                {
                    foreach (var header in response.Content.Headers)
                    {
                        try
                        {
                            if (header.Key.ToLower() == "content-type")
                            {
                                HttpContext.Response.ContentType = string.Join(",", header.Value);
                            }
                            else
                            {
                                HttpContext.Response.Headers.Add(header.Key, string.Join(",", header.Value));
                            }
                        }
                        catch
                        {
                            // Ignore problematic headers
                        }
                    }

                    // Copy response body
                    byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
                    Response.OutputStream.Write(responseBody, 0, responseBody.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Telescopius proxy error: {ex}");
            HttpContext.Response.StatusCode = 500;
            HttpContext.Response.ContentType = "application/json";

            var errorResponse = System.Text.Json.JsonSerializer.Serialize(new ApiResponse
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "ProxyError"
            });
            var errorBytes = System.Text.Encoding.UTF8.GetBytes(errorResponse);
            Response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
        }
    }
}
