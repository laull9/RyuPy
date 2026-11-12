"""默认公开入口AOT联合门；三图、三个ELF、八次Native，不把检查器通过当产品。"""
import os
import hashlib
import stat
import re
import shutil
import sys
import traceback
from pathlib import Path
from checking import require, parse, sha, build_argv, boundary_expected
from support import Runner, ROOT
sys.path.append(str(ROOT / "tests/aot/instance_class"))
sys.path.append(str(ROOT / "tests/aot/wcwidth_ci"))

HERE = Path(__file__).resolve().parent
TITLE = 'validation: default-source-aot-001'


def context():
    event = parse(Path(os.environ['GITHUB_EVENT_PATH']).read_bytes())
    require(bool(re.fullmatch('[0-9a-f]{40}', os.environ.get('GITHUB_SHA', ''))) and
            os.environ.get('GITHUB_ACTIONS') == 'true' and
            os.environ.get('GITHUB_RUN_ATTEMPT') == '1' and
            event.get('head_commit', {}).get('id') == event.get('after') == os.environ.get('GITHUB_SHA') and
            event.get('ref') == os.environ.get('GITHUB_REF') and
            os.environ.get('RUNNER_ENVIRONMENT') == 'github-hosted' and
            os.environ.get('GITHUB_EVENT_NAME') == 'push' and
            os.environ.get('GITHUB_REF') == 'refs/heads/ci/wcwidth-legacy-aot-2909056' and
            event.get('head_commit', {}).get('message') == TITLE, '非精确hosted窗口')
    require('RYUPY_DEFAULT_AOT_SOURCE_MANIFEST' not in os.environ, 'hosted不借本地身份')


def verify_blob(path, mode, blob):
    info = path.lstat()
    require(stat.S_ISREG(info.st_mode) and mode in ('100644', '100755'), '非普通Git源码')
    require(stat.S_IMODE(info.st_mode) == int(mode[-3:], 8), '源码实际mode不符')
    raw = path.read_bytes()
    actual = hashlib.sha1(b'blob ' + str(len(raw)).encode() + b'\0' + raw).hexdigest()
    require(actual == blob, '源码字节与Git索引不同')


def source_identity(r, label):
    r.checked(label, ['git', 'ls-files', '-s', '-z'])
    rows = []
    for raw in (r.out / (label + '.stdout')).read_bytes().split(b'\0'):
        if not raw: continue
        fields, path = raw.split(b'\t', 1)
        mode, blob, stage = fields.decode().split(); path = os.fsdecode(path)
        require(stage == '0' and (ROOT/path).is_file() and not (ROOT/path).is_symlink(), '源索引非法')
        verify_blob(ROOT/path, mode, blob)
        rows.append({'path': path, 'mode': mode, 'blob': blob, 'sha256': sha(ROOT/path)})
    r.save(label+'-files.json', rows)
    return rows


