# ryupy: pending
# 模块级已捕获cause与完整外层用户链；不触及旧嵌套caught traceback债。
events = []
try:
    raise ValueError('format-inner')
except ValueError as error:
    saved_cause = error

class Raises:
    def __format__(self, spec):
        events.append('slot')
        raise RuntimeError('format-outer') from saved_cause

def convert(value):
    try:
        return format(value, '')
    finally:
        events.append('finally')
        print(events)

def caller():
    return convert(Raises())

caller()
