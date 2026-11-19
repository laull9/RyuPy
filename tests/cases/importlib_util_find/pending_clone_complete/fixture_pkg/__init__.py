# 普通包初始化的可观察副作用。
import builtins
builtins.importlib_design_events.append('parent')
