// 已捕获绑定函数快路：独立参数、重入、星号回退及super上下文合同。
namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyCapturedBoundPreparedTests
{
    private static PyFunction Function(PyCall body, bool usesSuper = false) =>
        new("f", PySignature.Create("f", [], [], []), body, [], usesSuper);

    [Fact]
    public void EveryArityKeepsFreshBoundArgumentsDuringAndAfterReentry()
    {
        var cls = new PyClass("PreparedBound"); var self = new PyInstance(cls);
        var retained = new List<object?[]>(); bool nested = false; object? target = null;
        cls.SetAttr("f", Function((_, args, _, _) =>
        {
            Assert.Same(self, args[0]); retained.Add(args); object?[] before = args.ToArray();
            if (!nested)
            {
                nested = true;
                Assert.Equal(2, PyPic.CallCapturedMethod1(4830, true, target, "f", 99L));
                nested = false;
            }
            Assert.Equal(before, args); return args.Length;
        }));
        target = PyOps.CaptureMethodTarget(self, "f");
        cls.SetAttr("f", Function((_, _, _, _) => -1));
        Assert.Equal(1, PyPic.CallCapturedMethod0(4830, true, target, "f"));
        Assert.Equal(2, PyPic.CallCapturedMethod1(4830, true, target, "f", 10L));
        Assert.Equal(3, PyPic.CallCapturedMethod2(4830, true, target, "f", 20L, 21L));
        Assert.Equal(4, PyPic.CallCapturedMethod3(4830, true, target, "f", 30L, 31L, 32L));
        Assert.Equal(new object?[] { self, 20L, 21L }, retained[4]);
        Assert.Equal(new object?[] { self, 30L, 31L, 32L }, retained[6]);
        for (int i = 0; i < retained.Count; i++)
            for (int j = i + 1; j < retained.Count; j++) Assert.NotSame(retained[i], retained[j]);
    }

    [Fact]
    public void StarArgumentsStillExpandAtAllFixedArities()
    {
        var cls = new PyClass("PreparedStar"); var self = new PyInstance(cls);
        cls.SetAttr("f", Function((_, args, _, _) => args));
        object? target = PyOps.CaptureMethodTarget(self, "f");
        object star = PyOps.StarArg(new PyTuple([7L, 8L]));
        Assert.Equal(new object?[] { self, 7L, 8L }, Assert.IsType<object?[]>(PyPic.CallCapturedMethod1(4831, true, target, "f", star)));
        Assert.Equal(new object?[] { self, 1L, 7L, 8L }, Assert.IsType<object?[]>(PyPic.CallCapturedMethod2(4831, true, target, "f", 1L, star)));
        Assert.Equal(new object?[] { self, 1L, 7L, 8L, 2L }, Assert.IsType<object?[]>(PyPic.CallCapturedMethod3(4831, true, target, "f", 1L, star, 2L)));
    }

    [Fact]
    public void BoundSuperContextRestoresAfterThrowing()
    {
        var cls = new PyClass("PreparedSuper"); var self = new PyInstance(cls);
        var sentinel = new PyValueError("body");
        cls.SetAttr("f", Function((_, _, _, _) =>
        {
            Assert.Same(cls, PyMethodContext.CurrentOwner); Assert.Same(self, PyMethodContext.CurrentSelf);
            throw sentinel;
        }, true));
        object? target = PyOps.CaptureMethodTarget(self, "f");
        var oldOwner = PyMethodContext.CurrentOwner; var oldSelf = PyMethodContext.CurrentSelf;
        Assert.Same(sentinel, Assert.Throws<PyValueError>(() => PyPic.CallCapturedMethod2(4832, true, target, "f", 1L, 2L)));
        Assert.Same(oldOwner, PyMethodContext.CurrentOwner); Assert.Same(oldSelf, PyMethodContext.CurrentSelf);
    }
}
