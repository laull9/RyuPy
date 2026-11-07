import importlib.util as util
if False:
    import failpkg.user_matching
    import failpkg.class_matching
    import failpkg.overlap_matching
    import failpkg.wrong_matching

def expect_missing(name):
    spec = util.find_spec(name)
    target = util.module_from_spec(spec)
    try:
        spec.loader.exec_module(target)
    except ImportError as error:
        assert type(error) is ImportError
    else:
        raise AssertionError('missing error')
    assert target.a.__name__ == 'failpkg.a'

expect_missing('failpkg.user_matching')
expect_missing('failpkg.class_matching')
expect_missing('failpkg.overlap_matching')
spec = util.find_spec('failpkg.wrong_matching')
wrong = util.module_from_spec(spec)
try:
    spec.loader.exec_module(wrong)
except RuntimeError as error:
    assert type(error).__name__ == 'ModuleNotFoundError'
else:
    raise AssertionError('wrong family was swallowed')
assert 'a' not in wrong.__dict__
print('user exception name fields and real ancestry preserved')

# 普通公开读取不能继承相对导入的比较能力限制。
class NumericName(ImportError):
    name = 7
assert NumericName().name == 7
