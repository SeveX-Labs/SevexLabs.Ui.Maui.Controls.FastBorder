using Android.Content;
using Android.Graphics;
using Android.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using Paint = Android.Graphics.Paint;
using Path = Android.Graphics.Path;
using RectF = Android.Graphics.RectF;
using AView = Android.Views.View;

namespace SevexLabs.Ui.Maui.Controls.FastBorder.Handlers;

[Flags]
internal enum FastBorderNativeUpdateResult
{
    None = 0,
    Draw = 1,
    Layout = 2
}

internal sealed class FastBorderNativeView : ContentViewGroup
{
    private readonly Paint _fillPaint;
    private readonly Paint _strokePaint;
    private readonly Paint _overlayStrokePaint;

    private readonly RectF _borderOuterRect = new();
    private readonly RectF _borderInnerRect = new();
    private readonly RectF _overlayBorderRect = new();

    // Used to layout the real child. This includes BorderThickness + Padding.
    private readonly RectF _contentRect = new();

    // Used only to clip the child drawing. This includes BorderThickness only,
    // not Padding, otherwise the inner layout visually receives the same rounded
    // shape and can clip its own content too aggressively.
    private readonly RectF _contentClipRect = new();

    private readonly Path _backgroundPath = new();
    private readonly Path _borderPath = new();
    private readonly Path _contentClipPath = new();
    private readonly Path _overlayBorderPath = new();

    private readonly float[] _cornerRadiiPx = new float[8];
    private readonly float[] _drawCornerRadiiPx = new float[8];
    private readonly float[] _contentCornerRadiiPx = new float[8];
    private readonly float[] _overlayCornerRadiiPx = new float[8];

    private bool _chromePathDirty = true;
    private bool _contentClipPathDirty = true;
    private bool _overlayBorderPathDirty = true;

    private readonly FastBorderShadowView _shadowView;

    private FastBorder? _virtualView;
    private AView? _contentChild;

    private float _density;

    private float _borderThicknessPx;

    private float _paddingLeftPx;
    private float _paddingTopPx;
    private float _paddingRightPx;
    private float _paddingBottomPx;

    private Android.Graphics.Color _backgroundColor = Android.Graphics.Color.Transparent;
    private Android.Graphics.Color _borderColor = Android.Graphics.Color.Transparent;
    private bool _isOverlayBorderVisible;
    private float _overlayBorderThicknessPx;
    private Android.Graphics.Color _overlayBorderColor = Android.Graphics.Color.Transparent;

    private bool _hasShadow;
    private float _shadowRadiusPx;
    private float _shadowDxPx;
    private float _shadowDyPx;
    private Android.Graphics.Color _shadowColor = Android.Graphics.Color.Transparent;

    private int _shadowInsetLeft;
    private int _shadowInsetTop;
    private int _shadowInsetRight;
    private int _shadowInsetBottom;

    public FastBorderNativeView(Context context) : base(context)
    {
        SetWillNotDraw(false);
        SetClipChildren(false);
        SetClipToPadding(false);

        _fillPaint = new Paint(PaintFlags.AntiAlias)
        {
            Dither = true
        };
        _fillPaint.SetStyle(Paint.Style.Fill);

        _strokePaint = new Paint(PaintFlags.AntiAlias)
        {
            Dither = true
        };
        _strokePaint.SetStyle(Paint.Style.Stroke);

        _overlayStrokePaint = new Paint(PaintFlags.AntiAlias)
        {
            Dither = true
        };
        _overlayStrokePaint.SetStyle(Paint.Style.Stroke);

        _shadowView = new FastBorderShadowView(context);
        AddView(_shadowView, 0);
    }

