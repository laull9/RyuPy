# 候选模块只有现有实例AST语法；异常捕获在legacy入口。
import sys
import control
control.attempts += 1
attempt = control.attempts
value = [attempt]
def read():
    return value
control.saved.append(sys.modules[__name__])
control.functions.append(read)
control.dictionaries.append(read.__globals__)
control.events.append(attempt)
partial = 'written'
if attempt == 1:
    raise RuntimeError('ordinary-failure: escaped owner')
completed = True
