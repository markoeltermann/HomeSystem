# Consumption Calculator Specification

The `ConsumptionCalculatorRunner` service is a background task designed to calculate daily solar energy production based on PV input power readings from a PV inverter. It transforms real-time power metrics (Watts) into a daily energy progression (kWh) through integration.

## 1. Goal
Calculate the energy production of a single day (starting from 0 at the beginning of the day) by integrating the `pv-input-power` point from a Deye inverter. The resulting progression is mapped to a `day-pv-energy` point.

## 2. Dependencies
- **Data Source**: `PointValueStoreAdapter` is used to fetch historical values with a 5-minute resolution for power and solar elevation.
- **Context**: `HomeSystemContext` is used to locate the inverter and solar model devices and their specific data points.
- **Logic Base**: Inherits from `DeviceReader`.

## 3. Workflow Logic

### 3.1 Initialization and Setup
- The runner identifies the inverter device (`deye_inverter`) and the solar model device (`solar_model`).
- It locates `pv-input-power` on the inverter and `solar-elevation` on the solar model.
- It identifies the target point `day-pv-energy` from the current device execution context.
- All calculations are performed relative to the current day in **local time**, though input/output timestamps remain in UTC.

### 3.2 Data Retrieval
- Fetches all values for both `pv-input-power` and `solar-elevation` for the current local date using 5-minute resolution.
- If no values or no valid numeric values are found, the execution terminates.
- **Solar Elevation Threshold**: A constant (e.g., -1.0 degrees) is used to distinguish between day and night.

### 3.3 Power Integration
- The algorithm iterates through the 5-minute intervals for the power readings.
- **Integration Assumption**: Each power reading (in Watts) reflects the constant power level for the 5-minute period *ending* at that reading's timestamp.
- **Unit Conversion**: Watts integrated over 5 minutes are converted to kWh: `Energy_kWh = (Power_Watts * 5) / (60 * 1000) = Power_Watts / 12000`.
- **Night-time filtering**: If the solar elevation at the timestamp is below the threshold, the energy contribution for that interval is treated as 0.
- The `day-pv-energy` value at any timestamp is the sum of all energy contributions from the start of the day up to that timestamp.

## 4. Output
- Returns a list of `PointValue` objects for the `day-pv-energy` point.
- Each value is formatted to 2 decimal places (`0.00`).
- The `StorePointsWithReplace` property is set to `true`, ensuring the day's history is updated/overwritten in the store with the newly calculated progression.

## 5. Electricity Cost Calculation
Calculates daily and monthly electricity costs (or income) based on consumption and production data.

### 5.1 Dependencies
- **Data Source**: `estfeed` device providing `15-min-consumption` and `15-min-production` (kWh).
- **Pricing Data**: `electricity_price` device providing `total-buy-price` and `total-sell-price` (EUR/kWh).
- **Target Points**: `day-electricity-cost` and `month-electricity-cost` on the current device.

### 5.2 Workflow Logic
- **Time Range**: Calculates starting from the 1st of the current month. If the current day is 10 or less, it calculates from the 1st of the previous month.
- **Resolution**: Both input and output points use 5-minute resolution.
- **Consumption/Production Modeling**: Even though source points represent 15-minute windows, they are provided at 5-minute intervals (filled in). The consumption/production is assumed to be evenly distributed within the 15-minute period, so each 5-minute interval accounts for 1/3 of the 15-minute value.
- **Cost Calculation**: `IntervalCost = (IntervalConsumption * BuyPrice) - (IntervalProduction * SellPrice)`.
- **Accumulation**:
  - `day-electricity-cost` resets to 0 at the start of each day.
  - `month-electricity-cost` resets to 0 at the start of each month.
- **Output**: Returns cumulative cost time series formatted to 2 decimal places.