    public FastBorderNativeUpdateResult Update(FastBorder view)
    {
        _virtualView = view;
        var result = FastBorderNativeUpdateResult.None;
        var density = Context?.Resources?.DisplayMetrics?.Density ?? 1f;
        if (!AreClose(_density, density))
        {
            MarkAllPathsDirty();
            result |= FastBorderNativeUpdateResult.Draw | FastBorderNativeUpdateResult.Layout;
        }

        var radiiChanged = SetCornerRadiiPx(view.CornerRadius, density);
        if (radiiChanged)
        {
            MarkAllPathsDirty();
            result |= FastBorderNativeUpdateResult.Draw;
        }

        var borderThicknessPx = Math.Max(0f, (float)view.BorderThickness * density);
        if (!AreClose(_borderThicknessPx, borderThicknessPx))
        {
            _borderThicknessPx = borderThicknessPx;
            _strokePaint.StrokeWidth = _borderThicknessPx;
            MarkAllPathsDirty();
            result |= FastBorderNativeUpdateResult.Draw | FastBorderNativeUpdateResult.Layout;
        }

        var paddingLeftPx = (float)view.Padding.Left * density;
        var paddingTopPx = (float)view.Padding.Top * density;
        var paddingRightPx = (float)view.Padding.Right * density;
        var paddingBottomPx = (float)view.Padding.Bottom * density;

        if (!AreClose(_paddingLeftPx, paddingLeftPx) ||
            !AreClose(_paddingTopPx, paddingTopPx) ||
            !AreClose(_paddingRightPx, paddingRightPx) ||
            !AreClose(_paddingBottomPx, paddingBottomPx))
        {
            _paddingLeftPx = paddingLeftPx;
            _paddingTopPx = paddingTopPx;
            _paddingRightPx = paddingRightPx;
            _paddingBottomPx = paddingBottomPx;
            result |= FastBorderNativeUpdateResult.Layout;
        }

        var backgroundColor = ResolveBackgroundColor(view);
        if (_backgroundColor != backgroundColor)
        {
            _backgroundColor = backgroundColor;
            _fillPaint.Color = _backgroundColor;
            result |= FastBorderNativeUpdateResult.Draw;
        }

        var borderColor = view.BorderColor?.ToPlatform() ?? Android.Graphics.Color.Transparent;
        if (_borderColor != borderColor)
        {
            _borderColor = borderColor;
            _strokePaint.Color = _borderColor;
            result |= FastBorderNativeUpdateResult.Draw;
        }

        result |= UpdateOverlayBorder(view);

        var shadowResult = UpdateShadow(view, density);
        if ((radiiChanged && _hasShadow) || shadowResult != FastBorderNativeUpdateResult.None)
        {
            UpdateShadowView();
            result |= shadowResult;
        }

        _density = density;

        return result;
    }

    public void Reset()
    {
        _virtualView = null;
        _contentChild = null;
        _density = 0;

        Array.Clear(_cornerRadiiPx);
        Array.Clear(_drawCornerRadiiPx);
        Array.Clear(_contentCornerRadiiPx);
        Array.Clear(_overlayCornerRadiiPx);

        _borderThicknessPx = 0;
        _paddingLeftPx = 0;
        _paddingTopPx = 0;
        _paddingRightPx = 0;
        _paddingBottomPx = 0;

        _backgroundColor = Android.Graphics.Color.Transparent;
        _borderColor = Android.Graphics.Color.Transparent;
        _fillPaint.Color = _backgroundColor;
        _strokePaint.Color = _borderColor;
        _strokePaint.StrokeWidth = 0;

        _isOverlayBorderVisible = false;
        _overlayBorderThicknessPx = 0;
        _overlayBorderColor = Android.Graphics.Color.Transparent;
        _overlayStrokePaint.Color = _overlayBorderColor;
        _overlayStrokePaint.StrokeWidth = 0;

        _hasShadow = false;
        _shadowRadiusPx = 0;
        _shadowDxPx = 0;
        _shadowDyPx = 0;
        _shadowColor = Android.Graphics.Color.Transparent;
        _shadowInsetLeft = 0;
        _shadowInsetTop = 0;
        _shadowInsetRight = 0;
        _shadowInsetBottom = 0;

        _backgroundPath.Reset();
        _borderPath.Reset();
        _contentClipPath.Reset();
        _overlayBorderPath.Reset();
        MarkAllPathsDirty();

        _shadowView.Reset();
    }

