# ryupy: pending
# 错误正文顺序与原对象；结尾未捕获错误保全部用户帧和模块级context链。
from html import escape


class Stop:
    def __init__(self, at):
        self.at = at
        self.count = 0

    def replace(self, old, new):
        self.count += 1
        events.append(('replace', self.count))
        if self.count == self.at:
            raise marker
        return self


class BadQuote:
    def __bool__(self):
        events.append(('bool',))
        raise marker


class WrongBool:
    def __bool__(self):
        events.append(('bool',))
        return 1


class NegativeLength:
    def __len__(self):
        events.append(('len',))
        return -1


class GetterError:
    @property
    def replace(self):
        events.append(('get',))
        raise marker


class Noncallable:
    replace = 7


class Link:
    def __init__(self, following):
        self.following = following

    def replace(self, old, new):
        events.append(('link', old, new))
        return self.following


for at in [1, 2, 3, 4, 5]:
    marker = RuntimeError('replace-marker')
    events = []
    try:
        escape(Stop(at))
    except RuntimeError as error:
        assert error is marker and str(error) == 'replace-marker'
        assert events == [('replace', n) for n in range(1, at + 1)]
        print('H3.replace-' + str(at), type(error).__name__, str(error),
              error is marker, events)
    else:
        raise AssertionError('replace error expected')

missing = "'NoneType' object has no attribute 'replace'"
for label, subject, quote, name, message, expected_events, same in [
    ('early-skips-quote', Stop(1), BadQuote(), 'RuntimeError', 'identity-marker',
     [('replace', 1)], True),
    ('getter', GetterError(), BadQuote(), 'RuntimeError', 'identity-marker',
     [('get',)], True),
    ('bool-marker', Stop(99), BadQuote(), 'RuntimeError', 'identity-marker',
     [('replace', 1), ('replace', 2), ('replace', 3), ('bool',)], True),
    ('wrong-bool', Stop(99), WrongBool(), 'TypeError',
     '__bool__ should return bool, returned int',
     [('replace', 1), ('replace', 2), ('replace', 3), ('bool',)], False),
    ('negative-len', Stop(99), NegativeLength(), 'ValueError',
     '__len__() should return >= 0',
     [('replace', 1), ('replace', 2), ('replace', 3), ('len',)], False),
    ('missing-first', None, BadQuote(), 'AttributeError', missing, [], False),
    ('noncallable', Noncallable(), BadQuote(), 'TypeError',
     "'int' object is not callable", [], False),
    ('missing-second', Link(None), BadQuote(), 'AttributeError', missing,
     [('link', '&', '&amp;')], False),
    ('missing-fourth', Link(Link(Link(None))), True, 'AttributeError', missing,
     [('link', '&', '&amp;'), ('link', '<', '&lt;'), ('link', '>', '&gt;')], False),
]:
    marker = RuntimeError('identity-marker')
    events = []
    try:
        escape(subject, quote)
    except Exception as error:
        assert type(error).__name__ == name and str(error) == message
        assert (error is marker) == same and events == expected_events
        print('H3.' + label, type(error).__name__, str(error),
              error is marker, events)
    else:
        raise AssertionError(label + ' must raise')


# 显式cause在用户代码产生；escape不得换异常或丢失连接。
class WithCause:
    def replace(self, old, new):
        raise marker from cause


cause = ValueError('direct-cause')
marker = LookupError('with-cause')
try:
    escape(WithCause())
except LookupError as error:
    assert error is marker and error.__cause__ is cause
    assert error.__suppress_context__
    print('H3.cause', error is marker, error.__cause__ is cause,
          type(error).__name__, str(error))
else:
    raise AssertionError('cause error expected')

# 只在模块级捕获，避免把既有嵌套函数catch帧欠账混进本API。
active = ValueError('caught-context')
marker = RuntimeError('caught-inner')
events = []
try:
    raise active
except ValueError:
    try:
        escape(Stop(1))
    except RuntimeError as error:
        assert error is marker and error.__context__ is active
        assert error.__cause__ is None and not error.__suppress_context__
        print('H3.context', error is marker, error.__context__ is active,
              type(error).__name__, str(error), events)
    else:
        raise AssertionError('context error expected')


class ChainFailure:
    def replace(self, old, new):
        events.append((old, new))
        print('H3.chain-callback', events)
        return leaf()


def leaf():
    raise LookupError('replace chain failed')


def middle(value):
    return escape(value)


def outer(value):
    return middle(value)


events = []
try:
    raise ValueError('outer context')
except ValueError:
    outer(ChainFailure())
