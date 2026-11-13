"""既有 managed PE 直接进入 SDK 的隔离探针；不是公开 CLI 或有效产品 dist。"""
import json
import os
from pathlib import Path
import re
import shutil
import sys
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent/'standalone_app/benchmark'))
from measure import capture, require, save, sha, inventory, positive, elf


def literal(path):
    text = str(path)
    require(not any(ord(c) < 32 for c in text), '控制字符路径')
    return ''.join('%'+format(ord(c), '02X') if c in "%$@;'()*?" else c for c in text)


def main():
    require(os.environ.get('GITHUB_ACTIONS') == 'true', '只用于 hosted 探针')
    base = Path(os.environ['RUNNER_TEMP'])/'deployment-binaryornot'
    baseline = base/'build-profile'
    result = json.loads((baseline/'result.json').read_text())
    require(result['passed'] and 'first' in result['builds'], '需要本轮默认首次构建')
    attempt = Path(result['builds']['first']['attempt'])
    require(attempt.is_dir() and attempt.parent == baseline/'build-default/scanner.build', 'attempt来源不符')
    managed = attempt/'scanner.dll'
    old_host = attempt/'ryupy-aot-0/host/Host.csproj'
    require(managed.is_file() and old_host.is_file(), '本轮输入不存在')
    log = (baseline/'first-build.stderr').read_text()
    require(str(managed) in log, '首次构建日志未指向managed输入')
    dotnet = Path(os.environ['DOTNET']).resolve()
    project = base/'project'
    out = base/'direct-input-probe'; out.mkdir(exist_ok=False)
    source = inventory(project)
    frozen_paths = [managed, old_host, attempt/'RyuPy.Runtime.dll', dotnet,
                    base/'tools/ryupy', base/'tools/RyuPy.Runtime.dll', Path(os.environ['PYTHON'])]
    frozen = {str(p):sha(p) for p in frozen_paths}
    save(out/'source-before.json', source); save(out/'tools-before.json', frozen)
    report = {'passed': False, 'scope': '内部SDK探针；替换native后的目录不是manifest验证过的产品dist；不测缓存', 'attempts': []}
    env = dict(os.environ, DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1',
               MSBUILDDISABLENODEREUSE='1', UseSharedCompilation='false')
    try:
        for index in range(2):
            work = out/str(index); work.mkdir()
            host = work/'host'; host.mkdir()
            publish = work/'publish'
            # 第二轮保留安全SDK名称；输入路径刻意含MSBuild/XML特殊字符。
            sdk_name = managed.stem if index == 0 else 'RyuPyAotHost_direct_probe'
            supplied = managed
            if index == 1:
                supplied = work/'输入 $();% &.dll'
                shutil.copyfile(managed, supplied)
            tree = ET.parse(old_host); xml = tree.getroot()
            for group in xml.findall('ItemGroup'):
                for item in list(group):
                    if item.tag == 'Compile' or (item.tag == 'Reference' and item.get('Include') == managed.stem):
                        group.remove(item)
            for item in xml.findall('.//WriteLinesToFile'):
                old = item.get('File', '')
                require(old.endswith(('sdk.version', 'sdk.optimization')), '未知SDK记录路径')
                item.set('File', literal(host/Path(old).name))
            properties = {'AssemblyName': sdk_name, 'SkipCompilerExecution': 'true',
                          'ProduceReferenceAssembly': 'false', 'GenerateAssemblyInfo': 'false',
                          'GenerateTargetFrameworkAttribute': 'false', 'UseAppHost': 'false'}
            for node in xml.findall('.//AssemblyName'): node.text = sdk_name
            group = ET.SubElement(xml, 'PropertyGroup')
            for key,value in properties.items(): ET.SubElement(group,key).text=value
            target = ET.SubElement(xml,'Target',Name='RyuPySupplyExistingManagedInput',BeforeTargets='CoreCompile')
            ET.SubElement(target,'Copy',SourceFiles=literal(supplied),DestinationFiles='@(IntermediateAssembly)')
            path = host/'Host.csproj'; tree.write(path,encoding='unicode')
            for key in ['TreatWarningsAsErrors','IlcTreatWarningsAsErrors','ILLinkTreatWarningsAsErrors']:
                require(xml.find('.//'+key).text == 'true', 'warning合同改变')
            rec = capture(work/'publish', [dotnet,'publish',path,'-c','Release','-r','linux-x64',
                '--self-contained','true','-o',publish,'-p:ImportDirectoryBuildProps=false',
                '-p:ImportDirectoryBuildTargets=false','-clp:PerformanceSummary'], host, env, 1200)
            require(rec['exit']==0 and rec['failure'] is None and rec['raw_streams_complete'], 'SDK失败；保留原件')
            responses = list(host.rglob('*.ilc.rsp')); require(len(responses)==1, 'ILC响应不唯一')
            flags = responses[0].read_text(encoding='utf-8-sig').splitlines()
            inputs = [s.strip('"') for s in flags if s and not s.startswith('-')]
            require(len(inputs)==1, 'ILC主输入不唯一')
            entry = Path(inputs[0]); entry = entry if entry.is_absolute() else host/entry
            require(entry.resolve().is_relative_to(host.resolve()) and sha(entry)==sha(managed), 'ILC主输入不是原PE字节')
            native = publish/sdk_name; elf(native)
            # 仅运行副本，刻意不把旧manifest作为新native的证明。
            dist = work/'unvalidated-dist'; shutil.copytree(baseline/'first-dist',dist)
            shutil.copyfile(native,dist/'scanner'); (dist/'scanner').chmod(0o755)
            hidden = base/'project-hidden-direct-probe'; require(not hidden.exists(), '隔离目录冲突')
            project.rename(hidden)
            try:
                for load in ['startup','mixed64']:
                    argv = json.loads((baseline/(load+'-cp.argv.json')).read_text())['argv']
                    # capture保存的CP argv前三项为python、-B、-S，随后scanner.py。
                    require(isinstance(argv,list) and argv[1:3]==['-B','-S'], 'CP命令格式改变')
                    args = argv[4:]
                    expected = (baseline/(load+'-cp.stdout')).read_bytes()
                    require((baseline/'samples'/f'{load}-00-first.stdout').read_bytes()==expected, '基线与CP不符')
                    prefix = work/load
                    run = capture(prefix,[dist/'scanner',*args],baseline/'unrelated-cwd',
                                  {'PATH':'/usr/bin:/bin','LC_ALL':'C.UTF-8','TZ':'UTC'},120)
                    positive(run,prefix,expected)
            finally:
                hidden.rename(project)
            timing = (work/'publish.stdout').read_text()
            report['attempts'].append({'wall_ms':rec['wall_ms'],'native_sha256':sha(native),
                'sdk_assembly_name':sdk_name,'supplied_path':str(supplied),
                'managed_sha256':sha(entry),'ilc_response':str(responses[0]),
                'csc_task_summary':re.findall(r'^.*\bCsc\b.*$',timing,re.M),
                'compiler_scope':'SkipCompilerExecution=true；Csc task可能仍运行，但不应调用编译器；主输入逐字节验证'})
        report['passed'] = True
    finally:
        after = inventory(project); tools_after = {p:sha(Path(p)) for p in frozen}
        save(out/'source-after.json',after); save(out/'tools-after.json',tools_after)
        report['inputs_unchanged'] = after==source and tools_after==frozen
        report['passed'] = report['passed'] and report['inputs_unchanged']
        save(out/'result.json',report)
        require(report['inputs_unchanged'], '探针改变输入')


if __name__ == '__main__':
    main()
