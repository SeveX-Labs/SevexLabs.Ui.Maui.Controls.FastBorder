using Microsoft.Maui.Handlers;

namespace SevexLabs.Ui.Maui.Controls.FastBorder.Handlers;

internal partial class FastBorderHandler : ContentViewHandler
{
    public static readonly IPropertyMapper<FastBorder, FastBorderHandler> FastBorderMapper =
        new PropertyMapper<FastBorder, FastBorderHandler>(ContentViewHandler.Mapper)
        {
            [nameof(FastBorder.CornerRadius)] = MapCornerRadius,
            [nameof(FastBorder.BorderThickness)] = MapBorderThickness,
            [nameof(FastBorder.BorderColor)] = MapBorderColor,
            [nameof(FastBorder.Background)] = MapBackground,
            [nameof(FastBorder.Padding)] = MapPadding,
            [nameof(FastBorder.Shadow)] = MapShadow,
            [nameof(FastBorder.IsOverlayBorderVisible)] = MapOverlayBorder,
            [nameof(FastBorder.OverlayBorderColor)] = MapOverlayBorder,
            [nameof(FastBorder.OverlayBorderThickness)] = MapOverlayBorder
        };

    public FastBorderHandler() : base(FastBorderMapper)
    {
    }

    public static partial void MapCornerRadius(FastBorderHandler handler, FastBorder view);
    public static partial void MapBorderThickness(FastBorderHandler handler, FastBorder view);
    public static partial void MapBorderColor(FastBorderHandler handler, FastBorder view);
    public static partial void MapBackground(FastBorderHandler handler, FastBorder view);
    public static partial void MapPadding(FastBorderHandler handler, FastBorder view);
    public static partial void MapShadow(FastBorderHandler handler, FastBorder view);
    public static partial void MapOverlayBorder(FastBorderHandler handler, FastBorder view);
}
