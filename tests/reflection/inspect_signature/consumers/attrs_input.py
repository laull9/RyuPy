# ryupy: pending
# 真实attrs23.2.0消费者；CPython主消费者与原生descriptor候选分别跑，回调模块必须CIL。
import inspect
import attrs
from attr._compat import _AnnotationExtractor
import compiled_callbacks

extractor = _AnnotationExtractor(compiled_callbacks.convert)
assert extractor.get_first_param_type() is str
assert extractor.get_return_type() is int
assert inspect.Parameter.empty is inspect.Signature.empty


@attrs.define
class Record:
    value = attrs.field(converter=compiled_callbacks.convert)


record = Record("42")
assert record.value == 42
assert inspect.signature(Record).parameters["value"].annotation is str
assert compiled_callbacks.signature_names(Record) == ["value"]
print("attrs: converter annotation and actual CIL callback")

# 产品专用身份断言：真实CP函数会有CodeType；opaque CIL callable遵循受信缺失边界。
import sys
if "--require-cil" in sys.argv:
    try:
        compiled_callbacks.convert.__code__
    except (AttributeError, NotImplementedError):
        pass
    else:
        raise AssertionError("callback must execute compiled CIL")
