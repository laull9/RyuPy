// 冷态模块公开ABI门：跨程序集实现协议、opaque token以及原模块返回类型均以真实反射签名检查。
using RyuPy.Runtime;
namespace RyuPy.Runtime.Tests;

public class PyModuleNamespaceAbiTests
{
    private static void Method(Type type, string name, Type result, params Type[] arguments)
    {
        var method = type.GetMethod(name, arguments);
        Assert.NotNull(method); Assert.Equal(result, method.ReturnType); Assert.True(method.IsPublic);
    }
    [Fact]
    public void NeutralInterfacesArePublicAndHaveExactColdSignatures()
    {
        Assert.True(typeof(IPyModuleNamespace).IsPublic); Assert.True(typeof(IPyStrictImportProvider).IsPublic);
        Assert.Equal(typeof(object), typeof(IPyModuleNamespace).GetProperty("ViewObject")!.PropertyType);
        Method(typeof(IPyModuleNamespace), "TryGet", typeof(bool), typeof(string), typeof(object).MakeByRefType());
        Method(typeof(IPyModuleNamespace), "Set", typeof(void), typeof(string), typeof(object));
        Method(typeof(IPyModuleNamespace), "DeleteIfPresent", typeof(bool), typeof(string));
        Method(typeof(IPyStrictImportProvider), "TryGetSysModuleEntry", typeof(bool), typeof(string), typeof(object).MakeByRefType());
        Method(typeof(IPyStrictImportProvider), "TryGetNamespace", typeof(bool), typeof(object), typeof(IPyModuleNamespace).MakeByRefType());
        Method(typeof(IPyStrictImportProvider), "TryGetBuiltinsNamespace", typeof(bool), typeof(object), typeof(IPyModuleNamespace).MakeByRefType());
        Method(typeof(IPyStrictImportProvider), "ImportLevel", typeof(object), typeof(string), typeof(object), typeof(object[]), typeof(long));
        Method(typeof(IPyStrictImportProvider), "BindExistingMainModule", typeof(PyModule), typeof(object));
    }
    [Fact]
    public void NewOpaqueTransactionsKeepExistingExecuteAndImportAbi()
    {
        Method(typeof(PyModuleInstances), "CaptureRegisteredCode", typeof(object), typeof(string));
        Method(typeof(PyModuleInstances), "BindForeign", typeof(PyModule), typeof(object), typeof(IPyModuleNamespace), typeof(IPyForeignObject));
        Method(typeof(PyModuleInstances), "CaptureBinding", typeof(object), typeof(PyModule));
        Method(typeof(PyModuleInstances), "RestoreForeign", typeof(PyModule), typeof(object), typeof(IPyForeignObject));
        Method(typeof(PyModuleInstances), "ExecuteBound", typeof(void), typeof(PyModule));
        Method(typeof(PyModuleInstances), "Execute", typeof(void), typeof(PyModule));
        Method(typeof(PyModules), "Import", typeof(PyModule), typeof(string));
        Method(typeof(PyModules), "ImportLevel", typeof(object), typeof(string), typeof(object), typeof(object[]), typeof(long));
        Assert.False(typeof(PyModuleInstances).Assembly.GetType("RyuPy.Runtime.PyModuleInstanceState")!.IsPublic);
        Assert.False(typeof(PyModuleInstances).Assembly.GetType("RyuPy.Runtime.PyModuleExecutionContext")!.IsPublic);
    }
}
