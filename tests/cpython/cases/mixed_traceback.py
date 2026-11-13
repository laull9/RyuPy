# ryupy: pending
"""CP1b：未捕获的编译回调异常经解释域包装，保留异常链与双域栈。"""
import bridge_fixture


def compiled_leaf():
    raise ValueError('跨域叶异常')


def compiled_entry():
    bridge_fixture.chain(compiled_leaf)


compiled_entry()
