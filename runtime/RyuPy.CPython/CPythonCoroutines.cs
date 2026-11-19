// 双向 await 桥：原生槽保留暂停/终值，协程实体、发送和异常注入仍由 CIL 管理。
using System.Runtime.InteropServices;
using RyuPy.Runtime;

namespace RyuPy.CPython;

internal static class CoroutineNative
{
    [DllImport(Native.Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_managed_coroutine(nint identity, int iterator);
    [DllImport(Native.Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_await_iterator(nint source);
    [DllImport(Native.Library, CallingConvention = CallingConvention.Cdecl)]
    internal static extern nint rp_await_next(nint iterator, out int done);
}

internal sealed partial class PythonObject : IPyForeignAwaitable
{
    public IPyForeignAwaitIterator? GetAwaitIterator() => Host.PythonCall<IPyForeignAwaitIterator?>(() =>
    {
        nint result = CoroutineNative.rp_await_iterator(Pointer);
        if (result == 0) { Host.ThrowIfError(); return null; }
        // 这里需要原生迭代器本身供后续协议调用；包括由反向桥导出的迭代器。
        return new ForeignAwaitIterator(new PythonObject(Host, result));
    });

    private sealed class ForeignAwaitIterator(PythonObject iterator) : IPyForeignAwaitIterator
    {
        public object Target => iterator;
        public object? NextValue() => iterator.Host.PythonCall(() =>
        {
            nint value = CoroutineNative.rp_await_next(iterator.Pointer, out int done);
            object? result = iterator.Host.FromOwned(value);
            if (done != 0) throw new PyStopIteration(result);
            return result;
        });

        private object? current;
        public bool MoveNext()
        {
            try { current = NextValue(); return true; }
            catch (PyStopIteration) { current = null; return false; }
        }
        public object? Current() => current;
    }
}

public sealed partial class CPythonHost
{
    private object? ResumeCoroutineProxy(object? target, int operation, nint value) => target switch
    {
        PyCoroutine coroutine => operation switch
        {
            31 => coroutine.ForeignAwaitIterator(),
            32 => coroutine.Send(FromBorrowed(value)),
            33 => coroutine.Throw(FromBorrowed(value)),
            34 => coroutine.Close(),
            _ => throw new PyRuntimeError("invalid coroutine operation")
        },
        PyCoroutineIterator iterator => operation switch
        {
            32 => iterator.Send(FromBorrowed(value)),
            33 => iterator.Throw(FromBorrowed(value)),
            34 => iterator.Close(),
            _ => throw new PyRuntimeError("invalid coroutine iterator operation")
        },
        _ => throw new PyRuntimeError("invalid coroutine proxy")
    };
}
