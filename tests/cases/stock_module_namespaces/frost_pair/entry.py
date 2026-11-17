# ryupy: pending
# 第二组不同模块名与布局：保存闭包、真实全局更新及属性/元组异常目标，禁止按模块名字注册修复。
import ssl
import sys
import frost_state as state
import moss_routes as routes
assert sys.modules[state.__name__] is state
assert globals() is sys.modules[__name__].__dict__
first = state.make(4)
print(state.reveal() is state.__dict__, routes.read(first))
state.__dict__['token'] = 'after'
print(routes.read(first))
state.problem = IndexError
print(routes.match(IndexError), routes.match(ValueError))
state.problem = LookupError
print(routes.match(KeyError))
print(first is first, state.token)
