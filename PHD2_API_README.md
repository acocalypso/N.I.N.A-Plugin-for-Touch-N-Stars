# PHD2 Integration API Documentation

This document describes the PHD2 API endpoints added to the Touch 'N' Stars plugin.

## 🚀 Quick Start

1. **Start PHD2** - Make sure PHD2 is running on your system
2. **Start N.I.N.A** with the Touch 'N' Stars plugin enabled
3. **Connect to PHD2** via the API:

```powershell
# Connect to PHD2 (required first step)
Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/connect" -Method POST -ContentType "application/json" -Body '{"hostname": "localhost", "instance": 1}'

# Get all PHD2 information
Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" -Method GET
```

4. **View real-time PHD2 status**: Open `http://localhost:5000/api/phd2/all-info` in your browser or API client

## ✅ API Status

✅ **WORKING**: The PHD2 API integration is fully functional and provides real-time access to all PHD2 information, including:

- Connection status and control
- Real-time guiding statistics
- Equipment profiles
- Pixel scale information  
- Guiding capabilities and state
- Complete PHD2 control (start/stop guiding, dithering, etc.)
- **⭐ Star lost detection with detailed information** (frame, time, SNR, error codes, etc.)
- **🎛️ Full PHD2 parameter control** (all "set_" and "get_" methods from PHD2 API)

### 🆕 Newly Supported PHD2 "set_" Parameter Methods

The API now supports **ALL** PHD2 "set_" parameter methods documented in the official PHD2 EventMonitoring wiki:

- ✅ `set_exposure` - Camera exposure time control
- ✅ `set_dec_guide_mode` - Declination guiding mode (Off/Auto/North/South)
- ✅ `set_guide_output_enabled` - Enable/disable guide output
- ✅ `set_lock_position` - Set guide star lock position
- ✅ `set_lock_shift_enabled` - Enable/disable lock shift
- ✅ `set_lock_shift_params` - Configure lock shift parameters
- ✅ `set_algo_param` - Set guide algorithm parameters
- ✅ `set_variable_delay_settings` - Configure variable delay settings
- ✅ `set_connected` - Connect/disconnect equipment
- ✅ `set_paused` - Pause/unpause guiding
- ✅ `set_profile` - Set equipment profile

**Plus corresponding "get_" methods** for retrieving current parameter values.

## API Base URL

All API endpoints are accessible at: `http://localhost:5000/api/`

**Important:** Make sure to include the `/api` prefix in all API calls.

## Overview

The PHD2 integration allows you to control PHD2 guiding software through REST API endpoints. The integration is based on the PHD2 client implementation from https://github.com/agalasso/phd2client.

## Prerequisites

- PHD2 must be running on the same machine or network
- PHD2's server mode must be enabled (this is typically enabled by default)
- PHD2 listens on port 4400 (default) or 4400 + instance - 1 for multiple instances

## API Endpoints

### Connection Management

#### Get PHD2 Status
```
GET /api/phd2/status
```
Returns the current status of PHD2 including connection state, guiding state, and statistics.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "AppState": "Guiding",
    "AvgDist": 1.23,
    "Stats": {
      "RmsTotal": 1.45,
      "RmsRA": 1.02,
      "RmsDec": 0.98,
      "PeakRA": 2.34,
      "PeakDec": 1.89
    },
    "Version": "2.6.11",
    "PHDSubver": "Dev",
    "IsConnected": true,
    "IsGuiding": true,
    "IsSettling": false
  },
  "StatusCode": 200,
  "Type": "PHD2Status"
}
```

#### Connect to PHD2
```
POST /phd2/connect
```
Connects to PHD2 server.

**Request Body:**
```json
{
  "hostname": "localhost",
  "instance": 1
}
```

#### Disconnect from PHD2
```
POST /phd2/disconnect
```
Disconnects from PHD2 server.

### Equipment Management

#### Get Equipment Profiles
```
GET /phd2/profiles
```
Returns a list of available equipment profiles in PHD2.

#### Connect Equipment
```
POST /phd2/connect-equipment
```
Connects equipment using the specified profile.

**Request Body:**
```json
{
  "profileName": "Simulator"
}
```

#### Disconnect Equipment
```
POST /phd2/disconnect-equipment
```
Disconnects all equipment in PHD2.

**Example curl command:**
```bash
curl -X POST "http://localhost:5000/api/phd2/disconnect-equipment" \
     -H "Content-Type: application/json"
