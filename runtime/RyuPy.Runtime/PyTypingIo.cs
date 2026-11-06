// typing IO 族的真实普通类工厂；三根全部完成后，模块注册与内部专项才可取得身份。
using System;

namespace RyuPy.Runtime;

internal static partial class PyTypingIo
{
    internal static readonly PyTuple Empty = new([]);
    private static readonly Lazy<Roots> Classes = new(Build);
    private sealed record Roots(PyClass IO, PyClass BinaryIO, PyClass TextIO);

    // 公共注册与内部专项共享完成后的稳定根，不导出占位类或半成品。
    internal static PyClass Root(string name) => name switch
    {
        "IO" => Classes.Value.IO,
        "BinaryIO" => Classes.Value.BinaryIO,
        "TextIO" => Classes.Value.TextIO,
        _ => throw new ArgumentException("未知的 typing IO 根类", nameof(name)),
    };

    private static Roots Build()
    {
        // 空布局标志随类创建即固定，不能等到发布后才禁止第一次槽位分配。
        var io = new PyClass("IO") { NativeDefinitionModule = "typing", TypingIoKind = PyTypingIoKind.IO };
        var binary = new PyClass("BinaryIO")
        {
            NativeDefinitionModule = "typing", TypingIoKind = PyTypingIoKind.Binary, BaseClass = io,
        };
        var text = new PyClass("TextIO")
        {
            NativeDefinitionModule = "typing", TypingIoKind = PyTypingIoKind.Text, BaseClass = io,
        };
        Initialize(io, "Generic base class for TextIO and BinaryIO.\n\n"
            + "    This is an abstract, generic version of the return of open().\n\n"
            + "    NOTE: This does not distinguish between the different possible\n"
            + "    classes (text vs. binary, read vs. write vs. read/write,\n"
            + "    append-only, unbuffered).  The TextIO and BinaryIO subclasses\n"
            + "    below capture the distinctions between text vs. binary, which is\n"
            + "    pervasive in the interface; however we currently do not offer a\n"
            + "    way to track the other distinctions in the type system.\n    ");
        Initialize(binary, "Typed version of the return of open() in binary mode.");
        Initialize(text, "Typed version of the return of open() in text mode.");

        foreach (string name in new[] { "mode", "name", "closed" }) AddProperty(io, name);
        foreach (string name in new[] { "close", "fileno", "flush", "isatty", "readable", "seekable",
            "tell", "writable", "__enter__" }) AddMethod(io, name, ["self"], [null], 1);
        AddMethod(io, "read", ["self", "n"], [null, -1L], 1);
        AddMethod(io, "readline", ["self", "limit"], [null, -1L], 1);
        AddMethod(io, "readlines", ["self", "hint"], [null, -1L], 1);
        AddMethod(io, "seek", ["self", "offset", "whence"], [null, null, 0L], 2);
        AddMethod(io, "truncate", ["self", "size"], [null, PyNone.Instance], 1);
        AddMethod(io, "write", ["self", "s"], [null, null], 2);
        AddMethod(io, "writelines", ["self", "lines"], [null, null], 2);
        AddMethod(io, "__exit__", ["self", "type", "value", "traceback"], [null, null, null, null], 4);
        AddMethod(binary, "write", ["self", "s"], [null, null], 2);
        AddMethod(binary, "__enter__", ["self"], [null], 1);
        foreach (string name in new[] { "buffer", "encoding", "errors", "line_buffering", "newlines" })
            AddProperty(text, name);
        AddMethod(text, "__enter__", ["self"], [null], 1);
        PyOps.FinalizeOrdinaryClass(io);
        PyOps.FinalizeOrdinaryClass(binary);
        PyOps.FinalizeOrdinaryClass(text);
        return new Roots(io, binary, text);
    }

    private static void Initialize(PyClass cls, string doc)
    {
        cls.SetAttr("__module__", "typing");
        cls.SetAttr("__doc__", doc);
        cls.SetAttr("__slots__", Empty);
        if (cls.TypingIoKind is PyTypingIoKind.Binary or PyTypingIoKind.Text)
            cls.SetAttr("__parameters__", Empty);
    }

    internal static object Subscribe(PyClass cls)
    {
        if (cls.TypingIoKind is PyTypingIoKind.Binary or PyTypingIoKind.Text)
            throw Error(new PyTypeError($"{ClassRepr(cls)} is not a generic class"));
        throw Unsupported("typing IO generic subscription is not supported");
    }

    internal static PyNotImplementedError Unsupported(string message) => Error(new PyNotImplementedError(message));
    internal static T Error<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
