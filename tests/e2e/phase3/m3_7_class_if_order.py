# 类体条件按源序执行，方法默认值、标注与装饰器只在实际定义分支求值。
events = []


def record(label, value):
    events.append(label)
    return value


def decorate(label):
    events.append(label)

    def apply(function):
        events.append('apply')
        return function

    return apply


class Choice:
    events.append('start')
    if record('first test', False):
        events.append('wrong first body')
        value = 99
    elif record('second test', True):
        value = record('selected value', 7)
        annotation = int
        if record('nested test', True):
            value = record('nested value', value + 1)
        else:
            events.append('wrong nested body')

        @decorate('decorator')
        def method(self, item: record('annotation', annotation) = record('default', value)) -> annotation:
            return item
    else:
        events.append('wrong last body')
        def method(self, item=99):
            return item
    events.append('end')
    after = value


print(events)
print(Choice().method(), Choice.after)
print(Choice.method.__annotations__ == {'item': int, 'return': int})


class OtherChoice:
    if record('other test', False):
        def method(self, item=record('wrong default', 1)):
            return item
    else:
        value = 12
        annotation = str
        def method(self, item: annotation = record('other default', value)):
            return item


print(OtherChoice().method(), OtherChoice.method.__annotations__ == {'item': str})
print(events[-2:])
