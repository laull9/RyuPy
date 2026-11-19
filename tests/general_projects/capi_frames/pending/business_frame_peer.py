# 普通CIL函数的完整frame观察；globals与定义点builtins已有合同，locals/name/当前行号继续差分。
import native_scope_probe


def read_frame(token):
    business = 17
    observed = native_scope_probe.facts()
    return (observed[0] is globals(), observed[1].get('business'), observed[3].co_name, observed[4].f_lineno)
