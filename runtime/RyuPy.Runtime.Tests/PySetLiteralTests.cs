using Xunit;

namespace RyuPy.Runtime.Tests;

public class PySetLiteralTests
{
    [Fact]
    public void AddAndExtendDeduplicateUsingPythonEquality()
    {
        var target = new PySet();
        PyOps.SetAdd(target, 1L);
        PyOps.SetExtend(target, new PyTuple([true, 1.0, 2L, null]));
        Assert.Equal(3, target.Count);
        Assert.True(target.Contains(1L));
        Assert.True(target.Contains(2L));
        Assert.True(target.Contains(PyNone.Instance));
    }

    [Fact]
    public void IterationAndHashingAreInterleaved()
    {
        var log = new List<string>();
        object first = Key("first", log), second = Key("second", log);
        var iterator = new LoggingIterator([first, second], log);
        var target = new PySet();
        PyOps.SetExtend(target, iterator);
        Assert.Equal(new[] { "next:0", "hash:first", "next:1", "hash:second", "next:2" }, log);
        Assert.Equal(2, target.Count);
    }

    [Fact]
    public void HashFailureKeepsPrefixAndDoesNotAdvanceAgain()
    {
        var log = new List<string>();
        object first = Key("first", log), bad = Key("bad", log, fail: true);
        var iterator = new LoggingIterator([first, bad, 3L], log);
        var target = new PySet();
        Assert.Throws<PyValueError>(() => PyOps.SetExtend(target, iterator));
        Assert.Equal(new[] { "next:0", "hash:first", "next:1", "hash:bad" }, log);
        Assert.Same(first, Assert.Single(target.Items));
    }

    [Fact]
    public void IterationFailureKeepsPrefixAndInvalidInputsUsePythonErrors()
    {
        var target = new PySet();
        var iterator = new LoggingIterator([1L], [], failAtEnd: true);
        Assert.Throws<PyValueError>(() => PyOps.SetExtend(target, iterator));
        Assert.Equal(1, target.Count);
        Assert.True(target.Contains(1L));
        Assert.Throws<PyTypeError>(() => PyOps.SetAdd(new PyList(), 1L));
        Assert.Throws<PyTypeError>(() => PyOps.SetExtend(new PyList(), new PyTuple([])));
        Assert.Throws<PyTypeError>(() => PyOps.SetExtend(target, 42L));
        Assert.Throws<PyTypeError>(() => PyOps.SetAdd(target, new PyList()));
        Assert.Equal(1, target.Count);
    }

    [Fact]
    public void SetSourcesWithUserHashAreRejectedBeforeAnyRehashOrWrite()
    {
        var log = new List<string>();
        var key = (PyInstance)Key("first", log);
        var source = new PySet([key]);
        var frozen = new PyFrozenSet([key]);
        log.Clear();
        key.Class.SetAttr("__hash__", new PyFunction("__hash__", PySignature.Create(
            "__hash__", ["self"], ["pos"], ["0"]), (_, _, _, _) =>
            {
                log.Add("unexpected hash");
                throw new PyValueError("changed hash");
            }, []));
        foreach (object input in new object[] { source, frozen })
        {
            var target = new PySet([42L]);
            Assert.Throws<PyNotImplementedError>(() => PyOps.SetExtend(target, input));
            Assert.Equal(42L, Assert.Single(target.Items));
            Assert.Empty(log);
        }
    }

    [Fact]
    public void NestedTuplesFrozenSetsAndUserExceptionsAreAlsoRejected()
    {
        var log = new List<string>();
        object key = Key("first", log);
        var inner = new PyFrozenSet([new PyTuple([key])]);
        var source = new PySet([new PyTuple([inner])]);
        log.Clear();
        var target = new PySet();
        Assert.Throws<PyNotImplementedError>(() => PyOps.SetExtend(target, source));
        Assert.Empty(log);
        Assert.Equal(0, target.Count);
        var error = new PyValueError("user error");
        error.BindPythonClass((PyClass)PyOps.MakeClass("UserError", [], null));
        var exceptions = new PyFrozenSet([error]);
        Assert.Throws<PyNotImplementedError>(() => PyOps.SetExtend(target, exceptions));
        Assert.Equal(0, target.Count);
    }

    [Fact]
    public void BuiltinOnlySetSourcesRemainSupported()
    {
        var target = new PySet();
        var tuple = new PyTuple([1L, "value"]);
        var frozen = new PyFrozenSet([2L]);
        PyOps.SetExtend(target, new PySet([tuple, frozen, null]));
        PyOps.SetExtend(target, new PyFrozenSet([3L]));
        Assert.Equal(4, target.Count);
        Assert.True(target.Contains(tuple));
        Assert.True(target.Contains(frozen));
        Assert.True(target.Contains(PyNone.Instance));
        Assert.True(target.Contains(3L));
    }

    private static object Key(string name, List<string> log, bool fail = false)
    {
        var cls = (PyClass)PyOps.MakeClass("Key", [], null);
        cls.SetAttr("__hash__", new PyFunction("__hash__", PySignature.Create(
            "__hash__", ["self"], ["pos"], ["0"]), (_, _, _, _) =>
            {
                log.Add("hash:" + name);
                if (fail) throw new PyValueError("hash failure");
                return name == "first" ? 1L : 2L;
            }, []));
        return cls.Invoke([], [], [])!;
    }

    private sealed class LoggingIterator(object?[] items, List<string> log, bool failAtEnd = false) : IPyIterator
    {
        private int _index = -1;
        public bool MoveNext()
        {
            _index++;
            log.Add("next:" + _index);
            if (_index == items.Length && failAtEnd) throw new PyValueError("iteration failure");
            return _index < items.Length;
        }
        public object? Current() => items[_index];
    }
}
