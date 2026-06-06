using CoreAnimation;
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Primitives;
using ObjCRuntime;
using UIKit;
using ContentView = Microsoft.Maui.Platform.ContentView;
using LayoutAlignment = Microsoft.Maui.Controls.LayoutAlignment;

namespace SevexLabs.Ui.Maui.Controls.FastBorder.Handlers;

[Flags]
internal enum FastBorderContainerUpdateResult
{
    None = 0,
    Layout = 1,
    IntrinsicSize = 2
}

internal sealed class FastBorderContainerView : ContentView
{
    private readonly CAShapeLayer _shadowLayer = new();
    private readonly CAShapeLayer _shadowMaskLayer = new();
    private readonly CAShapeLayer _chromeLayer = new();
    private readonly CAShapeLayer _overlayBorderLayer = new();

    // Mask used only for clipping the content to the FastBorder shape.
    // The mask is applied to the MAUI content host, not to this container,
    // otherwise the shadow would be clipped too.
    private readonly CAShapeLayer _contentMaskLayer = new();

    private FastBorder? _virtualView;

    private CornerRadii _cornerRadii;
    private nfloat _borderThickness;
    private Thickness _padding;

    private UIColor _backgroundColorNative = UIColor.Clear;
    private UIColor _borderColorNative = UIColor.Clear;
    private bool _isOverlayBorderVisible;
    private UIColor _overlayBorderColorNative = UIColor.Clear;
    private nfloat _overlayBorderThickness;

    private bool _hasShadow;
    private UIColor _shadowColorNative = UIColor.Clear;
    private nfloat _shadowRadius;
    private CGSize _shadowOffset;

    public FastBorderContainerView()
    {
        Opaque = false;
        BackgroundColor = UIColor.Clear;
        ClipsToBounds = false;

        Layer.MasksToBounds = false;
        Layer.BackgroundColor = UIColor.Clear.CGColor;

        _shadowLayer.Hidden = true;
        _shadowLayer.FillColor = UIColor.Clear.CGColor;
        _shadowLayer.StrokeColor = UIColor.Clear.CGColor;
        _shadowLayer.LineWidth = 0;
        _shadowLayer.MasksToBounds = false;

        _chromeLayer.MasksToBounds = false;
        _chromeLayer.FillColor = UIColor.Clear.CGColor;
        _chromeLayer.StrokeColor = UIColor.Clear.CGColor;
        _chromeLayer.LineWidth = 0;
        _chromeLayer.Path = null;

        _overlayBorderLayer.Hidden = true;
        _overlayBorderLayer.MasksToBounds = false;
        _overlayBorderLayer.FillColor = UIColor.Clear.CGColor;
        _overlayBorderLayer.StrokeColor = UIColor.Clear.CGColor;
        _overlayBorderLayer.LineWidth = 0;
        _overlayBorderLayer.Path = null;
        _overlayBorderLayer.ZPosition = 10;

        _contentMaskLayer.FillColor = UIColor.Black.CGColor;
        _contentMaskLayer.StrokeColor = UIColor.Clear.CGColor;
        _contentMaskLayer.LineWidth = 0;

        Layer.InsertSublayer(_shadowLayer, 0);
        Layer.InsertSublayer(_chromeLayer, 1);
        Layer.InsertSublayer(_overlayBorderLayer, 2);
    }

    public FastBorderContainerUpdateResult Update(FastBorder view)
    {
        _virtualView = view;
        var result = FastBorderContainerUpdateResult.None;

        var cornerRadii = CornerRadii.FromCornerRadius(view.CornerRadius);
        if (!AreEqual(_cornerRadii, cornerRadii))
        {
            _cornerRadii = cornerRadii;
            result |= FastBorderContainerUpdateResult.Layout;
        }

        var borderThickness = (nfloat)Math.Max(0f, view.BorderThickness);
        if (!AreClose(_borderThickness, borderThickness))
        {
            _borderThickness = borderThickness;
            result |= FastBorderContainerUpdateResult.Layout | FastBorderContainerUpdateResult.IntrinsicSize;
        }

        if (!AreEqual(_padding, view.Padding))
        {
            _padding = view.Padding;
            result |= FastBorderContainerUpdateResult.Layout | FastBorderContainerUpdateResult.IntrinsicSize;
        }

        var backgroundColorNative = ResolveBackgroundColor(view);
        if (!AreEqual(_backgroundColorNative, backgroundColorNative))
        {
            _backgroundColorNative = backgroundColorNative;
            result |= FastBorderContainerUpdateResult.Layout;
        }

        var borderColorNative = view.BorderColor?.ToPlatform() ?? UIColor.Clear;
        if (!AreEqual(_borderColorNative, borderColorNative))
        {
            _borderColorNative = borderColorNative;
            result |= FastBorderContainerUpdateResult.Layout;
        }

        result |= UpdateOverlayBorderState(view);
        result |= UpdateShadow(view);

        return result;
    }

