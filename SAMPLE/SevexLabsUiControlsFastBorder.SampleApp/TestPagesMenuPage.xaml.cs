namespace SevexLabsUiControlsFastBorder.SampleApp;

public partial class TestPagesMenuPage : ContentPage
{
    public TestPagesMenuPage()
    {
        InitializeComponent();
    }

    private async void OnFastBorderGalleryClicked(object? sender, EventArgs e)
    {
        await GoToPage(nameof(FastBorderGalleryPage));
    }

    private async void OnFastBorderTestsClicked(object? sender, EventArgs e)
    {
        await GoToPage(nameof(FastBorderTests));
    }

    private async void OnBorderVsFastBorderProfilingTestsClicked(object? sender, EventArgs e)
    {
        await GoToPage(nameof(BorderVsFastBorderProfilingTests));
    }

    private static async Task GoToPage(string route)
    {
        await Shell.Current.GoToAsync(route);
    }
}
