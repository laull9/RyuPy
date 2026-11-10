"""hosted专用原入口有界部署验证；先保全日志/产物，任何偏差停止且不重试。"""
import json
import os
from pathlib import Path
import re
import signal
import subprocess
import sys
import time
import traceback

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]
sys.path.insert(0, str(HERE.parent))
from aot_layout import reported_aot
from docker_image import prepare_image
from manifest_verify import verify_manifest, relocate_manifest, read_sdk_version, ManifestError
from native_evidence import preserve_dist, snapshot
from guards import (ARCHIVE_SHA, ENTRY_SHA, RESOURCE_SHA, GOLDEN_SHA, FONT_SHA,
                    require, sha, save, extract, source_unchanged, pollution, graph,
                    reported_managed, resource_record, remove_copy_font, deployment_files)


class Recorder:
    """每个label只运行一次；启动前创建原流文件，超时杀本次进程组，不重跑。"""
    def __init__(self, logs):
        self.logs = logs
        logs.mkdir()
        self.records = {}

    def run(self, label, command, *, cwd, env=None, check=False, timeout=180):
        require(label not in self.records, '禁止重复命令：' + label)
        command = list(map(str, command))
        record = {'argv': command, 'cwd': str(cwd), 'timeout': timeout,
                  'started': time.time(), 'environment': env}
        self.records[label] = record
        save(self.logs / (label + '.command.json'), record)
        out, err = self.logs / (label + '.stdout'), self.logs / (label + '.stderr')
        with out.open('xb') as stdout, err.open('xb') as stderr:
            try:
                process = subprocess.Popen(command, cwd=cwd, env=env, stdin=subprocess.DEVNULL,
                                           stdout=stdout, stderr=stderr, start_new_session=True)
            except OSError as error:
                # 未启动进程不能归为应用拒绝；保留空原流与环境失败原件后重抛。
                record.update(status='launch-failed', classification='environment', exit=None,
                              error=repr(error), finished=time.time())
                save(self.logs / (label + '.status.json'), record)
                raise
            try:
                record['exit'] = process.wait(timeout=timeout)
            except subprocess.TimeoutExpired:
                os.killpg(process.pid, signal.SIGKILL)
                record['exit'] = process.wait()
                record['timeout_expired'] = True
                raise
            finally:
                record['finished'] = time.time()
                save(self.logs / (label + '.status.json'), record)
        result = subprocess.CompletedProcess(command, record['exit'], out.read_bytes(), err.read_bytes())
        require(not check or result.returncode == 0, label + '失败，见完整原流')
        return result


def positive(result, golden):
    require((result.returncode, result.stdout, result.stderr) == (0, golden, b''),
            '正测三流不匹配，停止，不补API/重跑')


def missing(result, golden, report_path=None):
    # 只接受正常应用失败1；信号/崩溃样退出不是正确缺件拒绝，不匹配异常文案。
    code = result.returncode
    classification = ('unexpected-crash' if code < 0 or code >= 128 else
                      'unexpected-exit' if code != 1 else
                      'unexpected-output' if golden in result.stdout else 'expected-refusal')
    report = {'classification': classification, 'exit': code,
              'success_glyph_present': golden in result.stdout}
    if report_path is not None:
        save(report_path, report)
    require(classification == 'expected-refusal',
            f'缺字体native {classification}: exit={code}；见完整原流')
    return report


