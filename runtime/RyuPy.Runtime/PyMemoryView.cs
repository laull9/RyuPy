using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RyuPy.Runtime;

/// <summary>提供真实原始buffer复制入口；不能用迭代协议代替，例如 c 视图迭代返回bytes而非整数。</summary>
public interface IPyBufferView
{
    /// <summary>复制当前有效视图的原始字节；已释放视图抛ValueError。</summary>
    byte[] CopyBuffer();
}

/// <summary>
/// 一维连续 B/c memoryview，共享真实exporter与偏移，不复制存储来冒充别名。
/// 每个切片/cast独立租约；显式release确定，自动回收沿D001的GC时机边界。
/// </summary>
public sealed class PyMemoryView : PyNativeObject, IPyBufferView
{
    private static readonly Lazy<PyNativeType> NativeType = new(() => new PyNativeType(
        "memoryview", "builtins", Create, value => value is PyMemoryView));
    private static readonly object MissingShape = new();
    private object? _owner;
    private readonly int _offset;
    private readonly int _length;
    private readonly string _format;
    private int _released;
    private long? _hash;

    private PyMemoryView(object owner, int offset, int length, string format)
    {
        _owner = owner; _offset = offset; _length = length; _format = format;
        if (owner is PyByteArray array) array.AcquireExport();
    }
    /// <summary>稳定内建类型，复用一等调用/type/isinstance。</summary>
    public static PyNativeType Type => NativeType.Value;
    /// <summary>单个object参数构造；只接已实现的bytes/bytearray/同类连续视图。</summary>
    public static object Create(object?[] args, string[] names, object?[] values)
    {
        _ = Type;
        object? source;
        if (names.Length == 0 && args.Length == 1) source = args[0];
        else if (args.Length == 0 && names.Length == 1 && names[0] == "object") source = values[0];
        else throw new PyTypeError("memoryview() requires exactly one object argument");
        return source switch
        {
            PyBytes bytes => new PyMemoryView(bytes, 0, bytes.Count, "B"),
            PyByteArray array => new PyMemoryView(array, 0, array.Count, "B"),
            PyMemoryView view => view.Clone(view._offset, view._length, view._format),
            _ => throw new PyTypeError($"memoryview: a bytes-like object is required, not '{PyRepr.TypeName(source)}'"),
        };
    }
    /// <inheritdoc />
    public override string TypeName => "memoryview";
    /// <inheritdoc />
    public override object? GetAttr(string name) => name switch
    {
        "__class__" => Type,
        "obj" => Owner(), "format" => Checked(_format), "itemsize" => Checked(1L),
        "ndim" => Checked(1L), "nbytes" => Checked((long)_length),
        "readonly" => Checked(_owner is PyBytes),
        "shape" => Checked(new PyTuple([(long)_length])), "strides" => Checked(new PyTuple([1L])),
        "suboffsets" => Checked(new PyTuple([])),
        "c_contiguous" or "f_contiguous" or "contiguous" => Checked(true),
        "__len__" => Method(name, [], [], 0, _ => Checked((long)_length)),
        "__getitem__" => Method(name, ["key"], [null], 1, a => GetItem(a[0])),
        "__setitem__" => Method(name, ["key", "value"], [null, null], 2, a => SetItem(a[0], a[1])),
        "__iter__" => Method(name, [], [], 0, _ => { Owner(); return new ViewIterator(this); }),
        "cast" => PyNativeFunctions.Create(name, ["format", "shape"], [null, MissingShape], 1, a => Cast(a[0], a[1])),
        "release" => Method(name, [], [], 0, _ => { Release(); return PyNone.Instance; }),
        "tobytes" => PyNativeFunctions.Create(name, ["order"], ["C"], 0, a => ToBytes(a[0])),
        "tolist" => Method(name, [], [], 0, _ => ToList()),
        "__enter__" => Method(name, [], [], 0, _ => { Owner(); return this; }),
        "__exit__" => Method(name, ["type", "value", "traceback"], [null, null, null], 3,
            _ => { Release(); return PyNone.Instance; }),
        "__hash__" => Method(name, [], [], 0, _ => Hash()),
        "__eq__" => Method(name, ["other"], [null], 1, a => EqualBuffer(a[0])),
        "__ne__" => Method(name, ["other"], [null], 1, a => NegateEquality(a[0])),
        _ => throw Missing(name),
    };
    private static PyFunction Method(string name, string[] names, object?[] defaults, int required, Func<object?[], object?> body) =>
        PyNativeFunctions.Create(name, names, defaults, required, body, Array.ConvertAll(names, _ => "posonly"));
    private object Owner() => Volatile.Read(ref _released) == 0 && _owner is object owner ? owner
        : throw new PyValueError("operation forbidden on released memoryview object");
    private object Checked(object value) { Owner(); return value; }
    private PyMemoryView Clone(int offset, int length, string format) => new(Owner(), offset, length, format);
    private PyMemoryView Cast(object? format, object? shape)
    {
        // format类型由CPython参数解析先检查；shape则在released检查之后检查。
        if (format is not string text) throw new PyTypeError("cast() argument 'format' must be str");
        Owner();
        if (!ReferenceEquals(shape, MissingShape))
        {
            if (shape is not PyList and not PyTuple) throw new PyTypeError("shape must be a list or a tuple");
            throw new PyNotImplementedError("memoryview cast shape is not supported");
        }
        if (text is not "B" and not "c") throw new PyNotImplementedError("memoryview supports only contiguous one-dimensional B/c formats");
        return Clone(_offset, _length, text);
    }
    private int Index(object? value, bool checkAfterConversion = false)
    {
        string sourceType = PyRepr.TypeName(value);
        if (!PyInt.IsInt(value))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out value))
                throw new PyTypeError("memoryview: invalid slice key");
            if (value is bool)
                throw new PyNotImplementedError("bool returned by __index__ in memoryview indexing is not supported");
            if (!PyInt.IsInt(value)) throw new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(value)})");
        }
        long index;
        if (!PyInt.TryI64(value!, out index))
        {
            var integer = PyInt.ToBigInteger(value!);
            if (integer < long.MinValue || integer > long.MaxValue)
                throw new PyIndexError($"cannot fit '{sourceType}' into an index-sized integer");
            index = (long)integer;
        }
        // get在可表示索引转换后先检查释放，set则先报越界，再转换右值，写前再检查。
        if (checkAfterConversion) Owner();
        if (index < 0) index += _length;
        if (index < 0 || index >= _length) throw new PyIndexError("index out of bounds on dimension 1");
        return (int)index;
    }
    private byte Byte(int index)
    {
        object owner = Owner();
        return owner is PyBytes bytes ? bytes.Bytes[_offset + index] : ((PyByteArray)owner).Bytes[_offset + index];
    }
    private object Value(int index) => _format == "B" ? PyInt.Box(Byte(index)) : new PyBytes([Byte(index)]);
    /// <summary>整型索引返回B整数或c单字节bytes；切片保持共享exporter与独立租约。</summary>
    public object GetItem(object? key)
    {
        object owner = Owner();
        if (key is PySlice slice)
        {
            // 切片转换期间独立持有buffer；用户回调可释放原视图，但不可resize exporter。
            var array = owner as PyByteArray;
            array?.AcquireExport();
            try
            {
                long step = SliceIndex(slice.Step, 1);
                if (step == 0) throw new PyValueError("slice step cannot be zero");
                object? startValue = slice.Start is null or PyNone ? PyNone.Instance : SliceIndex(slice.Start, 0);
                object? stopValue = slice.Stop is null or PyNone ? PyNone.Instance : SliceIndex(slice.Stop, 0);
                if (step != 1) throw new PyNotImplementedError("non-contiguous memoryview slices are not supported");
                var (start, _, _, count) = new PySlice(startValue, stopValue, step).Indices(_length);
                return new PyMemoryView(owner, _offset + (int)start, (int)count, _format);
            }
            finally { array?.ReleaseExport(); }
        }
        return Value(Index(key, checkAfterConversion: true));
    }
    /// <summary>同长单元素写入共享bytearray；不实现切片赋值或resize。</summary>
    public object SetItem(object? key, object? value)
    {
        object owner = Owner();
        if (owner is not PyByteArray array) throw new PyTypeError("cannot modify read-only memory");
        if (key is PySlice) throw new PyNotImplementedError("memoryview slice assignment is not supported");
        int index = Index(key);
        byte item;
        if (_format == "c")
        {
            if (value is not PyBytes bytes || bytes.Count != 1) throw new PyTypeError("memoryview: invalid type for format 'c'");
            item = bytes.Bytes[0];
        }
        else item = ConvertElement(value);
        // 两次整数协议均可能执行用户代码并release/resize；实际写前必须重新验证活性。
        array = (PyByteArray)Owner();
        array.Bytes[_offset + index] = item;
        return PyNone.Instance;
    }
    private static long SliceIndex(object? value, long fallback)
    {
        if (value is null or PyNone) return fallback;
        if (!PyInt.IsInt(value))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out value))
                throw new PyTypeError("slice indices must be integers or None or have an __index__ method");
            if (value is bool) throw new PyNotImplementedError("bool returned by __index__ in memoryview slicing is not supported");
            if (!PyInt.IsInt(value)) throw new PyTypeError($"__index__ returned non-int (type {PyRepr.TypeName(value)})");
        }
        if (PyInt.TryI64(value!, out long small)) return small;
        var integer = PyInt.ToBigInteger(value!);
        return integer < long.MinValue ? long.MinValue : integer > long.MaxValue ? long.MaxValue : (long)integer;
    }
    private static byte ConvertElement(object? value)
    {
        long depth = PyTraceback.CaptureDepth();
        try { return PyEncoding.ToByte(value); }
        catch (PyBaseException error) when (error is PyTypeError or PyValueError or PyOverflowError)
        {
            // memoryview与bytearray不同：替换这些数值转换错误，且不保留被吞回调帧。
            PyTraceback.PopTo(depth + 1);
            PyBaseException replacement = error is PyTypeError
                ? new PyTypeError("memoryview: invalid type for format 'B'")
                : new PyValueError("memoryview: invalid value for format 'B'");
            throw (PyBaseException)PyErrors.PrepareRaise(replacement);
        }
    }
    /// <inheritdoc />
    public byte[] CopyBuffer()
    {
        object owner = Owner();
        var result = new byte[_length];
        if (owner is PyBytes bytes) Array.Copy(bytes.Bytes, _offset, result, 0, _length);
        else ((PyByteArray)owner).Bytes.CopyTo(_offset, result, 0, _length);
        return result;
    }
    private PyBytes ToBytes(object? order)
    {
        if (order is not null and not PyNone and not string)
            throw new PyTypeError("tobytes() argument 'order' must be str or None");
        Owner();
        if (order is string text && text is not "C" and not "F" and not "A")
            throw new PyValueError("order must be 'C', 'F' or 'A'");
        // 一维连续布局的C/F/A顺序相同，仍真实复制当前内容而不缓存旧快照。
        return new PyBytes(CopyBuffer());
    }
    private PyList ToList()
    {
        Owner(); var items = new object?[_length];
        for (int i = 0; i < items.Length; i++) items[i] = Value(i);
        return new PyList(items);
    }
    private object EqualBuffer(object? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (Volatile.Read(ref _released) != 0) return false;
        if (other is PyMemoryView view)
        {
            if (Volatile.Read(ref view._released) != 0 || view._length != _length) return false;
            if (_length != 0 && _format != view._format) return false;
            for (int i = 0; i < _length; i++) if (Byte(i) != view.Byte(i)) return false;
            return true;
        }
        if (other is not PyBytes and not PyByteArray) return PyNotImplemented.Instance;
        int count = other is PyBytes buffer ? buffer.Count : ((PyByteArray)other).Count;
        if (count != _length || count != 0 && _format != "B") return false;
        for (int i = 0; i < count; i++)
        {
            byte expected = other is PyBytes bytes ? bytes.Bytes[i] : ((PyByteArray)other!).Bytes[i];
            if (Byte(i) != expected) return false;
        }
        return true;
    }
    private object NegateEquality(object? other)
    {
        object result = EqualBuffer(other);
        return result is bool value ? !value : result;
    }
    private object Hash()
    {
        // readonly视图已计算的hash在release后仍有效；从未计算则必须先检查活性。
        if (_hash is long cached) return cached;
        if (Owner() is PyByteArray) throw new PyValueError("cannot hash writable memoryview object");
        long value = PyHash.Of(new PyBytes(CopyBuffer()));
        _hash = value;
        return value;
    }
    /// <summary>幂等释放只撤销当前view的导出；孩子直接持exporter，不依赖父view。</summary>
    public void Release()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0) return;
        object? owner = _owner; _owner = null;
        if (owner is PyByteArray array) array.ReleaseExport();
        GC.SuppressFinalize(this);
    }
    /// <summary>无人再引用的租约最终回收；不调用用户代码，不保证CPython引用计数的立即释放时机。</summary>
    ~PyMemoryView() { Release(); }
    /// <inheritdoc />
    public override string PythonRepr() => $"<{(Volatile.Read(ref _released) != 0 ? "released " : "")}memory at 0x{RuntimeHelpers.GetHashCode(this):x}>";
    /// <summary>迭代器只保存view和下标；已耗尽后不因view释放重新报错。</summary>
    private sealed class ViewIterator(PyMemoryView view) : PyToolIterator
    {
        private int _index;
        private bool _finished;
        private object? _current;
        public override string TypeName => "memory_iterator";
        public override object? Current() => _current;
        public override bool MoveNext()
        {
            if (_finished) return false;
            view.Owner();
            if (_index == view._length) { _finished = true; _current = null; return false; }
            _current = view.Value(_index++);
            return true;
        }
    }
}
