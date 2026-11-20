using Len.Abp.Wpf.Mvvm;
using System.Windows.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// 使用嵌套视图模型的视图。
/// </summary>
/// <remarks>
/// 契约必须生成到嵌套类型本身：丢掉外层类型链时，生成器会写出一个同名的
/// 幽灵类型 <c>WpfApp.Views.NestedViewModel</c>，真正的嵌套类型回落到接口默认实现，
/// 参数校验静默失效。没有对应 XAML，仅用于编译期验证生成结果。
/// </remarks>
[View<NestedContractOuter.NestedViewModel>(ViewName = "nested-contract", RequiredParameters = ["Id:int"])]
public partial class NestedContractView : UserControl
{
    public NestedContractView() { }
}

/// <summary>
/// 使用 record 视图模型的视图。
/// </summary>
/// <remarks>
/// 没有对应 XAML，仅用于编译期验证生成结果。
/// </remarks>
[View<RecordContractViewModel>(ViewName = "record-contract", RequiredParameters = ["Id:int"])]
public partial class RecordContractView : UserControl
{
    public RecordContractView() { }
}
