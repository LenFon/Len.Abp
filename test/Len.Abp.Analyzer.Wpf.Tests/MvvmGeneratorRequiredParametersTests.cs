using Len.Abp.Analyzer.TestSupport;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖 <c>MvvmGenerator</c> 对 <c>RequiredParameters</c> 的解析与代码生成。
/// </summary>
public class MvvmGeneratorRequiredParametersTests
{
    /// <summary>
    /// 只声明名称时生成 null 类型。
    /// </summary>
    [Fact]
    public void NameOnly_GeneratesNullType()
    {
        var generated = Generate("""RequiredParameters = new[] { "Id" }""");

        Assert.Contains("[\"Id\"] = null", generated);
    }

    /// <summary>
    /// 名称 + 类型时生成 typeof 表达式。
    /// </summary>
    [Fact]
    public void NameWithType_GeneratesTypeof()
    {
        var generated = Generate("""RequiredParameters = new[] { "Id:int" }""");

        Assert.Contains("[\"Id\"] = typeof(int)", generated);
    }

    /// <summary>
    /// 冒号两侧允许空白。
    /// </summary>
    [Fact]
    public void NameWithType_AllowsWhitespace()
    {
        var generated = Generate("""RequiredParameters = new[] { "Id : int" }""");

        Assert.Contains("[\"Id\"] = typeof(int)", generated);
    }

    /// <summary>
    /// 两种写法可以混用。
    /// </summary>
    [Fact]
    public void MixedForms_AreSupported()
    {
        var generated = Generate("""RequiredParameters = new[] { "Id:int", "Source" }""");

        Assert.Contains("[\"Id\"] = typeof(int)", generated);
        Assert.Contains("[\"Source\"] = null", generated);
    }

    /// <summary>
    /// 可空引用类型与引用类型等价，不生成 Nullable。
    /// </summary>
    [Fact]
    public void NullableReferenceType_IsSameAsReferenceType()
    {
        var generated = Generate("""RequiredParameters = new[] { "Filter:string?" }""");

        Assert.Contains("[\"Filter\"] = typeof(string)", generated);
        Assert.DoesNotContain("Nullable<string>", generated);
    }

    /// <summary>
    /// 可空值类型生成 Nullable&lt;T&gt;。
    /// </summary>
    [Fact]
    public void NullableValueType_GeneratesNullable()
    {
        var generated = Generate("""RequiredParameters = new[] { "Id:int?" }""");

        Assert.Contains("typeof(global::System.Nullable<int>)", generated);
    }

    /// <summary>
    /// 非关键字别名的值类型同样识别可空性。
    /// </summary>
    /// <remarks>
    /// 早期实现按硬编码的类型名清单判断值类型，只覆盖常用别名，
    /// <c>DateTime?</c> 这类写法会被当成引用类型而丢掉 <c>Nullable&lt;T&gt;</c>，
    /// 导致类型校验漏掉 null。现在按解析出的符号判定，清单不再参与。
    /// </remarks>
    [Theory]
    [InlineData("DateTime", "global::System.DateTime")]
    [InlineData("Guid", "global::System.Guid")]
    [InlineData("TimeSpan", "global::System.TimeSpan")]
    public void NullableNonAliasValueType_GeneratesNullable(string typeName, string expected)
    {
        var generated = Generate($$"""RequiredParameters = new[] { "Value:{{typeName}}?" }""");

        Assert.Contains($"typeof(global::System.Nullable<{expected}>)", generated);
    }

    /// <summary>
    /// 自定义结构体的可空写法同样生成 Nullable&lt;T&gt;。
    /// </summary>
    [Fact]
    public void NullableCustomStruct_GeneratesNullable()
    {
        var generated = Generate(
            """RequiredParameters = new[] { "Value:Mode?" }""",
            extraType: "public struct Mode { }"
        );

        Assert.Contains("typeof(global::System.Nullable<global::Demo.Mode>)", generated);
    }

    /// <summary>
    /// 生成的字典使用大小写不敏感比较器。
    /// </summary>
    [Fact]
    public void GeneratedDictionary_UsesOrdinalIgnoreCase()
    {
        var generated = Generate("""RequiredParameters = new[] { "Id" }""");

        Assert.Contains("global::System.StringComparer.OrdinalIgnoreCase", generated);
    }

