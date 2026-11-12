using Len.Abp.Wpf;
using Xunit;

namespace Len.Abp.Wpf.Abstractions.Tests;

public class ParametersTests
{
    /// <summary>
    /// 从查询字符串创建时解析出所有键。
    /// </summary>
    [Fact]
    public void Create_WithQuery_ParsesAllKeys()
    {
        var parameters = IParameters.Create("?name=abc&id=42&flag=true");

        Assert.Equal(3, parameters.Count);
        Assert.Equal(["name", "id", "flag"], parameters.Keys);
    }

    /// <summary>
    /// 不传查询字符串时参数集合为空。
    /// </summary>
    [Fact]
    public void Create_WithoutQuery_IsEmpty()
    {
        var parameters = IParameters.Create();

        Assert.Equal(0, parameters.Count);
        Assert.Empty(parameters.Keys);
    }

    /// <summary>
    /// 查询字符串带或不带前导问号都能正确解析。
    /// </summary>
    [Fact]
    public void Create_WithLeadingQuestionMark_IsTolerated()
    {
        var withMark = IParameters.Create("?name=abc");
        var withoutMark = IParameters.Create("name=abc");

        Assert.Equal("abc", withMark.GetValue<string>("name"));
        Assert.Equal("abc", withoutMark.GetValue<string>("name"));
    }

    /// <summary>
    /// 索引器返回原始的字符串值。
    /// </summary>
    [Fact]
    public void Indexer_ReturnsRawStringValue()
    {
        var parameters = IParameters.Create("?name=abc");

        Assert.Equal("abc", parameters["name"]);
    }

    /// <summary>
    /// 索引器访问不存在的键时返回 null。
    /// </summary>
    [Fact]
    public void Indexer_ForMissingKey_ReturnsNull()
    {
        var parameters = IParameters.Create("?name=abc");

        Assert.Null(parameters["missing"]);
    }

    /// <summary>
    /// GetValue 能取到字符串类型的查询值。
    /// </summary>
    [Fact]
    public void GetValue_String_ReturnsQueryValue()
    {
        var parameters = IParameters.Create("?name=abc");

        Assert.Equal("abc", parameters.GetValue<string>("name"));
    }

    /// <summary>
    /// GetValue 能把查询字符串转换为 int。
    /// </summary>
    [Fact]
    public void GetValue_Int_ConvertsFromQueryString()
    {
        var parameters = IParameters.Create("?id=42");

        Assert.Equal(42, parameters.GetValue<int>("id"));
    }

    /// <summary>
    /// GetValue 能把查询字符串转换为 bool。
    /// </summary>
    [Fact]
    public void GetValue_Bool_ConvertsFromQueryString()
    {
        var parameters = IParameters.Create("?flag=true");

        Assert.True(parameters.GetValue<bool>("flag"));
    }

    /// <summary>
    /// GetValue 能把查询字符串转换为 Guid。
    /// </summary>
    [Fact]
    public void GetValue_Guid_ConvertsFromQueryString()
    {
        var expected = Guid.NewGuid();
        var parameters = IParameters.Create($"?id={expected}");

        Assert.Equal(expected, parameters.GetValue<Guid>("id"));
    }

    /// <summary>
    /// GetValue 能按名称把查询字符串转换为枚举。
    /// </summary>
    [Fact]
    public void GetValue_Enum_ConvertsFromName()
    {
        var parameters = IParameters.Create("?mode=Second");

        Assert.Equal(SampleMode.Second, parameters.GetValue<SampleMode>("mode"));
    }

    /// <summary>
    /// GetValue 能按数值把查询字符串转换为枚举。
    /// </summary>
    [Fact]
    public void GetValue_Enum_ConvertsFromNumericString()
    {
        var parameters = IParameters.Create("?mode=3");

        Assert.Equal(SampleMode.Third, parameters.GetValue<SampleMode>("mode"));
    }

    /// <summary>
    /// GetValue 能把查询字符串转换为可空 int。
    /// </summary>
    [Fact]
    public void GetValue_NullableInt_ConvertsFromQueryString()
    {
        var parameters = IParameters.Create("?id=42");

        Assert.Equal(42, parameters.GetValue<int?>("id"));
    }

    /// <summary>
    /// GetValue 访问不存在的键时返回默认值。
    /// </summary>
    [Fact]
    public void GetValue_MissingKey_ReturnsDefault()
    {
        var parameters = IParameters.Create("?name=abc");

        Assert.Equal(0, parameters.GetValue<int>("missing"));
        Assert.Null(parameters.GetValue<string>("missing"));
    }

