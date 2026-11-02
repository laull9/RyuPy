# M3.10 已有协议的三臂 AOT 行为验收，按命令行选择场景。
import sys
import state
from state import value as initial_value


class SampleError(Exception):
    pass


def fail():
    raise SampleError("aot-user-error")


def outer(start):
    total = start

    def add(amount=2, *, scale=1):
        nonlocal total
        total += amount * scale
        return total

    return add


def source():
    try:
        amount = yield 2
        yield amount + 1
    except ValueError:
        yield "handled"
    finally:
        print("generator cleanup")
    return 9


mode = sys.argv[1] if len(sys.argv) > 1 else "all"
if mode == "argv":
    print(sys.argv[1:])
elif mode == "exit":
    print("before exit")
    raise SystemExit(7)
elif mode == "error":
    print("before error")
    fail()
elif mode == "all":
    print(state.value, state.read(), initial_value)
    state.value = 10
    print(state.read(), state.increment(amount=4), initial_value)
    call = outer(5)
    print(call(), call(scale=3, amount=4), call())
    other = outer(0)
    print(other(amount=1), call(amount=0))
    iterator = source()
    print(next(iterator), iterator.send(4))
    print(iterator.throw(ValueError("injected")))
    try:
        next(iterator)
    except StopIteration as done:
        print("return", done.value)
    closing = source()
    print(next(closing))
    closing.close()
    print(next(closing, "closed"))
else:
    raise ValueError("unknown test mode")
