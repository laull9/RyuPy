# 验收模块/对象属性与保存函数；带副作用实参的点调用顺序仍由另一完整 pending 例覆盖。
import re as patterns

module_alias = patterns
compile_pattern = module_alias.compile
word_pattern = compile_pattern('[a-z]+')
number_pattern = patterns.compile('[0-9]+')
regex_results = word_pattern.fullmatch('source').group(0), number_pattern.fullmatch('123').group(0)
labels = 'compile exec eval'


class Processor:
    def __init__(self, label):
        self.label = label

    def compile(self, source):
        return self.label, 'compiled', source

    def exec(self, value, **options):
        return self.label, value, tuple(options.items())

    def eval(self, value):
        return self.label, value * 2


class Record:
    pass


processor = Processor('original')
other = Processor('other')
saved_compile = processor.compile
saved_exec = processor.exec
saved_eval = processor.eval
saved_results = saved_compile('input'), saved_exec('output'), saved_eval(6)
constant_results = other.compile('next'), other.exec('sent'), other.eval(7)
keyword_result = saved_exec('options', compile=1, exec=2, eval=3)

record = Record()
record.compile = ['source']
record.exec = ['action']
record.eval = ['value']
saved_data = record.compile, record.exec, record.eval
record.compile += ['checked']
record.exec = ['new action']
record.eval += ['read']
updated_data = record.compile, record.exec, record.eval


def replacement(self, source):
    return self.label, 'replacement', source


original_method = Processor.compile
Processor.compile = replacement
changed_result = other.compile('changed')
retained_result = saved_compile('retained')
Processor.compile = original_method
restored_result = other.compile('restored')
processor.exec = other.eval
saved_replacement = processor.exec
replacement_result = saved_replacement(8)
retained_exec_result = saved_exec('retained output')


def use_receiver(re):
    selected = re.compile
    return selected('receiver')


def use_named_receiver(builtins):
    selected = builtins.eval
    return selected(9)


receiver_results = use_receiver(other), use_named_receiver(other)
events = []


class OperationSelector:
    @property
    def compile(self):
        events.append('selected')
        return other.exec


selector = OperationSelector()
selected_operation = selector.compile
selection_events = tuple(events)
selected_result = selected_operation('selected output', compile=4, exec=5, eval=6)
