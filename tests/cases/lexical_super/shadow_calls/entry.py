# canonical 别名的后续成功证明隐式类 cell 真实存在。
import subject
obj = subject.Child()
assert obj.parameter(lambda: 'parameter user') == ('parameter user', obj)
assert obj.local() == ('local user', obj)
assert obj.global_value() == ('global user', obj)
assert subject.Child.no_arguments() == 'no arguments'
print('parameter, local and global super shadows preserve lexical cells')
