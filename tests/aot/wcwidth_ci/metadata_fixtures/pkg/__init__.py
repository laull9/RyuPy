# 包路径是非文件系统空列表；先发布再执行，使循环导入可见同一对象。
import state
import pkg
assert pkg.__path__ is __path__
assert __path__ == []
state.attempts += 1
if state.attempts == 1:
    state.first_path = __path__
    __path__.append("not-a-source")
    raise ValueError("retry")
assert state.first_path is not __path__
