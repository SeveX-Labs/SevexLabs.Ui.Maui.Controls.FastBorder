using SevexLabs.Ui.Maui.Controls.FastBorder.Handlers;

namespace SevexLabs.Ui.Maui.Controls.FastBorder.Extensions;

public static class SevexLabsFastBorderBuilderExtensions
{
    public static MauiAppBuilder UseSevexLabsFastBorder(this MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(FastBorder), typeof(FastBorderHandler));
        });

        return builder;
    }
}
