# 每次print固定目标；write或str副作用替换sys.stdout不能切走当前调用后续片段。
import io
import sys

original = sys.stdout
first = io.StringIO()
second = io.StringIO()


class Redirect:
    def write(self, text):
        first.write(text)
        sys.stdout = second

    def flush(self):
        pass


sys.stdout = Redirect()
print(1, 2)
print("after")
sys.stdout = original
print(repr(first.getvalue()), repr(second.getvalue()))


class Changing:
    def __str__(self):
        sys.stdout = second
        return "value"


sys.stdout = first
print(Changing(), "tail", sep=":")
sys.stdout = original
print(repr(first.getvalue()), repr(second.getvalue()))
print("none options", sep=None, end=None)

# 未捕获退出会由入口处理，捕获时必须保留code身份且构造不提前调用str。
class Code:
    def __str__(self):
        print("code str")
        return "custom"


code = Code()
try:
    sys.exit(code)
except SystemExit as error:
    print(error.code is code, error.args[0] is code)

# sys.stdout=None仍求值参数，但不执行结果的str。
class NotFormatted:
    def __str__(self):
        raise ValueError("must not format")


sys.stdout = None
print(NotFormatted())
sys.stdout = original
print("none target")


def move_target():
    sys.stdout = second
    return "argument"


sys.stdout = first
print(move_target())
sys.stdout = original
print(repr(second.getvalue()))

# flush的真值在sep验证、任何write和None目标早退之前检查，且只检查一次。
events = []


class Sink:
    def write(self, value):
        events.append("write " + value)

    def flush(self):
        events.append("flush")


class Flag:
    def __bool__(self):
        events.append("bool")
        return True


print("x", file=Sink(), flush=Flag())
print(events)
events.clear()
try:
    print("x", sep=1, file=Sink(), flush=Flag())
except TypeError:
    print(events)
events.clear()
sys.stdout = None
print("x", flush=Flag())
sys.stdout = original
print(events)


class BadFlag:
    def __bool__(self):
        raise ValueError("flush flag")


capture = io.StringIO()
try:
    print("must not write", file=capture, flush=BadFlag())
except ValueError:
    print(repr(capture.getvalue()))

sys.stdout = None
print("ignored", sep=3)
sys.stdout = original
print("none skips sep")
