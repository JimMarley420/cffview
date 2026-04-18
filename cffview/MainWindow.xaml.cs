using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace cffview;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        SearchPlaceholder.Visibility = Visibility.Collapsed;
    }

    private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SearchBox.Text))
        {
            SearchPlaceholder.Visibility = Visibility.Visible;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) 
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ViewModels.MainViewModel vm)
        {
            vm.SearchCommand.Execute(null);
        }
    }
}