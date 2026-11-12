"""同环境CP源入口控制；这些单测不充当异平台实际执行证据。"""
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from platform_project import source_command, check_missing_appdata
from platform_support import clean_environment


class SourceControlTests(unittest.TestCase):
    def test_explicit_source_wrapper_preserves_entry_and_argv(self):
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder);package=root/'package';package.mkdir()
            (package/'fixture.py').write_text('value = 42\n',encoding='utf-8')
            entry=root/'entry.py'
            entry.write_text('import fixture,json,sys\nprint(json.dumps([__name__, __spec__, sys.argv, fixture.value]))\n',encoding='utf-8')
            private=root/'private';private.mkdir();clean=clean_environment(private)
            before=dict(clean);argv=['arg with space','中文','--flag=one']
            actual=subprocess.run(source_command(sys.executable,package,entry,argv),cwd=root,env=clean,capture_output=True)
            expected=subprocess.run([sys.executable,'-B',str(entry)]+argv,cwd=root,
                env=dict(clean,PYTHONPATH=str(package)),capture_output=True)
            self.assertEqual(actual.returncode,0,actual.stderr)
            self.assertEqual((actual.stdout,actual.stderr),(expected.stdout,expected.stderr))
            self.assertEqual(clean,before);self.assertNotIn('PYTHONPATH',clean)

    def test_missing_appdata_requires_specific_failure_not_any_nonzero(self):
        check_missing_appdata(subprocess.CompletedProcess([],1,b'',b"KeyError: 'APPDATA'\r\n"))
        for code,out,err in [(0,b'',b"KeyError: 'APPDATA'"),(1,b'partial',b"KeyError: 'APPDATA'"),(1,b'',b'other failure')]:
            with self.assertRaises(ValueError):
                check_missing_appdata(subprocess.CompletedProcess([],code,out,err))


if __name__=='__main__':unittest.main()
