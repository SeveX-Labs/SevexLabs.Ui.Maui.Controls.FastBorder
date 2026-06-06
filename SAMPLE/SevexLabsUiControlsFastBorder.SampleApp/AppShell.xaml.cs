namespace SevexLabsUiControlsFastBorder.SampleApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        RegisterRoutes();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute(nameof(FastBorderGalleryPage), typeof(FastBorderGalleryPage));
        Routing.RegisterRoute(nameof(FastBorderTests), typeof(FastBorderTests));
        Routing.RegisterRoute(nameof(BorderVsFastBorderProfilingTests), typeof(BorderVsFastBorderProfilingTests));
    }
}
