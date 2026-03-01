# EstfeedReader

The `EstfeedReader` is a `DeviceReader` implementation that fetches electricity metering data from the Elering Estfeed API.

## Functionality

- Authenticates using ClientId and ClientSecret stored in the `Device.Address` (JSON serialized `DeviceAddress`).
- Fetches 15-minute interval metering data for a specific `MeteringPointEic` (stored as `DeviceId` in `DeviceAddress`).
- Processes data for the current day (based on local time of the execution timestamp).

## Requirements

- The API response must contain exactly one metering point and no errors.
- The reader requires 5 specific `DevicePoint` types to be defined for the device:
  1. `15-min-consumption`: Raw consumption in kWh for the 15-minute window.
  2. `15-min-production`: Raw production in kWh for the 15-minute window.
  3. `consumption-power`: Power in W, calculated from consumption (kWh * 4000).
  4. `production-power`: Power in W, calculated from production (kWh * 4000).
  5. `net-power`: Calculated as `consumption-power - production-power`.

## Data Conversion

- **Internal interval**: API returns 15-minute intervals.
- **Power calculation**: Power is assumed constant over the 15-minute interval. kWh values are multiplied by 4000 to get W.
- **Resolution**: All points are generated at 5-minute intervals by repeating the 15-minute values 3 times for each 15-minute window.
- **Null handling**: If the API returns `null` for consumption or production in an interval, the corresponding point values (including derived power values) will also be `null`.
- **Time Zones**: Input timestamps from Estfeed (with offset) are converted to UTC for storage.

## Error Handling

- Throws `DeviceRunException` if any of the 5 required point types are missing from the `devicePoints` collection.
- Returns `null` (skipping processing) if the device address is invalid or if the API response is empty/invalid.