using System.Diagnostics;
using Microsoft.Maui.Controls.Shapes;
using SevexLabs.Ui.Maui.Controls.FastBorder;

namespace SevexLabsUiControlsFastBorder.SampleApp;

internal enum BorderProfilingTarget
{
    Border,
    FastBorder
}

internal enum BorderProfilingTestMode
{
    Border,
    FastBorder,
    Both
}

internal enum BorderProfilingScenario
{
    EmptyBorder,
    BorderWithLabel,
    ComplexContent,
    ContainerOnlyBaseline,
    CollectionViewBaseline
}

internal enum BorderProfilingLayoutMode
{
    ScrollViewStack,
    CollectionView
}

internal sealed class BorderProfilingOptions
{
    public int ElementCount { get; init; } = 250;

    public bool UseShadow { get; init; }

    public double CornerRadius { get; init; } = 12d;

    public double BorderThickness { get; init; } = 1d;

    public BorderProfilingScenario Scenario { get; init; } = BorderProfilingScenario.ComplexContent;

    public BorderProfilingLayoutMode LayoutMode { get; init; } = BorderProfilingLayoutMode.ScrollViewStack;

    public BorderProfilingOptions Copy() => new()
    {
        ElementCount = ElementCount,
        UseShadow = UseShadow,
        CornerRadius = CornerRadius,
        BorderThickness = BorderThickness,
        Scenario = Scenario,
        LayoutMode = LayoutMode
    };
}

internal sealed class BorderProfilingBuildResult
{
    public BorderProfilingTarget Target { get; init; }

    public int ElementCount { get; init; }

    public bool UseShadow { get; init; }

    public double CornerRadius { get; init; }

    public double BorderThickness { get; init; }

    public BorderProfilingScenario Scenario { get; init; }

    public BorderProfilingLayoutMode LayoutMode { get; init; }

    public double BuildContainerMs { get; init; }

    public double BuildItemsMs { get; init; }

    public double AddToVisualTreeMs { get; init; }

    public double BuildAndAddMs { get; init; }

    public double AfterLayoutMs { get; set; }

    public long AllocatedBytesDelta { get; init; }

    public int Gen0CollectionsDelta { get; init; }

    public int Gen1CollectionsDelta { get; init; }

    public int Gen2CollectionsDelta { get; init; }

    public bool IsWarmUp { get; init; }
}

internal sealed class BorderProfilingNavigationResult
{
    public BorderProfilingTarget Target { get; init; }

    public int ElementCount { get; init; }

    public bool UseShadow { get; init; }

    public double CornerRadius { get; init; }

    public double BorderThickness { get; init; }

    public BorderProfilingScenario Scenario { get; init; }

    public BorderProfilingLayoutMode LayoutMode { get; init; }

    public double BuildContainerMs { get; init; }

    public double BuildItemsMs { get; init; }

    public double AddToVisualTreeMs { get; init; }

    public double BuildAndAddMs { get; init; }

    public long AllocatedBytesDelta { get; init; }

    public int Gen0CollectionsDelta { get; init; }

    public int Gen1CollectionsDelta { get; init; }

    public int Gen2CollectionsDelta { get; init; }

    public double AppearingMs { get; init; }

    public double LoadedMs { get; init; }

    public double SizeChangedMs { get; init; }

    public double AfterLayoutMs { get; init; }
}

internal static class BorderProfilingSupport
{
    public static readonly int[] ElementCounts = { 50, 100, 250, 500 };

    public static readonly BorderProfilingScenario[] Scenarios =
    {
        BorderProfilingScenario.EmptyBorder,
        BorderProfilingScenario.BorderWithLabel,
        BorderProfilingScenario.ComplexContent,
        BorderProfilingScenario.ContainerOnlyBaseline,
        BorderProfilingScenario.CollectionViewBaseline
    };

    public static readonly BorderProfilingLayoutMode[] LayoutModes =
    {
        BorderProfilingLayoutMode.ScrollViewStack,
        BorderProfilingLayoutMode.CollectionView
    };

    public static BorderProfilingBuildResult BuildInto(
        ContentView host,
        BorderProfilingTarget target,
        BorderProfilingOptions options,
        bool isWarmUp)
    {
        host.Content = null;

        var allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);

        var totalStopwatch = Stopwatch.StartNew();

        var containerStopwatch = Stopwatch.StartNew();
        var layoutMode = GetEffectiveLayoutMode(options);
        var container = CreateMeasuredContainer(target, options, layoutMode);
        containerStopwatch.Stop();

        var itemsStopwatch = Stopwatch.StartNew();
        var items = CreateMeasuredItems(target, options, layoutMode);
        itemsStopwatch.Stop();

        var addStopwatch = Stopwatch.StartNew();
        AddItemsToContainer(container, items);
        host.Content = container.Root;
        addStopwatch.Stop();

        totalStopwatch.Stop();

