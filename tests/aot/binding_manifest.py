"""恢复版schema3独立核验，按本轮driver实体；非ABI安全/保根/依赖闭包证明。"""
import json
from manifest_options import validate_options
from pathlib import Path
import re
import shutil
from manifest_files import check_artifacts, plain_stat, relative_path
from runtime_dependencies import RUNTIME_ROLES, validate_runtime_dependencies
from manifest_verify import (MANIFEST_NAME, TARGETS, SCOPE, digest_text, file_entry,
    image_header, integer, method, no_duplicate_keys, require, shape, text)

RELATIVE = {'linux-x64':'_internal/native/v1/b0/library.so',
    'osx-arm64':'_internal/native/v1/b0/library.dylib', 'win-x64':'_internal/native/v1/b0/library.dll'}


def identifier(value):
    require(isinstance(value,str) and re.fullmatch(r'[A-Za-z_][A-Za-z_0-9]{0,127}',value), '非固定ASCII标识符')


def validate_schema(data, rid, linkage, sdk):
    shape(data,'schema_version kind compiler target toolchain entry sources modules emitted_delegate_references native_bindings build_inputs artifacts scope')
    require(type(data['schema_version']) is int and data['schema_version']==3, '非schema3')
    require(rid in TARGETS and linkage in ('jit_shared','aot_static'), '未知RID/链接方式')
    aot = linkage=='aot_static'
    require(data['kind']==('ryupy-native-aot-artifact' if aot else 'ryupy-managed-binding-artifact'), 'kind错误')
    fmt, arch = TARGETS[rid]
    require(data['target']=={'rid':rid,'native_format':fmt,'architecture':arch}, 'RID/格式/架构错误')
    compiler = data['compiler']; shape(compiler,'package_version build_revision codegen_report_version options')
    text(compiler['package_version']); revision = compiler['build_revision']
    require(revision is None or isinstance(revision,str) and re.fullmatch('[0-9a-f]{40}',revision), 'revision错误')
    require(type(compiler['codegen_report_version']) is int and compiler['codegen_report_version'] in (1, 2), 'report版本错误')
    validate_options(compiler['options'])
    shape(data['toolchain'],'sdk_version target_framework_major')
    require(type(data['toolchain']['target_framework_major']) is int and data['toolchain']['target_framework_major']==10, '非net10')
    require(data['toolchain']['sdk_version']==sdk, 'SDK不等于同次记录')
    if aot: require(isinstance(sdk,str) and re.fullmatch(r'\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?',sdk), 'AOT缺实际SDK')
    else: require(sdk is None, 'JIT未消费publish SDK')
    shape(data['scope'],' '.join(sorted(SCOPE)))
    require(all(v is False for v in data['scope'].values()), 'scope越界声明')
    for key in ('sources','modules','emitted_delegate_references','native_bindings','build_inputs','artifacts'):
        require(isinstance(data[key],list), key+'必须是数组')
    require(len(data['native_bindings'])==1, '需要一个绑定')
    b = data['native_bindings'][0]
    shape(b,'binding_id module_id module export signature library symbol declaration_sha256 linkage relative_file initializer_id export_id body native pinvoke')
    for key in ('module_id','initializer_id','export_id'): integer(b[key])
    for key in ('module','export','library','symbol'): identifier(b[key])
    require(b['module']!='__main__' and b['export'] not in ('__file__','__name__','__package__'), '保留身份')
    require(b['binding_id']=='b0' and b['signature']=='cdecl_i32_i32_i64' and b['linkage']==linkage, '绑定签名/链接合同错误')
    require(b['relative_file']==(None if aot else RELATIVE[rid]), '绑定部署路径错误')
    digest_text(b['declaration_sha256']); method(b['body']); method(b['native'])
    require(b['native']['signature']=='static long (int, int)' and b['body']['signature']=='static object (object[])', '实际CIL签名错误')
    shape(b['pinvoke'],'calling_convention character_set no_mangle supports_last_error preserve_sig is_static has_body')
    require(b['pinvoke']=={'calling_convention':'Cdecl','character_set':'NotSpecified','no_mangle':True,
        'supports_last_error':False,'preserve_sig':True,'is_static':True,'has_body':False}, 'PInvoke标志错误')
    require(all(type(b['pinvoke'][key]) is bool for key in ('no_mangle','supports_last_error','preserve_sig','is_static','has_body')), 'PInvoke非bool')
    sources = {}; names = set()
    for source in data['sources']:
        shape(source,'module_id module_name package logical_source compiled_text_sha256')
        integer(source['module_id']); text(source['module_name'])
        require(all(part.isidentifier() for part in source['module_name'].split('.')), '源码模块身份错误')
        package = source['package']
        require(isinstance(package,str) and (package=='' or all(part.isidentifier() for part in package.split('.'))), 'package错误')
        require(relative_path(source['logical_source']).startswith(('entry/','modules/')), '非逻辑源码路径')
        digest_text(source['compiled_text_sha256'])
        require(source['module_id'] not in sources and source['module_name'] not in names, '重复源码身份')
        sources[source['module_id']] = source; names.add(source['module_name'])
    require(bool(sources) and set(sources)==set(range(len(sources))), '源码ID不连续/为空')
    require(b['module_id']==len(sources) and b['module'] not in names, '生成模块冒源码')
    require(all(not n.startswith(b['module']+'.') and not b['module'].startswith(n+'.') for n in names), '源码父子冲突')
    modules = {}
    for m in data['modules']:
        shape(m,'id name init getter setter exports origin environment_reason'); integer(m['id'])
        text(m['environment_reason'])
        require(m['id'] not in modules, '重复模块ID')
        for key in ('init','getter','setter'): method(m[key])
        require(isinstance(m['exports'],list) and all(isinstance(x,str) for x in m['exports']), 'exports错误')
        if m['id']==b['module_id']:
            require(m['name']==b['module'] and m['exports']==[b['export']], '生成模块/导出错误')
            require(m['origin']=={'kind':'declared_native_binding','binding_id':'b0','declaration_sha256':b['declaration_sha256']}, '生成origin错误')
        else:
            require(m['id'] in sources and m['name']==sources[m['id']]['module_name'], '源码报告身份错误')
            require(m['origin']=={'kind':'compiled_source','source_module_id':m['id']}, '源码origin错误')
            integer(m['origin']['source_module_id'])
        modules[m['id']] = m
    require(set(modules)==set(range(len(sources)+1)), '模块集合不闭合')
    shape(data['entry'],'module_id managed_entry'); integer(data['entry']['module_id'])
    require(data['entry']['module_id'] in sources, '入口非源码'); method(data['entry']['managed_entry'])
    sites = set(); bodies = 0
    roles = {'module_init','module_getter','module_setter','callable_thunk','generator_resume','exception_factory','unclassified','declared_native_body'}
    for ref in data['emitted_delegate_references']:
        shape(ref,'site target target_display role module_id function_id class_id')
        shape(ref['site'],'source_method instruction'); method(ref['site']['source_method']); integer(ref['site']['instruction'])
        site = json.dumps(ref['site'],sort_keys=True); require(site not in sites, '重复ldftn位置'); sites.add(site)
        if ref['target'] is not None: method(ref['target'])
        text(ref['target_display']); require(ref['role'] in roles, '引用角色错误')
        for key in ('module_id','function_id','class_id'):
            if ref[key] is not None: integer(ref[key])
        require(ref['module_id'] is None or ref['module_id'] in modules, '引用模块不存在')
        if ref['role']=='declared_native_body':
            require(ref['module_id']==b['module_id'] and ref['target']==b['body'], '绑定body引用错误'); bodies += 1
    require(bodies==1, '需要唯一实际绑定body委托')
    for f in data['build_inputs']+data['artifacts']: file_entry(f)
    inputs = data['build_inputs']; artifacts = data['artifacts']
    require(len(inputs)==3 and len({x['relative_path'] for x in inputs})==3, '输入数量/身份错误')
    require({(x['relative_path'],x['role']) for x in inputs}=={
        ('managed-entry.dll','compiled_managed_input'),('RyuPy.Runtime.dll','runtime_managed_input'),
        ('selected-native-input','native_static_archive' if aot else 'native_shared')}, '输入角色错误')
    require(len({x['relative_path'] for x in artifacts})==len(artifacts), '重复产物')
    validate_runtime_dependencies(artifacts, rid)
    if aot:
        require(all(x['role'] in {'native_executable','unclassified_sidecar'} | RUNTIME_ROLES for x in artifacts), 'AOT产物角色错误')
        require(sum(x['role']=='native_executable' for x in artifacts)==1, '缺唯一native')
    else:
        core = [row for row in artifacts if row['role'] not in RUNTIME_ROLES]
        require(len(core)==4 and {x['role'] for x in core}=={'managed_entry','runtime_managed','runtimeconfig','native_shared'}, 'JIT四项核心产物错误')
        by_role = {x['role']:x for x in core}; managed = by_role['managed_entry']['relative_path']
        require('/' not in managed and managed.endswith('.dll'), '托管入口名错误')
        require(by_role['runtime_managed']['relative_path']=='RyuPy.Runtime.dll' and by_role['runtimeconfig']['relative_path']==managed[:-4]+'.runtimeconfig.json', '托管部署名错误')
        require(by_role['native_shared']['relative_path']==RELATIVE[rid], 'shared部署名错误')
        for role,input_role in [('managed_entry','compiled_managed_input'),('runtime_managed','runtime_managed_input'),('native_shared','native_shared')]:
            original = next(x for x in inputs if x['role']==input_role)
            require(all(original[key]==by_role[role][key] for key in ('size','sha256')), 'JIT产物不等于本次输入')
    return b


