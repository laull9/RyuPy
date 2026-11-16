# 标准库原源、文件资源与通用 C ABI 共用一次构建和可搬迁分发。
import colorsys
import pkgdata
from importlib.resources import as_file, files
from native_probe import sum_bytes

resource = files(pkgdata).joinpath("data.txt")
with as_file(resource) as path:
    assert path is resource
    data = path.read_bytes()
assert resource.is_file()
print(colorsys.rgb_to_hsv(1.0, 0.0, 0.0))
print(sum_bytes(data))
print(data.decode("utf-8"))
