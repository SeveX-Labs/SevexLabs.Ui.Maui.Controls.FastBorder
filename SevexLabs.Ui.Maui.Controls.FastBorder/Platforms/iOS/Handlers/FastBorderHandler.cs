using Microsoft.Maui.Platform;
using ContentView = Microsoft.Maui.Platform.ContentView;

namespace SevexLabs.Ui.Maui.Controls.FastBorder.Handlers;

internal partial class FastBorderHandler
{
    private FastBorderContainerView? _fastBorderContainer;

    protected override ContentView CreatePlatformView()
    {
        _fastBorderContainer = new FastBorderContainerView();
        return _fastBorderContainer;
    }

    protected override void ConnectHandler(ContentView platformView)
    {
        base.ConnectHandler(platformView);
        UpdateAllProperties(forceLayout: true, forceIntrinsicSize: true);
    }

    protected override void DisconnectHandler(ContentView platformView)
    {
        _fastBorderContainer?.Reset();
        _fastBorderContainer = null;

        base.DisconnectHandler(platformView);
    }

    private void UpdateAllProperties(bool forceLayout = false, bool forceIntrinsicSize = false)
    {
        if (VirtualView is not FastBorder fastBorder || _fastBorderContainer is null)
        {
            return;
        }

        var result = _fastBorderContainer.Update(fastBorder);
        ApplyUpdateResult(_fastBorderContainer, result, forceLayout, forceIntrinsicSize);
    }

    private static void ApplyUpdateResult(
        FastBorderContainerView container,
        FastBorderContainerUpdateResult result,
        bool forceLayout = false,
        bool forceIntrinsicSize = false)
    {
        if (forceIntrinsicSize || result.HasFlag(FastBorderContainerUpdateResult.IntrinsicSize))
        {
            container.InvalidateIntrinsicContentSize();
        }

        if (forceLayout || result.HasFlag(FastBorderContainerUpdateResult.Layout))
        {
            container.SetNeedsLayout();
        }
    }

    public static partial void MapCornerRadius(FastBorderHandler handler, FastBorder view)
        => handler.UpdateAllProperties();

    public static partial void MapBorderThickness(FastBorderHandler handler, FastBorder view)
        => handler.UpdateAllProperties();

    public static partial void MapBorderColor(FastBorderHandler handler, FastBorder view)
        => handler.UpdateAllProperties();

    public static partial void MapBackground(FastBorderHandler handler, FastBorder view)
        => handler.UpdateAllProperties();

    public static partial void MapPadding(FastBorderHandler handler, FastBorder view)
        => handler.UpdateAllProperties();

    public static partial void MapShadow(FastBorderHandler handler, FastBorder view)
        => handler.UpdateAllProperties();

    public static partial void MapOverlayBorder(FastBorderHandler handler, FastBorder view)
    {
        if (handler._fastBorderContainer is null)
        {
            return;
        }

        var result = handler._fastBorderContainer.UpdateOverlayBorder(view);
        ApplyUpdateResult(handler._fastBorderContainer, result);
    }
}
