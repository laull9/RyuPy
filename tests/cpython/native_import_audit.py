"""C1 导入胶水退出的真实帧门：仅匹配原内部源码文件，保留标准库同名方法帧。"""
import json

REMOVED = ('_RyuPyFinder', '_RyuPyLoader', 'find_spec', 'create_module', 'exec_module')


def verify_import_audit(path, native_methods=False):
    report = json.loads(path.read_text())
    audit = report['execution_audit']
    assert report['state'] == 'closed'
    assert audit['enabled'] and audit['hook_installed'] and audit['finalized']
    assert not audit['complete'] and not audit['startup_complete']
    assert audit['dropped_frames'] == audit['coverage_failures'] == 0
    frames = audit['frames']
    for phase in ('startup', 'runtime', 'shutdown'):
        assert audit[phase + '_frames'] > 0
        assert sum(row['calls'] for row in frames if row['phase'] == phase) == audit[phase + '_frames']
    internal = [row for row in frames if row['file'] == '<string>']
    for name in REMOVED:
        assert sum(row['calls'] for row in internal if row['function'] == name) == 0, (name, internal)
    for name in ('_rp_wrap_method', '_rp_method'):
        calls = sum(row['calls'] for row in internal if row['function'] == name)
        assert (calls == 0 if native_methods else calls > 0), (name, calls)
