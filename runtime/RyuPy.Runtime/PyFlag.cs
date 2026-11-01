using System;
using System.Collections.Generic;
using System.Numerics;

namespace RyuPy.Runtime;

/// <summary>
/// Flag的有界真实协议：成员仍是Python实例，组合按值缓存；不伪装int，也不扩普通对象热分派。
/// 仅默认严格掩码，所有有效bit须有单bit声明；auto/边界配置/负成员等由前端或完成阶段明确拒绝。
/// </summary>
public static partial class PyEnum
{
    private static PyClass BuildFlagRoot(PyClass enumRoot)
    {
        var root = new PyClass("Flag") { BaseClass = enumRoot };
        foreach (string name in new[] { "__or__", "__ror__", "__and__", "__rand__", "__xor__", "__rxor__" })
        {
            string operation = name;
            root.SetAttr(name, PyNativeFunctions.Create(name, ["self", "other"], [null, null], 2,
                args => FlagBinary(args[0], args[1], operation)));
        }
        root.SetAttr("__invert__", FlagMethod("__invert__", self =>
        {
            ClassInfo info = FlagInfo(self);
            return Construct(self.Class, [PyInt.Normalize(info.FlagMask ^ FlagValue(self))], [], []);
        }));
        root.SetAttr("__bool__", FlagMethod("__bool__", self => !FlagValue(self).IsZero));
        root.SetAttr("__len__", FlagMethod("__len__", self => (long)BigInteger.PopCount(FlagValue(self))));
        root.SetAttr("__iter__", FlagMethod("__iter__", self => FlagIterate(self)));
        root.SetAttr("__contains__", PyNativeFunctions.Create("__contains__", ["self", "other"], [null, null], 2, args =>
        {
            PyInstance self = FlagMember(args[0]);
            if (args[1] is not PyInstance other || !ReferenceEquals(self.Class, other.Class) || !IsMember(other))
                throw new PyTypeError("unsupported operand type for Flag containment");
            BigInteger value = FlagValue(other);
            return (FlagValue(self) & value) == value;
        }));
        Classes.Add(root, new ClassInfo { IsFlag = true }); root.HasEnumMetadata = true;
        return root;
    }

    private static PyFunction FlagMethod(string name, Func<PyInstance, object?> body) =>
        PyNativeFunctions.Create(name, ["self"], [null], 1, args => body(FlagMember(args[0])));

    private static PyInstance FlagMember(object? value)
    {
        if (value is not PyInstance member || !IsMember(member) || !FlagInfo(member).IsFlag)
            throw new PyTypeError("Flag method requires a Flag member");
        return member;
    }
    private static ClassInfo FlagInfo(PyInstance member) => Classes.GetValue(member.Class, _ => throw new PyTypeError("not a Flag class"));
    private static BigInteger FlagValue(PyInstance member) => PyInt.ToBigInteger(Members.GetValue(member, _ => throw new PyTypeError("not a Flag member")).Value!);

    private static object FlagBinary(object? left, object? right, string operation)
    {
        PyInstance self = FlagMember(left);
        if (right is not PyInstance other || !ReferenceEquals(self.Class, other.Class) || !IsMember(other))
            return PyNotImplemented.Instance;
        BigInteger a = FlagValue(self), b = FlagValue(other);
        BigInteger result = operation.Contains("or", StringComparison.Ordinal) && !operation.Contains("xor", StringComparison.Ordinal)
            ? a | b : operation.Contains("and", StringComparison.Ordinal) ? a & b : a ^ b;
        return Construct(self.Class, [PyInt.Normalize(result)], [], []);
    }

    private static void FinalizeFlagInfo(ClassInfo info)
    {
        BigInteger all = BigInteger.Zero, singles = BigInteger.Zero;
        var unique = new List<object?>();
        foreach (object? item in info.Unique.Items)
        {
            var member = (PyInstance)item!;
            BigInteger value = FlagValue(member);
            all |= value;
            if (value > 0 && (value & (value - 1)).IsZero) { singles |= value; unique.Add(member); }
        }
        if (all != singles)
            throw new PyNotImplementedError("Flag composite bits require separately named single-bit members");
        info.FlagMask = singles;
        // 类迭代只含单bit，zero/复合/alias仍保留在Members/Values，不丢身份。
        info.Unique.Items.Clear(); info.Unique.Items.AddRange(unique);
    }

    private static object ConstructFlag(PyClass cls, ClassInfo info, object? original)
    {
        if (!PyInt.IsInt(original)) throw InvalidFlagValue(cls, original);
        BigInteger value = PyInt.ToBigInteger(original!);
        object? stored = original;
        if (value.Sign < 0)
        {
            long width = info.FlagMask.GetBitLength();
            if (width > int.MaxValue) throw new PyNotImplementedError("Flag mask is too wide");
            BigInteger range = BigInteger.One << (int)width;
            if (value < -range) throw InvalidFlagValue(cls, original);
            value += range; stored = PyInt.Normalize(value);
        }
        if ((value & ~info.FlagMask) != 0) throw InvalidFlagValue(cls, original);
        if (info.Values.TryGet(stored, out object? found)) return found!;
        var names = new List<string>();
        foreach (object? item in info.Unique.Items)
        {
            var member = (PyInstance)item!;
            if ((value & FlagValue(member)) != 0) names.Add(Members.GetValue(member, _ => throw new PyTypeError("invalid Flag member")).Name!);
        }
        var result = new PyInstance(cls);
        Members.Add(result, new MemberInfo(names.Count == 0 ? null : string.Join("|", names), stored));
        info.Values.Set(stored, result);
        return result;
    }
    private static PyValueError InvalidFlagValue(PyClass cls, object? value) => new($"{PyRepr.Repr(value)} is not a valid {cls.Name}");

    private static IPyIterator FlagIterate(PyInstance self)
    {
        BigInteger value = FlagValue(self);
        var members = new List<object?>();
        foreach (object? item in FlagInfo(self).Unique.Items)
            if ((value & FlagValue((PyInstance)item!)) != 0) members.Add(item);
        return PyIter.Create(new PyList(members));
    }
}
