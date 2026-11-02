# 第二个独立进程：logging之前换stderr；类型在io导入前读取，反向导入其余fresh表。
import sys
before_io = type(sys.stderr)
class Sink:
    def __init__(self):
        self.parts = []
    def write(self, text):
        self.parts.append(text)
    def flush(self):
        pass
first = Sink()
second = Sink()
original = sys.stderr
sys.stderr = first
try:
    import logging
    print('first stream', logging.lastResort.stream is first)
    logger = logging.getLogger('lazy-order')
    logger.warning('one')
    sys.stderr = second
    import logging as repeated
    print('second stream', repeated is logging, repeated.lastResort is logging.lastResort,
          repeated.lastResort.stream is second)
    logger.warning('two')
finally:
    sys.stderr = original
print('logs', first.parts, second.parts)
import dataclasses
import warnings
import string
import datetime
import copy
import argparse
import contextlib
import functools
import re
import json
import time
import math
import io
print('io stable', before_io is io.TextIOWrapper, type(sys.stderr) is before_io)
print('types', type(re.compile('a')) is re.Pattern,
      type(datetime.timezone.utc) is datetime.timezone,
      type(argparse.Namespace()) is argparse.Namespace)
print('operations', math.sqrt(16), json.loads('2'), copy.copy([1]), string.digits)
