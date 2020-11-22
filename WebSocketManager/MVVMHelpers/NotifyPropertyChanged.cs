using System.ComponentModel;

namespace MVVMPattern
{
    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        #region PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        protected void OnPropertyChanged(params string[] properties)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(properties[i]));
            }
        }
        #endregion
    }
}