# 版本 1 单导出声明也沿用相同的资源发布链路。
import pkgdata
from importlib.resources import as_file, files
from native_math import add

resource = files(pkgdata).joinpath("numbers.txt")
with as_file(resource) as path:
    assert path is resource
    parts = path.read_text(encoding="utf-8").split()
assert resource.is_file()
print(parts[2], add(int(parts[0]), int(parts[1])))