def base_environment(dotnet, runtime):
    # 不继承PYTHON启动注入、动态库注入或宿主包路径；每build显式三个工具变量。
    keys = ('PATH', 'HOME', 'RUNNER_TEMP', 'CARGO_HOME', 'RUSTUP_HOME', 'NUGET_PACKAGES')
    env = {key: os.environ[key] for key in keys if key in os.environ}
    env.update(LANG='C.UTF-8', LC_ALL='C.UTF-8', TZ='UTC', PYTHONNOUSERSITE='1',
               PYTHONHASHSEED='0', PYTHONDONTWRITEBYTECODE='1', DOTNET=str(dotnet),
               DOTNET_ROOT=str(dotnet.parent), RYUPY_RUNTIME_DIR=str(runtime.parent),
               DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1', DOTNET_MULTILEVEL_LOOKUP='0')
    return env


def execute(out, run):
    require(os.environ.get('GITHUB_ACTIONS') == 'true' and
            os.environ.get('RUNNER_ENVIRONMENT') == 'github-hosted', '仅允许GitHub hosted路线')
    require(sys.version_info[:3] == (3, 12, 13), '必须使用冻结CP3.12.13')
    dotnet = Path(os.environ['DOTNET']).resolve()
    runtime = REPO / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
    cli = REPO / 'target/debug/ryupy'
    env = base_environment(dotnet, runtime)
    source = out / 'source'
    entry = REPO / 'tests/projects/entries/pyfiglet_sample.py'
    golden_file = REPO / 'tests/projects/baselines/pyfiglet/stdout.txt'
    resources = HERE / 'pyfiglet-standard.resources.json'
    require(sha(entry) == ENTRY_SHA and sha(golden_file) == GOLDEN_SHA and
            sha(resources) == RESOURCE_SHA, '原入口/golden/资源合同被改变')
    golden = golden_file.read_bytes()
    recipe = json.loads((HERE.parent / 'wcwidth_ci/project-source-recovery.json').read_text())
    recipe = next(row for row in recipe['packages'] if row['package'] == 'pyfiglet')
    require(recipe['sha256'] == ARCHIVE_SHA, '锁源变更')
    archive = out / recipe['filename']
    run.run('source-fetch', ['curl', '--fail', '--location', '--show-error', '--max-time', '180',
            '--proto', '=https', '--proto-redir', '=https', recipe['url'], '-o', archive],
            cwd=out, env=env, check=True)
    members = extract(archive, source)
    root = source / 'pyfiglet-1.0.2'
    save(out / 'archive-members.json', members)
    # 固定官方镜像准备属于环境阶段，只hosted一次；产品运行禁止再pull。
    image = prepare_image(run, 'docker', out, env)
    save(out / 'phase.json', {'phase': 'environment-prepared', 'image': image})
    info = run.run('dotnet-info', [dotnet, '--info'], cwd=out, env=env, check=True)
    text = info.stdout.decode()
    require(re.findall(r'^\s*Version:\s*(\S+)', text, re.M)[:2] == ['10.0.403', '10.0.14']
            and re.findall(r'^\s*RID:\s*(\S+)', text, re.M) == ['linux-x64'], 'SDK/host/RID不符')
    # 只读stdlib检查不导入目标包；每次目标执行前重新扫描这些site/stdlib根。
    code = ('import sys,site,sysconfig,json; print(json.dumps(dict(version=list(sys.version_info[:3]),'
            'roots=site.getsitepackages()+[site.getusersitepackages()]+[p for p in sys.path if p]+'
            '[sysconfig.get_path("purelib"),sysconfig.get_path("platlib")],'
            'optimize=sys.flags.optimize,no_site=sys.flags.no_site)))')
    cpinfo = run.run('environment-readonly', [sys.executable, '-B', '-S', '-c', code],
                     cwd=out, env=env, check=True)
    require(not cpinfo.stderr, 'CP环境诊断stderr非空')
    cpinfo = json.loads(cpinfo.stdout)
    require(cpinfo['version'] == [3, 12, 13] and cpinfo['optimize'] == 0 and cpinfo['no_site'] == 1,
            'CP环境不符')
    roots = sorted(set(cpinfo['roots']))
    save(out / 'environment.json', cpinfo)
    # 工具构建不执行目标；本地准备阶段不调用本脚本。
    run.run('runtime-build', ['make', 'runtime'], cwd=REPO, env=env, check=True, timeout=300)
    run.run('cli-build', ['cargo', 'build', '--locked', '-p', 'ryupy-cli', '--features',
            'ryupy-driver/ordinary-dynamic-module-pilot'], cwd=REPO, env=env, check=True, timeout=1200)
    tools = [cli, runtime, dotnet, Path(sys.executable).resolve(), entry, golden_file,
             golden_file.with_name('meta.txt'), archive, resources]
    frozen = {str(path): sha(path) for path in tools}
    revision = run.run('source-revision', ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}'],
                       cwd=REPO, env=env, check=True).stdout.decode().splitlines()
    save(out / 'input-tools.json', {'files': frozen, 'source': revision, 'feature':
         'ryupy-driver/ordinary-dynamic-module-pilot', 'configuration': 'debug',
         'budget': {'cp': 1, 'control_jit': 1, 'explicit_jit': 1, 'publish': 1, 'native': 4}})

    def clean(label):
        cwd = out / ('cwd-' + label)
        save(out / 'phase.json', {'phase': 'pollution-preflight', 'target': label})
        try:
            cwd.mkdir()
        except OSError as error:
            save(out / (label + '-pollution.json'), {'phase': 'cwd-create', 'passed': False,
                 'observations': [{'path': str(cwd), 'error': repr(error)}]})
            raise
        pollution(cwd, entry, root, roots, report_path=out / (label + '-pollution.json'))
        save(out / 'phase.json', {'phase': 'product', 'target': label})
        return cwd

    def build(label, explicit, aot=False):
        target = out / label
        require(not target.exists(), '输出根必须独占')
        args = [cli, 'build', entry, '-I', root, '--diagnostic-stages', '--out-dir', target]
        if explicit:
            args += ['--include-module', 'pyfiglet.fonts']
        if aot:
            args += ['--aot', '--resources', resources]
        actual = run.run(label, args, cwd=clean(label), env=env, check=True, timeout=1200)
        if not aot:
            graph(target / 'module-environment.json', explicit)
        return target, actual

    save(out / 'phase.json', {'phase': 'product', 'environment_prepared': True})
    try:
        positive(run.run('cp', [sys.executable, '-B', '-S', entry], cwd=clean('cp'),
                         env=dict(env, PYTHONPATH=str(root)), timeout=30), golden)
        for arm, explicit in [('control', False), ('explicit', True)]:
            target, built = build(arm + '-build', explicit)
            dll = reported_managed(built.stderr, target)
            require(sha(dll.parent / 'RyuPy.Runtime.dll') == frozen[str(runtime)], 'JIT Runtime不符')
            save(out / (arm + '-artifacts.json'), snapshot(target)[0])
            actual = run.run(arm + '-jit', [dotnet, dll], cwd=clean(arm + '-jit'), env=env, timeout=30)
            positive(actual, golden)
        target, built = build('native-build', True, aot=True)
        layout = reported_aot(built.stderr, target, entry.stem, 'linux-x64')
        # 完整dist/构建原流已经落盘；先归档，任何后续校验失败仍可收件。
        original = preserve_dist(layout.dist, out)
        graph(layout.managed.parent / 'module-environment.json', True)
        sdk = read_sdk_version(layout.host / 'sdk.version')
        require(sdk == '10.0.403', '实际发布SDK不符')
        manifest = verify_manifest(layout.dist, layout.native, 'linux-x64', sdk)
        require(sha(layout.managed.parent / 'RyuPy.Runtime.dll') == frozen[str(runtime)], 'AOT Runtime不符')
        inputs = {row['role']: row for row in manifest['build_inputs']}
        require(inputs['runtime_managed_input']['sha256'] == frozen[str(runtime)] and
                inputs['compiled_managed_input']['sha256'] == sha(layout.managed), 'AOT build_inputs不符')
        expected_sources = {'__main__': entry, 'pyfiglet': root / 'pyfiglet/__init__.py',
                            'pyfiglet.version': root / 'pyfiglet/version.py',
                            'pyfiglet.fonts': root / 'pyfiglet/fonts/__init__.py'}
        require({row['module_name'] for row in manifest['sources']} == set(expected_sources),
                'manifest实际模块图改变')
        require(all(row['compiled_text_sha256'] == sha(expected_sources[row['module_name']])
                    for row in manifest['sources']), 'manifest源码SHA不匹配')
        record = resource_record(manifest)
        save(out / 'deployment-files.json', deployment_files(layout.dist, manifest))
        save(out / 'native-inputs.json', {'managed_sha256': sha(layout.managed), 'resource': record,
                                        'native_sha256': sha(layout.native)})
        positive(run.run('native-first', [layout.native], cwd=clean('native-first'), env=env, timeout=30), golden)
        moved = out / 'relocated'
        binary, _ = relocate_manifest(layout.dist, layout.native, moved, 'linux-x64', sdk)
        positive(run.run('native-relocated', [binary], cwd=clean('native-relocated'), env=env, timeout=30), golden)
        script = ('for tool in dotnet python python3 python3.12; do '
                  'if command -v "$tool" >/dev/null 2>&1; then exit 97; fi; done; '
                  'test -z "$(ls -A /tmp)" || exit 98; '
                  'for p in /usr/local/share/pyfiglet/standard.flf /usr/local/share/pyfiglet/standard.tlf; '
                  'do test ! -e "$p" && test ! -L "$p" || exit 99; done; exec "$1"')
        actual = run.run('native-isolated', ['docker', 'run', '--pull=never', '--rm', '--network', 'none',
            '--read-only', '--mount', f'type=bind,src={moved},dst=/app,readonly', '--workdir', '/tmp',
            '--entrypoint', '/bin/sh', image, '-c', script, 'pyfiglet', '/app/' + binary.name],
            cwd=clean('native-isolated'), env=env, timeout=30)
        positive(actual, golden)
        negative = out / 'missing-font'
        missing_binary, _ = relocate_manifest(layout.dist, layout.native, negative, 'linux-x64', sdk)
        removed = remove_copy_font(negative, layout.dist, root / 'pyfiglet/fonts/standard.flf', record)
        try:
            verify_manifest(negative, missing_binary, 'linux-x64', sdk)
        except ManifestError as error:
            # 不是启动时自动验证；独立记录check_artifacts缺文件拒绝。
            require(('missing=' + repr([removed])) in str(error) and 'extra=[]' in str(error),
                    '不是唯一指定缺文件的manifest拒绝')
            save(out / 'missing-manifest.json', {'removed': removed, 'error': str(error)})
        else:
            raise RuntimeError('缺字体manifest未拒绝')
        missing(run.run('native-missing', [missing_binary], cwd=clean('native-missing'), env=env, timeout=30),
                golden, out / 'missing-native-oracle.json')
        require(snapshot(layout.dist)[0] == original and snapshot(moved)[0] == original, '原/搬迁dist改变')
    finally:
        checks = {path: sha(Path(path)) == value for path, value in frozen.items()}
        source_unchanged(source, members)
        save(out / 'unchanged.json', checks)
        require(all(checks.values()), '冻结输入/工具改变')


def main():
    out = REPO / 'target/pyfiglet-deployment-evidence'
    out.mkdir(parents=True, exist_ok=False)
    recorder = Recorder(out / 'logs')
    result = {'status': 'stopped', 'default20': 'unchanged historical 10/8/2; not rerun here'}
    try:
        execute(out, recorder)
        result['status'] = 'explicit-deployment-passed'
    except BaseException as error:
        phase = out / 'phase.json'
        failure_phase = json.loads(phase.read_text()) if phase.exists() else {'phase': 'environment'}
        if recorder.records:
            label = next(reversed(recorder.records))
            if recorder.records[label].get('status') == 'launch-failed':
                failure_phase = {'phase': 'environment', 'status': 'launch-failed', 'target': label}
        result.update(error=repr(error), traceback=traceback.format_exc(), phase=failure_phase)
        raise
    finally:
        result['commands'] = list(recorder.records)
        save(out / 'result.json', result)


if __name__ == '__main__':
    main()
