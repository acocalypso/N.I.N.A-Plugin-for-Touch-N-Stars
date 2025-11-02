using TouchNStars.Server.Models;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using System;
using System.Linq;
using TouchNStars.Utility;

namespace TouchNStars.Server.Controllers;

/// <summary>
/// API Controller for NINA Dialog management
/// </summary>
public class DialogController : WebApiController
{
    /// <summary>
    /// GET /api/dialogs/list - Get all open NINA dialogs
    /// Query parameter: debug=true to include raw text elements
    /// </summary>
    [Route(HttpVerbs.Get, "/dialogs/list")]
    public ApiResponse GetAllNinaDialogs()
    {
        try
        {
            // Check for debug parameter
            bool debug = false;
            if (HttpContext.Request.QueryString.AllKeys.Contains("debug"))
            {
                bool.TryParse(HttpContext.Request.QueryString["debug"], out debug);
            }

            var dialogs = DialogManager.GetAllDialogs(debug);

            return new ApiResponse
            {
                Success = true,
                Response = new
                {
                    Count = dialogs.Count,
                    Dialogs = dialogs
                },
                StatusCode = 200,
                Type = "DialogList"
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    /// <summary>
    /// GET /api/dialogs/count - Get count of open dialogs
    /// </summary>
    [Route(HttpVerbs.Get, "/dialogs/count")]
    public ApiResponse GetNinaDialogCount()
    {
        try
        {
            int count = DialogManager.GetDialogCount();

            return new ApiResponse
            {
                Success = true,
                Response = new
                {
                    Count = count,
                    HasActive = count > 0
                },
                StatusCode = 200,
                Type = "DialogCount"
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    /// <summary>
    /// POST /api/dialogs/close-all - Close all open dialogs
    /// </summary>
    [Route(HttpVerbs.Post, "/dialogs/close-all")]
    public ApiResponse CloseAllNinaDialogs()
    {
        try
        {
            bool confirmResult = true;

            // Check for 'confirm' query parameter
            if (HttpContext.Request.QueryString.AllKeys.Contains("confirm"))
            {
                bool.TryParse(HttpContext.Request.QueryString["confirm"], out confirmResult);
            }

            int count = DialogManager.CloseAllDialogs(confirmResult);

            Logger.Info($"Closed {count} NINA dialog(s) via API - Confirm: {confirmResult}");

            return new ApiResponse
            {
                Success = true,
                Response = new
                {
                    Message = $"Closed {count} dialog(s)",
                    Count = count,
                    ConfirmResult = confirmResult
                },
                StatusCode = 200,
                Type = "DialogsClosed"
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    /// <summary>
    /// POST /api/dialogs/close-by-type - Close dialogs by ContentType
    /// </summary>
    [Route(HttpVerbs.Post, "/dialogs/close-by-type")]
    public ApiResponse CloseNinaDialogsByType()
    {
        try
        {
            string typeName = HttpContext.Request.QueryString["type"];
            bool confirmResult = true;

            if (string.IsNullOrEmpty(typeName))
            {
                HttpContext.Response.StatusCode = 400;
                return new ApiResponse
                {
                    Success = false,
                    Error = "Missing 'type' query parameter",
                    StatusCode = 400,
                    Type = "BadRequest"
                };
            }

            // Check for 'confirm' query parameter
            if (HttpContext.Request.QueryString.AllKeys.Contains("confirm"))
            {
                bool.TryParse(HttpContext.Request.QueryString["confirm"], out confirmResult);
            }

            int count = DialogManager.CloseDialogsByType(typeName, confirmResult);

            Logger.Info($"Closed {count} NINA dialog(s) of type '{typeName}' via API - Confirm: {confirmResult}");

            return new ApiResponse
            {
                Success = true,
                Response = new
                {
                    Message = $"Closed {count} dialog(s) of type '{typeName}'",
                    Count = count,
                    Type = typeName,
                    ConfirmResult = confirmResult
                },
                StatusCode = 200,
                Type = "DialogsClosed"
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    /// <summary>
    /// POST /api/dialogs/click-button - Unified endpoint for clicking buttons in dialogs
    /// </summary>
    [Route(HttpVerbs.Post, "/dialogs/click-button")]
    public ApiResponse ClickButton()
    {
        try
        {
            string window = HttpContext.Request.QueryString["window"];
            string button = HttpContext.Request.QueryString["button"];

            if (string.IsNullOrEmpty(button))
            {
                HttpContext.Response.StatusCode = 400;
                return new ApiResponse
                {
                    Success = false,
                    Error = "Missing 'button' query parameter (button name, text, or standard button: Yes/No/OK/Cancel)",
                    StatusCode = 400,
                    Type = "BadRequest"
                };
            }

            // If window is not specified, try to click button by text in any dialog
            if (string.IsNullOrEmpty(window))
            {
                int count = DialogManager.ClickButtonByTextInAnyDialog(button);

                if (count > 0)
                {
                    Logger.Info($"Clicked '{button}' button on {count} dialog(s) via API");
                    return new ApiResponse
                    {
                        Success = true,
                        Response = new
                        {
                            Message = $"Clicked '{button}' button on {count} dialog(s)",
                            Count = count,
                            Button = button
                        },
                        StatusCode = 200,
                        Type = "ButtonClicked"
                    };
                }
                else
                {
                    HttpContext.Response.StatusCode = 404;
                    return new ApiResponse
                    {
                        Success = false,
                        Error = $"No button '{button}' found in any dialog",
                        StatusCode = 404,
                        Type = "NotFound"
                    };
                }
            }

            // Window specified - click button in specific window
            bool clicked = DialogManager.ClickWindowButton(window, button);

            if (clicked)
            {
                Logger.Info($"Clicked button '{button}' in window '{window}' via API");
                return new ApiResponse
                {
                    Success = true,
                    Response = new
                    {
                        Message = $"Successfully clicked button '{button}' in window '{window}'",
                        Window = window,
                        Button = button
                    },
                    StatusCode = 200,
                    Type = "ButtonClicked"
                };
            }
            else
            {
                HttpContext.Response.StatusCode = 404;
                return new ApiResponse
                {
                    Success = false,
                    Error = $"Button '{button}' not found in window '{window}'",
                    StatusCode = 404,
                    Type = "NotFound"
                };
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }

    /// <summary>
    /// GET /api/dialogs/debug - Get detailed debug information about all windows
    /// </summary>
    [Route(HttpVerbs.Get, "/dialogs/debug")]
    public ApiResponse GetDetailedDialogDebugInfo()
    {
        try
        {
            var detailedInfo = DialogManager.GetDetailedWindowInfo();

            return new ApiResponse
            {
                Success = true,
                Response = new
                {
                    Count = detailedInfo.Count,
                    Windows = detailedInfo
                },
                StatusCode = 200,
                Type = "DebugInfo"
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            HttpContext.Response.StatusCode = 500;
            return new ApiResponse
            {
                Success = false,
                Error = ex.Message,
                StatusCode = 500,
                Type = "Error"
            };
        }
    }
}
