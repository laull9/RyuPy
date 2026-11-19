#!/usr/bin/env python3
"""两独立绑定声明的真正stub导出：同次CP差分、JIT/AOT删输入搬迁、解析及机器证据。"""
import argparse
import ast
import copy
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tests/cpython'))
from support import built_product, product_layout

def inspect_interface(bundle, declaration, rid):
    """只信manifest精确路径与角色；逐导出交叉核对实际PInvoke和原始声明。"""
    layout = product_layout(bundle)
    manifest = json.loads((layout.data / 'ryupy-artifact.json').read_text())
    interface = manifest['binding_interfaces']
    root = bundle / interface['search_root']
    stub = bundle / interface['python_stub']
    machine = json.loads((bundle / interface['machine_contract']).read_text())
    assert stub == root / (declaration['module'] + '.pyi')
    tree = ast.parse(stub.read_text(), filename=str(stub), feature_version=(3, 12))
    functions = [node for node in tree.body if isinstance(node, ast.FunctionDef)]
    assert functions
    for function in functions:
        assert not function.args.args and not function.args.kwonlyargs
        assert function.args.vararg is None and function.args.kwarg is None
    assert machine['target'] == {'rid': rid, 'pointer_bits': 64}
    assert machine['declaration_version'] == declaration['schema_version']
    assert machine['module'] == declaration['module']
    assert len(machine['exports']) == len(declaration['exports'])
    for index, (record, source, emitted) in enumerate(zip(
            machine['exports'], declaration['exports'], manifest['native_bindings'])):
        assert record['binding_id'] == emitted['binding_id'] == 'b' + str(index)
        assert record['export'] == emitted['export'] == source['name']
        assert record['symbol'] == emitted['symbol'] == source['symbol']
        assert record['library'] == emitted['library'] == declaration['library']
        assert record['declared_signature'] == emitted['signature'] == source['signature']
        assert record['machine']['pinvoke_signature'] == emitted['native']['signature']
        assert record['machine']['verified_report_reference'].endswith(
            '/native_bindings/' + str(index) + '/native/signature')
        assert record['python']['keywords'] is False
        parameters = record['python']['parameters']
        assert all(p['kind'] == 'positional_only' and p['required'] for p in parameters)
        if declaration['schema_version'] == 3:
            assert [p['declared_name'] for p in parameters] == [
                p['name'] for p in source['signature']['parameters']]
        assert record['same_symbol_exports'] == [other['name'] for other in declaration['exports']
            if other is not source and other['symbol'] == source['symbol']]
        if source['name'] == 'class':
            assert record['python']['representation'] == 'module_getattr_literal_callable'
            assert 'Literal["class"]' in stub.read_text()
    for key, role in [('python_stub', 'native_binding_python_stub'),
                      ('machine_contract', 'native_binding_machine_contract')]:
        matches = [item for item in manifest['artifacts'] if item['role'] == role]
        assert len(matches) == 1 and matches[0]['relative_path'] == interface[key]
        assert (bundle / interface[key]).is_file() and matches[0]['size'] > 0
    return {'stub': interface['python_stub'], 'machine': interface['machine_contract'],
            'exports': len(machine['exports']), 'rid': rid}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--python', default=sys.executable)
    args = parser.parse_args()
    args.ryupy = args.ryupy.resolve()
    assert not args.out.resolve().is_relative_to(ROOT), '构建/验收产物必须放仓库外'
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='binding-stubs-', dir=args.out.resolve()))
    logs = work / 'logs'
    logs.mkdir()
    print(work, flush=True)
    record = {'status': 'failed', 'steps': [], 'interfaces': {}}
    env = {**os.environ, 'DOTNET_CLI_UI_LANGUAGE': 'en-US', 'PYTHONUTF8': '1'}

    def run(label, command, *, cwd=work, timeout=120, environment=env):
        command = list(map(str, command))
        result = subprocess.run(command, cwd=cwd, env=environment, capture_output=True, timeout=timeout)
        for suffix, stream in [('stdout', result.stdout), ('stderr', result.stderr)]:
            (logs / (label + '.' + suffix)).write_bytes(stream)
        record['steps'].append({'step': label, 'argv': command, 'exit': result.returncode})
        print(label, result.returncode, flush=True)
        assert result.returncode == 0, (label, result.stderr.decode(errors='replace'))
        return result

    try:
        win, mac = sys.platform == 'win32', sys.platform == 'darwin'
        rid = 'win-x64' if win else 'osx-arm64' if mac else 'linux-x64'
        assert platform.machine().lower() in ('arm64',) if mac else platform.machine().lower() in ('amd64', 'x86_64')
        record['rid'] = rid
        fixture = Path(__file__).resolve().parent
        unrelated = work / 'unrelated'
        unrelated.mkdir()
        empty_tools = work / 'empty-tools'
        empty_tools.mkdir()
        restricted = {key: value for key, value in env.items() if not key.upper().startswith('PYTHON')}
        restricted.update(PATH=str(empty_tools), PYTHONHOME='/absent/binding-stubs-python')
        for family in ['legacy', 'typed']:
            source = work / (family + '-inputs')
            source.mkdir()
            origin = fixture.parent / ('binding_multi' if family == 'legacy' else 'binding_abi')
            for name in ['library.c']:
                shutil.copy2(origin / name, source / name)
            shutil.copy2(fixture / (family + '_app.py'), source / 'app.py')
            shutil.copy2(fixture / 'reference.py', source / 'reference.py')
            shutil.copy2(fixture.parent / 'binding_abi/contract.py', source / 'contract.py')
            shared = source / ('fixture.dll' if win else 'fixture.dylib' if mac else 'fixture.so')
            archive = source / ('fixture.lib' if win else 'fixture.a')
            obj = source / ('fixture.obj' if win else 'fixture.o')
            if win:
                run(family + '-object', ['cl', '/nologo', '/O2', '/MT', '/c', source / 'library.c', '/Fo' + str(obj)])
                run(family + '-shared', ['link', '/NOLOGO', '/DLL', '/OUT:' + str(shared), obj])
                run(family + '-archive', ['lib', '/NOLOGO', '/OUT:' + str(archive), obj])
            else:
                flags = ['-mmacosx-version-min=12.0'] if mac else []
                run(family + '-object', ['cc', '-O2', '-Wall', '-Wextra', '-Werror', '-fPIC', *flags,
                    '-c', source / 'library.c', '-o', obj])
                run(family + '-shared', ['cc', *flags, '-dynamiclib' if mac else '-shared', obj, '-o', shared])
                run(family + '-archive', ['ar', 'rcs', archive, obj])
            declaration = {'schema_version': 2, 'module': 'harvest_math', 'library': 'harvest_native',
                'target': {'rid': rid, 'shared': shared.name, 'archive': archive.name}, 'exports': [
                    {'name': name, 'symbol': symbol, 'signature': 'cdecl_i32_i32_i64'} for name, symbol in
                    [('add', 'demo_add'), ('subtract', 'demo_subtract'), ('class', 'demo_add'), ('alias', 'demo_add')]]}
            if family == 'typed':
                sys.path.insert(0, str(fixture.parent / 'binding_abi'))
                from contract import declaration as abi_declaration
                base = abi_declaration(rid, shared.name, archive.name)
                selected = dict(integer='u64', small='i8', real='f32', flag='bool8', empty='empty',
                    bytes='bytes', mix='mix', bad_bool='bad_bool', alias='u64', **{'class': 'utf8'})
                declaration = {**base, 'module': 'river_codec', 'library': 'river_native', 'exports': []}
                for name, previous in selected.items():
                    export = copy.deepcopy(next(e for e in base['exports'] if e['name'] == previous))
                    export['name'] = name
                    if name == 'mix':
                        for parameter, original in zip(export['signature']['parameters'], ['for', '_p0', '__debug__']):
                            parameter['name'] = original
                    declaration['exports'].append(export)
            binding = source / 'binding.json'
            binding.write_text(json.dumps(declaration), encoding='utf-8')
            oracle = run(family + '-cpython', [args.python, source / 'reference.py', binding, shared, source / 'app.py'])
            assert oracle.stderr == b'' and oracle.stdout.endswith((family + '-stub-ok\n').encode())
            products = []
            for mode in ['jit', 'aot']:
                label = family + '-' + mode
                output = work / (label + '-build')
                command = [args.ryupy, 'build', source / 'app.py', '--native-binding', binding, '-o', output]
                built = run(label + '-build', command + (['--aot'] if mode == 'aot' else []), timeout=600)
                layout = built_product(built.stderr, output, 'app', mode)
                entry = layout.entry
                if mode == 'aot':
                    assert entry == output / 'app.dist' / ('app.exe' if win else 'app')
                bundle = layout.root
                record['interfaces'][label] = inspect_interface(bundle, declaration, rid)
                moved = work / (label + '-relocated')
                shutil.move(bundle, moved)
                products.append((label, moved / entry.name, mode))
                if output.exists():
                    shutil.rmtree(output)
            # 原声明、C输入、静/动态库与中间构建树全部删除后，仅运行搬迁交付物。
            shutil.rmtree(source)
            for label, entry, mode in products:
                record['interfaces'][label + '-relocated'] = inspect_interface(entry.parent, declaration, rid)
                actual = run(label + '-relocated', [entry],
                             cwd=unrelated, environment=restricted)
                assert (actual.returncode, actual.stdout, actual.stderr) == (
                    oracle.returncode, oracle.stdout, oracle.stderr), label
            record['steps'].append({'step': family + '-inputs-and-builds-deleted', 'exit': 0})
        record['status'] = 'passed'
    finally:
        (work / 'result.json').write_text(json.dumps(record, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
