"""重建的schema4直接验证器；不改原manifest、不降schema、不隐去未知字段。"""
from pathlib import PurePosixPath
import hashlib
import json
import re
from manifest_verify import (require, shape, text, integer, digest_text, method, file_entry,
                             TARGETS, SCOPE)
from manifest_files import relative_path
from manifest_options import BOOL_OPTIONS, OPTION_KEYS, validate_options
from resource_manifest import portable, check_targets, PREFIX


def validate_default_options(options, report, expected_policy):
    require(isinstance(options, dict), 'options不是对象')
    require(set(options) == OPTION_KEYS | ({'distribution_metadata'} if report is not None else set()), 'options字段不符')
    # 字段级复用七项合同；不是将整个新artifact降级后交旧schema。
    validate_options({k:options[k] for k in OPTION_KEYS})
    require(options['instance_modules'] == [] and options['module_environment_policy'] == expected_policy, '非默认选择')
    if report is not None:
        require(options['distribution_metadata'] == report, '实际PE metadata报告不符')

def validate_schema4(data, expected_rid, expected_sdk, *, expected_automatic, metadata_report):
    # 旧公开门保持无显式资源请求；组合门不能隐式改变这里的合同。
    return _validate_schema4(data, expected_rid, expected_sdk,
        expected_automatic=expected_automatic, metadata_report=metadata_report,
        declared_request=False, expected_policy='ordinary-dynamic-module-pilot')


def _validate_schema4(data, expected_rid, expected_sdk, *, expected_automatic,
                      metadata_report, declared_request, expected_policy):
    expected_instance_modules = expected_automatic
    require(isinstance(data, dict), '清单必须是对象')
    version = data.get('schema_version')
    require(type(version) is int and version == 4, '未知schema')
    keys = 'schema_version kind compiler target toolchain entry sources modules emitted_delegate_references build_inputs artifacts scope'
    shape(data, keys + ' module_locations resources default_source_plan')
    require(data['kind'] == 'ryupy-native-aot-artifact', '错误清单kind')
    compiler = data['compiler']
    shape(compiler, 'package_version build_revision codegen_report_version options')
    text(compiler['package_version'])
    require(compiler['build_revision'] is None or
            isinstance(compiler['build_revision'], str) and bool(re.fullmatch('[0-9a-f]{40}', compiler['build_revision'])), '不可靠revision')
    require(type(compiler['codegen_report_version']) is int and compiler['codegen_report_version'] in (1, 2), '未知codegen报告')
    require(expected_policy in ('explicit-only', 'ordinary-dynamic-module-pilot'), '未知预期策略')
    if expected_policy == 'explicit-only':
        require(expected_automatic == (), 'ExplicitOnly不得自动选择实例')
    validate_default_options(compiler['options'], metadata_report, expected_policy)
    # None完整保留旧Legacy验证；只有调用方显式tuple才启用实例预期合同。
    if expected_instance_modules is not None:
        require(type(expected_instance_modules) is tuple, '实例预期必须是不可变tuple')
        require(all(type(name) is str and all(part.isidentifier() for part in name.split('.'))
                    for name in expected_instance_modules), '实例预期包含非法模块名字')
        require(len(set(expected_instance_modules)) == len(expected_instance_modules), '实例预期不能重复')
        require(compiler['options']['instance_modules'] == [], '实例预期与实际编译选项不一致')
        if expected_instance_modules:
            require(version == 4 and compiler['codegen_report_version'] == 2, '实例需要schema2及report2')
    shape(data['target'], 'rid native_format architecture')
    require(expected_rid in TARGETS, '未知期望RID')
    fmt, arch = TARGETS[expected_rid]
    require(data['target'] == {'rid':expected_rid, 'native_format':fmt, 'architecture':arch}, '目标RID/格式/架构不符')
    shape(data['toolchain'], 'sdk_version target_framework_major')
    sdk = data['toolchain']['sdk_version']
    require(isinstance(sdk, str) and bool(re.fullmatch(r'[0-9]+\.[0-9]+\.[0-9]+(?:-[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*)?', sdk)), 'SDK版本非法')
    require(sdk == expected_sdk, '清单SDK与本次真实publish记录不符')
    require(type(data['toolchain']['target_framework_major']) is int and data['toolchain']['target_framework_major'] == 10, '非当前net10合同')
    scope_keys = SCOPE | {'declared_resources_complete', 'automatic_module_locations_complete'}
    shape(data['scope'], ' '.join(sorted(scope_keys)))
    require(all(data['scope'][key] is False for key in SCOPE - {'resources_included'}),
            '错误宣称闭包/原生保留或自hash')
    require(type(declared_request) is bool, '声明请求模式必须是bool')
    require(data['scope']['automatic_module_locations_complete'] is True, '自动位置不完整')
    if not declared_request:
        require(all(data['scope'][k] is False for k in SCOPE), '新域无数据资源或完整闭包声明')
        require(data['scope']['declared_resources_complete'] is False, '位置与数据完整性混淆')
    else:
        require(data['scope']['declared_resources_complete'] is True, '显式声明未完整处理')
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
        shape(module, 'id name init getter setter exports environment_reason')
        text(module['environment_reason'])
        integer(module['id'])
        require(module['id'] in sources and module['id'] not in modules, '模块缺失/重复')
        require(module['name'] == sources[module['id']]['module_name'], '报告/来源模块不一致')
        method(module['init'])
        if expected_instance_modules is not None and module['name'] in expected_instance_modules:
            require(module['getter'] is None and module['setter'] is None and module['exports'] == [],
                    '预期实例只能双null accessor和空exports，不能伪造Legacy方法')
            require(module['environment_reason'] == 'compiler-catalog-authenticated-source', '预期实例选择理由不符')
        else:
            method(module['getter']); method(module['setter'])
            if expected_instance_modules is not None:
                require(module['environment_reason'] != 'explicit-instance', '未选择模块冒充显式实例')
        require(isinstance(module['exports'], list) and all(isinstance(x, str) for x in module['exports']), '错误exports')
        modules[module['id']] = module
    require(set(modules) == set(sources), '来源与发射模块不一一对应')
    shape(data['entry'], 'module_id managed_entry')
    integer(data['entry']['module_id'])
    require(data['entry']['module_id'] in modules, '入口模块不存在')
    method(data['entry']['managed_entry'])
    if expected_instance_modules is not None:
        require(set(expected_instance_modules) <= module_names, '预期实例缺少实际源码模块')
        require(sources[data['entry']['module_id']]['module_name'] not in expected_instance_modules,
                '显式实例不能选择入口')
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
    roles = {'native_executable', 'unclassified_sidecar'} | ({'declared_resource'} if declared_request else set())
    require(all(x['role'] in roles for x in data['artifacts']), '未知发布角色')
    require(sum(x['role'] == 'native_executable' for x in data['artifacts']) == 1, '需要唯一native')
    validate_default_locations(data, expected_automatic, declared_request=declared_request)
    if declared_request:
        from resource_manifest import validate_resources
        validate_resources(data)


