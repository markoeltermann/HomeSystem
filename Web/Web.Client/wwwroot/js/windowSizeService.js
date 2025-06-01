// windowSizeService.js
window.windowSizeService = {
    // Get current window dimensions
    getCurrentSize: function () {
        return {
            width: window.innerWidth,
            height: window.innerHeight
        };
    },

    // Register a .NET callback for window resize events
    registerResizeCallback: function (dotNetReference, callbackMethod) {
        // Store the reference to remove the listener later if needed
        if (!window.resizeCallbacks) {
            window.resizeCallbacks = [];
        }

        const callback = () => {
            dotNetReference.invokeMethodAsync(callbackMethod, {
                width: window.innerWidth,
                height: window.innerHeight
            });
        };

        window.resizeCallbacks.push({
            dotNetReference: dotNetReference,
            callback: callback
        });

        window.addEventListener('resize', callback);
        
        // Return current size on registration
        return this.getCurrentSize();
    },

    // Remove the resize event listener
    unregisterResizeCallback: function (dotNetReference) {
        if (window.resizeCallbacks) {
            const index = window.resizeCallbacks.findIndex(rc => rc.dotNetReference === dotNetReference);
            if (index !== -1) {
                const handler = window.resizeCallbacks[index];
                window.removeEventListener('resize', handler.callback);
                window.resizeCallbacks.splice(index, 1);
                return true;
            }
        }
        return false;
    }
};