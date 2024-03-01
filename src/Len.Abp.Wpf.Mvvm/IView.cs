namespace Len.Abp.Wpf.Mvvm;

public interface IView { }

public interface IView<T> where T : class
{
    protected internal T ViewModel { get; set; }
}
