using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

namespace Eurydice.Windows.App.Controls.ViewModel
{
    /// <summary>
    ///     Base for sunburst chart nodes.
    /// </summary>
    internal interface ISunburstChartNodeViewModel<T>
        where T : ISunburstChartNodeViewModel<T>
    {
        ICommand DrilldownCommand { get; }

        double Start { get; }
        double End { get; }
        ObservableCollection<T> Nodes { get; }
        Brush Brush { get; set; }
        Brush HoverBrush { get; set; }
    }
}