"""dedent有限验收合同：严格路径/模式/双流/来源核验；只复用已有采集器与清单检查。"""
import collections
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tests/validation'))
import distribution_metadata as metadata
import typing_noreturn as old
from distribution_metadata_identity import IdentityError, inventory, isolated, mapped
import manifest_verify

require, sha, save = old.require, old.sha, old.save
TITLE = 'validation: textwrap-dedent-001'
MANIFEST = ROOT / 'tests/validation/textwrap_dedent.json'


def parse(raw):
    return json.loads(raw, object_pairs_hook=manifest_verify.no_duplicate_keys)


def relative(value):
    require(type(value) is str and value and '\\' not in value, '路径不是普通相对路径')
    path = PurePosixPath(value)
    require(not path.is_absolute() and '..' not in path.parts and str(path) == value, '路径越界/不规范')
    return Path(value)


def load_manifest(root=ROOT, path=MANIFEST):
    data = parse(path.read_bytes())
    require(data['schema_version'] == 1 and data['title'] == TITLE
            and data['cp_version'] == '3.12.13' and data['sdk_version'] == '10.0.403', '清单模式/版本改变')
    require(data['default_project_delta'] == 0, '不得改默认项目分子')
    for name, digest in data['sources'].items():
        source = root / relative(name)
        require(source.is_file() and source.resolve() == source.absolute() and not source.is_symlink() and sha(source) == digest, '冻结源码身份不符：' + name)
    actual = {str(p.relative_to(root)) for p in (root / 'tests/fixtures/textwrap_dedent').rglob('*.py')}
    require(actual == set(data['sources']), 'fixture源码集合有漏项/增项')
    ids = []
    for case in data['cases']:
        require(case['mode'] in ('cp', 'ni', 'pending') and re.fullmatch('[a-z][a-z0-9_-]*', case['id']), '非法场景模式/id')
        ids.append(case['id'])
        require(case['source'] in data['sources'] and set(case['modules']) == {'__main__', *case['include']}, '来源/include图不完整')
        require(case['modules']['__main__'] == case['source'], '入口未绑定__main__')
        require(all(p in data['sources'] for p in case['modules'].values()), '模块源码未冻结')
        for name in case['include']:
            require(re.fullmatch(r'[A-Za-z_]\w*', name) and case['modules'][name] == str(Path(case['source']).parent / (name + '.py')), 'include来源不在实际搜索根/模块路径不符')
        require(len(case['include']) == len(set(case['include'])), 'include重复')
        require(type(case['argv']) is list and all(type(x) is str for x in case['argv']), 'argv非法')
        require(type(case['stdout']) is str and case['stdout'], '完整stdout缺失')
        require(case['native'] == (case['mode'] != 'pending'), 'pending不得计Native支持；其它新域必须Native')
        if case['mode'] in ('cp', 'pending'):
            oracle = case['oracle']
            require(oracle['python_sha256'] == old.shared.PYTHON_SHA and oracle['source_sha256'] == data['sources'][case['source']]
                    and oracle['stdout_sha256'] == hashlib.sha256(case['stdout'].encode()).hexdigest()
                    and oracle['stdout_bytes'] == len(case['stdout'].encode()) and oracle['stderr_bytes'] == oracle['exit'] == 0, '同源CP完整oracle身份不符')
        require(case['mode'] != 'pending' or b'# ryupy: pending' in (root / case['source']).read_bytes(), 'CP全正合同丢pending')
    require(len(ids) == len(set(ids)), '场景id重复')
    require(set(data['facts']) and len(data['facts']) == len(set(data['facts'])), 'C#facts缺失/重复')
    return data


def check_context(env, event):
    require(env.get('RUNNER_ENVIRONMENT') == 'github-hosted', '只允许原hosted runner')
    require(event.get('head_commit', {}).get('message') == TITLE, 'dedent标题字节不符')
    adjusted = dict(event, head_commit=dict(event['head_commit'], message=old.shared.TITLE))
    old.shared.check_context(env, adjusted)


def complete(record):
    return record['exit'] == 0 and record['failure'] is None and record['raw_streams_complete'] is True


