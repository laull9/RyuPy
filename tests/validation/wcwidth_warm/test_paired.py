"""成对窗口纯反例：身份、次序、漂移与包边界；不启动任何产品。"""
import copy
import io
import json
import os
from pathlib import Path
import tarfile
import tempfile
import unittest
from unittest.mock import patch
import paired_inputs as p
import paired_runner as r


class PairedTests(unittest.TestCase):
    def rows(self, ratio=.9):
        return [[{'arm': arm, 'values': [1.0 if arm == 'A' else ratio] * 12} for arm in order]
                for order in r.ORDER]

    def test_gain(self):
        got = r.analyze(self.rows())
        self.assertAlmostEqual(got['geometric_ratio'], .9)
        self.assertTrue(got['observed_window_reduction_at_least_5_percent'])
        self.assertIsNone(got['confidence_lower_bound'])

    def test_no_gain_not_failure(self):
        self.assertFalse(r.analyze(self.rows(1.0))['observed_window_reduction_at_least_5_percent'])
        self.assertFalse(r.analyze(self.rows(.97))['observed_window_reduction_at_least_5_percent'])

    def test_incomplete(self):
        with self.assertRaises(RuntimeError):
            r.analyze(self.rows()[:-1])

    def test_order(self):
        rows = self.rows(); rows[0].reverse()
        rows[0][0]['arm'] = 'B'
        with self.assertRaises(RuntimeError):
            r.analyze(rows)

    def test_bad_values(self):
        for value in (float('nan'), float('inf'), -.1, .099):
            rows = self.rows(); rows[0][0]['values'][0] = value
            with self.assertRaises(RuntimeError):
                r.analyze(rows)

    def test_within_process_drift(self):
        rows = self.rows(); rows[0][0]['values'][6:] = [1.2] * 6
        with self.assertRaises(RuntimeError):
            r.analyze(rows)

    def test_position(self):
        rows = self.rows(); rows[0][3]['values'] = [1.2] * 12
        with self.assertRaises(RuntimeError):
            r.analyze(rows)

    def test_late_drift(self):
        rows = self.rows()
        for block in rows[3:]:
            for row in block:
                row['values'] = [v * 1.2 for v in row['values']]
        with self.assertRaises(RuntimeError):
            r.analyze(rows)

    def test_reverse_order_effect(self):
        rows = self.rows()
        for i in (1, 3, 5):
            for row in rows[i]:
                if row['arm'] == 'B':
                    row['values'] = [1.] * 12
        with self.assertRaises(RuntimeError):
            r.analyze(rows)

    def test_one_block_wrong_direction(self):
        rows = self.rows(.96)
        for row in rows[0]:
            if row['arm'] == 'B':
                row['values'] = [1.001] * 12
        self.assertFalse(r.analyze(rows)['observed_window_reduction_at_least_5_percent'])

    def project(self):
        props = dict(OutputType='Exe', TargetFramework='net10.0', AssemblyName='RyuPyAotHost_1',
                     PublishAot='true', Optimize='true', EnableDefaultCompileItems='false',
                     TreatWarningsAsErrors='true', IlcTreatWarningsAsErrors='true',
                     ILLinkTreatWarningsAsErrors='true', TrimmerSingleWarn='false')
        return '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>' + ''.join(
            '<' + k + '>' + v + '</' + k + '>' for k, v in props.items()) + '''</PropertyGroup>
<ItemGroup><Compile Include="Program.cs"/><Reference Include="kernel"><HintPath>/old/kernel.dll</HintPath></Reference>
<Reference Include="RyuPy.Runtime"><HintPath>/old/RyuPy.Runtime.dll</HintPath></Reference></ItemGroup>
<Target Name="RyuPyRecordSdkVersion" BeforeTargets="Publish"><WriteLinesToFile File="/old/sdk.version" Lines="$(NETCoreSdkVersion)" Overwrite="true" Encoding="UTF-8"/></Target></Project>'''

    def test_project_relocation(self):
        text, name = p.project(self.project())
        self.assertNotIn('/old/', text)
        self.assertIn('../managed/kernel.dll', text)
        self.assertEqual(name, 'RyuPyAotHost_1')

    def test_strict_publish(self):
        for field in ('PublishAot', 'TreatWarningsAsErrors', 'IlcTreatWarningsAsErrors', 'ILLinkTreatWarningsAsErrors'):
            with self.assertRaises(RuntimeError):
                p.project(self.project().replace('>' + 'true</' + field, '>false</' + field))

    def test_reference_domain(self):
        with self.assertRaises(RuntimeError):
            p.project(self.project().replace('Include="kernel"', 'Include="other"'))

    def test_capsule_identity(self):
        with tempfile.TemporaryDirectory() as tmp, patch.dict(os.environ, GITHUB_SHA='head', GITHUB_RUN_ID='123', GITHUB_RUN_ATTEMPT='1'):
            root = Path(tmp); source = root / 'file'; source.write_bytes(b'abc')
            p.pack(root, 'warm', {'managed/file': source}, {})
            p.unpack(root / 'paired-warm.tar', root / 'restore', 'warm')
            with patch.dict(os.environ, GITHUB_RUN_ATTEMPT='2'), self.assertRaises(RuntimeError):
                p.unpack(root / 'paired-warm.tar', root / 'wrong', 'warm')

    def test_capsule_unsafe(self):
        for name in ('../escape', '/absolute'):
            with tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp); archive = root / 'bad.tar'
                with tarfile.open(archive, 'w') as tar:
                    info = tarfile.TarInfo(name); info.size = 1; tar.addfile(info, io.BytesIO(b'x'))
                with self.assertRaises(RuntimeError):
                    p.unpack(archive, root / 'restore', 'warm')

    def test_capsule_extra(self):
        with tempfile.TemporaryDirectory() as tmp, patch.dict(os.environ, GITHUB_SHA='head', GITHUB_RUN_ID='123', GITHUB_RUN_ATTEMPT='1'):
            root = Path(tmp); source = root / 'file'; source.write_bytes(b'abc')
            p.pack(root, 'warm', {'managed/file': source}, {})
            with tarfile.open(root / 'paired-warm.tar', 'a') as tar:
                info = tarfile.TarInfo('extra'); info.size = 1; tar.addfile(info, io.BytesIO(b'x'))
            with self.assertRaises(RuntimeError):
                p.unpack(root / 'paired-warm.tar', root / 'restore', 'warm')

    def test_ilc_binding(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); expected = {}
            for name in ('Host.dll', 'kernel.dll', 'RyuPy.Runtime.dll'):
                path = root / name; path.write_bytes(name.encode()); expected[name] = path
            rsp = root / 'Host.ilc.rsp'
            original = 'Host.dll\n-r:kernel.dll\n-r:RyuPy.Runtime.dll\n-o:result.o\n'
            rsp.write_text(original)
            self.assertEqual(set(p.ilc_binding(root, expected)['inputs']), set(expected))
            for malformed in (original.replace('-r:kernel.dll\n', ''), original + '-r:kernel.dll\n',
                              original.replace('Host.dll\n', '-r:Host.dll\n'), original.replace('-r:kernel.dll', 'kernel.dll')):
                rsp.write_text(malformed)
                with self.assertRaises(RuntimeError):
                    p.ilc_binding(root, expected)
            rsp.write_text(original)
            other = root / 'other.dll'; other.write_bytes(b'wrong')
            with self.assertRaises(RuntimeError):
                p.ilc_binding(root, dict(expected, **{'kernel.dll': other}))
            (root / 'duplicate.ilc.rsp').write_text(original)
            with self.assertRaises(RuntimeError):
                p.ilc_binding(root, expected)

    def pe(self):
        # 自建最小metadata/debug布局，只测字段门，不冒真实Host或IL执行。
        import struct
        b = bytearray(4096)
        def n(o, v, size=4):
            b[o:o+size] = v.to_bytes(size, 'little')
        n(60, 128); b[128:132] = b'PE\0\0'; n(134, 1, 2); n(148, 224, 2)
        opt = 152; n(opt, 0x10b, 2); sec = opt + 224
        n(sec+12, 0x2000); n(sec+16, 3584); n(sec+20, 512)
        def rva(o):
            return o - 512 + 0x2000
        dirs = opt+96; n(dirs+14*8, rva(512)); n(520, rva(768))
        b[768:772] = b'BSJB'; n(780, 4); b[784:788] = b'v4\0\0'
        n(790, 3, 2); pos = 792
        for name, start, size in [('#~', 1024, 50), ('#GUID', 1152, 16), ('#Strings', 1200, 4)]:
            n(pos,start-768); n(pos+4,size); raw=name.encode()+b'\0';b[pos+8:pos+8+len(raw)]=raw
            pos = (pos+8+len(raw)+3)&~3
        n(1032,1,8);n(1048,1); n(1056,1,2) # 唯一Module row的Mvid=1
        n(dirs+6*8,rva(1536));n(dirs+6*8+4,84)
        for i,(kind,raw,size) in enumerate([(2,1664,30),(19,1728,39),(16,0,0)]):
            o=1536+i*28;n(o+12,kind);n(o+16,size);n(o+24,raw)
        b[1664:1668]=b'RSDS';n(1684,1);b[1688:1694]=b'x.pdb\0';b[1728:1735]=b'SHA256\0'
        return b

    def test_host_exact_identity_fields(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);a=root/'a';b=root/'b';original=self.pe();a.write_bytes(original)
            slots=p.host_identity_slots(original)
            for _,offset,size in slots:
                changed=bytearray(original);changed[offset]^=1;b.write_bytes(changed)
                self.assertFalse(p.compare_hosts(a,b)['byte_identical'])
            for offset in (532, 1300, 1684, 1688, 1548): # entry token、IL区、age、path、debug kind
                changed=bytearray(original);changed[offset]^=1;b.write_bytes(changed)
                with self.assertRaises((RuntimeError, ValueError)):
                    p.compare_hosts(a,b)

    def test_host_extra_guid(self):
        raw=self.pe();raw[808:812]=(32).to_bytes(4,'little') # #GUID流size
        with self.assertRaises(RuntimeError):
            p.host_identity_slots(raw)

    def test_host_encid(self):
        raw=self.pe();raw[1058:1060]=(1).to_bytes(2,'little')
        with self.assertRaises(RuntimeError):
            p.host_identity_slots(raw)


if __name__ == '__main__':
    unittest.main()
