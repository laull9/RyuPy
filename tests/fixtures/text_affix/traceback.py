# ryupy: pending
# 模块级cause/ambient及完整index用户链；finally输出固定事件，不掩盖任何用户帧。
events = []
try:
    raise LookupError('affix-cause')
except LookupError as error:
    saved_cause = error

def fail():
    events.append('fail')
    raise ValueError('affix-index-body')

class Broken:
    def __index__(self):
        events.append('end')
        return fail()

class Start:
    def __index__(self):
        events.append('start')
        return 0

def match_window():
    try:
        return 'abc'.startswith('a', Start(), Broken())
    finally:
        events.append('finally')
        print('trace-events', events)

def caller():
    return match_window()

try:
    raise RuntimeError('affix-ambient') from saved_cause
except RuntimeError:
    caller()