```

**Response:**
```json
{
  "Success": true,
  "Response": {
    "EquipmentDisconnected": true,
    "Error": null
  },
  "StatusCode": 200,
  "Type": "PHD2Equipment"
}
```

### Guiding Control

#### Start Guiding
```
POST /phd2/start-guiding
```
Starts guiding with specified settle parameters.

**Request Body:**
```json
{
  "settlePixels": 2.0,
  "settleTime": 10.0,
  "settleTimeout": 100.0
}
```

#### Stop Guiding
```
POST /phd2/stop-guiding
```
Stops guiding and capture.

#### Pause Guiding
```
POST /phd2/pause
```
Pauses guiding (keeps capturing but stops guide corrections).

#### Unpause Guiding
```
POST /phd2/unpause
```
Resumes guiding after pause.

#### Start Looping
```
POST /phd2/start-looping
```
Starts looping exposures without guiding.

### Dithering

#### Dither
```
POST /phd2/dither
```
Performs a dither operation.

**Request Body:**
```json
{
  "ditherPixels": 3.0,
  "settlePixels": 2.0,
  "settleTime": 10.0,
  "settleTimeout": 100.0
}
```

### Monitoring

#### Check Settling
```
GET /phd2/settling
```
Returns the current settling progress.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Done": false,
    "Distance": 1.23,
    "SettlePx": 2.0,
    "Time": 5.2,
    "SettleTime": 10.0,
    "Status": 0,
    "Error": null
  },
  "StatusCode": 200,
  "Type": "PHD2Settling"
}
```

#### Get Pixel Scale
```
GET /phd2/pixel-scale
```
Returns the current pixel scale in arc-seconds per pixel.

#### Get All PHD2 Information
```
GET /phd2/all-info
```
Returns comprehensive information about PHD2 in a single API call, including status, profiles, settling state, and capabilities.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Connection": {
      "IsConnected": true,
      "LastError": null
    },
    "Status": {
      "AppState": "Guiding",
      "AvgDist": 1.23,
      "Stats": {
        "RmsTotal": 1.45,
        "RmsRA": 1.02,
        "RmsDec": 0.98,
        "PeakRA": 2.34,
        "PeakDec": 1.89
      },
      "Version": "2.6.11",
      "PHDSubver": "Dev",
      "IsConnected": true,
      "IsGuiding": true,
      "IsSettling": false,
      "SettleProgress": null
    },
    "EquipmentProfiles": ["Simulator", "Main Setup"],
    "Settling": {
      "Done": true,
      "Distance": 0.0,
      "SettlePx": 2.0,
      "Time": 0.0,
      "SettleTime": 10.0,
      "Status": 0,
      "Error": null
    },
    "PixelScale": 1.67,
    "Capabilities": {
      "CanGuide": true,
      "CanDither": true,
      "CanPause": true,
      "CanLoop": false
    },
    "GuideStats": {
      "RmsTotal": 1.45,
      "RmsRA": 1.02,
      "RmsDec": 0.98,
      "PeakRA": 2.34,
      "PeakDec": 1.89,
      "AvgDistance": 1.23
    },
    "StarLostInfo": {
      "Frame": 123,
      "Time": 45.6,
      "StarMass": 234.5,
      "SNR": 12.3,
      "AvgDist": 1.23,
      "ErrorCode": 2,
      "Status": "low SNR",
      "Timestamp": "2025-06-30T10:30:00",
      "TimeSinceLost": "00:00:15"
    },
    "ServerInfo": {
      "PHD2Version": "2.6.11",
      "PHD2Subversion": "Dev",
      "AppState": "Guiding",
      "IsGuiding": true,
      "IsSettling": false
    }
  },
  "StatusCode": 200,
  "Type": "PHD2AllInfo"
}
```

## �️ PHD2 Parameter Control

The API provides comprehensive control over PHD2 parameters through "set_" and "get_" methods, allowing advanced configuration of PHD2's behavior.

### Camera Settings

#### Set Camera Exposure
```
POST /phd2/set-exposure
```
Set the camera exposure time in milliseconds.

**Request Body:**
```json
{
  "exposureMs": 1500
}
```

**Response:**
```json
{
  "Success": true,
  "Response": {
    "ExposureSet": 1500
  },
  "StatusCode": 200,
  "Type": "PHD2Parameter"
}
```

#### Get Camera Exposure
```
GET /phd2/get-exposure
```
Get the current camera exposure time.

**⚠️ Note:** This parameter is only available when PHD2 is actively looping or guiding. When PHD2 is in "Stopped" state, it may return 0. Start looping first to get the actual exposure value.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Exposure": 1500
  },
  "StatusCode": 200,
  "Type": "PHD2Parameter"
}
```

