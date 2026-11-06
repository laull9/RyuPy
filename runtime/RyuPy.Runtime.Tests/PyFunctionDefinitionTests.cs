// 白盒验证不可变定义来源与可变限定名分离；不以公开代码反射冒充已支持的Python行为。
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace RyuPy.Runtime.Tests;

public class PyFunctionDefinitionTests
{
    private static PySignature Signature() => PySignature.Create("backend_symbol", [], [], []);
    private static PyFunction Function(PySignature signature) => new("visible", signature,
        (_, _, _, _) => PyNone.Instance, []);

    // 生产没有反射或额外公开接口；测试以强类型委托读取内部冷属性，保留原异常类型。
    private static readonly Func<PyFunction, long> FirstLine = typeof(PyFunction)
        .GetProperty("DefinitionFirstLine", BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetGetMethod(true)!.CreateDelegate<Func<PyFunction, long>>();

    private static object? ColdRecord(string tableName, object key)
    {
        Type storage = typeof(PyFunction).Assembly.GetType("RyuPy.Runtime.PyFunctionDefinition")!;
        object? table = storage.GetField(tableName, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);
        if (table is null) return null;
        object?[] arguments = [key, null];
        bool found = (bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table, arguments)!;
        return found ? arguments[1] : null;
    }

    [Fact]
    public void RepeatedCreationSharesImmutableSignatureSourceWithoutOverrides()
    {
        PySignature signature = Signature();
        PyOps.FunctionDefinition(signature, "Owner.factory.<locals>.inner", 23L);
        object record = ColdRecord("Definitions", signature)!;
        Assert.NotNull(record);
        foreach (FieldInfo field in record.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            Assert.True(field.IsInitOnly);
        for (int index = 0; index < 8; index++)
        {
            PyFunction function = Function(signature);
            Assert.Equal("Owner.factory.<locals>.inner", PyObject.GetAttr(function, "__qualname__"));
            Assert.Equal(23L, FirstLine(function));
            Assert.Same(record, ColdRecord("Definitions", function.Signature));
            Assert.Null(ColdRecord("Qualnames", function));
        }
        PySignature other = Signature();
        PyOps.FunctionDefinition(other, "other", 41L);
        Assert.NotSame(record, ColdRecord("Definitions", other));
        Assert.Equal(41L, FirstLine(Function(other)));
    }

    [Fact]
    public void QualnameWritesAreIsolatedAndDoNotChangeDefinitionOrName()
    {
        PySignature signature = Signature();
        PyOps.FunctionDefinition(signature, "outer.<locals>.inner", 12L);
        PyFunction first = Function(signature), second = Function(signature);
        PyObject.SetAttr(first, "__qualname__", "changed");
        object record = ColdRecord("Qualnames", first)!;
        Assert.NotNull(record);
        Assert.Null(ColdRecord("Qualnames", second));
        Assert.Equal("changed", PyObject.GetAttr(first, "__qualname__"));
        Assert.Equal("outer.<locals>.inner", PyObject.GetAttr(second, "__qualname__"));
        Assert.Equal("visible", PyObject.GetAttr(first, "__name__"));
        PyObject.SetAttr(first, "__name__", "renamed");
        Assert.Equal("changed", PyObject.GetAttr(first, "__qualname__"));
        PyObject.SetAttr(first, "__qualname__", "");
        Assert.Same(record, ColdRecord("Qualnames", first));
        Assert.Equal("", PyObject.GetAttr(first, "__qualname__"));
        Assert.Equal(12L, FirstLine(first));
        Assert.Equal(12L, FirstLine(second));
        foreach (object? invalid in new object?[] { null, PyNone.Instance, 42L, new PyList() })
        {
            var error = Assert.Throws<PyTypeError>(() => PyObject.SetAttr(first, "__qualname__", invalid));
            Assert.Equal("__qualname__ must be set to a string object", error.PyMessage);
        }
        var deletion = Assert.Throws<PyTypeError>(() => PyObject.DelAttr(first, "__qualname__"));
        Assert.Equal("__qualname__ must be set to a string object", deletion.PyMessage);
        Assert.Equal("", PyObject.GetAttr(first, "__qualname__"));
    }

    [Fact]
    public void RealStringSubclassFailsExplicitlyWithoutChangingQualnameOrDefinition()
    {
        // 从现有公开类构造路径取得真实str子类，不反射制造不合法载体。
        var labelClass = (PyClass)PyOps.MakeClass("Label", [PyType.Get("str")], null);
        PyOps.FinalizeOrdinaryClass(labelClass);
        var label = Assert.IsAssignableFrom<PyInstance>(labelClass.Invoke(["Namespace.target"], [], []));
        Assert.Equal("PyStringInstance", label.GetType().Name);
        Assert.Same(labelClass, PyObject.TypeOf(label));
        Assert.Equal(true, PyOps.CallBuiltin("isinstance", [label, PyType.Get("str")], [], []));
        PySignature signature = Signature();
        PyOps.FunctionDefinition(signature, "target", 7L);
        PyFunction function = Function(signature);
        object definition = ColdRecord("Definitions", signature)!;
        foreach (bool hasOverride in new[] { false, true })
        {
            if (hasOverride) PyObject.SetAttr(function, "__qualname__", "changed");
            object? previousOverride = ColdRecord("Qualnames", function);
            string expected = hasOverride ? "changed" : "target";
            var error = Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(function, "__qualname__", label));
            Assert.Equal("function qualname with str subclass identity is not supported", error.PyMessage);
            Assert.Equal(expected, PyObject.GetAttr(function, "__qualname__"));
            Assert.Same(previousOverride, ColdRecord("Qualnames", function));
            Assert.Same(definition, ColdRecord("Definitions", signature));
            Assert.Equal(7L, FirstLine(function));
            var invalid = Assert.Throws<PyTypeError>(() => PyObject.SetAttr(function, "__qualname__", 42L));
            Assert.Equal("__qualname__ must be set to a string object", invalid.PyMessage);
            Assert.Equal(expected, PyObject.GetAttr(function, "__qualname__"));
        }
    }

    [Fact]
    public void DictionaryCannotShadowQualnameOrCodeAndCodeNeverPretendsToMutate()
    {
        PySignature signature = Signature();
        PyOps.FunctionDefinition(signature, "lexical", 9L);
        PyFunction function = Function(signature);
        var attributes = Assert.IsType<PyDict>(PyObject.GetAttr(function, "__dict__"));
        attributes.Set("__qualname__", "fake");
        attributes.Set("__code__", "fake code");
        Assert.Equal("lexical", PyObject.GetAttr(function, "__qualname__"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(function, "__code__"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(function, "__code__", "replacement"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(function, "__code__"));
        Assert.Equal("fake code", attributes.Get("__code__"));
        Assert.Equal("fake", attributes.Get("__qualname__"));
        Assert.Equal(9L, FirstLine(function));
        PyObject.SetAttr(function, "__dict__", new PyDict());
        Assert.Equal("lexical", PyObject.GetAttr(function, "__qualname__"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(function, "__code__"));
        Assert.Throws<PyNotImplementedError>(() => PyObject.SetAttr(function, "__code__", PyNone.Instance));
        Assert.Throws<PyNotImplementedError>(() => PyObject.DelAttr(function, "__code__"));
        Assert.Equal(0, Assert.IsType<PyDict>(PyObject.GetAttr(function, "__dict__")).Count);
    }

    [Fact]
    public void UnknownNativeSourceDoesNotGuessFromNamesButExplicitQualnameRemainsReadable()
    {
        PyFunction function = Function(Signature());
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(function, "__qualname__"));
        Assert.Throws<PyNotImplementedError>(() => FirstLine(function));
        PyObject.SetAttr(function, "__name__", "not_source");
        var attributes = Assert.IsType<PyDict>(PyObject.GetAttr(function, "__dict__"));
        attributes.Set("__qualname__", "not_source_either");
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(function, "__qualname__"));
        PyObject.SetAttr(function, "__qualname__", "native.explicit");
        Assert.Equal("native.explicit", PyObject.GetAttr(function, "__qualname__"));
        Assert.Throws<PyNotImplementedError>(() => FirstLine(function));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(function, "__code__"));
        Assert.Null(ColdRecord("Definitions", function.Signature));
    }

    [Fact]
    public void RegistrationRejectsDuplicatesAndMalformedAbiWithoutReplacingSource()
    {
        PySignature signature = Signature();
        Assert.Throws<PyTypeError>(() => PyOps.FunctionDefinition(PyNone.Instance, "name", 1L));
        Assert.Throws<PyTypeError>(() => PyOps.FunctionDefinition(signature, null!, 1L));
        Assert.Throws<PyValueError>(() => PyOps.FunctionDefinition(signature, "name", 0L));
        Assert.Throws<PyValueError>(() => PyOps.FunctionDefinition(signature, "name", -1L));
        Assert.Null(ColdRecord("Definitions", signature));
        PyOps.FunctionDefinition(signature, "name", 5L);
        Assert.Throws<PyRuntimeError>(() => PyOps.FunctionDefinition(signature, "name", 5L));
        Assert.Throws<PyRuntimeError>(() => PyOps.FunctionDefinition(signature, "replacement", 90L));
        Assert.Equal("name", PyObject.GetAttr(Function(signature), "__qualname__"));
        Assert.Equal(5L, FirstLine(Function(signature)));
    }

    [Fact]
    public void QualnameOverrideTableUsesNullableLazyStorage()
    {
        // 全局测试顺序可能已有原生函数显式写入；验证可空冷槽而不依赖首次运行的偶然顺序。
        Type storage = typeof(PyFunction).Assembly.GetType("RyuPy.Runtime.PyFunctionDefinition")!;
        FieldInfo field = storage.GetField("Qualnames", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.False(field.IsInitOnly);
        Assert.Equal(NullabilityState.Nullable, new NullabilityInfoContext().Create(field).ReadState);
        PyFunction function = Function(Signature());
        Assert.Null(ColdRecord("Qualnames", function));
        Assert.Throws<PyNotImplementedError>(() => PyObject.GetAttr(function, "__qualname__"));
        Assert.Null(ColdRecord("Qualnames", function));
    }

    [Fact]
    public void DefinitionAbiMatchesCodegenReferenceExactly()
    {
        MethodInfo method = Assert.Single(typeof(PyOps).GetMethods(BindingFlags.Public | BindingFlags.Static),
            candidate => candidate.Name == "FunctionDefinition");
        Assert.Equal(typeof(void), method.ReturnType);
        Assert.False(method.IsGenericMethod);
        Assert.Equal(new[] { typeof(object), typeof(string), typeof(long) },
            method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
    }

    [Fact]
    public void FunctionInstanceLayoutDoesNotGrowForColdDefinitionMetadata()
    {
        string[] expected = ["<Name>k__BackingField", "<Signature>k__BackingField", "<Body>k__BackingField",
            "<Defaults>k__BackingField", "<UsesSuper>k__BackingField", "<Env>k__BackingField",
            "_doc", "_annotations", "_attributes", "_module"];
        string[] actual = typeof(PyFunction).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(field => field.Name).ToArray();
        Assert.Equal(expected.OrderBy(name => name), actual.OrderBy(name => name));
    }
}
