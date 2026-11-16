# ADR0219消费者输入：本模块须作为显式Instance编译，供真实外来框架检查并回调CIL。
calls = []


def convert(value: str) -> int:
    return int(value)


def model_init(self, **data):
    return None


def dependency(value: int = 1) -> int:
    calls.append(value)
    return value + 1


def signature_names(callable_value):
    import inspect
    return list(inspect.signature(callable_value).parameters)
