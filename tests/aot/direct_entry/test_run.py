"""入口验收器轻量自测；不将CP或伪SDK文件计为Native成功。"""
import os
from pathlib import Path
import subprocess
import sys
import tempfile
from types import SimpleNamespace
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parent))
import run as harness


class Contracts(unittest.TestCase):
    def test_pinned_fixture_oracle(self):
        for case, argv in harness.CASES.items():
            with self.subTest(case=case):
                result = subprocess.run([sys.executable, '-B', '-S', str(harness.HERE/'entry_probe.py'), *argv],
                                        capture_output=True, env=dict(os.environ, PYTHONIOENCODING='utf-8'))
                if case == 'error':
                    harness.error_signature(result.stderr, oracle=True)
                    self.assertEqual(result.returncode, 1)
                    self.assertEqual(result.stdout, b'|callback')
                else:
                    harness.compare(case, result, result)

    def test_error_does_not_ignore_extra_lines_or_changed_frames(self):
        raw = (b'Traceback (most recent call last):\n'
               b'  File "entry_probe.py", line 24, in <module>\n'
               b'  File "entry_probe.py", line 11, in fail\n'
               b'ValueError: direct-entry-error\n')
        harness.error_signature(raw, oracle=False)
        for broken in [b'extra\n'+raw, raw.replace(b'line 24', b'line 25'),
                       raw.replace(b'ValueError:', b'TypeError:'), raw+b'extra\n',
                       raw.replace(b'  File ', b'    hidden\n  File ', 1)]:
            with self.assertRaises(AssertionError):
                harness.error_signature(broken, oracle=False)

    def test_compare_keeps_exit_stdout_and_stderr_strict(self):
        good = subprocess.CompletedProcess([], 7, b'|callback', b'')
        for bad in [subprocess.CompletedProcess([], 0, good.stdout, b''),
                    subprocess.CompletedProcess([], 7, b'callback', b''),
                    subprocess.CompletedProcess([], 7, good.stdout, b'unexpected')]:
            with self.assertRaises(AssertionError):
                harness.compare('exit7', good, bad)

    def test_windows_requires_legacy_host(self):
        with tempfile.TemporaryDirectory() as directory:
            host = Path(directory)
            properties = ''.join('<'+key+'>true</'+key+'>' for key in [
                'PublishAot', 'TreatWarningsAsErrors', 'IlcTreatWarningsAsErrors', 'ILLinkTreatWarningsAsErrors'])
            properties += '<TrimmerSingleWarn>false</TrimmerSingleWarn>'
            project = '<Project><PropertyGroup>'+properties+'</PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Reference Include="entry_probe"/><Reference Include="RyuPy.Runtime"/></ItemGroup></Project>'
            (host/'Host.csproj').write_text(project)
            (host/'Program.cs').write_bytes(b'internal static class Program { private static int Main(string[] args) => global::RyuPy.Generated.Module.Main(args); }\n')
            layout = SimpleNamespace(host=host)
            self.assertEqual(harness.verify_entry(layout, 'win-x64')['mode'], 'thin-host')
            (host/'Host.csproj').write_text(project.replace('</PropertyGroup>', '<SkipCompilerExecution>true</SkipCompilerExecution></PropertyGroup>'))
            with self.assertRaises(AssertionError): harness.verify_entry(layout, 'win-x64')

    def test_real_input_identity_and_unique_response_are_required(self):
        # 合成SDK目录只测试审核器拒绝路径，不声称它证明真实编译。
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); host = root/'host'; host.mkdir()
            managed = root/'entry_probe.dll'; managed.write_bytes(b'current-managed')
            entry = host/'main.dll'; entry.write_bytes(managed.read_bytes())
            properties = ''.join('<'+key+'>true</'+key+'>' for key in [
                'PublishAot', 'TreatWarningsAsErrors', 'IlcTreatWarningsAsErrors',
                'ILLinkTreatWarningsAsErrors', 'SkipCompilerExecution'])
            properties += '<TrimmerSingleWarn>false</TrimmerSingleWarn>'
            properties += ''.join('<'+key+'>false</'+key+'>' for key in [
                'ProduceReferenceAssembly', 'GenerateAssemblyInfo', 'GenerateTargetFrameworkAttribute', 'UseAppHost'])
            (host/'Host.csproj').write_text('<Project><PropertyGroup>'+properties+
                '</PropertyGroup><ItemGroup><Reference Include="RyuPy.Runtime"/></ItemGroup>'+
                '<Target Name="Supply" BeforeTargets="CoreCompile"><Copy SourceFiles="'+str(managed.resolve())+'" DestinationFiles="@(IntermediateAssembly)"/></Target></Project>')
            response = host/'main.ilc.rsp'; response.write_text('"main.dll"\n-r:runtime.dll\n')
            layout = SimpleNamespace(host=host, managed=managed)
            self.assertTrue(harness.verify_entry(layout, 'linux-x64')['actual_ilc_input_checked'])
            # 即便ILC字节正确，缺属性/Copy或Copy指向旧输入也不能补验通过。
            from xml.etree import ElementTree as ET
            original = (host/'Host.csproj').read_text()
            for mutation in ('missing-property', 'missing-copy', 'old-source', 'wrong-destination', 'condition'):
                project = ET.fromstring(original)
                if mutation == 'missing-property':
                    props = project.find('./PropertyGroup')
                    props.remove(props.find('GenerateAssemblyInfo'))
                elif mutation == 'missing-copy':
                    project.find('./Target').remove(project.find('./Target/Copy'))
                elif mutation == 'old-source':
                    project.find('./Target/Copy').set('SourceFiles', '/old/input.dll')
                elif mutation == 'wrong-destination':
                    project.find('./Target/Copy').set('DestinationFiles', 'old.dll')
                else:
                    project.find('./Target').set('Condition', 'false')
                ET.ElementTree(project).write(host/'Host.csproj', encoding='unicode')
                with self.assertRaises(AssertionError): harness.verify_entry(layout, 'linux-x64')
            (host/'Host.csproj').write_text(original)
            entry.write_bytes(b'old-managed')
            with self.assertRaises(AssertionError): harness.verify_entry(layout, 'linux-x64')
            entry.write_bytes(managed.read_bytes())
            response.write_text('main.dll\nother.dll\n')
            with self.assertRaises(AssertionError): harness.verify_entry(layout, 'linux-x64')
            response.write_text('main.dll\n')
            (host/'other.ilc.rsp').write_text('main.dll\n')
            with self.assertRaises(AssertionError): harness.verify_entry(layout, 'linux-x64')


if __name__ == '__main__':
    unittest.main()
