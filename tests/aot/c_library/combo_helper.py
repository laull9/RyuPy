"""CPython载荷反向导入真正的编译C ABI模块，验证注册顺序和函数可见性。"""
import native_demo


def bound(size):
    return native_demo.bound(size)
