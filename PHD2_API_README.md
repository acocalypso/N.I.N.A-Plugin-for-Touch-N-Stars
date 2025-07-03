# PHD2 Integration API Documentation

> **Professional REST API for PHD2 Guiding Control**  
> Complete integration between N.I.N.A Touch 'N' Stars plugin and PHD2 guiding software

---

## 📋 Table of Contents

- [🚀 Quick Start](#-quick-start)
- [📡 API Overview](#-api-overview)
- [🔧 Prerequisites](#-prerequisites)
- [📚 API Reference](#-api-reference)
  - [Connection Management](#connection-management)
  - [Equipment Management](#equipment-management)
  - [Guiding Control](#guiding-control)
  - [Monitoring & Status](#monitoring--status)
  - [Advanced Parameters](#advanced-parameters)
  - [Star Selection](#star-selection)
- [🌟 Advanced Features](#-advanced-features)
- [📖 Usage Examples](#-usage-examples)
- [❌ Error Handling](#-error-handling)
- [🔍 Troubleshooting](#-troubleshooting)

---

## 🚀 Quick Start

### Step 1: Setup
1. **Start PHD2** on your system
2. **Start N.I.N.A** with Touch 'N' Stars plugin enabled
3. Ensure PHD2 server mode is active (default: port 4400)

### Step 2: Connect to PHD2
```powershell
# Essential first step - establish connection
Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/connect" `
    -Method POST -ContentType "application/json" `
    -Body '{"hostname": "localhost", "instance": 1}'
```

### Step 3: Verify Status
```powershell
# Get comprehensive PHD2 information
Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" -Method GET
```

### Step 4: Start Using
🌐 **Live Status**: `http://localhost:5000/api/phd2/all-info`

---

## 📡 API Overview

**Base URL**: `http://localhost:5000/api/`

> ⚠️ **Important**: Always include the `/api/` prefix in all API calls

### Key Features
✅ **Real-time guiding statistics**  
✅ **Complete equipment control**  
✅ **Advanced parameter management**  
✅ **Star lost detection with diagnostics**  
✅ **Professional error handling**  
✅ **Comprehensive monitoring capabilities**

### Technology Stack
- **Protocol**: REST API with JSON
- **Base Implementation**: [PHD2 Client](https://github.com/agalasso/phd2client)
- **Standards**: PHD2 EventMonitoring API

---

## 🔧 Prerequisites

| Requirement | Details |
|-------------|---------|
| **PHD2** | Running with server mode enabled |
| **Network** | PHD2 accessible on port 4400 (default) |
| **N.I.N.A** | Touch 'N' Stars plugin installed and active |
| **Ports** | 4400 + (instance - 1) for multiple PHD2 instances |

---

---

## 📚 API Reference

### Connection Management

#### 🔌 Connect to PHD2
**`POST /api/phd2/connect`**

Establishes connection to PHD2 server.

<details>
<summary><strong>📝 Request Details</strong></summary>

**Request Body:**
```json
{
  "hostname": "localhost",
  "instance": 1
}
```

**Parameters:**
- `hostname`: PHD2 server address (default: "localhost")
- `instance`: PHD2 instance number (default: 1)

</details>

<details>
<summary><strong>✅ Response Example</strong></summary>

```json
{
  "Success": true,
  "Response": {
    "Connected": true,
    "Error": null
  },
  "StatusCode": 200,
  "Type": "PHD2Connection"
}
```

</details>

---

#### 🔌 Disconnect from PHD2
**`POST /api/phd2/disconnect`**

Closes connection to PHD2 server.

<details>
<summary><strong>✅ Response Example</strong></summary>

```json
{
  "Success": true,
  "Response": {
    "Connected": false
  },
  "StatusCode": 200,
  "Type": "PHD2Connection"
}
```

</details>

---

#### 📊 Get PHD2 Status
**`GET /api/phd2/status`**

Returns current PHD2 status with comprehensive information.

<details>
<summary><strong>✅ Response Example</strong></summary>

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

</details>

---

#### 📋 Get Comprehensive Information
**`GET /api/phd2/all-info`**

**🎯 Recommended Endpoint** - Returns all PHD2 information in a single call.

<details>
<summary><strong>✅ Complete Response Example</strong></summary>

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
      "IsSettling": false
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

</details>

---

### Equipment Management

#### 📋 List Equipment Profiles
**`GET /api/phd2/profiles`**

Returns available equipment profiles configured in PHD2.

<details>
<summary><strong>✅ Response Example</strong></summary>

```json
{
  "Success": true,
  "Response": ["Simulator", "Main Setup", "Backup Config"],
  "StatusCode": 200,
  "Type": "PHD2Profiles"
}
```

</details>

---

#### 🔗 Connect Equipment
**`POST /api/phd2/connect-equipment`**

Connects equipment using specified profile.

<details>
<summary><strong>📝 Request Details</strong></summary>

**Request Body:**
```json
{
  "profileName": "Simulator"
}
```

**Parameters:**
- `profileName`: Name of the equipment profile (required)

</details>

<details>
<summary><strong>✅ Response Example</strong></summary>

```json
{
  "Success": true,
  "Response": {
    "EquipmentConnected": true,
    "Error": null
  },
  "StatusCode": 200,
  "Type": "PHD2Equipment"
}
```

</details>

---

#### 🔌 Disconnect Equipment
**`POST /api/phd2/disconnect-equipment`**

Disconnects all equipment in PHD2.

<details>
<summary><strong>💻 cURL Example</strong></summary>

```bash
curl -X POST "http://localhost:5000/api/phd2/disconnect-equipment" \
     -H "Content-Type: application/json"
```

</details>

<details>
<summary><strong>✅ Response Example</strong></summary>

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

</details>

---

### Guiding Control

#### 🎯 Start Guiding
**`POST /api/phd2/start-guiding`**

Initiates guiding with customizable settling parameters.

<details>
<summary><strong>📝 Request Details</strong></summary>

**Request Body:**
```json
{
  "settlePixels": 2.0,
  "settleTime": 10.0,
  "settleTimeout": 100.0
}
```

**Parameters:**
- `settlePixels`: Maximum deviation for settled guiding (pixels)
- `settleTime`: Minimum time to maintain settling (seconds)
- `settleTimeout`: Maximum time to wait for settling (seconds)

</details>

---

#### ⏹️ Stop Guiding
**`POST /api/phd2/stop-guiding`**

Stops guiding and capture operations.

---

#### ⏸️ Pause/Resume Guiding
**`POST /api/phd2/pause`** | **`POST /api/phd2/unpause`**

Pauses guiding (continues exposures) or resumes after pause.

---

#### 🔄 Start Looping
**`POST /api/phd2/start-looping`**

Starts continuous exposures without guiding corrections.

---

#### 🎲 Dither
**`POST /api/phd2/dither`**

Performs dithering operation with settling.

<details>
<summary><strong>📝 Request Details</strong></summary>

**Request Body:**
```json
{
  "ditherPixels": 3.0,
  "settlePixels": 2.0,
  "settleTime": 10.0,
  "settleTimeout": 100.0
}
```

</details>

---

### Monitoring & Status

#### ⏳ Check Settling Progress
**`GET /api/phd2/settling`**

Returns current settling status after guide or dither operations.

<details>
<summary><strong>✅ Response Example</strong></summary>

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

</details>

---

#### 📏 Get Pixel Scale
**`GET /api/phd2/pixel-scale`**

Returns current pixel scale in arc-seconds per pixel.

<details>
<summary><strong>✅ Response Example</strong></summary>

```json
{
  "Success": true,
  "Response": {
    "PixelScale": 1.67
  },
  "StatusCode": 200,
  "Type": "PHD2PixelScale"
}
```

</details>

---

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

---

### Advanced Parameters

#### ⚙️ Camera Settings

**Set Exposure Time** - `POST /api/phd2/set-exposure`

<details>
<summary><strong>📝 Request Details</strong></summary>

```json
{
  "exposureMs": 1500
}
```
> **Note**: Exposure setting only works when PHD2 is actively looping or guiding.

</details>

**Get Exposure Time** - `GET /api/phd2/get-exposure`

---

#### 🧭 Declination Guiding

**Set Dec Guide Mode** - `POST /api/phd2/set-dec-guide-mode`

<details>
<summary><strong>📝 Request Details</strong></summary>

```json
{
  "mode": "Auto"
}
```

**Valid Modes**: `"Off"` | `"Auto"` | `"North"` | `"South"`

</details>

**Get Dec Guide Mode** - `GET /api/phd2/get-dec-guide-mode`

---

#### 🎛️ Guide Output Control

**Enable/Disable Output** - `POST /api/phd2/set-guide-output-enabled`

<details>
<summary><strong>📝 Request Details</strong></summary>

```json
{
  "enabled": true
}
```

</details>

**Get Output Status** - `GET /api/phd2/get-guide-output-enabled`

---

#### 🎯 Lock Position Management

**Set Lock Position** - `POST /api/phd2/set-lock-position`

<details>
<summary><strong>📝 Request Details</strong></summary>

```json
{
  "x": 100.5,
  "y": 200.3,
  "exact": true
}
```

**Parameters:**
- `x`, `y`: Lock position coordinates
- `exact`: If true (default), move to exact coordinates

</details>

**Get Lock Position** - `GET /api/phd2/get-lock-position`

---

#### 🔄 Lock Shift Settings

**Enable Lock Shift** - `POST /api/phd2/set-lock-shift-enabled`

**Configure Parameters** - `POST /api/phd2/set-lock-shift-params`

<details>
<summary><strong>📝 Request Details</strong></summary>

```json
{
  "xRate": 1.10,
  "yRate": 4.50,
  "units": "arcsec/hr",
  "axes": "RA/Dec"
}
```

**Units**: `"arcsec/hr"` | `"pixels/hr"`  
**Axes**: `"RA/Dec"` | `"X/Y"`

</details>

---

#### 🧮 Algorithm Parameters

**Set Parameter** - `POST /api/phd2/set-algo-param`

<details>
<summary><strong>📝 Request Details</strong></summary>

```json
{
  "axis": "ra",
  "name": "MinMove",
  "value": 0.15
}
```

**Axes**: `"ra"` | `"x"` | `"dec"` | `"y"`

</details>

**Get Parameter Names** - `GET /api/phd2/get-algo-param-names?axis=ra`

**Get Parameter Value** - `GET /api/phd2/get-algo-param?axis=ra&name=MinMove`

---

#### ⏱️ Variable Delay Settings

**Configure Delays** - `POST /api/phd2/set-variable-delay-settings`

<details>
<summary><strong>📝 Request Details</strong></summary>

```json
{
  "enabled": true,
  "shortDelaySeconds": 2,
  "longDelaySeconds": 10
}
```

</details>

**Get Delay Settings** - `GET /api/phd2/get-variable-delay-settings`

---

#### 🔗 Connection Status

**Get Equipment Status** - `GET /api/phd2/get-connected`

**Get Pause Status** - `GET /api/phd2/get-paused`

---

### Star Selection

#### ⭐ Auto-Find Star
**`POST /api/phd2/find-star`**

Automatically selects the best guide star in the field or specified region.

<details>
<summary><strong>💻 Basic Usage (Full Frame)</strong></summary>

**cURL:**
```bash
curl -X POST "http://localhost:5000/api/phd2/find-star" \
     -H "Content-Type: application/json" \
     -d "{}"
```

**PowerShell:**
```powershell
$result = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/find-star" `
    -Method POST -ContentType "application/json" -Body "{}"
Write-Host "Star found at: X=$($result.Response.StarPosition.X), Y=$($result.Response.StarPosition.Y)"
```

</details>

<details>
<summary><strong>🎯 Region of Interest (ROI) Selection</strong></summary>

**cURL:**
```bash
curl -X POST "http://localhost:5000/api/phd2/find-star" \
     -H "Content-Type: application/json" \
     -d '{"roi": [100, 200, 400, 300]}'
```

**PowerShell:**
```powershell
$body = @{
    roi = @(100, 200, 400, 300)  # [x, y, width, height]
} | ConvertTo-Json

$result = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/find-star" `
    -Method POST -ContentType "application/json" -Body $body
```

**ROI Parameters**: `[x, y, width, height]`
- `x`, `y`: Starting coordinates of search region
- `width`, `height`: Dimensions of search area

</details>

<details>
<summary><strong>✅ Response Example</strong></summary>

```json
{
  "Success": true,
  "Response": {
    "StarPosition": {
      "X": 245.67,
      "Y": 189.34
    },
    "ROI": {
      "X": 100,
      "Y": 200,
      "Width": 400,
      "Height": 300
    }
  },
  "StatusCode": 200,
  "Type": "PHD2StarSelection"
}
```

</details>

---

## 🌟 Advanced Features

### Star Lost Detection & Diagnostics

The API provides comprehensive star loss monitoring with detailed diagnostic information:

#### 📊 Star Loss Information
When PHD2 loses the guide star, detailed diagnostics are captured:

| Field | Description |
|-------|-------------|
| **Frame** | Frame number when star was lost |
| **Time** | Time since guiding started (seconds) |
| **StarMass** | Star mass value at time of loss |
| **SNR** | Signal-to-noise ratio when lost |
| **AvgDist** | Average guide distance (pixels) |
| **ErrorCode** | PHD2 error code (see reference below) |
| **Status** | Human-readable error message |
| **Timestamp** | When the star loss occurred |
| **TimeSinceLost** | Duration since star was lost |

#### 🚨 Error Code Reference

| Code | Meaning | Description |
|------|---------|-------------|
| 1 | **Saturated** | Star is overexposed |
| 2 | **Low SNR** | Insufficient signal-to-noise ratio |
| 3 | **Low Mass** | Star mass below threshold |
| 4 | **Low HFD** | Half-flux diameter too small |
| 5 | **High HFD** | Half-flux diameter too large |
| 6 | **Edge of Frame** | Star moved to frame edge |
| 7 | **Mass Change** | Significant star mass variation |
| 8 | **Unexpected** | Unknown error condition |

#### 🔍 Monitoring Star Loss

<details>
<summary><strong>PowerShell Monitoring Example</strong></summary>

```powershell
# Monitor for star loss
$info = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" -Method GET

if ($info.Response.Status.AppState -eq "LostLock") {
    Write-Host "⚠️  Star Lost!" -ForegroundColor Red
    if ($info.Response.StarLostInfo) {
        Write-Host "Frame: $($info.Response.StarLostInfo.Frame)"
        Write-Host "Error: $($info.Response.StarLostInfo.Status)"
        Write-Host "SNR: $($info.Response.StarLostInfo.SNR)"
        Write-Host "Time since lost: $($info.Response.StarLostInfo.TimeSinceLost)"
    }
}
```

</details>

---
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

# 2. Connect equipment
$connectEquipment = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/connect-equipment" -Method POST
Write-Host "Equipment connected: $($connectEquipment.Success)"

# 3. Set exposure time
$setExposure = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-exposure" -Method POST -ContentType "application/json" -Body '{"seconds": 2.0}'
Write-Host "Exposure set: $($setExposure.Response.Message)"

# 4. Start looping to capture frames
$startLoop = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/loop" -Method POST
Write-Host "Loop started: $($startLoop.Success)"

# 5. Find a guide star
$findStar = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/find-star" -Method POST
Write-Host "Star found: $($findStar.Response.Message)"

# 6. Start guiding
$startGuide = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/start-guiding" -Method POST -ContentType "application/json" -Body '{"settlePixels": 2.0, "settleTime": 10.0, "settleTimeout": 100.0}'
Write-Host "Guiding started: $($startGuide.Success)"

# Monitor guiding status
do {
    Start-Sleep -Seconds 5
    $status = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" -Method GET
    Write-Host "Status: $($status.Response.Status.AppState) | RMS: $($status.Response.GuideStats.RmsRa)/$($status.Response.GuideStats.RmsDec)"
} while ($status.Response.Status.AppState -eq "Guiding")
```

---

## 🌟 Advanced Features

<details>
<summary><strong>🎯 ROI Selection & Auto-Star Finding</strong></summary>

### ROI (Region of Interest) Selection
```powershell
# Find star in specific region (ROI)
$findStarROI = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/find-star" `
    -Method POST -ContentType "application/json" `
    -Body '{"roi": [100, 200, 300, 400]}'
```

### Auto-Star Finding
```powershell
# Find best star automatically (full frame)
$autoStar = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/find-star" -Method POST
```

</details>

<details>
<summary><strong>⚙️ Advanced Guiding Parameters</strong></summary>

### Lock Position Management
```powershell
# Set custom lock position
$setLock = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-lock-position" `
    -Method POST -ContentType "application/json" `
    -Body '{"x": 150.5, "y": 200.3, "exact": true}'

# Enable lock shift for gradual position changes
$lockShift = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-lock-shift-params" `
    -Method POST -ContentType "application/json" `
    -Body '{"rate": [1.0, 2.0], "units": "arcsec/hr", "axes": "RA/Dec"}'
```

### Algorithm Fine-Tuning
```powershell
# Adjust RA axis minimum move
$setAlgo = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-algo-param" `
    -Method POST -ContentType "application/json" `
    -Body '{"axis": "ra", "name": "MinMove", "value": 0.12}'

# Get all available parameters for an axis
$algoParams = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/get-algo-param-names?axis=ra" -Method GET
```

</details>

<details>
<summary><strong>📊 Real-Time Monitoring</strong></summary>

### Comprehensive Status Monitoring
```powershell
function Monitor-PHD2Status {
    do {
        $info = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" -Method GET
        
        Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
        Write-Host "🔭 PHD2 Status: $($info.Response.Status.AppState)" -ForegroundColor Green
        Write-Host "📐 Pixel Scale: $($info.Response.PixelScale) arcsec/pixel" -ForegroundColor Yellow
        
        if ($info.Response.Status.IsGuiding) {
            Write-Host "📊 RMS Error: RA=$($info.Response.GuideStats.RmsRa) DEC=$($info.Response.GuideStats.RmsDec)" -ForegroundColor Magenta
            Write-Host "🎯 Total RMS: $($info.Response.GuideStats.RmsTotal)" -ForegroundColor Magenta
            Write-Host "📈 Peak Error: RA=$($info.Response.GuideStats.PeakRa) DEC=$($info.Response.GuideStats.PeakDec)" -ForegroundColor Red
        }
        
        if ($info.Response.StarLostInfo) {
            Write-Host "⚠️  Star Lost: $($info.Response.StarLostInfo.Status)" -ForegroundColor Red
            Write-Host "🔍 SNR: $($info.Response.StarLostInfo.SNR)" -ForegroundColor Red
        }
        
        Start-Sleep -Seconds 3
    } while ($true)
}

# Run the monitor
Monitor-PHD2Status
```

</details>

---

## 📖 Usage Examples

<details>
<summary><strong>🚀 Complete Automated Session</strong></summary>

```powershell
# Complete PHD2 automation script
param(
    [string]$Profile = "Main Camera",
    [double]$ExposureTime = 2.0,
    [double]$SettlePixels = 2.0,
    [double]$SettleTime = 10.0
)

Write-Host "🌟 Starting PHD2 Automated Session" -ForegroundColor Green

try {
    # 1. Connect to PHD2
    Write-Host "📡 Connecting to PHD2..." -ForegroundColor Yellow
    $connect = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/connect" `
        -Method POST -ContentType "application/json" `
        -Body '{"hostname": "localhost", "instance": 1}'
    
    if (-not $connect.Success) { throw "Failed to connect to PHD2: $($connect.Error)" }
    Write-Host "✅ Connected to PHD2" -ForegroundColor Green
    
    # 2. Set profile
    Write-Host "⚙️ Setting equipment profile..." -ForegroundColor Yellow
    $setProfile = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-profile" `
        -Method POST -ContentType "application/json" `
        -Body "{`"profileName`": `"$Profile`"}"
    
    if ($setProfile.Success) {
        Write-Host "✅ Profile set: $Profile" -ForegroundColor Green
    }
    
    # 3. Connect equipment
    Write-Host "🔌 Connecting equipment..." -ForegroundColor Yellow
    $connectEquip = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/connect-equipment" -Method POST
    
    if (-not $connectEquip.Success) { throw "Failed to connect equipment: $($connectEquip.Error)" }
    Write-Host "✅ Equipment connected" -ForegroundColor Green
    
    # 4. Set exposure
    Write-Host "⏱️ Setting exposure time..." -ForegroundColor Yellow
    $setExp = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-exposure" `
        -Method POST -ContentType "application/json" `
        -Body "{`"seconds`": $ExposureTime}"
    
    Write-Host "✅ Exposure set: $ExposureTime seconds" -ForegroundColor Green
    
    # 5. Start looping
    Write-Host "🔄 Starting image loop..." -ForegroundColor Yellow
    $loop = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/loop" -Method POST
    
    if (-not $loop.Success) { throw "Failed to start loop: $($loop.Error)" }
    Write-Host "✅ Looping started" -ForegroundColor Green
    
    # 6. Wait for stable loop and find star
    Write-Host "🔍 Finding guide star..." -ForegroundColor Yellow
    Start-Sleep -Seconds 3
    $findStar = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/find-star" -Method POST
    
    if (-not $findStar.Success) { throw "Failed to find star: $($findStar.Error)" }
    Write-Host "✅ Guide star found" -ForegroundColor Green
    
    # 7. Start guiding
    Write-Host "🎯 Starting guiding..." -ForegroundColor Yellow
    $startGuide = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/start-guiding" `
        -Method POST -ContentType "application/json" `
        -Body "{`"settlePixels`": $SettlePixels, `"settleTime`": $SettleTime, `"settleTimeout`": 100.0}"
    
    if (-not $startGuide.Success) { throw "Failed to start guiding: $($startGuide.Error)" }
    Write-Host "✅ Guiding started successfully!" -ForegroundColor Green
    
    # 8. Monitor guiding
    Write-Host "📊 Monitoring guiding performance..." -ForegroundColor Cyan
    do {
        Start-Sleep -Seconds 5
        $status = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" -Method GET
        
        if ($status.Response.Status.AppState -eq "Guiding") {
            $rmsRa = [math]::Round($status.Response.GuideStats.RmsRa, 2)
            $rmsDec = [math]::Round($status.Response.GuideStats.RmsDec, 2)
            $rmsTotal = [math]::Round($status.Response.GuideStats.RmsTotal, 2)
            Write-Host "📈 RMS: RA=$rmsRa DEC=$rmsDec Total=$rmsTotal" -ForegroundColor Magenta
        } elseif ($status.Response.Status.AppState -eq "LostLock") {
            Write-Host "⚠️ Star lost! Attempting recovery..." -ForegroundColor Red
            break
        }
    } while ($status.Response.Status.AppState -eq "Guiding")
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
```

</details>

<details>
<summary><strong>🔧 Equipment Management Workflow</strong></summary>

```powershell
# Equipment profile management
Write-Host "📋 Available Equipment Profiles:" -ForegroundColor Cyan
$profiles = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/profiles" -Method GET
$profiles.Response | ForEach-Object { Write-Host "  • $_" -ForegroundColor White }

# Connect/Disconnect equipment workflow
Write-Host "🔌 Equipment Connection Workflow" -ForegroundColor Cyan

# Check current connection status
$connected = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/get-connected" -Method GET
Write-Host "Current connection status: $($connected.Response.Connected)" -ForegroundColor $(if($connected.Response.Connected) {'Green'} else {'Red'})

if (-not $connected.Response.Connected) {
    # Connect equipment
    Write-Host "Connecting equipment..." -ForegroundColor Yellow
    $connect = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/connect-equipment" -Method POST
    
    if ($connect.Success) {
        Write-Host "✅ Equipment connected successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to connect equipment: $($connect.Error)" -ForegroundColor Red
    }
} else {
    # Disconnect equipment when done
    Write-Host "Disconnecting equipment..." -ForegroundColor Yellow
    $disconnect = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/disconnect-equipment" -Method POST
    
    if ($disconnect.Success) {
        Write-Host "✅ Equipment disconnected successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ Failed to disconnect equipment: $($disconnect.Error)" -ForegroundColor Red
    }
}
```

</details>

<details>
<summary><strong>🎛️ Advanced Parameter Configuration</strong></summary>

```powershell
# Advanced PHD2 configuration script
Write-Host "⚙️ Configuring Advanced PHD2 Parameters" -ForegroundColor Cyan

# 1. Configure variable delay settings
Write-Host "Setting variable delay..." -ForegroundColor Yellow
$varDelay = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-variable-delay" `
    -Method POST -ContentType "application/json" `
    -Body '{"enabled": true, "shortDelaySeconds": 1, "longDelaySeconds": 5}'
Write-Host "✅ Variable delay configured" -ForegroundColor Green

# 2. Configure dithering settings
Write-Host "Setting dither parameters..." -ForegroundColor Yellow
$dither = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-dither-settings" `
    -Method POST -ContentType "application/json" `
    -Body '{"amount": 3.0, "raOnly": false}'
Write-Host "✅ Dither settings configured" -ForegroundColor Green

# 3. Fine-tune guiding algorithms
Write-Host "Optimizing guide algorithms..." -ForegroundColor Yellow

# Get available parameters for RA axis
$raParams = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/get-algo-param-names?axis=ra" -Method GET
Write-Host "Available RA parameters: $($raParams.Response.ParameterNames -join ', ')" -ForegroundColor White

# Set optimal parameters
$setMinMove = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-algo-param" `
    -Method POST -ContentType "application/json" `
    -Body '{"axis": "ra", "name": "MinMove", "value": 0.1}'

$setAggression = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-algo-param" `
    -Method POST -ContentType "application/json" `
    -Body '{"axis": "ra", "name": "Aggression", "value": 75}'

Write-Host "✅ Algorithm parameters optimized" -ForegroundColor Green

# 4. Configure lock shift for long exposures
Write-Host "Setting lock shift parameters..." -ForegroundColor Yellow
$lockShift = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-lock-shift-params" `
    -Method POST -ContentType "application/json" `
    -Body '{"rate": [0.5, 1.2], "units": "arcsec/hr", "axes": "RA/Dec"}'
Write-Host "✅ Lock shift configured for drift compensation" -ForegroundColor Green

Write-Host "🎉 Advanced configuration complete!" -ForegroundColor Green
```

</details>

---

## ❌ Error Handling

### Response Format
All API endpoints return a consistent response structure:

```json
{
  "Success": boolean,
  "Response": object | null,
  "Error": string | null,
  "StatusCode": number,
  "Type": string
}
```

### Error Response Examples

<details>
<summary><strong>❌ Connection Errors</strong></summary>

```json
{
  "Success": false,
  "Response": null,
  "Error": "PHD2 is not running or not accepting connections on localhost:4400",
  "StatusCode": 500,
  "Type": "PHD2ConnectionError"
}
```

**Solutions:**
- Ensure PHD2 is running
- Check PHD2 server settings (Tools → Options → Advanced → Enable Server)
- Verify hostname and port settings
- Check firewall settings

</details>

<details>
<summary><strong>❌ Equipment Errors</strong></summary>

```json
{
  "Success": false,
  "Response": null,
  "Error": "Equipment not connected. Please connect camera and mount first.",
  "StatusCode": 400,
  "Type": "PHD2EquipmentError"
}
```

**Solutions:**
- Connect camera and mount in PHD2
- Select appropriate equipment profile
- Check equipment connections and drivers
- Verify equipment compatibility

</details>

<details>
<summary><strong>❌ Star Finding Errors</strong></summary>

```json
{
  "Success": false,
  "Response": null,
  "Error": "No suitable guide star found in the current field",
  "StatusCode": 404,
  "Type": "PHD2StarNotFound"
}
```

**Solutions:**
- Adjust exposure time for better star visibility
- Move to a field with more stars
- Check focus quality
- Verify camera settings and gain
- Use ROI to select specific area

</details>

<details>
<summary><strong>❌ Guiding Errors</strong></summary>

```json
{
  "Success": false,
  "Response": null,
  "Error": "Cannot start guiding: PHD2 is not in the correct state (current: Looping, required: Selected)",
  "StatusCode": 409,
  "Type": "PHD2StateError"
}
```

**Solutions:**
- Select a guide star first using `/phd2/find-star`
- Ensure PHD2 is in the correct state sequence
- Check guide star selection and lock position
- Verify mount connection and calibration

</details>

### Error Handling Best Practices

```powershell
function Invoke-PHD2ApiSafely {
    param(
        [string]$Uri,
        [string]$Method = "GET",
        [string]$Body = $null
    )
    
    try {
        $params = @{
            Uri = $Uri
            Method = $Method
            ContentType = "application/json"
        }
        
        if ($Body) {
            $params.Body = $Body
        }
        
        $response = Invoke-RestMethod @params
        
        if (-not $response.Success) {
            Write-Warning "API call failed: $($response.Error)"
            Write-Host "Error Type: $($response.Type)" -ForegroundColor Red
            Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Red
            return $null
        }
        
        return $response
        
    } catch {
        Write-Error "Network error calling PHD2 API: $($_.Exception.Message)"
        return $null
    }
}

# Usage example with error handling
$result = Invoke-PHD2ApiSafely -Uri "http://localhost:5000/api/phd2/connect" -Method "POST" -Body '{"hostname": "localhost", "instance": 1}'
if ($result) {
    Write-Host "✅ Successfully connected to PHD2" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to connect to PHD2" -ForegroundColor Red
}
```

---

## 🔍 Troubleshooting

### Common Issues & Solutions

<details>
<summary><strong>🔧 Connection Issues</strong></summary>

#### Issue: "Connection refused" or timeout errors

**Symptoms:**
- API returns connection timeout errors
- PHD2 appears running but API cannot connect
- Intermittent connection failures

**Solutions:**

1. **Check PHD2 Server Settings:**
   ```powershell
   # Verify PHD2 is listening
   Test-NetConnection -ComputerName localhost -Port 4400
   ```

2. **Enable PHD2 Server Mode:**
   - Open PHD2 → Tools → Options → Advanced
   - Check "Enable Server" 
   - Ensure port is set to 4400
   - Restart PHD2

3. **Firewall Configuration:**
   ```powershell
   # Add firewall rule for PHD2 (run as Administrator)
   New-NetFirewallRule -DisplayName "PHD2 Server" -Direction Inbound -Port 4400 -Protocol TCP -Action Allow
   ```

4. **Check for Port Conflicts:**
   ```powershell
   # Check if port 4400 is in use
   netstat -an | findstr :4400
   ```

</details>

<details>
<summary><strong>⭐ Star Selection Issues</strong></summary>

#### Issue: Cannot find suitable guide stars

**Symptoms:**
- "No suitable guide star found" errors
- Stars detected but not selectable
- Guide star keeps getting lost

**Diagnostic Commands:**
```powershell
# Check current exposure and star mass
$info = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" -Method GET
Write-Host "Exposure: $($info.Response.Status.ExposureTime)s"
Write-Host "Star Mass: $($info.Response.Status.StarMass)"
Write-Host "SNR: $($info.Response.Status.SNR)"
```

**Solutions:**

1. **Optimize Exposure Time:**
   ```powershell
   # Try different exposure times
   foreach ($exp in @(1.0, 2.0, 3.0, 5.0)) {
       $setExp = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-exposure" `
           -Method POST -ContentType "application/json" `
           -Body "{`"seconds`": $exp}"
       
       Start-Sleep -Seconds 2
       $findStar = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/find-star" -Method POST
       
       if ($findStar.Success) {
           Write-Host "✅ Star found with ${exp}s exposure" -ForegroundColor Green
           break
       }
   }
   ```

2. **Use ROI for Specific Areas:**
   ```powershell
   # Target specific regions
   $roi = @(100, 100, 200, 200)  # x, y, width, height
   $findStar = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/find-star" `
       -Method POST -ContentType "application/json" `
       -Body "{ `"roi`": $($roi | ConvertTo-Json) }"
   ```

3. **Check Star Quality:**
   ```powershell
   # Monitor star quality metrics
   $info = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" -Method GET
   if ($info.Response.Status.StarMass -lt 100) {
       Write-Host "⚠️ Star mass too low: $($info.Response.Status.StarMass)" -ForegroundColor Yellow
       Write-Host "💡 Try increasing exposure time or adjusting focus" -ForegroundColor Cyan
   }
   if ($info.Response.Status.SNR -lt 10) {
       Write-Host "⚠️ SNR too low: $($info.Response.Status.SNR)" -ForegroundColor Yellow
       Write-Host "💡 Try increasing exposure time or adjusting gain" -ForegroundColor Cyan
   }
   ```

</details>

<details>
<summary><strong>🎯 Guiding Performance Issues</strong></summary>

#### Issue: Poor guiding performance or frequent star loss

**Symptoms:**
- High RMS error values
- Frequent "LostLock" state
- Erratic guiding corrections
- Mount oscillation

**Performance Monitoring:**
```powershell
function Monitor-GuidingPerformance {
    $samples = @()
    
    for ($i = 0; $i -lt 20; $i++) {
        $info = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" -Method GET
        
        if ($info.Response.Status.IsGuiding) {
            $sample = [PSCustomObject]@{
                Time = Get-Date
                RmsRa = $info.Response.GuideStats.RmsRa
                RmsDec = $info.Response.GuideStats.RmsDec
                RmsTotal = $info.Response.GuideStats.RmsTotal
                StarMass = $info.Response.Status.StarMass
                SNR = $info.Response.Status.SNR
            }
            $samples += $sample
            
            Write-Host "Sample $($i+1): RMS=$($sample.RmsTotal) StarMass=$($sample.StarMass) SNR=$($sample.SNR)"
        }
        
        Start-Sleep -Seconds 5
    }
    
    # Analyze performance
    $avgRms = ($samples | Measure-Object -Property RmsTotal -Average).Average
    $maxRms = ($samples | Measure-Object -Property RmsTotal -Maximum).Maximum
    $minSNR = ($samples | Measure-Object -Property SNR -Minimum).Minimum
    
    Write-Host "📊 Performance Analysis:" -ForegroundColor Cyan
    Write-Host "  Average RMS: $([math]::Round($avgRms, 2))" -ForegroundColor White
    Write-Host "  Maximum RMS: $([math]::Round($maxRms, 2))" -ForegroundColor White
    Write-Host "  Minimum SNR: $([math]::Round($minSNR, 2))" -ForegroundColor White
    
    if ($avgRms -gt 2.0) {
        Write-Host "⚠️ High average RMS - check mount balance and polar alignment" -ForegroundColor Yellow
    }
    if ($minSNR -lt 10) {
        Write-Host "⚠️ Low SNR detected - consider increasing exposure time" -ForegroundColor Yellow
    }
}

# Run performance analysis
Monitor-GuidingPerformance
```

**Optimization Solutions:**

1. **Adjust Algorithm Parameters:**
   ```powershell
   # Reduce aggressiveness for better stability
   $setAggression = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-algo-param" `
       -Method POST -ContentType "application/json" `
       -Body '{"axis": "ra", "name": "Aggression", "value": 60}'
   
   # Increase minimum move threshold to reduce noise
   $setMinMove = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-algo-param" `
       -Method POST -ContentType "application/json" `
       -Body '{"axis": "ra", "name": "MinMove", "value": 0.15}'
   ```

2. **Configure Variable Delay:**
   ```powershell
   # Use variable delay for mount settling
   $varDelay = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/set-variable-delay" `
       -Method POST -ContentType "application/json" `
       -Body '{"enabled": true, "shortDelaySeconds": 2, "longDelaySeconds": 8}'
   ```

</details>

<details>
<summary><strong>📡 API Communication Issues</strong></summary>

#### Issue: API timeouts or incomplete responses

**Symptoms:**
- Requests timeout frequently
- Partial or corrupted JSON responses
- Intermittent API failures

**Diagnostic Tools:**
```powershell
# Test API responsiveness
function Test-PHD2ApiHealth {
    $endpoints = @(
        "http://localhost:5000/api/phd2/all-info",
        "http://localhost:5000/api/phd2/get-connected",
        "http://localhost:5000/api/phd2/profiles"
    )
    
    foreach ($endpoint in $endpoints) {
        try {
            $start = Get-Date
            $response = Invoke-RestMethod -Uri $endpoint -Method GET -TimeoutSec 10
            $duration = (Get-Date) - $start
            
            if ($response.Success) {
                Write-Host "✅ $endpoint - ${duration}ms" -ForegroundColor Green
            } else {
                Write-Host "⚠️ $endpoint - Error: $($response.Error)" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "❌ $endpoint - Failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Run health check
Test-PHD2ApiHealth
```

**Solutions:**

1. **Increase Timeout Values:**
   ```powershell
   # Use longer timeouts for slow operations
   $response = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" `
       -Method GET -TimeoutSec 30
   ```

2. **Implement Retry Logic:**
   ```powershell
   function Invoke-PHD2ApiWithRetry {
       param($Uri, $Method = "GET", $Body = $null, $MaxRetries = 3)
       
       for ($i = 0; $i -lt $MaxRetries; $i++) {
           try {
               $params = @{ Uri = $Uri; Method = $Method; TimeoutSec = 15 }
               if ($Body) { $params.Body = $Body; $params.ContentType = "application/json" }
               
               $response = Invoke-RestMethod @params
               return $response
           } catch {
               Write-Host "Attempt $($i+1) failed: $($_.Exception.Message)" -ForegroundColor Yellow
               if ($i -eq $MaxRetries - 1) { throw }
               Start-Sleep -Seconds 2
           }
       }
   }
   ```

</details>

### Debug Information Collection

```powershell
# Comprehensive debug information collector
function Get-PHD2DebugInfo {
    $debugInfo = @{}
    
    Write-Host "🔍 Collecting PHD2 Debug Information..." -ForegroundColor Cyan
    
    try {
        # System information
        $debugInfo.System = @{
            OS = (Get-ComputerInfo).WindowsProductName
            PowerShell = $PSVersionTable.PSVersion.ToString()
            Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        }
        
        # Network connectivity
        $debugInfo.Network = @{
            PHD2Port = (Test-NetConnection -ComputerName localhost -Port 4400 -WarningAction SilentlyContinue).TcpTestSucceeded
            APIPort = (Test-NetConnection -ComputerName localhost -Port 5000 -WarningAction SilentlyContinue).TcpTestSucceeded
        }
        
        # PHD2 API status
        try {
            $allInfo = Invoke-RestMethod -Uri "http://localhost:5000/api/phd2/all-info" -Method GET -TimeoutSec 10
            $debugInfo.PHD2 = $allInfo.Response
        } catch {
            $debugInfo.PHD2Error = $_.Exception.Message
        }
        
        # API endpoints test
        $debugInfo.EndpointTests = @{}
        $testEndpoints = @(
            "/api/phd2/get-connected",
            "/api/phd2/profiles",
            "/api/phd2/get-pixel-scale"
        )
        
        foreach ($endpoint in $testEndpoints) {
            try {
                $test = Invoke-RestMethod -Uri "http://localhost:5000$endpoint" -Method GET -TimeoutSec 5
                $debugInfo.EndpointTests[$endpoint] = $test.Success
            } catch {
                $debugInfo.EndpointTests[$endpoint] = $false
            }
        }
        
    } catch {
        $debugInfo.CollectionError = $_.Exception.Message
    }
    
    # Output debug info
    $debugInfo | ConvertTo-Json -Depth 10 | Out-File -FilePath "PHD2_Debug_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
    Write-Host "✅ Debug information saved to PHD2_Debug_$(Get-Date -Format 'yyyyMMdd_HHmmss').json" -ForegroundColor Green
    
    return $debugInfo
}

# Run debug collection
$debugInfo = Get-PHD2DebugInfo
```

---

## 📞 Support & Community

For additional support and community discussions:

- **GitHub Issues**: Report bugs and request features
- **Documentation**: Latest API updates and examples  
- **Community Forums**: Share experiences and solutions

---

> **💡 Pro Tip**: Always monitor the PHD2 application state and guide star quality metrics for optimal performance. Use the comprehensive status monitoring examples provided to build robust automation scripts.

*Last updated: $(Get-Date -Format 'yyyy-MM-dd')*
