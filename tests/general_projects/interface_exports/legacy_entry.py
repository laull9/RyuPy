# 传统扩展的实际Python API；应用回调由Rust编译为静态CIL，stub只供接口提取。
import builtins
import cedar_dock._atlas as native

def identity(x):
    return x

def increment(x):
    return x + 1

marker = object()
print(native.relay(marker, identity) is marker)
print(native.relay(38, increment, offset=3))
try:
    native.reject(5)
except ValueError as error:
    print(type(error).__name__, error.args, str(error))
try:
    native.relay(1, identity, 2)
except TypeError as error:
    print(type(error).__name__, error.args, str(error))
loader = getattr(builtins, "__import__")
for module in ["cedar_dock.ordinary_image", "cedar_dock.stub_only"]:
    try:
        loader(module)
    except ImportError as error:
        print(type(error).__name__, error.name)
