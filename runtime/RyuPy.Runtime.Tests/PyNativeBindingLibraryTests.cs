// 仅验证冷登记及纯路径；不生成动态程序集、不编译或执行 C，不替代真实三平台调用。
using System;
using System.IO;
using System.Reflection;
using RyuPy.Runtime;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public class PyNativeBindingLibraryTests
{
    [Fact]
    public void PurePathUsesExplicitAbsoluteBaseAndKeepsUnicodeHostDirectories()
    {
        string root = Path.GetFullPath("中文 & native fixture");
        string relative = "_internal/native/v1/demo/libprobe.so";
        Assert.Equal(Path.Combine(root, "_internal", "native", "v1", "demo", "libprobe.so"),
            PyNativeBindingLibrary.ResolveForBaseDirectory(root, relative));
        Assert.Throws<PyRuntimeError>(() => PyNativeBindingLibrary.ResolveForBaseDirectory("relative", relative));
    }

    [Fact]
    public void UnsafeRelativePathsFailWithoutTouchingFileSystem()
    {
        string root = Path.GetFullPath("not-created-native-fixture");
        foreach (string value in new[] { "/absolute", "_internal/native/v1/", "_internal/native/v1/../x",
            "_internal/native/v1/a//x", "_internal/native/v1/C:x", "_internal/native/v1/a\\x",
            "_internal/native/v1/CON.dll", "_internal/native/v1/LPT9.x", "_internal/native/v1/中文.so",
            "_internal/native/v1/x.", "_internal/native/v1/x ", "_internal/native/v1/a\0b",
            "_internal/native/v1/" + new string('a', 256) })
            Assert.Throws<PyRuntimeError>(() => PyNativeBindingLibrary.ResolveForBaseDirectory(root, value));
    }

    [Fact]
    public void ActualAssemblyRegistersWithoutOpeningMissingLibraryAndFailsClosed()
    {
        Assembly owner = typeof(PyNativeBindingLibraryTests).Assembly;
        const string name = "ryupy_binding_unit_fixture";
        const string relative = "_internal/native/v1/unit_fixture/never_deployed_61280567.so";
        Assert.Throws<PyRuntimeError>(() => PyNativeBindingLibrary.Register(null!, name, relative));
        Assert.Throws<PyRuntimeError>(() => PyNativeBindingLibrary.Register(owner, "../bad", relative));
        Assert.Throws<PyRuntimeError>(() => PyNativeBindingLibrary.Register(owner, name, "../bad"));
        // 上述失败未占用 CLR resolver 槽；真实登记不要求文件存在。
        PyNativeBindingLibrary.Register(owner, name, relative);
        PyNativeBindingLibrary.Register(owner, name, relative);
        Assert.Throws<PyRuntimeError>(() => PyNativeBindingLibrary.Register(owner, name + "_other", relative));
        Assert.Throws<PyRuntimeError>(() => PyNativeBindingLibrary.Register(owner, name, relative + "_other"));

        // 仅测试反射读取私有状态；生产无反射路径或动态 ALC。
        object table = typeof(PyNativeBindingLibrary).GetField("Libraries", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        object?[] lookup = { owner, null };
        Assert.True((bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table, lookup)!);
        object state = lookup[1]!;
        MethodInfo resolve = state.GetType().GetMethod("Resolve", BindingFlags.Instance | BindingFlags.NonPublic)!;
        void Missing(string requested, Assembly actualOwner)
        {
            var error = Assert.Throws<TargetInvocationException>(() => resolve.Invoke(state, new object?[] { requested, actualOwner, null }));
            Assert.IsType<DllNotFoundException>(error.InnerException);
        }
        Missing("undeclared", owner);
        Missing(name, typeof(PyNativeBindingLibrary).Assembly);
        Missing(name, owner);
        Missing(name, owner);
        Assert.Equal(IntPtr.Zero, state.GetType().GetField("_handle", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(state));
    }
}
