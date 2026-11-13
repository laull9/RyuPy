"""依赖门的假成功/错误阶段/漏诊断负测，不启动真实编译或SDK。"""
import copy
import json
from pathlib import Path
import tempfile
import unittest
from check_dependencies import CASES, INVALID_SOURCE, mutate, verify_rejection
from prepare import metadata_request
from validate_artifact import validate_dependency_report


class DependencyGateTests(unittest.TestCase):
    def test_request_is_explicit_and_not_global(self):
        self.assertEqual(metadata_request()['dependency_check'],
                         {'mode':'unconditional_closure','requirements':['binaryornot==0.4.4']})
        self.assertEqual(list(CASES), ['missing','version','marker','extras','url','base-section','default-off'])

    def test_rejection_rejects_fake_success_timeout_sdk_and_artifacts(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder); prefix=root/'run'; output=root/'out'; marker=root/'sdk'
            prefix.with_suffix('.stdout').write_bytes(b'')
            prefix.with_suffix('.stderr').write_text('error: 依赖预检: 缺少分发：binaryornot -> chardet>=3.0.2')
            good={'exit':1,'failure':None,'raw_streams_complete':True}
            verify_rejection('missing',good,prefix,output,marker)
            for bad in [dict(good,exit=0),dict(good,exit=None),dict(good,failure='timeout'),dict(good,raw_streams_complete=False)]:
                with self.assertRaises(ValueError):verify_rejection('missing',bad,prefix,output,marker)
            marker.write_text('invoked')
            with self.assertRaises(ValueError):verify_rejection('missing',good,prefix,output,marker)
            marker.unlink();output.mkdir();(output/'scanner.dll').write_bytes(b'not-valid')
            with self.assertRaises(ValueError):verify_rejection('missing',good,prefix,output,marker)
            (output/'scanner.dll').unlink();(output/'scanner.dist').mkdir()
            with self.assertRaises(ValueError):verify_rejection('missing',good,prefix,output,marker)

    def test_error_must_name_parent_requirement_and_correct_stage(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);prefix=root/'run';output=root/'out';marker=root/'sdk'
            prefix.with_suffix('.stdout').write_bytes(b'');good={'exit':1,'failure':None,'raw_streams_complete':True}
            for text in ['syntax error E0001', '依赖预检: chardet>=3.0.2', '依赖预检: binaryornot chardet',
                         '依赖预检: binaryornot chardet>=3.0.2; invalid syntax']:
                prefix.with_suffix('.stderr').write_text(text)
                with self.assertRaises(ValueError):verify_rejection('missing',good,prefix,output,marker)
            prefix.with_suffix('.stderr').write_text('error[E0001]: invalid syntax')
            verify_rejection('default-off',good,prefix,output,marker)
            with self.assertRaises(ValueError):verify_rejection('missing',good,prefix,output,marker)

    def test_positive_report_is_bound_to_exact_inputs_and_edges(self):
        good={'schema_version':1,'mode':'unconditional_closure','requirements':['binaryornot==0.4.4'],
              'validated_names':['binaryornot','chardet'],'request_sha256':'request','payload_sha256':'payload',
              'checks':[{'from':'<root>','requirement':'binaryornot==0.4.4','name':'binaryornot','version':'0.4.4',
                         'root':'vendor','path':'binaryornot.egg-info'},
                        {'from':'binaryornot','requirement':'chardet>=3.0.2','name':'chardet','version':'5.2.0',
                         'root':'vendor','path':'chardet.egg-info'}]}
        validate_dependency_report(good,'request','payload')
        for key,value in [('request_sha256','wrong'),('payload_sha256','wrong'),('validated_names',['binaryornot']),
                          ('checks',good['checks'][:1]),('requirements',[])]:
            with self.assertRaises(ValueError):validate_dependency_report(dict(good,**{key:value}),'request','payload')
        for key,value in [('from','<root>'),('requirement','chardet'),('version','2.0'),('root','other'),('path','wrong')]:
            bad=copy.deepcopy(good);bad['checks'][1][key]=value
            with self.assertRaises(ValueError):validate_dependency_report(bad,'request','payload')

    def test_base_marker_section_is_not_an_unselected_extra(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);egg=root/'vendor/binaryornot.egg-info';egg.mkdir(parents=True)
            mutate(root,'base-section')
            self.assertEqual((egg/'requires.txt').read_text(),'[:python_version >= "3"]\nchardet>=3.0.2\n')
            self.assertEqual((root/'scanner.py').read_text(),INVALID_SOURCE)


if __name__ == '__main__':unittest.main()