    /// <summary>
    /// 未声明 RequiredParameters 时不生成成员。
    /// </summary>
    [Fact]
    public void NoRequiredParameters_DoesNotGenerateMember()
    {
        var generated = Generate(null);

        Assert.DoesNotContain("RequiredNavigationParameters", generated);
    }

    /// <summary>
    /// 生成的成员必须是实例属性，否则类不会实现 <c>INavigationContract</c> 的实例成员。
    /// </summary>
    /// <remarks>
    /// 只断言文本包含 <c>["Id"] = typeof(int)</c> 无法发现"生成为静态属性"这类缺陷：
    /// 静态成员不实现接口的实例成员，编译器静默回落到接口默认实现，
    /// 校验器读到的永远是空字典。
    /// </remarks>
    [Fact]
    public void RequiredParameters_GeneratesInstanceProperty_NotStatic()
    {
        var generated = Generate("""RequiredParameters = new[] { "Id:int" }""");

        Assert.Contains(
            "public global::System.Collections.Generic.IReadOnlyDictionary<string, global::System.Type?> RequiredNavigationParameters =>",
            generated
        );
        Assert.DoesNotContain(
            "public static global::System.Collections.Generic.IReadOnlyDictionary<string, global::System.Type?> RequiredNavigationParameters",
            generated
        );
    }

    /// <summary>
    /// 契约成员生成到视图模型上，而不是视图上。
    /// </summary>
    /// <remarks>
    /// 视图模型才是参数的消费者，框架的其它导航契约（<c>INavigationAware</c>）也都在它上面。
    /// 视图模型是普通类，测试里直接 <c>new</c> 就能断言契约；视图是 WPF 控件，
    /// 测试通常只能用替身，真实产出从未被验证过。
    /// 视图与视图模型分成两个源文件后，这条约束可以直接按文件断言。
    /// </remarks>
    [Fact]
    public void RequiredParameters_GeneratedOnViewModel_NotOnView()
    {
        var source = GeneratorTestSources.With(
            """
            namespace Demo
            {
                public partial class PageViewModel { }

                [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page", RequiredParameters = new[] { "Id:int" })]
                public partial class PageView { }
            }
            """
        );

        var sources = GeneratorTestHost.Run(new MvvmGenerator(), source);

        var viewModel = sources.ViewModelSource().Text;
        var view = sources.ViewSource().Text;

        // 视图模型声明实现契约接口，成员在它的类型体内。
        Assert.Contains("partial class PageViewModel :", viewModel);
        Assert.Contains("global::Len.Abp.Wpf.Navigation.INavigationContract", viewModel);
        Assert.Contains("RequiredNavigationParameters", viewModel);

        // 视图文件里不应出现契约成员。
        Assert.DoesNotContain("RequiredNavigationParameters", view);
    }

    /// <summary>
    /// 字典内容放在静态字段里由所有实例共享，实例属性只做转发。
    /// </summary>
    /// <remarks>
    /// 不在这里编译生成结果：测试桩的 WPF 类型会与宿主引用的真实 WPF 冲突（CS0101），
    /// 这类用例只能做文本断言。接口实现的行为验证由
    /// <c>RequiredParameters_GeneratesInstanceProperty_NotStatic</c> 覆盖。
    /// </remarks>
    [Fact]
    public void RequiredParameters_StoresDictionaryInStaticField()
    {
        var generated = Generate("""RequiredParameters = new[] { "Id:int", "Source" }""");

        Assert.Contains("private static readonly", generated);
        Assert.Contains("RequiredNavigationParametersValue =", generated);
        Assert.Contains("=> RequiredNavigationParametersValue;", generated);
        Assert.Contains("[\"Id\"] = typeof(int)", generated);
        Assert.Contains("[\"Source\"] = null", generated);
    }

    /// <summary>
    /// 空字符串与重复名称被跳过。
    /// </summary>
    [Fact]
    public void EmptyAndDuplicateNames_AreSkipped()
    {
        var generated = Generate("""RequiredParameters = new[] { "Id", "", "id", "Other" }""");

        // 重复（大小写不同）只保留第一个。
        Assert.Equal(1, CountOccurrences(generated, "[\"Id\"]"));
        Assert.Contains("[\"Other\"]", generated);
    }

