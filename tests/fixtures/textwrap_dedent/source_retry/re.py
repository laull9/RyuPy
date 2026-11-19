# 第一次在循环导入后失败；第二次成功，不能回填旧textwrap对象。
import state
import textwrap
state.attempts += 1
state.escaped.append(textwrap)
if state.attempts == 1:
    textwrap.re = state.user_value
    raise state.marker
source_re_marker = "dedent-source-retry"
