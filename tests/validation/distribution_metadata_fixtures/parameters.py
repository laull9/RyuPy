# ryupy: pending
# 普通动态函数/kwargs与固定CP精确字符串参数子集，不扩duck-typed导入协议。
import importlib
function = importlib.import_module
print(function(**{'name': 'sys'}).__name__)
print(function('sys', 9).__name__)
print(function('.', 'importlib').__name__)
for name, package in [('', None), ('.target', None), ('.target', ''), ('.target', 9), ('..target', 'one'), (None, None), (4, None)]:
    try:
        function(name, package)
    except ValueError:
        print('ValueError')
    except TypeError:
        print('TypeError')
    except ImportError:
        print('ImportError')
    except AttributeError:
        print('AttributeError')
try:
    function()
except TypeError:
    print('missing TypeError')
try:
    function('sys', name='sys')
except TypeError:
    print('duplicate TypeError')
try:
    function('sys', unknown=1)
except TypeError:
    print('unknown TypeError')