    /// <summary>
    /// 只有空白字符的名称被跳过，不生成空键。
    /// </summary>
    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void WhitespaceOnlyName_IsSkipped(string name)
    {
        var generated = Generate($$"""RequiredParameters = new[] { "{{name}}", "Id" }""");

        Assert.Contains("[\"Id\"]", generated);
        Assert.DoesNotContain("[\"\"]", generated);
        Assert.DoesNotContain("[\" \"]", generated);
    }

    /// <summary>
    /// 冒号后没有类型名时退化为只校验存在性。
    /// </summary>
    [Theory]
    [InlineData("Id:")]
    [InlineData("Id:   ")]
    public void EmptyTypeName_FallsBackToNull(string declaration)
    {
        var generated = Generate($$"""RequiredParameters = new[] { "{{declaration}}" }""");

        Assert.Contains("[\"Id\"] = null", generated);
    }

    /// <summary>
    /// 冒号前没有名称时整条声明被跳过。
    /// </summary>
    [Theory]
    [InlineData(":int")]
    [InlineData("   :int")]
    public void EmptyNameWithType_IsSkipped(string declaration)
    {
        var generated = Generate($$"""RequiredParameters = new[] { "{{declaration}}", "Id" }""");

        Assert.Contains("[\"Id\"]", generated);
        Assert.DoesNotContain("[\"\"]", generated);
    }

    /// <summary>
    /// 名称本身包含多个冒号时只按第一个冒号切分，类型名保留剩余部分。
    /// </summary>
    /// <remarks>
    /// 剩余部分不是合法类型名，因此退化为只校验存在性，但名称必须完整保留。
    /// </remarks>
    [Fact]
    public void NameWithMultipleColons_SplitsOnFirstOnly()
    {
        var generated = Generate("""RequiredParameters = new[] { "Id:int:extra" }""");

        Assert.Contains("[\"Id\"]", generated);
    }

    /// <summary>
    /// 超长参数名完整保留，不被截断。
    /// </summary>
    [Fact]
    public void VeryLongName_IsPreserved()
    {
        var name = new string('N', 1_000);

        var generated = Generate($$"""RequiredParameters = new[] { "{{name}}:int" }""");

        Assert.Contains($"[\"{name}\"] = typeof(int)", generated);
    }

    /// <summary>
    /// 声明数量很大时全部生成，且不产生重复键。
    /// </summary>
    [Fact]
    public void ManyDeclarations_AreAllGenerated()
    {
        var declarations = string.Join(", ", Enumerable.Range(0, 100).Select(i => $"\"P{i}:int\""));

        var generated = Generate($"RequiredParameters = new[] {{ {declarations} }}");

        Assert.Contains("[\"P0\"] = typeof(int)", generated);
        Assert.Contains("[\"P99\"] = typeof(int)", generated);
        Assert.Equal(100, CountOccurrences(generated, "= typeof(int)"));
    }

    /// <summary>
    /// 空数组不生成成员，与未声明一致。
    /// </summary>
    [Fact]
    public void EmptyArray_DoesNotGenerateMember()
    {
        var generated = Generate("""RequiredParameters = new string[0]""");

        Assert.DoesNotContain("RequiredNavigationParameters", generated);
    }

    /// <summary>
    /// 无法解析的类型名退化为只校验存在性，不影响生成。
    /// </summary>
    [Fact]
    public void UnresolvableType_FallsBackToNull()
    {
        var generated = Generate("""RequiredParameters = new[] { "Id:NotAType" }""");

        Assert.Contains("[\"Id\"] = null", generated);
    }

    /// <summary>
    /// 所有 C# 关键字别名都解析为对应的系统类型。
    /// </summary>
    [Theory]
    [InlineData("long", "typeof(long)")]
    [InlineData("short", "typeof(short)")]
    [InlineData("byte", "typeof(byte)")]
    [InlineData("sbyte", "typeof(sbyte)")]
    [InlineData("uint", "typeof(uint)")]
    [InlineData("ulong", "typeof(ulong)")]
    [InlineData("ushort", "typeof(ushort)")]
    [InlineData("float", "typeof(float)")]
    [InlineData("double", "typeof(double)")]
    [InlineData("decimal", "typeof(decimal)")]
    [InlineData("bool", "typeof(bool)")]
    [InlineData("char", "typeof(char)")]
    [InlineData("string", "typeof(string)")]
    [InlineData("object", "typeof(object)")]
    public void KeywordAlias_ResolvesToSystemType(string alias, string expected)
    {
        var generated = Generate($$"""RequiredParameters = new[] { "Value:{{alias}}" }""");

        Assert.Contains($"[\"Value\"] = {expected}", generated);
    }

