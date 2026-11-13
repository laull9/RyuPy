"""平台资源边界门不能把零测试或忽略算成功。"""
import unittest
from resource_output_contract import check_resource_output_tests, expected_tests

class ResourceOutputContractTests(unittest.TestCase):
    def test_exact_test_set_and_count_required(self):
        for rid in ('win-x64','osx-arm64'):
            for section in ('boundaries','transaction'):
                names=expected_tests(rid,section)
                rows='\n'.join('test '+name+' ... ok' for name in sorted(names))
                good=rows+f'\ntest result: ok. {len(names)} passed; 0 failed; 0 ignored; 0 measured; 200 filtered out;'
                self.assertEqual(check_resource_output_tests(good,rid,section),len(names))
                for bad in ('test result: ok. 0 passed; 0 failed; 0 ignored;',
                            good.replace(' ... ok',' ... ignored',1),
                            good.replace(next(iter(names)),'wrong'),
                            good+'\n'+rows,good.replace('0 ignored','1 ignored')):
                    with self.assertRaises(ValueError):check_resource_output_tests(bad,rid,section)

if __name__=='__main__':unittest.main()
