"""公开pyfiglet Native的独立CPU采样；采样开销不进入性能对照，不改安全配置或产品编译选项。"""
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tarfile
import urllib.request

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path[:0] = [str(HERE.parent/'deployment_project'), str(HERE.parent/'standalone_app/benchmark'), str(HERE.parent)]
from measure import capture, positive, require, save, sha, elf, inventory
from prepare import prepare
from public_build import project_command, inputs
from audit_native import audit
from aot_layout import reported_aot

WORKLOADS = {'hi': ['-f', 'standard', 'hi'],
             'longhi100': ['-f', 'standard', '-w', '20000', 'hi'*100],
             'profilehi800': ['-f', 'standard', '-w', '20000', 'hi'*800]}
MIN_SAMPLES = 200


def profile_command(perf, data, stderr, native, argv):
    # shell仅负责分开应用stderr与perf自身日志；负载全部为位置参数，不拼接进shell代码。
    return ['sudo', '-n', '--', str(perf), 'record', '-o', str(data), '-e', 'cpu-clock:u',
            '-F', '499', '--call-graph', 'dwarf,16384', '--', '/bin/sh', '-c',
            'stderr=$1; shift; exec "$@" 2>"$stderr"', 'profile-target', str(stderr), str(native), *argv]


def sample_count(text):
    # period/event不请求IP或symbol，避免把DWARF调用链帧误计为独立采样。
    rows = [line.strip() for line in text.splitlines() if line.strip() and not line.startswith('#')]
    require(rows and all(re.fullmatch(r'(?:\d+\s+cpu-clock:u:?|cpu-clock:u:?\s+\d+)', line) for line in rows), 'perf script采样格式不符')
    return len(rows)


def report_rows(text):
    rows = []
    for line in text.splitlines():
        if not line.strip() or line.lstrip().startswith('#'): continue
        fields = [part.strip() for part in line.split(';', 3)]
        if len(fields) != 4: continue
        try: percent, count = float(fields[0].removesuffix('%')), int(fields[1])
        except ValueError: continue
        require(0 <= percent <= 100 and count >= 0, 'perf report数值无效')
        rows.append({'percent': percent, 'samples': count, 'dso': fields[2], 'symbol': fields[3]})
    require(rows, 'perf report没有可解析的symbol行')
    return rows


def snapshot(repo):
    names = subprocess.check_output(['git', 'ls-files', '-z'], cwd=repo).decode().split('\0')
    return {name: sha(repo/name) for name in names if name and (repo/name).is_file()}


