using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Eurydice.Windows.App.Controls
{
    /// <summary>
    ///     Sunburst chart slice shape.
    /// </summary>
    internal sealed class SunburstChartSlice : Shape, ICommandSource
    {
        private const double Epsilon = 0.0001;

        public static readonly DependencyProperty OriginProperty =
            DependencyProperty.Register("Origin", typeof(Point), typeof(SunburstChartSlice),
                new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register("StartAngle", typeof(double), typeof(SunburstChartSlice)
                , new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty EndAngleProperty =
            DependencyProperty.Register("EndAngle", typeof(double), typeof(SunburstChartSlice)
                , new FrameworkPropertyMetadata(Math.PI / 2.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty InnerRadiusProperty =
            DependencyProperty.Register("InnerRadius", typeof(double), typeof(SunburstChartSlice)
                , new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty OuterRadiusProperty =
            DependencyProperty.Register("OuterRadius", typeof(double), typeof(SunburstChartSlice)
                , new FrameworkPropertyMetadata(20.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                "Command",
                typeof(ICommand),
                typeof(SunburstChartSlice),
                new PropertyMetadata(null,
                    CommandChanged));

        private readonly Geometry _emptyGeometry = new PathGeometry();

        private EventHandler _canExecuteChangedHandler;

        private EllipseGeometry _ellipseGeometry;

        private PathFigure _figure;

        private PathGeometry _geometry;

        private ArcSegment _innerArcSegment;

        private LineSegment _innerLineSegment;

        private ArcSegment _outerArcSegment;

        private LineSegment _outerLineSegment;

        static SunburstChartSlice()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SunburstChartSlice),
                new FrameworkPropertyMetadata(typeof(SunburstChartSlice)));
        }

        public Point Origin
        {
            get => (Point) GetValue(OriginProperty);
            set => SetValue(OriginProperty, value);
        }

        public double StartAngle
        {
            get => (double) GetValue(StartAngleProperty);
            set => SetValue(StartAngleProperty, value);
        }

        public double EndAngle
        {
            get => (double) GetValue(EndAngleProperty);
            set => SetValue(EndAngleProperty, value);
        }

        public double InnerRadius
        {
            get => (double) GetValue(InnerRadiusProperty);
            set => SetValue(InnerRadiusProperty, value);
        }

        public double OuterRadius
        {
            get => (double) GetValue(OuterRadiusProperty);
            set => SetValue(OuterRadiusProperty, value);
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                var innerRadius = InnerRadius;
                var outerRadius = OuterRadius;

                var startAngle = StartAngle < 0 ? StartAngle + 2 * Math.PI : StartAngle;
                var endAngle = EndAngle < 0 ? EndAngle + 2 * Math.PI : EndAngle;

                if (endAngle < startAngle) endAngle += Math.PI * 2;

                if (Math.Abs(endAngle - startAngle) < Epsilon) return _emptyGeometry;

                if (Math.Abs(Math.PI * 2 - Math.Abs(endAngle - startAngle)) < Epsilon)
                {
                    if (InnerRadius < Epsilon)
                    {
                        if (_ellipseGeometry != null)
                        {
                            _ellipseGeometry.Center = Origin;
                            _ellipseGeometry.RadiusX = OuterRadius;
                            _ellipseGeometry.RadiusY = OuterRadius;
                        }
                        else
                        {
                            _ellipseGeometry = new EllipseGeometry(Origin, OuterRadius, OuterRadius);
                        }

                        return _ellipseGeometry;
                    }

                    endAngle -= 2 * Epsilon;
                }

                var isLargeArc = Math.Abs(endAngle - startAngle) > Math.PI;

                var startAngleSin = Math.Sin(startAngle);
                var startAngleCos = Math.Cos(startAngle);

                var endAngleSin = Math.Sin(endAngle);
                var endAngleCos = Math.Cos(endAngle);

                var innerStartPoint = Origin + new Vector(startAngleCos, startAngleSin) * innerRadius;
                var innerEndPoint = Origin + new Vector(endAngleCos, endAngleSin) * innerRadius;
                var outerEndPoint = Origin + new Vector(endAngleCos, endAngleSin) * outerRadius;
                var outerStartPoint = Origin + new Vector(startAngleCos, startAngleSin) * outerRadius;

                if (_innerArcSegment == null)
                {
                    _innerArcSegment = new ArcSegment(innerEndPoint, new Size(innerRadius, innerRadius), 0.0,
                        isLargeArc,
                        SweepDirection.Clockwise, true);
                }
                else
                {
                    _innerArcSegment.Point = innerEndPoint;
                    _innerArcSegment.IsLargeArc = isLargeArc;
                    _innerArcSegment.Size = new Size(innerRadius, innerRadius);
                }

                if (_innerLineSegment == null)
                    _innerLineSegment = new LineSegment(outerEndPoint, true);
                else
                    _innerLineSegment.Point = outerEndPoint;

                if (_outerArcSegment == null)
                {
                    _outerArcSegment = new ArcSegment(outerStartPoint, new Size(outerRadius, outerRadius), 0.0,
                        isLargeArc,
                        SweepDirection.Counterclockwise, true);
                }
                else
                {
                    _outerArcSegment.Point = outerStartPoint;
                    _outerArcSegment.IsLargeArc = isLargeArc;
                    _outerArcSegment.Size = new Size(outerRadius, outerRadius);
                }

                if (_outerLineSegment == null)
                    _outerLineSegment = new LineSegment(innerStartPoint, true);
                else
                    _outerLineSegment.Point = innerStartPoint;

                if (_figure == null)
                    _figure = new PathFigure(innerStartPoint, new List<PathSegment>
                    {
                        _innerArcSegment,
                        _innerLineSegment,
                        _outerArcSegment,
                        _outerLineSegment
                    }, true);
                else
                    _figure.StartPoint = innerStartPoint;

                if (_geometry == null)
                    _geometry = new PathGeometry(new List<PathFigure> {_figure}, FillRule.EvenOdd, null);

                return _geometry;
            }
        }

        public ICommand Command
        {
            get => (ICommand) GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object CommandParameter { get; }

        public IInputElement CommandTarget { get; }

        private static void CommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cs = (SunburstChartSlice) d;
            cs.HookUpCommand((ICommand) e.OldValue, (ICommand) e.NewValue);
        }

        private void HookUpCommand(ICommand oldCommand, ICommand newCommand)
        {
            if (oldCommand != null) oldCommand.CanExecuteChanged -= CanExecuteChanged;

            _canExecuteChangedHandler = CanExecuteChanged;
            if (newCommand != null) newCommand.CanExecuteChanged += _canExecuteChangedHandler;
        }

        private void CanExecuteChanged(object sender, EventArgs e)
        {
            if (Command != null)
            {
                if (Command is RoutedCommand command)
                    IsEnabled = command.CanExecute(DataContext, CommandTarget);
                else
                    IsEnabled = Command.CanExecute(DataContext);
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (Command != null)
            {
                if (Command is RoutedCommand command)
                    command.Execute(DataContext, CommandTarget);
                else
                    Command.Execute(DataContext);
            }
        }
    }
}