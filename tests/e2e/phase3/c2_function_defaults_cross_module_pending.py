# 单模块类型检查无法看见其它模块对原无默认值函数的变更；保留跨模块真实阻断用例。
import c2_function_defaults_exported_helper as exported

exported.required.__defaults__ = ("cross-module",)
print(exported.required())
