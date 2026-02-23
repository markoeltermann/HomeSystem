# Consumption Calculator Specification

The `ConsumptionCalculatorRunner` service is a background task designed to calculate daily solar energy production based on the total cumulative energy reading from a PV inverter. It transforms a low-resolution, staircase-like cumulative metric into a smooth, interpolated daily progression.

## 1. Goal
Calculate the energy production of a single day (starting from 0 at the beginning of the day) by reading the `total-pv-energy` point from a Deye inverter. The resulting values are interpolated linearly between measurement changes to provide a smoother curve, and the final output is mapped to a `day-pv-energy` point.

## 2. Dependencies
- **Data Source**: `PointValueStoreAdapter` is used to fetch historical values with a 5-minute resolution for energy and solar elevation.
- **Context**: `HomeSystemContext` is used to locate the inverter and solar model devices and their specific data points.
- **Logic Base**: Inherits from `DeviceReader`.

## 3. Workflow Logic

### 3.1 Initialization and Setup
- The runner identifies the inverter device (`deye_inverter`) and the solar model device (`solar_model`).
- It locates `total-pv-energy` on the inverter and `solar-elevation` on the solar model.
- It identifies the target point `day-pv-energy` from the current device execution context.
- All calculations are performed relative to the current day in **local time**, though input/output timestamps remain in UTC.

### 3.2 Data Retrieval
- Fetches all values for both `total-pv-energy` and `solar-elevation` for the current local date using 5-minute resolution.
- If no values or no valid numeric values are found, the execution terminates.
- **Solar Elevation Threshold**: A constant (set to -1 degrees) is used to distinguish between day and night.
- **Base Value**: The first numeric value of the day where solar elevation is **above** the threshold is identified as the starting reference for the day.

### 3.3 Anchor Point Detection
- The algorithm scans values up to the current `timestamp`.
- It identifies **Anchor Points**: Indices where the energy value has changed compared to the previous measurement, **but only if the solar elevation is above the threshold**. The first point that meets the threshold criteria is automatically treated as an anchor point to serve as the day's starting baseline.
- This filtering ensures that all energy increments occurring during the night (when solar elevation is below the threshold) are discarded, preventing false production slopes.
- If no changes are detected yet (e.g., early morning), the progression is set to `0.00` for all timestamps up to now.

### 3.4 Processing and Interpolation
The day's timeline is processed in three segments:

1.  **Pre-Production**:
    - For all timestamps before the first anchor point (before the first increase in energy), the value is set to `0.00`.
2.  **Solar Production (Interpolated)**:
    - Between any two consecutive anchor points, values are **linearly interpolated**.
    - This smooths the "staircase" effect caused by low-resolution readings.
    - `InterpVal = StartVal + (EndVal - StartVal) * (CurrentTime - StartTime) / (EndTime - StartTime)`.
3.  **Recent Values (Flat-fill)**:
    - For timestamps after the **last detected change** up to the current `timestamp`, interpolation is not possible (as the next increase is unknown).
    - These values are filled with the **latest known value**.

## 4. Output
- Returns a list of `PointValue` objects for the `day-pv-energy` point.
- Each value is formatted to 2 decimal places (`0.00`).
- The `StorePointsWithReplace` property is set to `true`, ensuring the day's history is updated/overwritten in the store with the new interpolated curve.
