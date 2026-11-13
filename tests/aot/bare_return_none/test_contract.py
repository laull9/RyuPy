"""失败输入精确合同，不替换空格/尾空格以规避真实兼容缺口。"""
import unittest
from run import cases

class ContractTests(unittest.TestCase):
    def test_original_failing_arguments_preserved(self):
        value=cases()
        self.assertEqual(value['hi_space'],['-f','standard','hi hi'])
        self.assertEqual(value['hello_world'],['-f','standard','hello world'])
        self.assertEqual(value['paragraph'],['-f','standard','The quick brown fox jumps over the lazy dog. '*20])
        self.assertTrue(value['paragraph'][-1].endswith(' '))
        self.assertNotIn('-w',value['paragraph'])

if __name__=='__main__':unittest.main()
