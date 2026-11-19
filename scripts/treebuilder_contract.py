"""TreeBuilder 窄合同：逐字原流、同源身份、原Markdown配置及独立Native。"""
import hashlib
from pathlib import Path
import re
import textwrap_dedent_contract as shared
from re_scanner_contract import verify_process, verify_command

ROOT, require, sha, parse = shared.ROOT, shared.require, shared.sha, shared.parse
complete, isolated, IdentityError = shared.complete, shared.isolated, shared.IdentityError
MANIFEST = ROOT / 'scripts/treebuilder.json'
FIXTURE = 'tests/fixtures/treebuilder/'
GROUPS = ('core', 'flush', 'comments', 'factories', 'binding', 'errors', 'all')
TITLE = 'validation: treebuilder-state-001'
ORIGINAL = 'tests/cases/xml_element/treebuilder_event_state/entry.py'


def check_context(env, event):
    require(env.get('RUNNER_ENVIRONMENT') == 'github-hosted', '只允许hosted真实产品')
    require(event.get('head_commit', {}).get('message') == TITLE, '精确TreeBuilder标题不符')
    adjusted = dict(event, head_commit=dict(event['head_commit'], message=shared.old.shared.TITLE))
    shared.old.shared.check_context(env, adjusted)


def load_manifest(root=ROOT, path=MANIFEST):
    data = parse(path.read_bytes())
    require(data['schema_version'] == 1 and data['title'] == TITLE and data['default_project_delta'] == 0,
            '清单版本/标题/默认计数改变')
    require(data['cp_sha256'] == shared.old.shared.PYTHON_SHA and data['sdk_version'] == '10.0.403', '固定工具改变')
    actual = {str(p.relative_to(root)) for p in (root / FIXTURE).glob('*.py')}
    require(actual == set(data['sources']) - {ORIGINAL}, '夹具集合漏项/增项')
    for name, digest in {**data['sources'], **data['protected']}.items():
        path = root / shared.relative(name)
        require(path.is_file() and not path.is_symlink() and sha(path) == digest, '冻结源改变：' + name)
    require(data['sources'][ORIGINAL] == '43eda0a6de2c0f7e375fceb254a533de29110d4eb49d5d5d4dafb4be6ca20050', '原pending入口被修改')
    require(tuple(data['groups']) == GROUPS and data['includes'] == shared.old.shared.INCLUDES, '组/实际include改变')
    resource_request(data)
    require(len(data['markdown_modules']) == 27 and set(data['markdown_modules']) ==
            {r['module'] for r in data['markdown_plan']['modules']}, '真实27模块图改变')
    require(data['markdown_plan']['compiled_feature'] is True and
            data['markdown_plan']['session_policy'] == 'ordinary-dynamic-module-pilot', '原pilot配置改变')
    return data


def resource_request(data):
    names = sorted(set(data['markdown_modules']) - {'__main__'})
    require(len(names) == 26, '完整非入口资源锚点必须26个')
    request = {'schema_version': 1, 'modules': [{'module': n, 'files': []} for n in names]}
    require(data['markdown_resources'] == request, '资源缺模块/重复/额外payload')
    return request


def positive(prefix, argv, golden):
    record, streams = verify_process(prefix, argv, exits=(0, 1))
    require(golden and record['exit'] == 0 and streams == {'stdout': golden, 'stderr': b''}, '完整exit/stdout/stderr不符')
    return {'passed': True, 'status': 'passed', 'stdout_sha256': hashlib.sha256(golden).hexdigest()}


def verify_plan(path, case, aot=False):
    plan = parse(path.read_bytes())
    if case.get('business'):
        require(not aot and plan == case['plan'], '原Markdown实际27模块/Instance/Package策略改变')
    else:
        require(plan['compiled_feature'] is True and plan['session_policy'] == 'ordinary-dynamic-module-pilot', 'CLI feature不符')
        require(plan['modules'] == [{'compiled': True, 'execution': 'LegacySingleton', 'module': '__main__',
            'ordinary_name_handlers': False, 'reason': 'aot-not-migrated' if aot else 'entry-or-package',
            'relative_from_import': False}], '核心真实单入口图不符')
    return plan


def build_argv(runner, case, target, aot=False):
    entry = runner.inputs_dir / case['source']
    if case.get('business'):
        argv = shared.old.shared.build_command(runner.cli, entry, runner.inputs_dir, target)
        argv += ['--distribution-metadata', runner.inputs_dir / 'metadata-request.json']
    else:
        argv = [runner.cli, 'build', entry, '--diagnostic-stages', '--out-dir', target]
    if aot:
        argv += ['--aot']
        if case.get('business'): argv += ['--resources', runner.inputs_dir / 'markdown-resources.json']
    return argv


def verify_native(runner, case, layout):
    result = shared.verify_native(runner, case, layout)
    require(result['compiler']['options']['instance_modules'] == [], '核心不允许隐藏instance')
    require(result.get('resources', []) == [] and result.get('module_locations', []) == []
            and result['scope']['resources_included'] is False, '核心应无外部资源或源码依赖')
    return result


