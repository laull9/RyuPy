// 字典插入回归：用户哈希/相等方法有副作用，查找与登记不能重复调用它们。
using Xunit;
using System.Linq;

namespace RyuPy.Runtime.Tests;

public class PyDictMutationTests
{
    [Fact]
    public void InsertionHashesEachKeyOnceAndReplacementKeepsPosition()
    {
        int hashes = 0;
        var cls = (PyClass)PyOps.MakeClass("Key", [], null);
        var hash = new PyFunction("__hash__",
            PySignature.Create("__hash__", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => { hashes++; return 11L; }, []);
        cls.SetAttr("__hash__", hash);
        var first = (PyInstance)cls.Invoke([], [], [])!;
        var second = (PyInstance)cls.Invoke([], [], [])!;
        var dict = new PyDict();
        dict.Set(first, 1L);
        dict.Set(second, 2L);
        Assert.Equal(2, hashes);
        long version = dict.Version;
        dict.Set(first, 3L);
        Assert.Equal(3, hashes);
        Assert.Equal(2, dict.Count);
        Assert.Equal(version, dict.Version);
        Assert.Same(first, dict.Entries().First().Key);
    }
}
