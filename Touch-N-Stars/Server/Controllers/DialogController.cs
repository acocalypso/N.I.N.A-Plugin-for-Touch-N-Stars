using TouchNStars.Server.Models;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            // Extrahiere Meridian Flip Steps BEVOR wir den DataContext wegwerfen
            var meridianFlipSteps = ExtractMeridianFlipSteps(dialogs);

            // Extrahiere Slew and Center Info BEVOR wir den DataContext wegwerfen
            var slewAndCenterInfo = ExtractSlewAndCenterInfo(dialogs);

            // Entferne DataContext aus allen Dialogen bevor sie serialisiert werden
            foreach (var dialog in dialogs)
            {
                dialog.DataContext = null;
            }

            return new ApiResponse
            {
                Success = true,
                Response = new
                {
                    Count = dialogs.Count,
                    Dialogs = dialogs,
                    MeridianFlip = meridianFlipSteps,
                    SlewAndCenter = slewAndCenterInfo
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

    /// <summary>
    /// Extrahiert Meridian Flip Steps aus den Dialogen mittels Reflection
    /// </summary>
    private object ExtractMeridianFlipSteps(List<DialogManager.DialogInfo> dialogs)
    {
        try
        {
            // Suche nach MeridianFlipVM
            var meridianFlipDialog = dialogs.FirstOrDefault(d =>
                d.ContentType != null && d.ContentType.Contains("MeridianFlipVM"));

            if (meridianFlipDialog == null)
            {
                return new
                {
                    Active = false,
                    Steps = new List<MeridianFlipStep>()
                };
            }

            // MeridianFlipVM ist das Window.Content, nicht DataContext!
            // Wir müssen via Reflection auf das window.Content object zugreifen
            // Das speichern wir als DataContext (Name ist verwirrend, aber das ist wo wir es speichern)
            var viewModel = meridianFlipDialog.DataContext;

            // Fallback: Versuche Window.Content zu bekommen via Reflection
            if (viewModel == null)
            {
                Logger.Debug("DialogController: DataContext is null, trying to access Window properties via Reflection");
                // Wir können nicht auf Window zugreifen, daher müssen wir das Content-Objekt direkt serialisieren
                return new
                {
                    Active = false,
                    Steps = new List<MeridianFlipStep>()
                };
            }

            var stepsProperty = viewModel.GetType().GetProperty("Steps",
                BindingFlags.Public | BindingFlags.Instance);

            if (stepsProperty == null)
            {
                return new
                {
                    Active = false,
                    Steps = new List<MeridianFlipStep>()
                };
            }

            var stepsCollection = stepsProperty.GetValue(viewModel) as System.Collections.IEnumerable;
            var steps = new List<MeridianFlipStep>();

            if (stepsCollection != null)
            {
                foreach (var step in stepsCollection)
                {
                    if (step == null) continue;

                    var idProp = step.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
                    var titleProp = step.GetType().GetProperty("Title", BindingFlags.Public | BindingFlags.Instance);
                    var finishedProp = step.GetType().GetProperty("Finished", BindingFlags.Public | BindingFlags.Instance);

                    if (idProp != null && titleProp != null && finishedProp != null)
                    {
                        steps.Add(new MeridianFlipStep
                        {
                            Id = idProp.GetValue(step)?.ToString() ?? "",
                            Title = titleProp.GetValue(step)?.ToString() ?? "",
                            Finished = (bool)(finishedProp.GetValue(step) ?? false)
                        });
                    }
                }
            }

            return new
            {
                Active = true,
                StepCount = steps.Count,
                Steps = steps
            };
        }
        catch (Exception ex)
        {
            Logger.Debug($"DialogController: Error extracting Meridian Flip steps: {ex.Message}");
            return new
            {
                Active = false,
                Steps = new List<MeridianFlipStep>(),
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Extrahiert Slew and Center Informationen aus den Dialogen
    /// </summary>
    private SlewAndCenterInfo ExtractSlewAndCenterInfo(List<DialogManager.DialogInfo> dialogs)
    {
        try
        {
            // Suche nach PlateSolvingStatusVM (wird für Slew and Center verwendet)
            var slewDialog = dialogs.FirstOrDefault(d =>
                d.ContentType != null && d.ContentType.Contains("PlateSolvingStatusVM") &&
                d.Title != null && d.Title.Contains("Slew and center"));

            if (slewDialog == null)
            {
                return new SlewAndCenterInfo
                {
                    Active = false,
                    MeasurementCount = 0,
                    CurrentMeasurement = new SlewAndCenterMeasurement()
                };
            }

            var viewModel = slewDialog.DataContext;
            if (viewModel == null)
            {
                return new SlewAndCenterInfo
                {
                    Active = false,
                    MeasurementCount = 0,
                    CurrentMeasurement = new SlewAndCenterMeasurement()
                };
            }

            // Extrahiere Status
            var statusProp = viewModel.GetType().GetProperty("Status", BindingFlags.Public | BindingFlags.Instance);
            var status = statusProp?.GetValue(viewModel) as dynamic;
            string statusMessage = status?.Status?.ToString() ?? "";

            // Extrahiere Messdaten aus PlateSolveHistory
            int measurementCount = 0;
            string latestTime = "--";
            bool latestSuccess = false;
            string latestErrorDistance = "--";
            string rotation = "--";
            var allMeasurements = new List<SlewAndCenterMeasurement>();

            var historyProp = viewModel.GetType().GetProperty("PlateSolveHistory", BindingFlags.Public | BindingFlags.Instance);

            if (historyProp != null)
            {
                var historyData = historyProp.GetValue(viewModel) as System.Collections.IEnumerable;
                if (historyData != null)
                {
                    var historyList = new List<object>();
                    foreach (var item in historyData)
                    {
                        historyList.Add(item);
                    }

                    measurementCount = historyList.Count;

                    if (historyList.Count > 0)
                    {
                        // Iteriere durch alle Items und sammle Messungen
                        for (int i = 0; i < historyList.Count; i++)
                        {
                            var item = historyList[i];
                            var itemType = item.GetType();

                            // PlateSolveResult Properties - from NINA
                            var timeProp = itemType.GetProperty("SolveTime", BindingFlags.Public | BindingFlags.Instance) ??
                                          itemType.GetProperty("Timestamp", BindingFlags.Public | BindingFlags.Instance) ??
                                          itemType.GetProperty("Time", BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
                            var successProp = itemType.GetProperty("Success", BindingFlags.Public | BindingFlags.Instance);
                            // Use RaPixError as the primary error metric (RA = Right Ascension error in pixels)
                            var errorProp = itemType.GetProperty("RaPixError", BindingFlags.Public | BindingFlags.Instance) ??
                                           itemType.GetProperty("DecPixError", BindingFlags.Public | BindingFlags.Instance) ??
                                           itemType.GetProperty("Error", BindingFlags.Public | BindingFlags.Instance) ??
                                           itemType.GetProperty("ErrorDistance", BindingFlags.Public | BindingFlags.Instance);

                            var time = "--";
                            var success = false;
                            var errorDistance = "--";

                            if (timeProp != null)
                            {
                                var timeVal = timeProp.GetValue(item);
                                if (timeVal is DateTime dt)
                                {
                                    time = dt.ToString("HH:mm:ss");
                                }
                                else
                                {
                                    time = timeVal?.ToString() ?? "--";
                                }
                            }

                            if (successProp != null)
                            {
                                var successVal = successProp.GetValue(item);
                                success = successVal != null && (bool)successVal;
                            }

                            // First try to get RaErrorString (already formatted as "00' 00\" 16")
                            var raErrorStringProp = itemType.GetProperty("RaErrorString", BindingFlags.Public | BindingFlags.Instance);
                            if (raErrorStringProp != null)
                            {
                                var raErrorStr = raErrorStringProp.GetValue(item)?.ToString();
                                if (!string.IsNullOrEmpty(raErrorStr) && raErrorStr != "--")
                                {
                                    errorDistance = raErrorStr;
                                }
                            }

                            // If RaErrorString is not available, try DecErrorString
                            if (errorDistance == "--")
                            {
                                var decErrorStringProp = itemType.GetProperty("DecErrorString", BindingFlags.Public | BindingFlags.Instance);
                                if (decErrorStringProp != null)
                                {
                                    var decErrorStr = decErrorStringProp.GetValue(item)?.ToString();
                                    if (!string.IsNullOrEmpty(decErrorStr) && decErrorStr != "--")
                                    {
                                        errorDistance = decErrorStr;
                                    }
                                }
                            }

                            // If still no error string, try numeric RaPixError/DecPixError
                            if (errorDistance == "--" && errorProp != null)
                            {
                                var errorVal = errorProp.GetValue(item);
                                if (errorVal is double dVal)
                                {
                                    // Format as pixel error with 2 decimal places, or show NaN as "--"
                                    errorDistance = double.IsNaN(dVal) ? "--" : dVal.ToString("F2");
                                }
                                else
                                {
                                    errorDistance = errorVal?.ToString() ?? "--";
                                }
                            }

                            // Extract rotation for this measurement
                            string measurementRotation = "--";
                            var positionAngleProp = itemType.GetProperty("PositionAngle", BindingFlags.Public | BindingFlags.Instance) ??
                                                   itemType.GetProperty("Orientation", BindingFlags.Public | BindingFlags.Instance);
                            if (positionAngleProp != null)
                            {
                                var angleVal = positionAngleProp.GetValue(item);
                                if (angleVal is double dVal && !double.IsNaN(dVal))
                                {
                                    measurementRotation = dVal.ToString("F2");
                                }
                            }

                            allMeasurements.Add(new SlewAndCenterMeasurement
                            {
                                Time = time,
                                Success = success,
                                ErrorDistance = errorDistance,
                                Rotation = measurementRotation
                            });

                            // Store latest (last) measurement - overwrite with each iteration
                            latestTime = time;
                            latestSuccess = success;
                            latestErrorDistance = errorDistance;
                            rotation = measurementRotation;
                        }
                    }
                }
            }

            return new SlewAndCenterInfo
            {
                Active = true,
                Status = statusMessage,
                Rotation = rotation,
                MeasurementCount = measurementCount,
                CurrentMeasurement = new SlewAndCenterMeasurement
                {
                    Time = latestTime,
                    Success = latestSuccess,
                    ErrorDistance = latestErrorDistance,
                    Rotation = rotation
                },
                Measurements = allMeasurements
            };
        }
        catch (Exception ex)
        {
            Logger.Debug($"DialogController: Error extracting Slew and Center info: {ex.Message}");
            return new SlewAndCenterInfo
            {
                Active = false,
                MeasurementCount = 0,
                CurrentMeasurement = new SlewAndCenterMeasurement(),
                Status = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// GET /api/dialogs/debug-slew-properties - Debug endpoint to list all properties of PlateSolvingStatusVM
    /// </summary>
    [Route(HttpVerbs.Get, "/dialogs/debug-slew-properties")]
    public ApiResponse DebugSlewProperties()
    {
        try
        {
            var dialogs = DialogManager.GetAllDialogs(false);
            var slewDialog = dialogs.FirstOrDefault(d =>
                d.ContentType != null && d.ContentType.Contains("PlateSolvingStatusVM") &&
                d.Title != null && d.Title.Contains("Slew and center"));

            if (slewDialog == null)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = "Slew and Center dialog not found",
                    StatusCode = 404,
                    Type = "NotFound"
                };
            }

            var viewModel = slewDialog.DataContext;
            if (viewModel == null)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = "ViewModel is null",
                    StatusCode = 500,
                    Type = "Error"
                };
            }

            var properties = viewModel.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new
                {
                    Name = p.Name,
                    Type = p.PropertyType.Name,
                    IsGeneric = p.PropertyType.IsGenericType,
                    GenericArgs = p.PropertyType.IsGenericType ?
                        string.Join(", ", p.PropertyType.GetGenericArguments().Select(t => t.Name)) : ""
                })
                .OrderBy(p => p.Name)
                .ToList();

            return new ApiResponse
            {
                Success = true,
                Response = new
                {
                    ViewModelType = viewModel.GetType().Name,
                    PropertyCount = properties.Count,
                    Properties = properties
                },
                StatusCode = 200,
                Type = "DebugInfo"
            };
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
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
