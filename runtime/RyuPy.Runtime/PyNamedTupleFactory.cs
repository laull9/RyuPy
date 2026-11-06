// 内部 namedtuple 工厂：按 Python 顺序绑定、转换、验证与消费，再取最内真实 owner 来源。
// 完整 helpers 闭合以前不向 collections 注册；不编译字符串、不按应用名称特判。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static class PyNamedTupleFactory
{
    private static readonly PySignature Signature = PySignature.Create("namedtuple",
        ["typename", "field_names", "rename", "defaults", "module"],
        ["pos", "pos", "kwonly", "kwonly", "kwonly"], ["0", "0", "1", "1", "1"]);
    private static readonly object?[] Defaults = [null, null, false, PyNone.Instance, PyNone.Instance];
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "False", "None", "True", "and", "as", "assert", "async", "await", "break", "class", "continue",
        "def", "del", "elif", "else", "except", "finally", "for", "from", "global", "if", "import", "in",
        "is", "lambda", "nonlocal", "not", "or", "pass", "raise", "return", "try", "while", "with", "yield",
    };

    internal static PyClass Invoke(object?[] args, string[] names, object?[] values)
    {
        object?[] bound = PyNamedTupleBinding.Bind(Signature, args, names, values, Defaults);
        object? fieldSource = bound[1];
        if (PyStringLayout.TryGet(fieldSource, out _))
        {
            // replace 可以返回自定义对象，其 split 必须再次动态查询；不可先剥成 CLR string。
            object? replaced = PyOps.CallMethod(fieldSource, "replace", [",", " "], [], []);
            fieldSource = PyOps.CallMethod(replaced, "split", [], [], []);
        }
        var fields = new List<object>();
        IPyIterator iterator = PyNamedTupleSequence.Start(fieldSource);
        while (PyNamedTupleSequence.Next(iterator, out object? field)) fields.Add(PyRepr.StrObject(field));
        object typename = PyRepr.StrObject(bound[0]);
        if (typename is not string typeName)
            throw PyNamedTupleLayout.Error(new PyTypeError($"can't intern {PyRepr.TypeName(typename)}"));
        // 保留 CP intern 的精确类型门与时序，但不进入 CLR 永久 intern 表。
        // 名称只随本 schema 存活；跨生成类的字符串 is 差异已明确登记。
        if (PyOps.Truthy(bound[2]))
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < fields.Count; index++)
            {
                if (fields[index] is not string name)
                    throw PyNamedTupleLayout.Unsupported("namedtuple rename validation of converted str subclasses is not supported");
                if (!Identifier(name) || Keywords.Contains(name) || name.StartsWith('_') || seen.Contains(name))
                    fields[index] = "_" + index;
                seen.Add(name); // 原名而不是改名后的字段参与后续重复判断。
            }
        }
        ValidateName(typeName);
        foreach (object field in fields) ValidateName(field);
        var unique = new HashSet<string>(StringComparer.Ordinal);
        foreach (string field in fields)
        {
            // CP 会在每个最终下划线字段再次判断 rename；状态性真值不能提前缓存。
            if (field.StartsWith('_') && !PyOps.Truthy(bound[2]))
                throw PyNamedTupleLayout.Error(new PyValueError($"Field names cannot start with an underscore: {PyRepr.Repr(field)}"));
            if (!unique.Add(field))
                throw PyNamedTupleLayout.Error(new PyValueError($"Encountered duplicate field name: {PyRepr.Repr(field)}"));
        }
        PyTuple? defaults = bound[3] is null or PyNone ? null : PyNamedTupleSequence.Materialize(bound[3]);
        if (defaults is not null && defaults.Length > fields.Count)
            throw PyNamedTupleLayout.Error(new PyTypeError("Got more default values than field names"));
        var fieldNames = new string[fields.Count];
        for (int index = 0; index < fields.Count; index++)
        {
            string field = (string)fields[index];
            // 验证后字段互异；只有同值 typename 需要在本 schema 内复用，不建全局缓存。
            fieldNames[index] = string.Equals(field, typeName, StringComparison.Ordinal) ? typeName : field;
        }
        PyClass cls = PyNamedTupleLayout.CreateClassWithModule(typeName, fieldNames, defaults?.Items ?? [], PyNone.Instance);
        // 保存已物化 tuple 的稳定身份，包括显式空 tuple；None 与空 defaults 不混淆。
        cls.NamedTupleLayout!.PublicDefaults = (object?)defaults ?? PyNone.Instance;
        object? module = bound[4];
        if (module is null or PyNone)
        {
            object raw = PyTraceback.GetCallerModule();
            module = PyOps.Truthy(raw) ? raw : "__main__";
        }
        cls.Attributes["__module__"] = module;
        return cls;
    }

    // typed class的name/fields已经是exact strings；复用相同校验，保全体名称先于下划线/重复。
    internal static void ValidateExactFields(string name, string[] fields)
    {
        ValidateName(name);
        foreach (string field in fields) ValidateName(field);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < fields.Length; index++)
        {
            string field = fields[index];
            if (field.StartsWith('_'))
                throw PyNamedTupleLayout.Error(new PyValueError($"Field names cannot start with an underscore: {PyRepr.Repr(field)}"));
            if (!seen.Add(field))
                throw PyNamedTupleLayout.Error(new PyValueError($"Encountered duplicate field name: {PyRepr.Repr(field)}"));
            if (string.Equals(field, name, StringComparison.Ordinal)) fields[index] = name;
        }
    }

    private static void ValidateName(object value)
    {
        if (value is not string name)
            throw PyNamedTupleLayout.Error(new PyTypeError("Type names and field names must be strings"));
        if (!Identifier(name))
            throw PyNamedTupleLayout.Error(new PyValueError($"Type names and field names must be valid identifiers: {PyRepr.Repr(name)}"));
        if (Keywords.Contains(name))
            throw PyNamedTupleLayout.Error(new PyValueError($"Type names and field names cannot be a keyword: {PyRepr.Repr(name)}"));
    }

    private static bool Identifier(string name)
    {
        // Unicode/NFKC 的公开字段名与编译参数名双映射尚未实现，不能误报无效标识符。
        foreach (char character in name)
            if (character > 127) throw PyNamedTupleLayout.Unsupported("non-ASCII namedtuple identifiers are not supported");
        if (name.Length == 0 || !(name[0] == '_' || char.IsAsciiLetter(name[0]))) return false;
        foreach (char character in name)
            if (!(character == '_' || char.IsAsciiLetterOrDigit(character))) return false;
        return true;
    }
}
