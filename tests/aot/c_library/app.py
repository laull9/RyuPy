# 通用动态 C ABI：同一输入经过 RyuPy 入口调用真实库和带相邻依赖的自有库。
import native_demo

print(native_demo.absolute(-123))
print(native_demo.measure(b"a\x00\xffz"))
print(native_demo.measure(b""))
print(native_demo.add(20, 22))
