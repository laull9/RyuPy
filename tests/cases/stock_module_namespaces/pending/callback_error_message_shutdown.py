# ryupy: pending
"""混合 handler 再跨域读异常文本；业务值可一致，carrier 关闭计数仍待验证。"""
import json


def reject(payload):
    raise ValueError("回调主动失败")


try:
    json.loads('{"unused": 31}', object_hook=reject)
except ValueError as error:
    assert str(error) == "回调主动失败"
else:
    raise AssertionError("异常未越过回调边界")
