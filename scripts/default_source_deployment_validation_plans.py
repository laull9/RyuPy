"""默认来源图的严格审计：保留已验全图和旧选择，不用候选自报替代来源证据。"""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTRACT = ROOT / 'tests/aot/wcwidth_ci/project-source-plan-contract.json'
CONTRACT_SHA = '96346fdde59d6cb4fc8f6e7a0c9efb2089b5b6b4c15431763a12dc528167cb3d'
OLD = {'module', 'execution', 'reason', 'compiled', 'relative_from_import', 'ordinary_name_handlers'}
FLAGS = {'candidate', 'hard', 'catalog_authenticated'}
POLICY = 'ordinary-dynamic-module-pilot'


def require(ok, message):
    if not ok:
        raise RuntimeError(message)


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def baseline(expected, path=CONTRACT):
    """原JSON字节随合同保存；两项历史差异明确，不篡改历史hash补齐。"""
    raw = path.read_bytes()
    require(digest(raw) == CONTRACT_SHA, '完整图合同身份改变')
    contract = json.loads(raw)
    require(contract['schema_version'] == 1 and contract['source_run'] == 42932089715
            and contract['source_tree'] == '28c9f2fe18f27d050a7f7281357449c0f9f831d2'
            and contract['source_commit'] == 'c80d24b783183b588b9c51f5e622593df2fcb685'
            and contract['artifact_id'] == 13817435988
            and contract['zip_sha256'] == '134fd762e18f558c5e76928b65572362efd9460524a344e8820b84166a5fdae6',
            '完整图来源缺失或改变')
    rows = contract['plans']
    require(len(rows) == 20 and {r['id'] for r in rows} == set(expected['selected']), '完整图项目遗漏/重复')
    result, differences = {}, set()
    for row in rows:
        pid = row['id']
        require(row['member'] == f'project-ci-evidence/gate/{pid}/native/{pid}/module-environment.json'
                and digest(row['raw'].encode()) == row['sha256'], '原图成员或原字节改变：' + pid)
        matches = row['sha256'] == expected['source_plan_sha256'][pid]
        require(row['historical_sha256_matches'] is matches, '历史身份差异记账不符：' + pid)
        if not matches:
            differences.add(pid)
        plan = json.loads(row['raw'])
        validate_plan(plan, new=False)
        result[pid] = plan
    require(differences == {'click', 'pyfiglet'}, '18匹配/两历史差异改变')
    return result


def validate_plan(plan, new=True):
    require(set(plan) == {'compiled_feature', 'session_policy', 'modules'}
            and plan['compiled_feature'] is True and plan['session_policy'] == POLICY,
            '计划schema/实际编译feature/session_policy不符')
    rows = plan['modules']
    require(type(rows) is list, '模块集合不是list')
    names = set()
    for row in rows:
        keys = OLD | (FLAGS if new else set())
        require(set(row) in (keys, keys | {'resource_discovery'}), '模块字段缺失或未知')
        require(type(row['module']) is str and row['module'] not in names, '模块身份重复或无效')
        names.add(row['module'])
        for flag in {'compiled', 'relative_from_import', 'ordinary_name_handlers'} | (FLAGS if new else set()):
            require(type(row[flag]) is bool, '模块布尔字段类型不符：' + flag)
        require(row['execution'] in {'InstanceGlobals', 'LegacySingleton'} and type(row['reason']) is str,
                '模块ABI或原因类型不符')
    return {row['module']: row for row in rows}


def old_projection(row):
    return {key: value for key, value in row.items() if key not in FLAGS}


