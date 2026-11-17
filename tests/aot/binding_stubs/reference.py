"""同次固定CPython参考：复用已有ABI严格ctypes适配器，执行原样手写应用。"""
import ctypes
import json
from pathlib import Path
import runpy
import sys
import types

binding = json.loads(Path(sys.argv[1]).read_text())
if binding['schema_version'] == 3:
    from contract import reference
    module = reference(Path(sys.argv[2]), binding)
else:
    library = ctypes.CDLL(sys.argv[2])
    module = types.ModuleType(binding['module'])
    for export in binding['exports']:
        native = getattr(library, export['symbol'])
        native.argtypes = [ctypes.c_int32, ctypes.c_int32]
        native.restype = ctypes.c_int64
        def make(operation):
            def invoke(left, right, /):
                for argument in (left, right):
                    if type(argument) is not int:
                        raise TypeError('actual integer required')
                    if not -(2**31) <= argument < 2**31:
                        raise OverflowError('outside signed32')
                return operation(left, right)
            return invoke
        setattr(module, export['name'], make(native))
sys.modules[binding['module']] = module
runpy.run_path(sys.argv[3], run_name='__main__')