    public FastBorderNativeUpdateResult UpdateOverlayBorder(FastBorder view)
    {
        var density = _density > 0
            ? _density
            : Context?.Resources?.DisplayMetrics?.Density ?? 1f;

        var isOverlayBorderVisible = view.IsOverlayBorderVisible;
        var overlayBorderThicknessPx = Math.Max(0f, (float)view.OverlayBorderThickness * density);
        var overlayBorderColor = view.OverlayBorderColor?.ToPlatform() ?? Android.Graphics.Color.Transparent;

        var overlayGeometryChanged = _isOverlayBorderVisible != isOverlayBorderVisible ||
                                     !AreClose(_overlayBorderThicknessPx, overlayBorderThicknessPx);
        var overlayColorChanged = _overlayBorderColor != overlayBorderColor;

        if (!overlayGeometryChanged && !overlayColorChanged)
        {
            return FastBorderNativeUpdateResult.None;
        }

        _isOverlayBorderVisible = isOverlayBorderVisible;
        _overlayBorderThicknessPx = overlayBorderThicknessPx;
        _overlayBorderColor = overlayBorderColor;

        _overlayStrokePaint.Color = _overlayBorderColor;
        _overlayStrokePaint.StrokeWidth = _overlayBorderThicknessPx;

        if (overlayGeometryChanged)
        {
            MarkOverlayBorderPathDirty();
        }

        return FastBorderNativeUpdateResult.Draw;
    }

    public override void OnViewAdded(AView child)
    {
        base.OnViewAdded(child);

        if (child != _shadowView)
        {
            _contentChild = child;
        }
    }

    public override void OnViewRemoved(AView child)
    {
        base.OnViewRemoved(child);

        if (_contentChild == child)
        {
            _contentChild = null;
        }
    }

    protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
    {
        var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
        var heightMode = MeasureSpec.GetMode(heightMeasureSpec);

        var widthSize = MeasureSpec.GetSize(widthMeasureSpec);
        var heightSize = MeasureSpec.GetSize(heightMeasureSpec);

        var extraHorizontal = GetBodyHorizontal();
        var extraVertical = GetBodyVertical();

        int childDesiredWidth = 0;
        int childDesiredHeight = 0;

        if (_contentChild is not null && _contentChild.Visibility != ViewStates.Gone)
        {
            var availableChildWidth = widthMode == MeasureSpecMode.Unspecified
                ? 0
                : Math.Max(0, widthSize - extraHorizontal);

            var availableChildHeight = heightMode == MeasureSpecMode.Unspecified
                ? 0
                : Math.Max(0, heightSize - extraVertical);

            var childHorizontalAlignment = GetChildHorizontalAlignment();
            var childVerticalAlignment = GetChildVerticalAlignment();

            var hasWidthRequest = TryGetRequestedWidthPx(out var requestedWidthPx);
            var hasHeightRequest = TryGetRequestedHeightPx(out var requestedHeightPx);

            int childWidthSpec;
            int childHeightSpec;

            if (hasWidthRequest)
            {
                childWidthSpec = MeasureSpec.MakeMeasureSpec(requestedWidthPx, MeasureSpecMode.Exactly);
            }
            else
            {
                var childWidthMode = widthMode == MeasureSpecMode.Unspecified
                    ? MeasureSpecMode.Unspecified
                    : childHorizontalAlignment == LayoutAlignment.Fill
                        ? MeasureSpecMode.Exactly
                        : MeasureSpecMode.AtMost;

                childWidthSpec = MeasureSpec.MakeMeasureSpec(availableChildWidth, childWidthMode);
            }

            if (hasHeightRequest)
            {
                childHeightSpec = MeasureSpec.MakeMeasureSpec(requestedHeightPx, MeasureSpecMode.Exactly);
            }
            else
            {
                var childHeightMode = heightMode == MeasureSpecMode.Unspecified
                    ? MeasureSpecMode.Unspecified
                    : childVerticalAlignment == LayoutAlignment.Fill
                        ? MeasureSpecMode.Exactly
                        : MeasureSpecMode.AtMost;

                childHeightSpec = MeasureSpec.MakeMeasureSpec(availableChildHeight, childHeightMode);
            }

            _contentChild.Measure(childWidthSpec, childHeightSpec);

            childDesiredWidth = _contentChild.MeasuredWidth;
            childDesiredHeight = _contentChild.MeasuredHeight;
        }

        var desiredWidth = childDesiredWidth + extraHorizontal;
        var desiredHeight = childDesiredHeight + extraVertical;

        var measuredWidth = ResolveSize(desiredWidth, widthMeasureSpec);
        var measuredHeight = ResolveSize(desiredHeight, heightMeasureSpec);

        SetMeasuredDimension(measuredWidth, measuredHeight);

        MeasureShadowChild(measuredWidth, measuredHeight);
    }

    protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
    {
        var width = right - left;
        var height = bottom - top;

        BuildRects(width, height);

        LayoutShadowChild(width, height);

        if (_contentChild is null || _contentChild.Visibility == ViewStates.Gone)
        {
            return;
        }

        var availableWidth = Math.Max(0, (int)Math.Floor(_contentRect.Width()));
        var availableHeight = Math.Max(0, (int)Math.Floor(_contentRect.Height()));

        var horizontalAlignment = GetChildHorizontalAlignment();
        var verticalAlignment = GetChildVerticalAlignment();

        var childWidth = horizontalAlignment == LayoutAlignment.Fill
            ? availableWidth
            : Math.Min(_contentChild.MeasuredWidth, availableWidth);

        var childHeight = verticalAlignment == LayoutAlignment.Fill
            ? availableHeight
            : Math.Min(_contentChild.MeasuredHeight, availableHeight);

        var childLeft = horizontalAlignment switch
        {
            LayoutAlignment.Center => (int)Math.Round(_contentRect.Left + ((availableWidth - childWidth) / 2f)),
            LayoutAlignment.End => (int)Math.Round(_contentRect.Right - childWidth),
            LayoutAlignment.Fill => (int)Math.Round(_contentRect.Left),
            _ => (int)Math.Round(_contentRect.Left)
        };

        var childTop = verticalAlignment switch
        {
            LayoutAlignment.Center => (int)Math.Round(_contentRect.Top + ((availableHeight - childHeight) / 2f)),
            LayoutAlignment.End => (int)Math.Round(_contentRect.Bottom - childHeight),
            LayoutAlignment.Fill => (int)Math.Round(_contentRect.Top),
            _ => (int)Math.Round(_contentRect.Top)
        };

        var childRight = childLeft + childWidth;
        var childBottom = childTop + childHeight;

        _contentChild.Layout(childLeft, childTop, childRight, childBottom);
    }

    protected override void DispatchDraw(Canvas canvas)
    {
        var drawingTime = DrawingTime;

        if (_hasShadow && _shadowView.Visibility == ViewStates.Visible)
        {
            DrawChild(canvas, _shadowView, drawingTime);
        }

        DrawBackgroundAndBorder(canvas);

        if (_contentChild is not null && _contentChild.Visibility != ViewStates.Gone)
        {
            var saveCount = canvas.Save();

            EnsureContentClipPath();

            if (!_contentClipPath.IsEmpty)
            {
                canvas.ClipPath(_contentClipPath);
            }

            DrawChild(canvas, _contentChild, drawingTime);

            canvas.RestoreToCount(saveCount);
        }

        DrawOverlayBorder(canvas);
    }

    private void DrawBackgroundAndBorder(Canvas canvas)
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        EnsureChromePaths();

        if (_backgroundColor != Android.Graphics.Color.Transparent && !_backgroundPath.IsEmpty)
        {
            canvas.DrawPath(_backgroundPath, _fillPaint);
        }