### Declination Guiding

#### Set Declination Guide Mode
```
POST /phd2/set-dec-guide-mode
```
Set the declination guiding mode.

**Request Body:**
```json
{
  "mode": "Auto"
}
```

**Valid modes:** `"Off"`, `"Auto"`, `"North"`, `"South"`

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Message": "Dec guide mode set to Auto"
  },
  "StatusCode": 200,
  "Type": "PHD2DecGuideModeSet"
}
```

#### Get Declination Guide Mode
```
GET /phd2/get-dec-guide-mode
```
Get the current declination guide mode.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "DecGuideMode": "Auto"
  },
  "StatusCode": 200,
  "Type": "PHD2DecGuideMode"
}
```

### Guide Output Control

#### Set Guide Output
```
POST /phd2/set-guide-output
```
Enable or disable guide output.

**Request Body:**
```json
{
  "enabled": true
}
```

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Message": "Guide output enabled"
  },
  "StatusCode": 200,
  "Type": "PHD2GuideOutputSet"
}
```

#### Get Guide Output Status
```
GET /phd2/get-guide-output
```
Get the current guide output status.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "GuideOutputEnabled": true
  },
  "StatusCode": 200,
  "Type": "PHD2GuideOutput"
}
```

### Lock Position Control

#### Set Lock Position
```
POST /phd2/set-lock-position
```
Set the guide star lock position.

**Request Body:**
```json
{
  "x": 100.5,
  "y": 200.3,
  "exact": true
}
```

**Parameters:**
- `x`, `y`: Lock position coordinates
- `exact` (optional): If true (default), move to exact coordinates. If false, move current position to coordinates and set lock position to guide star if in range.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Message": "Lock position set to (100.5, 200.3), exact=true"
  },
  "StatusCode": 200,
  "Type": "PHD2LockPositionSet"
}
```

#### Get Lock Position
```
GET /phd2/get-lock-position
```
Get the current lock position.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "X": 100.5,
    "Y": 200.3
  },
  "StatusCode": 200,
  "Type": "PHD2LockPosition"
}
```

### Lock Shift Settings

#### Set Lock Shift Enabled
```
POST /phd2/set-lock-shift
```
Enable or disable lock shift.

**Request Body:**
```json
{
  "enabled": true
}
```

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Message": "Lock shift enabled"
  },
  "StatusCode": 200,
  "Type": "PHD2LockShiftSet"
}
```

#### Set Lock Shift Parameters
```
POST /phd2/set-lock-shift-params
```
Configure lock shift parameters.

**Request Body:**
```json
{
  "xRate": 1.10,
  "yRate": 4.50,
  "units": "arcsec/hr",
  "axes": "RA/Dec"
}
```

**Parameters:**
- `xRate`, `yRate`: Rate values for X and Y axes
- `units` (optional): `"arcsec/hr"` or `"pixels/hr"` (default: `"arcsec/hr"`)
- `axes` (optional): `"RA/Dec"` or `"X/Y"` (default: `"RA/Dec"`)

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Message": "Lock shift parameters set: xRate=1.1, yRate=4.5, units=arcsec/hr, axes=RA/Dec"
  },
  "StatusCode": 200,
  "Type": "PHD2LockShiftParamsSet"
}
```

