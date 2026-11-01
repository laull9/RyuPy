// 独立布局/ABI实验；只覆盖明确的数值子集，不替换Python对象协议。引用与数值字段绝不重叠。
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

static class Tags
{
    internal static readonly object Integer = new(), Float = new(), Boolean = new(), None = new();
    internal static readonly object True = true, False = false;
    private static readonly object[] Small = Enumerable.Range(-5, 262).Select(value => (object)(long)value).ToArray();
    internal static object Box(long value) => value >= -5 && value <= 256 ? Small[value + 5] : (object)value;
}

[StructLayout(LayoutKind.Sequential)]
readonly struct Value16
{
    internal readonly object? Reference;
    internal readonly long Bits;
    private Value16(object? reference, long bits) { Reference = reference; Bits = bits; }
    internal static Value16 Int(long value) => new(Tags.Integer, value);
    internal static Value16 Float(double value) => new(Tags.Float, BitConverter.DoubleToInt64Bits(value));
    internal static Value16 Existing(object? value) => new(value ?? Tags.None, 0);
    internal static Value16 Fresh(object value) => value switch
    {
        long x => Int(x), double x => Float(x), bool x => new(Tags.Boolean, x ? 1 : 0), _ => Existing(value),
    };
    internal bool IsInt => ReferenceEquals(Reference, Tags.Integer);
    internal bool IsFloat => ReferenceEquals(Reference, Tags.Float);
    internal object? Materialize() => Reference is null ? throw new InvalidOperationException("unbound") : IsInt ? Tags.Box(Bits) : IsFloat ? BitConverter.Int64BitsToDouble(Bits)
        : ReferenceEquals(Reference, Tags.Boolean) ? (Bits != 0 ? Tags.True : Tags.False) : ReferenceEquals(Reference, Tags.None) ? null : Reference;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Value16 Add(Value16 left, Value16 right)
    {
        if (left.IsInt && right.IsInt)
        {
            long sum = unchecked(left.Bits + right.Bits);
            if (((left.Bits ^ sum) & (right.Bits ^ sum)) >= 0) return Int(sum);
        }
        if (left.IsFloat && right.IsFloat)
            return Float(BitConverter.Int64BitsToDouble(left.Bits) + BitConverter.Int64BitsToDouble(right.Bits));
        return Fresh(Numeric.Add(left.Materialize()!, right.Materialize()!));
    }
}

enum Kind : byte { Unbound, Reference, Integer, Float, Boolean, None }
[StructLayout(LayoutKind.Sequential)]
readonly struct Value24
{
    internal readonly object? Reference;
    internal readonly long Bits;
    internal readonly Kind Tag;
    private Value24(Kind tag, long bits, object? reference) { Tag = tag; Bits = bits; Reference = reference; }
    internal static Value24 Int(long value) => new(Kind.Integer, value, null);
    internal static Value24 Float(double value) => new(Kind.Float, BitConverter.DoubleToInt64Bits(value), null);
    internal static Value24 Existing(object? value) => value is null ? new(Kind.None, 0, null) : new(Kind.Reference, 0, value);
    internal static Value24 Fresh(object value) => value switch
    {
        long x => Int(x), double x => Float(x), bool x => new(Kind.Boolean, x ? 1 : 0, null), _ => Existing(value),
    };
    internal object? Materialize() => Tag switch
    {
        Kind.Integer => Tags.Box(Bits), Kind.Float => BitConverter.Int64BitsToDouble(Bits), Kind.Boolean => Bits != 0 ? Tags.True : Tags.False,
        Kind.None => null, Kind.Reference => Reference, _ => throw new InvalidOperationException("unbound"),
    };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Value24 Add(Value24 left, Value24 right)
    {
        if (left.Tag == Kind.Integer && right.Tag == Kind.Integer)
        {
            long sum = unchecked(left.Bits + right.Bits);
            if (((left.Bits ^ sum) & (right.Bits ^ sum)) >= 0) return Int(sum);
        }
        if (left.Tag == Kind.Float && right.Tag == Kind.Float)
            return Float(BitConverter.Int64BitsToDouble(left.Bits) + BitConverter.Int64BitsToDouble(right.Bits));
        return Fresh(Numeric.Add(left.Materialize()!, right.Materialize()!));
    }
}

