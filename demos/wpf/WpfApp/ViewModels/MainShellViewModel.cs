using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

public partial class MainShellViewModel : ObservableObject, IScopedDependency
{
    private bool _state = false;

    [ObservableProperty]
    private string _title = "Main Shell";

    [RelayCommand]
    private void ChangeTitle()
    {
        Title = _state ? "Main Shell" : "Main Shell Window";
        _state = !_state;
    }
}
