# unittest 生命周期、继承覆盖、断言与错误归类。
import unittest
log = []
class Base(unittest.TestCase):
    def setUp(self):
        log.append('setup ' + self._testMethodName)
    def tearDown(self):
        log.append('teardown ' + self._testMethodName)
    def test_base(self):
        log.append('base')
class Cases(Base):
    def test_base(self):
        log.append('override')
    def test_error(self):
        raise ValueError('oops')
    def test_failure(self):
        self.assertEqual(1, 2)
    def test_ok(self):
        self.assertTrue([1])
        self.assertFalse([])
        self.assertIs(None, None)
        self.assertIsNone(None)
        with self.assertRaises(ValueError) as context:
            raise ValueError('caught')
        self.assertEqual(str(context.exception), 'caught')
        self.assertRaises(ValueError, int, 'bad')
suite = unittest.defaultTestLoader.loadTestsFromTestCase(Cases)
print(suite.countTestCases())
r = unittest.TestResult()
print(suite.run(r) is r)
print(r.testsRun, len(r.failures), len(r.errors), r.wasSuccessful())
print(log)
class SetupFailure(unittest.TestCase):
    def setUp(self):
        log.append('setup fail')
        raise ValueError('setup')
    def tearDown(self):
        log.append('wrong teardown')
    def test_one(self):
        log.append('wrong test')
log.clear()
r = unittest.TestResult()
SetupFailure('test_one').run(r)
print(r.testsRun, len(r.errors), log)
print('test_error' in r.errors[0][1], 'test_failure' in r.errors[0][1])
case = unittest.TestCase()
try:
    with case.assertRaises(ValueError):
        pass
except AssertionError:
    print('missing exception')
try:
    with case.assertRaises(ValueError):
        raise TypeError('wrong')
except TypeError:
    print('wrong exception')
try:
    case.assertRaises(42)
except TypeError:
    print('invalid exception type')
# setUp 失败也执行 LIFO 清理，单个清理失败不阻断剩余清理。
class Cleanup(unittest.TestCase):
    def setUp(self):
        self.addCleanup(log.append, 'first')
        self.addCleanup(self.bad_cleanup)
        self.addCleanup(log.append, 'last')
        raise ValueError('setup')
    def bad_cleanup(self):
        log.append('bad cleanup')
        raise ValueError('cleanup')
    def test_one(self):
        pass
log.clear()
r = unittest.TestResult()
Cleanup('test_one').run(r)
print(r.testsRun, len(r.errors), log)
# 已消费 suite 槽不再次运行，并保留总计数。
suite = unittest.defaultTestLoader.loadTestsFromTestCase(Cases)
r = unittest.TestResult()
suite.run(r)
print(suite.countTestCases())
try:
    suite.run(r)
except TypeError:
    print('consumed suite')
with case.assertRaises(SystemExit) as context:
    raise SystemExit(7)
print(context.exception.code)
# suite 游标是活视图，能看见追加测试及执行后清除的槽。
a = Cases('test_base')
b = Cases('test_base')
suite = unittest.TestSuite([a])
it = iter(suite)
suite.addTest(b)
print([item is a for item in it])
it = iter(suite)
suite.run(unittest.TestResult())
print(list(it))
