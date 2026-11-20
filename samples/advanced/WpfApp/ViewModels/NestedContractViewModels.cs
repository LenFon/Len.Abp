namespace WpfApp.ViewModels;

/// <summary>
/// 承载嵌套视图模型的外层类型。
/// </summary>
public partial class NestedContractOuter
{
    /// <summary>
    /// 嵌套视图模型：契约成员必须生成到这个类型上，而不是同名的幽灵类型。
    /// </summary>
    public partial class NestedViewModel { }
}

/// <summary>
/// record 视图模型：生成器必须产出 <c>partial record</c>。
/// </summary>
/// <remarks>
/// 生成器必须保留 <c>record</c> 关键字：写成 <c>partial class</c> 会报 CS0261。
/// </remarks>
public partial record RecordContractViewModel { }
