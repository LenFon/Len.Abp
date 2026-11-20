using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer;

/// <summary>
/// 生成器诊断目录。
/// </summary>
/// <remarks>
/// ID 一经发布不可更改，新增只追加。编号按 `LENABP` 前缀连续分配，不复用废弃编号。
/// </remarks>
public static class DiagnosticDescriptors
{
    private const string Category = "Len.Abp";

    /// <summary>
    /// 视图缺少无参构造函数，生成的构造函数无法通过 <c>: this()</c> 链式调用。
    /// </summary>
    public static readonly DiagnosticDescriptor MissingParameterlessConstructor = new(
        id: "LENABP001",
        title: "视图缺少无参构造函数",
        messageFormat: "视图 {0} 声明了构造函数但没有无参版本，生成的构造函数无法通过 : this() 链式调用。请补充一个无参构造函数。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 应用了生成器特性的类型未声明 <c>partial</c>。
    /// </summary>
    public static readonly DiagnosticDescriptor TypeMustBePartial = new(
        id: "LENABP002",
        title: "类型必须声明为 partial",
        messageFormat: "类型 {0} 应用了生成器特性，但未声明为 partial。生成器需要为它追加分部声明。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 多个视图使用相同的 <c>ViewName</c>，导航会静默命中其中一个。
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateViewName = new(
        id: "LENABP003",
        title: "视图名称重复",
        messageFormat: "视图名称 \"{0}\" 被多个视图使用：{1}。导航按名称解析，只会命中其中一个。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 生成器特性的参数非法。
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidAttributeArgument = new(
        id: "LENABP004",
        title: "特性参数非法",
        messageFormat: "{0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 生成器内部异常，作为兜底诊断。
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratorFailure = new(
        id: "LENABP005",
        title: "生成器内部异常",
        messageFormat: "生成器 {0} 处理源代码时发生异常：{1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 视图模型未声明 <c>partial</c>，无法自动注册到容器。
    /// </summary>
    /// <remarks>
    /// 警告级：不自动注册只影响便利性，使用者可以手动注册。
    /// 若同时声明了 <c>RequiredParameters</c>，改用
    /// <see cref="ViewModelMustBePartialForContract"/>（错误级）。
    /// </remarks>
    public static readonly DiagnosticDescriptor ViewModelMustBePartial = new(
        id: "LENABP006",
        title: "视图模型必须声明为 partial",
        messageFormat: "视图模型 {0} 未声明为 partial，无法自动注册到容器。请添加 partial 修饰符，或手动声明生命周期接口。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 视图模型未声明 <c>partial</c>，无法生成导航契约成员。
    /// </summary>
    /// <remarks>
    /// 错误级：声明了 <c>RequiredParameters</c> 时契约成员必须生成到视图模型上，
    /// 生成不了就意味着参数校验静默失效——这比"不能自动注册"严重得多。
    /// </remarks>
    public static readonly DiagnosticDescriptor ViewModelMustBePartialForContract = new(
        id: "LENABP009",
        title: "视图模型必须声明为 partial 才能生成导航契约",
        messageFormat: "视图模型 {0} 声明了 RequiredParameters 但未声明为 partial，无法生成导航契约成员，参数校验会静默失效。请添加 partial 修饰符，或手动实现 INavigationContract。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 视图模型的构造函数包含不可注入的参数，无法自动注册。
    /// </summary>
    public static readonly DiagnosticDescriptor ViewModelConstructorNotInjectable = new(
        id: "LENABP007",
        title: "视图模型的构造函数包含不可注入的参数",
        messageFormat: "视图模型 {0} 的构造函数包含不可注入的参数（{1}），无法自动注册。请改用工厂注册，或把参数改为从导航参数读取。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 视图类型不能是泛型：生成的 <c>ExposeKeyedService</c> 特性参数不能用类型参数。
    /// </summary>
    public static readonly DiagnosticDescriptor ViewMustNotBeGeneric = new(
        id: "LENABP008",
        title: "视图类型不能是泛型",
        messageFormat: "视图 {0} 是泛型类型，生成器无法为它产出注册特性（特性参数不能使用类型参数）。请改为非泛型类型，或手写注册。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 视图模型必须是引用类型：<c>IView&lt;TViewModel&gt;</c> 声明了 <c>where TViewModel : class</c>。
    /// </summary>
    /// <remarks>
    /// 值类型视图模型无法满足泛型约束，生成的视图基类型会报 CS0452。
    /// 视图模型是参数的消费者，需要引用语义（状态在导航回调间保持），
    /// 值类型每次装箱都会得到独立副本，语义本身也不成立。
    /// </remarks>
    public static readonly DiagnosticDescriptor ViewModelMustBeReferenceType = new(
        id: "LENABP010",
        title: "视图模型必须是引用类型",
        messageFormat: "视图模型 {0} 是值类型，无法用作 IView<TViewModel> 的类型参数（该接口要求引用类型）。请改为 class 或 record。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 视图模型不能是开放泛型：<c>IView&lt;TViewModel&gt;</c> 需要闭合类型参数。
    /// </summary>
    /// <remarks>
    /// 开放泛型（<c>PageViewModel&lt;&gt;</c>）无法满足 <c>where TViewModel : class</c> 约束，
    /// 生成的基类型会变成未绑定泛型名（CS7003），分部声明也无法与用户声明匹配。
    /// 需要泛型视图模型时请指定闭合类型实参（<c>PageViewModel&lt;int&gt;</c>）。
    /// </remarks>
    public static readonly DiagnosticDescriptor ViewModelMustNotBeOpenGeneric = new(
        id: "LENABP011",
        title: "视图模型不能是开放泛型",
        messageFormat: "视图模型 {0} 是开放泛型，无法用作 IView<TViewModel> 的类型参数。请指定闭合的类型实参。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 视图的宿主类型不合法：静态类或结构体。
    /// </summary>
    /// <remarks>
    /// 视图必须能承载 <c>IView</c> 的实例成员与 <c>DataContext</c>：
    /// 静态类不能声明实例成员（CS0708/CS0710/CS0714），
    /// 结构体不具备引用语义（<c>DataContext</c> 是 <c>FrameworkElement</c> 的实例属性）。
    /// 生成器不产出代码，避免使用者看到一堆分散的编译错误。
    /// </remarks>
    public static readonly DiagnosticDescriptor ViewHostTypeInvalid = new(
        id: "LENABP012",
        title: "视图的宿主类型不合法",
        messageFormat: "视图 {0} {1}，无法承载 IView 的实例成员。请改为非静态的类或 record。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 非附加依赖属性或路由事件的宿主是静态类或结构体。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 非附加版本生成实例属性/事件与 <c>protected</c> 方法，
    /// 静态类无法承载（CS0708/CS1057）。
    /// </para>
    /// <para>
    /// 结构体同样不行：生成器写的是 <c>partial class</c>，与 <c>struct</c> 声明冲突（CS0261），
    /// 且生成的 <c>GetValue</c>/<c>SetValue</c> 调用在结构体上不存在（CS0103）。
    /// </para>
    /// <para>
    /// 附加版本只生成静态方法，不受影响。
    /// </para>
    /// </remarks>
    public static readonly DiagnosticDescriptor InstanceMemberHostMustNotBeStatic = new(
        id: "LENABP013",
        title: "非附加成员不能生成到静态类或结构体",
        messageFormat: "{0} 生成实例成员，但宿主 {1} {2}。请改用附加版本（{3}），或把宿主改为非静态类。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    /// <summary>
    /// <c>RequiredParameters</c> 中声明的类型名无法解析。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 解析失败时生成器退化为"只校验存在性"（字典值为 <see langword="null"/>），
    /// 导航参数的类型校验会静默失效：使用者以为传了 <c>int</c> 就安全，
    /// 实际传字符串也能通过。这类降级必须让使用者知道。
    /// </para>
    /// <para>
    /// 定为 Warning 而不是 Error：参数名本身仍然有效，导航仍能正常工作，
    /// 只是少了类型校验。构建不应因此中断。
    /// </para>
    /// </remarks>
    public static readonly DiagnosticDescriptor RequiredParameterTypeUnresolved = new(
        id: "LENABP014",
        title: "导航必需参数的类型名无法解析",
        messageFormat: "视图 {0} 的必需参数 \"{1}\" 声明的类型 \"{2}\" 无法解析，该参数将只校验存在性、不校验类型。请检查类型名拼写，或改用完全限定名。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 视图没有放在 <c>Views</c> 目录下。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 警告级：目录不影响生成的代码能否编译，只影响项目结构的一致性。
    /// 但视图与视图模型混放时，读代码的人无法从目录判断某个类型是哪一类，
    /// 生成器产出的两个文件也会与源文件的组织方式对不上。
    /// </para>
    /// <para>
    /// 只检查路径里是否出现名为 <c>Views</c> 的目录段，不要求它是项目根下的第一层：
    /// 按功能分目录（<c>Features/Orders/Views/OrderView.cs</c>）同样满足约定。
    /// 源文件没有路径时（生成器测试的内存源码、从标准输入编译）不检查。
    /// </para>
    /// </remarks>
    public static readonly DiagnosticDescriptor ViewMustBeInViewsFolder = new(
        id: "LENABP015",
        title: "视图应放在 Views 目录下",
        messageFormat: "视图 {0} 位于 \"{1}\"，不在 Views 目录下。请把视图移入 Views 目录，与视图模型分开放置。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>
    /// 视图模型没有放在 <c>ViewModels</c> 目录下。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="ViewMustBeInViewsFolder"/> 对称：视图模型必须与视图分目录存放。
    /// 只对生成器实际要追加成员（<c>partial</c>）的视图模型报告——
    /// 非 <c>partial</c> 的视图模型生成器根本不会碰它，报告目录问题只会增加噪音。
    /// </remarks>
    public static readonly DiagnosticDescriptor ViewModelMustBeInViewModelsFolder = new(
        id: "LENABP016",
        title: "视图模型应放在 ViewModels 目录下",
        messageFormat: "视图模型 {0} 位于 \"{1}\"，不在 ViewModels 目录下。请把视图模型移入 ViewModels 目录，与视图分开放置。",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
}
