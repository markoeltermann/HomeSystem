The goal: ConsumptionCalculatorRunner should start calculating various electricity prices based on raw NPS price and raw grid price.

The consumption calculator should calculate the following points that belong to the consumption calculator price:

## grid-buy-price
- Description: The transmission price (EUR/kWh) at which electricity is bought from the grid.
- Source: device type = electricity_price, point type = grid-price-raw
- Calculation: the raw price multiplied by VAT that is found in ConfigModel (it can be injected). The VAT is a percentage, so the formula is: `grid-buy-price = grid-price-raw * (1 + VAT/100)`

## electricity-buy-price
- Description: The price (EUR/kWh) at which electricity is bought. This is only the electricity price excluding the transmission (grid) price
- Source: device type = electricity_price, point type = nps-price-raw
- Calculation: raw NPS price + ElectricitySaleMargin from config then multiplied by VAT.

## electricity-sell-price
- Description: The price (EUR/kWh) at which electricity is sold back to the grid.
- Source: device type = electricity_price, point type = nps-price-raw
- Calculation: raw NPS price + ElectricityPurchaseMargin from config then multiplied by VAT.

## total-buy-price
- Description: The total price (EUR/kWh) at which electricity is bought, including both the electricity price and the grid price.
- Calculation: `total-buy-price = grid-buy-price + electricity-buy-price`

The price calculation should be performed as a separate step in addition to the 2 steps already performed by the ConsumptionCalculatorRunner (calculating day-pv-energy and electricity cost). The price calculation should be performed at the beginning of the execution, before any of the other calculations.


## Modification of the existing cost calculation
The existing cost calculation should be modified to use the newly calculated `total-buy-price` and `electricity-sell-price` instead of the `total-buy-price` and `electricity-sell-price` from the `electricity_price` device, as is the current implementation.

Notes for implementation:
- The points API provide the data at 5 min intervals and the calculation is done on the same interval even though the actual pricing is at 15 minute intervals. The data points in between are filled in and this is already done beforehand. Use the parameter fiveMinResolution: true to make sure the filling is done.