# 普通 import 失败保留对象：控制模块由测试宿主提供，源码不定义函数或类。
import sys
import fixture_control
fixture_control.attempts += 1
attempt = fixture_control.attempts
items = [attempt]
fixture_control.saved.append(sys.modules[__name__])
partial = 'written'
if attempt == 1:
    raise RuntimeError('ordinary-failure')
completed = True