    public void Reset()
    {
        _virtualView = null;
        _padding = default;
        _cornerRadii = default;
        _borderThickness = 0;
        _backgroundColorNative = UIColor.Clear;
        _borderColorNative = UIColor.Clear;
        _isOverlayBorderVisible = false;
        _overlayBorderColorNative = UIColor.Clear;
        _overlayBorderThickness = 0;
        _hasShadow = false;
        _shadowColorNative = UIColor.Clear;
        _shadowRadius = 0;
        _shadowOffset = CGSize.Empty;

        _shadowLayer.Hidden = true;
        _shadowLayer.Path = null;
        _shadowLayer.ShadowPath = null;
        _shadowLayer.Mask = null;
        _shadowLayer.ShadowOpacity = 0;
        _shadowLayer.ShadowRadius = 0;
        _shadowLayer.ShadowOffset = CGSize.Empty;
        _shadowLayer.ShadowColor = UIColor.Clear.CGColor;

        _shadowMaskLayer.Path = null;

        _chromeLayer.Path = null;
        _chromeLayer.FillColor = UIColor.Clear.CGColor;
        _chromeLayer.StrokeColor = UIColor.Clear.CGColor;
        _chromeLayer.LineWidth = 0;

        _overlayBorderLayer.Hidden = true;
        _overlayBorderLayer.Path = null;
        _overlayBorderLayer.FillColor = UIColor.Clear.CGColor;
        _overlayBorderLayer.StrokeColor = UIColor.Clear.CGColor;
        _overlayBorderLayer.LineWidth = 0;

        _contentMaskLayer.Path = null;
        _contentMaskLayer.Frame = CGRect.Empty;

        foreach (var subview in Subviews)
        {
            subview.Layer.Mask = null;
            subview.Layer.CornerRadius = 0;
            subview.Layer.MasksToBounds = false;
            subview.ClipsToBounds = false;
        }
    }

    public override CGSize SizeThatFits(CGSize size)
    {
        var measured = base.SizeThatFits(size);

        var horizontalInset = _borderThickness * 2f;
        var verticalInset = _borderThickness * 2f;

        if (measured.Width > 0)
        {
            measured.Width += horizontalInset;
        }

        if (measured.Height > 0)
        {
            measured.Height += verticalInset;
        }

        return measured;
    }

    public override CGSize IntrinsicContentSize
    {
        get
        {
            var size = base.IntrinsicContentSize;

            if (size.Width > 0 && size.Width < nfloat.MaxValue)
            {
                size.Width += _borderThickness * 2f;
            }

            if (size.Height > 0 && size.Height < nfloat.MaxValue)
            {
                size.Height += _borderThickness * 2f;
            }

            return size;
        }
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            RemoveContentClipMask();
            return;
        }

        var outerRect = bounds;
        var normalizedRadii = NormalizeRadii(_cornerRadii, outerRect);

        UpdateShadowLayer(outerRect, normalizedRadii);
        UpdateChromeLayer(outerRect, normalizedRadii);
        UpdateOverlayBorderLayer(outerRect, normalizedRadii);

        LayoutContentInsideContentRect();