#### Get Lock Shift Status
```
GET /phd2/get-lock-shift
```
Get the current lock shift enabled status.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "LockShiftEnabled": true
  },
  "StatusCode": 200,
  "Type": "PHD2LockShift"
}
```

#### Get Lock Shift Parameters
```
GET /phd2/get-lock-shift-params
```
Get the current lock shift parameters.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "enabled": true,
    "rate": [1.10, 4.50],
    "units": "arcsec/hr",
    "axes": "RA/Dec"
  },
  "StatusCode": 200,
  "Type": "PHD2LockShiftParams"
}
```

### Guide Algorithm Parameters

#### Set Algorithm Parameter
```
POST /phd2/set-algo-param
```
Set a guide algorithm parameter for a specific axis.

**Request Body:**
```json
{
  "axis": "ra",
  "name": "MinMove",
  "value": 0.15
}
```

**Parameters:**
- `axis`: `"ra"`, `"x"`, `"dec"`, or `"y"`
- `name`: Parameter name (use Get Algorithm Parameter Names to see available parameters)
- `value`: Parameter value

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Message": "Algorithm parameter set: ra.MinMove = 0.15"
  },
  "StatusCode": 200,
  "Type": "PHD2AlgoParamSet"
}
```

#### Get Algorithm Parameter Names
```
GET /phd2/get-algo-param-names?axis=ra
```
Get the list of available algorithm parameter names for an axis.

**Query Parameters:**
- `axis`: `"ra"`, `"x"`, `"dec"`, or `"y"`

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Axis": "ra",
    "ParameterNames": ["MinMove", "MaxMove", "Aggression", "Hysteresis"]
  },
  "StatusCode": 200,
  "Type": "PHD2AlgoParamNames"
}
```

#### Get Algorithm Parameter
```
GET /phd2/get-algo-param?axis=ra&name=MinMove
```
Get the value of a specific algorithm parameter.

**Query Parameters:**
- `axis`: `"ra"`, `"x"`, `"dec"`, or `"y"`
- `name`: Parameter name

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Axis": "ra",
    "Name": "MinMove",
    "Value": 0.15
  },
  "StatusCode": 200,
  "Type": "PHD2AlgoParam"
}
```

### Variable Delay Settings

#### Set Variable Delay Settings
```
POST /phd2/set-variable-delay
```
Configure variable delay settings.

**Request Body:**
```json
{
  "enabled": true,
  "shortDelaySeconds": 2,
  "longDelaySeconds": 10
}
```

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Message": "Variable delay settings updated: enabled=true, short=2s, long=10s"
  },
  "StatusCode": 200,
  "Type": "PHD2VariableDelaySet"
}
```

#### Get Variable Delay Settings
```
GET /phd2/get-variable-delay
```
Get the current variable delay settings.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Enabled": true,
    "ShortDelaySeconds": 2,
    "LongDelaySeconds": 10
  },
  "StatusCode": 200,
  "Type": "PHD2VariableDelay"
}
```

### Connection Status

#### Get Equipment Connection Status
```
GET /phd2/get-connected
```
Get the current equipment connection status.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Connected": true
  },
  "StatusCode": 200,
  "Type": "PHD2Connected"
}
```

#### Get Paused Status
```
GET /phd2/get-paused
```
Get the current paused status.

**Response:**
```json
{
  "Success": true,
  "Response": {
    "Paused": false
  },
  "StatusCode": 200,
  "Type": "PHD2Paused"
}
```

## �🌟 Star Lost Detection

The API provides comprehensive star lost detection with detailed diagnostic information:

