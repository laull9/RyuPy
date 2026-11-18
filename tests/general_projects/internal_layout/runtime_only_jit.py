#!/usr/bin/env python3
"""真实Runtime-only JIT门：官方muxer/fxr/shared，无SDK、packs或全局dotnet PATH。

先探测空SDK清单与可用Runtime；两个原样手写程序以同次CPython确认输入有效。
构建必须明确给出中文SDK需求、官方下载和一键安装命令。驱动仅检查提示，不执行安装；
旧CLI可用--expect-sdk-failure保留缺SDK时暴露底层msbuild错误的历史红证。
"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / 'tests/cpython'))


CASES = (
    ('harbor_invoice', ['6', '19'], b''),
    ('copper_receipt', [], '海港 Ada\n35\n'.encode()),
)


def runtime_environment(root, home):
    """显式唯一安装根；消除SDK/MSBuild、架构覆盖、开发Runtime与Python寻址污染。"""
    env = {key: value for key, value in os.environ.items()
           if not key.upper().startswith(('DOTNET', 'MSBUILD', 'COREHOST', 'CORECLR',
                                          'NUGET', 'RYUPY_', 'PYTHON', 'LD_', 'DYLD_'))}
    env.update(DOTNET=str(root / 'dotnet'), DOTNET_ROOT=str(root),
               DOTNET_MULTILEVEL_LOOKUP='0', DOTNET_CLI_HOME=str(home),
               DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',
               PATH='/usr/bin:/bin', PYTHONDONTWRITEBYTECODE='1',
               PYTHONHASHSEED='0', LC_ALL='C.UTF-8', TZ='UTC')
    return env


def invoke(command, cwd, env, evidence, label, data=b''):
    """同次完整保存命令、三流及退出码；不生成普通文件摘要。"""
    command = list(map(str, command))
    result = subprocess.run(command, cwd=cwd, env=env, input=data,
                            capture_output=True, timeout=240)
    (evidence / (label + '-stdout.log')).write_bytes(result.stdout)
    (evidence / (label + '-stderr.log')).write_bytes(result.stderr)
    (evidence / (label + '.json')).write_text(json.dumps(
        {'command': command, 'cwd': str(cwd), 'exit': result.returncode},
        ensure_ascii=False, indent=2))
    return result


def successful(result, label):
    assert result.returncode == 0, (label, result.returncode, result.stderr.decode(errors='replace'))
    return result


def create_runtime_only(dotnet, destination):
    """复制muxer以固定安装根；只链接官方host/fxr与shared，不链接整个安装目录。"""
    source = dotnet.resolve().parent
    assert (source / 'host/fxr').is_dir() and (source / 'shared').is_dir(), source
    destination.mkdir()
    shutil.copy2(dotnet.resolve(), destination / 'dotnet')
    (destination / 'host').mkdir()
    (destination / 'host/fxr').symlink_to(source / 'host/fxr', target_is_directory=True)
    (destination / 'shared').symlink_to(source / 'shared', target_is_directory=True)
    assert {p.name for p in destination.iterdir()} == {'dotnet', 'host', 'shared'}
    assert not (destination / 'sdk').exists() and not (destination / 'packs').exists()
    return {'muxer_source': str(dotnet.resolve()), 'root': str(destination),
            'host_fxr_source': str((destination / 'host/fxr').resolve()),
            'shared_source': str((destination / 'shared').resolve()),
            'runtime_link_policy': 'only_host_fxr_and_shared', 'sdk': [], 'packs': []}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cli', type=Path, required=True)
    parser.add_argument('--python', type=Path, required=True, help='固定CPython3.12.13，同次oracle')
    parser.add_argument('--dotnet', type=Path, default=Path(shutil.which('dotnet') or 'dotnet'))
    parser.add_argument('--expect-sdk-failure', action='store_true', help='旧实现真实缺SDK红证')
    parser.add_argument('--evidence', type=Path, default=Path.home() / '.cache/ryupy/validation/runtime-only-jit')
    args = parser.parse_args()
    assert os.name == 'posix', '此驱动使用POSIX官方签名工具PATH；Windows由独立平台门验收'
    cli, python, dotnet = args.cli.resolve(), args.python.resolve(), args.dotnet.resolve()
    evidence = args.evidence.expanduser().resolve() / datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
    assert not evidence.is_relative_to(ROOT)
    evidence.mkdir(parents=True)
    print('EVIDENCE', evidence, flush=True)
    rows = []
    with tempfile.TemporaryDirectory(prefix='ryupy-runtime-only-') as temporary:
        work = Path(temporary).resolve()
        runtime = work / 'runtime-only'
        facts = create_runtime_only(dotnet, runtime)
        home = work / 'private-home'
        home.mkdir()
        env = runtime_environment(runtime, home)
        facts['environment'] = {key: env[key] for key in ('DOTNET', 'DOTNET_ROOT', 'DOTNET_MULTILEVEL_LOOKUP', 'PATH')}
        sdk = successful(invoke([runtime / 'dotnet', '--list-sdks'], work, env, evidence, 'list-sdks'), 'SDK清单')
        assert sdk.stdout.strip() == b'', sdk.stdout
        runtimes = successful(invoke([runtime / 'dotnet', '--list-runtimes'], work, env, evidence, 'list-runtimes'), 'Runtime清单')
        assert any(line.startswith('Microsoft.NETCore.App ') and
                   int(line.split()[1].split('.')[0]) >= 10 for line in runtimes.stdout.decode().splitlines()), runtimes.stdout
        assert shutil.which('dotnet', path=env['PATH']) is None, 'PATH不能提供另一全局dotnet'
        facts['runtime_inventory'] = runtimes.stdout.decode().splitlines()
        (evidence / 'runtime-layout.json').write_text(json.dumps(facts, ensure_ascii=False, indent=2))
        version = successful(invoke([python, '-I', '-B', '-c', 'import platform; print(platform.python_version())'],
                                    work, env, evidence, 'python-version'), 'CPython版本')
        assert version.stdout.strip() == b'3.12.13', version.stdout
        inputs = work / 'source-input'
        inputs.mkdir()
        for name, argv, stdin in CASES:
            entry = inputs / (name + '.py')
            shutil.copy2(HERE / 'cases' / entry.name, entry)
            oracle = successful(invoke([python, '-I', '-B', entry, *argv], work, env,
                                       evidence, name + '-oracle', stdin), name + ' oracle')
            output = work / (name + '-build')
            built = invoke([cli, '--native', 'build', entry, '--out-dir', output], work,
                           env, evidence, name + '-build')
            assert built.returncode != 0, '无SDK构建成功；请另行验收实际成功与搬迁合同'
            if args.expect_sdk_failure:
                assert b'SDK' in built.stderr and b'apphost' in built.stderr.lower(), built.stderr
                status = 'legacy_msbuild_sdk_failure_reproduced'
            else:
                text = built.stderr.decode('utf-8')
                for expected in (
                    '构建应用入口需要.NET SDK 10及以上', '当前检测到的SDK：无',
                    '官方下载（请选择SDK）：https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0',
                    '一键自动安装（微软官方脚本）',
                    'curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0',
                    '请设置DOTNET指向其中的dotnet，再重新构建',
                ):
                    assert expected in text, (expected, text)
                assert 'No .NET SDKs were found' not in text, '应在调用msbuild前给出产品诊断'
                status = 'missing_sdk_chinese_install_guidance_pass'
            rows.append({'case': name, 'status': status, 'installer_executed': False,
                         'oracle_exit': oracle.returncode})
            print('PASS', name, status, flush=True)
        shutil.rmtree(inputs)
        assert not inputs.exists()
    (evidence / 'results.json').write_text(json.dumps(rows, ensure_ascii=False, indent=2))
    (evidence / 'cleanup.json').write_text(json.dumps({'temporary_tree_removed': not work.exists()}))
    print('RESULTS', json.dumps(rows, ensure_ascii=False), flush=True)


if __name__ == '__main__':
    main()
