# ryupy: pending
# 真实FastAPI0.158.1：解析原生CIL依赖签名、真实globals、OpenAPI、请求校验与回调。
import inspect
from fastapi import Depends, FastAPI
from fastapi.dependencies.utils import get_dependant, get_typed_signature
from fastapi.testclient import TestClient
import compiled_callbacks

signature = get_typed_signature(compiled_callbacks.dependency)
assert isinstance(signature, inspect.Signature)
assert signature.parameters["value"].annotation is int
assert signature.parameters["value"].default == 1
dependant = get_dependant(path="/value", call=compiled_callbacks.dependency)
assert [field.name for field in dependant.query_params] == ["value"]
assert compiled_callbacks.dependency.__globals__ is compiled_callbacks.__dict__
app = FastAPI()


@app.get("/value")
def route(answer: int = Depends(compiled_callbacks.dependency)):
    return {"value": answer}


with TestClient(app) as client:
    assert client.get("/value?value=41").json() == {"value": 42}
    assert client.get("/value?value=bad").status_code == 422
    schema = client.get("/openapi.json").json()
    parameters = schema["paths"]["/value"]["get"]["parameters"]
    assert parameters[0]["name"] == "value"
    assert parameters[0]["schema"]["default"] == 1
assert list(compiled_callbacks.calls) == [41]
print("fastapi: signature globals OpenAPI validation and CIL dependency")

# 产品专用身份断言：真实CP函数会有CodeType；opaque CIL callable遵循受信缺失边界。
import sys
if "--require-cil" in sys.argv:
    try:
        compiled_callbacks.convert.__code__
    except (AttributeError, NotImplementedError):
        pass
    else:
        raise AssertionError("callback must execute compiled CIL")
