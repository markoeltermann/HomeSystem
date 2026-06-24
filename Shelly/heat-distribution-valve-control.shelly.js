var deltas = [];
var current = 0;
var isProcessing = false;
var modeSettings = {
    mode: 'fixed',
    setpoint: 23,
    fixedValue: 0
};

HTTPServer.registerEndpoint('mode', function (request, response) {
    response.headers = [['Content-Type', 'application/json']];

    if (request.method === 'GET') {
        response.code = 200;
        response.body = JSON.stringify(modeSettings);
        response.send();
        return;
    }

    if (request.method !== 'POST') {
        response.code = 405;
        response.body = JSON.stringify({ error: 'method not allowed' });
        response.send();
        return;
    }

    try {
        var payload = JSON.parse(request.body);
        if (!Object.prototype.hasOwnProperty.call(payload, 'mode') ||
            !Object.prototype.hasOwnProperty.call(payload, 'setpoint') ||
            !Object.prototype.hasOwnProperty.call(payload, 'fixedValue')) {
            response.code = 400;
            response.body = JSON.stringify({ error: 'payload must include mode, setpoint and fixedValue' });
            response.send();
            return;
        }
        if (payload.mode !== 'fixed' && payload.mode !== 'setpoint') {
            response.code = 400;
            response.body = JSON.stringify({ error: "mode must be 'fixed' or 'setpoint'" });
            response.send();
            return;
        }
        if (typeof payload.setpoint !== 'number' || !isFinite(payload.setpoint)) {
            response.code = 400;
            response.body = JSON.stringify({ error: 'setpoint must be a number' });
            response.send();
            return;
        }
        if (typeof payload.fixedValue !== 'number' || !isFinite(payload.fixedValue)) {
            response.code = 400;
            response.body = JSON.stringify({ error: 'fixedValue must be a number' });
            response.send();
            return;
        }
        modeSettings = {
            mode: payload.mode,
            setpoint: payload.setpoint,
            fixedValue: payload.fixedValue
        };
        response.code = 200;
        response.body = JSON.stringify(modeSettings);
        response.send();
    } catch (e) {
        response.code = 400;
        response.body = JSON.stringify({ error: 'invalid JSON payload' });
        response.send();
    }
});

Timer.set(10000, true, function () {
    if (isProcessing) {
        return;
    }
    isProcessing = true;
    processStep();
});

function processStep() {
    try {
        if (modeSettings.mode === 'fixed') {
            current = modeSettings.fixedValue;
            if (deltas.length > 0) {
                deltas = [];
            }
            Shelly.call('Light.Set', { id: 0, on: true, brightness: Math.round(modeSettings.fixedValue) }, function (result, errorCode) {
                isProcessing = false;
                print('VALVE: Updated, error code ' + errorCode);
            });
        } else {
            var setPoint = modeSettings.setpoint;
            let temp = Shelly.getComponentStatus("temperature:101");
            const currentTemp = Shelly.getComponentStatus("temperature:101").tC;
            if (!setPoint || !currentTemp) {
                turnOff('setpoint or current temp missing');
                return;
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
            const diff = getMeanDelta(6, true);
            print('VALVE: Full delta ' + fullDelta + ', with diff ' + diff + ', latest delta ' + latestDelta);

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

            const step = ((fullDelta * fullDelta / 3 * Math.sign(fullDelta) + fullDelta * 2 / 3) + diff) / 20 * (current / 20 + 1);
            // const step = fullDelta * fullDelta * 5 / 60 + fullDelta * 1 / 6;
            // const sign = Math.sign(fullDelta);
            // const absFullDelta = Math.abs(fullDelta);
            // const step = absFullDelta / 6 * (absFullDelta / 2 + 1) * sign;
            current += step;

            if (current < 0) {
                current = 0;
            } else if (current > 50) {
                current = 50;
            }

            print('VALVE: Current value updating to ' + current + ' by step ' + step);

            Shelly.call('Light.Set', { id: 0, on: true, brightness: Math.round(current) }, function (result, errorCode) {
                isProcessing = false;
                print('VALVE: Updated, error code ' + errorCode);
            });
        }

    } catch (e) {
        turnOff('error occurred ' + e);
    }
}

function getMeanDelta(n, diffs) {
    const start = n > deltas.length ? 0 : deltas.length - n;

    var result = 0;

    for (let i = start; i < deltas.length; i++) {
        const current = deltas[i];
        if (diffs) {
            if (i > 0) {
                const prev = deltas[i - 1];
                const diff = current - prev;
                result += diff * 9;
            }
        } else {
            result += current;
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