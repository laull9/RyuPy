"""schema1/2发布清单核验；元数据校验不等于ILC保根/动态依赖闭包证明。"""
import argparse
import json
from pathlib import Path
import re
import shutil
import struct
from manifest_files import ManifestError, check_artifacts, plain_stat, relative_path

MANIFEST_NAME = 'ryupy-artifact.json'
TARGETS = {'linux-x64': ('ELF', 'x64'), 'osx-arm64': ('Mach-O', 'arm64'), 'win-x64': ('PE', 'x64')}
SCOPE = {'dynamic_dependency_closure_complete', 'resources_included',
         'native_retention_proven', 'manifest_self_hash_included'}


def require(test, message):
    if not test:
        raise ManifestError(message)


def shape(value, keys):
    require(isinstance(value, dict) and set(value) == set(keys.split()), f'字段不符：期望{keys}')


def text(value):
    require(isinstance(value, str) and bool(value), '需要非空字符串')


def integer(value):
    require(type(value) is int and value >= 0, '需要非负整数')


def digest_text(value):
    require(isinstance(value, str) and bool(re.fullmatch('[0-9a-f]{64}', value)), '需要小写SHA256')


def method(value):
    shape(value, 'owner name signature')
    for item in value.values():
        text(item)


def file_entry(value):
    shape(value, 'relative_path role size sha256')
    relative_path(value['relative_path'])
    text(value['role'])
    integer(value['size'])
    digest_text(value['sha256'])


def no_duplicate_keys(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, f'重复JSON键：{key}')
        result[key] = value
    return result


