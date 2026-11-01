# M3.7：TestCase 与结果统计，不输出含耗时的文本 runner。
import unittest


class Example(unittest.TestCase):
    def setUp(self):
        self.value = [1]

    def test_equal(self):
        self.assertEqual(self.value, [1])

    def test_raises(self):
        with self.assertRaises(ValueError):
            int("bad")


suite = unittest.defaultTestLoader.loadTestsFromTestCase(Example)
result = unittest.TestResult()
suite.run(result)
print(result.testsRun, result.wasSuccessful(), len(result.failures), len(result.errors))
