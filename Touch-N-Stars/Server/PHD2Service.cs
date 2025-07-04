using System;
using System.Linq;
using System.Threading.Tasks;
using NINA.Core.Utility;
using TouchNStars.PHD2;

namespace TouchNStars.Server
{
    public class PHD2Service : IDisposable
    {
        private PHD2Client client;
        private readonly object lockObject = new object();
        private string lastError;

        public bool IsConnected => client?.IsConnected ?? false;
        public string LastError => lastError;

        public PHD2Service()
        {
            client = new PHD2Client();
        }

        public async Task<bool> ConnectAsync(string hostname = "localhost", uint instance = 1)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        Logger.Info($"Attempting to connect to PHD2 at {hostname}, instance {instance}");
                        
                        if (client != null && client.IsConnected)
                        {
                            Logger.Info("Disconnecting existing PHD2 client");
                            client.Disconnect();
                        }

                        ushort port = (ushort)(4400 + instance - 1);
                        Logger.Info($"Creating PHD2 client for {hostname}:{port}");
                        
                        client = new PHD2Client(hostname, instance);
                        Logger.Info("Calling client.Connect()");
                        client.Connect();
                        
                        Logger.Info($"PHD2 connection successful. IsConnected: {client.IsConnected}");
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = $"Failed to connect to PHD2 at {hostname}:{4400 + instance - 1}: {ex.Message}";
                    Logger.Error($"Failed to connect to PHD2: {ex}");
                    Logger.Error($"Stack trace: {ex.StackTrace}");
                    return false;
                }
            });
        }

        public async Task DisconnectAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        client?.Disconnect();
                        lastError = null;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Error disconnecting from PHD2: {ex}");
                }
            });
        }

        public async Task<bool> StartGuidingAsync(double settlePixels = 2.0, double settleTime = 10.0, double settleTimeout = 100.0)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.Guide(settlePixels, settleTime, settleTimeout);
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to start guiding: {ex}");
                    return false;
                }
            });
        }

        public async Task<bool> StopGuidingAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.StopCapture();
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to stop guiding: {ex}");
                    return false;
                }
            });
        }

        public async Task<bool> DitherAsync(double ditherPixels = 3.0, double settlePixels = 2.0, double settleTime = 10.0, double settleTimeout = 100.0)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.Dither(ditherPixels, settlePixels, settleTime, settleTimeout);
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to dither: {ex}");
                    return false;
                }
            });
        }

        public async Task<bool> PauseGuidingAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.Pause();
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to pause guiding: {ex}");
                    return false;
                }
            });
        }

        public async Task<bool> UnpauseGuidingAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.Unpause();
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to unpause guiding: {ex}");
                    return false;
                }
            });
        }

        public async Task<bool> StartLoopingAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.Loop();
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to start looping: {ex}");
                    return false;
                }
            });
        }

        public async Task<System.Collections.Generic.List<string>> GetEquipmentProfilesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return new System.Collections.Generic.List<string>();
                        }

                        var profiles = client.GetEquipmentProfiles();
                        lastError = null;
                        return profiles;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get equipment profiles: {ex}");
                    return new System.Collections.Generic.List<string>();
                }
            });
        }

        public async Task<bool> ConnectEquipmentAsync(string profileName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.ConnectEquipment(profileName);
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to connect equipment: {ex}");
                    return false;
                }
            });
        }

        public async Task<bool> DisconnectEquipmentAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return false;
                        }

                        client.DisconnectEquipment();
                        lastError = null;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to disconnect equipment: {ex}");
                    return false;
                }
            });
        }

        public async Task<PHD2Status> GetStatusAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            return new PHD2Status
                            {
                                IsConnected = false,
                                AppState = "Disconnected"
                            };
                        }

                        var status = client.GetStatus();
                        lastError = null;
                        return status;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get PHD2 status: {ex}");
                    return new PHD2Status
                    {
                        IsConnected = false,
                        AppState = "Error"
                    };
                }
            });
        }

        public async Task<SettleProgress> CheckSettlingAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            lastError = "PHD2 is not connected";
                            return null;
                        }

                        if (!client.IsSettling())
                        {
                            return new SettleProgress { Done = true };
                        }

                        var progress = client.CheckSettling();
                        lastError = null;
                        return progress;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to check settling: {ex}");
                    return null;
                }
            });
        }

        public async Task<double> GetPixelScaleAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        return client.GetPixelScale();
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get pixel scale: {ex}");
                    return 0.0;
                }
            });
        }

        // PHD2 "set_" methods
        public async Task SetExposureAsync(int exposureMs)
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        client.SetExposure(exposureMs);
                        lastError = null;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to set exposure: {ex}");
                    throw;
                }
            });
        }

        public async Task SetDecGuideModeAsync(string mode)
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        client.SetDecGuideMode(mode);
                        lastError = null;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to set Dec guide mode: {ex}");
                    throw;
                }
            });
        }

        public async Task SetGuideOutputEnabledAsync(bool enabled)
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        client.SetGuideOutputEnabled(enabled);
                        lastError = null;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to set guide output enabled: {ex}");
                    throw;
                }
            });
        }

        public async Task SetLockPositionAsync(double x, double y, bool exact = true)
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        client.SetLockPosition(x, y, exact);
                        lastError = null;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to set lock position: {ex}");
                    throw;
                }
            });
        }

        /// <summary>
        /// Auto-select a star using PHD2's find_star method
        /// </summary>
        /// <param name="roi">Optional region of interest [x, y, width, height]. If null, uses full frame.</param>
        /// <returns>The lock position coordinates [x, y] of the selected star</returns>
        public async Task<double[]> FindStarAsync(int[] roi = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        var result = client.FindStar(roi);
                        lastError = null;
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to find star: {ex}");
                    throw;
                }
            });
        }

        public async Task SetLockShiftEnabledAsync(bool enabled)
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        client.SetLockShiftEnabled(enabled);
                        lastError = null;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to set lock shift enabled: {ex}");
                    throw;
                }
            });
        }

        public async Task SetLockShiftParamsAsync(double xRate, double yRate, string units = "arcsec/hr", string axes = "RA/Dec")
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        client.SetLockShiftParams(xRate, yRate, units, axes);
                        lastError = null;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to set lock shift params: {ex}");
                    throw;
                }
            });
        }

        public async Task SetAlgoParamAsync(string axis, string name, double value)
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        // Log the exact value being sent
                        Logger.Info($"Setting PHD2 algorithm parameter {axis}.{name} = {value:F10} (raw: {value})");
                        
                        // Round to a reasonable precision to avoid floating-point precision issues
                        // PHD2 typically works with values to 2-3 decimal places
                        double roundedValue = Math.Round(value, 3);
                        
                        // Add a tiny offset to all values to avoid PHD2's internal floating-point precision issues
                        // This ensures we don't hit any problematic decimal representations
                        // Use 0.001 offset which will survive the 3-decimal rounding
                        double adjustedValue = roundedValue + 0.001;
                        
                        Logger.Info($"Adjusted value from {roundedValue:F4} to {adjustedValue:F4} to avoid PHD2 floating-point issues");
                        
                        roundedValue = adjustedValue;

                        client.SetAlgoParam(axis, name, roundedValue);
                        
                        // Read back the value to verify what PHD2 actually received
                        try
                        {
                            var actualValue = client.GetAlgoParam(axis, name);
                            Logger.Info($"PHD2 confirmed parameter {axis}.{name} = {actualValue:F10} (expected: {roundedValue:F10})");
                            
                            if (Math.Abs(actualValue - roundedValue) > 0.001)
                            {
                                Logger.Warning($"Value mismatch! Sent: {roundedValue:F10}, Got back: {actualValue:F10}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning($"Could not read back parameter value: {ex.Message}");
                        }
                        
                        lastError = null;
                    }
                }
                catch (PHD2Exception ex) when (ex.Message.Contains("Invalid axis"))
                {
                    // This is expected behavior for invalid axis names
                    lastError = ex.Message;
                    Logger.Debug($"PHD2 invalid axis: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to set algorithm parameter: {ex}");
                    throw;
                }
            });
        }

        public async Task SetVariableDelaySettingsAsync(bool enabled, int shortDelaySeconds, int longDelaySeconds)
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        client.SetVariableDelaySettings(enabled, shortDelaySeconds, longDelaySeconds);
                        lastError = null;
                    }
                }
                catch (PHD2Exception ex) when (ex.Message.Contains("method not found"))
                {
                    // This is expected behavior for unsupported PHD2 versions
                    lastError = ex.Message;
                    Logger.Debug($"PHD2 method not supported: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to set variable delay settings: {ex}");
                    throw;
                }
            });
        }

        // PHD2 "get_" methods
        public async Task<int> GetExposureAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        return client.GetExposure();
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get exposure: {ex}");
                    throw;
                }
            });
        }

        public async Task<string> GetDecGuideModeAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        return client.GetDecGuideMode();
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get Dec guide mode: {ex}");
                    throw;
                }
            });
        }

        public async Task<bool> GetGuideOutputEnabledAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        return client.GetGuideOutputEnabled();
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get guide output enabled: {ex}");
                    throw;
                }
            });
        }

        public async Task<double[]> GetLockPositionAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        return client.GetLockPosition();
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get lock position: {ex}");
                    throw;
                }
            });
        }

        public async Task<bool> GetLockShiftEnabledAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        return client.GetLockShiftEnabled();
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get lock shift enabled: {ex}");
                    throw;
                }
            });
        }

        public async Task<object> GetLockShiftParamsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        return client.GetLockShiftParams();
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get lock shift params: {ex}");
                    throw;
                }
            });
        }

        public async Task<string[]> GetAlgoParamNamesAsync(string axis)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        return client.GetAlgoParamNames(axis);
                    }
                }
                catch (PHD2Exception ex) when (ex.Message.Contains("Invalid axis"))
                {
                    // This is expected behavior for invalid axis names
                    lastError = ex.Message;
                    Logger.Debug($"PHD2 invalid axis: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get algorithm parameter names: {ex}");
                    throw;
                }
            });
        }

        public async Task<double> GetAlgoParamAsync(string axis, string name)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        return client.GetAlgoParam(axis, name);
                    }
                }
                catch (PHD2Exception ex) when (ex.Message.Contains("Invalid axis") || ex.Message.Contains("could not get param"))
                {
                    // This is expected behavior for invalid axis or parameter names
                    lastError = ex.Message;
                    Logger.Debug($"PHD2 parameter error: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get algorithm parameter: {ex}");
                    throw;
                }
            });
        }

        public async Task<object> GetVariableDelaySettingsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        return client.GetVariableDelaySettings();
                    }
                }
                catch (PHD2Exception ex) when (ex.Message.Contains("method not found"))
                {
                    // This is expected behavior for older PHD2 versions
                    lastError = ex.Message;
                    Logger.Debug($"PHD2 method not supported: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get variable delay settings: {ex}");
                    throw;
                }
            });
        }

        public async Task<bool> GetConnectedAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        return client.GetConnected();
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get connected status: {ex}");
                    throw;
                }
            });
        }

        public async Task<bool> GetPausedAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (lockObject)
                    {
                        if (client == null || !client.IsConnected)
                        {
                            throw new InvalidOperationException("PHD2 not connected");
                        }

                        return client.GetPaused();
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    Logger.Error($"Failed to get paused status: {ex}");
                    throw;
                }
            });
        }

        public void Dispose()
        {
            try
            {
                lock (lockObject)
                {
                    client?.Dispose();
                    client = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error disposing PHD2Service: {ex}");
            }
        }
    }
}
