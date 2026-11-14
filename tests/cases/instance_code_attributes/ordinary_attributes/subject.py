# compile/exec/eval 属性不读取环境裸名；任意 receiver 都走已有对象协议。
import re as regex

events = []
text = 'compile exec eval globals locals super __class__'


class Box:
    def __init__(self, tag):
        self.tag = tag

    def compile(self, value=0, **values):
        events.append(('call', self.tag, value))
        return self.tag, value, tuple(values.items())

    def exec(self, value):
        return 'exec', self.tag, value

    def eval(self, value):
        return 'eval', self.tag, value


class PropertyBox:
    @property
    def compile(self):
        events.append('property')
        return callback


def callback(value):
    events.append(('callback', value))
    return value + 1


box = Box('first')
other = Box('second')
property_box = PropertyBox()
saved = box.compile
alias_result = saved(3)
keyword_result = box.compile(value=7, compile=8, exec=9, eval=10)
method_results = box.exec(4), box.eval(5)
box.exec = other.eval
rebound_result = box.exec(6)
box.eval = ['first']
box.eval += ['second']
stored_value = box.eval
pattern = regex.compile('a+')
regex_result = pattern.fullmatch('aaa').group(0)
module_alias = regex
compiled_alias = module_alias.compile
regex_alias_result = compiled_alias('b+').fullmatch('bbb').group(0)


def shadow_receiver(re):
    return re.compile(11)


def shadow_builtin_receiver(builtins):
    return builtins.compile(12)


shadow_result = shadow_receiver(other), shadow_builtin_receiver(other)


def replacement(self, value):
    return 'replacement', self.tag, value


original = Box.compile
Box.compile = replacement
changed_result = other.compile(13)
saved_after_change = saved(14)
Box.compile = original
restored_result = other.compile(15)
box.compile = callback
instance_result = box.compile(16)


def receiver():
    events.append('receiver')
    return property_box


def argument():
    events.append('argument')
    return 20


events.clear()
ordered_result = receiver().compile(argument())
ordered_events = tuple(events)


def missing_attribute():
    return ['not a code object'].compile


def failing_receiver():
    events.append('failed receiver')
    raise ValueError('receiver failed')


def failed_call():
    return failing_receiver().eval(argument())


def failed_lookup():
    return ['not a code object'].exec(argument())
