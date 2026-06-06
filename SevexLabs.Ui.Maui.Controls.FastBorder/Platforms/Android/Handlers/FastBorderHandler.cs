using Microsoft.Maui.Platform;

namespace SevexLabs.Ui.Maui.Controls.FastBorder.Handlers;

internal partial class FastBorderHandler
{
    protected override ContentViewGroup CreatePlatformView()
    {
        var viewGroup = new FastBorderNativeView(Context!);
        viewGroup.SetClipChildren(false);
        viewGroup.SetClipToPadding(false);
        return viewGroup;
    }

    protected override void ConnectHandler(ContentViewGroup platformView)
    {
        base.ConnectHandler(platformView);
        UpdateAllProperties(forceLayout: true);
    }

    protected override void DisconnectHandler(ContentViewGroup platformView)
    {
        if (platformView is FastBorderNativeView nativeView)
        {
            nativeView.Reset();
        }

        base.DisconnectHandler(platformView);
    }

    private void UpdateAllProperties(bool forceLayout = false)
    {
        if (VirtualView is not FastBorder fastBorder ||
            PlatformView is not FastBorderNativeView nativeView)
        {
            return;
        }

        var result = nativeView.Update(fastBorder);

        ApplyUpdateResult(nativeView, result, forceLayout);
    }

    private static void ApplyUpdateResult(
        FastBorderNativeView nativeView,
        FastBorderNativeUpdateResult result,
        bool forceLayout = false)
    {
        if (forceLayout || result.HasFlag(FastBorderNativeUpdateResult.Layout))
        {
            nativeView.RequestLayout();
        }

        if (forceLayout || result != FastBorderNativeUpdateResult.None)
        {
            nativeView.Invalidate();
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
        if (handler.PlatformView is not FastBorderNativeView nativeView)
        {
            return;
        }

        var result = nativeView.UpdateOverlayBorder(view);
        ApplyUpdateResult(nativeView, result);
    }
}
