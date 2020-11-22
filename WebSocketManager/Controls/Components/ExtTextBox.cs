using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WebSocketManager.Components 
{
    public class ExtTextBox : TextBox
    {
        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public Visibility PlaceholderVisibility
        {
            get => (Visibility)GetValue(PlaceholderVisibilityProperty);
            set => SetValue(PlaceholderVisibilityProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public ExtTextBox() : base() => this.TextChanged += OnTextChanged;

        private void OnTextChanged(object sender, TextChangedEventArgs e) => PlaceholderVisibility = Text?.Length > 0 ? Visibility.Collapsed : Visibility.Visible;

        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(ExtTextBox), new PropertyMetadata(""));
        public static readonly DependencyProperty PlaceholderVisibilityProperty = DependencyProperty.Register(nameof(PlaceholderVisibility), typeof(Visibility), typeof(ExtTextBox), new PropertyMetadata(Visibility.Visible));
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(nameof(CornerRadius), typeof(string), typeof(ExtTextBox), new PropertyMetadata(""));
    }
}