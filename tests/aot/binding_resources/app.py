# 同次发布读取包数据，并把数据作为两个真实 C 符号的参数。
import pkgdata
from importlib.resources import as_file, files
from native_math import add, subtract

resource = files(pkgdata).joinpath("numbers.txt")
context = as_file(resource)
context.__enter__ = lambda: None
context.__exit__ = lambda *args: True
with context as path:
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
# 装饰器必须调用公开重建入口，并读取该上下文部署后的真实资源。
decorator = as_file(resource)
recreated = []
def recreate():
    recreated.append(True)
    return as_file(resource)
decorator._recreate_cm = recreate
@decorator
def read_again():
    return resource.read_text(encoding="utf-8")
assert read_again() == text
assert len(recreated) == 1
parts = text.split()
left = int(parts[0])
right = int(parts[1])
print(parts[2])
print(add(left, right), subtract(left, right))
print(pkgdata.__file__ == pkgdata.__spec__.origin)
print("binding-resources-ok")
