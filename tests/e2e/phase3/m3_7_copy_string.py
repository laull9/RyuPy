# 容器复制保留环与共享引用；不可变 tuple 的身份按递归结果决定。
import copy
import string

source = []
source.append(source)
clone = copy.deepcopy(source)
print(clone is not source, clone[0] is clone)
shared = [1]
original = [shared, shared]
deep = copy.deepcopy(original)
shallow = copy.copy(original)
print(deep[0] is deep[1], deep[0] is shared, shallow[0] is shared)
tuple_value = (1, 'a', (2, 3))
print(copy.copy(tuple_value) is tuple_value, copy.deepcopy(tuple_value) is tuple_value)
mutable_tuple = (shared, shared)
copied_tuple = copy.deepcopy(mutable_tuple)
print(copied_tuple is mutable_tuple, copied_tuple[0] is copied_tuple[1])
cycle_list = []
cycle_tuple = (cycle_list,)
cycle_list.append(cycle_tuple)
result = copy.deepcopy(cycle_tuple)
print(result[0][0] is result)
mapping = {'one': shared, 'two': shared}
new_mapping = copy.deepcopy(mapping)
print(new_mapping['one'] is new_mapping['two'], new_mapping['one'] is shared)
mapping['self'] = mapping
print(copy.deepcopy(mapping)['self']['self'] is not mapping)
print(string.ascii_lowercase, string.ascii_uppercase, string.ascii_letters)
print(string.digits, string.hexdigits, string.octdigits)
print(repr(string.whitespace), string.punctuation)
print(string.printable == string.digits + string.ascii_letters + string.punctuation + string.whitespace)
