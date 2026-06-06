using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace SevexLabs.Ui.Maui.Controls.FastBorder;

/// <summary>
/// A lightweight single-content border optimized for Android and iOS.
/// Layout and content measurement are delegated to ContentView, while the
/// native handlers are responsible for rendering border, background, corner
/// radius, clipping, and shadow.
/// </summary>
/// <remarks>
/// <c>Padding</c> participates in child layout. Corner clipping is handled by the native
/// renderer separately from padding. Shadow rendering does not add to the measured size. The
/// internal overlay border is not part of the public API and normal border behavior is unchanged
/// when no overlay is active.
/// </remarks>
public class FastBorder : ContentView
{
    private bool _isOverlayBorderVisible;
    private Color _overlayBorderColor = Colors.Transparent;
    private double _overlayBorderThickness;

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius),
        typeof(CornerRadius),
        typeof(FastBorder),
        new CornerRadius(0));

    public static readonly BindableProperty BorderThicknessProperty = BindableProperty.Create(
        nameof(BorderThickness),
        typeof(double),
        typeof(FastBorder),
        1d);

    public static readonly BindableProperty BorderColorProperty = BindableProperty.Create(
        nameof(BorderColor),
        typeof(Color),
        typeof(FastBorder),
        Colors.Transparent);

    /// <summary>
    /// Gets or sets the border corner radius.
    /// </summary>
    /// <remarks>
    /// Supports different values for each corner and affects border, background, and clipping.
    /// </remarks>
    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>
    /// Gets or sets the border thickness.
    /// </summary>
    /// <remarks>
    /// Thickness participates in measure and layout before native rendering.
    /// </remarks>
    public double BorderThickness
    {
        get => (double)GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public Color BorderColor
    {
        get => (Color)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    internal bool IsOverlayBorderVisible
    {
        get => _isOverlayBorderVisible;
        set
        {
            if (_isOverlayBorderVisible == value)
            {
                return;
            }

            _isOverlayBorderVisible = value;
            OnPropertyChanged(nameof(IsOverlayBorderVisible));
        }
    }

    internal Color OverlayBorderColor
    {
        get => _overlayBorderColor;
        set
        {
            value ??= Colors.Transparent;

            if (_overlayBorderColor == value)
            {
                return;
            }

            _overlayBorderColor = value;
            OnPropertyChanged(nameof(OverlayBorderColor));
        }
    }

    internal double OverlayBorderThickness
    {
        get => _overlayBorderThickness;
        set
        {
            value = Math.Max(0, value);

            if (_overlayBorderThickness.Equals(value))
            {
                return;
            }

            _overlayBorderThickness = value;
            OnPropertyChanged(nameof(OverlayBorderThickness));
        }
    }

    /// <summary>
    /// Ensures invalid values do not reach native renderers.
    /// </summary>
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == CornerRadiusProperty.PropertyName)
        {
            var normalizedCornerRadius = NormalizeCornerRadius(CornerRadius);

            if (!AreEqual(CornerRadius, normalizedCornerRadius))
            {
                CornerRadius = normalizedCornerRadius;
            }
        }

        if (propertyName == BorderThicknessProperty.PropertyName && BorderThickness < 0)
        {
            BorderThickness = 0;
        }
    }

    private static CornerRadius NormalizeCornerRadius(CornerRadius cornerRadius)
    {
        return new CornerRadius(
            Math.Max(0, cornerRadius.TopLeft),
            Math.Max(0, cornerRadius.TopRight),
            Math.Max(0, cornerRadius.BottomLeft),
            Math.Max(0, cornerRadius.BottomRight));
    }

    private static bool AreEqual(CornerRadius left, CornerRadius right)
    {
        return left.TopLeft.Equals(right.TopLeft)
            && left.TopRight.Equals(right.TopRight)
            && left.BottomLeft.Equals(right.BottomLeft)
            && left.BottomRight.Equals(right.BottomRight);
    }
}