### Star Lost Information

When PHD2 loses the guide star, the API captures detailed information including:

- **Frame**: The frame number when the star was lost
- **Time**: Time since guiding started (in seconds) 
- **StarMass**: Star mass value at the time of loss
- **SNR**: Signal-to-noise ratio when the star was lost
- **AvgDist**: Average guide distance in pixels
- **ErrorCode**: PHD2 error code (1=saturated, 2=low SNR, 3=low mass, 4=low HFD, 5=high HFD, 6=edge of frame, 7=mass change, 8=unexpected)
- **Status**: Human-readable error message
- **Timestamp**: When the star loss occurred
- **TimeSinceLost**: How long ago the star was lost

### Checking for Star Loss

```powershell
# Get current PHD2 status including star lost info
$info = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" -Method GET

# Check if a star was lost
if ($info.Response.Status.AppState -eq "LostLock") {
    Write-Host "⚠️  Star Lost!"
    if ($info.Response.StarLostInfo) {
        Write-Host "Frame: $($info.Response.StarLostInfo.Frame)"
        Write-Host "Error: $($info.Response.StarLostInfo.Status)"
        Write-Host "SNR: $($info.Response.StarLostInfo.SNR)"
        Write-Host "Time since lost: $($info.Response.StarLostInfo.TimeSinceLost)"
    }
}
```

### Error Codes Reference

| Code | Meaning |
|------|---------|
| 1 | Saturated |
| 2 | Low SNR |
| 3 | Low mass |
| 4 | Low HFD |
| 5 | High HFD |
| 6 | Edge of frame |
| 7 | Mass change |
| 8 | Unexpected |

## Error Handling

All endpoints return a consistent response format:

```json
{
  "Success": boolean,
  "Response": object,
  "Error": string,
  "StatusCode": number,
  "Type": string
}
```

When an error occurs:
- `Success` will be `false`
- `Error` will contain the error message
- `StatusCode` will indicate the HTTP status code
- `Response` may contain additional error details

## Usage Examples

### JavaScript/Web Client

```javascript
// Connect to PHD2
const connectResponse = await fetch('/phd2/connect', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ hostname: 'localhost', instance: 1 })
});

// Get comprehensive PHD2 information
const allInfoResponse = await fetch('/phd2/all-info');
const allInfo = await allInfoResponse.json();

// Check if PHD2 is ready for guiding
if (allInfo.Response.Capabilities.CanGuide) {
  console.log('PHD2 is ready for guiding');
  console.log('Current state:', allInfo.Response.Status.AppState);
  console.log('Guide stats:', allInfo.Response.GuideStats);
}

// Start guiding
const guideResponse = await fetch('/phd2/start-guiding', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ 
    settlePixels: 2.0, 
    settleTime: 10.0, 
    settleTimeout: 100.0 
  })
});
```

### PowerShell Examples (Tested & Working)

```powershell
# Connect to PHD2 (required first step)
$connectResult = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/connect" -Method POST -ContentType "application/json" -Body '{"hostname": "localhost", "instance": 1}'
Write-Host "Connected: $($connectResult.Response.Connected)"

# Get all PHD2 information
$allInfo = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" -Method GET
$allInfo.Response | ConvertTo-Json -Depth 10

# Example real output:
# {
#     "Connection": {
#         "IsConnected": true,
#         "LastError": null
#     },
#     "Status": {
#         "AppState": "Looping",
#         "AvgDist": 0,
#         "Stats": {...},
#         "Version": "2.6.13",
#         "IsConnected": true,
#         "IsGuiding": false
#     },
#     "EquipmentProfiles": ["test"],
#     "PixelScale": 9.6257,
#     "Capabilities": {
#         "CanGuide": true,
#         "CanDither": false,
#         "CanPause": false,
#         "CanLoop": false
#     }
# }

# Equipment management workflow
# 1. Get available profiles
$profiles = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/profiles" -Method GET
Write-Host "Available profiles: $($profiles.Response -join ', ')"