static class Numeric
{
    private static readonly object One = 1L;
    // 与生产PyOps分离的对称操作：仅long/double/bool/BigInteger，不声称用户__add__完整语义。
    internal static object Add(object left, object right)
    {
        if (left is bool lb) left = lb ? 1L : 0L;
        if (right is bool rb) right = rb ? 1L : 0L;
        if (left is long a && right is long b)
        {
            long sum = unchecked(a + b);
            return ((a ^ sum) & (b ^ sum)) >= 0 ? (object)sum : new BigInteger(a) + b;
        }
        if (left is double || right is double)
            return Convert.ToDouble(left) + Convert.ToDouble(right);
        BigInteger x = left is BigInteger bigA ? bigA : new BigInteger((long)left);
        BigInteger y = right is BigInteger bigB ? bigB : new BigInteger((long)right);
        return x + y;
    }
    [MethodImpl(MethodImplOptions.NoInlining)] internal static object ObjectCall(object value) => Add(value, One);
    [MethodImpl(MethodImplOptions.NoInlining)] internal static Value16 Call16(Value16 value) => Value16.Add(value, Value16.Int(1));
    [MethodImpl(MethodImplOptions.NoInlining)] internal static Value24 Call24(Value24 value) => Value24.Add(value, Value24.Int(1));
    [MethodImpl(MethodImplOptions.NoInlining)] internal static Value16 Call16In(in Value16 value) => Value16.Add(value, Value16.Int(1));
    [MethodImpl(MethodImplOptions.NoInlining)] internal static Value24 Call24In(in Value24 value) => Value24.Add(value, Value24.Int(1));
}

