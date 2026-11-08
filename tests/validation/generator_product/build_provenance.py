"""读取既有baseline真实构建证据；不让手填hash或只有摘要JSON冒充本轮产物来源。"""
import json
from pathlib import Path
from product_contract import require, sha


def command_evidence(directory, commands, name, argv=None):
    found = [record for record in commands if record['name'] == name]
    require(len(found) == 1, '本轮命令缺失/重复：' + name)
    record = found[0]
    require(record.get('exit') == 0 and type(record.get('exit')) is int
            and not record.get('timeout_expired'), '本轮命令未正常成功：' + name)
    if argv is not None:
        require(record['argv'] == argv, '命令身份不符：' + name)
    for suffix in ('.stdout', '.stderr'):
        require((directory / (name + suffix)).is_file(), '命令原流缺失：' + name + suffix)
    return record


def compiler_evidence(directory, contract):
    commands = json.loads((directory / 'commands.json').read_text())
    command_evidence(directory, commands, 'git', ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}'])
    require((directory / 'git.stdout').read_text().splitlines() == [contract['commit'], contract['tree']],
            '真实构建git原流非最终HEAD/tree')
    command_evidence(directory, commands, 'runtime-build', ['make', 'runtime'])
    command_evidence(directory, commands, 'compiler-build', ['cargo', 'build', '--locked', '-p', 'ryupy-cli',
        '--features', 'ryupy-driver/ordinary-dynamic-module-pilot'])
    for name in ('rustc', 'cargo', 'dotnet'):
        command_evidence(directory, commands, name)
    identity = json.loads((directory / 'compiler-identity.json').read_text())
    require(identity['cli_sha256'] == contract['cli']['sha256']
            and identity['runtime_sha256'] == contract['runtime']['sha256'], '真实baseline构建与产品工具hash不符')
    require(identity['feature'] == 'ryupy-driver/ordinary-dynamic-module-pilot'
            and identity['configuration'] == 'debug', '真实CLI构建配置不符')
    files = ['commands.json', 'compiler-identity.json']
    files += [name + suffix for name in ('git', 'runtime-build', 'compiler-build', 'rustc', 'cargo', 'dotnet')
              for suffix in ('.stdout', '.stderr')]
    return {'directory': str(directory), 'files': {name: sha(directory / name) for name in files},
            'runtime_sha256': identity['runtime_sha256'], 'cli_sha256': identity['cli_sha256'],
            'commit': contract['commit'], 'tree': contract['tree']}


def freeze(path):
    data = json.loads(Path(path).read_text())
    require(data['contract']['frozen'] is True and data['compiler_provenance']['files'], '产品freeze缺真实构建provenance')
    require(data['compiler_provenance']['runtime_sha256'] == data['contract']['runtime']['sha256'], 'freeze runtime来源不符')
    require(data['candidate']['head'] == data['contract']['commit'] and not data['candidate']['status'], '产品freeze非clean最终提交')
    return data
