# ryupy: pending
# CPython反例保留；产品在旧默认顶层/新嵌套类体交界明确静态Diagnostic。
__class__ = 'must not become fallback'
class Outer:
    class Inner:
        value = __class__
