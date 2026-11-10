// 可变subject的显式finditer状态：创建即租约，每next刷新一次全前缀，成功后提交游标，真耗尽才释放。
using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace RyuPy.Runtime;

public partial class PyRegexPattern
{
    // pos/endpos的用户协议由共同入口先完成；此处立即取得扫描期租约，不延迟到first next。
    internal IPyIterator CreateMutableBytesIterator(object source, long start, long end) =>
        new MutableBytesIterator(this, source, start, end);

    private sealed class MutableBytesIterator : IPyIterator
    {
        private PyRegexPattern? _pattern;
        private object? _source;
        private PyRegexBufferLease? _lease;
        private PyRegexMatch? _current;
        private readonly int _start, _end;
        private int _cursor, _executing;
        private bool _retry, _finished;

        internal MutableBytesIterator(PyRegexPattern pattern, object source, long start, long end)
        {
            // 先分配无资源iterator；Acquire返回后仅基本clamp/赋值，无后续分配或用户代码。
            _pattern = pattern; _source = source;
            _lease = PyRegexBufferSubject.Acquire(source);
            _start = (int)Math.Clamp(start, 0L, _lease.Length);
            _end = (int)Math.Clamp(end, 0L, _lease.Length); _cursor = _start;
        }

        public bool MoveNext() => Step(null);
        public object? Current() => _current ?? (object)PyNone.Instance;

        // executor只服务本次内部受控专项；正常路径不分配/保存delegate，不开放Python callback。
        internal bool Step(Func<Regex, string, int, Match>? executor)
        {
            if (_finished) return false;
            if (Interlocked.CompareExchange(ref _executing, 1, 0) != 0)
                throw PyRegexBytes.Own(new PyValueError("regular expression scanner already executing"));
            try
            {
                int position = _cursor; bool retry = _retry;
                if (position > _end) return Finish();
                PyRegexPattern pattern = _pattern!;
                string input = _lease!.EncodeLatin1(_end);
                while (position <= _end)
                {
                    Regex regex = retry ? pattern._nonempty.Value : pattern._search;
                    Match result = executor is null ? RunBytes(regex, input, position) : executor(regex, input, position);
                    if (!result.Success)
                    {
                        if (!retry || position == _end) return Finish();
                        // 同次next内部没有用户代码；候选前进不再次编码，也不提前提交checkpoint。
                        position++; retry = false; continue;
                    }
                    var current = new PyRegexMatch(pattern,
                        new PyRegexMutableCaptures(pattern, _source!, result), _start, _end);
                    // marks/Match分配完成才提交。非Stop故障保旧checkpoint是Ryu资源策略，并非CP错误cursor还原。
                    _cursor = result.Index + result.Length; _retry = result.Length == 0; _current = current;
                    return true;
                }
                return Finish();
            }
            finally { Volatile.Write(ref _executing, 0); }
        }

        // 真耗尽确定撤自己的lease并清强引用；弃用则由既有lease finalizer兜底，不另加iterator finalizer。
        internal bool Finish()
        {
            _finished = true; PyRegexBufferLease? lease = _lease;
            _lease = null; _current = null; _source = null; _pattern = null;
            lease?.Dispose(); return false;
        }
    }
}