    /// <summary>
    /// GetValue 遇到无法转换的值时返回默认值而不抛异常。
    /// </summary>
    [Fact]
    public void GetValue_UnconvertibleValue_ReturnsDefaultInsteadOfThrowing()
    {
        var parameters = IParameters.Create("?name=abc");

        Assert.Equal(0, parameters.GetValue<int>("name"));
    }

    /// <summary>
    /// GetValue 取回通过 Add 添加的对象时返回同一实例。
    /// </summary>
    [Fact]
    public void GetValue_AddedObject_ReturnsSameInstance()
    {
        var parameters = IParameters.Create();
        var value = new SamplePayload("x");
        parameters.Add("payload", value);

        Assert.Same(value, parameters.GetValue<SamplePayload>("payload"));
    }

    /// <summary>
    /// GetValue 能把通过 Add 添加的 int 转换为字符串。
    /// </summary>
    [Fact]
    public void GetValue_AddedInt_ConvertsToString()
    {
        var parameters = IParameters.Create();
        parameters.Add("count", 7);

        Assert.Equal("7", parameters.GetValue<string>("count"));
    }

    /// <summary>
    /// TryGetValue 对存在的键返回 true。
    /// </summary>
    [Fact]
    public void TryGetValue_ExistingKey_ReturnsTrue()
    {
        var parameters = IParameters.Create("?id=42");

        var found = parameters.TryGetValue<int>("id", out var value);

        Assert.True(found);
        Assert.Equal(42, value);
    }

    /// <summary>
    /// TryGetValue 对不存在的键返回 false。
    /// </summary>
    [Fact]
    public void TryGetValue_MissingKey_ReturnsFalse()
    {
        var parameters = IParameters.Create("?id=42");

        var found = parameters.TryGetValue<int>("missing", out var value);

        Assert.False(found);
        Assert.Equal(0, value);
    }

    /// <summary>
    /// TryGetValue 遇到无法转换的值时返回 false。
    /// </summary>
    [Fact]
    public void TryGetValue_UnconvertibleValue_ReturnsFalse()
    {
        var parameters = IParameters.Create("?name=abc");

        var found = parameters.TryGetValue<int>("name", out var value);

        Assert.False(found);
        Assert.Equal(0, value);
    }

    /// <summary>
    /// TryGetValue 与其它访问方式一致，传入 null 键时抛出异常。
    /// </summary>
    [Fact]
    public void TryGetValue_NullKey_Throws()
    {
        var parameters = IParameters.Create("?id=42");

        Assert.Throws<ArgumentNullException>(() => parameters.TryGetValue<int>(null!, out _));
    }

    /// <summary>
    /// GetValues 返回同名键的全部字符串值。
    /// </summary>
    [Fact]
    public void GetValues_MultipleValues_ReturnsAllConverted()
    {
        var parameters = IParameters.Create("?tag=a&tag=b&tag=c");

        Assert.Equal(["a", "b", "c"], parameters.GetValues<string>("tag"));
    }

    /// <summary>
    /// GetValues 返回同名键的全部数值并完成转换。
    /// </summary>
    [Fact]
    public void GetValues_MultipleNumericValues_ReturnsAllConverted()
    {
        var parameters = IParameters.Create("?id=1&id=2");

        Assert.Equal([1, 2], parameters.GetValues<int>("id"));
    }

    /// <summary>
    /// GetValues 对不存在的键返回空集合。
    /// </summary>
    [Fact]
    public void GetValues_MissingKey_ReturnsEmpty()
    {
        var parameters = IParameters.Create("?tag=a");

        Assert.Empty(parameters.GetValues<string>("missing"));
    }

    /// <summary>
    /// ContainsKey 区分大小写。
    /// </summary>
    [Fact]
    public void ContainsKey_IsCaseSensitive()
    {
        var parameters = IParameters.Create("?Name=abc");

        Assert.True(parameters.ContainsKey("Name"));
        Assert.False(parameters.ContainsKey("name"));
    }

    /// <summary>
    /// Add 支持链式调用。
    /// </summary>
    [Fact]
    public void Add_IsChainable()
    {
        var parameters = IParameters.Create()
            .Add("a", 1)
            .Add("b", 2);

        Assert.Equal(2, parameters.Count);
        Assert.Equal(1, parameters.GetValue<int>("a"));
        Assert.Equal(2, parameters.GetValue<int>("b"));
    }

    /// <summary>
    /// 从查询字符串创建时对编码值进行解码。
    /// </summary>
    [Fact]
    public void Create_WithEncodedValue_DecodesIt()
    {
        var parameters = IParameters.Create("?name=hello%20world");

        Assert.Equal("hello world", parameters.GetValue<string>("name"));
    }

