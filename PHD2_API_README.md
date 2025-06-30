# PHD2 Integration API Documentation

This document describes the PHD2 API endpoints added to the Touch 'N' Stars plugin.

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

### cURL Examples

```bash
# Connect to PHD2
curl -X POST http://localhost:5000/api/phd2/connect \
  -H "Content-Type: application/json" \
  -d '{"hostname": "localhost", "instance": 1}'

# Get all PHD2 information
curl http://localhost:5000/api/phd2/all-info

# Start guiding
curl -X POST http://localhost:5000/api/phd2/start-guiding \
  -H "Content-Type: application/json" \
  -d '{"settlePixels": 2.0, "settleTime": 10.0, "settleTimeout": 100.0}'
```

## Integration Notes

- The PHD2 service maintains a persistent connection to PHD2
- All operations are asynchronous and return immediately
- Use the status endpoint to monitor the current state
- Always check the `Success` field in responses
- The PHD2 service automatically handles reconnection attempts
- Multiple simultaneous requests are handled safely with internal locking

## Troubleshooting

1. **Connection Failed**: Ensure PHD2 is running and server mode is enabled
2. **Port Issues**: Check if PHD2 is using the default port 4400
3. **Equipment Connection Failed**: Verify the profile name exists in PHD2
4. **Guiding Won't Start**: Ensure equipment is connected and a star is selected in PHD2

For more detailed PHD2 API documentation, see: https://github.com/OpenPHDGuiding/phd2/wiki/EventMonitoring
