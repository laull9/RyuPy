# 同次发布读取包数据，并把数据作为两个真实 C 符号的参数。
import pkgdata
from importlib.resources import as_file, files
from native_math import add, subtract

resource = files(pkgdata).joinpath("numbers.txt")
with as_file(resource) as path:
    assert path is resource
    text = path.read_text(encoding="utf-8")
assert resource.is_file()
# 异常退出同样保留已部署数据，并传播原异常对象。
error = ValueError("resource-context")
try:
    with as_file(resource) as path:
        assert path is resource
        raise error
except ValueError as caught:
    assert caught is error
assert resource.read_text(encoding="utf-8") == text
parts = text.split()
left = int(parts[0])
right = int(parts[1])
print(parts[2])
print(add(left, right), subtract(left, right))
print(pkgdata.__file__ == pkgdata.__spec__.origin)
print("binding-resources-ok")
