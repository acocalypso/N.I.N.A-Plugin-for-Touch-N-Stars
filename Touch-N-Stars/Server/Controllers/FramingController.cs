using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using NINA.Core.Utility;
using System;
using System.Windows.Input;
using TouchNStars.Server.Infrastructure;
using TouchNStars.Server.Models;
using TouchNStars.Utility;

namespace TouchNStars.Server.Controllers;

public class FramingController : WebApiController
{
    [Route(HttpVerbs.Get, "/framing/status")]
    public ApiResponse GetFramingStatus()
    {
        try
        {
            var framingAssistantVM = TouchNStars.Mediators.FramingAssistantVM;

            if (framingAssistantVM == null)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = "FramingAssistantVM is not available",
                    StatusCode = 503,
                    Type = "Error"
                };
            }

            // Check if SlewToCoordinatesCommand can be executed (implies slew is available/not running)
            bool canCancel = framingAssistantVM.SlewToCoordinatesCommand?.CanExecute(null) == false;

            return new ApiResponse
            {
                Success = true,
                Response = new
                {
                    isSlewRunning = canCancel,
                    canCancel = canCancel
                },
                StatusCode = 200,
                Type = "FramingStatus"
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

    [Route(HttpVerbs.Post, "/framing/cancel")]
    public ApiResponse CancelSlewAndCenter()
    {
        try
        {
            var framingAssistantVM = TouchNStars.Mediators.FramingAssistantVM as dynamic;

            if (framingAssistantVM == null)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = "FramingAssistantVM is not available",
                    StatusCode = 503,
                    Type = "Error"
                };
            }

            // Try to access CancelSlewToCoordinatesCommand via reflection since it's not in the interface
            var cancelCommandProperty = framingAssistantVM.GetType().GetProperty("CancelSlewToCoordinatesCommand");

            if (cancelCommandProperty == null)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = "CancelSlewToCoordinatesCommand is not available",
                    StatusCode = 503,
                    Type = "Error"
                };
            }

            var cancelCommand = cancelCommandProperty.GetValue(framingAssistantVM) as ICommand;

            if (cancelCommand == null)
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = "CancelSlewToCoordinatesCommand could not be resolved",
                    StatusCode = 503,
                    Type = "Error"
                };
            }

            if (!cancelCommand.CanExecute(null))
            {
                return new ApiResponse
                {
                    Success = false,
                    Error = "Cancel command cannot be executed at this time",
                    StatusCode = 409,
                    Type = "Error"
                };
            }

            cancelCommand.Execute(null);

            return new ApiResponse
            {
                Success = true,
                Response = new
                {
                    message = "Slew and Center cancelled successfully"
                },
                StatusCode = 200,
                Type = "FramingCancel"
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
