# 同一C库多个公开函数：身份、别名、重绑、逐函数错误与真实极值结果。
import sys
import native_math
from native_math import add, subtract, add_alias

mode = sys.argv[1] if len(sys.argv) > 1 else "normal"

def fail_call():
    return subtract(True, 2)

if mode == "type":
    fail_call()
elif mode == "load":
    subtract(1, 2)
elif mode == "lazy":
    print("imported")
elif mode == "missing-symbol":
    print(add(4, 5))
    try:
        subtract(1, 2)
    except ImportError:
        print("second-symbol-missing")
    print(add_alias(6, 7))
else:
    print(add(2147483647, 2147483647))
    print(subtract(-2147483648, 2147483647))
    print(add_alias(-2147483648, -2147483648))
    print(add is native_math.add, subtract is native_math.subtract, add is add_alias)
    print(add.__name__, subtract.__name__, add_alias.__name__)
    native_math.add = 42
    print(add(10, 20), subtract(10, 20), native_math.add)
    native_math.add = add
    for function in (add, subtract, add_alias):
        try:
            function(True, 1)
        except TypeError:
            print("type")
        try:
            function(2147483648, 1)
        except OverflowError:
            print("range")
        try:
            function(1)
        except TypeError:
            print("arity")
        try:
            function(left=1, right=2)
        except TypeError:
            print("posonly")
    def rebind():
        native_math.subtract = 99
        return 4
    print(native_math.subtract(rebind(), 1))
    print(subtract(4, 1), native_math.subtract)
    native_math.subtract = subtract
    print("multi-export-ok")
