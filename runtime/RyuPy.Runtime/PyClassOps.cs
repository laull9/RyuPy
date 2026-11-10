// 类创建及属性操作的公开 ABI；具体类布局仍由 PyClass 与各专用 partial 管理。
namespace RyuPy.Runtime;

/// <summary>类与属性协议的运行时入口（codegen 只依赖这几个方法）。</summary>
public static partial class PyOps
{
    /// <summary>创建类对象。</summary>
    /// <param name="name">类名。</param>
    /// <param name="bases">基类列表（0 或 1 个）。</param>
    /// <param name="factory">用户异常类的 CLR 工厂；普通类为 <c>null</c>。</param>
    public static object MakeClass(string name, object?[] bases, object? factory) =>
        MakeClassCore(name, bases, factory, captureAmbientMetadata: true);

    // 实例类使用相同真实布局构造，但元数据由显式class frame初始化，不借ambient来源。
    internal static object MakeClassCore(string name, object?[] bases, object? factory, bool captureAmbientMetadata)
    {
        object? baseValue = bases.Length == 1 ? bases[0] : null;
        PyTuple? originalBases = null;
        if (bases.Length == 1) baseValue = PyTypingGenericClass.ResolveBase(baseValue, out originalBases);
        if (bases.Length == 1 && factory is null && PyTypingNamedTuple.IsRoot(bases[0]))
            return PyTypingNamedTuple.BeginDefinition(name, bases[0]);
        if (ReferenceEquals(baseValue, PyType.Get("SyntaxError")) || ReferenceEquals(baseValue, PyType.Get("ParseError")))
            throw PyXmlTreeBuilderErrors.Unsupported("SyntaxError/ParseError user inheritance");
        var cls = new PyClass(name);
        if (bases.Length == 1 && ReferenceEquals(bases[0], PyMappingProxy.Type))
            throw PyMappingProxy.Error(new PyTypeError("type 'mappingproxy' is not an acceptable base type"));
        if (bases.Length == 1 && ReferenceEquals(bases[0], PySlice.Type))
            throw PySlice.PublicError(new PyTypeError("type 'slice' is not an acceptable base type"));
        if (bases.Length > 1)
        {
            throw new PyTypeError("多重继承暂不支持");
        }
        if (bases.Length == 1)
        {
            switch (baseValue)
            {
                case PyNativeType native when ReferenceEquals(native, PyOrderedDict.Type):
                    throw PyOrderedDict.Unsupported("inheritance");
                case PyNativeType native when ReferenceEquals(native, PyLexicalSuper.Type):
                    throw new PyNotImplementedError("super type inheritance is not supported");
                case PyClass baseClass:
                    PyNamedTupleLayout.GuardBase(baseClass);
                    baseClass.RequireTypingIoReady();
                    baseClass.RequireTypingGenericReady();
                    if (baseClass.HasEnumMetadata)
                        throw new PyNotImplementedError("dynamic Enum inheritance requires a compiled Enum factory");
                    cls.BaseClass = baseClass;
                    if (baseClass.IsTypingGenericFamily)
                        cls.GenericDefinition = new PyTypingGenericDefinition(originalBases);
                    cls.HasStringLayout = baseClass.HasStringLayout;
                    cls.HasListLayout = baseClass.HasListLayout;
                    cls.HasDictLayout = baseClass.HasDictLayout;
                    if (baseClass.HasDictLayout) baseClass.RequireDictReady();
                    if (baseClass.IsTypingIoFamily) cls.TypingIoKind = PyTypingIoKind.Derived;
                    break;
                case PyType type when ReferenceEquals(type, PyType.Get("str")):
                    cls.HasStringLayout = true;
                    break;
                case PyType type when ReferenceEquals(type, PyType.Get("list")):
                    cls.HasListLayout = true;
                    break;
                case PyType type when ReferenceEquals(type, PyType.Get("dict")):
                    cls.HasDictLayout = true;
                    break;
                case PyType type when type.Name == "object":
                    break;
                case PyType type:
                    cls.BuiltinBase = type.Name;
                    break;
                default:
                    throw new PyTypeError(
                        $"class base must be a class, not '{PyRepr.TypeName(bases[0])}'"
                    );
            }
        }
        cls.ExceptionFactory = factory as PyExceptionFactory;
        // 装饰器可能把普通基类替换成异常类，但编译器尚未为该派生类生成 CLR 异常布局。
        // 不能创建看似异常、实际只是 PyInstance 的对象，必须在类定义处明确拒绝。
        if (cls.IsSubclassOfBuiltin("BaseException") && cls.ExceptionFactory is null)
            throw new PyTypeError("dynamic exception inheritance requires a compiled exception factory");
        if (captureAmbientMetadata) PyClassNamespaceMetadata.Capture(cls);
        return cls;
    }

    /// <summary><c>obj.attr = value</c>。</summary>
    public static void SetAttr(object? obj, string name, object? value) =>
        PyObject.SetAttr(obj, name, value);

    /// <summary><c>del obj.attr</c>。</summary>
    public static void DelAttr(object? obj, string name) => PyObject.DelAttr(obj, name);

    /// <summary>应用类内修饰器：1 = property，2 = staticmethod，3 = classmethod。</summary>
    public static object Decorate(int kind, object? value) => kind switch
    {
        1 => new PyProperty(value),
        2 => new PyStaticMethod(value),
        3 => new PyClassMethod(value),
        _ => throw new PyRuntimeError($"unknown decorator kind {kind}"),
    };
}
