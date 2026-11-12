"""真实receipt接口负测：缺oracle不能通过；只测checker，不冒充Native验收。"""
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import validate_artifact as v


class AppReceiptTests(unittest.TestCase):
    def test_missing_independent_oracle_never_signs(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder); tool=root/'tool'; tool.write_bytes(b'fixed')
            out=root/'receipt'
            with self.assertRaisesRegex(ValueError,'缺独立发射前oracle'):
                v.validate_app_artifact(SimpleNamespace(),root,{str(tool):v.sha(tool)},
                    case='cli',python=tool,dotnet=tool,oracle_dir=root/'missing',output=out)
            self.assertFalse((out/'receipt.json').exists())

    def test_tampered_tool_never_signs(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder); tool=root/'tool'; tool.write_bytes(b'changed')
            with self.assertRaisesRegex(ValueError,'冻结工具变化'):
                v.validate_app_artifact(SimpleNamespace(),root,{str(tool):'0'*64},
                    case='cli',python=tool,dotnet=tool,oracle_dir=root,output=root/'out')
            self.assertFalse((root/'out/receipt.json').exists())

    def test_resource_oracle_uses_request_bytes_and_emitted_id(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder); package=root/'package/pkg/fonts'; package.mkdir(parents=True)
            (package/'a.flf').write_bytes(b'real-font')
            (root/'resources.json').write_text(json.dumps({'modules':[{'module':'pkg.fonts','files':['a.flf']}]}))
            oracle={'sources':[{'module_name':'pkg.fonts','module_id':7,'logical_source':'modules/pkg/fonts/__init__.py'}]}
            with patch.object(v,'HERE',root):
                rows=v.expected_resources(root,oracle)
                self.assertEqual(rows,[{'relative_path':'_internal/resources/v1/modules/pkg/fonts/a.flf',
                    'size':9,'sha256':v.sha(package/'a.flf'),'consumer_module_ids':[7]}])
                (package/'a.flf').unlink()
                with self.assertRaisesRegex(ValueError,'资源文件缺失'): v.expected_resources(root,oracle)

    def test_unknown_case_does_not_create_receipt(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder)
            with self.assertRaisesRegex(ValueError,'未知case'):
                v.validate_app_artifact(None,root,{},case='fake',python='',dotnet='',oracle_dir=root,output=root/'out')
            self.assertFalse((root/'out').exists())

if __name__=='__main__': unittest.main()
