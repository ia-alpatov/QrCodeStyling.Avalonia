using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Gma.QrCodeNet.Encoding;

namespace QrCodeStyling.Avalonia
{
    /// <summary>
    /// Avalonia QR Code control that builds a single StreamGeometry using a StreamGeometryContext.
    /// Uses NonZero fill with winding control to carve data and markers out of a solid foreground,
    /// which allows any brush (including gradients) to paint the whole code consistently.
    /// Follows ISO 18004 sizing concepts (quiet zone = 4 modules when enabled).
    /// Spec reference: https://www.swisseduc.ch/informatik/theoretische_informatik/qr_codes/docs/qr_standard.pdf
    /// </summary>
    public class QrCode : Control
    {
        #region Properties

        /// <summary>
        /// Property for the Background brush (i.e. the area that has no data)
        /// </summary>
        public static readonly StyledProperty<IBrush?>
            BackgroundProperty = Border.BackgroundProperty.AddOwner<QrCode>();

        /// <summary>
        /// Property for the Foreground brush (i.e. the actual data)
        /// </summary>
        public static readonly StyledProperty<IBrush?> ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner<QrCode>();

        /// <summary>
        /// Property indicating how rounded the corners will be
        /// </summary>
        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner<QrCode>();

        /// <summary>
        /// Property indicating the Quiet Zone (distance between the edge of the control and where the data actually starts)
        /// 
        /// Note: The Quiet Zone (aka Padding) is defined in the QC Code standard (ISO 18004) as the width of 4 modules on all
        /// sides, but is implemented separately in this control.  Official support may wish to remove this property as adjusting
        /// it will technically make the generated QRCodes "non-standard".  This implementation does not currently concern itself
        /// with this as the code itself it not meant for public consumption.
        /// </summary>
        public static readonly StyledProperty<Thickness> PaddingProperty = Decorator.PaddingProperty.AddOwner<QrCode>();

        /// <summary>
        /// Property indicating whether the Quiet Zone of 4 modules should be added to the QR Code as additional padding.  Default: True
        ///
        /// Note: Disabling the Quiet Zone makes the generated QRCodes "non-standard" according to the ISO 18004 standard.
        /// The padding created by the Quiet Zone depends on the module size and therefore on the amount of data. This can be
        /// disabled and a fixed <see cref="Padding"/> can be set instead to have more control over the layout. 
        /// </summary>
        public static readonly StyledProperty<bool> IsQuietZoneEnabledProperty =
            AvaloniaProperty.Register<QrCode, bool>(nameof(IsQuietZoneEnabled), true);

        /// <summary>
        /// Property indicating the Error Correction Code of the generated data.  Default: Medium
        ///
        /// Note: See <see cref="EccLevel" /> for the specific definitions of each value.
        /// </summary>
        public static readonly StyledProperty<EccLevel> ErrorCorrectionProperty =
            AvaloniaProperty.Register<QrCode, EccLevel>(nameof(ErrorCorrection), EccLevel.Medium);

        /// <summary>
        /// Property for the data represented in the QRCode
        /// </summary>
        public static readonly StyledProperty<string?> DataProperty =
            AvaloniaProperty.Register<QrCode, string?>(nameof(Data));

        /// <summary>Style for data modules (square, circle, triangle).</summary>
        public static readonly StyledProperty<DotType> DotProperty =
            AvaloniaProperty.Register<QrCode, DotType>(nameof(Dot), DotType.Square);

        /// <summary>Optional image drawn at the center of the QR.</summary>
        public static readonly StyledProperty<IImage?> ImageProperty =
            AvaloniaProperty.Register<QrCode, IImage?>(nameof(Image), null);

        /// <summary>Style for finder pattern rings (corner markers).</summary>
        public static readonly StyledProperty<CornerDotsType> CornerDotsProperty =
            AvaloniaProperty.Register<QrCode, CornerDotsType>(nameof(CornerDots), CornerDotsType.Square);

        /// <summary>Image size as a fraction of the inner data bounds’ smaller side. [0..1]</summary>
        public static readonly StyledProperty<double> ImageScaleProperty =
            AvaloniaProperty.Register<QrCode, double>(nameof(ImageScale), 0.25);

        /// <summary>Mask padding around the image measured in module units. Modules under the mask are cleared.</summary>
        public static readonly StyledProperty<double> ImagePaddingModulesProperty =
            AvaloniaProperty.Register<QrCode, double>(nameof(ImagePaddingModules), 0.2);

        /// <inheritdoc cref="BackgroundProperty" />
        public IBrush Background
        {
            get => GetValue(BackgroundProperty) ?? Brushes.White;
            set => SetValue(BackgroundProperty, value);
        }

        /// <inheritdoc cref="ForegroundProperty" />
        public IBrush Foreground
        {
            get => GetValue(ForegroundProperty) ?? Brushes.Black;
            set => SetValue(ForegroundProperty, value);
        }

