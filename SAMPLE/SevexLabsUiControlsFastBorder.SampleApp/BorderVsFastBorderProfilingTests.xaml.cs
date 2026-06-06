using System.Diagnostics;

namespace SevexLabsUiControlsFastBorder.SampleApp;

public partial class BorderVsFastBorderProfilingTests : ContentPage
{
    private const int MeasuredIterations = 3;
    private const int MaxLogEntries = 24;

    private readonly List<string> _logEntries = new();
    private bool _isRunning;
    private BorderProfilingBuildResult? _latestBorderInline;
    private BorderProfilingBuildResult? _latestFastBorderInline;
    private BorderProfilingNavigationResult? _latestBorderNavigation;
    private BorderProfilingNavigationResult? _latestFastBorderNavigation;

    public BorderVsFastBorderProfilingTests()
    {
        InitializeComponent();
        InitializeConfiguration();
        UpdateConfigurationLabels();
        UpdateResults();
        AddLog("Ready. Run inline or navigation profiling to collect practical measurements.");
    }

    private void InitializeConfiguration()
    {
        foreach (var count in BorderProfilingSupport.ElementCounts)
            ElementCountPicker.Items.Add(count.ToString());

        ElementCountPicker.SelectedIndex = 2;

        foreach (var scenario in BorderProfilingSupport.Scenarios)
            ScenarioPicker.Items.Add(BorderProfilingSupport.FormatScenario(scenario));

        ScenarioPicker.SelectedIndex = Array.IndexOf(BorderProfilingSupport.Scenarios, BorderProfilingScenario.ComplexContent);

        foreach (var layoutMode in BorderProfilingSupport.LayoutModes)
            LayoutModePicker.Items.Add(BorderProfilingSupport.FormatLayoutMode(layoutMode));

        LayoutModePicker.SelectedIndex = Array.IndexOf(BorderProfilingSupport.LayoutModes, BorderProfilingLayoutMode.ScrollViewStack);

        TestModePicker.Items.Add("Border");
        TestModePicker.Items.Add("FastBorder");
        TestModePicker.Items.Add("Both");
        TestModePicker.SelectedIndex = 2;

        CornerRadiusSlider.Value = 12;
        BorderThicknessSlider.Value = 1;
    }

    private async void OnRunInlineComparisonClicked(object? sender, EventArgs e)
    {
        if (_isRunning)
            return;

        await RunInlineComparisonAsync();
    }

    private async void OnOpenBorderPageClicked(object? sender, EventArgs e)
    {
        await OpenProfilingPageAsync(BorderProfilingTarget.Border, autoPop: false);
    }

    private async void OnOpenFastBorderPageClicked(object? sender, EventArgs e)
    {
        await OpenProfilingPageAsync(BorderProfilingTarget.FastBorder, autoPop: false);
    }

    private async void OnRunNavigationComparisonClicked(object? sender, EventArgs e)
    {
        if (_isRunning)
            return;

        await RunNavigationComparisonAsync();
    }

    private void OnClearLogClicked(object? sender, EventArgs e)
    {
        _logEntries.Clear();
        LogLabel.Text = string.Empty;
    }

    private void OnConfigurationChanged(object? sender, EventArgs e)
    {
        UpdateConfigurationLabels();
        UpdateResults();
    }

