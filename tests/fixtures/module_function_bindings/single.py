# 固定参考14个单模块组；只给各组模块绑定加独占前缀，避免组间污染。

# selected_unbound_before_def

case0_events = []
try:
    case0_f
except NameError as case0_error:
    case0_events.append(['unbound', str(case0_error)])
def case0_f():
    return 'created'
case0_events.append(case0_f())
case0_f = 'later'
case0_result = [case0_events, case0_f]
print('selected_unbound_before_def', case0_result)

# assigned_value_and_definition_default

case1_f = ['old']
case1_previous = case1_f
def case1_f(value=case1_f):
    return value
case1_saved = case1_f
case1_f = 'new binding'
case1_result = [case1_saved() is case1_previous, case1_saved(), case1_f]
print('assigned_value_and_definition_default', case1_result)

# for_collision_and_generator_publication

case2_lengths = []
for case2_values in [[], [1], [1, 2]]:
    case2_lengths.append(len(case2_values))
def case2_values(start):
    yield start
    yield start + 1
case2_result = [case2_lengths, list(case2_values(7))]
print('for_collision_and_generator_publication', case2_result)

# callee_is_frozen_before_argument_rebinding

case3_events = []
def case3_f(value):
    case3_events.append(['old body', value])
    return 'old result'
case3_saved = case3_f
def case3_replacement(value):
    case3_events.append(['new body', value])
    return 'new result'
def case3_argument():
    global case3_f
    case3_events.append('argument')
    case3_f = case3_replacement
    return 5
case3_first = case3_f(case3_argument())
case3_second = case3_f(6)
case3_third = case3_saved(7)
case3_result = [case3_first, case3_second, case3_third, case3_f is case3_replacement, case3_events]
print('callee_is_frozen_before_argument_rebinding', case3_result)

# future_body_reads_current_module_value

def case4_observe():
    return case4_future
case4_future = 'old value'
case4_first = case4_observe()
def case4_future():
    return 'new function'
case4_second = case4_observe() is case4_future
case4_future = 'last value'
case4_result = [case4_first, case4_second, case4_observe()]
print('future_body_reads_current_module_value', case4_result)

# global_write_and_alias

def case5_f():
    return 'old'
case5_saved = case5_f
def case5_replacement():
    return 'replacement'
def case5_change():
    global case5_f
    case5_f = case5_replacement
case5_change()
case5_result = [case5_f is case5_replacement, case5_saved is not case5_f, case5_f(), case5_saved()]
print('global_write_and_alias', case5_result)

# repeated_def_keeps_old_defaults_independent

def case6_f(value=[]):
    value.append('old')
    return value
case6_saved = case6_f
case6_old_values = case6_saved()
def case6_f(value=[]):
    value.append('new')
    return value
case6_new_values = case6_f()
case6_saved()
case6_f()
case6_result = [case6_saved is not case6_f, case6_old_values is not case6_new_values,
          case6_saved() is case6_old_values, case6_f() is case6_new_values, case6_old_values, case6_new_values]
print('repeated_def_keeps_old_defaults_independent', case6_result)

# saved_recursive_function_uses_late_global_name

def case7_recurse(number):
    if number == 0:
        return 0
    return case7_recurse(number - 1) + 1
case7_saved = case7_recurse
case7_first = case7_saved(3)
def case7_replacement(number):
    return 40
case7_recurse = case7_replacement
case7_result = [case7_first, case7_saved(2), case7_recurse(2), case7_saved is not case7_recurse]
print('saved_recursive_function_uses_late_global_name', case7_result)

# failed_defaults_and_annotations_keep_old_binding

case8_events = []
case8_f = 'original value'
def case8_fail():
    case8_events.append('fail')
    raise RuntimeError('definition failed')
try:
    def case8_f(value=case8_fail()):
        return value
except RuntimeError as case8_error:
    case8_events.append([str(case8_error), case8_f])
def case8_f():
    return 'published'
case8_saved = case8_f
try:
    def case8_f(value=case8_fail()):
        return value
except RuntimeError:
    case8_events.append(['default preserved', case8_f is case8_saved])
try:
    def case8_f(value: case8_fail()):
        return value
except RuntimeError:
    case8_events.append(['annotation preserved', case8_f is case8_saved])
case8_result = [case8_f is case8_saved, case8_f(), case8_events]
print('failed_defaults_and_annotations_keep_old_binding', case8_result)

# decorator_failure_preserves_binding_and_order

case9_events = []
case9_f = 'old binding'
def case9_reject(function):
    case9_events.append('apply decorator')
    raise RuntimeError('rejected')
def case9_decorator():
    case9_events.append('decorator expression')
    return case9_reject
def case9_default():
    case9_events.append('default expression')
    return []
try:
    @case9_decorator()
    def case9_f(value=case9_default()):
        return value
except RuntimeError as case9_error:
    case9_events.append([str(case9_error), case9_f])
def case9_f():
    return 'final'
case9_result = [case9_events, case9_f()]
print('decorator_failure_preserves_binding_and_order', case9_result)

# except_clear_with_and_augmented_binding

case10_events = []
case10_f = 100
try:
    raise ValueError('caught')
except ValueError as case10_f:
    case10_events.append(str(case10_f))
try:
    case10_f
except NameError as case10_error:
    case10_events.append(['cleared', str(case10_error)])
case10_f = 3
case10_f += 4
case10_events.append(case10_f)
class case10_Enter:
    def __enter__(self):
        return 8
    def __exit__(self, kind, value, trace):
        return False
with case10_Enter() as case10_f:
    case10_events.append(case10_f)
def case10_f():
    return 'published'
case10_result = [case10_events, case10_f()]
print('except_clear_with_and_augmented_binding', case10_result)

# annotated_selected_function_accepts_python_objects

case11_f = 0
def case11_f(value: int) -> int:
    return value
case11_saved = case11_f
case11_wide = 18446744073709551616
case11_marker = []
case11_result = [case11_f('text'), case11_saved(case11_wide), case11_f(True) is True, case11_saved(case11_marker) is case11_marker]
print('annotated_selected_function_accepts_python_objects', case11_result)

# import_alias_then_definition

from math import sqrt as case12_f
case12_first = case12_f(9)
def case12_f(value):
    return ['new f', value]
import math as case12_g
case12_second = case12_g.sqrt(16)
def case12_g(value):
    return ['new g', value]
case12_result = [case12_first, case12_second, case12_f(5), case12_g(6)]
print('import_alias_then_definition', case12_result)

# method_body_observes_late_module_binding

class case15_Reader:
    def read(self):
        return case15_f
case15_reader = case15_Reader()
case15_f = 'before'
case15_first = case15_reader.read()
def case15_f():
    return 'function'
case15_second = case15_reader.read() is case15_f
case15_f = 'after'
case15_result = [case15_first, case15_second, case15_reader.read()]
print('method_body_observes_late_module_binding', case15_result)
