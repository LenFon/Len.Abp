using System.Globalization;
using System.Numerics;

namespace Len.Abp.Wpf;

/// <summary>
/// 参数值的类型转换，供 <see cref="IParameters"/> 读取与导航参数校验共用。
/// </summary>
/// <remarks>
/// <para>
/// 两处必须使用同一套规则，否则会出现"导航校验通过但读取失败"的矛盾。
/// 校验侧走非泛型重载，读取侧走泛型重载，两者调用同一批 <c>TryParse</c> 方法，
/// 由 <c>ParameterValueConverterTests</c> 的一致性用例锁定。
/// </para>
/// <para>
/// <b>不使用反射。</b>泛型路径按 <see cref="Type"/> 显式分派到强类型委托，
/// 而不是用 <c>MakeGenericMethod</c> + <c>CreateDelegate</c> 构造
/// <c>IParsable&lt;T&gt;</c> 的调用委托：反射构造在裁剪（trimming）与 Native AOT 下不可靠，
/// 且每种类型都要额外缓存一份反射元数据。
/// </para>
/// <para>
/// 代价是新增类型时必须在三处各补一条分支：<see cref="TryParseString"/>（非泛型校验路径）、
/// <see cref="StringParser{T}.CreateValue"/>（非可空泛型读取路径）与
/// <see cref="StringParser{T}.CreateNullable"/>（可空泛型读取路径）。
/// 三处都调用同一个 <c>TryParse</c>，语义一致；漏补只会让该类型退化为
/// "不可从字符串解析"，不会静默给出错误结果。
/// </para>
/// <para>
/// 值以 <see cref="object"/> 存储，因此写入值类型时必然发生一次装箱，这是异构参数包的固有代价。
/// 读取路径通过强类型委托避免额外装箱：类型匹配时直接拆箱，字符串解析不产生任何分配。
/// </para>
/// </remarks>
internal static class ParameterConversion
{
    /// <summary>
    /// 把存储的原始值转换为 <typeparamref name="T"/>，尽可能避免装箱。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="raw">原始值。</param>
    /// <param name="value">转换结果；无法转换时为 <see langword="default"/>。</param>
    /// <returns>可转换时返回 <see langword="true"/>。</returns>
    public static bool TryConvert<T>(object? raw, out T? value)
    {
        if (raw is null)
        {
            value = default;

            // null 只能赋给引用类型或可空值类型。
            return !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) is not null;
        }

        // 快路径：类型已匹配。存储本身是 object，这里只发生一次拆箱。
        if (raw is T typed)
        {
            value = typed;
            return true;
        }

        // 字符串解析：走按类型缓存的强类型委托，不产生装箱。
        if (raw is string text && StringParser<T>.Instance is { } parse)
        {
            if (parse(text, out var parsed))
            {
                value = parsed;
                return true;
            }

            value = default;
            return false;
        }

