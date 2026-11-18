# 路由来自真实保护区，handler正文的异常只交外层，close沿同一清理路线。
import contextlib
import sys
events = []
def read(values, holder):
    with contextlib.nullcontext():
        for value in values:
            try:
                if value == 2:
                    raise OSError('skip')
                yield value
            except (holder.failure, ValueError):
                events.append(('continue', value))
                continue
def suspended():
    try:
        try:
            yield 'quarry-ready'
        except OSError as caught:
            events.append(('active', sys.exception() is caught))
            try:
                yield str(caught)
            finally:
                events.append(('leave-handler', sys.exception() is caught))
    finally:
        events.append('closed')
