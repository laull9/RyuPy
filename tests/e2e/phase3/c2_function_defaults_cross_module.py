# 跨模块给原typed函数新增异型默认值；缺省补齐后使用同一身份的通用伴生核心。
import c2_function_defaults_exported_helper as exported

exported.required.__defaults__ = ("cross-module",)
print(exported.required())
