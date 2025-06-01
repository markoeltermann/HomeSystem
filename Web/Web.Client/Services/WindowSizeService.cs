using Microsoft.JSInterop;
using Web.Client.Models;

namespace Web.Client.Services;

/// <summary>
/// Service to handle window size detection and resize events
/// </summary>
public class WindowSizeService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    private DotNetObjectReference<WindowSizeService>? _dotNetRef;
    private WindowSize _currentSize = new();

    /// <summary>
    /// Event that fires when the window size changes
    /// </summary>
    public event Action<WindowSize>? OnResize;

    /// <summary>
    /// Current window size
    /// </summary>
    public WindowSize CurrentSize => _currentSize;

    /// <summary>
    /// Initializes the window size service and starts listening for resize events
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        if (_dotNetRef == null)
        {
            _dotNetRef = DotNetObjectReference.Create(this);

            var size = await _jsRuntime.InvokeAsync<WindowSize>(
                "windowSizeService.registerResizeCallback",
                _dotNetRef,
                "OnBrowserResize");

            _currentSize = size;
        }
    }

    /// <summary>
    /// Gets the current window size
    /// </summary>
    public async ValueTask<WindowSize> GetWindowSizeAsync()
    {
        return await _jsRuntime.InvokeAsync<WindowSize>("windowSizeService.getCurrentSize");
    }

    /// <summary>
    /// Callback method invoked from JavaScript when the window is resized
    /// </summary>
    [JSInvokable]
    public void OnBrowserResize(WindowSize size)
    {
        _currentSize = size;
        OnResize?.Invoke(size);
    }

    /// <summary>
    /// Dispose of the service and remove the JavaScript event listener
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef != null)
        {
            await _jsRuntime.InvokeVoidAsync("windowSizeService.unregisterResizeCallback", _dotNetRef);
            _dotNetRef.Dispose();
            _dotNetRef = null;
        }
    }
}