def verify(root, entry, rid, linkage, sdk, expected):
    """expected来自本次冻结声明/输入，不信清单自述或再次读取已变声明。"""
    root, entry = Path(root), Path(entry); plain_stat(root,directory=True)
    manifest = root/MANIFEST_NAME; require(plain_stat(manifest).st_size<=4*1024*1024, '清单过大')
    with manifest.open('rb') as stream:
        raw = stream.read(4*1024*1024+1)
    require(len(raw)<=4*1024*1024, '读取中清单增长')
    data = json.loads(raw.decode('utf-8'),object_pairs_hook=no_duplicate_keys)
    b = validate_schema(data,rid,linkage,sdk)
    for key in ('module','export','library','symbol','declaration_sha256'): require(b[key]==expected[key], '声明身份不符：'+key)
    native = next(x for x in data['build_inputs'] if x['relative_path']=='selected-native-input')
    require(all(native[key]==expected['native_input'][key] for key in ('size','sha256')), '实际原生输入不符')
    require({x['module_name']:x['compiled_text_sha256'] for x in data['sources']}==expected['sources'], '源码集合/字节不符')
    actual = check_artifacts(root,data['artifacts'],MANIFEST_NAME,windows=rid=='win-x64')
    role = 'native_executable' if linkage=='aot_static' else 'managed_entry'
    item = next(x for x in data['artifacts'] if x['role']==role)
    plain_stat(entry)
    require(entry.samefile(actual[item['relative_path']]) and entry.parent.samefile(actual[item['relative_path']].parent)
            and entry.name==Path(item['relative_path']).name, 'CLI入口身份错误')
    if linkage=='aot_static': image_header(entry,rid)
    return data


def relocate(root, entry, destination, rid, linkage, sdk, expected):
    root, destination = Path(root), Path(destination)
    data = verify(root,entry,rid,linkage,sdk,expected); destination.mkdir()
    for f in data['artifacts']:
        target = destination/f['relative_path']; target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copy2(root/f['relative_path'],target)
    shutil.copy2(root/MANIFEST_NAME,destination/MANIFEST_NAME)
    role = 'native_executable' if linkage=='aot_static' else 'managed_entry'
    target = destination/next(x['relative_path'] for x in data['artifacts'] if x['role']==role)
    verify(destination,target,rid,linkage,sdk,expected)
    return target,data
