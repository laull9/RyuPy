"""CPython对照：为同一受信C库建立严格参数wrapper，再运行原样Python应用。"""
import ctypes
import runpy
import sys
import types

library = ctypes.CDLL(sys.argv[1])
module = types.ModuleType('native_math')
for name, symbol in [('add', 'demo_add'), ('subtract', 'demo_subtract'), ('add_alias', 'demo_add')]:
    native = getattr(library, symbol)
    native.argtypes = [ctypes.c_int32, ctypes.c_int32]
    native.restype = ctypes.c_int64
    def make_function(operation):
        def function(left, right, /):
            for argument in (left, right):
                if type(argument) is not int:
                    raise TypeError('actual integer required')
                if not -(2**31) <= argument < 2**31:
                    raise OverflowError('outside signed32')
            return operation(left, right)
        return function
    function = make_function(native)
    function.__name__ = name
    setattr(module, name, function)
sys.modules['native_math'] = module
sys.argv = [sys.argv[2]]
runpy.run_path(sys.argv[0], run_name='__main__')
