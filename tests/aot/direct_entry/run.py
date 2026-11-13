"""公开AOT单次构建与搬迁入口差分；保留原流，不宣称干净机或性能通过。"""
import argparse
import json
import os
from pathlib import Path
import re
import shutil
import sys
import time
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))
from aot_layout import reported_aot
from construction_smoke import inventory, require, file_record, write_json
from manifest_files import check_artifacts
from platform_runner import Runner
from platform_support import clean_environment

CASES = {'success': ['success'], 'exit7': ['exit7'], 'exittext': ['exittext'],
         'error': ['error'], 'argv': ['argv', '', '中文', '$();% &'], 'flush': ['flush']}
CODES = {'exit7': 7, 'exittext': 1, 'error': 1}
OUTPUT = {'success': 'entry-ok\n|callback', 'exit7': '|callback',
          'exittext': '|callback', 'error': '|callback',
          'argv': '0 \n2 中文\n7 $();% &\n|callback', 'flush': 'buffered|callback'}


def error_signature(raw, *, oracle):
    # 只去除fixture源码行及源目录；不忽略帧、行号、函数名、额外日志或异常链。
    lines = raw.decode('utf-8').splitlines()
    require(lines and lines[0] == 'Traceback (most recent call last):', '缺少唯一traceback头')
    source = (HERE/'entry_probe.py').read_text(encoding='utf-8').splitlines()
    frames = []; index = 1
    while index < len(lines) - 1:
        match = re.fullmatch(r'  File "([^"]+)", line (\d+), in (.+)', lines[index])
        require(match is not None, '非预期traceback内容')
        name, number, function = match.groups(); number = int(number)
        require(name.replace('\\', '/').rsplit('/', 1)[-1] == 'entry_probe.py', '非fixture帧')
        frames.append((number, function)); index += 1
        if oracle and index < len(lines)-1 and lines[index].startswith('    '):
            require(0 < number <= len(source) and lines[index].strip() == source[number-1].strip(), '源码行不同')
            index += 1
    require(frames == [(24, '<module>'), (11, 'fail')], '帧集合或行号改变')
    require(lines[-1] == 'ValueError: direct-entry-error', '异常类型/消息改变')
    return frames, lines[-1]


def compare(case, reference, actual):
    require(reference.returncode == actual.returncode == CODES.get(case, 0), case+':退出码不同')
    expected = OUTPUT[case].replace('\n', os.linesep).encode('utf-8')
    require(reference.stdout == actual.stdout == expected, case+':stdout不同')
    if case == 'error':
        require(error_signature(reference.stderr, oracle=True) == error_signature(actual.stderr, oracle=False), '异常不同')
    else:
        expected_error = ('direct-entry-text'+os.linesep).encode() if case == 'exittext' else b''
        require(reference.stderr == actual.stderr == expected_error, case+':stderr不同')


