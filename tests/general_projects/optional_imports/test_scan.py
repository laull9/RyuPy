"""静态可选导入边界：只验证执行时的异常保护、必需优先及异常名遮蔽。"""
import ast
import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
SPEC = importlib.util.spec_from_file_location(
    'dependency_collector', ROOT / 'crates/ryupy-driver/src/cpython_dependencies.py')
COLLECTOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(COLLECTOR)


def imports(source):
    return {node.names[0].name: optional
            for node, optional in COLLECTOR.source_imports(ast.parse(source))}


class ProtectionTests(unittest.TestCase):
    def test_only_try_body_inherits_current_handler(self):
        self.assertEqual(imports('''
try:
    import body
except (ValueError, ImportError):
    import handler
else:
    import otherwise
finally:
    import final
'''), dict(body=True, handler=False, otherwise=False, final=False))

    def test_nested_handler_else_and_finally_retain_outer_body(self):
        self.assertEqual(imports('''
try:
    try:
        import inner
    except ValueError:
        import handler
    else:
        import otherwise
    finally:
        import final
except ModuleNotFoundError:
    pass
'''), dict(inner=True, handler=True, otherwise=True, final=True))

    def test_function_body_resets_but_class_body_retains_protection(self):
        self.assertEqual(imports('''
try:
    class Model:
        import class_body
        def method(self):
            import method_body
    def function():
        import function_body
        try:
            import local_optional
        except ImportError:
            pass
    async def coroutine():
        import async_body
except ImportError:
    pass
'''), dict(class_body=True, method_body=False, function_body=False,
          local_optional=True, async_body=False))

    def test_broad_and_unknown_handlers_remain_required(self):
        for handler in ('Exception', 'BaseException', 'ValueError', 'other.ImportError', ''):
            with self.subTest(handler=handler):
                self.assertEqual(imports(
                    'try:\n    import dependency\nexcept' + (' ' + handler if handler else '') + ':\n    pass\n'),
                    {'dependency': False})

    def test_shadowed_exception_name_remains_required(self):
        for binding in ('ImportError = ValueError', 'def ImportError(): pass',
                        'import other as ImportError', 'class ImportError(Exception): pass',
                        'def outer(ImportError): pass'):
            with self.subTest(binding=binding):
                self.assertEqual(imports(binding + '''
try:
    import dependency
except ImportError:
    pass
'''), {'other': False, 'dependency': False} if binding.startswith('import ') else {'dependency': False})

    def test_except_star_is_conservatively_required(self):
        self.assertEqual(imports('''
try:
    import dependency
except* ImportError:
    pass
'''), {'dependency': False})
        self.assertEqual(imports('''
try:
    try:
        import dependency
    except* ValueError:
        pass
except ImportError:
    pass
'''), {'dependency': False})


if __name__ == '__main__':
    unittest.main()
