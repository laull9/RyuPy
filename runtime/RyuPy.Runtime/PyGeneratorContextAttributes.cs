// 生成器上下文的公开状态与绑定方法：实例字典保存真实协议状态，方法共享定义并固定接收者。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

public sealed partial class PyGeneratorContext
{
    private PyDict _attributes = new();
    private static readonly PyFunction EnterMethodFunction = Method("_GeneratorContextManager", "__enter__", ["self"],
        a => Receiver(a[0], "args").Enter());
    private static readonly PyFunction ExitMethodFunction = Method("_GeneratorContextManager", "__exit__", ["self", "typ", "value", "traceback"],
        a => Receiver(a[0], "gen").Exit(a[1], a[2]));
    private static readonly PyFunction CallMethodFunction = Method("ContextDecorator", "__call__", ["self", "func"],
        a => Receiver(a[0], "_recreate_cm").Decorate(a[1]));
    private static readonly PyFunction RecreateMethodFunction = Method("_GeneratorContextManagerBase", "_recreate_cm", ["self"],
        a => Receiver(a[0], "func").Recreate());

    /// <inheritdoc/>
    public override object? GetAttr(string name)
    {
        if (name == "__dict__") return _attributes;
        if (_attributes.TryGet(name, out object? value)) return value;
        return GetTypeMethod(name);
    }

    // 类型槽单独解析：隐式协议不会读取公开可写的实例字典。
    internal object? GetTypeMethod(string name) => name switch
        {
            "__class__" => PyType.Get(TypeName),
            "__enter__" => new BoundMethod(this, EnterMethodFunction),
            "__exit__" => new BoundMethod(this, ExitMethodFunction),
            "__call__" => new BoundMethod(this, CallMethodFunction),
            "_recreate_cm" => new BoundMethod(this, RecreateMethodFunction),
            _ => throw Missing(name),
        };

    /// <inheritdoc/>
    public override void SetAttr(string name, object? value)
    {
        if (name == "__dict__")
            _attributes = value as PyDict ?? throw new PyTypeError("__dict__ must be set to a dictionary");
        else _attributes.Set(name, value);
    }

    /// <inheritdoc/>
    public override void DelAttr(string name)
    {
        if (!_attributes.TryGet(name, out _)) throw Missing(name);
        _attributes.Delete(name);
    }

    private static PyGeneratorContext Receiver(object? value, string member)
    {
        if (value is PyGeneratorContext context) return context;
        // 直接调用__func__的错误接收者不能泄漏CLR强转异常；普通缺属性保留Python错误。
        PyObject.GetAttr(value, member);
        throw new PyNotImplementedError("unbound context methods on custom receivers are not implemented");
    }

    private PyGeneratorContext Recreate()
    {
        object? function = GetAttr("func");
        // 重建复用调用层的*args/**kwds展开，保持现有字典/映射错误边界。
        var (args, names, values) = PyOps.FlattenArgs([PyOps.StarArg(GetAttr("args"))], [null], [GetAttr("kwds")]);
        return new PyGeneratorContext(function, args, names, values);
    }

    private static PyFunction Method(string owner, string name, string[] parameters, Func<object?[], object?> body)
    {
        var kinds = new string[parameters.Length]; Array.Fill(kinds, "pos");
        var flags = new string[parameters.Length]; Array.Fill(flags, "0");
        var signature = PySignature.Create(owner + "." + name, parameters, kinds, flags);
        var function = new PyFunction(name, signature, (_, args, names, values) =>
        {
            object?[] bound;
            try
            {
                // 不执行方法体，仅先校验完整位置槽上的关键字；混合错误遵循CPython优先级。
                if (args.Length > parameters.Length) PyBinder.Bind(signature, args[..parameters.Length], names, values, []);
                bound = PyBinder.Bind(signature, args, names, values, []);
            }
            catch (PyTypeError error)
            {
                // 通用绑定器保留旧消息；这里只校准Python函数多缺参的英文并列格式。
                var missing = new List<string>();
                for (int i = args.Length; i < parameters.Length; i++)
                    if (Array.IndexOf(names, parameters[i]) < 0) missing.Add(parameters[i]);
                string old = $"{signature.Name}() missing {missing.Count} required positional arguments: '{string.Join("', '", missing)}'";
                if (missing.Count >= 2 && error.PyMessage == old)
                {
                    string listed = missing.Count == 2 ? $"'{missing[0]}' and '{missing[1]}'"
                        : "'" + string.Join("', '", missing.GetRange(0, missing.Count - 1)) + "', and '" + missing[^1] + "'";
                    throw new PyTypeError($"{signature.Name}() missing {missing.Count} required positional arguments: {listed}");
                }
                throw;
            }
            return body(bound) ?? PyNone.Instance;
        }, [], false);
        PyObject.SetAttr(function, "__qualname__", owner + "." + name);
        PyObject.SetAttr(function, "__module__", "contextlib");
        return function;
    }

    private sealed class BoundMethod(PyGeneratorContext receiver, PyFunction function) : PyNativeObject, IPyNativeCallable
    {
        private readonly PyGeneratorContext _receiver = receiver;
        private readonly PyFunction _function = function;
        public override string TypeName => "method";
        public override object? GetAttr(string name) => name switch
        {
            "__self__" => _receiver, "__func__" => _function, "__class__" => PyType.Get("method"),
            // 保存后再查找同一方法，仍按接收者/函数身份相等；可作为字典键，不依赖包装分配身份。
            "__eq__" => PyNativeFunctions.Create(name, ["other"], [null], 1,
                a => a[0] is BoundMethod other ? ReferenceEquals(_receiver, other._receiver)
                    && ReferenceEquals(_function, other._function) : PyNotImplemented.Instance),
            "__hash__" => PyNativeFunctions.Create(name, [], [], 0, _ => PyHash.Of(_receiver) ^ PyHash.Of(_function)),
            _ => PyObject.GetAttr(_function, name),
        };
        public object? Invoke(object?[] args, string[] names, object?[] values) =>
            _function.Invoke([_receiver, .. args], names, values);
    }
}
