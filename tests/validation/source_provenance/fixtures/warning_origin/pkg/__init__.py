# 该帧的 basename 与未执行的空 data 包相同。
import warnings
import importlib.resources
import sys


def unused():
    print("W:unused-called")
    return importlib.resources.files("pkg.data")


print("W:data-cached", "pkg.data" in sys.modules)
print("W:before-warn")
warnings.warn("origin-probe")
print("W:after-warn")