def names(values):
    require(type(values) is list and len(values) <= 4096, '名称列表/预算')
    require(all(type(v) is str and v and all(x.isidentifier() for x in v.split('.')) for v in values), '非法模块名')
    require(len(set(values)) == len(values), '重复名称')
    return set(values)


def validate_default_locations(data, selected, *, declared_request=False):
    plan = data['default_source_plan']
    shape(plan, 'schema_version mode candidate_payload_sha256 automatic_instance_modules deferred_sources')
    require(type(plan['schema_version']) is int and plan['schema_version'] == 1 and plan['mode'] == 'automatic_locations', '未知默认计划')
    digest_text(plan['candidate_payload_sha256'])
    require(plan['automatic_instance_modules'] == sorted(names(plan['automatic_instance_modules'])) == list(selected), '自动模块不符/未排序')
    if not declared_request:
        require(data['resources'] == [], '新域不声明数据资源')
    sources = {s['module_id']:s for s in data['sources']}
    locs = data['module_locations']; require(type(locs) is list, '位置必须数组')
    require(len(locs) == len(sources) and len(locs) <= 256, '位置不完整/预算')
    found = set(); anchors = []
    for loc in locs:
        shape(loc, 'module_id module_name kind relative_file')
        i = loc['module_id']; integer(i)
        require(i in sources and i not in found, '未知/重复位置')
        found.add(i)
        require(loc['module_name'] == sources[i]['module_name'] and loc['kind'] == 'executable_relative', '位置身份不符')
        path = portable(loc['relative_file'])
        require(path == PREFIX + sources[i]['logical_source'], '锚与编译源不符')
        anchors.append(path)
    deferred = plan['deferred_sources']; require(type(deferred) is list and len(deferred)+len(locs) <= 256, '延期预算')
    used = {s['module_name'] for s in sources.values()}
    for d in deferred:
        shape(d, 'module_name package logical_source source_sha256 relative_file is_package reason')
        names([d['module_name']]); require(d['module_name'] not in used, '延期和编译源重复')
        used.add(d['module_name']); digest_text(d['source_sha256']); text(d['reason'])
        require(type(d['is_package']) is bool and type(d['package']) is str, '延期类型')
        require(d['package'] == (d['module_name'] if d['is_package'] else d['module_name'].rpartition('.')[0]), '延期包身份')
        require(relative_path(d['logical_source']).startswith(('entry/','modules/')), '延期逻辑源')
        require(portable(d['relative_file']) == PREFIX+d['logical_source'], '延期真实锚不符')
        anchors.append(d['relative_file'])
    check_targets(data['artifacts'], anchors)


