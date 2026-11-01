// 流公共协议：参数绑定、关闭检查、逐行迭代及上下文管理。
// 文件与内存实现只负责读写存储；所有公开操作在此转换 CLR I/O 错误。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>Python I/O 协议的显式运行时实现。</summary>
public abstract class PyIOStream : PyNativeObject, IPyIterator
{
    private object? _current;
    /// <summary>流是否已经关闭；重复关闭不产生额外副作用。</summary>
    public bool Closed { get; private set; }
    /// <summary>构造时确定的可读能力。</summary>
    protected bool CanRead { get; }
    /// <summary>构造时确定的可写能力。</summary>
    protected bool CanWrite { get; }
    /// <summary>按给定配置构造运行时对象。</summary>
    protected PyIOStream(bool readable = true, bool writable = true)
    { CanRead = readable; CanWrite = writable; }

    /// <summary>读取最多 size 个元素；line 为真时在行结束处停止。</summary>
    protected abstract object ReadCore(long size, bool line);
    /// <summary>写入一个类型匹配的值并返回输入元素数。</summary>
    protected abstract long WriteCore(object? value);
    /// <summary>按流类型验证定位规则并返回新位置。</summary>
    protected abstract long SeekCore(long offset, long whence);
    /// <summary>返回可供 seek 恢复的位置。</summary>
    protected abstract long TellCore();
    /// <summary>调整有效内容长度而不移动当前位置。</summary>
    protected abstract long TruncateCore(long? size);
    /// <summary>将待写数据送往底层流。</summary>
    protected virtual void FlushCore() { }
    /// <summary>释放具体存储资源。</summary>
    protected virtual void CloseCore() { }
    /// <summary>内存流的内容快照支持。</summary>
    protected virtual object ValueCore() => throw new PyAttributeError($"'{TypeName}' object has no attribute 'getvalue'");
    /// <summary>内存流的内容快照支持。</summary>
    protected virtual bool HasValue => false;
    /// <summary>文本与磁盘二进制 write 在状态检查前验证值类型；BytesIO 保留其关闭优先级。</summary>
    protected virtual void ValidateWriteValue(object? value) { }
    /// <summary>多数 readlines 先转换 hint；BytesIO 的继承实现先检查关闭状态。</summary>
    protected virtual bool HintBeforeOpen => true;
    /// <summary>子类扩展文件名、编码等只读属性。</summary>
    protected virtual object? ExtraAttribute(string name) => throw Missing(name);

    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "closed" => Closed,
        "read" => Method(name, ["size"], [-1L], 0, a => Read(a[0], false)),
        "readline" => Method(name, ["size"], [-1L], 0, a => Read(a[0], true)),
        "readlines" => Method(name, ["hint"], [-1L], 0, a => ReadLines(a[0])),
        "write" => Method(name, ["data"], [null], 1, a => { ValidateWriteValue(a[0]); CheckWrite(); return WriteCore(a[0]); }),
        "writelines" => Method(name, ["lines"], [null], 1, a => WriteLines(a[0])),
        "seek" => Method(name, ["offset", "whence"], [null, 0L], 1,
            a => { CheckOpen(); return SeekCore(PyIO.Index(a[0]), PyIO.Index(a[1])); }),
        "tell" => Method(name, [], [], 0, _ => { CheckOpen(); return TellCore(); }),
        "truncate" => Method(name, ["size"], [null], 0,
            a => { CheckWrite(); return TruncateCore(PyIO.IsNone(a[0]) ? null : PyIO.Index(a[0])); }),
        "flush" => Method(name, [], [], 0, _ => { CheckOpen(); FlushCore(); return null; }),
        "close" => Method(name, [], [], 0, _ => { Close(); return null; }),
        "readable" => Method(name, [], [], 0, _ => { CheckOpen(); return CanRead; }),
        "writable" => Method(name, [], [], 0, _ => { CheckOpen(); return CanWrite; }),
        "seekable" => Method(name, [], [], 0, _ => { CheckOpen(); return true; }),
        "isatty" => Method(name, [], [], 0, _ => { CheckOpen(); return false; }),
        "__enter__" => Method(name, [], [], 0, _ => { CheckOpen(); return this; }),
        "__exit__" => Method(name, ["type", "value", "traceback"], [null, null, null], 3,
            _ => { Close(); return false; }),
        "__iter__" => Method(name, [], [], 0, _ => { CheckOpen(); return this; }),
        "__next__" => Method(name, [], [], 0, _ => MoveNext() ? Current() : throw new PyStopIteration()),
        "getvalue" when HasValue => Method(name, [], [], 0, _ => { CheckOpen(); return ValueCore(); }),
        _ => ExtraAttribute(name),
    };

    private PyFunction Method(string name, string[] parameters, object?[] defaults, int required,
        Func<object?[], object?> body) => PyNativeFunctions.Create(name, parameters, defaults, required,
            a => PyIO.Guard(() => body(a)), PositionalKinds(parameters.Length));

    private static string[] PositionalKinds(int count)
    { var kinds = new string[count]; Array.Fill(kinds, "posonly"); return kinds; }

    /// <summary>在执行具体操作前验证流未关闭。</summary>
    protected void CheckOpen()
    { if (Closed) throw new PyValueError("I/O operation on closed file."); }
    /// <summary>在执行具体操作前验证流状态和能力。</summary>
    protected void CheckRead()
    { CheckOpen(); if (!CanRead) throw new PyOSError("not readable"); }
    /// <summary>在执行具体操作前验证流状态和能力。</summary>
    protected void CheckWrite()
    { CheckOpen(); if (!CanWrite) throw new PyOSError("not writable"); }

    private object Read(object? size, bool line)
    {
        long count = PyIO.IsNone(size) ? -1 : PyIO.Index(size);
        CheckRead(); return ReadCore(count, line);
    }
    private object ReadLines(object? hint)
    {
        if (!HintBeforeOpen) CheckRead();
        long limit = PyIO.IsNone(hint) ? -1 : PyIO.Index(hint), count = 0;
        CheckRead();
        var result = new List<object?>();
        while (true)
        {
            object line = ReadCore(-1, true);
            long length = Length(line);
            if (length == 0) break;
            result.Add(line); count += length;
            if (limit > 0 && count > limit) break;
        }
        return new PyList(result);
    }
    private object? WriteLines(object? source)
    {
        CheckWrite(); var iterator = PyIter.Create(source!);
        while (iterator.MoveNext()) WriteCore(iterator.Current());
        return null;
    }
    private static long Length(object value) => value is string text ? PyText.Length(text) : ((PyBytes)value).Count;
    /// <inheritdoc/>
    public bool MoveNext() => (bool)PyIO.Guard(() =>
    {
        CheckRead(); _current = ReadCore(-1, true); return Length(_current) != 0;
    })!;
    /// <inheritdoc/>
    public object? Current() => _current;
    /// <summary>刷新并关闭流；即使刷新失败也释放底层资源。</summary>
    public void Close()
    {
        if (Closed) return;
        try { PyIO.Guard(() => { FlushCore(); return null; }); }
        finally { Closed = true; PyIO.Guard(() => { CloseCore(); return null; }); }
    }
}
