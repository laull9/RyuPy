# 真实进程退出fixture：由driver用例传入模式，不在普通stdout差分中隐去错误报告。
import atexit
import sys
mode = sys.argv[1]
class Callback:
    def __repr__(self):
        if mode == 'bad_repr':
            raise ValueError('repr failure')
        return '<exit callback>'
    def __call__(self):
        print('callback')
        if mode == 'system_exit':
            raise SystemExit(7)
        if mode == 'chain':
            raise TypeError('callback failure') from ValueError('hidden cause')
        if mode in ['callback_error', 'bad_repr', 'body_callback_error', 'stderr_error']:
            raise ValueError('callback failure')
atexit.register(print, 'oldest')
atexit.register(Callback())
print('body')
if mode == 'system_exit':
    sys.exit(3)
if mode in ['body_error', 'body_callback_error']:
    raise ValueError('body failure')
if mode == 'no_close':
    class Stream:
        def write(self, text):
            return sys.__stdout__.write(text)
        def flush(self):
            return sys.__stdout__.flush()
        def close(self):
            raise RuntimeError('must not close unregistered stream')
    sys.stdout = Stream()
if mode == 'flush_error':
    class FlushErrorStream:
        def write(self, text):
            return len(text)
        def flush(self):
            raise ValueError('flush failure')
    def change_stdout():
        sys.stdout = FlushErrorStream()
    atexit.register(change_stdout)

if mode == 'stderr_error':
    class BrokenErrorStream:
        def write(self, text):
            raise ValueError('error stream failure')
        def flush(self):
            pass
    sys.stderr = BrokenErrorStream()
