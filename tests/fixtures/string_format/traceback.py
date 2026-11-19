# ryupy: pending
# module级cause/ambient与完整未捕获用户链；不引入callback内部nested-caught旧债。
events = []
try:
    raise LookupError('template-cause')
except LookupError as error:
    saved_cause = error

def fail():
    events.append('fail')
    raise ValueError('template-body')

class Raises:
    def __format__(self, spec):
        events.append(('slot', spec))
        return fail()

def render(value):
    try:
        return 'prefix {0:spec}'.format(value)
    finally:
        events.append('finally')
        print(events)

def caller():
    return render(Raises())

try:
    raise RuntimeError('template-ambient') from saved_cause
except RuntimeError:
    caller()