        /// <inheritdoc cref="CornerRadiusProperty" />
        public CornerRadius CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <inheritdoc cref="PaddingProperty" />
        public Thickness Padding
        {
            get => GetValue(PaddingProperty);
            set => SetValue(PaddingProperty, value);
        }

        /// <inheritdoc cref="IsQuietZoneEnabledProperty" />
        public bool IsQuietZoneEnabled
        {
            get => GetValue(IsQuietZoneEnabledProperty);
            set => SetValue(IsQuietZoneEnabledProperty, value);
        }

        /// <inheritdoc cref="ErrorCorrectionProperty" />
        public EccLevel ErrorCorrection
        {
            get => GetValue(ErrorCorrectionProperty);
            set => SetValue(ErrorCorrectionProperty, value);
        }

        /// <inheritdoc cref="DataProperty" />
        public string? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        /// <inheritdoc cref="DotProperty" />
        public DotType Dot
        {
            get => GetValue(DotProperty);
            set => SetValue(DotProperty, value);
        }

        /// <inheritdoc cref="ImageProperty" />
        public IImage? Image
        {
            get => GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        /// <inheritdoc cref="CornerDotsProperty" />
        public CornerDotsType CornerDots
        {
            get => GetValue(CornerDotsProperty);
            set => SetValue(CornerDotsProperty, value);
        }

        /// <inheritdoc cref="ImageScaleProperty" />
        public double ImageScale
        {
            get => GetValue(ImageScaleProperty);
            set => SetValue(ImageScaleProperty, value);
        }

        /// <inheritdoc cref="ImagePaddingModulesProperty" />
        public double ImagePaddingModules
        {
            get => GetValue(ImagePaddingModulesProperty);
            set => SetValue(ImagePaddingModulesProperty, value);
        }

        #endregion

        /// <summary>
        /// Engine to actually calculate the bit matrix of the QRCode.  Currently a Nuget package, but official support may wish to implement and remove such dependency 
        /// </summary>
        private static readonly QrEncoder QrCodeGenerator = new();

        // CANCEL crossfade on update or detach
        private CancellationTokenSource? _transitionCts;

        // replace Hashtable with Dictionary so we can TrimExcess or recreate
        private Dictionary<int, bool> _setBitsCache = new();


        /// <summary>
        /// A cache of the last encoded QRCode.  This is used to reuse the last generated data whenever a style property like Width, Height or Padding was changed.
        /// </summary>
        private Gma.QrCodeNet.Encoding.QrCode? _encodedQrCode;

        // QRCode specs mandate a standard 4-symbol-sized space on each side of the data.  We support custom Padding and will ignore this zone when processing
        private int QuietZoneCount => IsQuietZoneEnabled ? 4 : 0;
        private int QuietMargin => QuietZoneCount * 2;

        /// <summary>Geometry and opacity of the previous frame used during crossfade.</summary>
        private (StreamGeometry, double)? _oldQrCodeGeometry;

        /// <summary>Geometry and opacity of the current frame used during crossfade.</summary>
        private (StreamGeometry, double)? _qrCodeGeometry;

        private Rect? _imageRect;
        private Rect? _imageMaskRect;

        public QrCode()
        {
            // Rendering-affecting properties that do not require recomputing the bit matrix.
            // Geometry is rebuilt on layout-affecting changes in OnPropertyChanged.
            AffectsRender<QrCode>(BackgroundProperty, ForegroundProperty, CornerRadiusProperty, WidthProperty,
                HeightProperty);

            // Opacity transition example. Crossfade is implemented manually for geometry swaps.
            Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = OpacityProperty,
                    Duration = TimeSpan.FromSeconds(1),
                }
            };
        }

        /// <summary>
        /// Handles property changes. Re-encodes data only when required (Data or ErrorCorrection changed).
        /// Rebuilds StreamGeometry for layout/appearance changes. Starts a geometry crossfade on updates.
        /// </summary>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            // When any property is changed, we will recalculate the bit matrix and rerender the control
            // For properties that do not require the data to be reprocessed, see the constructor.

            // We can only reprocess the data when data is available to reprocess...
            if (Data == null)
                return;

            // Invalidates the cached QRCode if needed.  We do not need recreate the bit matrix for layout changes.
            switch (change.Property.Name)
            {
                // Error Correction change requires the data to be reprocessed to recalculate the new bit matrix.  This is unavoidable.
                case nameof(ErrorCorrection):
                // A change in data obviously indicates the need to update the bit matrix    
                case nameof(Data):
                    _encodedQrCode = null;
                    break;
            }

            // Generating the QRCode bit matrix if needed.
            if (_encodedQrCode is null)
            {
                // Cache of matrix lookups with exclusion of finder patterns. Key = Hash(x,y). Reduces repeated checks per frame.
                _setBitsCache = new Dictionary<int, bool>(capacity: 0);

                QrCodeGenerator.ErrorCorrectionLevel = ToQrCoderEccLevel(ErrorCorrection);
                _encodedQrCode = QrCodeGenerator.Encode(Data);
            }

