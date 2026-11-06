// zipfile 的有限尾记录识别入口：借入流不关闭，自开文件按原异常上下文释放。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

/// <summary>真实 is_zipfile 与 BadZipFile；不提供尚未实现的归档解压接口。</summary>
public static class PyZipfile
{
    /// <summary>构造模块属性表。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["is_zipfile"] = PyNativeFunctions.Create("is_zipfile", ["filename"], [null], 1, a => IsZipfile(a[0])),
        ["BadZipFile"] = PyType.Get("BadZipFile"),
    };

    private static bool IsZipfile(object? filename)
    {
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        try
        {
            if (HasRead(filename)) return CheckFile(filename);
            var owned = (PyIOStream)PyIO.Open([filename, "rb"], [], [])!;
            PyBaseException? active = null; object? previous = null;
            try { return CheckFile(owned); }
            catch (PyBaseException error)
            {
                // Python with 的异常清理：close 看到原异常，不重复调用或伪造宿主异常类型。
                PyErrors.PrepareRaise(error);
                PyTraceback.PopTo(depth + 1);
                previous = PyErrors.EnterHandler(error); active = error;
                throw;
            }
            finally
            {
                try { owned.Close(); }
                finally { if (active is not null) PyErrors.LeaveHandler(previous); }
            }
        }
        catch (PyOSError error) { Restore(error, depth, context); return false; }
        catch (PyBadZipFile error) { Restore(error, depth, context); return false; }
    }

    // _check_zipfile 先吞读取OSError；因此owned close在原ambient上下文中执行。
    private static bool CheckFile(object? stream)
    {
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        try { return PyZipfileTail.Check(stream); }
        catch (PyOSError error) { Restore(error, depth, context); return false; }
    }

    private static bool HasRead(object? value)
    {
        long depth = PyTraceback.CaptureDepth(); object? context = PyErrors.EnterHandler(null);
        try { _ = PyObject.GetAttr(value, "read"); return true; }
        catch (PyAttributeError error) { Restore(error, depth, context); return false; }
    }

    // 仅吞错分支恢复帧和入口处理状态；非吞错传播保原对象与已记录的帧。
    internal static void Restore(PyBaseException error, long depth, object? context)
    {
        PyTraceback.Attach(error);
        PyTraceback.PopTo(depth + 1);
        PyErrors.LeaveHandler(context);
    }
}
