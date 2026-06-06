namespace SevexLabsUiControlsFastBorder.SampleApp;

public partial class BorderProfilingPage : ContentPage
{
    private readonly BorderProfilingOptions _options;
    private readonly long _navigationStartTimestamp;
    private readonly Func<BorderProfilingNavigationResult, Task>? _resultCallback;
    private readonly bool _autoPop;

    private BorderProfilingBuildResult? _buildResult;
    private double _appearingMs;
    private double _loadedMs;
    private double _sizeChangedMs;
    private double _afterLayoutMs;
    private bool _reported;

    public BorderProfilingPage()
        : this(new BorderProfilingOptions(), 0, null, false)
    {
    }

    internal BorderProfilingPage(
        BorderProfilingOptions options,
        long navigationStartTimestamp,
        Func<BorderProfilingNavigationResult, Task>? resultCallback,
        bool autoPop)
    {
        InitializeComponent();

        _options = options.Copy();
        _navigationStartTimestamp = navigationStartTimestamp;
        _resultCallback = resultCallback;
        _autoPop = autoPop;

        Loaded += OnLoaded;
        SizeChanged += OnPageSizeChanged;

        BuildContent();
        UpdateLabels();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _appearingMs = ElapsedFromNavigationStart();
        UpdateLabels();
    }

    private void BuildContent()
    {
        _buildResult = BorderProfilingSupport.BuildInto(
            ItemsHost,
            BorderProfilingTarget.Border,
            _options,
            isWarmUp: false);
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _loadedMs = ElapsedFromNavigationStart();
        UpdateLabels();
        _ = CompleteAfterLayoutAsync();
    }

    private async void OnPageSizeChanged(object? sender, EventArgs e)
    {
        if (_reported || Width <= 0 || Height <= 0)
            return;

        _sizeChangedMs = ElapsedFromNavigationStart();
        await CompleteAfterLayoutAsync();
    }

    private async Task CompleteAfterLayoutAsync()
    {
        if (_reported)
            return;

        await WaitForUiCycleAsync();
        if (_sizeChangedMs <= 0 && Width > 0 && Height > 0)
            _sizeChangedMs = ElapsedFromNavigationStart();

        _afterLayoutMs = ElapsedFromNavigationStart();
        _reported = true;
        UpdateLabels();

        if (_resultCallback is not null)
            await _resultCallback(CreateResult());

        if (_autoPop && Navigation.NavigationStack.Contains(this))
            await Navigation.PopAsync(animated: false);
    }

    private BorderProfilingNavigationResult CreateResult()
    {
        return new BorderProfilingNavigationResult
        {
            Target = BorderProfilingTarget.Border,
            ElementCount = _options.ElementCount,
            UseShadow = _options.UseShadow,
            CornerRadius = _options.CornerRadius,
            BorderThickness = _options.BorderThickness,
            Scenario = _options.Scenario,
            LayoutMode = _buildResult?.LayoutMode ?? _options.LayoutMode,
            BuildContainerMs = _buildResult?.BuildContainerMs ?? 0,
            BuildItemsMs = _buildResult?.BuildItemsMs ?? 0,
            AddToVisualTreeMs = _buildResult?.AddToVisualTreeMs ?? 0,
            BuildAndAddMs = _buildResult?.BuildAndAddMs ?? 0,
            AllocatedBytesDelta = _buildResult?.AllocatedBytesDelta ?? 0,
            Gen0CollectionsDelta = _buildResult?.Gen0CollectionsDelta ?? 0,
            Gen1CollectionsDelta = _buildResult?.Gen1CollectionsDelta ?? 0,
            Gen2CollectionsDelta = _buildResult?.Gen2CollectionsDelta ?? 0,
            AppearingMs = _appearingMs,
            LoadedMs = _loadedMs,
            SizeChangedMs = _sizeChangedMs,
            AfterLayoutMs = _afterLayoutMs
        };
    }

    private async Task WaitForUiCycleAsync()
    {
        await Task.Yield();

        var completion = new TaskCompletionSource<bool>();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () => completion.TrySetResult(true));
        await completion.Task;
    }

    private double ElapsedFromNavigationStart()
    {
        return _navigationStartTimestamp == 0
            ? 0
            : BorderProfilingSupport.TimestampToElapsedMilliseconds(_navigationStartTimestamp);
    }

    private void UpdateLabels()
    {
        var layoutMode = _buildResult?.LayoutMode ?? _options.LayoutMode;
        ConfigurationLabel.Text = $"N={_options.ElementCount}, scenario={BorderProfilingSupport.FormatScenario(_options.Scenario)}, layout={BorderProfilingSupport.FormatLayoutMode(layoutMode)}, shadow={(_options.UseShadow ? "on" : "off")}, radius={_options.CornerRadius:0}, thickness={_options.BorderThickness:0}";
        BuildLabel.Text = $"Build + add: {(_buildResult?.BuildAndAddMs ?? 0):0.0} ms";
        BuildContainerLabel.Text = $"Build container: {(_buildResult?.BuildContainerMs ?? 0):0.0} ms";
        BuildItemsLabel.Text = $"Build items: {(_buildResult?.BuildItemsMs ?? 0):0.0} ms";
        AddToVisualTreeLabel.Text = $"Add to visual tree: {(_buildResult?.AddToVisualTreeMs ?? 0):0.0} ms";
        MemoryLabel.Text = $"Alloc: {FormatBytes(_buildResult?.AllocatedBytesDelta ?? 0)}, GC gen0/gen1/gen2: {(_buildResult?.Gen0CollectionsDelta ?? 0)}/{(_buildResult?.Gen1CollectionsDelta ?? 0)}/{(_buildResult?.Gen2CollectionsDelta ?? 0)}";
        AppearingLabel.Text = $"Navigation -> OnAppearing: {_appearingMs:0.0} ms";
        LoadedLabel.Text = $"Navigation -> Loaded: {_loadedMs:0.0} ms";
        SizeChangedLabel.Text = $"Navigation -> first valid SizeChanged: {_sizeChangedMs:0.0} ms";
        AfterLayoutLabel.Text = $"Navigation -> delayed after layout: {_afterLayoutMs:0.0} ms";
    }

    private static string FormatBytes(long bytes)
    {
        const double kib = 1024d;
        const double mib = kib * 1024d;

        return Math.Abs(bytes) >= mib
            ? $"{bytes / mib:0.0} MiB"
            : $"{bytes / kib:0.0} KiB";
    }
}
