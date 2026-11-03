"""只验一次公开make bench-aot hello管线；不形成性能结论或额外运行产物。"""
import argparse
import hashlib
import json
import math
import os
import shutil
import stat
from pathlib import Path
import sys
import time
import uuid

from platform_runner import Runner
from manifest_files import check_artifacts, digest, plain_stat, files_under
from manifest_verify import MANIFEST_NAME, image_header, no_duplicate_keys, read_sdk_version, validate_schema


def read_json(path):
    plain_stat(path)
    with path.open('rb') as stream:
        raw = stream.read(4 * 1024 * 1024 + 1)
    if len(raw) > 4 * 1024 * 1024:
        raise ValueError('JSON证据超过4MiB')
    return json.loads(raw, object_pairs_hook=no_duplicate_keys), raw


def validate_result(data):
    env = data['environment']
    assert env['backend'] == 'aot' and env['runs'] == 5 and env['warmup'] == 1
    assert env['jit_metrics_applicable'] is False
    assert env['resource_aggregation'] == 'last_timed_process'
    assert len(data['results']) == 1
    result = data['results'][0]
    assert result['name'] == 'hello' and result['status'] == 'ok' and result['backend'] == 'aot'
    assert result['variants'] == {}
    assert isinstance(result['compile_ms'], (int, float)) and math.isfinite(result['compile_ms']) and result['compile_ms'] > 0
    for key in ('cpython', 'ryupy'):
        series = result[key]
        for field in ('wall_ms', 'main_ms', 'maxrss'):
            values = series[field]
            assert len(values) == 5 and all(type(x) in (int, float) and math.isfinite(x) and x >= 0 for x in values)
        assert isinstance(series['stats'], dict) and series['stats']
    return result


def validate_bundle(root, source):
    """本轮唯一新目录绑定清单；仅读文件，不伪造CLI报告或重跑程序。"""
    build, dist = root/'hello.build', root/'hello.dist'
    for path in (root, build, dist): plain_stat(path, directory=True)
    managed = list(build.glob('attempt-*/hello.dll'))
    assert len(managed) == 1
    managed = managed[0]
    plain_stat(managed.parent, directory=True)
    plain_stat(managed)
    sdk_roots = list(managed.parent.glob('ryupy-aot-*'))
    assert len(sdk_roots) == 1
    sdk_root = sdk_roots[0]
    for path in (sdk_root, sdk_root/'host'): plain_stat(path, directory=True)
    sdk = read_sdk_version(sdk_root/'host/sdk.version')
    manifest, raw = read_json(dist/MANIFEST_NAME)
    validate_schema(manifest, 'linux-x64', sdk)
    files = check_artifacts(dist, manifest['artifacts'], MANIFEST_NAME)
    native = [x for x in manifest['artifacts'] if x['role'] == 'native_executable']
    assert len(native) == 1 and native[0]['relative_path'] == 'hello'
    image_header(files['hello'], 'linux-x64')
    assert os.access(files['hello'], os.X_OK)
    inputs = {x['role']: x for x in manifest['build_inputs']}
    for role, path in (('compiled_managed_input', managed), ('runtime_managed_input', managed.parent/'RyuPy.Runtime.dll')):
        size, sha = digest(path)
        assert (size, sha) == (inputs[role]['size'], inputs[role]['sha256'])
    entry = [x for x in manifest['sources'] if x['module_name'] == '__main__']
    assert len(entry) == 1 and entry[0]['compiled_text_sha256'] == hashlib.sha256(source.read_bytes()).hexdigest()
    return {'manifest_sha256': hashlib.sha256(raw).hexdigest(), 'native': native[0], 'build_inputs': manifest['build_inputs'], 'sdk_version': sdk}, raw


def fresh_bundle(parent, before):
    fresh = set(parent.iterdir()) - before if parent.exists() else set()
    assert len(fresh) == 1, '不能安全唯一定位本次fresh树'
    bundle = fresh.pop()
    assert bundle.name.startswith('hello-')
    plain_stat(bundle, directory=True)
    return bundle