    private async Task RunInlineComparisonAsync()
    {
        SetRunning(true);
        try
        {
            _latestBorderInline = null;
            _latestFastBorderInline = null;
            var options = ReadOptions();
            var targets = ReadTargets();

            AddLog($"Inline comparison started. N={options.ElementCount}, mode={ReadMode()}, scenario={BorderProfilingSupport.FormatScenario(options.Scenario)}, layout={BorderProfilingSupport.FormatLayoutMode(options.LayoutMode)}, shadow={(options.UseShadow ? "on" : "off")}.");

            foreach (var target in targets)
            {
                var warmUp = await RunInlineIterationAsync(target, options, isWarmUp: true);
                AddLog(BorderProfilingSupport.FormatBuildResult(warmUp));

                var measured = new List<BorderProfilingBuildResult>();
                for (int i = 0; i < MeasuredIterations; i++)
                {
                    var result = await RunInlineIterationAsync(target, options, isWarmUp: false);
                    measured.Add(result);
                    AddLog($"Run {i + 1}/{MeasuredIterations}: {BorderProfilingSupport.FormatBuildResult(result)}");
                    await Task.Yield();
                }

                var average = AverageBuildResults(target, options, measured);
                if (target == BorderProfilingTarget.Border)
                    _latestBorderInline = average;
                else
                    _latestFastBorderInline = average;
            }

            UpdateResults();
        }
        finally
        {
            SetRunning(false);
        }
    }

    private async Task<BorderProfilingBuildResult> RunInlineIterationAsync(
        BorderProfilingTarget target,
        BorderProfilingOptions options,
        bool isWarmUp)
    {
        var total = Stopwatch.StartNew();
        var result = BorderProfilingSupport.BuildInto(InlineHost, target, options, isWarmUp);
        await WaitForUiCycleAsync();
        total.Stop();
        result.AfterLayoutMs = total.Elapsed.TotalMilliseconds;
        return result;
    }

    private async Task RunNavigationComparisonAsync()
    {
        SetRunning(true);
        try
        {
            _latestBorderNavigation = null;
            _latestFastBorderNavigation = null;
            AddLog("Navigation comparison started.");
            await OpenProfilingPageAsync(BorderProfilingTarget.Border, autoPop: true);
            await WaitForUiCycleAsync();
            await OpenProfilingPageAsync(BorderProfilingTarget.FastBorder, autoPop: true);
            UpdateResults();
        }
        finally
        {
            SetRunning(false);
        }
    }

