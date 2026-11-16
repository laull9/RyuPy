# 属性准入不是动态代码能力；不要用本地同名 builtins.py 或宿主假对象绕过真实导入。
import builtins as b

global_value = 10
direct_value = b.eval('global_value + 1')
compiled = b.compile('global_value + 2', '<instance-code-attribute>', 'eval')
compiled_value = b.eval(compiled)
namespace = {'value': 4}
b.exec('value += 3', namespace)
alias = b.eval
aliased_value = alias('global_value + 3')
reflected = getattr(b, 'eval')
reflected_value = reflected('global_value + 4')
compile_alias = getattr(b, 'compile')
exec_alias = getattr(b, 'exec')
exec_alias(compile_alias('value += 2', '<reflected-code-attribute>', 'exec'), namespace)


def local_environment():
    local_value = 20
    return b.eval('local_value + global_value')


local_result = local_environment()
