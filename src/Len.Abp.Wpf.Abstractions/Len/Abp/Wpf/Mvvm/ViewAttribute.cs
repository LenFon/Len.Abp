namespace Len.Abp.Wpf.Mvvm;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ViewAttribute(Type viewModel) : Attribute
{
    public bool IsShell { get; set; } = false;

    public Type ViewModel { get; set; } = viewModel;

    public string? ViewName { get; set; }
}