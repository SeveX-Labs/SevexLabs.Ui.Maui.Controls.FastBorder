using Android.Content;
using Android.Graphics;
using Android.Views;
using Paint = Android.Graphics.Paint;
using Path = Android.Graphics.Path;
using RectF = Android.Graphics.RectF;
using View = Android.Views.View;

namespace SevexLabs.Ui.Maui.Controls.FastBorder.Handlers;

internal sealed class FastBorderShadowView : View
{
    private readonly Paint _shadowPaint;
    private readonly Paint _clearPaint;

    private readonly RectF _bodyRect = new();
    private readonly RectF _shadowRect = new();

    private readonly Path _shadowPath = new();
    private readonly Path _clearPath = new();

    private readonly float[] _cornerRadiiPx = new float[8];

    private BlurMaskFilter? _shadowMaskFilter;
    private float _shadowMaskFilterRadiusPx;

    private bool _hasShadow;
    private float _shadowRadiusPx;
    private float _shadowDxPx;
    private float _shadowDyPx;
    private Android.Graphics.Color _shadowColor = Android.Graphics.Color.Transparent;

    private int _insetLeft;
    private int _insetTop;
    private int _insetRight;
    private int _insetBottom;

    public FastBorderShadowView(Context context) : base(context)
    {
        SetWillNotDraw(false);

        _shadowPaint = new Paint(PaintFlags.AntiAlias)
        {
            Dither = true
        };
        _shadowPaint.SetStyle(Paint.Style.Fill);

        _clearPaint = new Paint(PaintFlags.AntiAlias)
        {
            Dither = true
        };
        _clearPaint.SetStyle(Paint.Style.Fill);
        _clearPaint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.Clear));
    }

    public void Update(
        bool hasShadow,
        float[] cornerRadiiPx,
        float shadowRadiusPx,
        float shadowDxPx,
        float shadowDyPx,
        Android.Graphics.Color shadowColor,
        int insetLeft,
        int insetTop,
        int insetRight,
        int insetBottom)
    {
        _hasShadow = hasShadow;

        for (var i = 0; i < _cornerRadiiPx.Length; i++)
        {
            _cornerRadiiPx[i] = i < cornerRadiiPx.Length
                ? Math.Max(0f, cornerRadiiPx[i])
                : 0f;
        }

        _shadowRadiusPx = shadowRadiusPx;
        _shadowDxPx = shadowDxPx;
        _shadowDyPx = shadowDyPx;
        _shadowColor = shadowColor;

        _insetLeft = insetLeft;
        _insetTop = insetTop;
        _insetRight = insetRight;
        _insetBottom = insetBottom;

        if (!hasShadow)
        {
            ClearShadowMaskFilter();
        }

        Visibility = hasShadow ? ViewStates.Visible : ViewStates.Gone;
        SetLayerType(hasShadow ? LayerType.Software : LayerType.Hardware, null);

        Invalidate();
    }

    public void Reset()
    {
        _hasShadow = false;
        _shadowPath.Reset();
        _clearPath.Reset();
        ClearShadowMaskFilter();
        Visibility = ViewStates.Gone;
        SetLayerType(LayerType.Hardware, null);
    }

    protected override void OnDraw(Canvas canvas)
    {
        base.OnDraw(canvas);

        if (!_hasShadow || Width <= 0 || Height <= 0)
        {
            return;
        }

        _bodyRect.Set(
            _insetLeft,
            _insetTop,
            Width - _insetRight,
            Height - _insetBottom);

        if (_bodyRect.Width() <= 0 || _bodyRect.Height() <= 0)
        {
            return;
        }

        _shadowRect.Set(
            _bodyRect.Left + _shadowDxPx,
            _bodyRect.Top + _shadowDyPx,
            _bodyRect.Right + _shadowDxPx,
            _bodyRect.Bottom + _shadowDyPx);

        var saveLayerCount = canvas.SaveLayer(0, 0, Width, Height, null);

        _shadowPaint.Color = _shadowColor;
        _shadowPaint.SetMaskFilter(GetShadowMaskFilter());

        _shadowPath.Reset();
        _shadowPath.AddRoundRect(_shadowRect, _cornerRadiiPx, Path.Direction.Cw);
        _shadowPath.Close();

        canvas.DrawPath(_shadowPath, _shadowPaint);

        _clearPath.Reset();
        _clearPath.AddRoundRect(_bodyRect, _cornerRadiiPx, Path.Direction.Cw);
        _clearPath.Close();

        // Rimuove l'interno, così con background trasparente non vedi shadow nel body.
        canvas.DrawPath(_clearPath, _clearPaint);

        _shadowPaint.SetMaskFilter(null);

        canvas.RestoreToCount(saveLayerCount);
    }

    private BlurMaskFilter GetShadowMaskFilter()
    {
        if (_shadowMaskFilter is not null && AreClose(_shadowMaskFilterRadiusPx, _shadowRadiusPx))
        {
            return _shadowMaskFilter;
        }

        ClearShadowMaskFilter();

        _shadowMaskFilter = new BlurMaskFilter(_shadowRadiusPx, BlurMaskFilter.Blur.Normal);
        _shadowMaskFilterRadiusPx = _shadowRadiusPx;

        return _shadowMaskFilter;
    }

    private void ClearShadowMaskFilter()
    {
        _shadowPaint.SetMaskFilter(null);
        _shadowMaskFilter?.Dispose();
        _shadowMaskFilter = null;
        _shadowMaskFilterRadiusPx = 0;
    }

    private static bool AreClose(float first, float second)
    {
        return Math.Abs(first - second) < 0.001f;
    }
}
