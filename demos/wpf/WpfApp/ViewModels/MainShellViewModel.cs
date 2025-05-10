using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Core;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

public enum Status
{
    [Description("待处理")]
    Pending = 1,

    [Display(Name = "处理中")]
    Processing = 2,

    [Description("已完成")]
    Completed = 3
}

public partial class MainShellViewModel : ObservableObject, IScopedDependency
{
    private bool _state = false;

    [ObservableProperty]
    private string _title = "Main Shell";

    public IAbpLazyServiceProvider LazyServiceProvider { get; set; } = default!;

    [RelayCommand]
    private void ChangeTitle()
    {
        var items = EnumHelper.GetNameValues<Status>();
        Title = _state ? "Main Shell" : "Main Shell Window";
        _state = !_state;
    }
}