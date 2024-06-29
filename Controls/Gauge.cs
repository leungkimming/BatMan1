﻿//https://github.com/mono/SkiaSharp/issues/2410
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace BatMan2.Controls
{
    public class Gauge : SKCanvasView
    {
        public Gauge() {
        }
        #region Properties
        // Properties for the Values
        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create("Value", typeof(float), typeof(Gauge), 0.0f);

        public float Value
        {
            get { return (float)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly BindableProperty StartValueProperty =
            BindableProperty.Create("StartValue", typeof(float), typeof(Gauge), 0.0f);

        public float StartValue
        {
            get { return (float)GetValue(StartValueProperty); }
            set { SetValue(StartValueProperty, value); }
        }

        public static readonly BindableProperty EndValueProperty =
            BindableProperty.Create("EndValue", typeof(float), typeof(Gauge), 100.0f);

        public float EndValue
        {
            get { return (float)GetValue(EndValueProperty); }
            set { SetValue(EndValueProperty, value); }
        }

        public static readonly BindableProperty AlertValueProperty =
            BindableProperty.Create("AlertValue", typeof(float), typeof(Gauge), 100.0f);

        public float AlertValue
        {
            get { return (float)GetValue(AlertValueProperty); }
            set { SetValue(AlertValueProperty, value); }
        }

        public static readonly BindableProperty HighlightRangeStartValueProperty =
            BindableProperty.Create("HighlightRangeStartValue", typeof(float), typeof(Gauge), 0.0f);

        public float HighlightRangeStartValue
        {
            get { return (float)GetValue(HighlightRangeStartValueProperty); }
            set { SetValue(HighlightRangeStartValueProperty, value); }
        }

        public static readonly BindableProperty HighlightRangeEndValueProperty =
            BindableProperty.Create("HighlightRangeEndValue", typeof(float), typeof(Gauge), 100.0f);

        public float HighlightRangeEndValue
        {
            get { return (float)GetValue(HighlightRangeEndValueProperty); }
            set { SetValue(HighlightRangeEndValueProperty, value); }
        }

        // Properties for the Colors
        public static readonly BindableProperty GaugeLineColorProperty =
            BindableProperty.Create("GaugeLineColor", typeof(Color), typeof(Gauge), Color.FromHex("#70CBE6"));

        public Color GaugeLineColor
        {
            get { return (Color)GetValue(GaugeLineColorProperty); }
            set { SetValue(GaugeLineColorProperty, value); }
        }

        public static readonly BindableProperty ValueColorProperty =
            BindableProperty.Create("ValueColor", typeof(Color), typeof(Gauge), Color.FromHex("FF9A52"));

        public Color ValueColor
        {
            get { return (Color)GetValue(ValueColorProperty); }
            set { SetValue(ValueColorProperty, value); }
        }

        public static readonly BindableProperty RangeColorProperty =
            BindableProperty.Create("RangeColor", typeof(Color), typeof(Gauge), Color.FromHex("#E6F4F7"));

        public Color RangeColor
        {
            get { return (Color)GetValue(RangeColorProperty); }
            set { SetValue(RangeColorProperty, value); }
        }

        public static readonly BindableProperty NeedleColorProperty =
           BindableProperty.Create("NeedleColor", typeof(Color), typeof(Gauge), Color.FromRgb(252, 18, 30));

        public Color NeedleColor
        {
            get { return (Color)GetValue(NeedleColorProperty); }
            set { SetValue(NeedleColorProperty, value); }
        }

        // Properties for the Units

        public static readonly BindableProperty UnitsTextProperty =
            BindableProperty.Create("UnitsText", typeof(string), typeof(Gauge), "");

        public string UnitsText
        {
            get { return (string)GetValue(UnitsTextProperty); }
            set { SetValue(UnitsTextProperty, value); }
        }

        public static readonly BindableProperty ValueFontSizeProperty =
           BindableProperty.Create("ValueFontSize", typeof(float), typeof(Gauge), 33f);

        public float ValueFontSize
        {
            get { return (float)GetValue(ValueFontSizeProperty); }
            set { SetValue(ValueFontSizeProperty, value); }
        }

        public static readonly BindableProperty ValuePrecisionProperty =
           BindableProperty.Create("ValuePrecision", typeof(string), typeof(Gauge), "");

        public string Valueprecision
        {
            get { return (string)GetValue(ValuePrecisionProperty); }
            set { SetValue(ValuePrecisionProperty, value); }
        }
        #endregion

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var canvas = e.Surface.Canvas;
            canvas.Clear();

            int width = e.Info.Width;
            int height = e.Info.Height;

            SKPaint backPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.WhiteSmoke,
            };

            canvas.DrawRect(new SKRect(0, 0, width, height), backPaint);

            canvas.Save();

            canvas.Translate(width / 2, height / 2);
            canvas.Scale(Math.Min(width / 210f * 2.4f, height / 520f * 2.4f));
            SKPoint center = new SKPoint(0, 0);

            var rect = new SKRect(-100, -100, 100, 100);

            // Add a buffer for the rectangle
            //rect.Inflate(-10, -10);


            SKPaint GaugePointPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = ValueColor.ToSKColor()
            };

            SKPaint HighlightRangePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = RangeColor.ToSKColor()
            };


            // Draw the range of values

            var rangeStartAngle = AmountToAngle(HighlightRangeStartValue);
            var rangeEndAngle = AmountToAngle(HighlightRangeEndValue);
            var angleDistance = rangeEndAngle - rangeStartAngle;

            using (SKPath path = new SKPath())
            {
                path.AddArc(rect, rangeStartAngle, angleDistance);
                path.LineTo(center);
                canvas.DrawPath(path, HighlightRangePaint);
            }

            // Draw the main gauge line/arc
            var startAngle = 135;
            var sweepAngle = 270f * ValueToAmount(AlertValue) / 100f;
            SKColor P1color, P2color;

            if (sweepAngle > 270f / 2f)
            {
                P1color = SKColors.Green;
                P2color = SKColors.Red;
            } else
            {
                P1color = SKColors.Red;
                P2color = SKColors.Green;
            }

            SKPaint GaugeMainLinePaintP1 = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = P1color,
                StrokeWidth = 10
            };

            using (SKPath path = new SKPath())
            {
                path.AddArc(rect, startAngle, sweepAngle);
                canvas.DrawPath(path, GaugeMainLinePaintP1);
            }

            //Sector2
            SKPaint GaugeMainLinePaintP2 = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = P2color,
                StrokeWidth = 10
            };

            using (SKPath path = new SKPath())
            {
                path.AddArc(rect, 135f + sweepAngle, 270f - sweepAngle);
                canvas.DrawPath(path, GaugeMainLinePaintP2);
            }

            //Draw Needle
            DrawNeedle(canvas, ValueToAmount(Value));

            //Draw Screw
            SKPaint NeedleScrewPaint = new SKPaint()
            {
                IsAntialias = true,
                Shader = SKShader.CreateRadialGradient(center, width / 60, new SKColor[]
               { new SKColor(171, 171, 171), SKColors.White }, new float[] { 0.05f, 0.9f }, SKShaderTileMode.Mirror)
            };

            canvas.DrawCircle(center, width / 60, NeedleScrewPaint);

            SKPaint paint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = new SKColor(81, 84, 89).WithAlpha(100),
                StrokeWidth = 1f
            };

            canvas.DrawCircle(center, width / 60, paint);

            // Draw the Units of Measurement Text on the display
            SKPaint textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black
            };

            float textWidth = textPaint.MeasureText(UnitsText);
            textPaint.TextSize = ValueFontSize / 1.5f; // 12f;

            SKRect textBounds = SKRect.Empty;
            textPaint.MeasureText(UnitsText, ref textBounds);

            float xText = -1 * textBounds.MidX;
            float yText = ((200f + height) / height * 10f) + 105 - textBounds.Height;//95
            //350 97.857
            //510 100.7
            //260 97
            //
            // And draw the text
            canvas.DrawText(UnitsText, xText, yText, textPaint);

            // Draw the Value on the display
            var valueText = Value.ToString(Valueprecision); //You can set F1 or F2 if you need float values
            float valueTextWidth = textPaint.MeasureText(valueText);
            textPaint.TextSize = ValueFontSize;

            textPaint.MeasureText(valueText, ref textBounds);

            xText = -1 * textBounds.MidX;
            yText = ((200f + height) / height * 10f) + 82 - textBounds.Height; //85

            // And draw the text
            canvas.DrawText(valueText, xText, yText, textPaint);
            canvas.Restore();
        }

        float AmountToAngle(float value)
        {
//            return 135f + (value / (EndValue - StartValue)) * 270f;
            return 135f + (value / 100f) * 270f;
        }

        float ValueToAmount(float value)
        {
            return (value - StartValue) * 100f / (EndValue - StartValue);
        }

        void DrawNeedle(SKCanvas canvas, float value)
        {
            float angle = -135f + (value / (100 - 0)) * 270f;
            canvas.Save();
            canvas.RotateDegrees(angle);
            float needleWidth = 6f;
            float needleHeight = 76f;
            float x = 0f, y = 0f;

            SKPaint paint = new SKPaint
            {
                IsAntialias = true,
                Color = NeedleColor.ToSKColor()
            };

            SKPath needleRightPath = new SKPath();
            needleRightPath.MoveTo(x, y);
            needleRightPath.LineTo(x + needleWidth, y);
            needleRightPath.LineTo(x, y - needleHeight);
            needleRightPath.LineTo(x, y);
            needleRightPath.LineTo(x + needleWidth, y);


            SKPath needleLeftPath = new SKPath();
            needleLeftPath.MoveTo(x, y);
            needleLeftPath.LineTo(x - needleWidth, y);
            needleLeftPath.LineTo(x, y - needleHeight);
            needleLeftPath.LineTo(x, y);
            needleLeftPath.LineTo(x - needleWidth, y);


            canvas.DrawPath(needleRightPath, paint);
            canvas.DrawPath(needleLeftPath, paint);
            canvas.Restore();
        }

        protected override void OnPropertyChanged(string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            // Determine when to change. Basically on any of the properties that we've added that affect
            // the visualization, including the size of the control, we'll repaint
            if (propertyName == HighlightRangeEndValueProperty.PropertyName
                || propertyName == HighlightRangeStartValueProperty.PropertyName
                || propertyName == ValueProperty.PropertyName
                || propertyName == WidthProperty.PropertyName
                || propertyName == HeightProperty.PropertyName
                || propertyName == StartValueProperty.PropertyName
                || propertyName == EndValueProperty.PropertyName
                || propertyName == AlertValueProperty.PropertyName
                || propertyName == GaugeLineColorProperty.PropertyName
                || propertyName == ValueColorProperty.PropertyName
                || propertyName == RangeColorProperty.PropertyName
                || propertyName == UnitsTextProperty.PropertyName
                || propertyName == ValuePrecisionProperty.PropertyName)
            {
                InvalidateSurface();
            }
        }
    }
}
