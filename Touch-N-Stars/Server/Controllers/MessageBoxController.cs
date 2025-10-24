using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using TouchNStars.SequenceItems;
using TouchNStars.Utility;

namespace TouchNStars.Server.Controllers;

/// <summary>
/// API Controller for TNS MessageBox management
/// </summary>
public class MessageBoxController : WebApiController
{
    /// <summary>
    /// GET /api/messagebox/list - Get all active message boxes
    /// </summary>
    [Route(HttpVerbs.Get, "/messagebox/list")]
    public ApiResponse GetActiveMessageBoxes()
    {
        try
        {
            var activeBoxes = MessageBoxRegistry.GetAll();
            var result = activeBoxes.Select(box => new
            {
                Id = box.Id,
                Text = box.Text,
                CreatedAt = box.CreatedAt,
                Age = DateTime.Now - box.CreatedAt
            }).ToList();

            return new ApiResponse
            {
                Success = true,
                Response = new
                {
                    Count = result.Count,
                    MessageBoxes = result
                },
                StatusCode = 200,
                Type = "MessageBoxList"
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
    /// POST /api/messagebox/close/{id} - Close a specific message box
    /// </summary>
    [Route(HttpVerbs.Post, "/messagebox/close/{id}")]
    public ApiResponse CloseMessageBox(Guid id)
    {
        try
        {
            bool continueSequence = true;

            // Check if request has body with continue parameter
            try
            {
                var requestData = HttpContext.GetRequestDataAsync<Dictionary<string, object>>().Result;
                if (requestData != null && requestData.ContainsKey("continue"))
                {
                    if (bool.TryParse(requestData["continue"].ToString(), out bool shouldContinue))
                    {
                        continueSequence = shouldContinue;
                    }
                }
            }
            catch
            {
                // Use default if parsing fails
            }

            bool closed = MessageBoxRegistry.Close(id, continueSequence);

            if (closed)
            {
                Logger.Info($"MessageBox {id} closed via API - Continue: {continueSequence}");
                return new ApiResponse
                {
                    Success = true,
                    Response = new
                    {
                        Message = $"MessageBox {id} closed",
                        ContinueSequence = continueSequence
                    },
                    StatusCode = 200,
                    Type = "MessageBoxClosed"
                };
            }
            else
            {
                HttpContext.Response.StatusCode = 404;
                return new ApiResponse
                {
                    Success = false,
                    Error = $"MessageBox with ID {id} not found",
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
    /// POST /api/messagebox/close-all - Close all active message boxes
    /// </summary>
    [Route(HttpVerbs.Post, "/messagebox/close-all")]
    public ApiResponse CloseAllMessageBoxes()
    {
        try
        {
            bool continueSequence = true;

            // Check if request has body with continue parameter
            try
            {
                var requestData = HttpContext.GetRequestDataAsync<Dictionary<string, object>>().Result;
                if (requestData != null && requestData.ContainsKey("continue"))
                {
                    if (bool.TryParse(requestData["continue"].ToString(), out bool shouldContinue))
                    {
                        continueSequence = shouldContinue;
                    }
                }
            }
            catch
            {
                // Use default if parsing fails
            }

            int count = MessageBoxRegistry.CloseAll(continueSequence);

            Logger.Info($"Closed {count} MessageBoxes via API - Continue: {continueSequence}");

            return new ApiResponse
            {
                Success = true,
                Response = new
                {
                    Message = $"Closed {count} message box(es)",
                    Count = count,
                    ContinueSequence = continueSequence
                },
                StatusCode = 200,
                Type = "MessageBoxesClosed"
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
    /// GET /api/messagebox/info/{id} - Get information about a specific message box
    /// </summary>
    [Route(HttpVerbs.Get, "/messagebox/info/{id}")]
    public ApiResponse GetMessageBoxInfo(Guid id)
    {
        try
        {
            var box = MessageBoxRegistry.Get(id);

            if (box != null)
            {
                return new ApiResponse
                {
                    Success = true,
                    Response = new
                    {
                        Id = box.Id,
                        Text = box.Text,
                        CreatedAt = box.CreatedAt,
                        Age = DateTime.Now - box.CreatedAt,
                        IsActive = MessageBoxRegistry.IsActive(id)
                    },
                    StatusCode = 200,
                    Type = "MessageBoxInfo"
                };
            }
            else
            {
                HttpContext.Response.StatusCode = 404;
                return new ApiResponse
                {
                    Success = false,
                    Error = $"MessageBox with ID {id} not found",
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
    /// GET /api/messagebox/count - Get count of active message boxes
    /// </summary>
    [Route(HttpVerbs.Get, "/messagebox/count")]
    public ApiResponse GetMessageBoxCount()
    {
        try
        {
            int count = MessageBoxRegistry.Count;

            return new ApiResponse
            {
                Success = true,
                Response = new
                {
                    Count = count,
                    HasActive = count > 0
                },
                StatusCode = 200,
                Type = "MessageBoxCount"
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
