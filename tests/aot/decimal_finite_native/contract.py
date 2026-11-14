"""有限Decimal独立Native门的输入、原流与发布身份合同；不借检查器通过冒充产品。"""
import hashlib
import json
import os
import stat
from pathlib import Path

PYTHON_SHA = '021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7'


def require(value, message):
    if not value: raise ValueError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def complete(record):
    require(record['exit'] == 0 and record['failure'] is None and record['raw_streams_complete'],
            '非成功退出或原流不完整')


def positive(record, stdout, stderr, golden):
    complete(record)
    require(stdout == golden and stderr == b'', '产品输出与固定CP黄金不同')


def context(env, event):
    require(env.get('GITHUB_ACTIONS') == 'true' and env.get('RUNNER_ENVIRONMENT') == 'github-hosted'
            and env.get('GITHUB_EVENT_NAME') == 'push' and env.get('GITHUB_RUN_ATTEMPT') == '1'
            and env.get('GITHUB_REF') == event.get('ref') == 'refs/heads/ci/wcwidth-legacy-aot-2909056'
            and event.get('head_commit', {}).get('message') == 'validation: ordinary-mi-c3-001'
            and event.get('after') == event.get('head_commit', {}).get('id') == env.get('GITHUB_SHA')
            and len(env.get('GITHUB_SHA', '')) == 40
            and all(c in '0123456789abcdef' for c in env['GITHUB_SHA']), '非精确同源hosted窗口')


def source_index(root, raw):
    rows = []
    for item in raw.split(b'\0'):
        if not item: continue
        left, name = item.split(b'\t', 1); mode, blob, stage = left.decode().split()
        name = os.fsdecode(name); path = root/name
        require(stage == '0' and mode in ('100644', '100755') and path.is_file() and not path.is_symlink(), '非法源码索引')
        body = path.read_bytes()
        require(stat.S_IMODE(path.stat().st_mode) == int(mode[-3:], 8)
                and hashlib.sha1(b'blob '+str(len(body)).encode()+b'\0'+body).hexdigest() == blob,
                '源码字节/mode与索引不符')
        rows.append({'path': name, 'mode': mode, 'blob': blob, 'sha256': sha(path)})
    require(rows, '空源码集合')
    return rows


def fixture(root):
    data = json.loads((root/'SOURCE.json').read_text())
    require(data['python'] == 'CPython 3.12.13' and data['python_sha256'] == PYTHON_SHA, 'CP来源变化')
    require(len(data['files']) == 3 and {r['path'] for r in data['files']} == {'entry.py', 'values.py', 'stdout.txt'}, '样例集合变化')
    require({p.name for p in root.iterdir()} == {'SOURCE.json', 'entry.py', 'values.py', 'stdout.txt'}, '样例目录含额外来源')
    for row in data['files']:
        p = root/row['path']
        require(p.is_file() and not p.is_symlink() and p.stat().st_size == row['size'] and sha(p) == row['sha256'], '样例原件变化')
    return (root/'stdout.txt').read_bytes()
