"""CP1b：编译print实际写入CPython sys重绑定的同一stdout/stderr对象。"""
import io
import sys

original_stdout = sys.stdout
original_stderr = sys.stderr
captured_stdout = io.StringIO()
captured_stderr = io.StringIO()
try:
    sys.stdout = captured_stdout
    sys.stderr = captured_stderr
    print('共享输出', 42)
    print('共享错误', file=sys.stderr)
finally:
    sys.stdout = original_stdout
    sys.stderr = original_stderr
actual_stdout = captured_stdout.getvalue()
actual_stderr = captured_stderr.getvalue()
assert (actual_stdout, actual_stderr) == ('共享输出 42\n', '共享错误\n'), (actual_stdout, actual_stderr)
captured_stdout.close()
captured_stderr.close()
print('CP1b 编译print与CPython sys流共享通过')