def validate_schema(data, expected_rid, expected_sdk):
    require(isinstance(data, dict), '清单必须是对象')
    version = data.get('schema_version')
    require(type(version) is int and version in (1, 2), '未知schema')
    keys = 'schema_version kind compiler target toolchain entry sources modules emitted_delegate_references build_inputs artifacts scope'
    shape(data, keys + (' module_locations resources' if version == 2 else ''))
    require(data['kind'] == 'ryupy-native-aot-artifact', '错误清单kind')
    compiler = data['compiler']
    shape(compiler, 'package_version build_revision codegen_report_version options')
    text(compiler['package_version'])
    require(compiler['build_revision'] is None or
            isinstance(compiler['build_revision'], str) and bool(re.fullmatch('[0-9a-f]{40}', compiler['build_revision'])), '不可靠revision')
    require(type(compiler['codegen_report_version']) is int and compiler['codegen_report_version'] == 1, '未知codegen报告')
    shape(compiler['options'], 'experimental_direct_accumulator experimental_leaf_inline diagnostic_scalar_slp experimental_packed_slp')
    require(all(type(v) is bool for v in compiler['options'].values()), '优化开关不是bool')
    shape(data['target'], 'rid native_format architecture')
    require(expected_rid in TARGETS, '未知期望RID')
    fmt, arch = TARGETS[expected_rid]
    require(data['target'] == {'rid':expected_rid, 'native_format':fmt, 'architecture':arch}, '目标RID/格式/架构不符')
    shape(data['toolchain'], 'sdk_version target_framework_major')
    sdk = data['toolchain']['sdk_version']
    require(isinstance(sdk, str) and bool(re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+(?:-[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*)?', sdk)), 'SDK版本非法')
    require(sdk == expected_sdk, '清单SDK与本次真实publish记录不符')
    require(type(data['toolchain']['target_framework_major']) is int and data['toolchain']['target_framework_major'] == 10, '非当前net10合同')
    scope_keys = SCOPE | ({'declared_resources_complete'} if version == 2 else set())
    shape(data['scope'], ' '.join(sorted(scope_keys)))
    require(all(data['scope'][key] is False for key in SCOPE - {'resources_included'}),
            '错误宣称闭包/原生保留或自hash')
    if version == 1:
        require(data['scope']['resources_included'] is False, 'schema1不含声明资源')
    for name in ('sources', 'modules', 'emitted_delegate_references', 'build_inputs', 'artifacts'):
        require(isinstance(data[name], list), f'{name}必须是数组')
    require(bool(data['sources']), '没有源码模块')
    sources = {}
    module_names = set()
    for source in data['sources']:
        shape(source, 'module_id module_name package logical_source compiled_text_sha256')
        integer(source['module_id'])
        for name in ('module_name', 'package'):
            value = source[name]
            require(isinstance(value, str) and (name == 'package' and value == '' or all(p.isidentifier() for p in value.split('.'))), '非法模块身份')
        path = relative_path(source['logical_source'])
        require(path.startswith(('entry/', 'modules/')), '不是逻辑源码路径')
        require(source['module_name'] not in module_names and source['module_id'] not in sources, '重复源码身份')
        module_names.add(source['module_name'])
        sources[source['module_id']] = source
        digest_text(source['compiled_text_sha256'])
    require(set(sources) == set(range(len(sources))), '模块编号不连续')
    modules = {}
    for module in data['modules']:
        shape(module, 'id name init getter setter exports')
        integer(module['id'])
        require(module['id'] in sources and module['id'] not in modules, '模块缺失/重复')
        require(module['name'] == sources[module['id']]['module_name'], '报告/来源模块不一致')
        for name in ('init', 'getter', 'setter'):
            method(module[name])
        require(isinstance(module['exports'], list) and all(isinstance(x, str) for x in module['exports']), '错误exports')
        modules[module['id']] = module
    require(set(modules) == set(sources), '来源与发射模块不一一对应')
    shape(data['entry'], 'module_id managed_entry')
    integer(data['entry']['module_id'])
    require(data['entry']['module_id'] in modules, '入口模块不存在')
    method(data['entry']['managed_entry'])
    sites = set()
    for reference in data['emitted_delegate_references']:
        shape(reference, 'site target target_display role module_id function_id class_id')
        shape(reference['site'], 'source_method instruction')
        method(reference['site']['source_method'])
        integer(reference['site']['instruction'])
        site = (json.dumps(reference['site']['source_method'], sort_keys=True), reference['site']['instruction'])
        require(site not in sites, '重复发射引用位置')
        sites.add(site)
        if reference['target'] is not None:
            method(reference['target'])
        text(reference['target_display'])
        require(reference['role'] in {'module_init','module_getter','module_setter','callable_thunk','generator_resume','exception_factory','unclassified'}, '未知引用角色')
        for name in ('module_id', 'function_id', 'class_id'):
            if reference[name] is not None:
                integer(reference[name])
        require(reference['module_id'] is None or reference['module_id'] in modules, '引用模块不存在')
    for entry in data['build_inputs'] + data['artifacts']:
        file_entry(entry)
    require({(x['relative_path'], x['role']) for x in data['build_inputs']} ==
            {('managed-entry.dll','compiled_managed_input'), ('RyuPy.Runtime.dll','runtime_managed_input')}
            and len(data['build_inputs']) == 2, '托管输入名称/角色不符')
    roles = {'native_executable', 'unclassified_sidecar'} | ({'declared_resource'} if version == 2 else set())
    require(all(x['role'] in roles for x in data['artifacts']), '未知发布角色')
    require(sum(x['role'] == 'native_executable' for x in data['artifacts']) == 1, '需要唯一native')
    if version == 2:
        from resource_manifest import validate_resources
        validate_resources(data)


def image_header(path, rid):
    with path.open('rb') as stream:
        data = stream.read(64)
        if rid == 'linux-x64':
            require(len(data) == 64 and data[:7] == b'\x7fELF\x02\x01\x01', '非ELF64 little endian')
            kind, machine, version = struct.unpack_from('<HHI', data, 16)
            require(kind in (2, 3) and machine == 62 and version == 1
                    and struct.unpack_from('<H', data, 52)[0] == 64, '非x64 ELF可执行/头长度错误')
        elif rid == 'osx-arm64':
            require(len(data) >= 32 and struct.unpack_from('<II', data) == (0xfeedfacf, 0x100000c)
                    and struct.unpack_from('<I', data, 12)[0] == 2, '非arm64 Mach-O可执行')
        else:
            require(len(data) == 64 and data[:2] == b'MZ', '非MZ')
            offset = struct.unpack_from('<I', data, 60)[0]
            require(64 <= offset <= 1024*1024, 'PE偏移越界')
            stream.seek(offset)
            header = stream.read(24)
            require(len(header) == 24 and header[:4] == b'PE\0\0', '非PE')
            machine, size, flags = struct.unpack_from('<H',header,4)[0], struct.unpack_from('<H',header,20)[0], struct.unpack_from('<H',header,22)[0]
            require(machine == 0x8664 and flags & 2 and not flags & 0x2000 and 240 <= size <= 4096, '非AMD64 EXE')
            optional = stream.read(size)
            require(len(optional) == size and struct.unpack_from('<H',optional)[0] == 0x20b
                    and struct.unpack_from('<I',optional,108)[0] >= 15
                    and struct.unpack_from('<II',optional,224) == (0,0), 'PE含CLR目录或非PE32+')


def read_sdk_version(path):
    path = Path(path)
    require(plain_stat(path).st_size <= 256, 'SDK记录过大')
    raw = path.read_bytes()
    require(len(raw) <= 256, 'SDK记录读取期间增长')
    value = raw.decode('utf-8-sig')
    if value.endswith('\r\n'):
        value = value[:-2]
    elif value.endswith('\n'):
        value = value[:-1]
    require(bool(re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+(?:-[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*)?', value)), 'SDK记录必须恰一合法版本行')
    return value


def verify_manifest(publish, native, rid, sdk_version):
    publish, native = Path(publish).absolute(), Path(native).absolute()
    plain_stat(publish, directory=True)
    manifest = publish/MANIFEST_NAME
    plain_stat(manifest)
    data = json.loads(manifest.read_text(encoding='utf-8'), object_pairs_hook=no_duplicate_keys)
    validate_schema(data, rid, sdk_version)
    actual = check_artifacts(publish, data['artifacts'], MANIFEST_NAME, windows=rid == 'win-x64')
    entry = next(x for x in data['artifacts'] if x['role'] == 'native_executable')
    expected = actual[entry['relative_path']]
    require(native.is_file() and native.samefile(expected) and native.parent.samefile(expected.parent)
            and native.name == expected.name, 'CLI native不等于清单唯一native')
    image_header(expected, rid)
    return data


def published_manifest(stderr, native):
    names = [line.removeprefix('AOT 清单：') for line in stderr.decode('utf-8', errors='strict').splitlines()
             if line.startswith('AOT 清单：')]
    require(len(names) == 1, 'CLI必须明确报告唯一AOT清单')
    path = Path(names[0])
    expected = Path(native).parent/MANIFEST_NAME
    require(path.is_absolute() and path.is_file() and path.samefile(expected), 'CLI清单不属于同次publish')
    plain_stat(path)
    return path


def dependency_binaries(root, data):
    # sidecar仍是未分类；只把有已知动态库后缀的项交平台格式检查，不猜ILC闭包。
    paths = []
    for item in data['artifacts']:
        name = item['relative_path']
        if item['role'] == 'native_executable' or (item['role'] == 'unclassified_sidecar'
                and re.search(r'(?:\.so(?:\.[0-9]+)*|\.dylib|\.dll)$', name, re.I)):
            paths.append(Path(root)/name)
    return paths


def relocate_manifest(publish, native, destination, rid, sdk_version):
    publish, destination = Path(publish), Path(destination)
    data = verify_manifest(publish, native, rid, sdk_version)
    destination.mkdir()  # 不覆盖旧目录，不把用户已有文件混进本次搬迁。
    for entry in data['artifacts']:
        target = destination/entry['relative_path']
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(publish/entry['relative_path'], target)
    shutil.copy2(publish/MANIFEST_NAME, destination/MANIFEST_NAME)
    name = next(x['relative_path'] for x in data['artifacts'] if x['role'] == 'native_executable')
    relocated_native = destination/name
    verify_manifest(destination, relocated_native, rid, sdk_version)
    return relocated_native, data


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--publish', required=True, type=Path)
    parser.add_argument('--native', required=True, type=Path)
    parser.add_argument('--rid', required=True, choices=TARGETS)
    parser.add_argument('--sdk-version', required=True)
    args = parser.parse_args()
    verify_manifest(args.publish, args.native, args.rid, args.sdk_version)
    print('manifest verified (not a native retention/closure proof)')
