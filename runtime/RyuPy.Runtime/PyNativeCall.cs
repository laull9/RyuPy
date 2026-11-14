// v3 ABI 调用帧：先检查全部参数，再固定自有缓冲快照；生成的 finally 负责确定性释放。
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace RyuPy.Runtime;

/// <summary>只供静态生成 wrapper 使用的调用期借用帧，不进入 Python 可见值域。</summary>
public sealed class PyNativeCall : IDisposable
{
    private readonly object[] values;
    private readonly GCHandle[] pins;
    private bool disposed;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private PyNativeCall(object[] converted)
    {
        values = converted;
        pins = new GCHandle[converted.Length];
        try
        {
            for (int index = 0; index < converted.Length; index++)
                if (converted[index] is byte[] bytes)
                    pins[index] = GCHandle.Alloc(bytes.Length == 0 ? new byte[1] : bytes, GCHandleType.Pinned);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>类型/名字来自已验证静态声明；所有检查结束前绝不固定缓冲或进入 C。</summary>
    public static PyNativeCall Prepare(object?[] arguments, string types, string names, string function)
    {
        string[] kinds = types.Length == 0 ? [] : types.Split(',');
        string[] parameters = names.Length == 0 ? [] : names.Split(',');
        if (arguments.Length != kinds.Length || arguments.Length != parameters.Length || arguments.Length > 16)
            throw new InvalidOperationException("invalid static ABI contract");
        object[] converted = new object[arguments.Length];
        for (int index = 0; index < arguments.Length; index++)
            converted[index] = ConvertValue(arguments[index], kinds[index], function, parameters[index]);
        return new PyNativeCall(converted);
    }

    private static object ConvertValue(object? value, string kind, string function, string parameter)
    {
        string label = $"{function}() argument '{parameter}'";
        value = PyNativeBindings.ReadExactScalar(value, kind switch
        {
            "utf8" => "exact_str", "bytes" => "exact_bytes", "bool8" => "exact_bool",
            "f32" or "f64" => "exact_float", _ => "exact_int",
        });
        if (kind is "utf8" or "bytes")
        {
            if (kind == "bytes" && value is PyBytes bytes) return (byte[])bytes.Bytes.Clone();
            if (kind == "utf8" && value is string text)
            {
                try { return StrictUtf8.GetBytes(text); }
                catch (EncoderFallbackException)
                { throw Error(new PyUnicodeEncodeError($"{label} contains an invalid Unicode surrogate")); }
            }
            throw Error(new PyTypeError($"{label} must be an actual {(kind == "utf8" ? "str" : "bytes")}"));
        }
        if (kind == "bool8")
            return value is bool boolean ? (byte)(boolean ? 1 : 0)
                : throw Error(new PyTypeError($"{label} must be an actual bool"));
        if (kind is "f32" or "f64")
        {
            if (value is not double floating)
                throw Error(new PyTypeError($"{label} must be an actual float"));
            if (kind == "f32" && double.IsFinite(floating) && float.IsInfinity((float)floating))
                throw Error(new PyOverflowError($"{label} is outside finite float32 range"));
            return floating;
        }
        BigInteger integer = value switch
        {
            int small => small,
            long number => number,
            BigInteger big => big,
            _ => throw Error(new PyTypeError($"{label} must be an actual integer, not bool or a converted value"))
        };
        (int width, bool signed) = kind switch
        {
            "i8" => (8, true), "u8" => (8, false),
            "i16" => (16, true), "u16" => (16, false),
            "i32" => (32, true), "u32" => (32, false),
            "i64" => (64, true), "u64" => (64, false),
            "isize" => (IntPtr.Size * 8, true), "usize" => (IntPtr.Size * 8, false),
            _ => throw new InvalidOperationException("unsupported static ABI type")
        };
        BigInteger limit = BigInteger.One << (signed ? width - 1 : width);
        if (integer < (signed ? -limit : BigInteger.Zero) || integer >= limit)
            throw Error(new PyOverflowError($"{label} is outside {kind} range"));
        // 分支分别返回，防止条件表达式的数值提升将 signed 装箱成 BigInteger。
        if (signed) return (long)integer;
        return (ulong)integer;
    }

    /// <summary>已检查的有符号标量。</summary>
    public long Signed(int index) => (long)values[index];
    /// <summary>已检查的无符号标量。</summary>
    public ulong Unsigned(int index) => (ulong)values[index];
    /// <summary>已检查的浮点数，float32 舍入在静态 wrapper 中执行。</summary>
    public double Floating(int index) => (double)values[index];
    /// <summary>显式 uint8_t 布尔值。</summary>
    public byte Boolean(int index) => (byte)values[index];
    /// <summary>只供 PInvoke 消费的地址；释放后访问立即失败。</summary>
    public nint Pointer(int index)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return pins[index].AddrOfPinnedObject();
    }
    /// <summary>编码后的字节长度，无隐含终止符，也不接受用户提供的独立长度。</summary>
    public nuint Length(int index) => (nuint)((byte[])values[index]).Length;

    /// <summary>释放固定句柄；幂等，支持原生装载失败和返回转换异常的 finally 路径。</summary>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        for (int index = 0; index < pins.Length; index++)
            if (pins[index].IsAllocated) pins[index].Free();
    }

    private static PyBaseException Error(PyBaseException error) =>
        (PyBaseException)PyErrors.PrepareRaise(error);
}