        // Important:
        // Layout uses BorderThickness + Padding.
        // Clipping uses BorderThickness only.
        // This clips images near the FastBorder corners, but does not visually
        // apply the same corner radius to padded inner layouts.
        ApplyContentClipMask(outerRect, normalizedRadii);
    }

    private void UpdateShadowLayer(CGRect outerRect, CornerRadii outerRadii)
    {
        if (!_hasShadow)
        {
            _shadowLayer.Hidden = true;
            _shadowLayer.Path = null;
            _shadowLayer.ShadowPath = null;
            _shadowLayer.Mask = null;
            _shadowLayer.ShadowOpacity = 0;
            return;
        }

        var shadowPadding = (_shadowRadius * 2f)
                            + NMath.Abs(_shadowOffset.Width)
                            + NMath.Abs(_shadowOffset.Height);

        var shadowFrame = new CGRect(
            outerRect.X - shadowPadding,
            outerRect.Y - shadowPadding,
            outerRect.Width + (shadowPadding * 2f),
            outerRect.Height + (shadowPadding * 2f));

        _shadowLayer.Hidden = false;
        _shadowLayer.Frame = shadowFrame;

        var localBodyRect = new CGRect(
            shadowPadding,
            shadowPadding,
            outerRect.Width,
            outerRect.Height);

        using var bodyPath = CreateRoundedPath(localBodyRect, outerRadii);

        _shadowLayer.Path = bodyPath;
        _shadowLayer.ShadowPath = bodyPath;
        _shadowLayer.FillColor = UIColor.Clear.CGColor;
        _shadowLayer.StrokeColor = UIColor.Clear.CGColor;
        _shadowLayer.LineWidth = 0;
        _shadowLayer.ShadowColor = _shadowColorNative.CGColor;
        _shadowLayer.ShadowOpacity = 1f;
        _shadowLayer.ShadowRadius = _shadowRadius;
        _shadowLayer.ShadowOffset = _shadowOffset;
        _shadowLayer.MasksToBounds = false;

        using var outerMaskPath = new CGPath();
        outerMaskPath.AddRect(_shadowLayer.Bounds);
        outerMaskPath.AddPath(bodyPath);

        _shadowMaskLayer.Frame = _shadowLayer.Bounds;
        _shadowMaskLayer.Path = outerMaskPath;
        _shadowMaskLayer.FillRule = CAShapeLayer.FillRuleEvenOdd;
        _shadowMaskLayer.FillColor = UIColor.Black.CGColor;

        _shadowLayer.Mask = _shadowMaskLayer;
    }

    private void UpdateChromeLayer(CGRect outerRect, CornerRadii outerRadii)
    {
        _chromeLayer.Frame = outerRect;

        var hasVisibleBorder = _borderThickness > 0 && !IsClearColor(_borderColorNative);
        var strokeInset = hasVisibleBorder ? _borderThickness / 2f : 0f;

        var localRect = _chromeLayer.Bounds;

        var drawRect = new CGRect(
            localRect.X + strokeInset,
            localRect.Y + strokeInset,
            NMath.Max(0, localRect.Width - _borderThickness),
            NMath.Max(0, localRect.Height - _borderThickness));

        var drawRadii = outerRadii.Reduce(strokeInset);
        drawRadii = NormalizeRadii(drawRadii, drawRect);

        using var path = CreateRoundedPath(drawRect, drawRadii);

        _chromeLayer.Path = path;
        _chromeLayer.FillColor = _backgroundColorNative.CGColor;
        _chromeLayer.StrokeColor = _borderColorNative.CGColor;
        _chromeLayer.LineWidth = hasVisibleBorder ? _borderThickness : 0;
        _chromeLayer.MasksToBounds = false;
    }

    public FastBorderContainerUpdateResult UpdateOverlayBorder(FastBorder view)
    {
        return UpdateOverlayBorderState(view);
    }

    private FastBorderContainerUpdateResult UpdateOverlayBorderState(FastBorder view)
    {
        var isOverlayBorderVisible = view.IsOverlayBorderVisible;
        var overlayBorderThickness = (nfloat)Math.Max(0, view.OverlayBorderThickness);
        var overlayBorderColorNative = view.OverlayBorderColor?.ToPlatform() ?? UIColor.Clear;

        if (_isOverlayBorderVisible == isOverlayBorderVisible &&
            AreClose(_overlayBorderThickness, overlayBorderThickness) &&
            AreEqual(_overlayBorderColorNative, overlayBorderColorNative))
        {
            return FastBorderContainerUpdateResult.None;
        }

        _isOverlayBorderVisible = isOverlayBorderVisible;
        _overlayBorderThickness = overlayBorderThickness;
        _overlayBorderColorNative = overlayBorderColorNative;

        return FastBorderContainerUpdateResult.Layout;
    }

    private void UpdateOverlayBorderLayer(CGRect outerRect, CornerRadii outerRadii)
    {
        var hasVisibleOverlay = _isOverlayBorderVisible
                                && _overlayBorderThickness > 0
                                && !IsClearColor(_overlayBorderColorNative);

        if (!hasVisibleOverlay)
        {
            _overlayBorderLayer.Hidden = true;
            _overlayBorderLayer.Path = null;
            _overlayBorderLayer.LineWidth = 0;
            _overlayBorderLayer.StrokeColor = UIColor.Clear.CGColor;
            return;
        }

        _overlayBorderLayer.Hidden = false;
        _overlayBorderLayer.Frame = outerRect;

        var strokeInset = _overlayBorderThickness / 2f;
        var localRect = _overlayBorderLayer.Bounds;

        var drawRect = new CGRect(
            localRect.X + strokeInset,
            localRect.Y + strokeInset,
            NMath.Max(0, localRect.Width - _overlayBorderThickness),
            NMath.Max(0, localRect.Height - _overlayBorderThickness));

        var drawRadii = outerRadii.Reduce(strokeInset);
        drawRadii = NormalizeRadii(drawRadii, drawRect);

        using var path = CreateRoundedPath(drawRect, drawRadii);

        _overlayBorderLayer.Path = path;
        _overlayBorderLayer.FillColor = UIColor.Clear.CGColor;
        _overlayBorderLayer.StrokeColor = _overlayBorderColorNative.CGColor;
        _overlayBorderLayer.LineWidth = _overlayBorderThickness;
        _overlayBorderLayer.MasksToBounds = false;
    }

    private void LayoutContentInsideContentRect()
    {
        var contentSubview = GetContentSubview();
        if (contentSubview is null)
        {
            RemoveContentClipMask();
            return;
        }

        var contentRect = GetContentRect(Bounds, _borderThickness, _padding);
        if (contentRect.Width <= 0 || contentRect.Height <= 0)
        {
            contentSubview.Frame = new CGRect(contentRect.X, contentRect.Y, 0, 0);
            RemoveContentClipMask(contentSubview);
            return;
        }

        var content = _virtualView?.Content;
        var horizontalAlignment = content?.HorizontalOptions.Alignment ?? LayoutAlignment.Fill;
        var verticalAlignment = content?.VerticalOptions.Alignment ?? LayoutAlignment.Fill;

        var baseFrame = contentSubview.Frame;

        var arrangedWidth = horizontalAlignment == LayoutAlignment.Fill
            ? contentRect.Width
            : baseFrame.Width;

        var arrangedHeight = verticalAlignment == LayoutAlignment.Fill
            ? contentRect.Height
            : baseFrame.Height;

        if (arrangedWidth < 0)
        {
            arrangedWidth = 0;
        }

        if (arrangedHeight < 0)
        {
            arrangedHeight = 0;
        }

        var arrangedX = AlignCoordinate(
            contentRect.X,
            contentRect.Width,
            arrangedWidth,
            horizontalAlignment);

        var arrangedY = AlignCoordinate(
            contentRect.Y,
            contentRect.Height,
            arrangedHeight,
            verticalAlignment);

        var arrangedFrame = new CGRect(arrangedX, arrangedY, arrangedWidth, arrangedHeight);

        if (!AreClose(contentSubview.Frame, arrangedFrame))
        {
            contentSubview.Frame = arrangedFrame;
        }
    }

    private void ApplyContentClipMask(CGRect outerRect, CornerRadii outerRadii)
    {
        var contentSubview = GetContentSubview();
        if (contentSubview is null)
        {
            RemoveContentClipMask();
            return;
        }

        if (contentSubview.Bounds.Width <= 0 || contentSubview.Bounds.Height <= 0)
        {
            RemoveContentClipMask(contentSubview);
            return;
        }

        var clipRect = GetContentClipRect(outerRect, _borderThickness);
        if (clipRect.Width <= 0 || clipRect.Height <= 0)
        {
            RemoveContentClipMask(contentSubview);
            return;
        }

        var contentFrame = contentSubview.Frame;

        // Convert the FastBorder-local clip rect into the contentSubview-local
        // coordinate system.
        var localClipRect = new CGRect(
            clipRect.X - contentFrame.X,
            clipRect.Y - contentFrame.Y,
            clipRect.Width,
            clipRect.Height);

        var clipRadii = outerRadii.Reduce(_borderThickness);
        clipRadii = NormalizeRadii(clipRadii, clipRect);

        using var clipPath = CreateRoundedPath(localClipRect, clipRadii);

        _contentMaskLayer.Frame = contentSubview.Bounds;
        _contentMaskLayer.Path = clipPath;
        _contentMaskLayer.FillColor = UIColor.Black.CGColor;

        contentSubview.Layer.Mask = _contentMaskLayer;

        // Do not enable MasksToBounds here. The mask already clips the content.
        // Keeping ClipsToBounds false avoids reintroducing the previous issue
        // where padded inner layouts were clipped as if they had their own radius.
        contentSubview.Layer.MasksToBounds = false;
        contentSubview.ClipsToBounds = false;
    }

    private void RemoveContentClipMask()
    {
        foreach (var subview in Subviews)
        {
            RemoveContentClipMask(subview);
        }
    }

    private void RemoveContentClipMask(UIView subview)
    {
        if (subview.Layer.Mask == _contentMaskLayer)
        {
            subview.Layer.Mask = null;
        }

        _contentMaskLayer.Path = null;
    }

    private UIView? GetContentSubview()
    {
        if (Subviews.Length == 0)
        {
            return null;
        }

        UIView? candidate = null;

        foreach (var subview in Subviews)
        {
            candidate = subview;
        }

        return candidate;
    }

    private static nfloat AlignCoordinate(
        nfloat start,
        nfloat availableLength,
        nfloat arrangedLength,
        LayoutAlignment alignment)
    {
        var remaining = availableLength - arrangedLength;
        if (remaining <= 0)
        {
            return start;
        }

        return alignment switch
        {
            LayoutAlignment.Center => start + (remaining / 2f),
            LayoutAlignment.End => start + remaining,
            _ => start
        };
    }

    private static CGRect GetContentRect(CGRect outerRect, nfloat borderThickness, Thickness padding)
    {
        var leftInset = NMath.Max(0, borderThickness + (nfloat)padding.Left);
        var topInset = NMath.Max(0, borderThickness + (nfloat)padding.Top);
        var rightInset = NMath.Max(0, borderThickness + (nfloat)padding.Right);
        var bottomInset = NMath.Max(0, borderThickness + (nfloat)padding.Bottom);

        var contentRect = new CGRect(
            outerRect.X + leftInset,
            outerRect.Y + topInset,
            outerRect.Width - leftInset - rightInset,
            outerRect.Height - topInset - bottomInset);

        if (contentRect.Width < 0)
        {
            contentRect.Width = 0;
        }

        if (contentRect.Height < 0)
        {
            contentRect.Height = 0;
        }

        return contentRect;
    }

    private static CGRect GetContentClipRect(CGRect outerRect, nfloat borderThickness)
    {
        var inset = NMath.Max(0, borderThickness);

        var clipRect = new CGRect(
            outerRect.X + inset,
            outerRect.Y + inset,
            outerRect.Width - (inset * 2f),
            outerRect.Height - (inset * 2f));

        if (clipRect.Width < 0)
        {
            clipRect.Width = 0;
        }

        if (clipRect.Height < 0)
        {
            clipRect.Height = 0;
        }

        return clipRect;
    }

    private FastBorderContainerUpdateResult UpdateShadow(FastBorder view)
    {
        var shadow = view.Shadow;
        if (shadow is null)
        {
            if (!_hasShadow &&
                AreEqual(_shadowColorNative, UIColor.Clear) &&
                AreClose(_shadowRadius, 0) &&
                AreClose(_shadowOffset.Width, 0) &&
                AreClose(_shadowOffset.Height, 0))
            {
                return FastBorderContainerUpdateResult.None;
            }

            _hasShadow = false;
            _shadowColorNative = UIColor.Clear;
            _shadowRadius = 0;
            _shadowOffset = CGSize.Empty;
            return FastBorderContainerUpdateResult.Layout;
        }

        var shadowColorNative = ResolveShadowColor(shadow);
        var shadowRadius = (nfloat)Math.Max(0, shadow.Radius);
        var shadowOffset = new CGSize(shadow.Offset.X, shadow.Offset.Y);

        if (_hasShadow &&
            AreEqual(_shadowColorNative, shadowColorNative) &&
            AreClose(_shadowRadius, shadowRadius) &&
            AreClose(_shadowOffset.Width, shadowOffset.Width) &&
            AreClose(_shadowOffset.Height, shadowOffset.Height))
        {
            return FastBorderContainerUpdateResult.None;
        }

        _hasShadow = true;
        _shadowColorNative = shadowColorNative;
        _shadowRadius = shadowRadius;
        _shadowOffset = shadowOffset;

        return FastBorderContainerUpdateResult.Layout;
    }

    private static CornerRadii NormalizeRadii(CornerRadii radii, CGRect rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return CornerRadii.Zero;
        }

        radii = radii.ClampToPositive();

        var topScale = GetScale(rect.Width, radii.TopLeft + radii.TopRight);
        var bottomScale = GetScale(rect.Width, radii.BottomLeft + radii.BottomRight);
        var leftScale = GetScale(rect.Height, radii.TopLeft + radii.BottomLeft);
        var rightScale = GetScale(rect.Height, radii.TopRight + radii.BottomRight);

        var scale = NMath.Min(NMath.Min(topScale, bottomScale), NMath.Min(leftScale, rightScale));

        if (scale >= 1f)
        {
            return radii;
        }

        return radii.Scale(scale);
    }

    private static nfloat GetScale(nfloat available, nfloat requested)
    {
        if (requested <= 0 || requested <= available)
        {
            return 1f;
        }

        return available / requested;
    }

    private static CGPath CreateRoundedPath(CGRect rect, CornerRadii radii)
    {
        radii = NormalizeRadii(radii, rect);

        var path = new CGPath();

        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return path;
        }

        var minX = rect.GetMinX();
        var minY = rect.GetMinY();
        var maxX = rect.GetMaxX();
        var maxY = rect.GetMaxY();

        var topLeft = radii.TopLeft;
        var topRight = radii.TopRight;
        var bottomRight = radii.BottomRight;
        var bottomLeft = radii.BottomLeft;

        path.MoveToPoint(minX + topLeft, minY);

        path.AddLineToPoint(maxX - topRight, minY);
        if (topRight > 0)
        {
            path.AddArc(
                maxX - topRight,
                minY + topRight,
                topRight,
                DegreesToRadians(-90),
                DegreesToRadians(0),
                false);
        }

        path.AddLineToPoint(maxX, maxY - bottomRight);
        if (bottomRight > 0)
        {
            path.AddArc(
                maxX - bottomRight,
                maxY - bottomRight,
                bottomRight,
                DegreesToRadians(0),
                DegreesToRadians(90),
                false);
        }

        path.AddLineToPoint(minX + bottomLeft, maxY);
        if (bottomLeft > 0)
        {
            path.AddArc(
                minX + bottomLeft,
                maxY - bottomLeft,
                bottomLeft,
                DegreesToRadians(90),
                DegreesToRadians(180),
                false);
        }

        path.AddLineToPoint(minX, minY + topLeft);
        if (topLeft > 0)
        {
            path.AddArc(
                minX + topLeft,
                minY + topLeft,
                topLeft,
                DegreesToRadians(180),
                DegreesToRadians(270),
                false);
        }

        path.CloseSubpath();

        return path;
    }

    private static nfloat DegreesToRadians(double degrees)
    {
        return (nfloat)(degrees * Math.PI / 180d);
    }

    private static UIColor ResolveBackgroundColor(FastBorder view)
    {
        if (view.Background is SolidColorBrush solidBrush && solidBrush.Color is not null)
        {
            return solidBrush.Color.ToPlatform();
        }

        if (view.BackgroundColor is not null)
        {
            return view.BackgroundColor.ToPlatform();
        }

        return UIColor.Clear;
    }

    private static UIColor ResolveShadowColor(Shadow shadow)
    {
        UIColor baseColor;

        if (shadow.Brush is SolidColorBrush solidBrush && solidBrush.Color is not null)
        {
            baseColor = solidBrush.Color.ToPlatform();
        }
        else
        {
            baseColor = UIColor.Black;
        }

        var opacity = shadow.Opacity;
        if (opacity < 0)
        {
            opacity = 0;
        }
        else if (opacity > 1)
        {
            opacity = 1;
        }

        return baseColor.ColorWithAlpha((nfloat)opacity);
    }

    private static bool IsClearColor(UIColor color)
    {
        return color.CGColor.Alpha <= 0;
    }

    private static bool AreEqual(Thickness first, Thickness second)
    {
        return Math.Abs(first.Left - second.Left) < 0.001d
               && Math.Abs(first.Top - second.Top) < 0.001d
               && Math.Abs(first.Right - second.Right) < 0.001d
               && Math.Abs(first.Bottom - second.Bottom) < 0.001d;
    }

    private static bool AreEqual(CornerRadii first, CornerRadii second)
    {
        return AreClose(first.TopLeft, second.TopLeft)
               && AreClose(first.TopRight, second.TopRight)
               && AreClose(first.BottomLeft, second.BottomLeft)
               && AreClose(first.BottomRight, second.BottomRight);
    }

    private static bool AreEqual(UIColor first, UIColor second)
    {
        if (ReferenceEquals(first, second))
        {
            return true;
        }

        first.GetRGBA(out var firstRed, out var firstGreen, out var firstBlue, out var firstAlpha);
        second.GetRGBA(out var secondRed, out var secondGreen, out var secondBlue, out var secondAlpha);

        return AreClose(firstRed, secondRed)
               && AreClose(firstGreen, secondGreen)
               && AreClose(firstBlue, secondBlue)
               && AreClose(firstAlpha, secondAlpha);
    }

    private static bool AreClose(nfloat first, nfloat second)
    {
        return NMath.Abs(first - second) < 0.001d;
    }

    private static bool AreClose(CGRect first, CGRect second)
    {
        const double epsilon = 0.5d;

        return Math.Abs(first.X - second.X) < epsilon
               && Math.Abs(first.Y - second.Y) < epsilon
               && Math.Abs(first.Width - second.Width) < epsilon
               && Math.Abs(first.Height - second.Height) < epsilon;
    }

    private readonly struct CornerRadii
    {
        public static readonly CornerRadii Zero = new(0, 0, 0, 0);

        public CornerRadii(nfloat topLeft, nfloat topRight, nfloat bottomLeft, nfloat bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }

        public nfloat TopLeft { get; }

        public nfloat TopRight { get; }

        public nfloat BottomLeft { get; }

        public nfloat BottomRight { get; }

        public static CornerRadii FromCornerRadius(CornerRadius cornerRadius)
        {
            return new CornerRadii(
                (nfloat)Math.Max(0, cornerRadius.TopLeft),
                (nfloat)Math.Max(0, cornerRadius.TopRight),
                (nfloat)Math.Max(0, cornerRadius.BottomLeft),
                (nfloat)Math.Max(0, cornerRadius.BottomRight));
        }

        public CornerRadii ClampToPositive()
        {
            return new CornerRadii(
                NMath.Max(0, TopLeft),
                NMath.Max(0, TopRight),
                NMath.Max(0, BottomLeft),
                NMath.Max(0, BottomRight));
        }

        public CornerRadii Reduce(nfloat value)
        {
            return new CornerRadii(
                NMath.Max(0, TopLeft - value),
                NMath.Max(0, TopRight - value),
                NMath.Max(0, BottomLeft - value),
                NMath.Max(0, BottomRight - value));
        }

        public CornerRadii Scale(nfloat scale)
        {
            return new CornerRadii(
                TopLeft * scale,
                TopRight * scale,
                BottomLeft * scale,
                BottomRight * scale);
        }
    }
}
