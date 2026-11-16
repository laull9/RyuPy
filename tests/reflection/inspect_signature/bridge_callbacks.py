# ryupy: pending
# 真实CPinspect消费者所调用的CIL模块：函数元数据/签名对象来回保持同一身份。
import inspect

shared = []

def direct(value=shared, *, option=1):
    return value

def wrapper(*args, **kwargs):
    return direct(*args, **kwargs)

wrapper.__wrapped__ = direct

def api_identity(value):
    return value

def current_signature_api():
    return inspect.signature

def inspect_names(value):
    return list(inspect.signature(value).parameters)

def snapshot(value):
    return inspect.signature(value)

def make_bound():
    return inspect.signature(direct).bind()

def read_bound(value):
    return value.arguments, value.args, value.kwargs

def apply_bound(value):
    value.apply_defaults()
    return value
