import sys
saved = exit
other = quit
assert saved is exit
assert saved is not other
assert type(saved) is type(other)
assert type(saved).__name__ == 'Quitter'
assert type(saved).__module__ == '_sitebuiltins'
assert repr(type(saved)) == "<class '_sitebuiltins.Quitter'>"
assert saved.name == 'exit' and other.name == 'quit'
assert repr(saved) == 'Use exit() or ' + saved.eof + ' to exit'
assert str(saved) == repr(saved)
original = sys.stdin
log = []
class Input:
    def close(self):
        log.append('close')
sys.stdin = Input()
try:
    saved()
except SystemExit as error:
    assert error.code is None
    assert error.args == (None,)
assert log == ['close']
log.clear()
marker = []
try:
    saved(code=marker)
except SystemExit as error:
    assert error.code is marker
    assert error.args[0] is marker
assert log == ['close']
for case in range(3):
    log.clear()
    try:
        if case == 0:
            saved(1, 2)
        elif case == 1:
            saved(1, code=2)
        else:
            saved(status=1)
    except TypeError:
        pass
    else:
        raise AssertionError('bad binder accepted')
    assert log == []
class BadInput:
    def __init__(self, problem):
        self.problem = problem
    def close(self):
        log.append('bad-close')
        raise self.problem
for problem in [ValueError('close'), SystemExit('close'), KeyboardInterrupt('close'), GeneratorExit('close')]:
    sys.stdin = BadInput(problem)
    try:
        raise RuntimeError('active')
    except RuntimeError as active:
        try:
            saved(marker)
        except SystemExit as error:
            assert error.code is marker
            assert error.__context__ is active
class MissingClose:
    pass
for missing in [None, MissingClose()]:
    sys.stdin = missing
    try:
        saved(8)
    except SystemExit as error:
        assert error.code == 8
class Reentrant:
    def __init__(self):
        self.active = False
    def close(self):
        log.append('enter')
        if not self.active:
            self.active = True
            try:
                other('inner')
            except SystemExit as error:
                assert error.code == 'inner'
                log.append('inner')
log.clear()
sys.stdin = Reentrant()
try:
    saved('outer')
except SystemExit as error:
    assert error.code == 'outer'
assert log == ['enter', 'enter', 'inner']
sys.stdin = original
old_name = saved.name
old_eof = saved.eof
class Text:
    def __init__(self, text):
        self.text = text
    def __str__(self):
        log.append(self.text)
        return self.text
log.clear()
saved.name = Text('leave')
saved.eof = Text('END')
assert repr(saved) == 'Use leave() or END to exit'
assert log == ['leave', 'END']
class BadText:
    def __str__(self):
        raise ValueError('bad-name')
saved.name = BadText()
try:
    repr(saved)
except ValueError as error:
    assert str(error) == 'bad-name'
else:
    raise AssertionError('repr swallowed error')
saved.name = old_name
saved.eof = old_eof
klass = type(saved)
original_call = klass.__call__
def replacement(self, code=None):
    return (self.name, code)
klass.__call__ = replacement
assert exit(1) == ('exit', 1)
assert quit(code=2) == ('quit', 2)
assert saved(3) == ('exit', 3)
klass.__call__ = original_call
def use_parameter(exit, quit):
    return exit(quit)
assert use_parameter(lambda value: value + 1, 4) == 5
def local_shadow():
    exit = 6
    quit = 7
    return exit + quit
assert local_shadow() == 13
# 模块重绑另模块验证，避免同一模块的静态本地发现遮蔽上方真实内建读取。
import shadow
assert shadow.result == 17
print('quitter-core-ok')
