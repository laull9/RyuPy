"""符号已分离后只恢复采样副本，拒绝篡改/逃逸及不匹配native。"""
import hashlib,json,tempfile,unittest
from pathlib import Path
from types import SimpleNamespace
from profile_symbols import prepare_profile_symbols
from measure import inventory


class SymbolTests(unittest.TestCase):
    def test_copy_is_diagnostic_only_and_bad_identity_rejects(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);sdk=root/'sdk';publish=sdk/'publish';publish.mkdir(parents=True)
            dist=root/'app.dist';dist.mkdir();profile=root/'profile';profile.mkdir()
            (publish/'Host').write_bytes(b'native');(dist/'app').write_bytes(b'native');(profile/'app').write_bytes(b'native')
            (publish/'Host.dbg').write_bytes(b'symbols')
            h=lambda b:hashlib.sha256(b).hexdigest()
            record={'schema_version':1,'platform':'linux-x64','status':'separated','reason':'verified_debuglink_crc_and_build_id',
                'native_relative_path':'sdk/publish/Host','symbols_relative_path':'sdk/publish/Host.dbg',
                'native_sha256':h(b'native'),'symbols_sha256':h(b'symbols')}
            path=root/'debug-symbols.json';path.write_text(json.dumps(record))
            layout=SimpleNamespace(attempt=root,sdk_root=sdk,native=dist/'app')
            product_before=inventory(dist)
            target,_=prepare_profile_symbols(layout,profile)
            profile_before=inventory(profile)
            self.assertEqual(inventory(dist),product_before)
            self.assertEqual(inventory(profile),profile_before)
            self.assertNotEqual(profile_before,product_before)
            self.assertEqual(target.read_bytes(),b'symbols');self.assertFalse((dist/'Host.dbg').exists())
            target.unlink();record['native_sha256']='bad';path.write_text(json.dumps(record))
            with self.assertRaises(ValueError):prepare_profile_symbols(layout,profile)
            self.assertFalse(target.exists());record['native_sha256']=h(b'native')
            record['symbols_relative_path']='../outside.dbg';path.write_text(json.dumps(record))
            with self.assertRaises(ValueError):prepare_profile_symbols(layout,profile)


if __name__=='__main__':unittest.main()