def main():
    require(os.environ.get('GITHUB_ACTIONS') == 'true' and os.environ.get('RUNNER_ENVIRONMENT') == 'github-hosted', '仅hosted')
    current = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    require(current == os.environ['GITHUB_SHA'], '候选身份不符')
    perf = Path(os.environ['PERF_BINARY']); require(perf.is_absolute(), 'perf须为绝对路径')
    perf = perf.resolve(); require(perf.is_file(), 'perf须为真实文件')
    out = Path(os.environ['RUNNER_TEMP'])/'native-hotspot-profile'; out.mkdir(exist_ok=True)
    require(not (out/'result.json').exists() and not (out/'tools').exists(), '已有profile证据，不覆盖')
    python = Path(os.environ['PYTHON']).resolve(); dotnet = Path(os.environ['DOTNET']).resolve()
    env = dict(os.environ, PYTHONDONTWRITEBYTECODE='1', PYTHONOPTIMIZE='0', PYTHONNOUSERSITE='1',
               DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1', MSBUILDDISABLENODEREUSE='1', UseSharedCompilation='false')
    source = snapshot(ROOT); save(out/'source-before.json', source)
    result = {'passed': False, 'candidate': current, 'tree': subprocess.check_output(['git', 'rev-parse', 'HEAD^{tree}'], cwd=ROOT, text=True).strip(),
              'profile_only': True, 'performance_comparison': 'not-measured', 'event': 'cpu-clock:u', 'frequency': 499,
              'stack': 'dwarf,16384', 'minimum_samples': MIN_SAMPLES}
    project = hidden = None
    def command(label, argv, cwd=ROOT, extra=None, timeout=900):
        rec = capture(out/label, argv, cwd, dict(env, **(extra or {})), timeout)
        require(rec['exit'] == 0 and rec['failure'] is None, label+'失败')
        return rec
    try:
        command('perf-version', [perf, '--version']); command('dotnet-info', [dotnet, '--info'])
        command('host-kernel', ['uname', '-a']); command('host-cpu', ['lscpu'])
        require(subprocess.check_output([dotnet, '--version'], env=env, text=True).strip() == '10.0.403', 'SDK漂移')
        require(subprocess.check_output([python, '-c', 'import platform;print(platform.python_version())'], env=env, text=True).strip() == '3.12.13', 'CP漂移')
        command('cli-build', ['cargo', 'build', '--locked', '--release', '-p', 'ryupy-cli'])
        command('runtime-build', [dotnet, 'build', ROOT/'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj', '-c', 'Release', '-nologo'])
        tools = out/'tools'; tools.mkdir(); cli = tools/'ryupy'; runtime = tools/'RyuPy.Runtime.dll'
        shutil.copy2(ROOT/'target/release/ryupy', cli)
        shutil.copy2(ROOT/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll', runtime)
        frozen = {str(p): sha(p) for p in (cli, runtime, python, dotnet, perf)}; save(out/'tools.json', frozen)
        recipe = json.loads((ROOT/'tests/aot/wcwidth_ci/project-source-recovery.json').read_text())
        item = next(x for x in recipe['packages'] if x['package'] == 'pyfiglet'); archive = out/item['filename']
        with urllib.request.urlopen(item['url'], timeout=120) as response: archive.write_bytes(response.read())
        require(sha(archive) == item['sha256'], '包下载hash不符'); save(out/'download.json', item)
        unpacked = out/'upstream'; unpacked.mkdir()
        with tarfile.open(archive) as bundle: bundle.extractall(unpacked, filter='data')
        project = prepare(out/'project', next(unpacked.iterdir())); original_inputs = inputs(project)
        save(out/'project-inputs.json', original_inputs); cwd = out/'unrelated-cwd'; cwd.mkdir()
        output = out/'public-build'
        command('public-build', project_command(cli, project, 'cli', str(output)), cwd, {'RYUPY_RUNTIME_DIR': str(tools)}, 1200)
        layout = reported_aot((out/'public-build.stderr').read_bytes(), output, '__main__', 'linux-x64'); elf(layout.native)
        audit(layout, project, 'cli', out/'audit', cli, runtime, python, dotnet, env)
        dist = out/'profile-dist'; shutil.copytree(layout.dist, dist); native = dist/layout.native.name
        before_dist = inventory(dist); save(out/'dist-before.json', before_dist)
        debug = list(dist.glob('*.dbg')); require(len(debug) == 1, '调试符号必须唯一且保留')
        command('debuglink', ['readelf', '-x', '.gnu_debuglink', native])
        command('symbols', ['nm', '-C', '--defined-only', debug[0]])
        require('RyuPy_Runtime_' in (out/'symbols.stdout').read_text(), 'dbg缺少真实runtime符号')
        golden = {}
        for name, argv in WORKLOADS.items():
            command(name+'-cp', [python, '-B', project/'package/pyfiglet/__main__.py', *argv], cwd,
                    {'PYTHONPATH': str(project/'package')}, 90)
            require((out/(name+'-cp.stderr')).read_bytes() == b'', 'CP stderr非空')
            golden[name] = (out/(name+'-cp.stdout')).read_bytes()
        hidden = out/'project-hidden'; project.rename(hidden)
        clean = {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC'}
        for name, argv in WORKLOADS.items():
            prefix = out/(name+'-native'); rec = capture(prefix, [native, *argv], cwd, clean, 90)
            positive(rec, prefix, golden[name])
        data = out/'perf.data'; app_stderr = out/'profile-app.stderr'
        rec = capture(out/'perf-record', profile_command(perf, data, app_stderr, native, WORKLOADS['profilehi800']), cwd, clean, 180)
        # 失败的perf.data也保留为可上传原件；权限调整仅限本次明确路径。
        if data.exists():
            command('perf-data-owner', ['sudo', '-n', '--', '/bin/chown', f'{os.getuid()}:{os.getgid()}', data], cwd)
        require(rec['exit'] == 0 and rec['failure'] is None, '真实perf采样失败；不降级为源码猜测')
        require((out/'perf-record.stdout').read_bytes() == golden['profilehi800'] and app_stderr.read_bytes() == b'', '采样时应用原流不匹配')
        require(data.is_file() and data.stat().st_size > 0, '无perf.data')
        command('perf-samples', [perf, 'script', '-i', data, '-F', 'period,event'], cwd)
        count = sample_count((out/'perf-samples.stdout').read_text()); require(count >= MIN_SAMPLES, '真实CPU采样数不足')
        command('perf-report', [perf, 'report', '-i', data, '--stdio', '--no-children', '--percent-limit', '0',
                               '--sort', 'dso,symbol', '--show-nr-samples', '--field-separator', ';'], cwd)
        rows = report_rows((out/'perf-report.stdout').read_text())
        own = [row for row in rows if Path(row['dso']).name == native.name]
        require(sum(row['samples'] for row in own) >= MIN_SAMPLES, '应用本体CPU采样不足')
        require(any('RyuPy_Runtime_' in row['symbol'] for row in own), '应用runtime采样未符号化')
        require(inventory(dist) == before_dist, '采样改变Native产物')
        hidden.rename(project); hidden = None
        require(inputs(project) == original_inputs, '输入改变')
        require(all(sha(p) == h for p,h in frozen.items()), '工具改变')
        after = snapshot(ROOT); save(out/'source-after.json', after); require(source == after, '源码改变')
        result.update(passed=True, samples=count, native_samples=sum(row['samples'] for row in own),
                      perf_data_sha256=sha(data), native_sha256=sha(native), debug_sha256=sha(debug[0]),
                      rows=rows, scope='真实公开Native采样；采样wall不作为A/B性能数据；未改变产品插桩/系统配置')
    finally:
        if hidden is not None and hidden.exists() and project is not None and not project.exists(): hidden.rename(project)
        save(out/'result.json', result)


if __name__ == '__main__': main()
