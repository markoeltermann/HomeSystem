
const addresses = {
    awaySetpoint: "OBJECT_ANALOG_VALUE:0",
    normalSetpoint: "OBJECT_ANALOG_VALUE:1",
    intensiveSetpoint: "OBJECT_ANALOG_VALUE:2",
    supplyTemperature: "OBJECT_ANALOG_VALUE:11",
    isHeating: "OBJECT_BINARY_VALUE:33",
    isCooling: "OBJECT_BINARY_VALUE:34",
    currentMode: "OBJECT_MULTI_STATE_VALUE:1",
};

const modes = {
    away: 2,
    normal: 3,
    intensive: 4,
};

var deltas = [];

var current = 0;

var isProcessing = false;

Timer.set(10000, true, function () {
    if (isProcessing) {
        return;
    }
    isProcessing = true;
    try {
        Shelly.call(
            "HTTP.GET", {
                url: "http://sinilille:5100/values?a=" + getValues(addresses).join('&a='),
                ssl_ca: "*",
                timeout: 2
            },
            processStep
        );
    } catch (e) {
        isProcessing = false;
    }
});

function processStep(httpResult) {
    try {
        if (!httpResult) {
            turnOff('ventilation api request failed');
            return;
        }
        if (httpResult.code !== 200) {
            turnOff('ventilation api returned error code: ' + httpResult.code);
            return;
        }
        const dict = {};
        const body = JSON.parse(httpResult.body);
        for (const element of body) {
            dict[element.address] = element.value;
        }

        print(dict[addresses.currentMode]);

        const isCooling = dict[addresses.isCooling] >= 1;

        if (!isCooling && dict[addresses.isHeating] < 1) {
            turnOff('not cooling, not heating');
            return;
        }
        
        var setPoint = getSetpoint(dict);
        const currentTemp = dict[addresses.supplyTemperature];
        if (!setPoint || !currentTemp) {
            turnOff('setpoint or current temp missing');
            return;
        }

        if (isCooling) {
            setPoint += 1;
        } else {
            setPoint -= 1;
        }

        var delta = currentTemp - setPoint;
        if (delta > 2) {
            delta = 2;
        } else if (delta < -2) {
            delta = -2;
        }
        deltas.push(delta);
        if (deltas.length > 7) {
            deltas.splice(0, 1);
        }

        const latestDelta = getMeanDelta(3, false);
        const fullDelta = getMeanDelta(6, false);
        const fullDeltaWithDiff = getMeanDelta(6, true);
        // print('VALVE: Latest delta ' + latestDelta);
        print('VALVE: Full delta ' + fullDelta + ', with diff ' + fullDeltaWithDiff + ', latest delta ' + latestDelta);

        // if (latestDelta <= 0.2 && latestDelta >= -0.2) {
        //     isProcessing = false;
        //     print('VALVE: Delta is under threshold');
        //     return;
        // }

        // if ((latestDelta > 0 && fullDelta < 0) || (latestDelta < 0 && fullDelta > 0)) {
        //     isProcessing = false;
        //     print('VALVE: Turnaround detected');
        //     return;
        // }

        const step = fullDeltaWithDiff / 30 * (current / 30 + 1);
        // const step = fullDelta * fullDelta * 5 / 60 + fullDelta * 1 / 6;
        // const sign = Math.sign(fullDelta);
        // const absFullDelta = Math.abs(fullDelta);
        // const step = absFullDelta / 6 * (absFullDelta / 2 + 1) * sign;
        current += isCooling ? step : -step;

        if (current < 0) {
            current = 0;
        } else if (current > 65) {
            current = 65;
        }

        print('VALVE: Current value updating to ' + current + ' by step ' + step);

        Shelly.call('Light.Set', { id: 0, on: true, brightness: Math.round(current) }, function(result, errorCode) {
            isProcessing = false;
            print('VALVE: Updated, error code ' + errorCode);
        });

    } catch (e) {
        turnOff('error occurred ' + e);
    }
}

function getMeanDelta(n, withDiffs) {
    const start = n > deltas.length ? 0 : deltas.length - n;

    var result = 0;

    for (let i = start; i < deltas.length; i++) {
        const current = deltas[i];
        result += current;
        if (withDiffs && i > 0) {
            const prev = deltas[i - 1];
            const diff = current - prev;
            result += diff * 9;
        }
    }

    return result;
}

function turnOff(reason) {
    try {
        isProcessing = false;
        deltas = [];
        current = 0;
        print("VALVE: Turning off. Reason: " + reason);
        Shelly.call('Light.Set', { id: 0, on: false, brightness: 0 });
    } catch (e) {

    }
}

function getSetpoint(dict) {
    const currentMode = +dict[addresses.currentMode];
    if (!currentMode) {
        return null;
    }

    var setPoint = null;

    if (currentMode === modes.away) {
        setPoint = dict[addresses.awaySetpoint];
    } else if (currentMode === modes.normal) {
        setPoint = dict[addresses.normalSetpoint];
    } else if (currentMode === modes.intensive) {
        setPoint = dict[addresses.intensiveSetpoint];
    }

    if (!setPoint) {
        return null;
    } else {
        return setPoint;
    }
}

function getValues(obj) {
    const values = [];

    for (const key of Object.keys(obj)) {
        values.push(obj[key]);
    }

    return values;
}