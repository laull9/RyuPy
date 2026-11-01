// time 首批时钟与 sleep：墙钟只用于 epoch 时间，等待与耗时使用单调 Stopwatch。
// sleep 用单调已逝时间校验并分块等待，不将 CLR 毫秒参数上限误当成 Python 时间上限。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

namespace RyuPy.Runtime;

/// <summary>time 模块的秒数读取与等待 API；不含日历、时区和格式化接口。</summary>
public static class PyTimeModule
{
    /// <summary>构造首批原生 time 属性。</summary>
    public static Dictionary<string, object?> CreateAttributes() => new(StringComparer.Ordinal)
    {
        ["time"] = Clock("time", () => (DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks) / 10_000_000.0),
        ["monotonic"] = Clock("monotonic", Monotonic),
        ["perf_counter"] = Clock("perf_counter", Monotonic),
        ["sleep"] = PyNativeFunctions.Create("sleep", ["secs"], [null], 1,
            a => Sleep(a[0]), ["posonly"]),
    };
    private static PyFunction Clock(string name, Func<double> read) =>
        PyNativeFunctions.Create(name, [], [], 0, _ => read());
    private static double Monotonic() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

    private static object? Sleep(object? value)
    {
        double seconds = SleepSeconds(value);
        if (seconds < 0) throw new PyValueError("sleep length must be non-negative");
        if (seconds == 0)
        {
            try { Thread.Sleep(0); }
            catch (ThreadInterruptedException) { /* 零等待只让出调度机会，不泄漏宿主线程异常。 */ }
            return null;
        }
        long start = Stopwatch.GetTimestamp();
        while (true)
        {
            double elapsed = (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency;
            double remaining = seconds - elapsed;
            if (remaining <= 0) return null;
            int milliseconds = (int)Math.Min(int.MaxValue, Math.Ceiling(remaining * 1000));
            try { Thread.Sleep(milliseconds); }
            catch (ThreadInterruptedException) { /* 宿主线程中断不提前结束 Python 的计时等待。 */ }
        }
    }
    private static double SleepSeconds(object? value)
    {
        if (value is double seconds)
        {
            if (double.IsNaN(seconds)) throw new PyValueError("Invalid value NaN (not a number)");
            // CPython 的内部时间用有符号 64 位纳秒；不假装可接受任意 double 秒数。
            if (!double.IsFinite(seconds) || seconds >= 9_223_372_036.854776 || seconds < -9_223_372_036.854776)
                throw new PyOverflowError("timestamp out of range for platform time_t");
            return seconds;
        }
        if (!PyInt.IsInt(value))
        {
            if (!PyObject.TryMethod(value, "__index__", [], out value))
                throw new PyTypeError("object cannot be interpreted as an integer");
            if (!PyInt.IsInt(value)) throw new PyTypeError("__index__ returned non-int");
        }
        BigInteger integer = PyInt.ToBigInteger(value!);
        BigInteger nanoseconds = integer * 1_000_000_000;
        if (nanoseconds < long.MinValue || nanoseconds > long.MaxValue)
            throw new PyOverflowError("timestamp too large to convert to C _PyTime_t");
        return (double)integer;
    }
}