def verify_entry(layout, rid):
    text = (layout.host/'Host.csproj').read_text(encoding='utf-8')
    project = ET.fromstring(text)
    require(not any(word in text for word in ('TrimmerRoot', 'RootAll', 'DynamicDependency', 'NoWarn', 'WarningsNotAsErrors', 'SuppressTrim')), '额外保根或警告屏蔽')
    refs = [item.get('Include') for item in project.findall('./ItemGroup/Reference')]
    for name in ('PublishAot', 'TreatWarningsAsErrors', 'IlcTreatWarningsAsErrors', 'ILLinkTreatWarningsAsErrors'):
        require(project.findtext('./PropertyGroup/'+name) == 'true', name+'未严格开启')
    require(rid in ('linux-x64', 'osx-arm64', 'win-x64'), '未知入口平台')
    properties = {'SkipCompilerExecution': 'true', 'ProduceReferenceAssembly': 'false',
                  'GenerateAssemblyInfo': 'false', 'GenerateTargetFrameworkAttribute': 'false', 'UseAppHost': 'false'}
    require(project.findtext('./PropertyGroup/TrimmerSingleWarn') == 'false', '裁剪警告被折叠')
    if rid == 'win-x64':
        require(refs == ['entry_probe', 'RyuPy.Runtime'], 'Windows双引用改变')
        require([item.get('Include') for item in project.findall('./ItemGroup/Compile')] == ['Program.cs'], 'Windows编译入口改变')
        expected = b'internal static class Program { private static int Main(string[] args) => global::RyuPy.Generated.Module.Main(args); }\n'
        require((layout.host/'Program.cs').read_bytes() == expected, 'Windows薄入口改变')
        for name in properties:
            require(not project.findall('./PropertyGroup/'+name), 'Windows混入直接入口属性')
        require(not project.findall('.//Copy'), 'Windows混入直接入口Copy')
        return {'mode': 'thin-host', 'actual_ilc_input_checked': False}
    require(refs == ['RyuPy.Runtime'] and not project.findall('./ItemGroup/Compile'), '直接入口仍有宿主编译或输入引用')
    require(not (layout.host/'Program.cs').exists(), '直接入口仍写薄宿主')
    for name, expected in properties.items():
        values = project.findall('./PropertyGroup/'+name)
        require(len(values) == 1 and values[0].text == expected, '直接入口属性不同：'+name)
    targets = [target for target in project.findall('./Target') if target.get('BeforeTargets') == 'CoreCompile']
    require(len(targets) == 1, '必须有唯一CoreCompile前置目标')
    target = targets[0]
    require(set(target.attrib) == {'Name', 'BeforeTargets'}, '直接入口目标存在额外条件')
    copies = project.findall('.//Copy')
    require(len(copies) == 1 and list(target) == copies, '前置目标必须仅复制一次输入')
    escaped = ''.join('%'+format(ord(ch), '02X') if ch in "%$@;'()*?" else ch
                      for ch in str(layout.managed.resolve(strict=True)))
    require(copies[0].attrib == {'SourceFiles': escaped, 'DestinationFiles': '@(IntermediateAssembly)'},
            'Copy不是本轮managed到SDK中间程序集')
    responses = list(layout.host.rglob('*.ilc.rsp'))
    require(len(responses) == 1, 'ILC响应不唯一')
    args = responses[0].read_text(encoding='utf-8-sig').splitlines()
    inputs = [arg.strip('"') for arg in args if arg and not arg.startswith('-')]
    require(len(inputs) == 1, 'ILC主输入不唯一')
    entry = Path(inputs[0]); entry = entry if entry.is_absolute() else layout.host/entry
    require(entry.resolve().is_relative_to(layout.host.resolve()), 'ILC主输入不在本次工作根')
    require(file_record(entry) == file_record(layout.managed), 'ILC主输入不是本次managed字节')
    return {'mode': 'direct', 'actual_ilc_input_checked': True,
            'input': str(entry), 'digest': file_record(entry), 'response': str(responses[0])}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for arg in ('ryupy', 'python', 'out'):
        parser.add_argument('--'+arg, required=True, type=Path)
    parser.add_argument('--rid', required=True, choices=['linux-x64', 'osx-arm64', 'win-x64'])
    args = parser.parse_args()
    cli, python, out = args.ryupy.resolve(), args.python.resolve(), args.out.resolve()
    out.mkdir(parents=True, exist_ok=False)
    logs = out/'logs'; logs.mkdir()
    runner = Runner(logs, deadline=time.monotonic()+1500)
    result = {'passed': False, 'rid': args.rid, 'clean_machine': False, 'runs': []}
    source = out/'source'; source.mkdir(); script = source/'entry_probe.py'
    shutil.copy2(HERE/'entry_probe.py', script)
    cwd = out/'unrelated-cwd'; cwd.mkdir()
    tools = {str(path): file_record(path) for path in (cli, python)}
    result['fixture'] = file_record(script)
    try:
        env = dict(os.environ, PYTHONIOENCODING='utf-8', PYTHONDONTWRITEBYTECODE='1',
                   PYTHONNOUSERSITE='1', DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1',
                   MSBUILDDISABLENODEREUSE='1', UseSharedCompilation='false')
        references = {}
        for case, argv in CASES.items():
            references[case] = runner.run('cp-'+case, [str(python), '-B', '-S', str(script), *argv], cwd=cwd, env=env, timeout=30)
            compare(case, references[case], references[case]) if case != 'error' else error_signature(references[case].stderr, oracle=True)
        build = out/'build'
        built = runner.run('public-build', [str(cli), 'build', str(script), '--aot', '-o', str(build)], cwd=cwd, env=env, check=True, timeout=1200)
        layout = reported_aot(built.stderr, build, 'entry_probe', args.rid)
        result['entry'] = verify_entry(layout, args.rid)
        manifest = json.loads(layout.manifest.read_text(encoding='utf-8'))
        check_artifacts(layout.dist, manifest['artifacts'], layout.manifest.name, windows=args.rid == 'win-x64')
        before = inventory(layout.dist)
        moved = out/'moved-dist'; shutil.copytree(layout.dist, moved)
        require(inventory(moved) == before, '搬迁改变交付字节')
        require(file_record(script) == result['fixture'], '构建改变输入源码')
        source.rename(out/'source-hidden')
        native_env = clean_environment(cwd)
        for place, directory in [('original', layout.dist), ('moved', moved)]:
            for case, argv in CASES.items():
                actual = runner.run(place+'-'+case, [str(directory/layout.native.name), *argv], cwd=cwd, env=native_env, timeout=30)
                compare(case, references[case], actual)
                result['runs'].append(place+'-'+case)
            require(inventory(directory) == before, '运行改变交付内容')
        require(all(file_record(Path(path)) == digest for path, digest in tools.items()), '工具字节改变')
        result['passed'] = True
    finally:
        result['tools'] = tools
        write_json(out/'result.json', result)


if __name__ == '__main__':
    main()
