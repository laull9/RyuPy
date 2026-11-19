# PEP489模块状态和不同的heap type布局；方法调用、实例标签和CIL回调一起参与搬迁差分。
import builtins
import violet_ridge.engine._nacre as native

def identity(x):
    return x

def increment(x):
    return x + 1

probe = native.Gauge(2, label="雪")
marker = object()
print(probe.echo(marker, identity) is marker)
print(probe.sample(34, increment), probe.label())
try:
    probe.sample("bad", identity)
except TypeError as error:
    print(type(error).__name__, error.args, str(error))
try:
    native.Gauge(bias=2)
except TypeError as error:
    print(type(error).__name__, error.args, str(error))
loader = getattr(builtins, "__import__")
for module in ["violet_ridge.engine.ordinary_image", "violet_ridge.engine.stub_only"]:
    try:
        loader(module)
    except ImportError as error:
        print(type(error).__name__, error.name)
