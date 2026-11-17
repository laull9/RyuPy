"""公开internal布局的纯静态合同门；不运行CLI，不用普通文件摘要。"""
import json
from pathlib import Path
import tempfile
import unittest
from support import built_product, product_layout, provider_data

class ProductLayoutTests(unittest.TestCase):
    def tree(self, root, name='entry', kind='apphost'):
        data=root/'internal';data.mkdir();(root/name).write_bytes(b'native fixture')
        marker={'schema_version':1,'kind':'ryupy-application-layout','payload_mode':'canonical-v1','entry':name,'entry_kind':kind}
        (data/'.ryupy-layout.json').write_text(json.dumps(marker));return marker

    def test_jit_and_native_use_the_marker_entry_and_one_data_root(self):
        for name,kind in [('entry','apphost'),('internal.bin','apphost'),('internal.exe','native_aot')]:
            with tempfile.TemporaryDirectory(prefix='ryupy-layout-static-') as tmp:
                root=Path(tmp).resolve();self.tree(root,name,kind);layout=product_layout(root)
                self.assertEqual(layout.entry,root/name);self.assertEqual(layout.data,root/'internal')
                self.assertEqual(layout.provider_config,root/'internal/ryupy-cpython.json')

    def test_flat_wrong_case_extra_root_files_and_bad_marker_are_rejected(self):
        for fault in ('flat','case','extra','schema','kind','traversal','entry_kind','bool_schema'):
            with tempfile.TemporaryDirectory(prefix='ryupy-layout-static-') as tmp:
                root=Path(tmp)
                if fault=='flat':(root/'entry.dll').write_bytes(b'old');
                else:
                    marker=self.tree(root)
                    if fault=='case':(root/'internal').rename(root/'Internal')
                    elif fault=='extra':(root/'RyuPy.Runtime.dll').write_bytes(b'old root dependency')
                    else:
                        if fault=='schema':marker['schema_version']=2
                        if fault=='bool_schema':marker['schema_version']=True
                        if fault=='kind':marker['kind']='other'
                        if fault=='traversal':marker['entry']='../entry'
                        if fault=='entry_kind':marker['entry_kind']='managed_dll'
                        (root/'internal/.ryupy-layout.json').write_text(json.dumps(marker))
                with self.assertRaises(AssertionError):product_layout(root)

    def test_generated_record_must_match_marker_and_onefile_is_explicitly_flat(self):
        with tempfile.TemporaryDirectory(prefix='ryupy-layout-static-') as tmp:
            root=Path(tmp).resolve();output=root/'jit';output.mkdir();self.tree(output)
            stderr=('已生成 '+str(output/'entry')+'\n').encode()
            self.assertEqual(built_product(stderr,output,'entry','jit').entry,output/'entry')
            with self.assertRaises(AssertionError):built_product(('已生成 '+str(output/'other')).encode(),output,'entry','jit')
            dist=root/'entry.dist';dist.mkdir();(dist/'cpython-provider.json').write_text('{}')
            self.assertEqual(provider_data(root,'entry','onefile'),dist)
            with self.assertRaises(AssertionError):provider_data(root,'entry','standalone')

if __name__=='__main__':unittest.main()
