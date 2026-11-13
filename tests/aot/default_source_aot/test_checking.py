"""命令/原流门纯测；不执行构建或产品。"""
import unittest
from checking import build_argv, positive, boundary_expected, named_test_output

class CheckingTests(unittest.TestCase):
    def test_named_discovery_never_accepts_zero_or_wrong_identity(self):
        name = 'default_aot::tests::default_aot_real_graph_emit'
        ok = 'test ' + name + ' ... ok'
        named_test_output([ok], name)
        for lines in [[], ['running 0 tests'], [ok, ok],
                      ['test prefix::' + name + ' ... ok'],
                      ['test ' + name + ' ... ignored'],
                      ['test default_aot::tests::oracle::default_aot_real_graph_emit ... ok']]:
            with self.assertRaises(ValueError): named_test_output(lines, name)

    def test_default_argv(self):
        self.assertEqual(build_argv('cli','entry','out','root',True),['cli','build','entry','--out-dir','out','-I','root','--aot'])
        self.assertNotIn('--aot',build_argv('cli','entry','out','root',False))
    def test_actual_flow(self):
        positive({'exit':0,'failure':None,'raw_streams_complete':True},b'yes',b'',b'yes')
        for code in [1,-11,139,None]:
            with self.assertRaises(ValueError):positive({'exit':code,'failure':None,'raw_streams_complete':True},b'yes',b'',b'yes')
        for bad in [{'exit':0,'failure':'timeout','raw_streams_complete':True},{'exit':0,'failure':None,'raw_streams_complete':False}]:
            with self.assertRaises(ValueError):positive(bad,b'yes',b'',b'yes')
        with self.assertRaises(ValueError):positive({'exit':0,'failure':None,'raw_streams_complete':True},b'yes',b'warn',b'yes')
    def test_boundary_requires_exact_cp(self):
        with self.assertRaises(ValueError):boundary_expected(b'NI')

if __name__=='__main__':unittest.main()
