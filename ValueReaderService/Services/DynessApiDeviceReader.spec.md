# DynessApiDeviceReader Specification

## Purpose
Reads real-time point values from the Dyness Open API for a physical energy device (e.g. lithium battery, solar inverter).

## Device Address Fields (JSON in `device.Address`)
| Field          | Description                                  |
|----------------|----------------------------------------------|
| `DeviceId`     | Device serial number (`DeviceSn`) on the API |
| `ClientId`     | API client ID for authentication             |
| `ClientSecret` | API client secret for authentication         |

If any of these fields is null or empty, the reader returns `null` (no values).

## API Interaction
- Endpoint: `POST /v1/device/realTime/data` via `DynessClientFactory`
- Request body: `RequestOpenApiPointDto { DeviceSn = address.DeviceId }`
- Response: `ResponseResultListResponseOpenApiPointDto` with a `Data` list of `ResponseOpenApiPointDto`

## Timestamp Validation
- The response must contain a point with `PointId == "T"`.
- Its `PointValue` is a UTC timestamp string in the format `"yyyy-MM-dd HH:mm:ss"`.
- If the timestamp point is missing, unparseable, or its value is more than **6 minutes** older than the `timestamp` parameter (UTC), the reader returns `null`.

## Point Mapping
- Each `DevicePoint.Address` maps to the `PointId` of a `ResponseOpenApiPointDto`.
- The point value is taken from `ResponseOpenApiPointDto.PointValue`.
- If no matching API point is found, or its `PointValue` is null, that point is skipped.

## Value Conversion
| DataType  | Conversion                                      |
|-----------|-------------------------------------------------|
| `Boolean` | `"1"` → `"True"`, any other value → `"False"` |
| Other     | `PointValue` written as-is                      |