    /// <summary>
    /// 可空枚举能从名称解析。
    /// </summary>
    [Fact]
    public void GetValue_NullableEnum_ConvertsFromName()
    {
        var parameters = IParameters.Create("?mode=Second");

        Assert.Equal(SampleMode.Second, parameters.GetValue<SampleMode?>("mode"));
    }

    /// <summary>
    /// 可空枚举遇到无法解析的值时返回 null。
    /// </summary>
    [Fact]
    public void GetValue_NullableEnum_UnparsableValue_ReturnsNull()
    {
        var parameters = IParameters.Create("?mode=NotAMode");

        Assert.Null(parameters.GetValue<SampleMode?>("mode"));
    }

    /// <summary>
    /// 可空 Guid 能从字符串解析。
    /// </summary>
    [Fact]
    public void GetValue_NullableGuid_ConvertsFromString()
    {
        var expected = Guid.NewGuid();
        var parameters = IParameters.Create($"?id={expected}");

        Assert.Equal(expected, parameters.GetValue<Guid?>("id"));
    }

    /// <summary>
    /// 可空 Guid 遇到无法解析的值时返回 null。
    /// </summary>
    [Fact]
    public void GetValue_NullableGuid_UnparsableValue_ReturnsNull()
    {
        var parameters = IParameters.Create("?id=not-a-guid");

        Assert.Null(parameters.GetValue<Guid?>("id"));
    }

    /// <summary>
    /// 可空数值遇到无法解析的值时返回 null。
    /// </summary>
    [Fact]
    public void GetValue_NullableInt_UnparsableValue_ReturnsNull()
    {
        var parameters = IParameters.Create("?id=abc");

        Assert.Null(parameters.GetValue<int?>("id"));
    }

    /// <summary>
    /// 非 IParsable 的引用类型从字符串转换时返回默认值。
    /// </summary>
    [Fact]
    public void GetValue_NonParsableReferenceType_ReturnsDefault()
    {
        var parameters = IParameters.Create("?payload=abc");

        Assert.Null(parameters.GetValue<SamplePayload>("payload"));
    }

    /// <summary>
    /// 跨数值类型转换走回退路径，仍能正确转换。
    /// </summary>
    [Fact]
    public void GetValue_CrossNumericType_ConvertsViaFallback()
    {
        var parameters = IParameters.Create();
        parameters.Add("count", 42);

        Assert.Equal(42L, parameters.GetValue<long>("count"));
        Assert.Equal(42.0, parameters.GetValue<double>("count"));
    }

    /// <summary>
    /// 数值转枚举走回退路径。
    /// </summary>
    [Fact]
    public void GetValue_NumericToEnum_ConvertsViaFallback()
    {
        var parameters = IParameters.Create();
        parameters.Add("mode", 3);

        Assert.Equal(SampleMode.Third, parameters.GetValue<SampleMode>("mode"));
    }

    /// <summary>
    /// 数值转枚举不校验定义范围，超出范围的数值仍被接受。
    /// </summary>
    [Fact]
    public void GetValue_OutOfRangeNumericToEnum_IsAccepted()
    {
        var parameters = IParameters.Create();
        parameters.Add("mode", 999);

        Assert.Equal((SampleMode)999, parameters.GetValue<SampleMode>("mode"));
    }

    /// <summary>
    /// 已存储的引用类型实例直接返回同一对象。
    /// </summary>
    [Fact]
    public void GetValue_StoredReferenceType_ReturnsSameInstance()
    {
        var payload = new SamplePayload("x");
        var parameters = IParameters.Create();
        parameters.Add("payload", payload);

        Assert.Same(payload, parameters.GetValue<SamplePayload>("payload"));
    }

    /// <summary>
    /// 非 IConvertible 的引用类型从字符串转换时返回默认值。
    /// </summary>
    [Fact]
    public void GetValue_NonConvertibleReferenceTypeFromString_ReturnsDefault()
    {
        var parameters = IParameters.Create("?payload=abc");

        Assert.Null(parameters.GetValue<SamplePayload>("payload"));
    }

    /// <summary>
    /// 跨类型转换失败（溢出）时返回默认值。
    /// </summary>
    [Fact]
    public void GetValue_OverflowingConversion_ReturnsDefault()
    {
        var parameters = IParameters.Create();
        parameters.Add("big", long.MaxValue);

        Assert.Equal(0, parameters.GetValue<int>("big"));
    }

    /// <summary>
    /// 键存在但值为 null 时，值类型返回默认值。
    /// </summary>
    [Fact]
    public void GetValue_NullValueForValueType_ReturnsDefault()
    {
        var parameters = IParameters.Create();
        parameters.Add("id", DBNull.Value);

        Assert.Equal(0, parameters.GetValue<int>("id"));
    }

