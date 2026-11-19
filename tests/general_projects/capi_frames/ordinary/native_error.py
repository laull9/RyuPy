# 原生扩展错误穿过CIL入口，异常的CP traceback不能持根至关闭后。
import native_scope_probe
native_scope_probe.fail()
