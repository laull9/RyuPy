"""hello发布搬迁正确性门；原路径暂隐藏，额外执行不混入性能样本。"""
from contextlib import contextmanager
import json
import os
import sys
import uuid

from manifest_files import plain_stat
from manifest_verify import relocate_manifest


class RestorationError(ValueError):
    """全部恢复尝试完成后汇总错误；未恢复的hidden路径保留供人工处理。"""


@contextmanager
def hidden_originals(paths, *, recovery=None):
    """逐项恢复所有安全路径；不能用原路径存在冒充原对象已恢复。"""
    state = recovery if recovery is not None else {}
    state.update(complete=True, paths=[], errors=[])
    moved = []
    try:
        for original in paths:
            directory = original.is_dir()
            info = plain_stat(original, directory=directory)
            hidden = original.with_name('.ryupy-relocation-' + uuid.uuid4().hex + '-' + original.name)
            if os.path.lexists(hidden):
                raise ValueError('隔离目标已存在')
            original.rename(hidden)
            record = {'original': str(original), 'hidden': str(hidden), 'restored': False}
            state['paths'].append(record)
            state['complete'] = False
            moved.append((original, hidden, directory, (info.st_dev, info.st_ino), record))
        if any(os.path.lexists(original) for original, *_ in moved):
            raise ValueError('原路径未成功隐藏')
        yield
    finally:
        for original, hidden, directory, identity, record in reversed(moved):
            try:
                if os.path.lexists(original):
                    raise ValueError(f'拒绝覆盖搬迁期间新出现的原路径：{original}')
                info = plain_stat(hidden, directory=directory)
                if (info.st_dev, info.st_ino) != identity:
                    raise ValueError(f'隐藏路径身份变化，保留现场：{hidden}')
                hidden.rename(original)
                restored = plain_stat(original, directory=directory)
                if (restored.st_dev, restored.st_ino) != identity or os.path.lexists(hidden):
                    raise ValueError(f'恢复后身份不符：{original}')
                record['restored'] = True
            except (OSError, ValueError) as error:
                record['error'] = f'{type(error).__name__}: {error}'
                state['errors'].append(record['error'])
                # 继续恢复其余无冲突路径，不删除新出现的文件或未恢复的hidden。
        state['complete'] = all(record['restored'] for record in state['paths'])
        if state['errors']:
            raise RestorationError('路径恢复失败：' + '; '.join(state['errors']))


def verify_relocation(bundle, source, out, sdk_version, runner):
    """仅搬迁dist；oracle在隐藏前生成，native直接在无工具PATH的独立cwd执行。"""
    area = out/'搬迁 空格 & 验证'
    area.mkdir()
    cwd = area/'empty-cwd'; cwd.mkdir()
    home = area/'home'; home.mkdir()
    tmp = area/'tmp'; tmp.mkdir()
    empty_path = area/'empty-path'; empty_path.mkdir()
    oracle = area/'oracle-input'; oracle.mkdir()
    oracle_source = oracle/'hello.py'
    plain_stat(source)
    oracle_source.write_bytes(source.read_bytes())
    # 不继承LD_LIBRARY_PATH、DOTNET/COMPlus、PYTHONPATH、RYUPY_STATS等宿主注入。
    env = {'PATH': str(empty_path), 'HOME': str(home), 'TMPDIR': str(tmp),
           'LANG': 'C.UTF-8', 'LC_ALL': 'C.UTF-8',
           'DOTNET_ROOT': str(area/'absent-dotnet'),
           'PYTHONHOME': str(area/'absent-python'), 'PYTHONPATH': str(area/'absent-python')}
    report = {'status': 'started', 'purpose': 'relocation_correctness_only',
              'clean_machine': False, 'native_processes': 0, 'cpython_processes': 0,
              'original_build_dist_hidden_during_native': False, 'source_checkout_hidden': False}
    recovery = {'complete': True, 'paths': [], 'errors': []}
    report['path_restoration'] = recovery
    report_path = out/'relocation.json'
    try:
        # -I忽略污染的Python环境；使用同次固定解释器和未改源码，不冻结golden。
        expected = runner.run('relocation-cpython', [sys.executable, '-I', '-B', str(oracle_source)],
                              cwd=cwd, env=env, check=True, timeout=30)
        report['cpython_processes'] = 1
        native, _ = relocate_manifest(bundle/'hello.dist', bundle/'hello.dist/hello',
                                      area/'hello.dist', 'linux-x64', sdk_version)
        # 只隐藏本轮自有build/dist及oracle副本，不移动仓库源码或用户文件。
        # 原编译源码目录仍可访问，因此不宣称隔绝整个开发机文件系统。
        with hidden_originals((bundle, oracle), recovery=recovery):
            report['original_build_dist_hidden_during_native'] = True
            actual = runner.run('relocation-native', [str(native)], cwd=cwd, env=env,
                                check=True, timeout=30)
            report['native_processes'] = 1
            if (actual.returncode, actual.stdout, actual.stderr) != (expected.returncode, expected.stdout, expected.stderr):
                raise ValueError('搬迁native退出码/stdout/stderr与同次CPython不一致')
        report.update(status='passed', destination=str(native.relative_to(out)),
                      original_paths_restored=recovery['complete'], stdout_bytes=len(actual.stdout))
        return report
    except BaseException as error:
        report.update(status='failed', error=f'{type(error).__name__}: {error}',
                      original_paths_restored=recovery['complete'])
        raise
    finally:
        report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2))
