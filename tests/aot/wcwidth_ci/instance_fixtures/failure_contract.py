# 第三次装饰失败：原模块已发布的函数逃逸、旧globals及定义时builtins不得丢失。
import sys
import functools
import importlib.util as util
import wcwidth as package
import wcwidth.wcwidth
m = sys.modules['wcwidth.wcwidth']
spec = util.find_spec('wcwidth.wcwidth')
left = util.module_from_spec(spec)
spec.loader.exec_module(left)
old = left.wcwidth
old_match = left._wcmatch_version
old_globals = left.__dict__
old('汉')
original_decorator = functools.lru_cache

def fail_third(maxsize):
    if maxsize == 8:
        raise ValueError('third-decorator')
    return original_decorator(maxsize=maxsize)

functools.lru_cache = fail_third
try:
    spec.loader.exec_module(left)
except ValueError as error:
    print('failure', str(error))
finally:
    functools.lru_cache = original_decorator
assert left.__dict__ is old_globals
assert left.wcwidth is not old
assert left._wcmatch_version is old_match
escaped = left.wcwidth
assert escaped.__wrapped__.__globals__ is old_globals
assert old.__wrapped__.__globals__ is old_globals

def absent(ucs, table):
    return 0

left._bisearch = absent
escaped.cache_clear()
print('escaped-miss', escaped('汉'), left.wcswidth('汉字'))
print('older-hit', old('汉'))
old.cache_clear()
print('older-miss', old('汉'))
assert escaped('汉') == 1 and old('汉') == 1
assert package.wcwidth is m.wcwidth and package.wcwidth('汉') == 2
assert sys.modules['wcwidth.wcwidth'] is m
spec.loader.exec_module(left)
escaped.cache_clear()
print('escaped-after-success', escaped('汉'), left.wcwidth('汉'))
assert escaped('汉') == 2 and left.wcwidth('汉') == 2
print('failure-owner-complete')
