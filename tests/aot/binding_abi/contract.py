"""自有夹具的声明数据与严格 ctypes 参考；生产实现不依赖此模块。"""
import ctypes
import math
import types

SCALARS = dict(zip(
    ('i8', 'u8', 'i16', 'u16', 'i32', 'u32', 'i64', 'u64', 'isize', 'usize', 'f32', 'f64', 'bool8'),
    (ctypes.c_int8, ctypes.c_uint8, ctypes.c_int16, ctypes.c_uint16, ctypes.c_int32,
     ctypes.c_uint32, ctypes.c_int64, ctypes.c_uint64, ctypes.c_ssize_t, ctypes.c_size_t,
     ctypes.c_float, ctypes.c_double, ctypes.c_uint8)))


def parameter(name, kind):
    result = {'name': name, 'type': kind}
    if kind in ('utf8', 'bytes'):
        result.update(ownership='borrowed_call', length='usize')
    return result


def declaration(rid, shared, archive):
    exports = []

    def add(name, symbol, kinds, returns):
        exports.append({'name': name, 'symbol': symbol, 'signature': {
            'calling_convention': 'cdecl', 'parameters': [parameter('value' if len(kinds) == 1
            else 'arg' + str(index), kind) for index, kind in enumerate(kinds)], 'returns': returns}})

    for kind in SCALARS:
        add(kind, 'echo_' + kind, [kind], kind)
    for kind in ('utf8', 'bytes', 'optional'):
        add(kind, kind, ['utf8' if kind == 'utf8' else 'bytes'], 'u64')
    add('mix', 'mix', ['utf8', 'bytes', 'i32'], 'u64')
    add('count', 'call_count', [], 'u64')
    add('empty', 'empty', [], 'void')
    add('bad_bool', 'bad_bool', [], 'bool8')
    add('bad_bool_buffer', 'bad_bool_buffer', ['bytes'], 'bool8')
    add('sixteen', 'sixteen', list(SCALARS)[:12] + ['i32'] * 4, 'i64')
    add('sixteen_buffers', 'sixteen_buffers', ['bytes'] * 16, 'u64')
    add('i32_alias', 'echo_i32', ['i32'], 'i32')
    return {'schema_version': 3, 'module': 'abi', 'library': 'abi_c', 'exports': exports,
            'target': {'rid': rid, 'shared': shared, 'archive': archive}}


def reference(library, declarations):
    module = types.ModuleType('abi')
    library = ctypes.CDLL(str(library))
    for export in declarations['exports']:
        signature = export['signature']
        native = getattr(library, export['symbol'])
        kinds = [item['type'] for item in signature['parameters']]
        native.argtypes = [item for kind in kinds for item in (
            [ctypes.c_void_p, ctypes.c_size_t] if kind in ('utf8', 'bytes') else [SCALARS[kind]])]
        result_kind = signature['returns']
        native.restype = None if result_kind == 'void' else SCALARS[result_kind]

        def invoke(*arguments, _native=native, _kinds=kinds, _result=result_kind):
            if len(arguments) != len(_kinds):
                raise TypeError('arity')
            converted, buffers = [], []
            for value, kind in zip(arguments, _kinds):
                if kind in ('utf8', 'bytes'):
                    if type(value) is not (str if kind == 'utf8' else bytes):
                        raise TypeError('buffer type')
                    data = value.encode('utf-8', errors='strict') if kind == 'utf8' else value
                    buffer = ctypes.create_string_buffer(data, max(1, len(data)))
                    buffers.append(buffer)
                    converted.extend((ctypes.cast(buffer, ctypes.c_void_p), len(data)))
                elif kind == 'bool8':
                    if type(value) is not bool:
                        raise TypeError('bool type')
                    converted.append(int(value))
                elif kind in ('f32', 'f64'):
                    if type(value) is not float:
                        raise TypeError('float type')
                    if kind == 'f32' and math.isfinite(value) and math.isinf(ctypes.c_float(value).value):
                        raise OverflowError('float32 range')
                    converted.append(value)
                else:
                    if type(value) is not int:
                        raise TypeError('integer type')
                    width = ctypes.sizeof(SCALARS[kind]) * 8
                    signed = kind.startswith('i')
                    limit = 1 << (width - 1 if signed else width)
                    if not (-limit if signed else 0) <= value < limit:
                        raise OverflowError('integer range')
                    converted.append(value)
            result = _native(*converted)
            if _result == 'bool8':
                if result not in (0, 1):
                    raise ValueError('noncanonical bool8')
                return bool(result)
            return result

        setattr(module, export['name'], invoke)
    return module
