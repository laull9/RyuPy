// 集合方法的统一调用分派；生成tuple先捕获属性，再进入实参展开，普通集合保原路径。
namespace RyuPy.Runtime;

public static partial class PyOps
{
    // ───────────────────────── 方法调用 ─────────────────────────

    /// <summary>方法调用：<c>obj.name(args)</c>。</summary>
    /// <param name="obj">接收者。</param>
    /// <param name="name">方法名。</param>
    /// <param name="rawArgs">位置实参（可能含 <c>*iterable</c> 标记）。</param>
    /// <param name="rawNames">关键字名；值为 <c>None</c> 表示 <c>**mapping</c>。</param>
    /// <param name="rawValues">关键字值。</param>
    public static object? CallMethod(
        object? obj,
        string name,
        object?[] rawArgs,
        object?[] rawNames,
        object?[] rawValues
    )
    {
        // 公共runtime调用已取得参数值，仍须先读super属性，再展开其star参数。
        if (obj is PySuper)
            return CallValue(CaptureMethodTarget(obj, name), rawArgs, rawNames, rawValues);
        // 生成类的字段可遮蔽tuple方法；缺属性须先于star展开失败，不能先FlattenArgs。
        // 精确list保留原FlattenArgs与checkedmethod路径，不探测派生布局冷分支。
        if (obj is not PyList && (obj is PyListInstance or PyClass { HasListLayout: true } or PySuper { Owner.HasListLayout: true }
            || obj is PyType listType && ReferenceEquals(listType, PyType.Get("list"))))
            return CallValue(PyObject.GetAttr(obj, name), rawArgs, rawNames, rawValues);
        if (obj is not PyDict && (obj is PyDictInstance or PyClass { HasDictLayout: true } or PySuper { Owner.HasDictLayout: true }
            || obj is PyType dictType && ReferenceEquals(dictType, PyType.Get("dict"))))
            return CallValue(PyObject.GetAttr(obj, name), rawArgs, rawNames, rawValues);
        // wrapper的真实__func__也须先捕获；缺属性不能被后续*实参展开错误抢先覆盖。
        if (obj is PySlice or PyStaticMethod or PyClassMethod || ReferenceEquals(obj, PySlice.Type))
            return CallValue(PyObject.GetAttr(obj, name), rawArgs, rawNames, rawValues);
        if (obj is PyNamedTupleInstance || obj is PyClass { NamedTupleLayout: not null })
            return CallValue(CaptureModuleTarget(obj, name), rawArgs, rawNames, rawValues);
        var (args, kwNames, kwValues) = FlattenArgs(rawArgs, rawNames, rawValues);
        if (obj is PyBytes or PyByteArray && name == "decode")
            return PyBinaryDecode.Invoke(obj, args, kwNames, kwValues);
        if (obj is PyBytes or PyByteArray && name == "translate")
            return PyBinaryTranslate.Invoke(obj, args, kwNames, kwValues);
        // 精确二进制分类不创建bound包装；专用检查保kwargs先arity及类型限定文案。
        if (obj is PyBytes or PyByteArray && PyBinaryClassification.Has(name))
            return PyBinaryClassification.Invoke(obj, name, args.Length, kwNames.Length);
        // 普通实例 / 类 / super 代理及本轮GetoptError：按真实属性读取，不按方法名伪造槽。
        // GetoptError只扩冷分派；其保存方法保原接收者，实例覆盖属性无需额外self。
        if (obj is PyNativeType or PyNativeObject or PyInstance or PyClass or PySuper or PyModule or PyFunction or PyBoundMethod or PyBuiltinFunction or PyProperty
            || (obj is PyType type && (ReferenceEquals(type, PyBaseObject.Type) || ReferenceEquals(type, PyType.Get("str"))
                || PyBinaryClassificationDescriptors.Has(type, name) || PyBinaryTranslateDescriptors.Has(type, name) || PyBinaryDecodeDescriptors.Has(type, name)))
            || obj is PyGetoptError
            || (obj is PyBaseException exception && exception.PythonClass is not null))
        {
            object? attribute = PyObject.GetAttr(obj, name);
            return CallValue(attribute, args, kwNames, kwValues);
        }
        if (name == "__len__" && IsBuiltinSized(obj))
        {
            ValidatePreparedArity(obj!, name, args.Length);
            return PreparedLength(obj!, name, args, kwNames, kwValues);
        }
        if (obj is string splitText && name == "split") return PyTextSplit.Invoke(splitText, args, kwNames, kwValues);
        if (obj is string spaceText && name is "isspace" or "isalnum") return PyUnicodeWhitespace.Invoke(spaceText, args, kwNames, name);
        // 搜索族在通用keyword拒绝前进入同一绑定器；派生实例仍走上面的真实属性路径。
        if (obj is string searchText && PyTextSearch.Has(name))
            return PyTextSearch.Invoke(searchText, name, args, kwNames);
        // 前后缀的keyword先于arity，精确str不创建绑定对象；保存/根入口共用同一正文。
        if (obj is string affixText && name is "startswith" or "endswith")
            return PyTextAffix.Match(affixText, name, args, kwNames, qualified: true);
        // partition独立绑定保keyword先arity；不为精确str制造绑定对象。
        if (obj is string partitionText && name is "partition" or "rpartition")
            return PyTextPartition.Invoke(partitionText, name, args, kwNames);
        // 仅精确CLR文本快路，不吞派生override/实例遮蔽，也不创建绑定对象。
        if (obj is string expandText && name == "expandtabs")
            return PyTextExpandTabs.Invoke(expandText, args, kwNames, kwValues);
        // 精确字符串保原直接路径，不构造绑定方法；format单独消费全部命名字段。
        if (obj is string template && name == "format")
            return PyStringTemplate.Format(template, args, kwNames, kwValues);
        if (obj is PySet family && HasSetFamilyMethod(name))
            return CallSetFamilyMethod(family, name, args, kwNames);
        if (obj is PyDict updateTarget && name == "update")
            return CallDictUpdate(updateTarget, args, kwNames, kwValues);
        // format已在上方分流；其余集合仅list.sort接收关键字，未知关键字立即报TypeError，
        // 避免“多写了参数却被静默忽略”。
        if (kwNames.Length > 0)
        {
            if (obj is PySet checkedSet && name == "clear")
                return CallSetClear(checkedSet, args, kwNames);
            if (obj is PyList checkedList && name is "clear" or "pop" or "reverse" or "copy")
            {
                return CallListCheckedMethod(checkedList, name, args, kwNames);
            }
            if (obj is PyList list && name == "sort")
            {
                return SortList(list, args, kwNames, kwValues);
            }
            throw new PyTypeError(
                $"'{PyRepr.TypeName(obj)}'.{name}() got an unexpected keyword argument "
                    + $"'{kwNames[0]}'"
            );
        }
        return obj switch
        {
            PyList list => ListMethod(list, name, args),
            PyTuple tuple => TupleMethod(tuple, name, args),
            PyDict dict => DictMethod(dict, name, args),
            PySet set => SetMethod(set, name, args),
            PyByteArray array => ByteArrayMethod(array, name, args),
            string text => StringMethod(text, name, args),
            PyBytes bytes => BytesMethod(bytes, name, args),
            PyGenerator generator => generator.CallMethod(name, args),
            _ => throw new PyAttributeError(
                $"'{PyRepr.TypeName(obj)}' object has no attribute '{name}'"
            ),
        };
    }

}
