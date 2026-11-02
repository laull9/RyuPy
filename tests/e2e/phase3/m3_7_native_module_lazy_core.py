# 13个fresh工厂的真实公开语义；独立进程，不依赖C#测试单例执行顺序。
import sys
before_io = type(sys.stderr)
import io
print('io', before_io is io.TextIOWrapper, type(sys.stdout) is io.TextIOWrapper)

import math
import time
import json
import re
import functools
import contextlib
import logging
import argparse
import copy
import datetime
import string
import warnings
import dataclasses

import math as math_again
import time as time_again
import json as json_again
import re as re_again
import functools as functools_again
import contextlib as contextlib_again
import logging as logging_again
import argparse as argparse_again
import copy as copy_again
import datetime as datetime_again
import string as string_again
import warnings as warnings_again
import dataclasses as dataclasses_again
print('modules', math is math_again, time is time_again, json is json_again,
      re is re_again, functools is functools_again, contextlib is contextlib_again,
      logging is logging_again, argparse is argparse_again, copy is copy_again,
      datetime is datetime_again, string is string_again, warnings is warnings_again,
      dataclasses is dataclasses_again)
old_pi = math.pi
try:
    math.pi = 'changed'
    import math as changed_math
    print('math', changed_math.pi, changed_math.sqrt(9))
finally:
    math.pi = old_pi
print('math restored', math_again.pi == old_pi)
print('time', time.perf_counter() > 0)
print('json', json.loads('{"value": [1, 2]}')['value'])
pattern = re.compile('a+')
match = pattern.fullmatch('aaa')
print('re', type(pattern) is re.Pattern, type(match) is re.Match,
      re.Pattern is re_again.Pattern, re.Match is re_again.Match,
      re.error is re_again.error, re_again.compile('a+') is pattern, match.group(0))
try:
    re.compile('[')
except Exception as error:
    print('re error', isinstance(error, re.error))

def add(a, b):
    return a + b
part = functools.partial(add, 2)
print('partial', type(part) is functools.partial, functools.partial is functools_again.partial, part(3))

@contextlib.contextmanager
def manager():
    yield 7
with manager() as value:
    print('context', value)
print('logging', logging.getLogger('lazy') is logging_again.getLogger('lazy'),
      type(logging.StreamHandler()) is logging.StreamHandler)
parser = argparse.ArgumentParser(prog='lazy')
parser.add_argument('--number', type=int, default=3)
print('argparse', type(parser) is argparse.ArgumentParser,
      type(parser.parse_args([])) is argparse.Namespace, parser.parse_args([]).number)
source = [1, [2]]
cloned = copy.copy(source)
print('copy', cloned == source, cloned is source, cloned[1] is source[1])
print('datetime', datetime.timezone.utc is datetime_again.timezone.utc,
      datetime.date is datetime_again.date, datetime.date(2024, 2, 29).isoformat())
print('string', string.ascii_lowercase, string.digits)
with warnings.catch_warnings(record=True) as seen:
    warnings.simplefilter('always')
    warnings.warn('lazy-check')
    print('warnings', len(seen), str(seen[0].message), seen[0].category is UserWarning)
@dataclasses.dataclass
class Record:
    value: int
field = dataclasses.fields(Record)[0]
print('dataclasses', dataclasses.MISSING is dataclasses_again.MISSING,
      field.default is dataclasses.MISSING, Record(4).value)
import collections.abc
from collections import abc
import os.path
from os import path
print('parents', collections.abc is abc, os.path is path)
