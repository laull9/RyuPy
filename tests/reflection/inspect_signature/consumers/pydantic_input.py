# ryupy: pending
# 真实Pydantic2.14.3/core2.50.4：模型签名生成、validator参数判定和真实校验回调。
import inspect
from typing import Annotated
from pydantic import BeforeValidator, Field, TypeAdapter, ValidationError
from pydantic.fields import FieldInfo
from pydantic._internal._decorators import inspect_validator
from pydantic._internal._signature import generate_pydantic_signature
import compiled_callbacks

generated = generate_pydantic_signature(
    init=compiled_callbacks.model_init,
    fields={"value": FieldInfo(annotation=int, default=4, alias="value_alias")},
    validate_by_alias=True,
    extra="forbid",
)
assert isinstance(generated, inspect.Signature)
assert generated.parameters["value_alias"].kind is inspect.Parameter.KEYWORD_ONLY
assert generated.parameters["value_alias"].default == 4
assert generated.return_annotation is None
assert inspect_validator(compiled_callbacks.convert, mode="before", type="field") is False
adapter = TypeAdapter(Annotated[int, BeforeValidator(compiled_callbacks.convert), Field(gt=0)])
assert adapter.validate_python("42") == 42
try:
    adapter.validate_python("-1")
except ValidationError:
    print("pydantic: validation rejected")
else:
    raise AssertionError("validation must run")
print("pydantic: real signature rebuild and CIL validator")