    /// <summary>
    /// 可空值类型的关键字别名同样包上 <c>Nullable&lt;T&gt;</c>。
    /// </summary>
    [Theory]
    [InlineData("long")]
    [InlineData("decimal")]
    [InlineData("bool")]
    public void NullableKeywordAlias_GeneratesNullable(string alias)
    {
        var generated = Generate($$"""RequiredParameters = new[] { "Value:{{alias}}?" }""");

        Assert.Contains($"typeof(global::System.Nullable<{alias}>)", generated);
    }

    /// <summary>
    /// 相对类型名先按视图所在命名空间解析。
    /// </summary>
    [Fact]
    public void RelativeTypeName_ResolvesInViewNamespace()
    {
        var generated = Generate("""RequiredParameters = new[] { "Mode:Mode" }""", extraType: "public enum Mode { A, B }");

        Assert.Contains("[\"Mode\"] = typeof(global::Demo.Mode)", generated);
    }

    /// <summary>
    /// 命名空间中找不到时回退到 <c>System</c> 命名空间。
    /// </summary>
    [Fact]
    public void UnqualifiedSystemTypeName_FallsBackToSystemNamespace()
    {
        var generated = Generate("""RequiredParameters = new[] { "Id:Guid" }""");

        Assert.Contains("[\"Id\"] = typeof(global::System.Guid)", generated);
    }

    /// <summary>
    /// 完全限定名直接解析。
    /// </summary>
    [Fact]
    public void FullyQualifiedTypeName_ResolvesDirectly()
    {
        var generated = Generate("""RequiredParameters = new[] { "When:System.DateTime" }""");

        Assert.Contains("[\"When\"] = typeof(global::System.DateTime)", generated);
    }

    /// <summary>
    /// 生成的字典初始化器语法正确，不出现空的 typeof。
    /// </summary>
    /// <remarks>
    /// 这里只断言生成文本，不做编译验证：<see cref="GeneratorTestSources.Stubs"/>
    /// 含 WPF 桩类型，而测试宿主本身也引用了真实 WPF，两者会冲突（CS0101）。
    /// 编译验证由 <see cref="MvvmGeneratorTests"/> 中不引用 WPF 的用例覆盖。
    /// </remarks>
    [Fact]
    public void GeneratedCode_ContainsValidDictionaryInitializer()
    {
        var source = GeneratorTestSources.With(
            """
            namespace Demo
            {
                public partial class PageViewModel { }

                [Len.Abp.Wpf.Mvvm.View<PageViewModel>(
                    ViewName = "page",
                    RequiredParameters = new[] { "Id:int", "Source" })]
                public partial class PageView { }
            }
            """
        );

        var sources = GeneratorTestHost.Run(new MvvmGenerator(), source);
        var text = sources.ViewModelSource().Text;

        Assert.Contains("[\"Id\"] = typeof(int)", text);
        Assert.Contains("[\"Source\"] = null", text);
        Assert.DoesNotContain("typeof()", text);
    }

    private static string Generate(string? requiredParameters, string? extraType = null)
    {
        var attribute = requiredParameters is null
            ? """[Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]"""
            : $"""[Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page", {requiredParameters})]""";

        var source = GeneratorTestSources.With(
            "namespace Demo\n"
                + "{\n"
                // 契约成员生成到视图模型上，因此它必须是 partial。
                + "    public partial class PageViewModel { }\n\n"
                + (extraType is null ? string.Empty : "    " + extraType + "\n\n")
                + "    " + attribute + "\n"
                + "    public partial class PageView { }\n"
                + "}\n"
        );

        var sources = GeneratorTestHost.Run(new MvvmGenerator(), source);

        return sources.ViewModelSource().Text;
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
