using System.Windows;
using System.Windows.Controls;

namespace Eurydice.Windows.App.Controls
{
    /// <summary>
    ///     Sunburst chart code behind.
    /// </summary>
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(SunburstChartItem))]
    internal sealed class SunburstChart : ItemsControl
    {
        public static readonly DependencyProperty SliceWidthProperty =
            DependencyProperty.Register(nameof(SliceWidth), typeof(double), typeof(SunburstChart));

        public static readonly DependencyProperty DepthProperty =
            DependencyProperty.Register(nameof(Depth), typeof(int), typeof(SunburstChart),
                new FrameworkPropertyMetadata(4));

        static SunburstChart()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SunburstChart),
                new FrameworkPropertyMetadata(typeof(SunburstChart)));
        }

        public double SliceWidth
        {
            get => (double) GetValue(SliceWidthProperty);
            set => SetValue(SliceWidthProperty, value);
        }

        public int Depth
        {
            get => (int) GetValue(DepthProperty);
            set => SetValue(DepthProperty, value);
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is SunburstChartItem;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new SunburstChartItem();
        }
    }
}