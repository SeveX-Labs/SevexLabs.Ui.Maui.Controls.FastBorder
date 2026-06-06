namespace SevexLabsUiControlsFastBorder.SampleApp;

public partial class FastBorderGalleryPage : ContentPage
{
    public FastBorderGalleryPage()
    {
        InitializeComponent();
    }

    private async void OnOpenProfilingClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(BorderVsFastBorderProfilingTests));
    }
}
