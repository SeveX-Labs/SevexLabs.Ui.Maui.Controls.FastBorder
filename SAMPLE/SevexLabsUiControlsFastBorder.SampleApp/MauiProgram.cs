using Microsoft.Extensions.Logging;
using SevexLabs.Ui.Maui.Controls.FastBorder.Extensions;

namespace SevexLabsUiControlsFastBorder.SampleApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSevexLabsFastBorder()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("icomoon.ttf", "CustomIcon");
                    fonts.AddFont("Montserrat-Black.ttf", "MontserratBlack");
                    fonts.AddFont("Montserrat-Bold.ttf", "MontserratBold");
                    fonts.AddFont("Montserrat-ExtraBold.ttf", "MontserratExtraBold");
                    fonts.AddFont("Montserrat-ExtraLight.ttf", "MontserratExtraLight");
                    fonts.AddFont("Montserrat-Light.ttf", "MontserratLight");
                    fonts.AddFont("Montserrat-Medium.ttf", "MontserratMedium");
                    fonts.AddFont("Montserrat-Regular.ttf", "MontserratRegular");
                    fonts.AddFont("Montserrat-SemiBold.ttf", "MontserratSemiBold");
                    fonts.AddFont("Montserrat-Thin.ttf", "MontserratThin");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
