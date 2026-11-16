// 原生translate明确拒绝尚未接入的外部表/包装值，不触碰provider转换或调用副作用。
using System;
using Xunit;
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyTextTranslateForeignBoundaryTests
{
    [Fact]
    public void ForeignTableAndMappingValueFailExplicitlyWithoutConversions()
    {
        var foreign = new ForeignSentinel();
        long frame = PyTraceback.PushFrame("foreign_translate.py", "caller");
        var outer = new PyRuntimeError("outer");
        object? previous = PyErrors.EnterHandler(outer);
        try
        {
            foreach (string source in new[] { "", "a", "é" })
            {
                var error = Assert.Throws<PyNotImplementedError>(() =>
                    PyOps.CallMethod(source, "translate", [foreign], [], []));
                Assert.Contains("foreign tables", error.Message);
                Assert.Same(outer, error.Context);
                Assert.Single(error.Frames);
            }
            foreach (string source in new[] { "a", "é", "猫é" })
            {
                // 最后一项先删除猫，再在通用删除段的预查中遇到外部包装值。
                var map = new PyDict(); map.Set(97L, foreign); map.Set(233L, foreign);
                map.Set(29483L, PyNone.Instance);
                var valueError = Assert.Throws<PyNotImplementedError>(() =>
                    PyOps.CallMethod(source, "translate", [map], [], []));
                Assert.Contains("foreign mapping values", valueError.Message);
                Assert.Same(outer, valueError.Context);
                Assert.Single(valueError.Frames);
            }
            Assert.Equal(0, foreign.Calls);
        }
        finally { PyErrors.LeaveHandler(previous); PyTraceback.PopTo(frame); }
    }

    // 仅测试中性接口边界，不模拟CPython生命周期，不允许任何隐式foreign调用。
    private sealed class ForeignSentinel : IPyForeignObject
    {
        public int Calls;
        private Exception Unexpected() { Calls++; return new InvalidOperationException("foreign callback must not run"); }
        public object ForeignType => throw Unexpected();
        public bool IsExceptionClass => throw Unexpected();
        public object ReadScalar(string kind) => throw Unexpected();
        public object? Item(object? key) => throw Unexpected();
        public void WriteItem(object? key, object? value, bool deleting) => throw Unexpected();
        public IPyIterator Iterate() => throw Unexpected();
        public long Length() => throw Unexpected();
        public bool Truth() => throw Unexpected();
        public long Hash() => throw Unexpected();
        public object Binary(int operation, object? other, bool reverse) => throw Unexpected();
        public bool Compare(int operation, object? other, bool reverse) => throw Unexpected();
        public object RichCompare(int operation, object? other, bool reverse) => throw Unexpected();
        public object Unary(int operation) => throw Unexpected();
        public bool IsInstanceOf(object type) => throw Unexpected();
        public bool Accepts(object? value) => throw Unexpected();
        public bool MatchesException(PyBaseException error) => throw Unexpected();
        public PyStackFrame[] TracebackFrames() => throw Unexpected();
        public void SynchronizeException(PyBaseException error, bool write) => throw Unexpected();
        public object? Invoke(object?[] args, string[] names, object?[] values) => throw Unexpected();
    }
}