    private async Task OpenProfilingPageAsync(BorderProfilingTarget target, bool autoPop)
    {
        var options = ReadOptions();
        var completion = new TaskCompletionSource<bool>();
        var startTimestamp = Stopwatch.GetTimestamp();

        Task OnResult(BorderProfilingNavigationResult result)
        {
            if (result.Target == BorderProfilingTarget.Border)
                _latestBorderNavigation = result;
            else
                _latestFastBorderNavigation = result;

            AddLog(BorderProfilingSupport.FormatNavigationResult(result));
            UpdateResults();
            completion.TrySetResult(true);
            return Task.CompletedTask;
        }

        Page page = target == BorderProfilingTarget.Border
            ? new BorderProfilingPage(options, startTimestamp, OnResult, autoPop)
            : new FastBorderProfilingPage(options, startTimestamp, OnResult, autoPop);

        await Navigation.PushAsync(page);

        if (autoPop)
            await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private BorderProfilingOptions ReadOptions()
    {
        var selectedCount = ElementCountPicker.SelectedItem?.ToString();
        if (!int.TryParse(selectedCount, out var count))
            count = 250;

        var scenario = ReadScenario();
        var layoutMode = scenario == BorderProfilingScenario.CollectionViewBaseline
            ? BorderProfilingLayoutMode.CollectionView
            : ReadLayoutMode();

        return new BorderProfilingOptions
        {
            ElementCount = count,
            UseShadow = ShadowCheckBox.IsChecked,
            CornerRadius = Math.Round(CornerRadiusSlider.Value),
            BorderThickness = Math.Round(BorderThicknessSlider.Value),
            Scenario = scenario,
            LayoutMode = layoutMode
        };
    }

    private BorderProfilingScenario ReadScenario()
    {
        var index = ScenarioPicker.SelectedIndex;
        return index >= 0 && index < BorderProfilingSupport.Scenarios.Length
            ? BorderProfilingSupport.Scenarios[index]
            : BorderProfilingScenario.ComplexContent;
    }

    private BorderProfilingLayoutMode ReadLayoutMode()
    {
        var index = LayoutModePicker.SelectedIndex;
        return index >= 0 && index < BorderProfilingSupport.LayoutModes.Length
            ? BorderProfilingSupport.LayoutModes[index]
            : BorderProfilingLayoutMode.ScrollViewStack;
    }

    private BorderProfilingTestMode ReadMode()
    {
        return TestModePicker.SelectedIndex switch
        {
            0 => BorderProfilingTestMode.Border,
            1 => BorderProfilingTestMode.FastBorder,
            _ => BorderProfilingTestMode.Both
        };
    }

    private IReadOnlyList<BorderProfilingTarget> ReadTargets()
    {
        return ReadMode() switch
        {
            BorderProfilingTestMode.Border => new[] { BorderProfilingTarget.Border },
            BorderProfilingTestMode.FastBorder => new[] { BorderProfilingTarget.FastBorder },
            _ => new[] { BorderProfilingTarget.Border, BorderProfilingTarget.FastBorder }
        };
    }

    private static BorderProfilingBuildResult AverageBuildResults(
        BorderProfilingTarget target,
        BorderProfilingOptions options,
        IReadOnlyList<BorderProfilingBuildResult> measured)
    {
        return new BorderProfilingBuildResult
        {
            Target = target,
            ElementCount = options.ElementCount,
            UseShadow = options.UseShadow,
            CornerRadius = options.CornerRadius,
            BorderThickness = options.BorderThickness,
            Scenario = options.Scenario,
            LayoutMode = measured.Count > 0 ? measured[0].LayoutMode : options.LayoutMode,
            BuildContainerMs = measured.Average(r => r.BuildContainerMs),
            BuildItemsMs = measured.Average(r => r.BuildItemsMs),
            AddToVisualTreeMs = measured.Average(r => r.AddToVisualTreeMs),
            BuildAndAddMs = measured.Average(r => r.BuildAndAddMs),
            AfterLayoutMs = measured.Average(r => r.AfterLayoutMs),
            AllocatedBytesDelta = Convert.ToInt64(measured.Average(r => r.AllocatedBytesDelta)),
            Gen0CollectionsDelta = Convert.ToInt32(measured.Average(r => r.Gen0CollectionsDelta)),
            Gen1CollectionsDelta = Convert.ToInt32(measured.Average(r => r.Gen1CollectionsDelta)),
            Gen2CollectionsDelta = Convert.ToInt32(measured.Average(r => r.Gen2CollectionsDelta))
        };
    }

    private async Task WaitForUiCycleAsync()
    {
        await Task.Yield();

        var completion = new TaskCompletionSource<bool>();
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () => completion.TrySetResult(true));
        await completion.Task;
    }

    private void UpdateConfigurationLabels()
    {
        CornerRadiusValueLabel.Text = $"Corner radius: {Math.Round(CornerRadiusSlider.Value):0}";
        BorderThicknessValueLabel.Text = $"Border thickness: {Math.Round(BorderThicknessSlider.Value):0}";
    }