def prepare(r):
    r.checked('head', ['git', 'rev-parse', 'HEAD'])
    require((r.out/'head.stdout').read_text().strip() == os.environ['GITHUB_SHA'], 'HEAD变化')
    r.before = source_identity(r, 'source-before'); r.freeze([ROOT/x['path'] for x in r.before])
    r.checked('format', ['cargo', 'fmt', '--all', '--check'], 120)
    r.checked('runtime-build', ['dotnet', 'build', 'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj', '-c', 'Release', '-nologo'], 300)
    r.checked('cli-build', ['cargo', 'build', '--locked', '-p', 'ryupy-cli', '--features',
                          'ryupy-driver/ordinary-dynamic-module-pilot'], 1200)
    tools = r.out/'tools'; tools.mkdir()
    r.cli = tools/'ryupy'; r.runtime = tools/'RyuPy.Runtime.dll'
    shutil.copy2(ROOT/'target/debug/ryupy', r.cli)
    shutil.copy2(ROOT/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll', r.runtime)
    r.env.update(RYUPY_RUNTIME_DIR=str(tools), DOTNET=str(r.dotnet), DOTNET_ROOT=str(r.dotnet.parent))
    r.freeze([r.cli, r.runtime, r.dotnet, r.python])
    r.inputs_dir = r.inputs()
    shutil.copy2(ROOT/'tests/fixtures/treebuilder/markdown_html.py', r.inputs_dir/'markdown_html.py')
    r.fixture = r.out/'fixtures'; shutil.copytree(HERE/'fixtures', r.fixture)
    rebuilt = parse((r.fixture/'REBUILT-ORACLE.json').read_bytes())
    require(rebuilt['cpython_sha256'] == sha(r.python), '新CP来源不符')
    for row in rebuilt['files']:
        path = r.fixture/row['path']
        require(path.is_file() and path.stat().st_size == row['size'] and sha(path) == row['sha256'], '新fixture身份变化')
    semantic = (r.fixture/'semantic.stdout').read_bytes()
    boundary = (r.fixture/'boundary-cp.stdout').read_bytes()
    r.cases = [
        {'name': 'markdown_sample', 'entry': r.inputs_dir/'markdown_sample.py', 'root': r.inputs_dir/'markdown',
         'executions': [([], (r.inputs_dir/'stdout.txt').read_bytes())]},
        {'name': 'markdown_html', 'entry': r.inputs_dir/'markdown_html.py', 'root': r.inputs_dir/'markdown',
         'executions': [([], (ROOT/'tests/fixtures/treebuilder/oracle/markdown-html.stdout').read_bytes())]},
        {'name': 'native_entry', 'entry': r.fixture/'native_entry.py', 'root': r.fixture,
         'executions': [(['semantic'], semantic), (['boundary'], boundary_expected(boundary))]}]
    require([len(c['executions'][0][1]) for c in r.cases[:2]] == [77, 102], '原业务黄金变化')
    r.freeze([p for root in (r.inputs_dir, r.fixture) for p in root.rglob('*') if p.is_file()])
    r.save('cases.json', [{'name': c['name'], 'entry': str(c['entry']), 'root': str(c['root']),
                           'executions': [{'argv': a, 'stdout_sha256': __import__('hashlib').sha256(g).hexdigest()}
                                          for a, g in c['executions']]} for c in r.cases])


def managed(r):
    for case in r.cases:
        label = case['name']; out = r.out/'jit'/label
        r.checked(label+'-jit-build', build_argv(r.cli, case['entry'], out, case['root'], False), 180)
        pe = out/(case['entry'].stem+'.dll')
        require(pe.is_file() and sha(out/'RyuPy.Runtime.dll') == sha(r.runtime), 'JIT实际输入不符')
        r.freeze([pe, out/'RyuPy.Runtime.dll'])
        for n, (args, golden) in enumerate(case['executions']):
            r.run_positive(label+'-jit-'+str(n), [r.dotnet, pe, *args], golden)


def structure(r):
    from native_checks import audit_structure
    for case in r.cases:
        name = case['name']; folder = r.out/'structure'/name
        r.env.update(RYUPY_DEFAULT_AOT_ENTRY=str(case['entry']), RYUPY_DEFAULT_AOT_ROOT=str(case['root']),
                     RYUPY_DEFAULT_AOT_OUTPUT=str(folder))
        r.named_test(name+'-structure', ['cargo', 'test', '--locked', '-p', 'ryupy-driver', '--features',
                     'ordinary-dynamic-module-pilot', 'default_aot::tests::default_aot_real_graph_emit', '--',
                     '--ignored', '--exact', '--nocapture'], 'default_aot::tests::default_aot_real_graph_emit', 1200)
        case['structure'] = folder
        audit_structure(r, case)
    for key in ('RYUPY_DEFAULT_AOT_ENTRY', 'RYUPY_DEFAULT_AOT_ROOT', 'RYUPY_DEFAULT_AOT_OUTPUT'):
        r.env.pop(key, None)


def native(r):
    from native_checks import audit_native
    for case in r.cases:
        name = case['name']; target = r.out/'native-build'/name; label = name+'-publish'
        record = r.command(label, build_argv(r.cli, case['entry'], target, case['root'], True), 600)
        saved = r.out/(name+'-publish-inputs'); saved.mkdir()
        for path in target.rglob('*') if target.exists() else []:
            if not path.is_file() or path.is_symlink() or any(p in {'obj','bin','publish'} for p in path.relative_to(target).parts): continue
            if path.suffix in ('.dll', '.csproj', '.json', '.rsp') or path.name in ('Program.cs', 'sdk.version', 'publish.stdout.log', 'publish.stderr.log'):
                dest = saved/path.relative_to(target); dest.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(path, dest)
        require(record['exit'] == 0 and record['failure'] is None and record['raw_streams_complete'], 'publish失败，保原流')
        layout = r.native_layout(label, target, case['entry'])
        audit_native(r, case, layout)
        with r.isolated_sources([r.inputs_dir, r.fixture], name):
            for n, (args, golden) in enumerate(case['executions']):
                r.native_pair(name+'-'+str(n), layout, golden, args)


def main():
    context(); r = Runner()
    try:
        r.phase('prepare', lambda: prepare(r))
        r.phase('default-jit', lambda: managed(r))
        r.phase('structure', lambda: structure(r))
        r.phase('default-native', lambda: native(r))
        require(source_identity(r, 'source-after') == r.before, '全源前后变化')
        r.result.update(passed=True, scope='默认AOT三图/8执行；非任意数据资源/全部trim证明')
    except Exception:
        r.result['exception'] = traceback.format_exc()
        raise
    finally:
        r.save('result.json', r.result)


if __name__ == '__main__': main()