            switch (change.Property.Name)
            {
                // Padding and size requires the geometry paths to be adjusted to match the new locations. ToDo: Can this be simulated with a scale to enhance performance?
                case nameof(Padding):
                case nameof(Width):
                case nameof(Height):
                case nameof(IsQuietZoneEnabled):
                case nameof(ErrorCorrection):
                case nameof(Data):
                case nameof(Dot):
                case nameof(CornerDots):
                case nameof(Image):
                case nameof(ImageScale):
                case nameof(ImagePaddingModules):
                    OnLayoutChanged(_encodedQrCode);

                    // This is hard coded for now as I'm sure there is a better and more "Avalonia" way to transition between renders.
                    // Eventually, it may be a property of some sort.
                    _transitionCts?.Cancel();
                    _transitionCts = new CancellationTokenSource();
                    StartCrossfadeAsync(_transitionCts.Token);

                    break;
            }
        }

        private async void StartCrossfadeAsync(System.Threading.CancellationToken ct)
        {
            try
            {
                // fade new geometry from 0→1; if updated again, this gets cancelled
                while (_qrCodeGeometry is (_, < 1))
                {
                    ct.ThrowIfCancellationRequested();

                    if (_qrCodeGeometry is var (g, opacity))
                        _qrCodeGeometry = (g, Math.Min(1, opacity + 0.1));

                    InvalidateVisual();
                    await Task.Delay(30, ct);
                }

                // old geometry no longer needed
                _oldQrCodeGeometry = null;
                InvalidateVisual();
            }
            catch (OperationCanceledException)
            {
                // ensure we do not keep old refs on cancel
                _oldQrCodeGeometry = null;
            }
        }


        /// <summary>
        /// Computes sizes and builds a single StreamGeometry for the entire QR.
        /// Uses NonZero fill with winding to create “holes” for modules and marker centers:
        /// - First figure: full control bounds, clockwise (solid).
        /// - Data modules: counter-clockwise (subtract).
        /// - Finder pattern outer ring: counter-clockwise (subtract).
        /// - Finder pattern inner ring: clockwise (add).
        /// - Finder pattern center: counter-clockwise (subtract).
        /// Also creates an optional rectangular mask to clear modules under the center image.
        /// </summary>
        private void OnLayoutChanged(Gma.QrCodeNet.Encoding.QrCode qrCodeData)
        {
            var bounds = new Rect(0, 0, Width, Height);
            var matrix = qrCodeData.Matrix;
            var columnCount = matrix.Width + QuietMargin;
            var rowCount = matrix.Height + QuietMargin;

            var symbolSize = new Size(
                (Width  - Padding.Left - Padding.Right)  / columnCount,
                (Height - Padding.Top  - Padding.Bottom) / rowCount
            );

            var dataBounds = new Rect(0, 0, Width, Height)
                .Deflate(Padding)
                .Deflate(new Thickness(symbolSize.Width * QuietZoneCount,
                    symbolSize.Height * QuietZoneCount));

            _imageRect = null;
            _imageMaskRect = null;

            if (Image is not null)
            {
                var clampedScale = Math.Clamp(ImageScale, 0.0, 1.0);
                var side = Math.Min(dataBounds.Width, dataBounds.Height) * clampedScale;

                var cx = dataBounds.Left + dataBounds.Width  / 2.0;
                var cy = dataBounds.Top  + dataBounds.Height / 2.0;

                var dest = new Rect(cx - side / 2.0, cy - side / 2.0, side, side);

                var pads = Math.Max(0.0, ImagePaddingModules);
                var padX = symbolSize.Width  * pads;
                var padY = symbolSize.Height * pads;

                _imageRect = dest;
                _imageMaskRect = dest.Inflate(new Thickness(padX, padY, padX, padY));
            }

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                AddPositionDetectionPattern(ctx, bounds, symbolSize);

                for (var row = 0; row < matrix.Height; row++)
                    ProcessRow(ctx, matrix, row, symbolSize);
            }

            _oldQrCodeGeometry = _qrCodeGeometry;
            _qrCodeGeometry = (geometry, 0);
        }


        /// <summary>
        /// Walks one matrix row and appends figures for set modules, honoring the image mask,
        /// and computing neighbor flags for dot shaping.
        /// </summary>
        /// <param name="geometry">Geometry of the QR Code</param>
        /// <param name="bitMatrix">The bit matrix being processed</param>
        /// <param name="row">The row to process</param>
        /// <param name="symbolSize">The calculated size of each symbol</param>
        private void ProcessRow(StreamGeometryContext geometry, BitMatrix bitMatrix, int row, Size symbolSize)
        {
            for (var column = 0; column < bitMatrix.Width; column++)
            {
                if (!IsValid(bitMatrix, column, row))
                    continue;

                var bounds = new Rect(
                    (column + QuietZoneCount) * symbolSize.Width + Padding.Left,
                    (row + QuietZoneCount) * symbolSize.Height + Padding.Top,
                    symbolSize.Width,
                    symbolSize.Height
                );

                // Skip modules under the image mask
                if (IntersectsImageMask(bounds))
                    continue;

                // Neighbor checks also treat the mask as empty
                bool left = IsValid(bitMatrix, column - 1, row) && !IntersectsImageMask(new Rect(
                    (column - 1 + QuietZoneCount) * symbolSize.Width + Padding.Left,
                    (row + QuietZoneCount) * symbolSize.Height + Padding.Top,
                    symbolSize.Width, symbolSize.Height));

                bool right = IsValid(bitMatrix, column + 1, row) && !IntersectsImageMask(new Rect(
                    (column + 1 + QuietZoneCount) * symbolSize.Width + Padding.Left,
                    (row + QuietZoneCount) * symbolSize.Height + Padding.Top,
                    symbolSize.Width, symbolSize.Height));

                bool top = IsValid(bitMatrix, column, row - 1) && !IntersectsImageMask(new Rect(
                    (column + QuietZoneCount) * symbolSize.Width + Padding.Left,
                    (row - 1 + QuietZoneCount) * symbolSize.Height + Padding.Top,
                    symbolSize.Width, symbolSize.Height));

                bool bottom = IsValid(bitMatrix, column, row + 1) && !IntersectsImageMask(new Rect(
                    (column + QuietZoneCount) * symbolSize.Width + Padding.Left,
                    (row + QuietZoneCount) * symbolSize.Height + Padding.Top,
                    symbolSize.Width, symbolSize.Height));

                AddModuleFigure(geometry, bounds, left, right, top, bottom, Dot);
            }
        }

        private bool IntersectsImageMask(Rect r) =>
            _imageMaskRect is Rect m && m.Intersects(r);

        private void AddModuleFigure(
            StreamGeometryContext ctx, Rect b,
            bool left, bool right, bool top, bool bottom,
            DotType type)
        {
            switch (type)
            {
                case DotType.Square:
                    AddRect(ctx, b, ccw: false);  
                    return;
                case DotType.Circle:
                    AddCircle(ctx, b, ccw: false);
                    return;
                case DotType.Triangle:
                    AddTriangle(ctx, b, left, right, top, bottom); 
                    return;
                default:
                    AddRect(ctx, b, ccw: false);
                    return;
            }
        }


        private static void AddRect(StreamGeometryContext ctx, Rect b, bool ccw = false)
        {
            // CW: TL -> TR -> BR -> BL
            // CCW: TL -> BL -> BR -> TR
            if (!ccw)
            {
                ctx.BeginFigure(b.TopLeft, true);
                ctx.LineTo(b.TopRight);
                ctx.LineTo(b.BottomRight);
                ctx.LineTo(b.BottomLeft);
            }
            else
            {
                ctx.BeginFigure(b.TopLeft, true);
                ctx.LineTo(b.BottomLeft);
                ctx.LineTo(b.BottomRight);
                ctx.LineTo(b.TopRight);
            }

            ctx.EndFigure(true);
        }

        private static void AddCircle(StreamGeometryContext ctx, Rect b, bool ccw = false)
        {
            var r = new Size(b.Width / 2, b.Height / 2);
            var leftMid = new Point(b.Left, b.Top + r.Height);
            var rightMid = new Point(b.Right, b.Top + r.Height);

            ctx.BeginFigure(leftMid, true);
            if (!ccw)
            {
                ctx.ArcTo(rightMid, r, 0, false, SweepDirection.Clockwise);
                ctx.ArcTo(leftMid, r, 0, false, SweepDirection.Clockwise);
            }
            else
            {
                ctx.ArcTo(rightMid, r, 0, false, SweepDirection.CounterClockwise);
                ctx.ArcTo(leftMid, r, 0, false, SweepDirection.CounterClockwise);
            }

            ctx.EndFigure(true);
        }

        private static void AddSideRounded(StreamGeometryContext ctx, Rect b, Side side)
        {
            var rx = b.Width / 2;
            var ry = b.Height / 2;

            switch (side)
            {
                case Side.Right:
                {
                    var start = b.TopLeft;
                    ctx.BeginFigure(start, isFilled: true);
                    ctx.LineTo(b.BottomLeft);
                    ctx.LineTo(new Point(b.Left + rx, b.Bottom));
                    ctx.ArcTo(new Point(b.Left + rx, b.Top), new Size(rx, ry), 0, false, SweepDirection.Clockwise);
                    ctx.EndFigure(true);
                    break;
                }
                case Side.Left:
                {
                    var start = new Point(b.Right, b.Top);
                    ctx.BeginFigure(start, isFilled: true);
                    ctx.LineTo(b.BottomRight);
                    ctx.LineTo(new Point(b.Right - rx, b.Bottom));
                    ctx.ArcTo(new Point(b.Right - rx, b.Top), new Size(rx, ry), 0, false, SweepDirection.Clockwise);
                    ctx.EndFigure(true);
                    break;
                }
                case Side.Top:
                {
                    var start = b.BottomLeft;
                    ctx.BeginFigure(start, isFilled: true);
                    ctx.LineTo(b.BottomRight);
                    ctx.LineTo(new Point(b.Right, b.Top + ry));
                    ctx.ArcTo(new Point(b.Left, b.Top + ry), new Size(rx, ry), 0, false, SweepDirection.Clockwise);
                    ctx.EndFigure(true);
                    break;
                }
                case Side.Bottom:
                {
                    var start = b.TopLeft;
                    ctx.BeginFigure(start, isFilled: true);
                    ctx.LineTo(b.TopRight);
                    ctx.LineTo(new Point(b.Right, b.Bottom - ry));
                    ctx.ArcTo(new Point(b.Left, b.Bottom - ry), new Size(rx, ry), 0, false, SweepDirection.Clockwise);
                    ctx.EndFigure(true);
                    break;
                }
            }
        }

        private static void AddCornerRounded(StreamGeometryContext ctx, Rect b, Corner corner, bool extra)

        {
            var r = extra ? Math.Min(b.Width, b.Height) / 1.0 : Math.Min(b.Width, b.Height) / 2.0;
            var R = new Size(r, r);

            switch (corner)
            {
                case Corner.TL:
                {
                    var start = new Point(b.Left, b.Top + r);
                    ctx.BeginFigure(start, isFilled: true);
                    ctx.LineTo(b.BottomLeft);
                    ctx.LineTo(b.BottomRight);
                    ctx.LineTo(b.TopRight);
                    ctx.LineTo(new Point(b.Left + r, b.Top));
                    ctx.ArcTo(start, R, 0, false, SweepDirection.CounterClockwise);
                    ctx.EndFigure(true);
                    break;
                }
                case Corner.TR:
                {
                    var start = new Point(b.Right - r, b.Top);
                    ctx.BeginFigure(start, isFilled: true);
                    ctx.LineTo(b.TopLeft);
                    ctx.LineTo(b.BottomLeft);
                    ctx.LineTo(b.BottomRight);
                    ctx.LineTo(new Point(b.Right, b.Top + r));
                    ctx.ArcTo(start, R, 0, false, SweepDirection.CounterClockwise);
                    ctx.EndFigure(true);
                    break;
                }
                case Corner.BR:
                {
                    var start = new Point(b.Right, b.Bottom - r);
                    ctx.BeginFigure(start, isFilled: true);
                    ctx.LineTo(new Point(b.Right, b.Top));
                    ctx.LineTo(new Point(b.Left, b.Top));
                    ctx.LineTo(new Point(b.Left, b.Bottom));
                    ctx.LineTo(new Point(b.Right - r, b.Bottom));
                    ctx.ArcTo(start, R, 0, false, SweepDirection.CounterClockwise);
                    ctx.EndFigure(true);
                    break;
                }
                case Corner.BL:
                {
                    var start = new Point(b.Left + r, b.Bottom);
                    ctx.BeginFigure(start, isFilled: true);
                    ctx.LineTo(b.BottomRight);
                    ctx.LineTo(b.TopRight);
                    ctx.LineTo(b.TopLeft);
                    ctx.LineTo(new Point(b.Left, b.Bottom - r));
                    ctx.ArcTo(start, R, 0, false, SweepDirection.CounterClockwise);
                    ctx.EndFigure(true);
                    break;
                }
            }
        }

        private static void AddCornersRounded(StreamGeometryContext ctx, Rect b,
            bool horizontal /*true => L/R; false => T/B*/)
        {
            var rx = b.Width / 2;
            var ry = b.Height / 2;

            if (horizontal)
            {
                var start = new Point(b.Left, b.Top);
                ctx.BeginFigure(start, isFilled: true);
                ctx.LineTo(new Point(b.Left, b.Top + ry));
                ctx.ArcTo(new Point(b.Left + rx, b.Bottom), new Size(rx, ry), 0, false, SweepDirection.Clockwise);
                ctx.LineTo(b.BottomRight);
                ctx.LineTo(new Point(b.Right, b.Top + ry));
                ctx.ArcTo(new Point(b.Left + rx, b.Top), new Size(rx, ry), 0, false, SweepDirection.Clockwise);
                ctx.EndFigure(true);
            }
            else
            {
                var start = new Point(b.Left, b.Bottom);
                ctx.BeginFigure(start, isFilled: true);
                ctx.LineTo(new Point(b.Left + rx, b.Bottom));
                ctx.ArcTo(new Point(b.Right, b.Bottom - ry), new Size(rx, ry), 0, false, SweepDirection.Clockwise);
                ctx.LineTo(b.TopRight);
                ctx.LineTo(new Point(b.Left + rx, b.Top));
                ctx.ArcTo(new Point(b.Left, b.Bottom - ry), new Size(rx, ry), 0, false, SweepDirection.Clockwise);
                ctx.EndFigure(true);
            }
        }

        private void AddRounded(StreamGeometryContext ctx, Rect b, bool left, bool right, bool top, bool bottom,
            bool extra)
        {
            int count = (left ? 1 : 0) + (right ? 1 : 0) + (top ? 1 : 0) + (bottom ? 1 : 0);

            if (count == 0)
            {
                AddCircle(ctx, b);
                return;
            }

            if (count > 2 || (left && right) || (top && bottom))
            {
                AddRect(ctx, b);
                return;
            }

            if (count == 2)
            {
                if (left && top)
                {
                    AddCornerRounded(ctx, b, Corner.TL, extra);
                    return;
                }

                if (top && right)
                {
                    AddCornerRounded(ctx, b, Corner.TR, extra);
                    return;
                }

                if (right && bottom)
                {
                    AddCornerRounded(ctx, b, Corner.BR, extra);
                    return;
                }

                AddCornerRounded(ctx, b, Corner.BL, extra);
                return;
            }

            if (top)
            {
                AddSideRounded(ctx, b, Side.Top);
                return;
            }

            if (right)
            {
                AddSideRounded(ctx, b, Side.Right);
                return;
            }

            if (bottom)
            {
                AddSideRounded(ctx, b, Side.Bottom);
                return;
            }

            AddSideRounded(ctx, b, Side.Left);
        }

        private static void AddTriangle(StreamGeometryContext ctx, Rect b, bool left, bool right, bool top, bool bottom)
        {
            int sides = (left ? 1 : 0) + (right ? 1 : 0) + (top ? 1 : 0) + (bottom ? 1 : 0);
            if (sides > 1 || (left && right) || (top && bottom))
            {
                AddRect(ctx, b);
                return;
            }

            var cx = b.Left + b.Width / 2.0;
            var cy = b.Top + b.Height / 2.0;

            if (top)
            {
                ctx.BeginFigure(b.TopLeft, true);
                ctx.LineTo(b.TopRight);
                ctx.LineTo(new Point(b.Right, cy));
                ctx.LineTo(new Point(cx, b.Bottom));
                ctx.LineTo(new Point(b.Left, cy));
                ctx.EndFigure(true);
                return;
            }

            if (left)
            {
                ctx.BeginFigure(b.TopLeft, true);
                ctx.LineTo(new Point(cx, b.Top));
                ctx.LineTo(new Point(b.Right, cy));
                ctx.LineTo(new Point(cx, b.Bottom));
                ctx.LineTo(b.BottomLeft);
                ctx.EndFigure(true);
                return;
            }

            if (right)
            {
                ctx.BeginFigure(new Point(cx, b.Top), true);
                ctx.LineTo(b.TopRight);
                ctx.LineTo(b.BottomRight);
                ctx.LineTo(new Point(cx, b.Bottom));
                ctx.LineTo(new Point(b.Left, cy));
                ctx.EndFigure(true);
                return;
            }

            if (bottom)
            {
                ctx.BeginFigure(new Point(cx, b.Top), true);
                ctx.LineTo(new Point(b.Right, cy));
                ctx.LineTo(b.BottomRight);
                ctx.LineTo(b.BottomLeft);
                ctx.LineTo(new Point(b.Left, cy));
                ctx.EndFigure(true);
                return;
            }

            AddRect(ctx, b);
        }

        private static void AddCornersRounded(PathGeometry g, Rect b,
            bool horizontal /*true => left/right rounded, false => top/bottom rounded*/)
        {
            var rx = b.Width / 2;
            var ry = b.Height / 2;

            if (horizontal)
            {
                var start = new Point(b.Left, b.Top);
                g.Figures!.Add(new PathFigure
                {
                    StartPoint = start,
                    Segments = new PathSegments
                    {
                        new LineSegment { Point = new Point(b.Left, b.Top + ry) },
                        new ArcSegment
                        {
                            Size = new Size(rx, ry), SweepDirection = SweepDirection.Clockwise,
                            Point = new Point(b.Left + rx, b.Bottom)
                        },
                        new LineSegment { Point = b.BottomRight },
                        new LineSegment { Point = new Point(b.Right, b.Top + ry) },
                        new ArcSegment
                        {
                            Size = new Size(rx, ry), SweepDirection = SweepDirection.Clockwise,
                            Point = new Point(b.Left + rx, b.Top)
                        }
                    },
                    IsClosed = true
                });
            }
            else
            {
                var start = new Point(b.Left, b.Bottom);
                g.Figures!.Add(new PathFigure
                {
                    StartPoint = start,
                    Segments = new PathSegments
                    {
                        new LineSegment { Point = new Point(b.Left + rx, b.Bottom) },
                        new ArcSegment
                        {
                            Size = new Size(rx, ry), SweepDirection = SweepDirection.Clockwise,
                            Point = new Point(b.Right, b.Bottom - ry)
                        },
                        new LineSegment { Point = b.TopRight },
                        new LineSegment { Point = new Point(b.Left + rx, b.Top) },
                        new ArcSegment
                        {
                            Size = new Size(rx, ry), SweepDirection = SweepDirection.Clockwise,
                            Point = new Point(b.Left, b.Bottom - ry)
                        }
                    },
                    IsClosed = true
                });
            }
        }



        /// <summary>
        /// Returns whether a module at (x,y) is set and not part of a finder pattern.
        /// Results are cached per frame. Finder pattern exclusion avoids double-drawing with custom corner styles.
        /// </summary>
        /// <param name="bitMatrix">BitMatrix containing the data</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private bool IsValid(BitMatrix bitMatrix, int x, int y)
        {
            // Validate bounds
            if (x < 0 || y < 0 || x >= bitMatrix.Width || y >= bitMatrix.Height)
                return false;

            int key = HashCode.Combine(x, y);

            if (_setBitsCache.TryGetValue(key, out bool cached))
                return cached;

            // Exclude finder patterns
            if (x < 8 && y < 8) return _setBitsCache[key] = false; // TL
            if (x > bitMatrix.Width - 9 && y < 8) return _setBitsCache[key] = false; // TR
            if (x < 8 && y > bitMatrix.Height - 9) return _setBitsCache[key] = false; // BL

            return _setBitsCache[key] = bitMatrix[y, x];
        }

        /// <summary>
        /// Appends the three finder patterns. Each is composed as:
        /// outer ring (hole) → inner ring (solid) → center (hole).
        /// For “drop” style, a rotated teardrop path is used.
        /// </summary>
        /// <param name="geometry">Geometry containing the QRCode Geometry</param>
        /// <param name="bounds">Bounds of the control itself</param>
        /// <param name="symbolSize">The size of each symbol</param>
        private void AddPositionDetectionPattern(StreamGeometryContext ctx, Rect bounds, Size symbolSize)
        {
            var dataBounds = bounds
                .Deflate(Padding)
                .Deflate(new Thickness(symbolSize.Width * QuietZoneCount,
                    symbolSize.Height * QuietZoneCount));

            var markerSize = symbolSize * 7;

            for (var i = 0; i < 3; i++)
            {
                var topLeft = new Point(
                    i == 1 ? dataBounds.Right  - markerSize.Width  : dataBounds.Left,
                    i == 2 ? dataBounds.Bottom - markerSize.Height : dataBounds.Top);

                var outerRect = new Rect(topLeft, markerSize);                                     
                var innerRect = outerRect.Deflate(new Thickness(symbolSize.Width, symbolSize.Height));
                var centerTopLeft = new Point(topLeft.X + 2 * symbolSize.Width, topLeft.Y + 2 * symbolSize.Height);
                var centerRect = new Rect(centerTopLeft, symbolSize * 3);                          

                if (CornerDots == CornerDotsType.Drop)
                {
                    AddRect(ctx, outerRect, ccw: false);
                    AddRect(ctx, innerRect, ccw: true);
                    AddRect(ctx, centerRect, ccw: false);
                }
                else if (CornerDots == CornerDotsType.Circle)
                {
                    AddCircle(ctx, outerRect, ccw: false);
                    AddCircle(ctx, innerRect, ccw: true);
                    AddCircle(ctx, centerRect, ccw: false);
                }
                else 
                {
                    AddRect(ctx, outerRect, ccw: false);
                    AddRect(ctx, innerRect, ccw: true);
                    AddRect(ctx, centerRect, ccw: false);
                }
            }
        }

        private void DrawCornerByType(StreamGeometryContext ctx, Rect rect, CornerDotsType type, double rotation,
            bool ccw)
        {
            switch (type)
            {
                case CornerDotsType.Square: AddRect(ctx, rect, ccw); break;
                case CornerDotsType.Drop: AddDrop(ctx, rect, rotation); break;
                case CornerDotsType.Circle: AddCircle(ctx, rect, ccw); break;
            }
        }

        // Teardrop (rounded “drop”) shape. Path points define winding.
        private void AddDrop(StreamGeometryContext ctx, Rect b, double rotation)
        {
            const double minX = 0.243347;
            const double minY = 0.24324;
            const double size = 32.43246;
            Point P(double x, double y) => TransformToBounds(new Point(x - minX, y - minY), b, rotation, size);

            ctx.BeginFigure(P(0.243347, 23.8472), isFilled: true);
            ctx.LineTo(P(0.243347, 9.06324));
            ctx.CubicBezierTo(P(0.243347, 4.20097), P(4.26367, 0.24324), P(9.20443, 0.24324));
            ctx.LineTo(P(23.7147, 0.24324));
            ctx.CubicBezierTo(P(28.6551, 0.24324), P(32.6758, 4.20097), P(32.6758, 9.06324));
            ctx.LineTo(P(32.6758, 32.6757));
            ctx.LineTo(P(9.20443, 32.6621));
            ctx.CubicBezierTo(P(4.26367, 32.6621), P(0.243347, 28.7095), P(0.243347, 23.8472));
            ctx.EndFigure(isClosed: true);
        }

        private static Point TransformToBounds(Point p, Rect bounds, double rotationRad, double originalSize)
        {
            double sx = bounds.Width / originalSize;
            double sy = bounds.Height / originalSize;
            double cx = bounds.Left + bounds.Width / 2.0;
            double cy = bounds.Top + bounds.Height / 2.0;

            double x0 = (p.X - originalSize / 2.0) * sx;
            double y0 = (p.Y - originalSize / 2.0) * sy;

            double cos = Math.Cos(rotationRad);
            double sin = Math.Sin(rotationRad);

            double xr = x0 * cos - y0 * sin;
            double yr = x0 * sin + y0 * cos;

            return new Point(cx + xr, cy + yr);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (_qrCodeGeometry == null)
                return;

            var bounds = new Rect(0, 0, Width, Height);

            using var _clip = context.PushClip(new RoundedRect(
                bounds, CornerRadius.TopLeft, CornerRadius.TopRight,
                CornerRadius.BottomRight, CornerRadius.BottomLeft));

            if (_oldQrCodeGeometry is var (oldGeometry, _))
            {
                context.DrawRectangle(Background, null, bounds);     
                context.DrawGeometry(Foreground, null, oldGeometry); 
            }

            if (_qrCodeGeometry is var (newGeometry, newOpacity))
            {
                using var _ = context.PushOpacity(newOpacity);
                context.DrawRectangle(Background, null, bounds);
                context.DrawGeometry(Foreground, null, newGeometry);
            }

            if (Image is { } img && _imageRect is Rect dest)
            {
                var src = new Rect(img.Size);
                context.DrawImage(img, src, dest);
            }
        }


        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            // stop animations
            _transitionCts?.Cancel();
            _transitionCts = null;

            // drop heavy refs so GC can reclaim immediately
            _oldQrCodeGeometry = null;
            _qrCodeGeometry = null;
            _imageRect = null;
            _imageMaskRect = null;
            _encodedQrCode = null;

            // drop caches by replacing with empty instances
            _setBitsCache = new Dictionary<int, bool>(0);

            // optional: also clear transitions to avoid holding delegates
            Transitions = null;

            InvalidateVisual();
        }

        /// <summary>
        /// Indicates the level of error correction available in case of data loss or corruption.  The higher the correction level, the more data will be included in the QRCode
        /// </summary>
        public enum EccLevel
        {
            /// <summary>
            /// The lowest level of error correction where up to ~7% of data can be be recovered if lost and uses the least amount of symbols to represent the data
            /// </summary>
            Lowest,

            /// <summary>
            /// The standard level of error correction where up to ~15% of data can be be recovered if lost and represents a good compromise between a small size and reliability
            /// </summary>
            Medium,

            /// <summary>
            /// A high readability level of error correction where up to ~25% of data can be be recovered if lost but requires a larger footprint to represent the data
            /// </summary>
            Quality,

            /// <summary>
            /// The maximum level of error correction where up to ~30% of data can be be recovered if lost and represents the maximum achievable reliability
            /// </summary>
            Highest,
        }

        /// <summary>Data module shape.</summary>
        public enum DotType
        {
            Circle,
            Square,
            Triangle
        }

        /// <summary>Finder pattern ring style.</summary>
        public enum CornerDotsType
        {
            Circle,
            Square,
            Drop
        }

        /// <summary>Sides used by rounded-side helpers.</summary>
        private enum Side
        {
            Left,
            Right,
            Top,
            Bottom
        }

        /// <summary>Corners used by rounded-corner helpers.</summary>
        private enum Corner
        {
            TL,
            TR,
            BR,
            BL
        }

        /// <summary>
        /// Converts from our EccLevel to the one used by whichever algorithm being used.
        /// This exists as an abstraction layer for if/when the package or namespace of the actual QR Generator changes so that breaking changes are not introduced  
        /// </summary>
        /// <param name="eccLevel">The selected ECC Level to convert</param>
        /// <returns>The appropriate ECC Level type used by the generator</returns>
        /// <exception cref="ArgumentOutOfRangeException">When an unsupported ECC Level is provided</exception>
        private static ErrorCorrectionLevel ToQrCoderEccLevel(EccLevel eccLevel)
        {
            return eccLevel switch
            {
                EccLevel.Lowest => ErrorCorrectionLevel.L,
                EccLevel.Medium => ErrorCorrectionLevel.M,
                EccLevel.Quality => ErrorCorrectionLevel.Q,
                EccLevel.Highest => ErrorCorrectionLevel.H,
                _ => throw new ArgumentOutOfRangeException(nameof(eccLevel), eccLevel, null)
            };
        }
    }
}