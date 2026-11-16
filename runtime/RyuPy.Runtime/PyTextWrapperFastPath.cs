// 默认精确实例的局部快路资格；不调用用户协议，失败只回动态路径，不改变错误时序。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

internal static partial class PyTextWrapper
{
    private sealed class FastPlan
    {
        private readonly PyClass root;
        private readonly Dictionary<string, object?> module;
        private readonly int[] slots;
        private readonly long version;
        private readonly MethodSnapshot[] methods;

        private sealed record MethodSnapshot(string Name, PyFunction Function, PyCall Body,
            object?[] Defaults, object?[] Values);

        internal FastPlan(PyClass root, Dictionary<string, object?> module, int[] slots)
        {
            this.root = root; this.module = module; this.slots = slots;
            version = root.Version;
            var entries = new List<MethodSnapshot>();
            foreach (var (name, value) in root.Attributes)
                if (value is PyFunction function)
                    entries.Add(new(name, function, function.Body, function.Defaults, (object?[])function.Defaults.Clone()));
            methods = entries.ToArray();
        }

        internal bool TryWrap(object? self, object? text, out object? result)
        {
            result = null;
            if (text is not string input || self is not PyInstance instance || self.GetType() != typeof(PyInstance)
                || !ReferenceEquals(instance.Class, root)
                || !module.TryGetValue("TextWrapper", out object? exported) || !ReferenceEquals(exported, root)
                || root.Version != version || root.BaseClass is not null || root.BuiltinBase is not null
                || root.ExceptionFactory is not null || root.HasStringLayout || root.HasEnumMetadata
                || root.DataclassActive || root.IsNativeRuntimeClass || root.NamedTupleLayout is not null)
                return false;
            // Python类写入和新增实例slot会Touch。绕SetAttr直接改宿主Attributes整体不在合同内，
            // 因为原lookup/property缓存也不支持该通道；不借本快路伪称修复host直写。
            foreach (MethodSnapshot snapshot in methods)
            {
                if (!root.Attributes.TryGetValue(snapshot.Name, out object? current)
                    || !ReferenceEquals(current, snapshot.Function) || !ReferenceEquals(snapshot.Function.Body, snapshot.Body)
                    || !ReferenceEquals(snapshot.Function.Defaults, snapshot.Defaults)) return false;
                // 真实数组内容可变，不能拿Python普通metadata里同名假属性代替它。
                for (int index = 0; index < snapshot.Values.Length; index++)
                    if (!ReferenceEquals(snapshot.Defaults[index], snapshot.Values[index])) return false;
            }
            if (!instance.TryGetSlot(slots[0], out object? widthValue) || widthValue is not long width || width <= 0
                || !instance.TryGetSlot(slots[1], out object? firstValue) || firstValue is not string first
                || !instance.TryGetSlot(slots[2], out object? restValue) || restValue is not string rest
                || !instance.TryGetSlot(slots[3], out object? expandValue) || expandValue is not bool expand
                || !instance.TryGetSlot(slots[4], out object? replaceValue) || replaceValue is not bool replace
                || !instance.TryGetSlot(slots[5], out object? fixValue) || fixValue is not false
                || !instance.TryGetSlot(slots[6], out object? breakValue) || breakValue is not bool breakWords
                || !instance.TryGetSlot(slots[7], out object? dropValue) || dropValue is not bool dropSpace
                || !instance.TryGetSlot(slots[8], out object? hyphenValue) || hyphenValue is not bool hyphens
                || !instance.TryGetSlot(slots[9], out object? tabValue) || tabValue is not long tabsize
                || tabsize < int.MinValue || tabsize > int.MaxValue
                || !instance.TryGetSlot(slots[10], out object? maxValue) || !ReferenceEquals(maxValue, PyNone.Instance)
                || !instance.TryGetSlot(slots[11], out object? placeholder) || placeholder is not string)
                return false;
            // 资格成立后所有中间块只属于默认实现，无用户回调/alias可观察，复用源区间算法。
            string normalized = PyTextWrapEngine.Normalize(input, expand, replace, tabsize);
            List<string> lines = PyTextWrapEngine.Wrap(normalized, width, first, rest, breakWords, dropSpace, hyphens);
            var values = new object?[lines.Count];
            for (int index = 0; index < lines.Count; index++) values[index] = lines[index];
            result = new PyList(values);
            return true;
        }
    }
}