def verify_output(record, prefix, stdout):
    # NI只接受fixture内精确捕获后的exit0 marker；signal/timeout/其它异常绝不算支持或预期拒绝。
    require(complete(record) and prefix.with_suffix('.stdout').read_bytes() == stdout.encode()
            and prefix.with_suffix('.stderr').read_bytes() == b'', '完整exit/stdout/stderr不符，见原流')


def verify_trx(path, facts, legacy=False):
    rows = ET.parse(path).findall('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult')
    require(rows and all(row.get('outcome') == 'Passed' for row in rows), '零测试/失败/skip不是通过')
    names = [row.attrib['testName'].removeprefix('RyuPy.Runtime.Tests.') for row in rows]
    actual = collections.Counter(name for name in names if not legacy or name.startswith('PyTextDedentTests.'))
    require(actual == collections.Counter(facts), '专用C#facts漏项/重复/越界')
    if legacy:
        prefixes = ('PyTextWrapTests.', 'PyTextWrapperClassTests.', 'PyTextWrapperAlgorithmTests.')
        require(all(name.startswith(('PyTextDedentTests.', *prefixes)) for name in names), '混入域外C#')
        require(all(any(name.startswith(p) for name in names) for p in prefixes), '旧TextWrapper类漏跑')
    return {'total': len(rows), 'new_facts': len(facts), 'skipped': 0, 'all_passed': True}


def build_argv(runner, case, target, aot=False):
    entry = runner.inputs_dir / case['source']
    argv = [runner.cli, 'build', entry, '--diagnostic-stages', '--out-dir', target]
    if case['include']:
        argv += ['-I', entry.parent]
        for name in case['include']: argv += ['--include-module', name]
    return argv + (['--aot'] if aot else [])


def verify_plan(path, case, aot=False):
    plan = parse(path.read_bytes())
    require(plan['compiled_feature'] is True and plan['session_policy'] == 'ordinary-dynamic-module-pilot', '编译模式不符')
    modules = plan['modules']
    require(len(modules) == len(case['modules']) and {row['module'] for row in modules} == set(case['modules']), '实际编译图漏source re/多源')
    require(all(row['compiled'] is True for row in modules), '仅旁置源未进入编译图')
    if aot:
        require(all(row['execution'] == 'LegacySingleton' and row['reason'] == 'aot-not-migrated'
                    and row['ordinary_name_handlers'] is False and row['relative_from_import'] is False
                    for row in modules), 'Native实际模式不符')
    return plan


def verify_native(runner, case, layout):
    sdk = old.read_sdk_version(layout.host / 'sdk.version')
    require(sdk == runner.data['sdk_version'], '实际publish SDK不符')
    manifest = old.verify_manifest(layout.dist, layout.native, 'linux-x64', sdk)
    sources = manifest['sources']
    require(len(sources) == len(case['modules']) and {s['module_name'] for s in sources} == set(case['modules']), 'Native源码图不符')
    for row in sources:
        name = row['module_name']; source = case['modules'][name]
        logical = 'entry/' + Path(source).name if name == '__main__' else 'modules/' + name.replace('.', '/') + '.py'
        require(row['compiled_text_sha256'] == runner.data['sources'][source]
                and row['logical_source'] == logical and row['package'] == '', '实际Native逐源绑定不符')
    require(next(s['module_id'] for s in sources if s['module_name'] == '__main__') == manifest['entry']['module_id'], '入口module_id错误')
    bindings = {row['role']: row for row in manifest['build_inputs']}
    require(bindings['runtime_managed_input']['sha256'] == sha(runner.runtime) == sha(layout.managed.parent / 'RyuPy.Runtime.dll')
            and bindings['compiled_managed_input']['sha256'] == sha(layout.managed), 'Native PE/runtime绑定不符')
    options = manifest['compiler']['options']
    require(options['module_environment_policy'] == 'ordinary-dynamic-module-pilot' and options['instance_modules'] == []
            and all(not value for key, value in options.items() if key.startswith(('experimental_', 'diagnostic_'))), 'Native选项变更')
    verify_plan(layout.managed.parent / 'module-environment.json', case, True)
    return manifest