static class Work
{
    internal static int Count = 1_000_000;
    internal static object? Sink;
    internal static long ObjectArithmetic()
    {
        object total = 1000L, step = 3L;
        for (int i = 0; i < Count; i++) total = Numeric.Add(total, step);
        Sink = total; return (long)total;
    }
    internal static long SelectiveLocal()
    {
        long total = 1000;
        for (int i = 0; i < Count; i++) total += 3;
        Sink = total; return total;
    }
    internal static long Arithmetic16()
    {
        Value16 total = Value16.Int(1000), step = Value16.Int(3);
        for (int i = 0; i < Count; i++) total = Value16.Add(total, step);
        Sink = total.Materialize(); return total.Bits;
    }
    internal static long Arithmetic24()
    {
        Value24 total = Value24.Int(1000), step = Value24.Int(3);
        for (int i = 0; i < Count; i++) total = Value24.Add(total, step);
        Sink = total.Materialize(); return total.Bits;
    }
    internal static long ObjectFloat()
    {
        object total = 1000.0, step = 0.5;
        for (int i = 0; i < Count; i++) total = Numeric.Add(total, step);
        Sink = total; return (long)(double)total;
    }
    internal static long Float16()
    {
        Value16 total = Value16.Float(1000), step = Value16.Float(0.5);
        for (int i = 0; i < Count; i++) total = Value16.Add(total, step);
        Sink = total.Materialize(); return (long)BitConverter.Int64BitsToDouble(total.Bits);
    }
    internal static long Float24()
    {
        Value24 total = Value24.Float(1000), step = Value24.Float(0.5);
        for (int i = 0; i < Count; i++) total = Value24.Add(total, step);
        Sink = total.Materialize(); return (long)BitConverter.Int64BitsToDouble(total.Bits);
    }
    internal static long Calls(string mode)
    {
        object boxed = 1000L; Value16 a = Value16.Int(1000); Value24 b = Value24.Int(1000);
        switch (mode)
        {
            case "object": for (int i = 0; i < Count; i++) boxed = Numeric.ObjectCall(boxed); Sink = boxed; return (long)boxed;
            case "16": for (int i = 0; i < Count; i++) a = Numeric.Call16(a); break;
            case "16in": for (int i = 0; i < Count; i++) a = Numeric.Call16In(in a); break;
            case "24": for (int i = 0; i < Count; i++) b = Numeric.Call24(b); break;
            case "24in": for (int i = 0; i < Count; i++) b = Numeric.Call24In(in b); break;
            case "16object": for (int i = 0; i < Count; i++) a = Value16.Fresh(Numeric.ObjectCall(a.Materialize()!)); break;
            case "24object": for (int i = 0; i < Count; i++) b = Value24.Fresh(Numeric.ObjectCall(b.Materialize()!)); break;
        }
        Sink = mode.StartsWith("16") ? a.Materialize() : b.Materialize(); return (long)Sink!;
    }
    internal static long Container(string mode)
    {
        if (mode == "object")
        {
            var array = new object[64]; Array.Fill(array, (object)1000L); object one = 1L;
            for (int i = 0; i < Count; i++) array[i & 63] = Numeric.Add(array[i & 63], one);
            long sum = 0; foreach (object x in array) sum += (long)x; Sink = array; return sum;
        }
        if (mode == "16")
        {
            var array = new Value16[64]; Array.Fill(array, Value16.Int(1000));
            for (int i = 0; i < Count; i++) array[i & 63] = Value16.Add(array[i & 63], Value16.Int(1));
            long sum = 0; foreach (Value16 x in array) sum += x.Bits; Sink = array; return sum;
        }
        else
        {
            var array = new Value24[64]; Array.Fill(array, Value24.Int(1000));
            for (int i = 0; i < Count; i++) array[i & 63] = Value24.Add(array[i & 63], Value24.Int(1));
            long sum = 0; foreach (Value24 x in array) sum += x.Bits; Sink = array; return sum;
        }
    }
    internal static long RefContainer(string mode)
    {
        object first = new byte[1], second = new byte[2]; long sum = 0;
        if (mode == "object")
        {
            var array = new object[4096]; Array.Fill(array, first);
            for (int i = 0; i < Count; i++)
            {
                int slot = i & 4095; if (ReferenceEquals(array[slot], first)) sum++;
                array[slot] = (i & 1) == 0 ? second : first;
            }
            Sink = array;
        }
        else if (mode == "16")
        {
            var array = new Value16[4096]; Array.Fill(array, Value16.Existing(first));
            for (int i = 0; i < Count; i++)
            {
                int slot = i & 4095; if (ReferenceEquals(array[slot].Reference, first)) sum++;
                array[slot] = Value16.Existing((i & 1) == 0 ? second : first);
            }
            Sink = array;
        }
        else
        {
            var array = new Value24[4096]; Array.Fill(array, Value24.Existing(first));
            for (int i = 0; i < Count; i++)
            {
                int slot = i & 4095; if (ReferenceEquals(array[slot].Reference, first)) sum++;
                array[slot] = Value24.Existing((i & 1) == 0 ? second : first);
            }
            Sink = array;
        }
        return sum;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static (object, WeakReference) Rooted(bool small)
    {
        object target = new byte[100]; var weak = new WeakReference(target);
        return (small ? new Value16[] { Value16.Existing(target) } : (object)new Value24[] { Value24.Existing(target) }, weak);
    }
    internal static void Check()
    {
        bool unbound16 = false, unbound24 = false;
        try { default(Value16).Materialize(); } catch (InvalidOperationException) { unbound16 = true; }
        try { default(Value24).Materialize(); } catch (InvalidOperationException) { unbound24 = true; }
        if (!unbound16 || !unbound24) throw new Exception("unbound conflated with None");
        object shared = 123456789L;
        var a = Value16.Existing(shared); var b = a;
        if (!ReferenceEquals(a.Materialize(), b.Materialize())) throw new Exception("existing alias16");
        var c = Value24.Existing(shared); var d = c;
        if (!ReferenceEquals(c.Materialize(), d.Materialize())) throw new Exception("existing alias24");
        // 反例刻意保留：朴素inline复制+各自物化不能满足Python别名身份，不能当完整实现发布。
        var fresh = Value16.Int(123456789); var copy = fresh;
        bool naiveIdentity = ReferenceEquals(fresh.Materialize(), copy.Materialize());
        if (naiveIdentity) throw new Exception("unexpected scalar boxing identity");
        foreach (bool small in new[] { true, false })
        {
            var roots = Rooted(small);
            GC.Collect(2, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
            if (!roots.Item2.IsAlive) throw new Exception("GC lost struct reference");
            GC.KeepAlive(roots.Item1);
        }
        BigInteger wanted = new BigInteger(long.MaxValue) + 1;
        if (!Equals(Value16.Add(Value16.Int(long.MaxValue), Value16.Int(1)).Materialize(), wanted)) throw new Exception("overflow16");
        if (!Equals(Value24.Add(Value24.Int(long.MaxValue), Value24.Int(1)).Materialize(), wanted)) throw new Exception("overflow24");
        if (!Equals(Value16.Add(Value16.Fresh(true), Value16.Int(2)).Materialize(), 3L)) throw new Exception("bool16");
        if (!Equals(Value24.Add(Value24.Int(2), Value24.Float(0.5)).Materialize(), 2.5)) throw new Exception("mixed24");
        if (!ReferenceEquals(Value16.Int(7).Materialize(), Value24.Int(7).Materialize())) throw new Exception("small identity");
        if (!ReferenceEquals(Value16.Fresh(true).Materialize(), Value24.Fresh(true).Materialize())) throw new Exception("bool identity");
        if (Value16.Existing(null).Materialize() is not null || Value24.Existing(null).Materialize() is not null) throw new Exception("none");
        double negativeZero = -0.0;
        if (BitConverter.DoubleToInt64Bits((double)Value16.Float(negativeZero).Materialize()!) != long.MinValue) throw new Exception("negative zero");
        Console.WriteLine(JsonSerializer.Serialize(new { check = "pass", size16 = Unsafe.SizeOf<Value16>(), size24 = Unsafe.SizeOf<Value24>(),
            containsRefs16 = RuntimeHelpers.IsReferenceOrContainsReferences<Value16>(), containsRefs24 = RuntimeHelpers.IsReferenceOrContainsReferences<Value24>(),
            existingAlias = true, naiveImmediateAlias = naiveIdentity, gcRoots = true, overflowBigInteger = true }));
    }
}

class Program
{
    static void Main(string[] args)
    {
        Work.Check();
        if (args.Contains("check")) return;
        if (args.Contains("jit"))
        {
            Work.Count = 10000;
            Console.WriteLine(Work.Arithmetic16()); Console.WriteLine(Work.Arithmetic24());
            foreach (string mode in new[] { "16", "24", "16in", "24in" }) Console.WriteLine(Work.Calls(mode));
            return;
        }
        var tests = new Dictionary<string, (Func<long> Body, long Expected)>
        {
            ["object_integer"] = (Work.ObjectArithmetic, 1000 + 3L * Work.Count),
            ["selective_local"] = (Work.SelectiveLocal, 1000 + 3L * Work.Count),
            ["value16_integer"] = (Work.Arithmetic16, 1000 + 3L * Work.Count),
            ["value24_integer"] = (Work.Arithmetic24, 1000 + 3L * Work.Count),
            ["object_float"] = (Work.ObjectFloat, 1000 + Work.Count / 2),
            ["value16_float"] = (Work.Float16, 1000 + Work.Count / 2),
            ["value24_float"] = (Work.Float24, 1000 + Work.Count / 2),
        };
        foreach (string mode in new[] { "object", "16", "24", "16in", "24in", "16object", "24object" })
            tests["call_" + mode] = (() => Work.Calls(mode), 1000 + Work.Count);
        foreach (string mode in new[] { "object", "16", "24" })
            tests["container_" + mode] = (() => Work.Container(mode), 64000 + Work.Count);
        foreach (string mode in new[] { "object", "16", "24" })
            tests["references_" + mode] = (() => Work.RefContainer(mode), Work.Count / 2 + 2048);
        foreach (var (name, test) in tests)
        {
            for (int warm = 0; warm < 3; warm++) if (test.Body() != test.Expected) throw new Exception(name + " warm result");
            for (int repeat = 0; repeat < 7; repeat++)
            {
                long bytes = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
                long result = test.Body(); double seconds = Stopwatch.GetElapsedTime(start).TotalSeconds;
                bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
                if (result != test.Expected) throw new Exception(name + " result");
                Console.WriteLine(JsonSerializer.Serialize(new { name, repeat, seconds, bytes, result, iterations = Work.Count }));
            }
        }
    }
}
