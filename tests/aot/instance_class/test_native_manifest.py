"""metadata投影窄适配纯测；真实PE/request/dist另由固定原件审计，不以mock代替。"""
import copy
from pathlib import Path
import sys
import unittest
from unittest.mock import patch
ROOT = Path(__file__).resolve().parents[3]
sys.path[:0] = [str(ROOT/'tests/validation'), str(ROOT/'tests/aot')]
import native_manifest as current
import manifest_options


class ProjectionTests(unittest.TestCase):
    def data(self):
        options = {key: False for key in manifest_options.BOOL_OPTIONS}
        options.update(instance_modules=['html.parser'],module_environment_policy='explicit-only',
                       distribution_metadata={'checked':True})
        return {'compiler': {'options': options}}

    def test_selected_tuple_forwarded_and_original_unchanged(self):
        data=self.data();before=copy.deepcopy(data)
        def validate(projected,rid,sdk,*,expected_instance_modules):
            self.assertEqual((rid,sdk,expected_instance_modules),('linux-x64','10.0.403',('html.parser',)))
            manifest_options.validate_options(projected['compiler']['options'])
        with patch.object(current.legacy,'validate_schema',side_effect=validate):
            current.validate_projected(data,{'checked':True},['html.parser'],'10.0.403')
        self.assertEqual(data,before)

    def test_bad_report_and_unknown_ninth_not_removed(self):
        data=self.data()
        with self.assertRaises(ValueError):current.validate_projected(data,{'checked':False},['html.parser'],'10.0.403')
        data['compiler']['options']['unknown_ninth']=True
        def validate(projected,*args,**kwargs):manifest_options.validate_options(projected['compiler']['options'])
        with patch.object(current.legacy,'validate_schema',side_effect=validate):
            with self.assertRaises(ValueError):current.validate_projected(data,{'checked':True},['html.parser'],'10.0.403')


if __name__=='__main__':unittest.main()
