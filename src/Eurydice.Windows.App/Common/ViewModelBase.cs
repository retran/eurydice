using System;
using System.ComponentModel;

namespace Eurydice.Windows.App.Common
{
    /// <summary>
    ///     Base for view models.
    /// </summary>
    internal abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propname)
        {
            if (propname == null) throw new ArgumentNullException(nameof(propname));

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propname));
        }
    }
}