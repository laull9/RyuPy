#!/usr/bin/env python3
"""固定CPython即时生成实体key事件oracle；不执行RyuPy，不代表默认优化命中。"""
import argparse
import json
from pathlib import Path
import re
import subprocess
import sys


def build_events():
    events = []
    def emit(*parts):
        assert all(isinstance(p, str) for p in parts)
        events.append(list(parts))
    def b(value): return str(bool(value)).lower()
    def increment(d, key): d[key] = d.get(key, 0) + 1

    d = {}; first = ('ann', 'buy'); increment(d, first)
    emit('identity-miss', str(d[first]), b(next(iter(d)) is first))
    later = tuple(['ann', 'buy']); increment(d, later)
    emit('identity-hit', str(d[later]), b(next(iter(d)) is first), b(next(iter(d)) is later))
    del d[first]; increment(d, later)
    emit('identity-reinsert', str(d[later]), b(next(iter(d)) is later))

    for label, seed in [('-6', -6), ('255', 255), ('256', 256),
                        ('max', 9223372036854775807), ('min', -9223372036854775808), ('bool', True)]:
        d = {first: seed}; increment(d, first); value = d[first]
        emit('integer', label, str(value), type(value).__name__, b(value is int(str(value))))

    d = {}; empty = ('', 'buy'); nul = ('\0', 'buy')
    increment(d, empty); increment(d, nul); increment(d, empty)
    for key, value in d.items(): emit('collision-domain', key[0], str(value))

    d = {}; replacement = tuple(['ann', 'buy'])
    class ReplaceCounter:
        def __add__(self, other):
            emit('add-replace', str(other), b(next(iter(d)) is first))
            del d[first]; d[replacement] = 100
            return 41
    d[first] = ReplaceCounter(); increment(d, first)
    emit('replace-result', str(d[first]), b(next(iter(d)) is replacement), b(next(iter(d)) is first))

    class ThrowCounter:
        def __add__(self, other):
            emit('add-throw', str(other))
            raise ValueError('counter failed')
    d = {}; old = ThrowCounter(); d[first] = old
    try: increment(d, first)
    except ValueError as error: emit('exception', type(error).__name__, str(error), b(d[first] is old))

    observing = False
    class UserKey:
        def __init__(self, label): self.label = label
        def __hash__(self):
            if observing: emit('hash', self.label)
            return 123
        def __eq__(self, other):
            if observing: emit('eq', self.label, other.label)
            return True
    stored = UserKey('stored'); query = UserKey('query'); d = {stored: 4}; observing = True
    increment(d, query)
    # entries读取不再触发被测hash/eq。
    key, value = next(iter(d.items()))
    emit('user-key-result', str(value), b(key is stored), b(key is query))
    observing = False

    class Custom(dict):
        def get(self, key, default=None):
            emit('custom-get', key[0], key[1]); return 5
        def __setitem__(self, key, value):
            emit('custom-set', str(value)); super().__setitem__(key, value)
    d = Custom(); increment(d, first)
    emit('custom-result', str(d[first]), b(next(iter(d)) is first))

    d = {}
    for i in range(300): increment(d, (str(i), 'buy'))
    zero = next(iter(d)); d[('outside', 'buy')] = 0; increment(d, zero)
    emit('budget-external', str(len(d)), str(d[zero]), b(next(iter(d)) is zero))
    d.clear(); increment(d, zero)
    emit('budget-clear', str(len(d)), str(d[zero]), b(next(iter(d)) is zero))
    return events


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--expected-sha', required=True)
    parser.add_argument('--token', required=True)
    args = parser.parse_args()
    if sys.version_info[:3] != (3, 12, 13): parser.error('必须使用固定CPython3.12.13')
    if not re.fullmatch('[0-9a-f]{40}', args.expected_sha) or not re.fullmatch('[0-9a-f]{32}', args.token):
        parser.error('提交或本次token格式错误')
    root = Path(__file__).resolve().parents[2]
    if args.output.resolve().is_relative_to(root): parser.error('oracle必须写仓库外')
    actual_sha = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
    if actual_sha != args.expected_sha: parser.error('实际提交不匹配')
    payload = {'schema': 1, 'scope': 'runtime-candidate-only', 'python': [3, 12, 13],
               'commit': actual_sha, 'token': args.token, 'events': build_events()}
    with args.output.open('x') as stream: json.dump(payload, stream, ensure_ascii=True, indent=2)
    print(f'CPython3.12.13 oracle: {len(payload["events"])} events; runtime candidate only')


if __name__ == '__main__': main()
