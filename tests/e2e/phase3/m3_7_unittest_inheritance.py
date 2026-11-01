# 原生TestCase必须是真实可继承类：别名、继承方法与用户覆盖沿普通MRO执行。
import unittest as testing
from unittest import TestCase as BaseCase

class First(BaseCase):
    def setUp(self):
        super().setUp()
        self.values = [2]
    def test_value(self):
        self.assertEqual(self.values, [2])

class Second(testing.TestCase):
    def setUp(self):
        self.values = [3]
    def test_value(self):
        self.assertTrue(self.values == [3])

class Derived(First):
    def setUp(self):
        super().setUp()
        self.values.append(4)
    def test_value(self):
        self.assertEqual(self.values, [2, 4])

for cls in [First, Second, Derived]:
    case = cls('test_value')
    result = testing.TestResult()
    case.run(result)
    print(cls.__name__, result.testsRun, result.wasSuccessful())
print(issubclass(First, BaseCase), isinstance(First(), testing.TestCase))
