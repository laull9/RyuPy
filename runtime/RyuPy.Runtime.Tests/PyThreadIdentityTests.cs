// 线程身份与版本常量逐项验证，不把线程创建当作已支持的 Python API。
using System.Threading;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyThreadIdentityTests
{
    [Fact]
    public void IdentityIsStableAndDistinctAcrossLiveHostThreads()
    {
        var function = PyThreadIdentity.CreateAttributes()["get_ident"];
        long Here() => Assert.IsType<long>(PyOps.CallValue(function, [], [], []));
        long current = Here();
        Assert.NotEqual(0L, current);
        Assert.Equal(current, Here());
        long other = 0;
        using var ready = new ManualResetEventSlim();
        using var finish = new ManualResetEventSlim();
        var worker = new Thread(() =>
        {
            try { other = (long)PyOps.CallValue(function, [], [], [])!; }
            finally { ready.Set(); }
            finish.Wait();
        });
        worker.Start();
        try { ready.Wait(); Assert.NotEqual(current, other); Assert.NotEqual(0L, other); }
        finally { finish.Set(); worker.Join(); }
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(function, [1L], [], []));
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(function, [], ["value"], [1L]));
    }
}
