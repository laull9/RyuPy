// NamedTuple功能式冷工厂：真实posonly/kwargs绑定、caller早快照及两遍fields消费。
// 不借类Finish、不物化输入来回放、不创建假typing帧；最终metadata准备完才提交真实tuple类。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static class PyTypingNamedTupleFunctional
{
    internal static object Invoke(PyFunction self, object?[] args, string[] names, object?[] values)
    {
        // 本API冷绑定器兼容此posonly+varkw签名；只校准自己的参数错误，不包用户正文。
        object?[] bound = PyNamedTupleBinding.Bind(self.Signature, args, names, values, self.Defaults);
        var keywords = (PyDict)bound[2]!;
        object? fields = bound[1];
        if (fields is null or PyNone) fields = new PyDictView(keywords, PyDictViewKind.Items);
        else if (keywords.Count != 0)
            throw PyNamedTupleLayout.Error(new PyTypeError("Either list of fields or keywords can be provided to NamedTuple, not both"));

        // CP先捕获_make_nmtuple，再求module=_caller()，两者均早于首次iter(fields)。
        PyTypingNamedTuple.RejectTypingOverride("_make_nmtuple");
        object module = CallerModule();
        var rawFields = new List<object?>();
        IPyIterator iterator = PyNamedTupleSequence.Start(fields);
        while (PyNamedTupleSequence.Next(iterator, out object? pair))
            rawFields.Add(UnpackPair(pair).Items[0]);

        // 第二遍必须重新取得同一个源的iterator；一次性源耗尽后annotations可以真实为空。
        var annotations = new PyDict();
        iterator = PyNamedTupleSequence.Start(fields);
        while (PyNamedTupleSequence.Next(iterator, out object? pair))
        {
            PyTuple parts = UnpackPair(pair);
            // f-string字段消息先于_type_check；未知__format__协议在其消费点明确拒绝。
            if (parts.Items[0] is not string field)
                throw PyNamedTupleLayout.Unsupported("NamedTuple functional annotation keys currently require exact strings");
            object annotation = PyTypingNamedTupleAnnotations.NormalizeAnnotation(parts.Items[1], field);
            annotations.Set(field, annotation);
        }

        PyTypingNamedTuple.GuardActualFactory();
        // 原factory先逐项str(fields)，再str(typename)。仅此处缩窄多态str域，不提前吞pair/type错误。
        var fieldNames = new string[rawFields.Count];
        for (int index = 0; index < rawFields.Count; index++)
            fieldNames[index] = rawFields[index] as string
                ?? throw PyNamedTupleLayout.Unsupported("NamedTuple functional field str conversion is not supported");
        if (bound[0] is not string typeName)
            throw PyNamedTupleLayout.Unsupported("NamedTuple functional typename str conversion is not supported");
        PyNamedTupleFactory.ValidateExactFields(typeName, fieldNames);

        var cls = new PyClass(typeName);
        var defaults = new PyTuple([]);
        PyNamedTupleLayout.PreparedClass prepared = PyNamedTupleLayout.PrepareClass(cls, fieldNames, [], module);
        PyTypingNamedTuple.ApplyTypedMetadata(prepared, annotations, defaults);
        // 这是functional末尾的当前global，不是类语法的原base或入口捕获的Root。
        if (!PyTyping.CreateAttributes().TryGetValue("NamedTuple", out object? currentRoot))
            throw PyNamedTupleLayout.Error(new PyNameError("name 'NamedTuple' is not defined"));
        prepared.Attributes["__orig_bases__"] = new PyTuple([currentRoot]);
        cls.CommitNamedTuple(prepared);
        return cls;
    }

    private static object CallerModule()
    {
        PyTypingNamedTuple.RejectTypingOverride("_caller");
        PyTypingNamedTuple.RejectTypingOverride("sys");
        // sys未初始化时不为来源新导入模块；已缓存真实sys的新增替换也不能被内部来源快路忽略。
        if (PyModules.TryCached("sys", out PyModule? sys))
        {
            bool replaced = false;
            try { _ = sys!.GetAttr("_getframemodulename"); replaced = true; }
            catch (PyAttributeError) { }
            if (replaced)
                throw PyNamedTupleLayout.Unsupported("NamedTuple caller with replaced sys._getframemodulename is not supported");
        }
        object raw = PyTraceback.GetCallerModule();
        return PyOps.Truthy(raw) ? raw : "__main__";
    }

    private static PyTuple UnpackPair(object? pair)
    {
        // 明确限定无用户特殊槽的载体，不把所有IPyIterator视为安全（map/filter会执行用户回调）。
        // set游标已拆为独立类，但仍无用户回调；只补原白名单，不放开任意IPyIterator。
        // tuple派生留待单独证明override合同。
        if (pair is PyList or string or PyIter or PySetIterator || pair is PyTuple && pair.GetType() == typeof(PyTuple))
            return (PyTuple)PyOps.UnpackSequence(pair, 2);
        // 非iterable的CP unpack消息与通用iter错误不同；此阶段不使用近似错误冒兼容。
        throw PyNamedTupleLayout.Unsupported("NamedTuple field pairs currently require an exact tuple, list, str, or builtin iterator");
    }
}
