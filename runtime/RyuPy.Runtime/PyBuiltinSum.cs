// sum流式累计：精确long连续原生运算，溢出/异构回退前准确物化，保留空输入与回调结果身份。
using System;
using System.Collections.Generic;

namespace RyuPy.Runtime;

public static partial class PyOps
{
    /// <summary>
    /// 内建 sum() 的流式累加实现：
    /// 对精确 CLR long 的连续加法采用原生 long 累计，消去每项中间装箱和通用 Binary；
    /// 异构类型、用户协议或结束时才按需物化。
    /// </summary>
    private static object BuiltinSumFast(object?[] args, string[] names, object?[] values)
    {
        RejectUnknownKeywords("sum", names, "start");
        if (args.Length < 1)
        {
            throw new PyTypeError("sum expected at least 1 argument, got 0");
        }
        object? total = KeywordValue(names, values, "start") ?? PyInt.Box(0L);
        if (args.Length > 1 && Array.IndexOf(names, "start") < 0)
        {
            total = args[1];
            if (args.Length > 2)
            {
                throw new PyTypeError("sum takes at most 2 arguments");
            }
        }

        // 每次取值后立即相加，保留用户迭代/加法的交错、早停和活列表变更。
        // 内部互操作仍可传入原生object数组；复用元组游标，无元素复制。
        object? source = args[0] is object?[] array ? new PyTuple(array) : args[0];
        IPyIterator iterator = PyIter.Create(source);

        object? totalObj = total;
        long nativeTotal = 0;
        bool nativeMode = false;
        bool dirty = false;

        if (totalObj is long startLong)
        {
            nativeMode = true;
            nativeTotal = startLong;
            dirty = false;
        }

        while (iterator.MoveNext())
        {
            object? current = iterator.Current();

            if (nativeMode && current is long itemLong)
            {
                long sum = unchecked(nativeTotal + itemLong);
                // 两个同号数相加得到异号结果 ⇔ 溢出 (按 PyInt.AddI64 同符号溢出位公式判断)
                if (((nativeTotal ^ sum) & (itemLong ^ sum)) < 0)
                {
                    // 溢出时物化准确左值后沿既有 Binary(Add) 升级 BigInteger
                    object left = dirty ? PyInt.Box(nativeTotal) : totalObj!;
                    dirty = false;
                    totalObj = Binary(Add, left, current ?? PyNone.Instance);
                    if (totalObj is long recoveredLong)
                    {
                        nativeMode = true;
                        nativeTotal = recoveredLong;
                        dirty = false;
                    }
                    else
                    {
                        nativeMode = false;
                    }
                }
                else
                {
                    nativeTotal = sum;
                    dirty = true;
                }
            }
            else
            {
                // 异构item/foreign或用户回调之前，若dirty才调用PyInt.Box物化左值；
                // 没有dirty时保留原对象，沿Binary(Add, total??None, current??None)
                if (dirty)
                {
                    totalObj = PyInt.Box(nativeTotal);
                    dirty = false;
                }
                totalObj = Binary(Add, totalObj ?? PyNone.Instance, current ?? PyNone.Instance);
                if (totalObj is long newLong)
                {
                    nativeMode = true;
                    nativeTotal = newLong;
                    dirty = false;
                }
                else
                {
                    nativeMode = false;
                }
            }
        }

        if (dirty)
        {
            totalObj = PyInt.Box(nativeTotal);
        }

        return totalObj ?? PyNone.Instance;
    }
}
