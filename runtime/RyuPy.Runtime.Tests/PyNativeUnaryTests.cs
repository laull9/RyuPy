// 受控native一参资格及数组回退；不以分配计时替代参数、身份与重入语义断言。
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyNativeUnaryTests
{
    private static PyFunction MathFunction(string name) => Assert.IsType<PyFunction>(PyMathModule.CreateAttributes()[name]);
    private static object? Scalar(object? target, object? value) =>
        PyPic.CallCapturedMethod1(3950, true, target, "not-a-dispatch-name", value);
    private static object? ArrayCall(object? target, params object?[] args) => PyOps.CallValue(target, args, [], []);

    [Fact]
    public void AllThirteenUniformFunctionsPreserveValuesAndExactResultTypes()
    {
        (string, object, object)[] cases =
        [
            ("sqrt", 4.0, 2.0), ("fabs", -2.0, 2.0), ("exp", 0.0, 1.0),
            ("sin", 0.0, 0.0), ("cos", 0.0, 1.0), ("tan", 0.0, 0.0),
            ("isfinite", 1.0, true), ("isinf", double.PositiveInfinity, true),
            ("isnan", double.NaN, true), ("floor", 1.5, 1L), ("ceil", 1.5, 2L),
            ("trunc", 1.5, 1L), ("log10", 100.0, 2.0)
        ];
        foreach (var (name, argument, expected) in cases)
        {
            PyFunction function = MathFunction(name);
            object? scalar = Scalar(function, argument);
            Assert.Equal(expected, scalar); Assert.Equal(expected.GetType(), scalar!.GetType());
            Assert.Equal(ArrayCall(function, argument), scalar);
        }
    }

    [Fact]
    public void QualificationUsesExactOwnerAndBodyNotMutableName()
    {
        PyFunction original = MathFunction("sqrt");
        PyObject.SetAttr(original, "__name__", "renamed");
        Assert.Equal(3.0, Scalar(original, 9.0));
        var copied = new PyFunction("sqrt", original.Signature, original.Body, original.Defaults, false);
        MethodInfo check = typeof(PyNativeFunctions).GetMethod("TryInvokeUnary", BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] args = [copied, 9.0, null];
        Assert.Equal(false, check.Invoke(null, args)); Assert.Null(args[2]);
        args = [original, 9.0, null];
        Assert.Equal(true, check.Invoke(null, args)); Assert.Equal(3.0, args[2]);
        Assert.Equal(3.0, Scalar(copied, 9.0));
    }

    [Fact]
    public void ArrayBinderAndStarMarkerKeepArityAndKeywordRules()
    {
        PyFunction function = MathFunction("sqrt");
        Assert.Equal(3.0, Scalar(function, PyOps.StarArg(new PyTuple([9.0]))));
        foreach (object?[] values in new object?[][] { [], [1.0, 2.0] })
        {
            PyTypeError expected = Assert.Throws<PyTypeError>(() => ArrayCall(function, values));
            PyTypeError actual = Assert.Throws<PyTypeError>(() => Scalar(function, PyOps.StarArg(new PyTuple(values))));
            Assert.Equal(expected.Message, actual.Message);
        }
        Assert.Throws<PyTypeError>(() => PyOps.CallValue(function, [], ["x"], [9.0]));
        Assert.Throws<PyTypeError>(() => Scalar(function, "9"));
        Assert.Throws<PyTypeError>(() => Scalar(PyNone.Instance, 9.0));
    }

    [Fact]
    public void SavedCalleeAndConversionReentryRemainIndependent()
    {
        PyFunction original = MathFunction("sqrt");
        string name = "native_unary_" + Guid.NewGuid().ToString("N");
        PyModules.RegisterNative(name, new Dictionary<string, object?> { ["f"] = original });
        PyModule module = PyModules.Import(name);
        object? captured = PyOps.CaptureModuleTarget(module, "f");
        var replacement = PyNativeFunctions.Create("sqrt", ["x"], [null], 1, _ => 99L);
        module.SetAttr("f", replacement);
        int converted = 0;
        var cls = Assert.IsType<PyClass>(PyOps.MakeClass("UnaryConversion", [], null));
        cls.SetAttr("__float__", new PyFunction("__float__", PySignature.Create("__float__", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => { converted++; Assert.Equal(4.0, Scalar(original, 16.0)); return 9.0; }, [null], false));
        object argument = cls.Invoke([], [], [])!;
        Assert.Equal(3.0, Scalar(captured, argument)); Assert.Equal(1, converted);
        Assert.Equal(99L, Scalar(PyOps.CaptureModuleTarget(module, "f"), argument)); Assert.Equal(1, converted);
    }

    [Fact]
    public void ArbitrarySameNameBodiesRetainFreshArraysAcrossReentry()
    {
        var retained = new List<object?[]>(); bool nested = false; PyFunction? function = null;
        function = new PyFunction("sqrt", PySignature.Create("sqrt", ["x"], ["posonly"], ["0"]),
            (_, args, _, _) =>
            {
                retained.Add(args); object? value = args[0];
                if (!nested) { nested = true; Assert.Equal(7L, Scalar(function, 7L)); nested = false; }
                Assert.Same(value, args[0]); return args[0];
            }, [null], false);
        Assert.Equal(4L, Scalar(function, 4L)); Assert.Equal(9L, Scalar(function, 9L));
        Assert.Equal(4, retained.Count);
        for (int i = 0; i < retained.Count; i++)
            for (int j = i + 1; j < retained.Count; j++) Assert.NotSame(retained[i], retained[j]);
        Assert.Equal(4L, retained[0][0]); Assert.Equal(9L, retained[2][0]);
    }

    [Fact]
    public void ConversionErrorsKeepOriginalExceptionAndNullNormalizesToNone()
    {
        var error = new PyValueError("same-instance");
        var cls = Assert.IsType<PyClass>(PyOps.MakeClass("UnaryFailure", [], null));
        cls.SetAttr("__float__", new PyFunction("__float__", PySignature.Create("__float__", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => throw error, [null], false));
        object argument = cls.Invoke([], [], [])!; PyFunction sqrt = MathFunction("sqrt");
        Assert.Same(error, Assert.Throws<PyValueError>(() => Scalar(sqrt, argument)));
        Assert.Same(error, Assert.Throws<PyValueError>(() => ArrayCall(sqrt, argument)));
        Assert.Equal(Assert.Throws<PyValueError>(() => ArrayCall(sqrt, -1.0)).Message,
            Assert.Throws<PyValueError>(() => Scalar(sqrt, -1.0)).Message);
        cls.SetAttr("__floor__", new PyFunction("__floor__", PySignature.Create("__floor__", ["self"], ["pos"], ["0"]),
            (_, _, _, _) => null, [null], false));
        Assert.Same(PyNone.Instance, Scalar(MathFunction("floor"), argument));
    }

    [Fact]
    public void DomainExceptionFramesAndActiveContextMatchArrayEntry()
    {
        PyFunction sqrt = MathFunction("sqrt");
        var outer = new PyValueError("outer"); object? context = PyErrors.EnterHandler(outer);
        long depth = PyTraceback.PushFrame("native-unary.py", "caller");
        try
        {
            PyTraceback.SetLine(23);
            PyValueError expected = Assert.Throws<PyValueError>(() => ArrayCall(sqrt, -1.0));
            PyValueError actual = Assert.Throws<PyValueError>(() => Scalar(sqrt, -1.0));
            Assert.Equal(expected.Message, actual.Message);
            Assert.Same(expected.Context, actual.Context);
            Assert.Equal(expected.Frames, actual.Frames);
        }
        finally { PyTraceback.PopTo(depth); PyErrors.LeaveHandler(context); }
    }

    [Fact]
    public void FalseFlagKeepsOrdinaryListPic()
    {
        var list = new PyList([]);
        Assert.Same(PyNone.Instance, PyPic.CallCapturedMethod1(3951, false, list, "append", 4L));
        Assert.Equal(4L, list.Items[0]);
    }
}