def bind_candidate(data, raw, payload):
    shape(payload, 'schema_version roots dependencies locations deferred outside protected closed_unplanned external_dependencies')
    require(type(payload['schema_version']) is int and payload['schema_version'] == 3, '未知candidate schema')
    plan = data['default_source_plan']
    require(hashlib.sha256(raw.encode()).hexdigest() == plan['candidate_payload_sha256'], '原字符串身份')
    roots, outside, protected, closed, external = (names(payload[k]) for k in ('roots','outside','protected','closed_unplanned','external_dependencies'))
    require(not closed, '默认不授closed权限')
    locations = payload['locations']; deferred = payload['deferred']; dependencies = payload['dependencies']
    require(type(locations) is dict and len(locations) <= 256 and type(deferred) is dict and type(dependencies) is dict, '对象/预算')
    names(list(locations)); names(list(deferred)); names(list(dependencies))
    compiled = {s['module_name']:s for s in data['sources']}; loc_by = {x['module_name']:x for x in data['module_locations']}
    delayed = {d['module_name']:d for d in plan['deferred_sources']}
    require(set(locations) == set(compiled)|set(delayed) and set(deferred) == set(delayed), '来源全集不符')
    for name, loc in locations.items():
        shape(loc, 'package file kind path is_package registration')
        require(type(loc['kind']) is int and loc['kind'] == 2 and type(loc['is_package']) is bool, '非kind2/布尔')
        text(loc['file']); portable(loc['path'])
        require(loc['file'] == PurePosixPath(loc['path']).name, '原basename与锚不符')
        require(loc['package'] == (name if loc['is_package'] else name.rpartition('.')[0]), 'package不符')
        require(loc['registration'] in ('active','candidate','deferred'), '未知注册类型')
        if name in delayed:
            d=delayed[name]
            require(loc['registration']=='deferred' and loc['package']==d['package'] and loc['is_package']==d['is_package'] and loc['path']==d['relative_file'] and deferred[name]==d['reason'], '延期载荷不符')
        else:
            require(loc['registration'] in ('active','candidate') and loc['path']==loc_by[name]['relative_file'] and loc['package']==compiled[name]['package'] and loc['is_package'] == (compiled[name]['package']==name), 'compiled载荷不符')
        parent, _, child = name.rpartition('.')
        if parent:
            require(parent in locations and locations[parent]['is_package'] is True, '缺真实父包')
            parent_path = PurePosixPath(locations[parent]['path'])
            require(parent_path.name == '__init__.py', '父包锚非法')
            expected = parent_path.parent / child / '__init__.py' if loc['is_package'] else parent_path.parent/(child+'.py')
            require(str(expected)==loc['path'], '父子几何不符')
    require(roots <= set(locations) and all(locations[n]['registration']!='active' for n in roots), 'root非法')
    require(not(outside & (set(locations)|closed)), 'outside与位置/closed冲突')
    require(not(closed & (set(locations)|roots|protected)), 'closed冲突')
    # protected可重叠dead候选/Outside；实际Activate仍按Runtime既有相位NI。
    targets=set()
    for owner, values in dependencies.items():
        require(owner in locations, '依赖owner无源')
        targets |= names(values)
    require(external == targets-set(locations)-outside-closed-protected, 'external不是精确剩余集合')
