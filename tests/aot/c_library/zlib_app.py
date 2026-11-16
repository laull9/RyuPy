# 真实第三方 zlib 公开 C API：固定 1.3.1，直接声明签名，无专用产品代码。
import native_demo

print(native_demo.bound(0))
print(native_demo.bound(4096))
print(native_demo.checksum(0, b"a\x00\xffz"))
print(native_demo.checksum(1234, b""))
