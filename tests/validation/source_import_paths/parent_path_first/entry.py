# 首次按字符串导入：空父路径后，资源查询不得绕过真正的父包查找。
import importlib.resources
import sys
import pkg

print("P:data-cached-before", "pkg.data" in sys.modules)
pkg.__path__ = []
print("P:path", pkg.__path__)
print("P:before-files")
importlib.resources.files("pkg.data")
print("P:files-returned")
print("P:data-cached-after", "pkg.data" in sys.modules)
