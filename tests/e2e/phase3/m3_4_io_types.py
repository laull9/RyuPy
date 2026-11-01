# 流构造器是真实类型对象，类型身份/基类检查不把工厂函数当类。
import io

text = io.StringIO("value")
raw = io.BytesIO(b"value")
print(type(text) is io.StringIO, type(raw) is io.BytesIO)
print(isinstance(text, io.TextIOBase), isinstance(raw, io.BufferedIOBase))
print(isinstance(text, io.IOBase), isinstance(raw, io.IOBase))
print(issubclass(io.StringIO, io.TextIOBase), issubclass(io.BytesIO, io.IOBase))
print(io.StringIO.__name__, io.StringIO.__module__, repr(io.StringIO))
print(callable(io.StringIO))