def archive_bundle(bundle, out, deadline):
    """仅复制普通fresh树，包含隐藏项/空目录/owner；不声称敌对并发替换安全。"""
    def check():
        if time.monotonic() >= deadline:
            raise TimeoutError('整组1800秒预算耗尽，停止归档')
    def snapshot(root):
        check()
        files_under(root)  # 先拒整树内链接/reparse/特殊文件，包括隐藏项。
        records = {}
        for path in sorted(root.rglob('*')):
            check()
            directory = path.is_dir()
            info = plain_stat(path, directory=directory)
            record = {'directory': directory, 'mode': stat.S_IMODE(info.st_mode)}
            if not directory:
                size, sha = digest(path); record.update(size=size, sha256=sha)
            records[path.relative_to(root).as_posix()] = record
        check()
        return records
    initial = snapshot(bundle)
    destination = out/'owned-bundle'
    destination.mkdir(exist_ok=False)
    for name, record in initial.items():
        check()
        source, target = bundle/name, destination/name
        if record['directory']:
            target.mkdir(exist_ok=False)
        else:
            with source.open('rb') as reader, target.open('xb') as writer:
                while True:
                    check()
                    chunk = reader.read(1024*1024)
                    if not chunk: break
                    writer.write(chunk)
            shutil.copystat(source, target, follow_symlinks=False)
    for name, record in reversed(list(initial.items())):
        if record['directory']:
            check(); shutil.copystat(bundle/name, destination/name, follow_symlinks=False)
    assert snapshot(bundle) == initial == snapshot(destination), '归档前后树身份不同'
    check()
    owner = initial.get('hello.build/ryupy-build-owner.json')
    return {'relative_path':'owned-bundle', 'files_and_directories':initial,
            'owner_sha256':owner['sha256'] if owner and not owner['directory'] else None}


def run(root, out, target):
    started = time.monotonic()
    out.mkdir(parents=True, exist_ok=False)
    logs = out/'logs'; logs.mkdir()
    name = 'aot-hello-smoke-' + uuid.uuid4().hex
    saved = target/'bench/results'/f'{name}.json'
    parent = target/'bench/aot'
    before = set(parent.iterdir()) if parent.exists() else set()
    evidence = {'schema_version': 1, 'purpose': 'command_pipeline_only', 'budget_seconds': 1800,
                'save_name': name, 'expected_application_processes': {'cpython': 6, 'native': 6},
                'process_count_basis': 'fixed runner warmup=1/runs=5, not independent process tracing',
                'per_application_timeout': None, 'per_publish_timeout': None, 'status': 'started'}
    (out/'result.json').write_text(json.dumps(evidence, indent=2))
    try:
        assert sys.platform == 'linux', '仅Ubuntu现有workflow验收'
        assert not saved.exists()
        env = dict(os.environ, CARGO_TARGET_DIR=str(target), PYTHON=sys.executable)
        Runner(logs, deadline=started+1800).run('make-bench-aot',
            ['make', 'bench-aot', 'BENCH=hello', f'ARGS=--save {name}'], cwd=root, env=env, check=True, timeout=1800)
        assert time.monotonic() < started + 1800
        data, raw = read_json(saved)
        (out/'saved-result.json').write_bytes(raw)
        validate_result(data)
        bundle = fresh_bundle(parent, before)
        checked, manifest = validate_bundle(bundle, root/'tests/benchmarks/hello.py')
        (out/'ryupy-artifact.json').write_bytes(manifest)
        archived = archive_bundle(bundle, out, started+1800)
        assert archived['owner_sha256'] is not None, '成功树缺owner'
        copied, copied_manifest = validate_bundle(out/'owned-bundle', root/'tests/benchmarks/hello.py')
        assert copied == checked and copied_manifest == manifest
        evidence['archive'] = archived
        assert time.monotonic() < started + 1800
        evidence.update(status='passed', saved_result_sha256=hashlib.sha256(raw).hexdigest(),
                        bundle_relative_to_target=bundle.relative_to(target).as_posix(), **checked)
    except BaseException as error:
        evidence.update(status='failed', error=f'{type(error).__name__}: {error}')
        # 只在唯一fresh树和剩余预算均明确时留失败现场；不覆盖部分归档或接管未知树。
        if not (out/'owned-bundle').exists():
            try:
                evidence['archive'] = archive_bundle(fresh_bundle(parent, before), out, started+1800)
            except Exception as archive_error:
                evidence['archive_error'] = f'{type(archive_error).__name__}: {archive_error}'
        raise
    finally:
        evidence['elapsed_seconds'] = time.monotonic() - started
        (out/'result.json').write_text(json.dumps(evidence, ensure_ascii=False, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--target', type=Path, default=Path('target'))
    args = parser.parse_args()
    run(args.root.resolve(), args.out.resolve(), args.target.resolve())