def audit_old_rows(actual, original, selected):
    """每个既有模块逐字段相等；旧Instance选择另行独立比较，不能靠candidate逃过。"""
    rows = validate_plan(actual)
    old_rows = validate_plan(original, new=False)
    for name, old in old_rows.items():
        require(name in rows and old_projection(rows[name]) == old, '既有全图字段改变：' + name)
        require({key: rows[name][key] for key in FLAGS}
                == {'candidate': False, 'hard': True, 'catalog_authenticated': False},
                '旧活动源码身份改变：' + name)
    chosen = [old_projection(rows[name]) for name in old_rows if rows[name]['execution'] == 'InstanceGlobals']
    require(sorted(chosen, key=lambda x: x['module']) == sorted(selected, key=lambda x: x['module']),
            '历史selected选择/编译状态改变')
    return {name: row for name, row in rows.items() if name not in old_rows}


def source_contract():
    raw = CONTRACT.read_bytes()
    require(digest(raw) == CONTRACT_SHA, '候选来源合同身份改变')
    return json.loads(raw)['candidate_sources']


def candidate_rows(name, compiled=True):
    """具名增量来自锁源角色及selector合同，不读取本轮失败结果造期待。"""
    sources = source_contract()
    names = set(sources) if name == 'markdown' else ({'html', 'html.entities'} if name == 'pyparsing'
                                                    else {'html', 'html.entities', 'html.parser', '_markupbase'})
    result = {}
    for module in names:
        instance = module == 'html.parser'
        reason = {'html.parser': 'compiler-catalog-authenticated-source',
                  'html': 'entry-or-package', 'html.entities': 'hir-boundary:NoFunction',
                  'markdown.extensions.legacy_attrs': 'hir-boundary:ClassDefinition'}.get(
                      module, 'hir-boundary:InstanceSyntaxBoundary')
        hard = name in {'parser', 'pyparsing'} or module in {'html', 'html.entities'}
        result[module] = dict(module=module, execution='InstanceGlobals' if instance else 'LegacySingleton',
            reason=reason, compiled=compiled, relative_from_import=instance, ordinary_name_handlers=instance,
            candidate=True, hard=hard, catalog_authenticated=sources[module]['catalog'])
    return result


def audit_candidates(rows, name, compiled=True):
    wanted = candidate_rows(name, compiled)
    require(rows == wanted, '具名候选集合/ABI/能力/强度/编译状态不符：' + name)
    return wanted


def audit_deployment(output, rows, name, markdown_root=None):
    """成功必须有完整来源交叉证据；编译计划、落盘字节均不冒称激活执行。"""
    report = json.loads((output / 'optional-sources.json').read_text())
    require(set(report) == {'schema_version', 'roots', 'dependencies', 'locations', 'deferred',
                           'outside', 'protected', 'diagnostics', 'metadata_unavailable'}
            and report['schema_version'] == 1, 'optional报告schema改变')
    sources = source_contract()
    roots = set(rows) - {'html', '_markupbase'}
    if name == 'parser': roots = {'html.parser'}
    # 完整metadata保留未选console_scripts；只记录Outside，不授予候选来源/loader。
    outside = ['markdown.__main__'] if name == 'markdown' else []
    require(report['roots'] == sorted(roots) and report['outside'] == outside
            and report['deferred'] == {} and report['diagnostics'] == {}
            and report['metadata_unavailable'] is None, '候选根/延期/metadata状态不符')
    require(report['protected'] == ['json', 'zipfile', 'optparse', 'getopt', 're', 'dataclasses',
                                   'unittest', 'enum', 'io', 'typing', 'xml.etree.ElementTree'],
            '原生保护集合改变')
    require(report['dependencies'] == {n: sources[n]['dependencies'] for n in rows}, '依赖图改变')
    require(set(report['locations']) == set(rows), '来源位置集合改变')
    # 编译器族目录是四固定文件顺序、摘要及license摘要的联合身份。
    family = b''
    for module in ('_markupbase', 'html', 'html.entities', 'html.parser'):
        src = sources[module]; family += src['relative'].encode() + b'\0' + src['sha256'].encode()
    family += b'3b2f81fe21d181c499c59a256c8e1968455d6689d269aa85373bfb6af41da3bf'
    catalog_root = output.resolve() / 'compiler-sources' / digest(family)
    verified = {}
    for module in rows:
        src = sources[module]
        root = catalog_root if src['catalog'] else markdown_root
        require(root is not None, '普通候选缺明确项目根')
        path = root.resolve() / src['relative']
        require(path.is_absolute() and path.resolve() == path and path.is_file()
                and not path.is_symlink() and digest(path.read_bytes()) == src['sha256'],
                'SourceAbsolute实际路径/固定源码摘要不符：' + module)
        require(report['locations'][module] == {'package': src['package'], 'file': src['file'],
                    'path': str(path), 'is_package': src['is_package']}, 'SourceAbsolute位置/种类不符：' + module)
        verified[module] = {'path': str(path), 'sha256': src['sha256']}
    return {'level': 'deployment-verified-not-activation', 'sources': verified}


