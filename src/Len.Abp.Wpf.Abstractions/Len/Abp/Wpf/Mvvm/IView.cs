namespace Len.Abp.Wpf.Mvvm;

public interface IView<out TViewModel>
    where TViewModel : class
{
    TViewModel ViewModel { get; }
}
