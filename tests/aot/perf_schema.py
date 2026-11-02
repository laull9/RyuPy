"""冻结统计与逐样本协议：保留原始输入，任何缺失/重复/非有限值立即拒绝。"""
import json
import math
from pathlib import Path


FIELDS = {'main_ms', 'alloc_bytes', 'gc0', 'gc1', 'gc2', 'gc_pause_ms',
          'jit_ms', 'jit_methods', 'heap_bytes'}
COUNTS = {'alloc_bytes', 'gc0', 'gc1', 'gc2', 'jit_methods', 'heap_bytes'}


def pairs(items):
    result = {}
    for key, value in items:
        if key in result:
            raise ValueError(f'重复JSON字段: {key}')
        result[key] = value
    return result


def constant(value):
    raise ValueError(f'非法JSON数值: {value}')


def stats(path):
    raw = Path(path).read_bytes()
    value = json.loads(raw, object_pairs_hook=pairs, parse_constant=constant)
    if not isinstance(value, dict) or set(value) != FIELDS:
        raise ValueError('stats字段与冻结schema不同')
    for key, item in value.items():
        if isinstance(item, bool) or not isinstance(item, (int, float)):
            raise ValueError(f'{key}: 非数值')
        if not math.isfinite(item) or item < 0 or (key in COUNTS and item != int(item)):
            raise ValueError(f'{key}: 非法统计值')
    return value


def output(raw, checksum, *, hot=False, hello=False):
    text = raw.decode('utf-8', errors='strict')
    if hello:
        if text != 'hello\n':
            raise ValueError('hello输出不一致')
        return []
    lines = text.splitlines()
    if not hot:
        if lines != ['result ' + checksum]:
            raise ValueError(f'cold checksum不一致: {text!r}')
        return []
    if len(lines) != 10:
        raise ValueError('hot必须恰有3warm和7sample')
    for index, line in enumerate(lines[:3]):
        if line != f'warm {index} {checksum}':
            raise ValueError(f'warm校验失败: {line}')
    samples = []
    for index, line in enumerate(lines[3:]):
        pieces = line.split(' ', 3)
        if len(pieces) != 4 or pieces[0] != 'sample' or pieces[1] != str(index) or pieces[3] != checksum:
            raise ValueError(f'sample校验失败: {line}')
        seconds = float(pieces[2])
        if not math.isfinite(seconds) or seconds < 0:
            raise ValueError('sample时长必须有限非负')
        samples.append(seconds)
    return samples
