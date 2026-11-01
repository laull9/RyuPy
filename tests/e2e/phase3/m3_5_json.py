# JSON：顺序、大整数、转义、错误位置与文件协议，所有文件都使用内存流。
import json
import io

value = {"猫": [10**30, True, None, -0.0], "later": 2}
print(json.dumps(value))
print(json.dumps(value, ensure_ascii=False, separators=(",", ":")))
print(json.loads(json.dumps(value)) == value)
print(json.dumps({"b": 2, "a": [1, 3]}, sort_keys=True, indent=2))
print(json.loads('{"a":1,"b":2,"a":3}'))
print(json.loads(b'{"x":1}'))
print(json.loads('"\\ud83d\\ude00"'))
print(repr(json.loads('"a\\n\\t\\\\\\\"b"')))
print(json.dumps([float("nan"), float("inf"), -float("inf")]))
try:
    json.dumps(float("nan"), allow_nan=False)
except ValueError:
    print("nan rejected")
for source in ['{"a":}', '[1,]', '01', 'true false', '"bad\nline"']:
    try:
        json.loads(source)
    except json.JSONDecodeError as error:
        print(error.msg, error.pos, error.lineno, error.colno, error.doc == source)
stream = io.StringIO()
print(json.dump(value, stream), stream.getvalue())
stream.seek(0)
print(json.load(stream) == value)
cycle = []
cycle.append(cycle)
try:
    json.dumps(cycle)
except ValueError:
    print("cycle")
print(json.dumps({1: "one", None: "none", False: "false"}))
try:
    json.dumps({(1,): 2})
except TypeError:
    print("key rejected")
print(json.dumps({(1,): 2, "ok": 3}, skipkeys=True))

# dump按块写出；后项编码失败不能撤销此前已写前缀。
class Sink:
    def __init__(self):
        self.parts = []
    def write(self, value):
        self.parts.append(value)

class Token:
    pass

sink = Sink()
json.dump([1, {"猫": 2}, 3], sink, ensure_ascii=False)
print(sink.parts)
sink = Sink()
try:
    json.dump([1, Token()], sink)
except TypeError:
    print(sink.parts)
print(json.loads('{"x":1,"x":2}', object_pairs_hook=lambda pairs: pairs))
print(json.loads('[1,1.5,NaN]', parse_int=lambda x: "int:" + x,
                 parse_float=lambda x: "float:" + x, parse_constant=lambda x: "constant:" + x))
print(json.dumps(Token(), default=lambda value: {"custom": True}))
print(json.loads('"a\n"', strict=False))

# Unicode码点位置、编码检测、钩子异常与严格整数边界。
for source in ['["😀",\n?]', '"\\u12x4"', '"unterminated', '[-]', '[1e+]']:
    try:
        json.loads(source)
    except json.JSONDecodeError as error:
        print(error.msg, error.pos, error.lineno, error.colno)
print(json.loads(b'\xef\xbb\xbf[1]'))
print(json.loads(b'\xff\xfe[\x001\x00]\x00'))
print(json.loads('{"a":1}', object_hook=lambda value: list(value.items())))
try:
    json.dumps({"a": 1, 2: 3}, sort_keys=True)
except TypeError:
    print("mixed keys")
try:
    json.loads('1', parse_int=lambda value: 1 // 0)
except ZeroDivisionError:
    print("hook error")
print(json.dumps({"😀": 1, "\ue000": 2, "a": 3}, sort_keys=True))

# 每一块读取当前write，不提前绑定或先验证缺失write。
events = []
class ChangingSink:
    def write(self, chunk):
        events.append("first:" + chunk)
        self.write = lambda next_chunk: events.append("later:" + next_chunk)
json.dump([1, 2], ChangingSink())
print(events)
events = []
try:
    json.dump(Token(), Token(), default=lambda value: events.append("default"))
except AttributeError:
    print(events)
print(json.dumps("\x7f/\u2028\x1f"))
print(repr(json.dumps("\x7f/\u2028\x1f", ensure_ascii=False)))
try:
    json.dumps({10**4300: 1})
except ValueError:
    print("large integer key")
constructed = json.JSONDecodeError("custom", "😀x", 1)
print(callable(json.JSONDecodeError), constructed.pos, constructed.colno)
try:
    raise constructed
except json.JSONDecodeError as caught:
    print(caught is constructed, isinstance(caught, ValueError), str(caught))
print(type(constructed) is json.JSONDecodeError, json.JSONDecodeError.__module__)