def qualified_oracles(data, root=ROOT):
    require(set(data['oracles']) == {*GROUPS, 'original', 'markdown-full', 'markdown-html', 'ordinary-bases', 'mutable-parent'}, 'oracle角色漏项/增项')
    for name, row in data['oracles'].items():
        require(row['status'] == 'observed' and row['python_sha256'] == data['cp_sha256']
                and type(row['exit']) is int and row['exit'] == 0 and row['failure'] is None and row['raw_streams_complete'] is True,
                'oracle尚未实际观测：' + name)
        expected_source = ORIGINAL if name == 'original' else (
            'tests/projects/entries/markdown_sample.py' if name == 'markdown-full' else
            FIXTURE + ('mutable_parent.py' if name == 'mutable-parent' else
                       'ordinary_bases.py' if name == 'ordinary-bases' else
                       'markdown_html.py' if name == 'markdown-html' else 'core.py'))
        require(row['source_sha256'] == sha(root / expected_source), 'oracle不是同源：' + name)
        for stream in ('stdout', 'stderr'):
            path = root / shared.relative(row[stream + '_file'])
            raw = path.read_bytes()
            require(row['streams'][stream] == {'bytes': len(raw), 'sha256': hashlib.sha256(raw).hexdigest()}, 'oracle双流身份改变')
            require(bool(raw) if stream == 'stdout' else raw == b'', 'oracle空结果/非空stderr')
    require(b''.join((root / data['oracles'][g]['stdout_file']).read_bytes() for g in GROUPS[:-1]) ==
            (root / data['oracles']['all']['stdout_file']).read_bytes(), 'all不是同六组有序串联')


def subclass_rejection(prefix, argv, source, message):
    record, streams = verify_process(prefix, argv, exits=(1,))
    stderr = streams['stderr'].decode('utf-8')
    require(not streams['stdout'] and stderr.count('[E0100]') == 1 and message in stderr
            and source.name in stderr and re.search(r':\d+:\d+', stderr)
            and all(s not in stderr for s in ('MissingMethod', 'UnknownName', '[E020', 'panic')),
            '必须真正达到准确新族有位置E0100，不接受任意首错')
    return {'status': 'expected_E0100', 'passed': True, 'cp_equivalent': False}


def native_eligible(stages):
    # 语义红不擦掉独立Native资格；来源或编译身份失败则不得执行。
    return all(stages.get(k, {}).get('passed') is True for k in ('identity', 'tools', 'sourceprep', 'core-compile', 'all-cp'))


def verify_markdown_native(runner, layout, dist=None, binary=None):
    """仅复用既有schema/ELF/metadata验证器，叠加本业务准确27源与26锚点合同。"""
    metadata = shared.metadata.product.contract
    manifest = metadata.manifest
    dist, binary = dist or layout.dist, binary or layout.native
    data = parse((dist / 'ryupy-artifact.json').read_bytes())
    report = parse((layout.managed.parent / 'distribution-metadata.json').read_bytes())
    sdk = shared.old.read_sdk_version(layout.host / 'sdk.version')
    require(sdk == runner.data['sdk_version'], 'Markdown实际SDK不符')
    manifest.validate_schema(metadata.schema_projection(data, report), 'linux-x64', sdk,
                             expected_instance_modules=('html.parser',))
    actual = manifest.check_artifacts(dist, data['artifacts'], 'ryupy-artifact.json', windows=False)
    native = next(x for x in data['artifacts'] if x['role'] == 'native_executable')
    require(binary.samefile(actual[native['relative_path']]), '实际Native不是清单唯一ELF')
    manifest.image_header(binary, 'linux-x64')
    modules = runner.data['markdown_modules']
    require(len(data['sources']) == 27 and {s['module_name'] for s in data['sources']} == set(modules), 'Markdown Native源集合不符')
    for row in data['sources']:
        name = row['module_name']; relative = Path(modules[name])
        package = name if relative.name == '__init__.py' else name.rpartition('.')[0]
        logical = 'entry/markdown_sample.py' if name == '__main__' else 'modules/' + name.replace('.', '/') + (
            '/__init__.py' if relative.name == '__init__.py' else '.py')
        require(row['compiled_text_sha256'] == sha(runner.inputs_dir / relative)
                and row['package'] == package and row['logical_source'] == logical, 'Native模块来源/Package/逻辑路径不符')
    expected = {s['module_name']: dict(module_id=s['module_id'], module_name=s['module_name'],
                kind='executable_relative', relative_file='_internal/resources/v1/' + s['logical_source'])
                for s in data['sources'] if s['module_name'] != '__main__'}
    require(len(data['module_locations']) == 26 and {r['module_name']: r for r in data['module_locations']} == expected,
            '完整26个可搬迁空资源锚点不符')
    require(data['resources'] == [] and data['scope']['resources_included'] is False
            and data['scope']['declared_resources_complete'] is True, '空files不能夹带资源')
    bindings = {r['role']: r['sha256'] for r in data['build_inputs']}
    require(bindings == {'runtime_managed_input': sha(runner.runtime), 'compiled_managed_input': sha(layout.managed)}
            and sha(layout.managed.parent / 'RyuPy.Runtime.dll') == sha(runner.runtime), '实际PE/runtime换源')
    options = data['compiler']['options']
    require(options['module_environment_policy'] == 'ordinary-dynamic-module-pilot'
            and all(not v for k, v in options.items() if k.startswith(('experimental_', 'diagnostic_'))), '实际编译策略改变')
    return data
