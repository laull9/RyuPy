# ryupy: pending
# 原设计完整 parser 程序，保留所有身份/全局/异常/解析断言。
# 固定 CPython 的公开语义探针；每个模式可单独运行，无 RyuPy 调用。
import sys
import builtins
import importlib.util as util
from importlib.machinery import ModuleSpec
from types import ModuleType
builtins.importlib_design_events = []
def error(label, action):
    try:
        action()
    except Exception as exc:
        cause = exc.__cause__
        print(label, type(exc).__name__, str(exc), 'cause', None if cause is None else type(cause).__name__)
    else:
        raise AssertionError(label + ': 未抛预期异常')

import html.parser as original
import re
s = util.find_spec('html.parser')
m = util.module_from_spec(s)
s.loader.exec_module(m)
assert m.HTMLParser is not original.HTMLParser
assert m.HTMLParser.parse_pi.__globals__ is m.__dict__
assert m.HTMLParser.goahead.__globals__ is m.__dict__
m.piclose = re.compile(r'\?>')
class CopyParser(m.HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=False)
        self.events = []
    def handle_pi(self, text):
        self.events.append(text)
class OriginalParser(original.HTMLParser):
    def __init__(self):
        super().__init__(convert_charrefs=False)
        self.events = []
    def handle_pi(self, text):
        self.events.append(text)
a, b = CopyParser(), OriginalParser()
a.feed('<?probe>')
b.feed('<?probe>')
assert a.events == [] and b.events == ['probe']
a.feed('?>')
assert a.events == ['probe>']
assert original.piclose.pattern == '>'
print('原parser与复制parser真实处理差异', a.events, b.events)