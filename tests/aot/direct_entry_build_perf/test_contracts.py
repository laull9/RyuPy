"""对照编排与审核器负例；不把合成SDK文本计作真实Native证据。"""
import importlib.util
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
import hashlib
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from contracts import schedule, timings, summaries, verify_project


class ContractsTests(unittest.TestCase):
    def test_schedule_is_frozen_balanced_and_disjoint(self):
        rows=schedule();self.assertEqual(len(rows),36)
        self.assertEqual(len({r['id'] for r in rows}),36)
        self.assertEqual({len(r['id']) for r in rows},{3})
        self.assertTrue(all(r['warmup'] for r in rows[:4]))
        for app in ('pyfiglet','binaryornot'):
            self.assertEqual([r['arm'] for r in rows[:4] if r['app']==app],['direct','thin'])
            for block in range(4):
                arms=[r['arm'] for r in rows if r['app']==app and r['block']==block]
                self.assertEqual(arms,['direct','thin','thin','direct'] if block%2==0 else ['thin','direct','direct','thin'])

    def test_task_and_target_timing_stay_separate(self):
        text='Target Performance Summary:\n  900 ms  IlcCompile  1 calls\n  80 ms LinkNative 1 calls\nTask Performance Summary:\n  2 ms Csc 1 calls\n  700 ms Exec 2 calls\n'
        rows=timings(text);self.assertEqual(len(rows),4)
        self.assertEqual(rows[0]['section'],'target');self.assertEqual(rows[2]['section'],'task')
        self.assertEqual(rows[2]['milliseconds'],2)
        self.assertEqual(rows[2]['raw'],'  2 ms Csc 1 calls')
        for broken in [text.replace('Csc','Other'),text+'  1 ms Csc 1 calls\n',text.replace('IlcCompile','Different')]:
            with self.assertRaises(AssertionError):timings(broken)

    def test_summary_excludes_warmup_and_requires_every_sample(self):
        rows=[dict(r,wall_ms=99999 if r['warmup'] else 90 if r['arm']=='direct' else 100) for r in schedule()]
        result=summaries(rows)
        self.assertEqual(result['pyfiglet']['arms']['direct']['median_ms'],90)
        self.assertEqual(result['binaryornot']['paired_blocks'][0]['direct_minus_thin_ms'],-10)
        with self.assertRaises(AssertionError):summaries(rows[:-1])

    def test_direct_project_rejects_wrong_ilc_bytes_and_copy(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);host=root/'host';host.mkdir();managed=root/'input.dll';managed.write_bytes(b'managed')
            entry=host/'main.dll';entry.write_bytes(b'managed');(host/'main.ilc.rsp').write_text('main.dll\n-r:runtime.dll\n')
            props={'PublishAot':'true','TreatWarningsAsErrors':'true','IlcTreatWarningsAsErrors':'true','ILLinkTreatWarningsAsErrors':'true',
                   'SkipCompilerExecution':'true','ProduceReferenceAssembly':'false','GenerateAssemblyInfo':'false','GenerateTargetFrameworkAttribute':'false','UseAppHost':'false'}
            text='<Project><PropertyGroup>'+''.join(f'<{k}>{v}</{k}>' for k,v in props.items())+'</PropertyGroup><ItemGroup><Reference Include="RyuPy.Runtime"/></ItemGroup><Target Name="Supply" BeforeTargets="CoreCompile"><Copy SourceFiles="'+str(managed)+'" DestinationFiles="@(IntermediateAssembly)"/></Target></Project>'
            project=host/'Host.csproj';project.write_text(text)
            digest=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
            layout=SimpleNamespace(host=host,managed=managed)
            verify_project(layout,'direct',digest)
            entry.write_bytes(b'wrong')
            with self.assertRaises(AssertionError):verify_project(layout,'direct',digest)
            entry.write_bytes(b'managed');project.write_text(text.replace('@(IntermediateAssembly)','wrong.dll'))
            with self.assertRaises(AssertionError):verify_project(layout,'direct',digest)

    def test_thin_control_requires_actual_distinct_host(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);host=root/'host';host.mkdir();managed=root/'input.dll';managed.write_bytes(b'managed')
            entry=host/'main.dll';entry.write_bytes(b'thin-host')
            (host/'main.ilc.rsp').write_text('main.dll\n-r:runtime.dll\n')
            props=''.join(f'<{k}>true</{k}>' for k in ('PublishAot','TreatWarningsAsErrors','IlcTreatWarningsAsErrors','ILLinkTreatWarningsAsErrors'))
            (host/'Host.csproj').write_text('<Project><PropertyGroup>'+props+'</PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Reference Include="input"/><Reference Include="RyuPy.Runtime"/></ItemGroup></Project>')
            (host/'Program.cs').write_bytes(b'internal static class Program { private static int Main(string[] args) => global::RyuPy.Generated.Module.Main(args); }\n')
            digest=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
            layout=SimpleNamespace(host=host,managed=managed)
            verify_project(layout,'thin',digest)
            entry.write_bytes(b'managed')
            with self.assertRaises(AssertionError):verify_project(layout,'thin',digest)


if __name__=='__main__':unittest.main()
