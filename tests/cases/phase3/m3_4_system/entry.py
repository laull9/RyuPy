import sys
import io
import os

print(sys.argv[1:])
print(sys.modules["sys"] is sys, sys.modules["io"] is io)
print("sys" in sys.modules, "unknown_ryupy_module" in sys.modules)
print(sys.stdin.readline().strip())
original = sys.stdout
capture = io.StringIO()
sys.stdout = capture
print(12)
print("dynamic", 3, sep=":", end="!")
sys.stdout = original
print(capture.getvalue())
print("explicit", file=capture, flush=True)
print(capture.getvalue())
os.environ["RYUPY_M3_4_TEST"] = "value"
print(os.getenv("RYUPY_M3_4_TEST"), os.environ["RYUPY_M3_4_TEST"])
del os.environ["RYUPY_M3_4_TEST"]
print(os.getenv("RYUPY_M3_4_TEST", "missing"))
try:
    sys.exit(7)
except SystemExit as error:
    print(error.code)

# input只剥除readline结果末尾一个LF；自定义输入的CR不作二次通用换行。
original_input = sys.stdin
for value in ["abc\r", "abc\r\n", "abc\n"]:
    sys.stdin = io.StringIO(value, newline="")
    print(repr(input()))
sys.stdin = original_input

# 空环境值仍是存在的映射项，putenv不能反向更新Python.environ缓存。
os.environ["RYUPY_M3_4_TEST"] = ""
print("RYUPY_M3_4_TEST" in os.environ, repr(os.getenv("RYUPY_M3_4_TEST")))
os.putenv("RYUPY_M3_4_TEST", "outside")
print(repr(os.getenv("RYUPY_M3_4_TEST")))
del os.environ["RYUPY_M3_4_TEST"]
for key in ["", "a=b", "bad\x00"]:
    try:
        os.unsetenv(key)
    except ValueError:
        print("bad env value")
    except OSError:
        print("bad env name")
