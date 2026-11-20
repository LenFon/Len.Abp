using Len.Abp.Analyzer.TestSupport;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖：生成器专用诊断 <c>LENABP001</c>–<c>LENABP005</c>、<c>LENABP008</c>、<c>LENABP010</c> 与 <c>LENABP011</c>。
/// </summary>
/// <remarks>
/// 这些诊断都是错误级，必须走 <see cref="GeneratorTestHost.RunWithErrors"/>，
/// 否则会被测试宿主的"生成器产生错误"检查拦下。
/// </remarks>
public class GeneratorDiagnosticsTests
{
    /// <summary>
    /// 视图只声明带参构造函数时报告 <c>LENABP001</c>，位置指向视图标识符。
    /// </summary>
    [Fact]
    public void Mvvm_MissingParameterlessConstructor_ReportsLENABP001()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public class PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
            public partial class PageView
            {
                public PageView(int value) { }
            }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP001", "PageView");
    }

    /// <summary>
    /// 手写接收视图模型的构造函数时不需要无参构造，不报告 <c>LENABP001</c>。
    /// </summary>
    [Fact]
    public void Mvvm_ViewModelConstructor_DoesNotReportLENABP001()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public class PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
            public partial class PageView
            {
                public PageView(PageViewModel viewModel) { }
            }
            """
        );

        Assert.Single(sources);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP001");
    }

    /// <summary>
    /// 非 partial 的视图报告 <c>LENABP002</c>，且不生成源文件。
    /// </summary>
    [Fact]
    public void Mvvm_NonPartialView_ReportsLENABP002()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public class PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
            public class PageView { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP002", "PageView");
    }

    /// <summary>
    /// 嵌套类型的外层未声明 partial 时同样报告 <c>LENABP002</c>。
    /// </summary>
    /// <remarks>
    /// 生成代码会逐层展开 partial 声明，外层缺失时内层同样无法编译。
    /// </remarks>
    [Fact]
    public void Mvvm_NestedTypeWithNonPartialOuter_ReportsLENABP002()
    {
        var (_, diagnostics) = RunMvvm(
            """
            public class PageViewModel { }

            public class Outer
            {
                [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                public partial class PageView { }
            }
            """
        );

        AssertDiagnostic(diagnostics, "LENABP002", "Outer");
    }

    /// <summary>
    /// 依赖属性宿主未声明 partial 时报告 <c>LENABP002</c>。
    /// </summary>
    [Fact]
    public void DependencyProperty_NonPartialHost_ReportsLENABP002()
    {
        var (sources, diagnostics) = Run(
            new DependencyPropertyGenerator(),
            """
            [Len.Abp.Wpf.DependencyProperty<string>("Title")]
            public class DemoControl : System.Windows.DependencyObject { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP002", "DemoControl");
    }

    /// <summary>
    /// 同一类型上的多个特性只报告一次 <c>LENABP002</c>。
    /// </summary>
    /// <remarks>
    /// <c>GetClasses</c> 为每个特性产出一个条目，<c>ReportDiagnostics</c> 会按特性调用多次。
    /// 不去重的话，标了 3 个特性就会报 3 次同样的问题。
    /// </remarks>
    [Fact]
    public void MultipleAttributes_ReportLENABP002Once()
    {
        var (_, diagnostics) = Run(
            new DependencyPropertyGenerator(),
            """
            [Len.Abp.Wpf.DependencyProperty<string>("Title")]
            [Len.Abp.Wpf.DependencyProperty<string>("Subtitle")]
            [Len.Abp.Wpf.DependencyProperty<string>("Caption")]
            public class DemoControl : System.Windows.DependencyObject { }
            """
        );

        Assert.Single(diagnostics, d => d.Id == "LENABP002");
    }

    /// <summary>
    /// 两个视图使用相同 ViewName 时报告一次 <c>LENABP003</c>。
    /// </summary>
    [Fact]
    public void Mvvm_DuplicateViewName_ReportsLENABP003()
    {
        var (_, diagnostics) = RunMvvm(
            """
            public class FirstViewModel { }
            public class SecondViewModel { }

            [Len.Abp.Wpf.Mvvm.View<FirstViewModel>(ViewName = "page")]
            public partial class FirstView { }

            [Len.Abp.Wpf.Mvvm.View<SecondViewModel>(ViewName = "page")]
            public partial class SecondView { }
            """
        );

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "LENABP003");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.NotEqual(Location.None, diagnostic.Location);
        Assert.Contains("page", diagnostic.GetMessage());
        Assert.Contains("FirstView", diagnostic.GetMessage());
        Assert.Contains("SecondView", diagnostic.GetMessage());
    }

    /// <summary>
    /// 视图名不同时不报告 <c>LENABP003</c>。
    /// </summary>
    [Fact]
    public void Mvvm_DistinctViewNames_DoesNotReportLENABP003()
    {
        var (_, diagnostics) = RunMvvm(
            """
            public class FirstViewModel { }
            public class SecondViewModel { }

            [Len.Abp.Wpf.Mvvm.View<FirstViewModel>(ViewName = "first")]
            public partial class FirstView { }

            [Len.Abp.Wpf.Mvvm.View<SecondViewModel>(ViewName = "second")]
            public partial class SecondView { }
            """
        );

        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP003");
    }

    /// <summary>
    /// 空 ViewName 不参与重复判定，由 <c>LENABP004</c> 报告。
    /// </summary>
    [Fact]
    public void Mvvm_EmptyViewName_ReportsLENABP004()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public class FirstViewModel { }
            public class SecondViewModel { }

            [Len.Abp.Wpf.Mvvm.View<FirstViewModel>(ViewName = "")]
            public partial class FirstView { }

            [Len.Abp.Wpf.Mvvm.View<SecondViewModel>(ViewName = "")]
            public partial class SecondView { }
            """
        );

        Assert.Empty(sources);
        Assert.Equal(2, diagnostics.Count(d => d.Id == "LENABP004"));
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP003");
    }

    /// <summary>
    /// 同一类型同时应用泛型与非泛型视图特性时报告一条 <c>LENABP004</c>，且不产出源文件。
    /// </summary>
    /// <remarks>
    /// 回归：两种形态是两个不同的特性类，<c>AllowMultiple = false</c> 只约束同一个类。
    /// 两条生成管线会产出同名 hintName，Roslyn 抛异常（CS8785），该类型一个源文件都不产出。
    /// 诊断必须只报一次——按特性类去重会报两次。
    /// </remarks>
    [Fact]
    public void Mvvm_BothViewAttributeForms_ReportsSingleLENABP004()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public class FirstViewModel { }
            public class SecondViewModel { }

            [Len.Abp.Wpf.Mvvm.View<FirstViewModel>(ViewName = "one")]
            [Len.Abp.Wpf.Mvvm.View(typeof(SecondViewModel), ViewName = "two")]
            public partial class PageView { }
            """
        );

        Assert.Empty(sources);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "LENABP004");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.NotEqual(Location.None, diagnostic.Location);
        Assert.Contains("ViewAttribute<T>", diagnostic.GetMessage());
        Assert.Contains("ViewAttribute", diagnostic.GetMessage());
        Assert.Contains("PageView", diagnostic.GetMessage());
    }

    /// <summary>
    /// 只应用一种视图特性时不受影响。
    /// </summary>
    [Theory]
    [InlineData("""[Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]""")]
    [InlineData("""[Len.Abp.Wpf.Mvvm.View(typeof(PageViewModel), ViewName = "page")]""")]
    public void Mvvm_SingleViewAttribute_GeneratesWithoutDiagnostics(string attribute)
    {
        var (sources, diagnostics) = RunMvvm(
            "public class PageViewModel { }\n\n"
                + attribute + "\n"
                + "public partial class PageView { }\n"
        );

        Assert.Single(sources);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP004");
    }

    /// <summary>
    /// 依赖属性名称为空时报告 <c>LENABP004</c>。
    /// </summary>
    [Fact]
    public void DependencyProperty_EmptyName_ReportsLENABP004()
    {
        var (sources, diagnostics) = Run(
            new DependencyPropertyGenerator(),
            """
            [Len.Abp.Wpf.DependencyProperty<string>("")]
            public partial class DemoControl : System.Windows.DependencyObject { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "不能为空");
    }

    /// <summary>
    /// 只有空白字符的名称同样视为空，报告 <c>LENABP004</c>。
    /// </summary>
    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void DependencyProperty_WhitespaceName_ReportsLENABP004(string name)
    {
        var (sources, diagnostics) = Run(
            new DependencyPropertyGenerator(),
            $$"""
            [Len.Abp.Wpf.DependencyProperty<string>("{{name}}")]
            public partial class DemoControl : System.Windows.DependencyObject { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "不能为空");
    }

    /// <summary>
    /// 视图名称为空白时同样报告 <c>LENABP004</c>。
    /// </summary>
    [Fact]
    public void Mvvm_WhitespaceViewName_ReportsLENABP004()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public class PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "   ")]
            public partial class PageView { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "不能为空");
    }

    /// <summary>
    /// 超长名称不截断，正常生成。
    /// </summary>
    [Fact]
    public void DependencyProperty_VeryLongName_IsGenerated()
    {
        var name = new string('N', 500);

        var (sources, diagnostics) = Run(
            new DependencyPropertyGenerator(),
            $$"""
            [Len.Abp.Wpf.DependencyProperty<string>("{{name}}")]
            public partial class DemoControl : System.Windows.DependencyObject { }
            """
        );

        Assert.Single(sources);
        Assert.Contains(name, sources[0].Text);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP004");
    }

    /// <summary>
    /// 名称不是合法 C# 标识符时报告 <c>LENABP004</c>，且不生成代码。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：名称会被直接拼进成员声明、支撑字段名与 <c>Register(name: "...")</c>
    /// 的字面量。非法名称产出的是十余条互相矛盾的语法错误
    /// （CS1002/CS1519/CS1003/CS1010/CS0116/CS0246/CS0501/CS0751 等），
    /// 没有一条指向真正的问题。
    /// </para>
    /// <para>
    /// 含引号的名称还会破坏字符串字面量，产生跨行的语法错误，错误位置完全对不上。
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("My Title")]
    [InlineData("1Title")]
    [InlineData("Title-2")]
    [InlineData("Title.X")]
    [InlineData("Title+")]
    public void DependencyProperty_InvalidIdentifierName_ReportsLENABP004(string name)
    {
        var (sources, diagnostics) = Run(
            new DependencyPropertyGenerator(),
            $$"""
            [Len.Abp.Wpf.DependencyProperty<string>("{{name}}")]
            public partial class DemoControl : System.Windows.DependencyObject { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "不是合法的 C# 标识符");
    }

    /// <summary>
    /// 路由事件的名称同样校验标识符合法性。
    /// </summary>
    [Theory]
    [InlineData("My Event")]
    [InlineData("1Event")]
    [InlineData("Event-2")]
    public void RoutedEvent_InvalidIdentifierName_ReportsLENABP004(string name)
    {
        var (sources, diagnostics) = Run(
            new RoutedEventGenerator(),
            $$"""
            [Len.Abp.Wpf.RoutedEvent("{{name}}", System.Windows.RoutingStrategy.Bubble)]
            public partial class DemoControl : System.Windows.UIElement { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "不是合法的 C# 标识符");
    }

    /// <summary>
    /// 附加版本的依赖属性同样校验标识符。
    /// </summary>
    [Fact]
    public void AttachedDependencyProperty_InvalidIdentifierName_ReportsLENABP004()
    {
        var (sources, diagnostics) = Run(
            new AttachedDependencyPropertyGenerator(),
            """
            [Len.Abp.Wpf.AttachedDependencyProperty<string>("My Title")]
            public static partial class DemoAttached { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "不是合法的 C# 标识符");
    }

    /// <summary>
    /// 名称是 C# 关键字时报告 <c>LENABP004</c>，提示换名而不是让使用者看到 @ 前缀。
    /// </summary>
    /// <remarks>
    /// <c>@class</c> 在生成代码里是合法的，但支撑字段会变成 <c>@classProperty</c>，
    /// 可读性很差。这里提前拦截并给出明确建议。
    /// </remarks>
    [Theory]
    [InlineData("class")]
    [InlineData("int")]
    [InlineData("event")]
    public void DependencyProperty_KeywordName_ReportsLENABP004(string name)
    {
        var (sources, diagnostics) = Run(
            new DependencyPropertyGenerator(),
            $$"""
            [Len.Abp.Wpf.DependencyProperty<string>("{{name}}")]
            public partial class DemoControl : System.Windows.DependencyObject { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "是 C# 关键字");
    }

    /// <summary>
    /// Unicode 标识符（中文名）是合法的 C# 标识符，正常生成。
    /// </summary>
    /// <remarks>
    /// <see cref="Microsoft.CodeAnalysis.CSharp.SyntaxFacts.IsValidIdentifier"/> 按语言规则
    /// 判定，不做 ASCII 限制。这条用例防止校验写死成"只允许字母数字下划线"。
    /// </remarks>
    [Fact]
    public void DependencyProperty_UnicodeName_IsGenerated()
    {
        var (sources, diagnostics) = Run(
            new DependencyPropertyGenerator(),
            """
            [Len.Abp.Wpf.DependencyProperty<string>("标题")]
            public partial class DemoControl : System.Windows.DependencyObject { }
            """
        );

        Assert.Single(sources);
        Assert.Contains("标题", sources[0].Text);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP004");
    }

    /// <summary>
    /// 同一类型上依赖属性名称重复时报告 <c>LENABP004</c>。
    /// </summary>
    [Fact]
    public void DependencyProperty_DuplicateName_ReportsLENABP004()
    {
        var (_, diagnostics) = Run(
            new DependencyPropertyGenerator(),
            """
            [Len.Abp.Wpf.DependencyProperty<string>("Title")]
            [Len.Abp.Wpf.DependencyProperty<string>("Title")]
            public partial class DemoControl : System.Windows.DependencyObject { }
            """
        );

        AssertDiagnostic(diagnostics, "LENABP004", "重复");
    }

    /// <summary>
    /// 仅大小写不同的名称视为重复，报告 <c>LENABP004</c>。
    /// </summary>
    /// <remarks>
    /// 生成的文件名是 <c>{类名}.{名称}.g.cs</c>，Roslyn 要求 hintName 大小写不敏感唯一。
    /// 若不提前拦截，生成器会因 hintName 冲突整体抛异常（CS8785），一个源文件都不产出。
    /// </remarks>
    [Theory]
    [InlineData("Title", "title")]
    [InlineData("title", "TITLE")]
    [InlineData("TiTlE", "title")]
    public void DependencyProperty_NamesDifferingOnlyByCase_ReportLENABP004(string first, string second)
    {
        var (sources, diagnostics) = Run(
            new DependencyPropertyGenerator(),
            $$"""
            [Len.Abp.Wpf.DependencyProperty<string>("{{first}}")]
            [Len.Abp.Wpf.DependencyProperty<string>("{{second}}")]
            public partial class DemoControl : System.Windows.DependencyObject { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "重复");

        // 不能退化成生成器崩溃：CS8785 表示 hintName 冲突未被拦截。
        Assert.DoesNotContain(diagnostics, d => d.Id == "CS8785");
    }

    /// <summary>
    /// 路由事件仅大小写不同时同样视为重复，且不触发 hintName 冲突。
    /// </summary>
    [Theory]
    [InlineData("Clicked", "clicked")]
    [InlineData("clicked", "CLICKED")]
    public void RoutedEvent_NamesDifferingOnlyByCase_ReportLENABP004(string first, string second)
    {
        var (sources, diagnostics) = Run(
            new RoutedEventGenerator(),
            $$"""
            [Len.Abp.Wpf.RoutedEvent("{{first}}", System.Windows.RoutingStrategy.Bubble)]
            [Len.Abp.Wpf.RoutedEvent("{{second}}", System.Windows.RoutingStrategy.Bubble)]
            public partial class DemoControl : System.Windows.UIElement { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "重复");
        Assert.DoesNotContain(diagnostics, d => d.Id == "CS8785");
    }

    /// <summary>
    /// 名称确实不同（不只是大小写）时正常生成两个源文件。
    /// </summary>
    [Fact]
    public void DependencyProperty_DistinctNames_GenerateBothSources()
    {
        var (sources, diagnostics) = Run(
            new DependencyPropertyGenerator(),
            """
            [Len.Abp.Wpf.DependencyProperty<string>("Title")]
            [Len.Abp.Wpf.DependencyProperty<string>("Subtitle")]
            public partial class DemoControl : System.Windows.DependencyObject { }
            """
        );

        Assert.Equal(2, sources.Count);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP004");
    }

    /// <summary>
    /// 依赖属性与路由事件同名时报告 <c>LENABP004</c>，且只报一条。
    /// </summary>
    /// <remarks>
    /// 两者都生成与名称同名的实例成员（属性 / 事件），落在同一成员空间，
    /// 编译器会报 <c>CS0102</c>。提前拦截比让用户看到晦涩的编译器错误更好。
    /// 按成员名去重，因此两个特性只产生一条诊断。
    /// </remarks>
    [Fact]
    public void DependencyPropertyAndRoutedEvent_SameName_ReportsSingleLENABP004()
    {
        var (sources, diagnostics) = Run(
            new DependencyPropertyGenerator(),
            """
            [Len.Abp.Wpf.DependencyProperty<string>("Title")]
            [Len.Abp.Wpf.RoutedEvent("Title", System.Windows.RoutingStrategy.Bubble)]
            public partial class DemoControl : System.Windows.DependencyObject { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "重复");
    }

    /// <summary>
    /// 附加属性与附加事件同名不报告诊断：它们生成带前缀的静态方法，不冲突。
    /// </summary>
    [Fact]
    public void AttachedPropertyAndAttachedEvent_SameName_DoesNotReportDuplicate()
    {
        var (_, diagnostics) = Run(
            new AttachedDependencyPropertyGenerator(),
            """
            [Len.Abp.Wpf.AttachedDependencyProperty<string>("Tag")]
            [Len.Abp.Wpf.AttachedRoutedEvent("Tag", System.Windows.RoutingStrategy.Bubble)]
            public static class DemoAttached { }
            """
        );

        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP004");
    }

    /// <summary>
    /// 同名特性组合的冲突矩阵。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 冲突有两种来源：支撑字段同名（<c>{名称}Property</c> / <c>{名称}Event</c>），
    /// 或裸名成员同名（属性 <c>X</c> 与事件 <c>X</c>）。附加版本不改变支撑字段名，
    /// 因此 <c>[DependencyProperty]</c> 与 <c>[AttachedDependencyProperty]</c> 会冲突。
    /// </para>
    /// <para>
    /// 不冲突的组合必须真的能编译：只断言"没有诊断"会漏掉生成器产出的非法代码，
    /// 所以这里用 <see cref="GeneratorTestHost.RunAll"/> 同时运行四个生成器并检查编译错误。
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("DependencyProperty", "AttachedDependencyProperty", true)]
    [InlineData("RoutedEvent", "AttachedRoutedEvent", true)]
    [InlineData("DependencyProperty", "RoutedEvent", true)]
    [InlineData("DependencyProperty", "AttachedRoutedEvent", false)]
    [InlineData("AttachedDependencyProperty", "RoutedEvent", false)]
    [InlineData("AttachedDependencyProperty", "AttachedRoutedEvent", false)]
    public void SameNameAttributePairs_ReportLENABP004OnlyWhenConflicting(
        string first,
        string second,
        bool conflicts
    )
    {
        var source = GeneratorTestSources.With(
            "namespace Demo\n"
                + "{\n"
                + $"    {AttributeUsage(first)}\n"
                + $"    {AttributeUsage(second)}\n"
                + "    public partial class Mixed : System.Windows.DependencyObject { }\n"
                + "}\n"
        );

        var (sources, errors, diagnostics) = GeneratorTestHost.RunAll(
            [
                new DependencyPropertyGenerator(),
                new AttachedDependencyPropertyGenerator(),
                new RoutedEventGenerator(),
                new AttachedRoutedEventGenerator(),
            ],
            source
        );

        if (conflicts)
        {
            AssertDiagnostic(diagnostics, "LENABP004", "X");
            Assert.Empty(sources);
            Assert.DoesNotContain(errors, error => error.Contains("CS0102", StringComparison.Ordinal));
            Assert.DoesNotContain(errors, error => error.Contains("CS0111", StringComparison.Ordinal));
            return;
        }

        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP004");
        Assert.Equal(2, sources.Count);
        Assert.DoesNotContain(errors, error => error.Contains("CS0102", StringComparison.Ordinal));
        Assert.DoesNotContain(errors, error => error.Contains("CS0111", StringComparison.Ordinal));
    }

    /// <summary>
    /// 实例版本与附加版本用不同名称时正常生成，不报告诊断。
    /// </summary>
    [Fact]
    public void DependencyPropertyAndAttachedProperty_DistinctNames_DoNotConflict()
    {
        var source = GeneratorTestSources.With(
            """
            namespace Demo
            {
                [Len.Abp.Wpf.DependencyProperty<string>("Label")]
                [Len.Abp.Wpf.AttachedDependencyProperty<string>("Tag")]
                public partial class Mixed : System.Windows.DependencyObject { }
            }
            """
        );

        var (sources, errors, diagnostics) = GeneratorTestHost.RunAll(
            [new DependencyPropertyGenerator(), new AttachedDependencyPropertyGenerator()],
            source
        );

        Assert.Equal(2, sources.Count);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP004");
        Assert.DoesNotContain(errors, error => error.Contains("CS0102", StringComparison.Ordinal));
    }

    /// <summary>
    /// 同名的依赖属性与附加属性并存时，两个生成器都不产出源文件。
    /// </summary>
    /// <remarks>
    /// 只报诊断不够：若仍生成代码，用户会在修复诊断之前先看到一堆
    /// <c>CS0102</c> / <c>CS0111</c>，无法判断哪个才是根因。
    /// </remarks>
    [Fact]
    public void DependencyPropertyAndAttachedProperty_SameName_GenerateNoSources()
    {
        var source = GeneratorTestSources.With(
            """
            namespace Demo
            {
                [Len.Abp.Wpf.DependencyProperty<string>("Label")]
                [Len.Abp.Wpf.AttachedDependencyProperty<string>("Label")]
                public partial class Mixed : System.Windows.DependencyObject { }
            }
            """
        );

        var (sources, errors, _) = GeneratorTestHost.RunAll(
            [new DependencyPropertyGenerator(), new AttachedDependencyPropertyGenerator()],
            source
        );

        Assert.Empty(sources);
        Assert.DoesNotContain(errors, error => error.Contains("CS0102", StringComparison.Ordinal));
    }

    /// <summary>
    /// 构造指定特性的用法文本，名称固定为 <c>X</c>。
    /// </summary>
    /// <param name="attributeName">特性简单名。</param>
    /// <returns>特性用法。</returns>
    private static string AttributeUsage(string attributeName) => attributeName switch
    {
        "RoutedEvent" or "AttachedRoutedEvent" =>
            $"[Len.Abp.Wpf.{attributeName}(\"X\", System.Windows.RoutingStrategy.Bubble)]",
        _ => $"[Len.Abp.Wpf.{attributeName}<string>(\"X\")]",
    };

    /// <summary>
    /// 泛型视图报告 <c>LENABP008</c>，且不生成无法编译的代码。
    /// </summary>
    /// <remarks>
    /// 生成的注册特性形如 <c>[ExposeKeyedService&lt;Page&lt;T&gt;&gt;]</c>，
    /// 而 C# 不允许特性参数使用类型参数（CS8968 / CS0416）。
    /// </remarks>
    [Fact]
    public void Mvvm_GenericView_ReportsLENABP008()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public class PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
            public partial class PageView<T> { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP008", "泛型");
    }

    /// <summary>
    /// 泛型与非泛型同名视图共存时，非泛型视图仍正常生成。
    /// </summary>
    /// <remarks>
    /// 两者的 hintName 曾都是 <c>{类名}.g.cs</c>，Roslyn 抛异常导致
    /// 该类一个源文件都不产出（CS8785）。现在 hintName 带泛型 arity 与生成器标识，
    /// 两者互不干扰。
    /// </remarks>
    [Fact]
    public void Mvvm_GenericAndNonGenericSameName_NonGenericStillGenerated()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public class PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "a")]
            public partial class PageView { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "b")]
            public partial class PageView<T> { }
            """
        );

        // 非泛型视图正常生成，泛型视图报 LENABP008 而不是让生成器整体崩溃。
        Assert.Single(sources);
        Assert.Contains("PageView", sources[0].HintName);
        Assert.DoesNotContain(diagnostics, d => d.Id == "CS8785");
        AssertDiagnostic(diagnostics, "LENABP008", "泛型");
    }

    /// <summary>
    /// 同一类型上依赖属性与路由事件同名时，两个生成器的 hintName 不冲突。
    /// </summary>
    /// <remarks>
    /// 文件名曾都是 <c>{类名}.{名称}.g.cs</c>，两个生成器产出同名文件，
    /// Roslyn 只保留一个，另一个成员静默丢失。
    /// </remarks>
    [Fact]
    public void DependencyPropertyAndRoutedEvent_HintNamesAreDistinct()
    {
        var snippet =
            """
            [Len.Abp.Wpf.DependencyProperty<string>("Title")]
            [Len.Abp.Wpf.RoutedEvent("Clicked", System.Windows.RoutingStrategy.Bubble)]
            public partial class DemoControl : System.Windows.DependencyObject { }
            """;

        var (dpSources, _) = Run(new DependencyPropertyGenerator(), snippet);
        var (reSources, _) = Run(new RoutedEventGenerator(), snippet);

        var dpHint = Assert.Single(dpSources).HintName;
        var reHint = Assert.Single(reSources).HintName;

        Assert.NotEqual(dpHint, reHint);
    }

    /// <summary>
    /// 路由事件的处理程序类型不是委托时报告 <c>LENABP004</c>。
    /// </summary>
    [Fact]
    public void RoutedEvent_NonDelegateHandlerType_ReportsLENABP004()
    {
        var (sources, diagnostics) = Run(
            new RoutedEventGenerator(),
            """
            [Len.Abp.Wpf.RoutedEvent("Changed", System.Windows.RoutingStrategy.Bubble, typeof(string))]
            public partial class DemoControl : System.Windows.UIElement { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "不是委托类型");
    }

    /// <summary>
    /// 路由事件的处理程序类型是委托时不报告 <c>LENABP004</c>。
    /// </summary>
    [Fact]
    public void RoutedEvent_DelegateHandlerType_DoesNotReportLENABP004()
    {
        var (sources, diagnostics) = Run(
            new RoutedEventGenerator(),
            """
            [Len.Abp.Wpf.RoutedEvent("Changed", System.Windows.RoutingStrategy.Bubble, typeof(System.Windows.RoutedEventHandler))]
            public partial class DemoControl : System.Windows.UIElement { }
            """
        );

        Assert.Single(sources);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP004");
    }

    /// <summary>
    /// 附加路由事件的处理程序类型不是委托时同样报告 <c>LENABP004</c>。
    /// </summary>
    /// <remarks>
    /// 回归：校验用前缀匹配识别特性族，<c>AttachedRoutedEventAttribute</c>
    /// 不以 <c>RoutedEventAttribute</c> 开头，校验被整体跳过，
    /// 生成器随后在取委托参数时抛 <c>NullReferenceException</c>（<c>LENABP005</c>）。
    /// </remarks>
    [Fact]
    public void AttachedRoutedEvent_NonDelegateHandlerType_ReportsLENABP004()
    {
        var (sources, diagnostics) = Run(
            new AttachedRoutedEventGenerator(),
            """
            [Len.Abp.Wpf.AttachedRoutedEvent("Changed", System.Windows.RoutingStrategy.Bubble, typeof(System.EventArgs))]
            public partial class DemoControl { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "不是委托类型");
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP005");
    }

    /// <summary>
    /// 路由事件的处理程序委托参数不足两个时报告 <c>LENABP004</c>，而不是让生成器崩溃。
    /// </summary>
    /// <remarks>
    /// 回归：生成器从 <c>DelegateInvokeMethod.Parameters[1]</c> 取事件参数类型，
    /// 单参数委托会抛 <c>IndexOutOfRangeException</c>（<c>LENABP005</c>）。
    /// </remarks>
    [Fact]
    public void RoutedEvent_SingleParameterDelegate_ReportsLENABP004()
    {
        var (sources, diagnostics) = Run(
            new RoutedEventGenerator(),
            """
            public delegate void SingleArgHandler(System.Windows.RoutedEventArgs e);

            [Len.Abp.Wpf.RoutedEvent<SingleArgHandler>("Changed", System.Windows.RoutingStrategy.Bubble)]
            public partial class DemoControl : System.Windows.UIElement { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP004", "至少有两个参数");
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP005");
    }

    /// <summary>
    /// 视图嵌套在泛型外层类型中时报告 <c>LENABP008</c>。
    /// </summary>
    /// <remarks>
    /// 回归：注册特性里会出现 <c>Outer&lt;T&gt;.PageView</c>，
    /// 其中的 <c>T</c> 是类型参数，编译器拒绝（CS0416）。
    /// 原先只检查视图自身的类型参数，外层泛型漏网。
    /// </remarks>
    [Fact]
    public void Mvvm_ViewNestedInGenericOuter_ReportsLENABP008()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public class PageViewModel { }

            public partial class Outer<T>
            {
                [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                public partial class PageView { }
            }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP008", "泛型");
    }

    /// <summary>
    /// 值类型视图模型报告 <c>LENABP010</c>，且不生成违反泛型约束的代码。
    /// </summary>
    /// <remarks>
    /// 回归：<c>IView&lt;TViewModel&gt;</c> 声明了 <c>where TViewModel : class</c>，
    /// 值类型视图模型会报 CS0452。
    /// </remarks>
    [Fact]
    public void Mvvm_ValueTypeViewModel_ReportsLENABP010()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public struct PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
            public partial class PageView { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP010", "值类型");
    }

    /// <summary>
    /// 开放泛型视图模型报告 <c>LENABP011</c>，且不生成未绑定泛型名的代码。
    /// </summary>
    /// <remarks>
    /// 回归：<c>IView&lt;TViewModel&gt;</c> 需要闭合的类型实参，
    /// 开放泛型会让生成的基类型变成 <c>IView&lt;PageViewModel&lt;&gt;&gt;</c>（CS7003）。
    /// </remarks>
    [Fact]
    public void Mvvm_OpenGenericViewModel_ReportsLENABP011()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public class PageViewModel<T> { }

            [Len.Abp.Wpf.Mvvm.View(typeof(PageViewModel<>), ViewName = "page")]
            public partial class PageView { }
            """
        );

        Assert.Empty(sources);
        AssertDiagnostic(diagnostics, "LENABP011", "开放泛型");
    }

    /// <summary>
    /// 闭合泛型视图模型不报告 <c>LENABP011</c>，正常生成代码。
    /// </summary>
    /// <remarks>
    /// 闭合泛型是合法用法：分部声明用未闭合的类型参数（<c>PageViewModel&lt;T&gt;</c>），
    /// 注册特性用闭合实参（<c>PageViewModel&lt;int&gt;</c>）。两者不能混用。
    /// </remarks>
    [Fact]
    public void Mvvm_ClosedGenericViewModel_DoesNotReportLENABP011()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public class PageViewModel<T> { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel<int>>(ViewName = "page")]
            public partial class PageView { }
            """
        );

        Assert.NotEmpty(sources);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP011");
    }

    /// <summary>
    /// 生成器内部异常报告 <c>LENABP005</c>，不再使用 <c>"Exception: "</c> 描述符。
    /// </summary>
    [Fact]
    public void GeneratorException_ReportsLENABP005()
    {        var (_, diagnostics) = Run(
            new ThrowingGenerator(),
            """
            [Len.Abp.Analyzer.Wpf.Tests.GeneratorDiagnosticsTests.Marker]
            public partial class Sample { }
            """
        );

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "LENABP005");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("InvalidOperationException", diagnostic.GetMessage());
    }

    private static (IReadOnlyList<GeneratedSource> Sources, IReadOnlyList<Diagnostic> Diagnostics) RunMvvm(string snippet) =>
        Run(new MvvmGenerator(), snippet);

    private static (IReadOnlyList<GeneratedSource> Sources, IReadOnlyList<Diagnostic> Diagnostics) Run(
        IIncrementalGenerator generator,
        string snippet
    ) => GeneratorTestHost.RunWithErrors(generator, GeneratorTestSources.With(snippet));

    private static void AssertDiagnostic(IReadOnlyList<Diagnostic> diagnostics, string id, string expectedText)
    {
        var diagnostic = Assert.Single(diagnostics, d => d.Id == id);

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.NotEqual(Location.None, diagnostic.Location);
        Assert.Contains(expectedText, diagnostic.GetMessage());
    }

    /// <summary>
    /// 测试用的标记特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MarkerAttribute : Attribute;

    /// <summary>
    /// 在准备数据时抛异常的生成器，用于覆盖 <c>LENABP005</c>。
    /// </summary>
    private sealed class ThrowingGenerator()
        : Len.Abp.Analyzer.BasicGenerator<object>("Len.Abp.Analyzer.Wpf.Tests.GeneratorDiagnosticsTests+MarkerAttribute")
    {
        protected override string CreateFileName((Len.Abp.Analyzer.ClassData Class, object Data, string Version) context) => "x.g.cs";

        protected override string CreateSourceCode((Len.Abp.Analyzer.ClassData Class, object Data, string Version) context) => "// x";

        protected override (Len.Abp.Analyzer.ClassData Class, object Data, string Version)? PrepareData(
            (Len.Abp.Analyzer.ClassContext, string) context
        ) => throw new InvalidOperationException("准备数据失败");
    }
}