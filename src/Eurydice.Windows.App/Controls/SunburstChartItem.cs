using System;
using System.Windows;
using System.Windows.Controls;

namespace Eurydice.Windows.App.Controls
{
    /// <summary>
    ///     Sunburst chart item code behind.
    /// </summary>
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(SunburstChartItem))]
    internal sealed class SunburstChartItem : HeaderedItemsControl
    {
        public static readonly DependencyProperty LevelProperty =
            DependencyProperty.Register("Level", typeof(int), typeof(SunburstChartItem),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register("StartAngle", typeof(double), typeof(SunburstChartItem)
                , new FrameworkPropertyMetadata(Math.PI, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty EndAngleProperty =
            DependencyProperty.Register("EndAngle", typeof(double), typeof(SunburstChartItem)
                , new FrameworkPropertyMetadata(Math.PI, FrameworkPropertyMetadataOptions.AffectsRender));

        static SunburstChartItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SunburstChartItem),
                new FrameworkPropertyMetadata(typeof(SunburstChartItem)));
        }

        public int Level
        {
            get => (int) GetValue(LevelProperty);
            set => SetValue(LevelProperty, value);
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

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new SunburstChartItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is SunburstChartItem;
        }
    }
}