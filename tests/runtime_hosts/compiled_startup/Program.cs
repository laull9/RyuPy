// 独立进程宿主合同：基础启动不登记inspect，旧公共入口仍完整，升级/重复调用保留已导入状态。
using System;
using System.Collections.Generic;
using RyuPy.Runtime;

static class Program
{
    static void Require(bool value) { if (!value) throw new Exception("startup contract failed"); }
    static void Main(string[] args)
    {
        var observer = args[0].StartsWith("order-") ? new Observer() : null;
        if (observer is not null) PyProvider.Install(observer);
        if (args[0] is "legacy-initialize" or "order-initialize") PySystem.Initialize("host.py", []);
        else if (args[0] is "legacy-register" or "order-register") PyNativeModules.Register();
        else
        {
            PySystem.InitializeCompiledBase("host.py", ["first"]);
            bool missing = false;
            try { PyModules.Import("inspect"); }
            catch (PyModuleNotFoundError) { missing = true; }
            Require(missing);
            if (args[0] == "initialize") PySystem.Initialize("host.py", ["upgrade"]);
            else PyNativeModules.Register();
        }
        if (observer is not null)
        {
            int index = observer.Names.IndexOf("inspect");
            Require(index > 0 && observer.Names[index - 1] == "functools" && observer.Names[index + 1] == "operator");
        }
        var inspect = PyModules.Import("inspect");
        var signature = inspect.GetAttr("signature");
        inspect.SetAttr("marker", 42L);
        PySystem.InitializeCompiledBase("host.py", []);
        PyNativeModules.Register();
        PySystem.Initialize("host.py", []);
        Require(ReferenceEquals(inspect, PyModules.Import("inspect")));
        Require(ReferenceEquals(signature, inspect.GetAttr("signature")));
        Require(Equals(inspect.GetAttr("marker"), 42L));
        if (observer is not null)
        {
            Require(observer.Names.FindAll(name => name == "inspect").Count == 1);
            PyProvider.Remove(observer);
        }
        Console.WriteLine("startup-upgrade-ok");
    }

    // 登记期间重入旧API也必须保原短路，不能提前插入inspect或重排后续通知。
    sealed class Observer : IPyModuleProvider
    {
        internal readonly List<string> Names = new();
        public bool Owns(string name) => false;
        public object? SystemAttribute(string name) => null;
        public PyModule Import(string name) => throw new PyModuleNotFoundError(name);
        public void Registered(string name) { Names.Add(name); if (name == "io") PyNativeModules.Register(); }
        public void Initializing(PyModule module) { }
        public void Failed(string name) { }
    }
}
