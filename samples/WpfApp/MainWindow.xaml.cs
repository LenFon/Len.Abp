using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Router;
using System.Windows;

namespace WpfApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[View(ViewModel = typeof(MainWindowViewModel))]
public partial class MainWindow : Window, IRouterShell
{
    public MainWindow()
    {
        InitializeComponent();
    }
}