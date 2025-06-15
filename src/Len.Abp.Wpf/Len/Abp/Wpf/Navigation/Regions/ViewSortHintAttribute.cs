namespace Len.Abp.Wpf.Navigation.Regions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ViewSortHintAttribute(string hint) : Attribute
{
    public string Hint { get; } = hint;
}