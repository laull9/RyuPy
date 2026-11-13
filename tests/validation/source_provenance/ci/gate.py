"""来源闭包有界runner草案：复用已存在Recorder；不构建CLI/runtime，不提供自动重试。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import sys
import traceback

from warning_split import cases, instance_modules, load as load_split, check_cp, rejection


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(ok, text):
    if not ok:
        raise RuntimeError(text)


def save(path, obj):
    Path(path).write_text(json.dumps(obj, ensure_ascii=False, indent=2) + '\n')


def project_output(data, roots):
    """只投影明确warning-record末两列的已知绝对filename；原流始终保留。"""
    lines, locations = [], []
    for line in data.decode().splitlines(keepends=True):
        prefix = line.split(' ', 1)[0]
        record = prefix in ('record', 'lifecycle-record') or any(
            line.startswith(action + ' record ') for action in ('default', 'module', 'once'))
        if record:
            start, filename, lineno = line.rstrip('\n').rsplit(' ', 2)
            require(lineno.isdigit(), 'warning源行不是整数')
            matches = [root for root in roots if filename.startswith(str(root) + '/')]
            if matches:
                relative = str(Path(filename).relative_to(matches[0]))
                locations.append({'record': start, 'source': relative, 'line': int(lineno)})
                filename = Path(filename).name
            elif '/' in filename:
                raise RuntimeError('未知warning绝对路径，不可宽归一化：' + filename)
            line = f'{start} {filename} {lineno}\n'
        lines.append(line)
    return ''.join(lines).encode(), locations


def compare(case, cp, actual, role, fixture, oracle_root):
    """正例全流相等，三个已声明差异分支独立报告；None控制绝不猜CP值。"""
    cp_out, cp_err = (oracle_root / 'stdout').read_bytes(), (oracle_root / 'stderr').read_bytes()
    cp_status = json.loads((oracle_root / 'result.json').read_text())
    require(cp_status['exit'] == 0, 'CP完整输入自身未通过')
    if role == 'old':
        require(actual.returncode == 0 or (actual.returncode == 1 and actual.stderr == b'NotImplementedError: warning source file maps to multiple modules\n'), '未核旧引擎失败，停止并交审，不扩剩余')
        return {'classification': 'old-control-observed', 'exit': actual.returncode,
                'equal_stdout': actual.stdout == cp_out, 'equal_stderr': actual.stderr == cp_err}
    require(actual.returncode == 0, '新JIT不是正常成功退出')
    mode = case['comparison']
    if mode == 'original-warning-location':
        require(actual.stdout == cp_out, '原warning stdout改变')
        require(actual.stderr == b'__init__.py:14: UserWarning: origin-probe\n', '原warning位置/类别/文本改变')
        require(cp_err.endswith(b':14: UserWarning: origin-probe\n  warnings.warn("origin-probe")\n'), 'CP原warning不是冻结14行')
    elif mode == 'cp-warning-locations':
        require(not cp_err and not actual.stderr, '正例stderr非空')
        old_roots = [Path(cp_status['source_root']), fixture]
        expected, source_locations = project_output(cp_out, old_roots)
        observed, _ = project_output(actual.stdout, old_roots)
        require(expected == observed, 'warning完整输出/精确行投影不符')
        return {'classification': 'semantic-match-pending-independent-pe', 'cp_locations': source_locations}
    elif mode == 'static-accepted-debt':
        require(not actual.stderr, 'Static旧能力stderr非空')
        old = (fixture.parents[1] / 'oracles/static_td.old.stdout').read_bytes()
        require(actual.stdout == old, 'Static默认或已明确rebound旧算法改变')
        return {'classification': 'accepted-static-debt', 'cp_equal': actual.stdout == cp_out,
                'metadata_claim': False, 'requires_actual_static_pe': True}
    elif mode == 'td-bounded-debt':
        require(not actual.stderr and not cp_err, 'TD捕获完整输入不应有stderr')
        expected = cp_out.decode().splitlines()
        observed = actual.stdout.decode().splitlines()
        require(len(expected) == len(observed), 'TD完整观察行数不符')
        boundary = []
        for a, b in zip(expected, observed):
            if a.startswith('fallback none-control '):
                # 旧输出必须由另一个角色真运行产生并经三方汇合，不可自动将NI算通过。
                boundary.append({'kind': 'fallback-none-three-way-required', 'cp': a, 'new': b})
                require(b == a or ('error NotImplementedError' in b and "['module-bool']" in b), 'None分支不是CP一致或精确一次真值NI')
            elif a.startswith('display class ') or a.startswith('display typing-alias '):
                boundary.append({'kind': 'nonstring-display-boundary', 'cp': a, 'new': b})
                require('error NotImplementedError' in b, '非str直接/alias显示没有明确NI')
            else:
                require(a == b, 'TD非边界行不符：' + a)
        return {'classification': 'requires-three-way-review', 'boundaries': boundary}
    else:
        require(actual.stdout == cp_out and actual.stderr == cp_err, 'CP与JIT完整流不符')
    return {'classification': 'semantic-match-pending-independent-pe'}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--repo', required=True, type=Path)
    p.add_argument('--input', required=True, type=Path)
    p.add_argument('--out', required=True, type=Path)
    p.add_argument('--role', required=True, choices=['cp', 'old', 'new'])
    p.add_argument('--tools', required=True, type=Path)
    p.add_argument('--oracle', type=Path)
    p.add_argument('--phase', required=True, choices=['cp', 'build', 'jit'])
    p.add_argument('--binding-version', choices=['legacy-names-v1', 'lexical-chain-v1'], default='legacy-names-v1')
    p.add_argument('--build-evidence', type=Path)
    p.add_argument('--pe-reviews', type=Path)
    args = p.parse_args()
    # 明确角色输入在开窗前由父冻结；不能凭当前PATH或相同文件名猜工具身份。
    tools = json.loads(args.tools.read_text())
    require(tools['role'] == args.role, '工具角色不符')
    require((args.role == 'cp') == (args.phase == 'cp'), 'CP角色与阶段不符')
    for name, row in tools['files'].items():
        require(digest(row['path']) == row['sha256'], '工具SHA不符：' + name)
    contract = json.loads((args.input / 'contract.json').read_text())
    for name, expected in contract['files'].items():
        require(digest(args.input / name) == expected, '输入改变：' + name)
    split = load_split(args.input, contract)
    args.out.mkdir(parents=True, exist_ok=False)
    # 复用原runner/环境构造；只在获准执行时导入，不在AST审查时导入仓库。
    sys.path.insert(0, str(args.repo / 'tests/aot/pyfiglet_ci'))
    sys.path.insert(1, str(args.repo / 'tests/aot'))
    from run import Recorder, base_environment
    from guards import reported_managed
    runner = Recorder(args.out / 'logs')
    dotnet = Path(tools['files']['dotnet']['path'])
    runtime = Path(tools['files']['runtime']['path'])
    env = base_environment(dotnet, runtime)
    env['HOME'] = str(args.out / 'home')
    env['TMPDIR'] = str(args.out / 'tmp')
    for name in ('home', 'tmp', 'cwd', 'results'):
        (args.out / name).mkdir()
    save(args.out / 'admitted-input.json', {'contract_sha256': digest(args.input / 'contract.json'), 'tools': tools})
    results = {}
    try:
        for case in cases(contract):
            name = case['id']
            source = args.input / case['entry']
            def cwd(label):
                d = args.out / 'cwd' / label
                d.mkdir()
                return d
            if args.role == 'cp':
                actual = runner.run(name, [tools['files']['cp']['path'], '-B', '-S', source],
                                    cwd=cwd(name), env=env, timeout=15)
                d = args.out / 'results' / name; d.mkdir()
                (d / 'stdout').write_bytes(actual.stdout); (d / 'stderr').write_bytes(actual.stderr)
                save(d / 'result.json', {'exit': actual.returncode, 'source_root': str(source.parent),
                                         'entry_sha256': digest(source)})
                require(actual.returncode == 0, 'CP输入失败，不可改源追首错：' + name)
                check_cp(split, name, actual)
                results[name] = {'classification': 'cp-observed-not-golden-before-review'}
                continue
            require(args.oracle is not None, '产品比较必须指定已冻结CP观察目录')
            cli = Path(tools['files']['cli']['path'])
            for arm in case['arms']:
                label = name + '-' + arm['id']
                if args.phase == 'build':
                    target = args.out / 'products' / label
                    command = [cli]
                    for module in instance_modules(case, arm):
                        command += ['--instance-module', module]
                    command += ['build', source, '--diagnostic-stages', '--out-dir', target]
                    for module in arm['include_modules']:
                        command += ['--include-module', module]
                    build = runner.run(label + '-build', command, cwd=cwd(label + '-build'), env=env, timeout=120)
                    if arm.get('reject_phase') == 'build':
                        result = rejection(split, arm, build, source.parent / 'subject.py')
                        require(not list(target.rglob('*.dll')), 'Legacy编译拒绝却留下可执行PE')
                        result.update(role=args.role, entry_sha256=digest(source),
                                      contract_sha256=digest(args.input / 'contract.json'),
                                      stderr_sha256=digest(args.out / 'logs' / (label + '-build.stderr')))
                        save(args.out / 'results' / (label + '.json'), result)
                        results[label] = result
                        continue
                    if build.returncode != 0:
                        results[label] = {'classification': 'compile-failed-unreviewed', 'exit': build.returncode}
                        raise RuntimeError('首次未核编译失败，停止该窗口：' + label)
                    dll = reported_managed(build.stderr, target)
                    graph = json.loads((target / 'module-environment.json').read_text())
                    require(sorted(row['module'] for row in graph['modules']) == sorted(set(case['graph'] + arm['include_modules'] + (case.get('discovered_modules', []) if args.role == 'new' else []))), '实际图改变')
                    for row in graph['modules']:
                        expected = 'InstanceGlobals' if row['module'] in instance_modules(case, arm) else 'LegacySingleton'
                        require(row['execution'] == expected, '真实模块执行模式不符')
                    require(digest(dll.parent / 'RyuPy.Runtime.dll') == tools['files']['runtime']['sha256'], '产物runtime角色错配')
                    product = {'pe': {'path': str(dll), 'sha256': digest(dll)},
                               'role': args.role, 'entry_sha256': digest(source),
                               'contract_sha256': digest(args.input / 'contract.json')}
                    # 只读ECMA解析先于任何JIT；完整producer审查是另一个明确前置，不以字段存在自签。
                    parsed = runner.run(label + '-pe-read', [sys.executable, args.input / 'ci/read_pe.py',
                                        dll, args.role, name, '--binding-version', args.binding_version], cwd=cwd(label + '-pe-read'), env=env, timeout=30)
                    require(parsed.returncode == 0, 'PE有限结构读取失败，保原件停止')
                    product['pe_read_sha256'] = digest(args.out / 'logs' / (label + '-pe-read.stdout'))
                    save(args.out / 'results' / (label + '.json'), product)
                    results[label] = {'classification': 'built-awaiting-independent-pe-review'}
                    continue
                require(args.build_evidence is not None and args.pe_reviews is not None, 'JIT阶段缺真实build/独立PE审查')
                product = json.loads((args.build_evidence / 'results' / (label + '.json')).read_text())
                if arm.get('reject_phase') == 'build':
                    require(product['role'] == args.role and product['entry_sha256'] == digest(source)
                            and product['contract_sha256'] == digest(args.input / 'contract.json')
                            and product['classification'] == 'expected-capability-rejection'
                            and product['phase'] == 'build' and product['exit'] == 1 and 'pe' not in product,
                            'Legacy负例缺实际编译拒绝证据')
                    log = args.build_evidence / 'logs' / (label + '-build.stderr')
                    require(digest(log) == product['stderr_sha256'], 'Legacy负例build原流漂移')
                    results[label] = dict(product, jit='not-applicable-compile-rejected')
                    save(args.out / 'results' / (label + '.json'), results[label])
                    continue
                dll = Path(product['pe']['path'])
                require(product['role'] == args.role and digest(dll) == product['pe']['sha256'] and
                        product['entry_sha256'] == digest(source) and product['contract_sha256'] == digest(args.input / 'contract.json'),
                        'JIT与冻结build非同一输入/角色/PE')
                review = json.loads((args.pe_reviews / (label + '.json')).read_text())
                require(review['admitted'] is True and review['pe_sha256'] == digest(dll) and review['role'] == args.role and
                        review['unit_link_mapping_verified'] and review['frame_carriers_verified'],
                        '缺独立真实unit/link/frame/PE准入，禁止JIT')
                actual = runner.run(label + '-jit', [dotnet, dll], cwd=cwd(label + '-jit'), env=env, timeout=20)
                results[label] = (rejection(split, arm, actual, source.parent / 'subject.py')
                                  if arm.get('reject_phase') == 'jit' else
                                  compare(case, None, actual, args.role, source.parent, args.oracle / name))
                results[label]['pe'] = {'path': str(dll), 'sha256': digest(dll)}
                save(args.out / 'results' / (label + '.json'), results[label])
        # X/Y是单独Session+托管宿主阶段，不隐式从此脚本补SDK或调用旧host未知产物。
        save(args.out / 'host-pending.json', {'required': True, 'contract': contract['host'], 'status': 'not-run-here'})
    finally:
        checks = {name: digest(args.input / name) == expected for name, expected in contract['files'].items()}
        tool_checks = {name: digest(row['path']) == row['sha256'] for name, row in tools['files'].items()}
        save(args.out / 'tools-after.json', tool_checks)
        save(args.out / 'unchanged.json', checks)
        save(args.out / 'result.json', {'role': args.role, 'case_results': results,
                                      'product_acceptance': False, 'host_and_pe_and_full_ci_required': True})
        require(all(checks.values()) and all(tool_checks.values()), '源码或工具执行前后改变')


if __name__ == '__main__':
    main()
