"""将同次发射 PE 与明确期望封为单一测试输入；哈希校验在此完成而非冒称 Rust 校验。"""
import hashlib
import json
from pathlib import Path
import struct


def digest(data):
    return hashlib.sha256(data).hexdigest()


def envelope(directory, case, commit):
    directory = Path(directory)
    raw = (directory / f'{case}-expected.json').read_bytes()
    expected = json.loads(raw)
    pe = (directory / f'{case}.dll').read_bytes()
    if (expected['schema_version'], expected['report_version']) != (1, 2):
        raise ValueError('不支持的期望/report版本')
    if expected['commit'] != commit or expected['case'] != case:
        raise ValueError('陈旧commit/case')
    if expected['pe'] != {'name': f'{case}.dll', 'size': len(pe), 'sha256': digest(pe)}:
        raise ValueError('PE身份不符')
    if expected['owner_count'] != len(expected['owners']):
        raise ValueError('MIR owner全集不符')
    sources = {source['module_name'] for source in expected['sources']}
    if not set(expected['selected']) <= sources:
        raise ValueError('缺失编译源码身份')
    modules = expected['modules']
    if (not expected['owners'] or not modules or
            len({m['module_id'] for m in modules}) != len(modules) or
            len({m['module_name'] for m in modules}) != len(modules) or
            sorted(m['module_name'] for m in modules) != sorted(expected['selected'])):
        raise ValueError('空或重复/缺失的选择映射')
    data = bytearray(b'RYUPE002')

    def number(value):
        if type(value) is not int or not 0 <= value < 2 ** 32:
            raise ValueError('无效u32')
        data.extend(struct.pack('<I', value))

    def text(value):
        value = value.encode('utf-8')
        if len(value) > 100_000:
            raise ValueError('字符串过长')
        number(len(value))
        data.extend(value)

    def locator(value):
        for key in ('owner', 'name', 'signature'):
            text(value[key])
        number(value['slot'])

    def root(value):
        locator(value['source'])
        locator(value['target'])
        number(value['instruction'])
        text(value['role'])

    number(2)
    text(commit)
    text(case)
    locator(expected['entry'])
    number(len(expected['owners']))
    ids = set()
    for owner in expected['owners']:
        if owner['function_id'] in ids:
            raise ValueError('重复FnId')
        ids.add(owner['function_id'])
        number(owner['function_id'])
        locator(owner['core'])
        root(owner['root'])
        number(owner['calls'])
    number(len(modules))
    for module in modules:
        root(module['root'])
    number(len(pe))
    data.extend(pe)
    if len(data) > 64 * 1024 * 1024:
        raise ValueError('输入过大')
    destination = directory / f'{case}.pe-envelope'
    with destination.open('xb') as output:
        output.write(data)
    return {f'{case}.dll': digest(pe), f'{case}-expected.json': digest(raw),
            destination.name: digest(data)}


def unchanged(directory, identity):
    for name, expected in identity.items():
        if digest((Path(directory) / name).read_bytes()) != expected:
            raise ValueError('PE验收输入被改写')