    private void UpdateResults()
    {
        var options = ReadOptions();
        ElementCountResultLabel.Text = $"Elements: {options.ElementCount} | Scenario: {BorderProfilingSupport.FormatScenario(options.Scenario)} | Layout: {BorderProfilingSupport.FormatLayoutMode(options.LayoutMode)} | Shadow: {(options.UseShadow ? "on" : "off")} | Radius: {options.CornerRadius:0} | Thickness: {options.BorderThickness:0}";

        BorderResultLabel.Text = _latestBorderInline is null
            ? "Border inline: no result yet"
            : $"Border inline avg: container {_latestBorderInline.BuildContainerMs:0.0} ms, items {_latestBorderInline.BuildItemsMs:0.0} ms, add {_latestBorderInline.AddToVisualTreeMs:0.0} ms, build+add {_latestBorderInline.BuildAndAddMs:0.0} ms, after layout {_latestBorderInline.AfterLayoutMs:0.0} ms, alloc {FormatBytes(_latestBorderInline.AllocatedBytesDelta)}, GC {_latestBorderInline.Gen0CollectionsDelta}/{_latestBorderInline.Gen1CollectionsDelta}/{_latestBorderInline.Gen2CollectionsDelta}";

        FastBorderResultLabel.Text = _latestFastBorderInline is null
            ? "FastBorder inline: no result yet"
            : $"FastBorder inline avg: container {_latestFastBorderInline.BuildContainerMs:0.0} ms, items {_latestFastBorderInline.BuildItemsMs:0.0} ms, add {_latestFastBorderInline.AddToVisualTreeMs:0.0} ms, build+add {_latestFastBorderInline.BuildAndAddMs:0.0} ms, after layout {_latestFastBorderInline.AfterLayoutMs:0.0} ms, alloc {FormatBytes(_latestFastBorderInline.AllocatedBytesDelta)}, GC {_latestFastBorderInline.Gen0CollectionsDelta}/{_latestFastBorderInline.Gen1CollectionsDelta}/{_latestFastBorderInline.Gen2CollectionsDelta}";

        DeltaResultLabel.Text = _latestBorderInline is not null && _latestFastBorderInline is not null
            ? $"Inline delta: build {BorderProfilingSupport.FormatDelta(_latestBorderInline.BuildAndAddMs, _latestFastBorderInline.BuildAndAddMs)} | add {BorderProfilingSupport.FormatDelta(_latestBorderInline.AddToVisualTreeMs, _latestFastBorderInline.AddToVisualTreeMs)} | after layout {BorderProfilingSupport.FormatDelta(_latestBorderInline.AfterLayoutMs, _latestFastBorderInline.AfterLayoutMs)}"
            : "Inline delta: run both targets";

        NavigationResultLabel.Text = FormatNavigationSummary();
    }

    private string FormatNavigationSummary()
    {
        if (_latestBorderNavigation is null && _latestFastBorderNavigation is null)
            return "Navigation: no result yet";

        var border = _latestBorderNavigation is null
            ? "Border navigation: no result"
            : $"Border navigation: build+add {_latestBorderNavigation.BuildAndAddMs:0.0} ms, after layout {_latestBorderNavigation.AfterLayoutMs:0.0} ms";

        var fast = _latestFastBorderNavigation is null
            ? "FastBorder navigation: no result"
            : $"FastBorder navigation: build+add {_latestFastBorderNavigation.BuildAndAddMs:0.0} ms, after layout {_latestFastBorderNavigation.AfterLayoutMs:0.0} ms";

        var delta = _latestBorderNavigation is not null && _latestFastBorderNavigation is not null
            ? $" | Delta: build {BorderProfilingSupport.FormatDelta(_latestBorderNavigation.BuildAndAddMs, _latestFastBorderNavigation.BuildAndAddMs)}, add {BorderProfilingSupport.FormatDelta(_latestBorderNavigation.AddToVisualTreeMs, _latestFastBorderNavigation.AddToVisualTreeMs)}, after layout {BorderProfilingSupport.FormatDelta(_latestBorderNavigation.AfterLayoutMs, _latestFastBorderNavigation.AfterLayoutMs)}"
            : string.Empty;

        return $"{border} | {fast}{delta}";
    }

    private static string FormatBytes(long bytes)
    {
        const double kib = 1024d;
        const double mib = kib * 1024d;

        return Math.Abs(bytes) >= mib
            ? $"{bytes / mib:0.0} MiB"
            : $"{bytes / kib:0.0} KiB";
    }

    private void AddLog(string entry)
    {
        _logEntries.Insert(0, $"{DateTime.Now:HH:mm:ss}  {entry}");
        if (_logEntries.Count > MaxLogEntries)
            _logEntries.RemoveAt(_logEntries.Count - 1);

        LogLabel.Text = string.Join(Environment.NewLine, _logEntries);
    }

    private void SetRunning(bool isRunning)
    {
        _isRunning = isRunning;
        RunInlineButton.IsEnabled = !isRunning;
        RunNavigationButton.IsEnabled = !isRunning;
    }
}
