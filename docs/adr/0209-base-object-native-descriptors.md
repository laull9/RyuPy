# ADR-0209：基础 object 槽直接调用原生描述符

- 日期：2026-11-16
- 状态：定向类桥、macOS arm64 JIT/NativeAOT 搬迁及完整 make check 通过
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md) 的 C1 首切

## 问题

跨域类桥通过 `_rp_object_new`、`_rp_object_init`、`_rp_object_setattr` 和
`_rp_object_delattr` 四个 Python 函数转发基础对象操作。这些函数没有自己的语言语义，
仍增加 Python 执行帧，并把编译类的基础槽调用绑定到源码 helper。

## 决定

通过已有静态 C API `rp_builtin` 和 `rp_op(GetAttr)` 取得 `object` 的
`__new__`、`__init__`、`__setattr__`、`__delattr__` 原生描述符，由 CIL 类桥缓存
拥有的新引用。现有参数元组、关键字字典、异常转换及确定释放路径继续承担调用。

直接调用描述符保留 CPython 原生参数绑定、类型布局检查和错误消息；尤其不得绕过
`object.__new__` 对 deque 等扩展布局的拒绝。setattr/delattr 的基础槽语义仍绕过用户
钩子，但数据描述符及属性错误由对象协议处理。这里不直接写实例字典。

新增代码不扩展 RuntimeFn、MIR 或原生 ABI，不使用动态 IL、反射发射或 Python 替代载荷。
描述符与其他类桥缓存一起关闭释放，临时 object 类型引用在取得描述符后立即释放。

## 验收与边界

真实解释器测试覆盖分配所得编译实例的身份、基础初始化、同一对象属性引用、删除后读取
及重复删除异常，以及扩展布局非法分配。已有多层继承、super、property 和真实 Tk
回调测试一并回归。2026-11-16 单继承 `foreign_object_slots` 已通过固定 CPython 3.12.13
同次差分、JIT、NativeAOT，以及删除私有输入和构建目录后的中文空格路径搬迁。
完整 `foreign_class_inheritance` 的 JIT 已通过，普通多重继承仍被既有 AOT 门拒绝。

本切片移除四个 Python 转发函数。super 两个 helper 已由真实原生 super 接管；导入 finder/loader、类构建与默认方法 wrapper
仍在 C1 后续范围内；初始化 frozen 链与第三方 Python 执行继续由 C0/C4 处理。
执行审计需实际观察这些路径，严格产品准入同时检查生命周期覆盖和帧计数。