        if (_borderThicknessPx > 0 &&
            _borderColor != Android.Graphics.Color.Transparent &&
            !_borderPath.IsEmpty)
        {
            canvas.DrawPath(_borderPath, _strokePaint);
        }
    }

    private void DrawOverlayBorder(Canvas canvas)
    {
        if (!_isOverlayBorderVisible ||
            _overlayBorderThicknessPx <= 0 ||
            Width <= 0 ||
            Height <= 0)
        {
            return;
        }

        EnsureOverlayBorderPath();

        if (_overlayBorderColor == Android.Graphics.Color.Transparent ||
            _overlayBorderPath.IsEmpty)
        {
            return;
        }

        canvas.DrawPath(_overlayBorderPath, _overlayStrokePaint);
    }

    private void BuildRects(int width, int height)
    {
        _borderOuterRect.Set(0, 0, width, height);

        var halfStroke = _borderThicknessPx / 2f;

        _borderInnerRect.Set(
            _borderOuterRect.Left + halfStroke,
            _borderOuterRect.Top + halfStroke,
            _borderOuterRect.Right - halfStroke,
            _borderOuterRect.Bottom - halfStroke);

        // This rect is used only for clipping the content drawing.
        // It removes the border area but intentionally ignores Padding.
        _contentClipRect.Set(
            _borderOuterRect.Left + _borderThicknessPx,
            _borderOuterRect.Top + _borderThicknessPx,
            _borderOuterRect.Right - _borderThicknessPx,
            _borderOuterRect.Bottom - _borderThicknessPx);

        // This rect is used for laying out the child.
        // It removes both the border area and Padding.
        _contentRect.Set(
            _borderOuterRect.Left + _borderThicknessPx + _paddingLeftPx,
            _borderOuterRect.Top + _borderThicknessPx + _paddingTopPx,
            _borderOuterRect.Right - _borderThicknessPx - _paddingRightPx,
            _borderOuterRect.Bottom - _borderThicknessPx - _paddingBottomPx);

        MarkAllPathsDirty();
    }

    private void EnsureChromePaths()
    {
        if (!_chromePathDirty)
        {
            return;
        }

        _chromePathDirty = false;
        _backgroundPath.Reset();
        _borderPath.Reset();

        if (Width <= 0 || Height <= 0 || _borderInnerRect.Width() <= 0 || _borderInnerRect.Height() <= 0)
        {
            return;
        }

        var halfStroke = _borderThicknessPx / 2f;

        BuildAdjustedRadii(_cornerRadiiPx, _drawCornerRadiiPx, halfStroke);

        _backgroundPath.AddRoundRect(_borderInnerRect, _drawCornerRadiiPx, Path.Direction.Cw);
        _backgroundPath.Close();

        if (_borderThicknessPx <= 0)
        {
            return;
        }

        _borderPath.AddRoundRect(_borderInnerRect, _drawCornerRadiiPx, Path.Direction.Cw);
        _borderPath.Close();
    }

    private void EnsureContentClipPath()
    {
        if (!_contentClipPathDirty)
        {
            return;
        }

        _contentClipPathDirty = false;
        _contentClipPath.Reset();

        if (_contentClipRect.Width() <= 0 || _contentClipRect.Height() <= 0)
        {
            return;
        }

        BuildAdjustedRadii(_cornerRadiiPx, _contentCornerRadiiPx, _borderThicknessPx);

        _contentClipPath.AddRoundRect(_contentClipRect, _contentCornerRadiiPx, Path.Direction.Cw);
        _contentClipPath.Close();
    }

    private void EnsureOverlayBorderPath()
    {
        if (!_overlayBorderPathDirty)
        {
            return;
        }

        _overlayBorderPathDirty = false;
        _overlayBorderPath.Reset();

        if (!_isOverlayBorderVisible ||
            _overlayBorderThicknessPx <= 0 ||
            Width <= 0 ||
            Height <= 0)
        {
            return;
        }

        var halfStroke = _overlayBorderThicknessPx / 2f;

        _overlayBorderRect.Set(
            _borderOuterRect.Left + halfStroke,
            _borderOuterRect.Top + halfStroke,
            _borderOuterRect.Right - halfStroke,
            _borderOuterRect.Bottom - halfStroke);

        if (_overlayBorderRect.Width() <= 0 || _overlayBorderRect.Height() <= 0)
        {
            return;
        }

        BuildAdjustedRadii(_cornerRadiiPx, _overlayCornerRadiiPx, halfStroke);

        _overlayBorderPath.AddRoundRect(_overlayBorderRect, _overlayCornerRadiiPx, Path.Direction.Cw);
        _overlayBorderPath.Close();
    }

    private void MarkAllPathsDirty()
    {
        MarkChromePathDirty();
        MarkContentClipPathDirty();
        MarkOverlayBorderPathDirty();
    }

    private void MarkChromePathDirty()
    {
        _chromePathDirty = true;
    }

    private void MarkContentClipPathDirty()
    {
        _contentClipPathDirty = true;
    }

    private void MarkOverlayBorderPathDirty()
    {
        _overlayBorderPathDirty = true;
    }

    private void MeasureShadowChild(int measuredWidth, int measuredHeight)
    {
        if (!_hasShadow)
        {
            _shadowView.Measure(
                MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Exactly),
                MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Exactly));
            return;
        }

        var shadowWidth = measuredWidth + _shadowInsetLeft + _shadowInsetRight;
        var shadowHeight = measuredHeight + _shadowInsetTop + _shadowInsetBottom;

        _shadowView.Measure(
            MeasureSpec.MakeMeasureSpec(shadowWidth, MeasureSpecMode.Exactly),
            MeasureSpec.MakeMeasureSpec(shadowHeight, MeasureSpecMode.Exactly));
    }

    private void LayoutShadowChild(int width, int height)
    {
        if (!_hasShadow)
        {
            _shadowView.Layout(0, 0, 0, 0);
            return;
        }

        var shadowLeft = -_shadowInsetLeft;
        var shadowTop = -_shadowInsetTop;
        var shadowRight = width + _shadowInsetRight;
        var shadowBottom = height + _shadowInsetBottom;

        _shadowView.Layout(shadowLeft, shadowTop, shadowRight, shadowBottom);
    }

    private int GetBodyHorizontal()
    {
        return (int)Math.Ceiling(_borderThicknessPx * 2f)
             + (int)Math.Ceiling(_paddingLeftPx + _paddingRightPx);
    }

    private int GetBodyVertical()
    {
        return (int)Math.Ceiling(_borderThicknessPx * 2f)
             + (int)Math.Ceiling(_paddingTopPx + _paddingBottomPx);
    }

    private LayoutAlignment GetChildHorizontalAlignment()
    {
        return _virtualView?.Content?.HorizontalOptions.Alignment ?? LayoutAlignment.Fill;
    }

    private LayoutAlignment GetChildVerticalAlignment()
    {
        return _virtualView?.Content?.VerticalOptions.Alignment ?? LayoutAlignment.Fill;
    }

    private FastBorderNativeUpdateResult UpdateShadow(FastBorder view, float density)
    {
        var shadow = view.Shadow;

        if (shadow is null)
        {
            if (!_hasShadow &&
                AreClose(_shadowRadiusPx, 0) &&
                AreClose(_shadowDxPx, 0) &&
                AreClose(_shadowDyPx, 0) &&
                _shadowColor == Android.Graphics.Color.Transparent &&
                _shadowInsetLeft == 0 &&
                _shadowInsetTop == 0 &&
                _shadowInsetRight == 0 &&
                _shadowInsetBottom == 0)
            {
                return FastBorderNativeUpdateResult.None;
            }

            _hasShadow = false;
            _shadowRadiusPx = 0;
            _shadowDxPx = 0;
            _shadowDyPx = 0;
            _shadowColor = Android.Graphics.Color.Transparent;

            _shadowInsetLeft = 0;
            _shadowInsetTop = 0;
            _shadowInsetRight = 0;
            _shadowInsetBottom = 0;
            return FastBorderNativeUpdateResult.Draw | FastBorderNativeUpdateResult.Layout;
        }

        var shadowRadiusPx = Math.Max(1f, (float)shadow.Radius * density);
        var shadowDxPx = (float)shadow.Offset.X * density;
        var shadowDyPx = (float)shadow.Offset.Y * density;
        var shadowColor = ResolveShadowColor(shadow);

        var safety = (int)Math.Ceiling(shadowRadiusPx * 1.25f);

        var shadowInsetLeft = (int)Math.Ceiling(Math.Max(0, -shadowDxPx)) + safety;
        var shadowInsetTop = (int)Math.Ceiling(Math.Max(0, -shadowDyPx)) + safety;
        var shadowInsetRight = (int)Math.Ceiling(Math.Max(0, shadowDxPx)) + safety;
        var shadowInsetBottom = (int)Math.Ceiling(Math.Max(0, shadowDyPx)) + safety;

        var layoutChanged = !_hasShadow ||
                            !AreClose(_shadowRadiusPx, shadowRadiusPx) ||
                            !AreClose(_shadowDxPx, shadowDxPx) ||
                            !AreClose(_shadowDyPx, shadowDyPx) ||
                            _shadowInsetLeft != shadowInsetLeft ||
                            _shadowInsetTop != shadowInsetTop ||
                            _shadowInsetRight != shadowInsetRight ||
                            _shadowInsetBottom != shadowInsetBottom;

        var colorChanged = _shadowColor != shadowColor;

        if (!layoutChanged && !colorChanged)
        {
            return FastBorderNativeUpdateResult.None;
        }

        _hasShadow = true;
        _shadowRadiusPx = shadowRadiusPx;
        _shadowDxPx = shadowDxPx;
        _shadowDyPx = shadowDyPx;
        _shadowColor = shadowColor;

        _shadowInsetLeft = shadowInsetLeft;
        _shadowInsetTop = shadowInsetTop;
        _shadowInsetRight = shadowInsetRight;
        _shadowInsetBottom = shadowInsetBottom;

        return layoutChanged
            ? FastBorderNativeUpdateResult.Draw | FastBorderNativeUpdateResult.Layout
            : FastBorderNativeUpdateResult.Draw;
    }

    private void UpdateShadowView()
    {
        _shadowView.Update(
            _hasShadow,
            _cornerRadiiPx,
            _shadowRadiusPx,
            _shadowDxPx,
            _shadowDyPx,
            _shadowColor,
            _shadowInsetLeft,
            _shadowInsetTop,
            _shadowInsetRight,
            _shadowInsetBottom);
    }

    private bool TryGetRequestedWidthPx(out int widthPx)
    {
        widthPx = 0;

        var widthRequest = _virtualView?.Content?.WidthRequest ?? -1;
        if (widthRequest < 0)
        {
            return false;
        }

        widthPx = Math.Max(0, (int)Math.Round(widthRequest * _density));
        return true;
    }

    private bool TryGetRequestedHeightPx(out int heightPx)
    {
        heightPx = 0;

        var heightRequest = _virtualView?.Content?.HeightRequest ?? -1;
        if (heightRequest < 0)
        {
            return false;
        }

        heightPx = Math.Max(0, (int)Math.Round(heightRequest * _density));
        return true;
    }

    private bool SetCornerRadiiPx(CornerRadius cornerRadius, float density)
    {
        var topLeft = Math.Max(0f, (float)cornerRadius.TopLeft * density);
        var topRight = Math.Max(0f, (float)cornerRadius.TopRight * density);
        var bottomLeft = Math.Max(0f, (float)cornerRadius.BottomLeft * density);
        var bottomRight = Math.Max(0f, (float)cornerRadius.BottomRight * density);

        if (AreClose(_cornerRadiiPx[0], topLeft) &&
            AreClose(_cornerRadiiPx[2], topRight) &&
            AreClose(_cornerRadiiPx[4], bottomRight) &&
            AreClose(_cornerRadiiPx[6], bottomLeft))
        {
            return false;
        }

        // Android expects:
        // top-left-x, top-left-y,
        // top-right-x, top-right-y,
        // bottom-right-x, bottom-right-y,
        // bottom-left-x, bottom-left-y.
        _cornerRadiiPx[0] = topLeft;
        _cornerRadiiPx[1] = topLeft;

        _cornerRadiiPx[2] = topRight;
        _cornerRadiiPx[3] = topRight;

        _cornerRadiiPx[4] = bottomRight;
        _cornerRadiiPx[5] = bottomRight;

        _cornerRadiiPx[6] = bottomLeft;
        _cornerRadiiPx[7] = bottomLeft;

        return true;
    }

    private static bool AreClose(float first, float second)
    {
        return Math.Abs(first - second) < 0.001f;
    }

    private static void BuildAdjustedRadii(float[] source, float[] target, float reduction)
    {
        for (var i = 0; i < source.Length; i++)
        {
            target[i] = Math.Max(0f, source[i] - reduction);
        }
    }

    private static Android.Graphics.Color ResolveBackgroundColor(FastBorder view)
    {
        if (view.Background is SolidColorBrush solidBrush && solidBrush.Color is not null)
        {
            return solidBrush.Color.ToPlatform();
        }

        if (view.BackgroundColor is not null)
        {
            return view.BackgroundColor.ToPlatform();
        }

        return Android.Graphics.Color.Transparent;
    }

    private static Android.Graphics.Color ResolveShadowColor(Shadow shadow)
    {
        Android.Graphics.Color baseColor;

        if (shadow.Brush is SolidColorBrush solidBrush && solidBrush.Color is not null)
        {
            baseColor = solidBrush.Color.ToPlatform();
        }
        else
        {
            baseColor = Android.Graphics.Color.Black;
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

        var alpha = (int)Math.Round(255 * opacity);

        return new Android.Graphics.Color(baseColor.R, baseColor.G, baseColor.B, alpha);
    }
}
