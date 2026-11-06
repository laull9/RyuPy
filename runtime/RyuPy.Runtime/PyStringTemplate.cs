// str.format内部执行器：逐字段交错解析、导航、转换、动态规格和类型级format；不注册公共方法。
// 每次调用独立编号状态，嵌套规格共用状态；只准备本层生成的错误，用户回调不进宽catch。
using System;

namespace RyuPy.Runtime;

internal static class PyStringTemplate
{
    internal static object Format(object template, object?[] args, string[] names, object?[] values)
    {
        string text = PyStringLayout.RequireRaw(template);
        var call = new Arguments(args, names, values);
        return Build(template, text, 0, text.Length, 2, call);
    }

    private static object Build(object source, string text, int start, int end, int budget, Arguments call)
    {
        if (budget <= 0) throw PyStringTemplateParser.Syntax("Max string recursion exceeded");
        var writer = new PyStringTemplateWriter();
        int cursor = start;
        while (PyStringTemplateParser.Next(text, ref cursor, end, out var part))
        {
            // 同段同时有field时，字面不是终端：它必须先物化，不能借整个原模板。
            writer.Literal(source, text, part.LiteralStart, part.LiteralLength,
                part.Terminal && !part.HasField);
            if (!part.HasField) continue;
            object? value = Resolve(text, part.NameStart, part.NameEnd, call);
            value = PyStringTemplateConversions.Convert(value, part.Conversion);
            string spec = part.ExpandSpec
                ? PyStringLayout.RequireRaw(Build(source, text, part.SpecStart, part.SpecEnd, budget - 1, call))
                : text.Substring(part.SpecStart, part.SpecEnd - part.SpecStart);
            // 模板的生成规格是精确raw字符串；不同于公共builtin传入spec子类时保其原对象。
            object result = PyBuiltinFormat.Format(value, spec, spec);
            writer.Result(result, part.Terminal);
        }
        return writer.Finish();
    }

    private static object? Resolve(string text, int start, int end, Arguments call)
    {
        int cursor = start;
        while (cursor < end && text[cursor] is not ('.' or '[')) cursor++;
        long index = PyStringTemplateParser.Index(text, start, cursor);
        bool automatic = cursor == start;
        object? value = automatic || index >= 0
            ? call.Positional(index, automatic)
            : call.Keyword(text.Substring(start, cursor - start));
        // 每轮只识别本段；已有getter/getitem先执行，下一段空属性或非法尾字符之后才报错。
        while (cursor < end)
        {
            char access = text[cursor++];
            int itemStart = cursor;
            if (access == '.')
            {
                while (cursor < end && text[cursor] is not ('.' or '[')) cursor++;
                if (cursor == itemStart) throw PyStringTemplateParser.Syntax("Empty attribute in format string");
                value = PyObject.GetAttr(value, text.Substring(itemStart, cursor - itemStart));
            }
            else if (access == '[')
            {
                while (cursor < end && text[cursor] != ']') cursor++;
                if (cursor == end) throw PyStringTemplateParser.Syntax("Missing ']' in format string");
                int itemEnd = cursor++;
                long numeric = PyStringTemplateParser.Index(text, itemStart, itemEnd);
                if (itemStart == itemEnd) throw PyStringTemplateParser.Syntax("Empty attribute in format string");
                object key = numeric >= 0 ? numeric : text.Substring(itemStart, itemEnd - itemStart);
                value = PyOps.GetItem(value, key);
            }
            else throw PyStringTemplateParser.Syntax("Only '.' or '[' may follow ']' in format field specifier");
        }
        return value;
    }

    private sealed class Arguments(object?[] args, string[] names, object?[] values)
    {
        // 0未使用、1自动、2显式数字；命名字段永不改变此状态。
        private int _numbering;
        private long _next;

        internal object? Positional(long index, bool automatic)
        {
            int mode = automatic ? 1 : 2;
            if (_numbering != 0 && _numbering != mode)
                throw PyStringTemplateParser.Syntax(automatic
                    ? "cannot switch from manual field specification to automatic field numbering"
                    : "cannot switch from automatic field numbering to manual field specification");
            _numbering = mode;
            if (automatic) index = _next++;
            if (index >= args.Length)
                throw Error(new PyIndexError($"Replacement index {index} out of range for positional args tuple"));
            return args[(int)index];
        }

        internal object? Keyword(string key)
        {
            // 常见少量kwargs用ordinal数组查找；零kwargs和纯位置字段无需字典分配。
            for (int i = 0; i < names.Length; i++)
                if (string.Equals(names[i], key, StringComparison.Ordinal)) return values[i];
            throw Error(PyKeyError.ForKey(key));
        }
    }

    internal static T Error<T>(T error) where T : PyBaseException => (T)PyErrors.PrepareRaise(error);
}
