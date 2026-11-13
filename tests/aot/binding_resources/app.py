# 同次发布读取包数据，并把数据作为两个真实 C 符号的参数。
import pkgdata
from importlib.resources import files
from native_math import add, subtract

text = files(pkgdata).joinpath("numbers.txt").read_text(encoding="utf-8")
parts = text.split()
left = int(parts[0])
right = int(parts[1])
print(parts[2])
print(add(left, right), subtract(left, right))
print(pkgdata.__file__ == pkgdata.__spec__.origin)
print("binding-resources-ok")
