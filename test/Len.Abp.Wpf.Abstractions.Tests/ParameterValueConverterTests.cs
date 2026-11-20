using Len.Abp.Wpf;
using System.Numerics;
using Xunit;

namespace Len.Abp.Wpf.Abstractions.Tests;

/// <summary>
/// 覆盖 <see cref="ParameterValueConverter"/> 的类型兼容判定。
/// </summary>
/// <remarks>
/// 该判定必须与 <c>IParameters.TryGetValue{T}</c> 的转换规则一致，
/// 否则会出现"导航校验通过但读取时抛异常"的矛盾，因此这里逐条锁定各分支。
/// </remarks>
public class ParameterValueConverterTests
{
    /// <summary>
    /// 目标类型为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void CanConvert_NullTargetType_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ParameterValueConverter.CanConvert(null!, "x"));
    }

    /// <summary>
    /// null 只能赋给引用类型或可空值类型。
    /// </summary>
    [Theory]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int?), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(SampleMode?), true)]
    [InlineData(typeof(SampleMode), false)]
    public void CanConvert_NullValue_DependsOnTargetType(Type targetType, bool expected)
    {
        Assert.Equal(expected, ParameterValueConverter.CanConvert(targetType, null));
    }

    /// <summary>
    /// 原始值已经是目标类型的实例时直接通过。
    /// </summary>
    [Fact]
    public void CanConvert_AlreadyAssignable_ReturnsTrue()
    {
        Assert.True(ParameterValueConverter.CanConvert(typeof(int), 42));
        Assert.True(ParameterValueConverter.CanConvert(typeof(object), 42));
        Assert.True(ParameterValueConverter.CanConvert(typeof(string), "text"));
    }

    /// <summary>
    /// 字符串按枚举名解析，大小写不敏感。
    /// </summary>
    [Theory]
    [InlineData("Second", true)]
    [InlineData("second", true)]
    [InlineData("NotAMember", false)]
    public void CanConvert_StringToEnum_ParsesByName(string raw, bool expected)
    {
        Assert.Equal(expected, ParameterValueConverter.CanConvert(typeof(SampleMode), raw));
    }

    /// <summary>
    /// 可空枚举走同一套字符串解析规则。
    /// </summary>
    [Fact]
    public void CanConvert_StringToNullableEnum_ParsesByName()
    {
        Assert.True(ParameterValueConverter.CanConvert(typeof(SampleMode?), "Second"));
        Assert.False(ParameterValueConverter.CanConvert(typeof(SampleMode?), "NotAMember"));
    }

    /// <summary>
    /// 实现 <see cref="IParsable{TSelf}"/> 的类型通过反射调用 <c>TryParse</c>。
    /// </summary>
    [Theory]
    [InlineData("42", true)]
    [InlineData("not-a-number", false)]
    public void CanConvert_StringToParsable_UsesTryParse(string raw, bool expected)
    {
        Assert.Equal(expected, ParameterValueConverter.CanConvert(typeof(int), raw));
    }

    /// <summary>
    /// <c>bool</c> 的 <see cref="IParsable{TSelf}"/> 方法是显式接口实现，判定必须与读取一致。
    /// </summary>
    /// <remarks>
    /// 按名称反射查找 <c>TryParse(string, IFormatProvider, out bool)</c> 会返回 null，
    /// 旧实现因此把 <c>"true"</c> 判为不可转换，而 <c>IParameters.TryGetValue&lt;bool&gt;</c>
    /// 能正常读出——导航校验会误报"类型不是 Boolean"。
    /// </remarks>
    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("FALSE", true)]
    [InlineData("not-a-bool", false)]
    [InlineData("", false)]
    public void CanConvert_StringToBool_MatchesReadPath(string raw, bool expected)
    {
        Assert.Equal(expected, ParameterValueConverter.CanConvert(typeof(bool), raw));
        Assert.Equal(expected, IParameters.Create($"?v={raw}").TryGetValue<bool>("v", out _));
    }

    /// <summary>
    /// <c>char</c> 的 <see cref="IParsable{TSelf}"/> 方法同样是显式接口实现。
    /// </summary>
    [Theory]
    [InlineData("a", true)]
    [InlineData("ab", false)]
    [InlineData("", false)]
    public void CanConvert_StringToChar_MatchesReadPath(string raw, bool expected)
    {
        Assert.Equal(expected, ParameterValueConverter.CanConvert(typeof(char), raw));
        Assert.Equal(expected, IParameters.Create($"?v={raw}").TryGetValue<char>("v", out _));
    }

    /// <summary>
    /// 可空 <c>bool</c> 与可空 <c>char</c> 走同一套规则。
    /// </summary>
    [Fact]
    public void CanConvert_StringToNullableBoolAndChar_MatchesReadPath()
    {
        Assert.True(ParameterValueConverter.CanConvert(typeof(bool?), "true"));
        Assert.False(ParameterValueConverter.CanConvert(typeof(bool?), "not-a-bool"));

        Assert.True(ParameterValueConverter.CanConvert(typeof(char?), "a"));
        Assert.False(ParameterValueConverter.CanConvert(typeof(char?), "ab"));
    }

    /// <summary>
    /// 可空值类型按底层类型解析。
    /// </summary>
    [Fact]
    public void CanConvert_StringToNullableParsable_UsesUnderlyingType()
    {
        Assert.True(ParameterValueConverter.CanConvert(typeof(int?), "42"));
        Assert.False(ParameterValueConverter.CanConvert(typeof(int?), "not-a-number"));
    }

    /// <summary>
    /// 不在 <see cref="TypeCode"/> 覆盖范围内的常用值类型同样支持字符串解析。
    /// </summary>
    /// <remarks>
    /// 这些类型没有对应的 <see cref="TypeCode"/>，转换实现里逐个显式判定。
    /// 去掉反射后不再有"自动发现 <c>IParsable</c>"的兜底，漏掉一个就会静默退化为不可转换，
    /// 因此每个类型都要有正向与负向用例。
    /// </remarks>
    [Theory]
    [InlineData(typeof(DateOnly), "2024-01-02", true)]
    [InlineData(typeof(DateOnly), "not-a-date", false)]
    [InlineData(typeof(TimeOnly), "01:02:03", true)]
    [InlineData(typeof(TimeOnly), "not-a-time", false)]
    [InlineData(typeof(DateTimeOffset), "2024-01-02T03:04:05+08:00", true)]
    [InlineData(typeof(DateTimeOffset), "2024-01-02T03:04:05Z", true)]
    [InlineData(typeof(DateTimeOffset), "not-a-date", false)]
    [InlineData(typeof(Half), "1.5", true)]
    [InlineData(typeof(Half), "not-a-number", false)]
    [InlineData(typeof(Int128), "170141183460469231731687303715884105727", true)]
    [InlineData(typeof(Int128), "not-a-number", false)]
    [InlineData(typeof(UInt128), "340282366920938463463374607431768211455", true)]
    [InlineData(typeof(UInt128), "-1", false)]
    [InlineData(typeof(BigInteger), "123456789012345678901234567890", true)]
    [InlineData(typeof(BigInteger), "not-a-number", false)]
    [InlineData(typeof(IntPtr), "42", true)]
    [InlineData(typeof(IntPtr), "not-a-number", false)]
    [InlineData(typeof(UIntPtr), "42", true)]
    [InlineData(typeof(UIntPtr), "-1", false)]
    public void CanConvert_StringToNonTypeCodeValueType_Parses(Type targetType, string raw, bool expected)
    {
        Assert.Equal(expected, ParameterValueConverter.CanConvert(targetType, raw));
    }

    /// <summary>
    /// 上述类型的可空写法同样支持。
    /// </summary>
    [Theory]
    [InlineData(typeof(DateOnly?), "2024-01-02", true)]
    [InlineData(typeof(DateOnly?), "not-a-date", false)]
    [InlineData(typeof(TimeOnly?), "01:02:03", true)]
    [InlineData(typeof(DateTimeOffset?), "2024-01-02T03:04:05Z", true)]
    [InlineData(typeof(DateTimeOffset?), "not-a-date", false)]
    [InlineData(typeof(Half?), "1.5", true)]
    [InlineData(typeof(Int128?), "42", true)]
    [InlineData(typeof(UInt128?), "42", true)]
    [InlineData(typeof(BigInteger?), "42", true)]
    [InlineData(typeof(IntPtr?), "42", true)]
    [InlineData(typeof(UIntPtr?), "42", true)]
    public void CanConvert_StringToNullableNonTypeCodeValueType_Parses(Type targetType, string raw, bool expected)
    {
        Assert.Equal(expected, ParameterValueConverter.CanConvert(targetType, raw));
    }

    /// <summary>
    /// 非 <see cref="TypeCode"/> 覆盖的值类型在读取路径上同样可用。
    /// </summary>
    /// <remarks>
    /// 读取走泛型路径、校验走非泛型路径，两条路径各有一份类型分派表。
    /// 本用例确保新增类型时两处都补上了分支。
    /// </remarks>
    [Fact]
    public void ReadPath_SupportsNonTypeCodeValueTypes()
    {
        var parameters = IParameters.Create("?d=2024-01-02&t=01:02:03&h=1.5&i=42&u=42&b=42&p=42&q=42&o=2024-01-02T03:04:05Z");

        Assert.Equal(new DateOnly(2024, 1, 2), parameters.GetValue<DateOnly>("d"));
        Assert.Equal(new TimeOnly(1, 2, 3), parameters.GetValue<TimeOnly>("t"));
        Assert.Equal((Half)1.5, parameters.GetValue<Half>("h"));
        Assert.Equal((Int128)42, parameters.GetValue<Int128>("i"));
        Assert.Equal((UInt128)42, parameters.GetValue<UInt128>("u"));
        Assert.Equal(new BigInteger(42), parameters.GetValue<BigInteger>("b"));
        Assert.Equal((IntPtr)42, parameters.GetValue<IntPtr>("p"));
        Assert.Equal((UIntPtr)42, parameters.GetValue<UIntPtr>("q"));
        Assert.Equal(
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            parameters.GetValue<DateTimeOffset>("o")
        );
    }

    /// <summary>
    /// 非 <see cref="TypeCode"/> 覆盖的值类型的可空写法在读取路径上同样可用。
    /// </summary>
    [Fact]
    public void ReadPath_SupportsNullableNonTypeCodeValueTypes()
    {
        var parameters = IParameters.Create("?d=2024-01-02&t=01:02:03&h=1.5&i=42&u=42&b=42&p=42&q=42&o=2024-01-02T03:04:05Z");

        Assert.Equal(new DateOnly(2024, 1, 2), parameters.GetValue<DateOnly?>("d"));
        Assert.Equal(new TimeOnly(1, 2, 3), parameters.GetValue<TimeOnly?>("t"));
        Assert.Equal((Half)1.5, parameters.GetValue<Half?>("h"));
        Assert.Equal((Int128)42, parameters.GetValue<Int128?>("i"));
        Assert.Equal((UInt128)42, parameters.GetValue<UInt128?>("u"));
        Assert.Equal(new BigInteger(42), parameters.GetValue<BigInteger?>("b"));
        Assert.Equal((IntPtr)42, parameters.GetValue<IntPtr?>("p"));
        Assert.Equal((UIntPtr)42, parameters.GetValue<UIntPtr?>("q"));
        Assert.Equal(
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            parameters.GetValue<DateTimeOffset?>("o")
        );
    }

    /// <summary>
    /// 枚举名称解析不产生堆分配。
    /// </summary>
    /// <remarks>
    /// 枚举解析改为在首次使用时建名称表，之后按名称查找不再分配。
    /// 若退回 <see cref="Enum.Parse(Type, string, bool)"/> 的装箱路径，本用例会失败。
    /// </remarks>
    [Fact]
    public void EnumNameLookup_DoesNotAllocate()
    {
        var parameters = IParameters.Create("?mode=Second");
        Assert.Equal(SampleMode.Second, parameters.GetValue<SampleMode>("mode"));

        // 预热：首次调用会建名称表，这部分分配必须落在测量窗口之外。
        for (var i = 0; i < 10; i++)
        {
            parameters.GetValue<SampleMode>("mode");
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            parameters.GetValue<SampleMode>("mode");
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>
    /// 可空枚举名称解析不产生堆分配。
    /// </summary>
    [Fact]
    public void NullableEnumNameLookup_DoesNotAllocate()
    {
        var parameters = IParameters.Create("?mode=Second");
        Assert.Equal(SampleMode.Second, parameters.GetValue<SampleMode?>("mode"));

        for (var i = 0; i < 10; i++)
        {
            parameters.GetValue<SampleMode?>("mode");
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            parameters.GetValue<SampleMode?>("mode");
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>
    /// 非字符串的 <see cref="IConvertible"/> 值可以转成枚举。
    /// </summary>
    [Fact]
    public void CanConvert_ConvertibleToEnum_UsesEnumToObject()
    {
        Assert.True(ParameterValueConverter.CanConvert(typeof(SampleMode), 2));
    }

    /// <summary>
    /// 无法作为枚举底层类型的值返回 false，而不是抛异常。
    /// </summary>
    [Fact]
    public void CanConvert_InvalidConvertibleToEnum_ReturnsFalse()
    {
        Assert.False(ParameterValueConverter.CanConvert(typeof(SampleMode), 1.5d));
    }

    /// <summary>
    /// 不实现 <see cref="IConvertible"/> 的类型无法转换。
    /// </summary>
    [Fact]
    public void CanConvert_NonConvertibleTarget_ReturnsFalse()
    {
        Assert.False(ParameterValueConverter.CanConvert(typeof(SamplePayload), 42));
    }

    /// <summary>
    /// 跨数值类型通过 <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/> 转换。
    /// </summary>
    [Fact]
    public void CanConvert_CrossNumericType_ReturnsTrue()
    {
        Assert.True(ParameterValueConverter.CanConvert(typeof(long), 42));
        Assert.True(ParameterValueConverter.CanConvert(typeof(double), 42));
    }

    /// <summary>
    /// 转换失败（溢出、格式或无效转换）时返回 false。
    /// </summary>
    [Theory]
    [InlineData(typeof(byte), 1000)]
    [InlineData(typeof(int), "not-a-number")]
    [InlineData(typeof(DateTime), "not-a-date")]
    public void CanConvert_FailedConversion_ReturnsFalse(Type targetType, object raw)
    {
        Assert.False(ParameterValueConverter.CanConvert(targetType, raw));
    }

    /// <summary>
    /// 目标类型的边界值可以转换，越界一位则失败。
    /// </summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(255, true)]
    [InlineData(256, false)]
    [InlineData(-1, false)]
    public void CanConvert_ByteBoundary_MatchesRange(int raw, bool expected)
    {
        Assert.Equal(expected, ParameterValueConverter.CanConvert(typeof(byte), raw));
    }

    /// <summary>
    /// 字符串形式的边界值同样按目标类型范围判定。
    /// </summary>
    [Theory]
    [InlineData("255", true)]
    [InlineData("256", false)]
    [InlineData("-1", false)]
    public void CanConvert_ByteBoundaryFromString_MatchesRange(string raw, bool expected)
    {
        Assert.Equal(expected, ParameterValueConverter.CanConvert(typeof(byte), raw));
    }

    /// <summary>
    /// 空字符串对字符串类型有效，对数值类型无效。
    /// </summary>
    [Fact]
    public void CanConvert_EmptyString_IsValidOnlyForString()
    {
        Assert.True(ParameterValueConverter.CanConvert(typeof(string), string.Empty));
        Assert.False(ParameterValueConverter.CanConvert(typeof(int), string.Empty));
        Assert.False(ParameterValueConverter.CanConvert(typeof(int?), string.Empty));
    }

    /// <summary>
    /// 空白字符串不是合法的数值或枚举。
    /// </summary>
    [Fact]
    public void CanConvert_WhitespaceString_IsNotNumeric()
    {
        Assert.False(ParameterValueConverter.CanConvert(typeof(int), " "));
        Assert.False(ParameterValueConverter.CanConvert(typeof(SampleMode), " "));
    }

    /// <summary>
    /// 枚举的边界：未定义但可解析的数值字符串按枚举名解析失败。
    /// </summary>
    /// <remarks>
    /// <see cref="Enum.TryParse(Type, string, bool, out object)"/> 接受数字字符串，
    /// 因此 "999" 会被判定为可转换，与 <c>IParameters</c> 的读取行为一致。
    /// </remarks>
    [Theory]
    [InlineData("999", true)]
    [InlineData("-1", true)]
    [InlineData("First", true)]
    public void CanConvert_EnumNumericString_MatchesTryParse(string raw, bool expected)
    {
        Assert.Equal(expected, ParameterValueConverter.CanConvert(typeof(SampleMode), raw));
    }

    /// <summary>
    /// 超长数字字符串不会因溢出抛异常，只返回 false。
    /// </summary>
    [Fact]
    public void CanConvert_HugeNumericString_ReturnsFalse()
    {
        var raw = new string('9', 1_000);

        Assert.False(ParameterValueConverter.CanConvert(typeof(int), raw));
    }

    /// <summary>
    /// 校验判定与读取结果在所有常见类型上必须一致。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 这是回归测试：两处规则曾经各写一套，<c>bool</c> 与 <c>char</c> 因此分叉。
    /// 现在校验直接复用读取路径的转换实现，本测试锁定这一约束。
    /// </para>
    /// <para>
    /// 用显式类型分派而不是 <c>MakeGenericMethod</c> 反射调用 <c>TryGetValue&lt;T&gt;</c>：
    /// 反射在裁剪与 AOT 下不可靠，且类型列表本来就是固定的，直接写出来更清楚。
    /// 新增类型时在 <c>Read</c> 里补一条分支即可。
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(typeof(bool))]
    [InlineData(typeof(bool?))]
    [InlineData(typeof(char))]
    [InlineData(typeof(char?))]
    [InlineData(typeof(int))]
    [InlineData(typeof(int?))]
    [InlineData(typeof(long))]
    [InlineData(typeof(double))]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(byte))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(TimeSpan))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(SampleMode))]
    [InlineData(typeof(SampleMode?))]
    [InlineData(typeof(string))]
    public void CanConvert_MatchesReadPath_ForCommonTypes(Type targetType)
    {
        string[] samples = ["true", "a", "42", "3.5", "2024-01-02", "1:02:03", "Second", "", "not-a-value"];

        foreach (var sample in samples)
        {
            var read = Read(targetType, sample);
            var canConvert = ParameterValueConverter.CanConvert(targetType, sample);

            Assert.True(
                read == canConvert,
                $"类型 {targetType.Name}、样本 \"{sample}\"：读取={read}，校验={canConvert}，两者必须一致。"
            );
        }
    }

    /// <summary>
    /// 按目标类型读取参数，与 <c>IParameters.TryGetValue&lt;T&gt;</c> 走同一条路径。
    /// </summary>
    /// <param name="targetType">目标类型。</param>
    /// <param name="sample">原始字符串值。</param>
    /// <returns>读取是否成功。</returns>
    /// <exception cref="ArgumentOutOfRangeException">目标类型不在支持列表里时抛出。</exception>
    private static bool Read(Type targetType, string sample)
    {
        var parameters = IParameters.Create($"?v={Uri.EscapeDataString(sample)}");

        return targetType switch
        {
            _ when targetType == typeof(bool) => parameters.TryGetValue<bool>("v", out _),
            _ when targetType == typeof(bool?) => parameters.TryGetValue<bool?>("v", out _),
            _ when targetType == typeof(char) => parameters.TryGetValue<char>("v", out _),
            _ when targetType == typeof(char?) => parameters.TryGetValue<char?>("v", out _),
            _ when targetType == typeof(int) => parameters.TryGetValue<int>("v", out _),
            _ when targetType == typeof(int?) => parameters.TryGetValue<int?>("v", out _),
            _ when targetType == typeof(long) => parameters.TryGetValue<long>("v", out _),
            _ when targetType == typeof(double) => parameters.TryGetValue<double>("v", out _),
            _ when targetType == typeof(decimal) => parameters.TryGetValue<decimal>("v", out _),
            _ when targetType == typeof(byte) => parameters.TryGetValue<byte>("v", out _),
            _ when targetType == typeof(DateTime) => parameters.TryGetValue<DateTime>("v", out _),
            _ when targetType == typeof(TimeSpan) => parameters.TryGetValue<TimeSpan>("v", out _),
            _ when targetType == typeof(Guid) => parameters.TryGetValue<Guid>("v", out _),
            _ when targetType == typeof(SampleMode) => parameters.TryGetValue<SampleMode>("v", out _),
            _ when targetType == typeof(SampleMode?) => parameters.TryGetValue<SampleMode?>("v", out _),
            _ when targetType == typeof(string) => parameters.TryGetValue<string>("v", out _),
            _ => throw new ArgumentOutOfRangeException(
                nameof(targetType),
                targetType,
                "新增类型时需要在 Read 里补一条分支。"
            ),
        };
    }

    /// <summary>
    /// 测试用枚举。
    /// </summary>
    public enum SampleMode
    {
        First = 1,
        Second = 2,
    }

    /// <summary>
    /// 不实现 <see cref="IConvertible"/> 的引用类型。
    /// </summary>
    public sealed record SamplePayload(string Value);
}
