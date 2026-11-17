# 生成器动态目标与异常as绑定不能在yield前预读或在恢复时换对象。
import sys
target = TypeError
events = []
def route():
    try:
        yield 'ready'
    except target as caught:
        events.append(('caught', str(caught), sys.exception() is caught))
        yield ('handler', str(caught), sys.exception() is caught)
        events.append(('resumed', sys.exception() is caught))
    try:
        caught
    except UnboundLocalError:
        events.append('as-cleaned')
    return 23
def getter(holder):
    try:
        try:
            yield 'getter-ready'
        except holder.missing:
            events.append('wrong-first')
        except BaseException:
            events.append('wrong-sibling')
    except AttributeError as error:
        events.append(('outer', type(error).__name__))
        yield 'getter-failed'
