// dedent首次导入的有限来源见证：只接受实际登记的native compile，不试跑用户代理。
using System;

namespace RyuPy.Runtime;

internal sealed class PyTextDedentCompile
{
    private readonly PyFunction function;
    private readonly PyCall body;
    private readonly PySignature signature;
    private readonly string[] names;
    private readonly PyParamKind[] kinds;
    private readonly bool[] hasDefault;
    private readonly object?[] defaults;
    private readonly string[] originalNames;
    private readonly PyParamKind[] originalKinds;
    private readonly bool[] originalHasDefault;
    private readonly object?[] originalDefaults;
    private static readonly string[] PrivateBackends =
        ["_compile", "_compiler", "_parser", "_cache", "_cache2", "_MAXCACHE", "_MAXCACHE2", "_compile_template"];

    // 必须在实际re字典登记时捕获；数组内容可由CLR宿主改变，不能只守readonly引用。
    internal PyTextDedentCompile(PyFunction function)
    {
        this.function = function; body = function.Body; signature = function.Signature;
        names = signature.Names; kinds = signature.Kinds; hasDefault = signature.HasDefault;
        defaults = function.Defaults;
        originalNames = (string[])names.Clone(); originalKinds = (PyParamKind[])kinds.Clone();
        originalHasDefault = (bool[])hasDefault.Clone(); originalDefaults = (object?[])defaults.Clone();
    }

    internal bool Supports(PyModule re)
    {
        // 源码成功导入不应反向收窄旧API；此分支不读取源码compile或私有后端。
        if (!re.IsNativeRegistration) return false;
        if (!ReferenceEquals(re.GetAttr("compile"), function)
            || re.GetAttr("MULTILINE") is not long flags || flags != 8L
            || !ReferenceEquals(function.Body, body) || !ReferenceEquals(function.Signature, signature)
            || !ReferenceEquals(signature.Names, names) || !ReferenceEquals(signature.Kinds, kinds)
            || !ReferenceEquals(signature.HasDefault, hasDefault) || !ReferenceEquals(function.Defaults, defaults))
            return false;
        if (names.Length != originalNames.Length || kinds.Length != originalKinds.Length
            || hasDefault.Length != originalHasDefault.Length || defaults.Length != originalDefaults.Length)
            return false;
        for (int index = 0; index < names.Length; index++)
            if (!string.Equals(names[index], originalNames[index], StringComparison.Ordinal)) return false;
        for (int index = 0; index < kinds.Length; index++)
            if (kinds[index] != originalKinds[index]) return false;
        for (int index = 0; index < hasDefault.Length; index++)
            if (hasDefault[index] != originalHasDefault[index]) return false;
        for (int index = 0; index < defaults.Length; index++)
            if (!ReferenceEquals(defaults[index], originalDefaults[index])) return false;
        foreach (string name in PrivateBackends)
        {
            // native re原来没有这些键；挂载任何值均是新API的明确负门，不调用该值。
            try { re.GetAttr(name); return false; }
            catch (PyAttributeError) { }
        }
        return true;
    }

    internal PyRegexPattern Compile(string pattern) =>
        (PyRegexPattern)PyOps.CallValue(function, [pattern, 8L], [], [])!;
}