        return TryConvertFallback(raw, out value);
    }

    /// <summary>
    /// 把存储的原始值转换为运行时指定的目标类型。
    /// </summary>
    /// <param name="raw">原始值。</param>
    /// <param name="targetType">目标类型，可为可空值类型。</param>
    /// <param name="value">转换结果；无法转换时为 <see langword="null"/>。</param>
    /// <returns>可转换时返回 <see langword="true"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="targetType"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <remarks>
    /// 非泛型入口供 <see cref="ParameterValueConverter"/> 使用：目标类型在运行时才确定，
    /// 无法走泛型路径。这里允许装箱——它只用于导航参数校验，不在读取热路径上。
    /// </remarks>
    public static bool TryConvert(object? raw, Type targetType, out object? value)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        var underlyingType = Nullable.GetUnderlyingType(targetType);
        var type = underlyingType ?? targetType;

        if (raw is null)
        {
            value = null;

            // null 只能赋给引用类型或可空值类型。
            return underlyingType is not null || !targetType.IsValueType;
        }

        if (type.IsInstanceOfType(raw))
        {
            value = raw;
            return true;
        }

        if (raw is string text && TryParseString(text, type, out value))
        {
            return true;
        }

        return TryConvertFallback(raw, type, out value);
    }

    /// <summary>
    /// 把字符串解析为目标类型。
    /// </summary>
    /// <param name="text">待解析的文本。</param>
    /// <param name="type">目标类型，已去掉可空包装。</param>
    /// <param name="value">解析结果；失败时为 <see langword="null"/>。</param>
    /// <returns>解析成功时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 与 <see cref="StringParser{T}"/> 调用同一批 <c>TryParse</c> 方法，因此两条路径的
    /// 判定结果一致。全部使用 <see cref="CultureInfo.InvariantCulture"/>：
    /// 查询串与导航参数是机器生成的文本，不能随宿主线程的区域设置变化。
    /// </remarks>
    private static bool TryParseString(string text, Type type, out object? value)
    {
        // 枚举必须先判：它的 TypeCode 是底层整型，走数值分支会把 "999" 解析成 int 而不是枚举。
        if (type.IsEnum)
        {
            if (Enum.TryParse(type, text, ignoreCase: true, out var enumValue))
            {
                value = enumValue;
                return true;
            }

            value = null;
            return false;
        }

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean when bool.TryParse(text, out var boolean):
                value = boolean;
                return true;

            case TypeCode.Char when char.TryParse(text, out var character):
                value = character;
                return true;

            case TypeCode.SByte when sbyte.TryParse(text, CultureInfo.InvariantCulture, out var sbyteValue):
                value = sbyteValue;
                return true;

            case TypeCode.Byte when byte.TryParse(text, CultureInfo.InvariantCulture, out var byteValue):
                value = byteValue;
                return true;

            case TypeCode.Int16 when short.TryParse(text, CultureInfo.InvariantCulture, out var int16Value):
                value = int16Value;
                return true;

            case TypeCode.UInt16 when ushort.TryParse(text, CultureInfo.InvariantCulture, out var uint16Value):
                value = uint16Value;
                return true;

            case TypeCode.Int32 when int.TryParse(text, CultureInfo.InvariantCulture, out var int32Value):
                value = int32Value;
                return true;

            case TypeCode.UInt32 when uint.TryParse(text, CultureInfo.InvariantCulture, out var uint32Value):
                value = uint32Value;
                return true;

            case TypeCode.Int64 when long.TryParse(text, CultureInfo.InvariantCulture, out var int64Value):
                value = int64Value;
                return true;

            case TypeCode.UInt64 when ulong.TryParse(text, CultureInfo.InvariantCulture, out var uint64Value):
                value = uint64Value;
                return true;

            case TypeCode.Single when float.TryParse(text, CultureInfo.InvariantCulture, out var singleValue):
                value = singleValue;
                return true;

            case TypeCode.Double when double.TryParse(text, CultureInfo.InvariantCulture, out var doubleValue):
                value = doubleValue;
                return true;

            case TypeCode.Decimal when decimal.TryParse(text, CultureInfo.InvariantCulture, out var decimalValue):
                value = decimalValue;
                return true;

            case TypeCode.DateTime when DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue):
                value = dateTimeValue;
                return true;
        }

        // DateTimeOffset 必须单独列出：它的 TypeCode 是 Object，不会命中上面的 switch。
        if (type == typeof(DateTimeOffset) && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffset))
        {
            value = dateTimeOffset;
            return true;
        }

        // TypeCode 只覆盖内建基元类型，其余常用值类型逐个判定。
        if (type == typeof(Guid) && Guid.TryParse(text, out var guid))
        {
            value = guid;
            return true;
        }

        if (type == typeof(TimeSpan) && TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var timeSpan))
        {
            value = timeSpan;
            return true;
        }

        if (type == typeof(DateOnly) && DateOnly.TryParse(text, CultureInfo.InvariantCulture, out var dateOnly))
        {
            value = dateOnly;
            return true;
        }

        if (type == typeof(TimeOnly) && TimeOnly.TryParse(text, CultureInfo.InvariantCulture, out var timeOnly))
        {
            value = timeOnly;
            return true;
        }

        if (type == typeof(Half) && Half.TryParse(text, CultureInfo.InvariantCulture, out var half))
        {
            value = half;
            return true;
        }

        if (type == typeof(Int128) && Int128.TryParse(text, CultureInfo.InvariantCulture, out var int128))
        {
            value = int128;
            return true;
        }

        if (type == typeof(UInt128) && UInt128.TryParse(text, CultureInfo.InvariantCulture, out var uint128))
        {
            value = uint128;
            return true;
        }

        if (type == typeof(BigInteger) && BigInteger.TryParse(text, CultureInfo.InvariantCulture, out var bigInteger))
        {
            value = bigInteger;
            return true;
        }

        if (type == typeof(IntPtr) && IntPtr.TryParse(text, CultureInfo.InvariantCulture, out var intPtr))
        {
            value = intPtr;
            return true;
        }

        if (type == typeof(UIntPtr) && UIntPtr.TryParse(text, CultureInfo.InvariantCulture, out var uintPtr))
        {
            value = uintPtr;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// 回退路径：处理跨数值类型、数值转枚举等非字符串场景。
    /// </summary>
    /// <param name="raw">原始值，非 <see langword="null"/>。</param>
    /// <param name="type">目标类型，已去掉可空包装。</param>
    /// <param name="value">转换结果；失败时为 <see langword="null"/>。</param>
    /// <returns>可转换时返回 <see langword="true"/>。</returns>
    private static bool TryConvertFallback(object raw, Type type, out object? value)
    {
        if (type.IsEnum && raw is IConvertible)
        {
            try
            {
                value = Enum.ToObject(type, raw);
                return true;
            }
            catch (ArgumentException)
            {
                value = null;
                return false;
            }
        }

        if (!typeof(IConvertible).IsAssignableFrom(type))
        {
            value = null;
            return false;
        }

        try
        {
            value = Convert.ChangeType(raw, type, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// 泛型回退路径：结果直接拆箱写入 <typeparamref name="T"/>，不额外装箱。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="raw">原始值，非 <see langword="null"/>。</param>
    /// <param name="value">转换结果；失败时为 <see langword="default"/>。</param>
    /// <returns>可转换时返回 <see langword="true"/>。</returns>
    private static bool TryConvertFallback<T>(object raw, out T? value)
    {
        var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (type.IsEnum && raw is IConvertible)
        {
            try
            {
                value = (T?)Enum.ToObject(type, raw);
                return true;
            }
            catch (ArgumentException)
            {
                value = default;
                return false;
            }
        }

        if (!typeof(IConvertible).IsAssignableFrom(type))
        {
            value = default;
            return false;
        }

        try
        {
            value = (T?)Convert.ChangeType(raw, type, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// 把字符串解析为 <typeparamref name="T"/> 的强类型委托。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="text">待解析的文本。</param>
    /// <param name="value">解析结果。</param>
    /// <returns>解析成功时返回 <see langword="true"/>。</returns>
    private delegate bool TryParseDelegate<T>(string text, out T value);

    /// <summary>
    /// 按类型缓存字符串解析委托。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <remarks>
    /// 委托在首次使用时构造一次，之后每次调用都是直接调用，不产生装箱。
    /// 构造走显式的 <see cref="Type"/> 分派而不是反射：反射在裁剪与 AOT 下不可靠。
    /// </remarks>
    private static class StringParser<T>
    {
        /// <summary>
        /// 该类型的解析委托；类型不支持从字符串解析时为 <see langword="null"/>。
        /// </summary>
        public static readonly TryParseDelegate<T>? Instance = Create();

        /// <summary>
        /// 构造解析委托。
        /// </summary>
        /// <returns>解析委托；不支持时返回 <see langword="null"/>。</returns>
        private static TryParseDelegate<T>? Create()
        {
            var type = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(type);

            // 可空值类型：复用底层类型的解析逻辑，直接产出 T（即 TValue?），无需中间装箱。
            if (underlyingType is not null)
            {
                return CreateNullable(underlyingType);
            }

            return type.IsEnum ? CreateEnum() : CreateValue(type);
        }

        /// <summary>
        /// 为可空值类型构造解析委托。
        /// </summary>
        /// <param name="underlyingType">底层值类型。</param>
        /// <returns>解析委托；不支持时返回 <see langword="null"/>。</returns>
        private static TryParseDelegate<T>? CreateNullable(Type underlyingType)
        {
            if (underlyingType == typeof(bool)) return Cast<bool?>(ParseNullable<bool>);
            if (underlyingType == typeof(char)) return Cast<char?>(ParseNullable<char>);
            if (underlyingType == typeof(sbyte)) return Cast<sbyte?>(ParseNullable<sbyte>);
            if (underlyingType == typeof(byte)) return Cast<byte?>(ParseNullable<byte>);
            if (underlyingType == typeof(short)) return Cast<short?>(ParseNullable<short>);
            if (underlyingType == typeof(ushort)) return Cast<ushort?>(ParseNullable<ushort>);
            if (underlyingType == typeof(int)) return Cast<int?>(ParseNullable<int>);
            if (underlyingType == typeof(uint)) return Cast<uint?>(ParseNullable<uint>);
            if (underlyingType == typeof(long)) return Cast<long?>(ParseNullable<long>);
            if (underlyingType == typeof(ulong)) return Cast<ulong?>(ParseNullable<ulong>);
            if (underlyingType == typeof(float)) return Cast<float?>(ParseNullable<float>);
            if (underlyingType == typeof(double)) return Cast<double?>(ParseNullable<double>);
            if (underlyingType == typeof(decimal)) return Cast<decimal?>(ParseNullable<decimal>);
            if (underlyingType == typeof(DateTime)) return Cast<DateTime?>(ParseNullable<DateTime>);
            if (underlyingType == typeof(DateTimeOffset)) return Cast<DateTimeOffset?>(ParseNullable<DateTimeOffset>);
            if (underlyingType == typeof(Guid)) return Cast<Guid?>(ParseNullable<Guid>);
            if (underlyingType == typeof(TimeSpan)) return Cast<TimeSpan?>(ParseNullable<TimeSpan>);
            if (underlyingType == typeof(DateOnly)) return Cast<DateOnly?>(ParseNullable<DateOnly>);
            if (underlyingType == typeof(TimeOnly)) return Cast<TimeOnly?>(ParseNullable<TimeOnly>);
            if (underlyingType == typeof(Half)) return Cast<Half?>(ParseNullable<Half>);
            if (underlyingType == typeof(Int128)) return Cast<Int128?>(ParseNullable<Int128>);
            if (underlyingType == typeof(UInt128)) return Cast<UInt128?>(ParseNullable<UInt128>);
            if (underlyingType == typeof(BigInteger)) return Cast<BigInteger?>(ParseNullable<BigInteger>);
            if (underlyingType == typeof(IntPtr)) return Cast<IntPtr?>(ParseNullable<IntPtr>);
            if (underlyingType == typeof(UIntPtr)) return Cast<UIntPtr?>(ParseNullable<UIntPtr>);

            // 可空枚举：底层类型是枚举，同样走枚举解析。
            return underlyingType.IsEnum ? CastNullableEnum(underlyingType) : null;
        }

        /// <summary>
        /// 为非可空值类型构造解析委托。
        /// </summary>
        /// <param name="type">目标类型。</param>
        /// <returns>解析委托；不支持时返回 <see langword="null"/>。</returns>
        private static TryParseDelegate<T>? CreateValue(Type type)
        {
            if (type == typeof(bool)) return Cast<bool>(ParseValue<bool>);
            if (type == typeof(char)) return Cast<char>(ParseValue<char>);
            if (type == typeof(sbyte)) return Cast<sbyte>(ParseValue<sbyte>);
            if (type == typeof(byte)) return Cast<byte>(ParseValue<byte>);
            if (type == typeof(short)) return Cast<short>(ParseValue<short>);
            if (type == typeof(ushort)) return Cast<ushort>(ParseValue<ushort>);
            if (type == typeof(int)) return Cast<int>(ParseValue<int>);
            if (type == typeof(uint)) return Cast<uint>(ParseValue<uint>);
            if (type == typeof(long)) return Cast<long>(ParseValue<long>);
            if (type == typeof(ulong)) return Cast<ulong>(ParseValue<ulong>);
            if (type == typeof(float)) return Cast<float>(ParseValue<float>);
            if (type == typeof(double)) return Cast<double>(ParseValue<double>);
            if (type == typeof(decimal)) return Cast<decimal>(ParseValue<decimal>);
            if (type == typeof(DateTime)) return Cast<DateTime>(ParseValue<DateTime>);
            if (type == typeof(DateTimeOffset)) return Cast<DateTimeOffset>(ParseValue<DateTimeOffset>);
            if (type == typeof(Guid)) return Cast<Guid>(ParseValue<Guid>);
            if (type == typeof(TimeSpan)) return Cast<TimeSpan>(ParseValue<TimeSpan>);
            if (type == typeof(DateOnly)) return Cast<DateOnly>(ParseValue<DateOnly>);
            if (type == typeof(TimeOnly)) return Cast<TimeOnly>(ParseValue<TimeOnly>);
            if (type == typeof(Half)) return Cast<Half>(ParseValue<Half>);
            if (type == typeof(Int128)) return Cast<Int128>(ParseValue<Int128>);
            if (type == typeof(UInt128)) return Cast<UInt128>(ParseValue<UInt128>);
            if (type == typeof(BigInteger)) return Cast<BigInteger>(ParseValue<BigInteger>);
            if (type == typeof(IntPtr)) return Cast<IntPtr>(ParseValue<IntPtr>);
            if (type == typeof(UIntPtr)) return Cast<UIntPtr>(ParseValue<UIntPtr>);

            return null;
        }

        /// <summary>
        /// 为枚举构造解析委托。
        /// </summary>
        /// <returns>解析委托。</returns>
        /// <remarks>
        /// <para>
        /// 名称到值的映射在首次使用时建表并缓存，之后按名称解析不产生任何分配。
        /// 数字字符串（如 <c>"999"</c>）与未定义值由 <see cref="Enum.TryParse(Type, string, bool, out object)"/>
        /// 处理，与 <see cref="TryParseString"/> 的行为保持一致。
        /// </para>
        /// <para>
        /// <b>这里只能用接受 <see cref="Type"/> 的非泛型重载。</b>
        /// <see cref="Enum.GetValues{TEnum}"/> 等泛型版本要求 <c>where TEnum : struct, Enum</c>，
        /// 而本类的 <c>T</c> 是无约束的（它可能是引用类型、可空类型或任意值类型），
        /// 无法满足该约束。要绕开只能靠反射构造泛型方法，那正是这里要避免的做法。
        /// </para>
        /// <para>
        /// 建表只在首次解析该类型时发生一次，之后按名称查找是纯字典命中，不涉及装箱；
        /// 只有数字字符串与未定义值才会走 <see cref="Enum.TryParse(Type, string, bool, out object)"/>
        /// 的装箱路径，属于少见场景。
        /// </para>
        /// </remarks>
        private static TryParseDelegate<T> CreateEnum()
        {
            var names = Enum.GetNames(typeof(T));
            var values = Enum.GetValues(typeof(T));
            var lookup = new Dictionary<string, T>(names.Length, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < names.Length; i++)
            {
                lookup[names[i]] = (T)values.GetValue(i)!;
            }

            return (string text, out T value) =>
            {
                if (lookup.TryGetValue(text, out var named))
                {
                    value = named;
                    return true;
                }

                if (Enum.TryParse(typeof(T), text, ignoreCase: true, out var parsed))
                {
                    value = (T)parsed!;
                    return true;
                }

                value = default!;
                return false;
            };
        }

        /// <summary>
        /// 为可空枚举构造解析委托。
        /// </summary>
        /// <param name="underlyingType">枚举类型。</param>
        /// <returns>解析委托。</returns>
        /// <remarks>
        /// 与 <see cref="CreateEnum"/> 同理，只能用非泛型重载。
        /// </remarks>
        private static TryParseDelegate<T> CastNullableEnum(Type underlyingType)
        {
            var names = Enum.GetNames(underlyingType);
            var values = Enum.GetValues(underlyingType);
            var lookup = new Dictionary<string, object>(names.Length, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < names.Length; i++)
            {
                lookup[names[i]] = values.GetValue(i)!;
            }

            return (string text, out T value) =>
            {
                if (lookup.TryGetValue(text, out var named))
                {
                    value = (T)named;
                    return true;
                }

                if (Enum.TryParse(underlyingType, text, ignoreCase: true, out var parsed))
                {
                    value = (T)parsed;
                    return true;
                }

                value = default!;
                return false;
            };
        }

        /// <summary>
        /// 把强类型委托重解释为 <typeparamref name="T"/> 的委托。
        /// </summary>
        /// <typeparam name="TValue">委托实际处理的类型，必须与 <typeparamref name="T"/> 相同。</typeparam>
        /// <param name="parser">强类型解析委托。</param>
        /// <returns>同一委托，以 <typeparamref name="T"/> 的类型呈现。</returns>
        /// <remarks>
        /// 委托是引用类型，这里只是类型转换，不装箱也不复制。调用点都按
        /// <see cref="Type"/> 精确匹配，因此运行时类型必然一致。
        /// </remarks>
        private static TryParseDelegate<T> Cast<TValue>(TryParseDelegate<TValue> parser) =>
            (TryParseDelegate<T>)(object)parser;

        /// <summary>
        /// 用 <see cref="IParsable{TSelf}"/> 解析非可空值类型。
        /// </summary>
        /// <typeparam name="TValue">目标值类型。</typeparam>
        /// <param name="text">待解析的文本。</param>
        /// <param name="value">解析结果。</param>
        /// <returns>解析成功时返回 <see langword="true"/>。</returns>
        /// <remarks>
        /// 必须调用接口方法而不是反射查找 <c>TryParse(string, IFormatProvider, out T)</c>：
        /// <c>bool</c> 与 <c>char</c> 把该接口方法实现为显式接口实现，按名称反射找不到。
        /// 泛型约束调用走接口分派，对显式实现同样有效。
        /// </remarks>
        private static bool ParseValue<TValue>(string text, out TValue value)
            where TValue : IParsable<TValue> =>
            TValue.TryParse(text, CultureInfo.InvariantCulture, out value!);

        /// <summary>
        /// 用 <see cref="IParsable{TSelf}"/> 解析可空值类型。
        /// </summary>
        /// <typeparam name="TValue">底层值类型。</typeparam>
        /// <param name="text">待解析的文本。</param>
        /// <param name="value">解析结果。</param>
        /// <returns>解析成功时返回 <see langword="true"/>。</returns>
        private static bool ParseNullable<TValue>(string text, out TValue? value)
            where TValue : struct, IParsable<TValue>
        {
            if (TValue.TryParse(text, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }

            value = null;
            return false;
        }
    }
}
