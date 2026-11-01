# 原生模块异常只经真实导入身份解析；模块、from 别名与元组均保留异常层级。
import json as codec
import re as regex
from json import JSONDecodeError as DecodeError
from re import error as PatternError

for kind in [0, 1]:
    try:
        if kind == 0:
            codec.loads("[")
        else:
            regex.compile("[")
    except (DecodeError, PatternError) as caught:
        print(isinstance(caught, Exception))

def decode():
    try:
        codec.loads("{")
    except codec.JSONDecodeError as error:
        print(isinstance(error, ValueError), error.pos)

def pattern():
    try:
        regex.compile("(")
    except regex.error as error:
        print(isinstance(error, Exception), error.pos)

decode()
pattern()

# 生成器的逻辑 handler 与普通 CLR typed catch 使用同一模块异常身份。
def suspended():
    try:
        codec.loads("[")
    except codec.JSONDecodeError as error:
        yield error.pos
        try:
            regex.compile("(")
        except PatternError as nested:
            yield nested.pos

print(list(suspended()))

# 用户同名异常类不能覆盖模块异常的内部 CLR 身份。
class error(Exception):
    pass

class JSONDecodeError(Exception):
    pass

for kind in [0, 1]:
    try:
        if kind == 0:
            raise error("user")
        raise JSONDecodeError("user")
    except (regex.error, codec.JSONDecodeError):
        print("wrong native match")
    except Exception:
        print("user remains separate")

# 定义同名类之后重新执行普通/可恢复原生捕获。
decode()
pattern()
print(list(suspended()))

def distinguish():
    try:
        raise error("user")
    except regex.error:
        yield "wrong native match"
    except Exception:
        yield "user generator separate"

print(list(distinguish()))