def evidence_layer(result, pid, matrix):
    """仅完整同轮runner结果证明的HasErrors进入计划层，缺文件不是豁免依据。"""
    row = next((r for r in matrix if r['id'] == pid), None)
    require(row is not None, '缺同轮结果：' + pid)
    runner = json.loads((result / pid / 'runner.json').read_text())
    require(runner['exit'] == 0 and runner['failure'] is None and runner['raw_streams_complete'] is True,
            'runner非正常完成')
    actual_rows = [json.loads(line) for line in (result / pid / 'native/projects.jsonl').read_text().splitlines()]
    require(len(actual_rows) == 1 and all(actual_rows[0][k] == row[k]
            for k in ('id', 'status', 'failure_kind', 'run_id', 'mode', 'run_exit', 'detail')), '实际runner结果不符')
    # matrix逐文件身份由完整门产生，此处再核其列明的原流，拒绝后来改写诊断。
    required = {f'{pid}/runner.json', f'{pid}/native/projects.jsonl'}
    if row['status'] == 'unsupported':
        required |= {f'{pid}/native/{pid}/compile.error.txt', f'{pid}/native/{pid}/compile.diagnostics.txt'}
    require(required <= set(row['evidence']), '缺必须原流身份')
    for relative, sha in row['evidence'].items():
        require(not Path(relative).is_absolute() and '..' not in Path(relative).parts, '非法证据路径')
        require(digest((result / relative).read_bytes()) == sha, '项目原流身份改变')
    output = result / pid / 'native' / pid
    if row['status'] == 'native':
        require(row['failure_kind'] == '' and row['run_exit'] == 0, 'native状态矛盾')
        return 'deployment'
    known = {'six', 'attrs', 'click', 'toml', 'networkx', 'pyparsing', 'python-dateutil', 'sortedcontainers'}
    require(pid in known and row['status'] == 'unsupported' and row['failure_kind'] == 'diagnostic'
            and row['run_exit'] is None, '非已知正常未支持，禁止计划层豁免')
    error = (output / 'compile.error.txt').read_text()
    diagnostic = (output / 'compile.diagnostics.txt').read_text()
    require(error == '编译失败，存在错误' and '[E' in diagnostic and '] Error:' in diagnostic
            and re.search(r'\.py:\d+:\d+', diagnostic) is not None
            and row['detail'] in diagnostic and 'internal error' not in diagnostic.lower(),
            'HasErrors与真实源码诊断证据不符')
    return 'planned-only'


def audit_product_plan(output, plan, inputs, name):
    if name == 'markdown':
        expected = json.loads((ROOT / 'tests/aot/wcwidth_ci/project-pilot-plans.json').read_text())
        original = baseline(expected)['markdown']
        additions = audit_old_rows(plan, original, expected['selected']['markdown'])
    else:
        rows = validate_plan(plan)
        entry = dict(module='__main__', execution='LegacySingleton', reason='entry-or-package',
                     compiled=True, relative_from_import=False, ordinary_name_handlers=False,
                     candidate=False, hard=True, catalog_authenticated=False)
        require(rows.pop('__main__', None) == entry, '独立parser入口计划改变')
        additions = rows
    audit_candidates(additions, name)
    return audit_deployment(output, additions, name, inputs / 'markdown')
