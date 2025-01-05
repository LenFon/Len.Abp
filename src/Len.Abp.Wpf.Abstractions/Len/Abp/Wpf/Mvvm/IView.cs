namespace Len.Abp.Wpf.Mvvm;

public interface IView
{
    object DataContext { get; }
}

public interface IView<out TViewModel> : IView
    where TViewModel : class
{
    TViewModel ViewModel { get; }
}
