// Enum类体冷赋值协议：明确成员名单控制生成时刻，普通类不进入此状态或查询历史。
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

public static partial class PyEnum
{
    internal sealed class PendingEnum
    {
        private readonly string[] names;
        private readonly HashSet<string> memberSet = new(StringComparer.Ordinal);
        private readonly List<object?> history = new();
        private readonly bool flag;
        private int assigned;
        internal PendingEnum(object?[] declared, bool flag)
        {
            names = new string[declared.Length]; this.flag = flag;
            for (int i = 0; i < declared.Length; i++)
            {
                if (declared[i] is not string name || name.Length == 0
                    || name.StartsWith("__", StringComparison.Ordinal)
                    || (name.StartsWith('_') && name.EndsWith('_')) || name == "mro" || !memberSet.Add(name))
                    throw new PyTypeError("Enum declaration requires unique string member names");
                names[i] = name;
            }
        }
        internal object? Prepare(string name, object? value)
        {
            if (!memberSet.Contains(name)) return value; // 方法与编译器元数据不参与生成历史。
            if (assigned >= names.Length || names[assigned] != name)
                throw new PyTypeError("Enum member assignments must follow the frozen declaration order");
            PyEnumAuto? marker = value as PyEnumAuto;
            bool generated = marker is { Unresolved: true };
            object? resolved = marker is null ? value : generated ? Generate() : marker.Value;
            if (resolved is not (string or bool or long or int or BigInteger or PyNone) && resolved is not null)
                throw new PyNotImplementedError("Enum values support scalar literals and resolved scalar auto values only");
            if (flag && (!PyInt.IsInt(resolved) || PyInt.ToBigInteger(resolved!).Sign < 0))
                throw new PyNotImplementedError("Flag members require non-negative integer values");
            // 警告（含error filter）与验证均成功后才缓存；复用同标记保持值及别名身份。
            if (generated) marker!.Resolve(resolved);
            history.Add(resolved); assigned++;
            return resolved;
        }
        private object Generate()
        {
            if (history.Count == 0) return 1L;
            bool anyInteger = false, allInteger = true;
            BigInteger max = BigInteger.Zero;
            foreach (object? value in history)
            {
                if (!PyInt.IsInt(value)) { allInteger = false; continue; }
                BigInteger number = PyInt.ToBigInteger(value!);
                if (!anyInteger || number > max) max = number;
                anyInteger = true;
            }
            if (flag)
            {
                // 既有Flag非负整数守卫在每次成员写入执行，不扩大负值或混合值域。
                if (!allInteger || max.Sign < 0) throw new PyNotImplementedError("Flag auto requires non-negative integer history");
                long bits = max.GetBitLength();
                if (bits > int.MaxValue) throw new PyOverflowError("Flag auto value is too large");
                return PyInt.Normalize(BigInteger.One << (int)bits);
            }
            if (allInteger)
            {
                // CP3.12先检查最后值是否等于排序后的最大值；非单调/alias历史仍有可见警告。
                if (PyInt.ToBigInteger(history[^1]!) != max) PyWarnings.EnumAutoGeneration();
                return PyInt.Normalize(max + BigInteger.One);
            }
            PyWarnings.EnumAutoGeneration();
            if (!anyInteger) return 1L; // str/None均不能加1，CP默认回到start。
            // CP混合排序失败后的部分排列可影响倒序回退；共享排序尚未实现这一异常合同。
            throw new PyNotImplementedError("Enum auto with mixed integer and non-integer history requires partial-sort failure semantics");
        }
        internal void ValidateFinalization(object?[] declared)
        {
            if (declared.Length != names.Length || assigned != names.Length)
                throw new PyTypeError("Enum finalization does not match its frozen member declaration");
            for (int i = 0; i < names.Length; i++)
                if (declared[i] is not string name || name != names[i])
                    throw new PyTypeError("Enum finalization does not match its frozen member declaration");
        }
    }
}
