# ryupy: pending
"""冻结provider执行域的真实模块状态、动态属性调用和argv同源合同。"""
import json
import json as repeated_json
import math
import math as repeated_math
import sys
import sys as repeated_sys
import urllib
import urllib.parse

assert json is repeated_json
assert math is repeated_math
assert sys is repeated_sys
assert urllib.parse is sys.modules['urllib.parse']
assert sys.argv[1:] == ['参数 中文 &']


def compiled_marker(value):
    return '编译函数标记:' + value


original = json.loads
json.loads = compiled_marker
assert json.loads('真实属性调用') == '编译函数标记:真实属性调用'
from json import loads

assert loads is compiled_marker
assert loads('from绑定') == '编译函数标记:from绑定'
json.loads = original
print('CP1b 模块共享状态、动态绑定与argv通过')
