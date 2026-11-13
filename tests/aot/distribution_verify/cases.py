"""真实包独立复制负例；自洽重写清单不属于身份/恶意篡改防护承诺。"""
import copy
import json
import os
from pathlib import Path
import shutil
from common import capture, digest, require, save, tree

MANIFEST = 'ryupy-artifact.json'


def verify(cli, package, label, logs, cwd, env, marker, success):
    before = tree(package); save(logs/(label+'-before.json'), before)
    actual = capture(logs/label, [cli, 'verify', package], cwd, env)
    after = tree(package); save(logs/(label+'-after.json'), after)
    require(before == after, 'verify写入了待验包：'+label)
    require(not marker.exists(), 'verify启动了SDK/Python哨兵：'+label)
    try:
        report = json.loads(actual.stdout)
    except (ValueError, UnicodeError):
        report = None
    if success:
        require(actual.returncode == 0 and actual.stderr == b'', '验包未成功：'+label)
        manifest = json.loads((package/MANIFEST).read_text(encoding='utf-8'))
        expected = {'report_schema_version': 1, 'kind': 'ryupy-distribution-integrity-verification',
                    'content_integrity_verified': True,
                    'manifest_schema_version': manifest['schema_version'],
                    'reported_target_rid': manifest.get('target', {}).get('rid'),
                    'native_executable': next(a['relative_path'] for a in manifest['artifacts']
                                              if a['role'] == 'native_executable'),
                    'artifact_count': len(manifest['artifacts']),
                    'total_bytes': sum(a['size'] for a in manifest['artifacts']),
                    'authenticity': 'not_checked', 'runtime_compatibility': 'not_checked',
                    'schema_semantics': 'not_checked'}
        require(report == expected, '报告计数/边界字段不符：'+label)
    else:
        require(actual.returncode != 0, '坏包被接受：'+label)
        require(not isinstance(report, dict) or report.get('content_integrity_verified') is not True,
                '失败输出冒充成功报告：'+label)
    return {'case': label, 'exit': actual.returncode, 'content_and_permissions_unchanged': True,
            'report': report, 'expected_success': success}


def mutation_cases(cli, original, output, logs, cwd, env, marker):
    baseline = tree(original); output.mkdir(); rows = []
    manifest = json.loads((original/MANIFEST).read_text(encoding='utf-8'))
    native = next(a['relative_path'] for a in manifest['artifacts'] if a['role'] == 'native_executable')
    def mutate(label, change, good=False):
        package = output/label; shutil.copytree(original, package)
        current = copy.deepcopy(manifest); change(package, current)
        if label not in ('tamper', 'missing', 'extra', 'empty-directory', 'readonly', 'link'):
            save(package/MANIFEST, current)
        row = verify(cli, package, label, logs, cwd, env, marker, good)
        rows.append(row); require(tree(original) == baseline, '负例改动了原包')
    def tamper(package, _):
        path = package/native
        with path.open('r+b') as stream:
            first = stream.read(1); require(first, '空native'); stream.seek(0); stream.write(bytes([first[0]^1]))
    mutate('tamper', tamper)
    mutate('missing', lambda p, _: (p/native).unlink())
    mutate('extra', lambda p, _: (p/'unlisted.txt').write_bytes(b'extra'))
    mutate('unknown-schema', lambda _, m: m.update(schema_version=99))
    mutate('jit-kind', lambda _, m: m.update(kind='ryupy-managed-artifact'))
    mutate('escape', lambda _, m: m['artifacts'][0].update(relative_path='../outside'))
    mutate('duplicate', lambda _, m: m['artifacts'].append(copy.deepcopy(m['artifacts'][0])))
    mutate('no-native', lambda _, m: next(a for a in m['artifacts'] if a['role'] == 'native_executable').update(role='sidecar'))
    def two_native(package, m):
        extra = package/'second-native.bin'; extra.write_bytes(b'not executed')
        m['artifacts'].append({'relative_path': extra.name, 'role': 'native_executable',
                               'size': extra.stat().st_size, 'sha256': digest(extra)})
    mutate('two-native', two_native)
    def self_consistent(package, m):
        tamper(package, m)
        next(a for a in m['artifacts'] if a['role'] == 'native_executable')['sha256'] = digest(package/native)
    mutate('self-consistent-not-authenticated', self_consistent, True)
    rows[-1]['scope'] = '仅文件与自报清单一致；故意改native并同步摘要，不执行该副本，不是身份认证'
    mutate('empty-directory', lambda p, _: (p/'unlisted-empty-directory').mkdir(), True)
    def readonly(package, _):
        for path in sorted(package.rglob('*'), reverse=True):
            path.chmod(0o555 if path.is_dir() else 0o444)
        package.chmod(0o555)
    mutate('readonly', readonly, True)
    rows[-1]['readonly_scope'] = ('Windows chmod只读属性，不等价目录ACL拒写' if os.name == 'nt'
                                  else 'POSIX去写位；不声称特权用户无法绕过')
    package = output/'link'; shutil.copytree(original, package)
    try:
        (package/'unlisted-link').symlink_to(original/native)
    except OSError as error:
        if os.name != 'nt' or error.winerror != 1314: raise
        rows.append({'case': 'link', 'skip': 'Windows创建符号链接权限不足', 'winerror': error.winerror})
    else:
        rows.append(verify(cli, package, 'link', logs, cwd, env, marker, False))
    require(tree(original) == baseline, '负例结束后原包变化')
    return rows
