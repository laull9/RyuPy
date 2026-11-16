# 仅从预先核定来源路径推导原始metadata，绝不消费Host观察输出。
from pathlib import PurePosixPath


def expected_for_origin(origin):
    if not isinstance(origin, str) or not origin.startswith('/') or not origin.endswith('.py'):
        raise ValueError('需要实际部署的Linux源码绝对位置或虚拟源锚')
    path = PurePosixPath(origin)
    cached = str(path.parent / '__pycache__' / (path.stem + '.cpython-312.pyc'))
    loader = ['compiled-loader', 'subject', origin]
    slots = [
        ['__name__', ['str', 'subject']],
        ['__doc__', ['None', None]],
        ['__package__', ['str', '']],
        ['__loader__', loader],
        ['__spec__', ['ModuleSpec', 'subject', origin, loader]],
        ['__file__', ['str', origin]],
        ['__cached__', ['str', cached]],
    ]
    # module_from_spec在exec前不插builtins；受控seed随后独立写入真实字典。
    return {'functional': [slots, slots, slots, slots], 'default': [slots], 'indirect': []}
