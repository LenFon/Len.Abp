namespace Len.Abp.Wpf.Mvvm;

public interface IView
{
    object ViewModel { get; }

    string ViewName { get; }
}

public interface IView<out TViewModel> : IView
    where TViewModel : class
{
    new TViewModel ViewModel { get; }
}