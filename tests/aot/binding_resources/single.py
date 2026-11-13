# 版本 1 单导出声明也沿用相同的资源发布链路。
import pkgdata
from importlib.resources import files
from native_math import add

parts = files(pkgdata).joinpath("numbers.txt").read_text(encoding="utf-8").split()
print(parts[2], add(int(parts[0]), int(parts[1])))