        return new BorderProfilingBuildResult
        {
            Target = target,
            ElementCount = options.ElementCount,
            UseShadow = options.UseShadow,
            CornerRadius = options.CornerRadius,
            BorderThickness = options.BorderThickness,
            Scenario = options.Scenario,
            LayoutMode = layoutMode,
            BuildContainerMs = containerStopwatch.Elapsed.TotalMilliseconds,
            BuildItemsMs = itemsStopwatch.Elapsed.TotalMilliseconds,
            AddToVisualTreeMs = addStopwatch.Elapsed.TotalMilliseconds,
            BuildAndAddMs = totalStopwatch.Elapsed.TotalMilliseconds,
            AllocatedBytesDelta = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesBefore,
            Gen0CollectionsDelta = GC.CollectionCount(0) - gen0Before,
            Gen1CollectionsDelta = GC.CollectionCount(1) - gen1Before,
            Gen2CollectionsDelta = GC.CollectionCount(2) - gen2Before,
            IsWarmUp = isWarmUp
        };
    }

    public static View CreateItem(BorderProfilingTarget target, BorderProfilingOptions options, int index)
    {
        return CreateScenarioItem(target, options, index);
    }

    public static string FormatTarget(BorderProfilingTarget target) =>
        target == BorderProfilingTarget.Border ? "Border" : "FastBorder";

    public static string FormatScenario(BorderProfilingScenario scenario) =>
        scenario switch
        {
            BorderProfilingScenario.EmptyBorder => "Empty border",
            BorderProfilingScenario.BorderWithLabel => "Border with Label",
            BorderProfilingScenario.ComplexContent => "Complex content",
            BorderProfilingScenario.ContainerOnlyBaseline => "Container-only baseline",
            BorderProfilingScenario.CollectionViewBaseline => "CollectionView baseline",
            _ => scenario.ToString()
        };

    public static string FormatLayoutMode(BorderProfilingLayoutMode layoutMode) =>
        layoutMode switch
        {
            BorderProfilingLayoutMode.ScrollViewStack => "ScrollView + VerticalStackLayout",
            BorderProfilingLayoutMode.CollectionView => "CollectionView",
            _ => layoutMode.ToString()
        };

    public static string FormatDelta(double borderMs, double fastBorderMs)
    {
        var delta = borderMs - fastBorderMs;
        var percent = borderMs <= 0 ? 0 : (delta / borderMs) * 100d;
        return $"{delta:+0.0;-0.0;0.0} ms ({percent:+0.0;-0.0;0.0}%)";
    }

    public static string FormatBuildResult(BorderProfilingBuildResult result)
    {
        var warmUp = result.IsWarmUp ? " warm-up" : string.Empty;
        return $"{FormatTarget(result.Target)}{warmUp}: build+add {result.BuildAndAddMs:0.0} ms (container {result.BuildContainerMs:0.0}, items {result.BuildItemsMs:0.0}, add {result.AddToVisualTreeMs:0.0}), after layout {result.AfterLayoutMs:0.0} ms, N={result.ElementCount}, scenario={FormatScenario(result.Scenario)}, layout={FormatLayoutMode(result.LayoutMode)}, shadow={(result.UseShadow ? "on" : "off")}";
    }

    public static string FormatNavigationResult(BorderProfilingNavigationResult result)
    {
        return $"{FormatTarget(result.Target)} navigation: appearing {result.AppearingMs:0.0} ms, loaded {result.LoadedMs:0.0} ms, size {result.SizeChangedMs:0.0} ms, after layout {result.AfterLayoutMs:0.0} ms, build+add {result.BuildAndAddMs:0.0} ms, scenario={FormatScenario(result.Scenario)}, layout={FormatLayoutMode(result.LayoutMode)}, N={result.ElementCount}";
    }

    public static double TimestampToElapsedMilliseconds(long startTimestamp) =>
        Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

    public static Shadow CreateShadow() => new()
    {
        Brush = new SolidColorBrush(Color.FromArgb("#66000000")),
        Offset = new Point(0, 2),
        Opacity = 0.35f,
        Radius = 8
    };

    public static Label CreateValueLabel(string text, bool strong = false) => new()
    {
        Text = text,
        FontSize = strong ? 16 : 13,
        FontAttributes = strong ? FontAttributes.Bold : FontAttributes.None,
        TextColor = strong ? Colors.Black : Color.FromArgb("#374151")
    };

    private static BorderProfilingLayoutMode GetEffectiveLayoutMode(BorderProfilingOptions options) =>
        options.Scenario == BorderProfilingScenario.CollectionViewBaseline
            ? BorderProfilingLayoutMode.CollectionView
            : options.LayoutMode;

    private static BorderProfilingScenario GetEffectiveContentScenario(BorderProfilingOptions options) =>
        options.Scenario == BorderProfilingScenario.CollectionViewBaseline
            ? BorderProfilingScenario.ComplexContent
            : options.Scenario;

    private static MeasuredContainer CreateMeasuredContainer(
        BorderProfilingTarget target,
        BorderProfilingOptions options,
        BorderProfilingLayoutMode layoutMode)
    {
        if (layoutMode == BorderProfilingLayoutMode.CollectionView)
        {
            var collectionView = new CollectionView
            {
                ItemSizingStrategy = ItemSizingStrategy.MeasureFirstItem,
                SelectionMode = SelectionMode.None,
                ItemTemplate = CreateItemTemplate(target, options),
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.Fill
            };

            return new MeasuredContainer(collectionView, null, collectionView);
        }

        var stack = new VerticalStackLayout
        {
            Spacing = 0
        };

        var scrollView = new ScrollView
        {
            Content = stack
        };

        return new MeasuredContainer(scrollView, stack, null);
    }

    private static IReadOnlyList<object> CreateMeasuredItems(
        BorderProfilingTarget target,
        BorderProfilingOptions options,
        BorderProfilingLayoutMode layoutMode)
    {
        if (layoutMode == BorderProfilingLayoutMode.CollectionView)
            return Enumerable.Range(0, options.ElementCount)
                .Select(static index => new BorderProfilingItemModel(index))
                .Cast<object>()
                .ToArray();

        return Enumerable.Range(0, options.ElementCount)
            .Select(index => CreateScenarioItem(target, options, index))
            .Cast<object>()
            .ToArray();
    }

    private static void AddItemsToContainer(MeasuredContainer container, IReadOnlyList<object> items)
    {
        if (container.CollectionView is not null)
        {
            container.CollectionView.ItemsSource = items;
            return;
        }

        if (container.Stack is null)
            return;

        foreach (var item in items)
        {
            if (item is View view)
                container.Stack.Children.Add(view);
        }
    }

    private static DataTemplate CreateItemTemplate(BorderProfilingTarget target, BorderProfilingOptions options)
    {
        return new DataTemplate(() =>
        {
            var presenter = new ContentView();
            presenter.BindingContextChanged += (_, _) =>
            {
                if (presenter.BindingContext is BorderProfilingItemModel item)
                    presenter.Content = CreateScenarioItem(target, options, item.Index);
            };

            return presenter;
        });
    }

    private static View CreateScenarioItem(BorderProfilingTarget target, BorderProfilingOptions options, int index)
    {
        if (GetEffectiveContentScenario(options) == BorderProfilingScenario.ContainerOnlyBaseline)
            return CreateContainerOnlyItem(options, index);

        var content = CreateScenarioContent(options, index);

        return target == BorderProfilingTarget.Border
            ? CreateMauiBorder(options, content)
            : CreateFastBorder(options, content);
    }

    private static View? CreateScenarioContent(BorderProfilingOptions options, int index)
    {
        return GetEffectiveContentScenario(options) switch
        {
            BorderProfilingScenario.EmptyBorder => null,
            BorderProfilingScenario.BorderWithLabel => CreateLabelContent(index),
            _ => CreateComplexContent(index)
        };
    }

    private static Border CreateMauiBorder(BorderProfilingOptions options, View? content)
    {
        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            Stroke = new SolidColorBrush(Color.FromArgb("#2563EB")),
            StrokeThickness = options.BorderThickness,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(options.CornerRadius) },
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8),
            HeightRequest = 54,
            HorizontalOptions = LayoutOptions.Fill,
            Content = content
        };

        if (options.UseShadow)
            border.Shadow = CreateShadow();

        return border;
    }

    private static FastBorder CreateFastBorder(BorderProfilingOptions options, View? content)
    {
        var border = new FastBorder
        {
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            BorderColor = Color.FromArgb("#2563EB"),
            BorderThickness = options.BorderThickness,
            CornerRadius = new CornerRadius(options.CornerRadius),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8),
            HeightRequest = 54,
            HorizontalOptions = LayoutOptions.Fill,
            Content = content
        };

        if (options.UseShadow)
            border.Shadow = CreateShadow();

        return border;
    }

    private static View CreateContainerOnlyItem(BorderProfilingOptions options, int index)
    {
        var content = CreateComplexContent(index);
        return new Grid
        {
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8),
            HeightRequest = 54,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                content
            }
        };
    }

    private static Label CreateLabelContent(int index) => new()
    {
        Text = $"Item {index + 1}",
        FontSize = 14,
        TextColor = Colors.Black,
        VerticalOptions = LayoutOptions.Center
    };

    private static Grid CreateComplexContent(int index)
    {
        var title = new Label
        {
            Text = $"Item {index + 1}",
            FontSize = 14,
            TextColor = Colors.Black,
            VerticalOptions = LayoutOptions.Center
        };

        var caption = new Label
        {
            Text = "sample",
            FontSize = 12,
            TextColor = Color.FromArgb("#6B7280"),
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };

        Grid.SetColumn(caption, 1);

        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children =
            {
                title,
                caption
            }
        };
    }

    private sealed record BorderProfilingItemModel(int Index);

    private sealed record MeasuredContainer(View Root, VerticalStackLayout? Stack, CollectionView? CollectionView);
}