    /// <summary>
    /// 枚举器能遍历所有条目。
    /// </summary>
    [Fact]
    public void GetEnumerator_ReturnsAllEntries()
    {
        var parameters = IParameters.Create("?a=1&b=2");

        var count = 0;
        foreach (var entry in parameters)
        {
            count++;
            Assert.False(string.IsNullOrEmpty(entry.Key));
        }

        Assert.Equal(2, count);
    }

    /// <summary>
    /// 非泛型枚举与泛型枚举返回相同条目。
    /// </summary>
    [Fact]
    public void NonGenericEnumerator_ReturnsAllEntries()
    {
        var parameters = IParameters.Create("?a=1&b=2");

        var count = 0;
        foreach (var _ in (System.Collections.IEnumerable)parameters)
        {
            count++;
        }

        Assert.Equal(2, count);
    }

    /// <summary>
    /// 读取已解析的字符串值不产生堆分配。
    /// </summary>
    [Fact]
    public void GetValue_String_DoesNotAllocate()
    {
        var parameters = IParameters.Create("?name=abc");
        Assert.Equal("abc", parameters.GetValue<string>("name"));

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            parameters.GetValue<string>("name");
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>
    /// 读取已解析的数值不产生堆分配（不装箱）。
    /// </summary>
    [Fact]
    public void GetValue_Int_DoesNotAllocate()
    {
        var parameters = IParameters.Create("?id=42");
        Assert.Equal(42, parameters.GetValue<int>("id"));

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            parameters.GetValue<int>("id");
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>
    /// 读取已解析的 Guid 不产生堆分配。
    /// </summary>
    [Fact]
    public void GetValue_Guid_DoesNotAllocate()
    {
        var parameters = IParameters.Create($"?id={Guid.Empty}");
        Assert.Equal(Guid.Empty, parameters.GetValue<Guid>("id"));

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            parameters.GetValue<Guid>("id");
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>
    /// 读取已解析的可空值类型不产生堆分配。
    /// </summary>
    [Fact]
    public void GetValue_NullableInt_DoesNotAllocate()
    {
        var parameters = IParameters.Create("?id=42");
        Assert.Equal(42, parameters.GetValue<int?>("id"));

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            parameters.GetValue<int?>("id");
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>
    /// 读取已解析的枚举不产生堆分配。
    /// </summary>
    [Fact]
    public void GetValue_Enum_DoesNotAllocate()
    {
        var parameters = IParameters.Create("?mode=Second");
        Assert.Equal(SampleMode.Second, parameters.GetValue<SampleMode>("mode"));

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            parameters.GetValue<SampleMode>("mode");
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>
    /// ContainsKey 不产生堆分配（不使用捕获变量的 lambda）。
    /// </summary>
    [Fact]
    public void ContainsKey_DoesNotAllocate()
    {
        var parameters = IParameters.Create("?a=1&b=2&c=3");
        Assert.True(parameters.ContainsKey("c"));

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            parameters.ContainsKey("c");
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>
    /// 索引器不产生堆分配。
    /// </summary>
    [Fact]
    public void Indexer_DoesNotAllocate()
    {
        var parameters = IParameters.Create("?a=1&b=2&c=3");
        Assert.NotNull(parameters["c"]);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            _ = parameters["c"];
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>
    /// Keys 在集合未变更时复用缓存，不产生堆分配。
    /// </summary>
    [Fact]
    public void Keys_DoesNotAllocateWhenUnchanged()
    {
        var parameters = IParameters.Create("?a=1&b=2&c=3");
        Assert.Equal(3, parameters.Keys.Count());

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            _ = parameters.Keys.Count();
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>
    /// 添加新条目后 Keys 缓存失效，能反映最新键集合。
    /// </summary>
    [Fact]
    public void Keys_RefreshesAfterAdd()
    {
        var parameters = IParameters.Create("?a=1");
        Assert.Equal(["a"], parameters.Keys);

        parameters.Add("b", 2);

        Assert.Equal(["a", "b"], parameters.Keys);
    }

    /// <summary>
    /// GetValues 返回的集合可安全枚举多次。
    /// </summary>
    [Fact]
    public void GetValues_CanBeEnumeratedRepeatedly()
    {
        var parameters = IParameters.Create("?tag=a&tag=b");

        var values = parameters.GetValues<string>("tag");

        Assert.Equal(["a", "b"], values);
        Assert.Equal(["a", "b"], values);
    }

    public enum SampleMode
    {
        First = 1,
        Second = 2,
        Third = 3,
    }

    public sealed record SamplePayload(string Value);
}
