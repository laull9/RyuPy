"""新位置普通class工厂有限合同：未冻结即硬停，CP错误与能力拒绝不混计。"""
import collections
import hashlib
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET
import textwrap_dedent_contract as legacy

ROOT, old, metadata = legacy.ROOT, legacy.old, legacy.metadata
require, sha, save = legacy.require, legacy.sha, legacy.save
IdentityError, isolated = legacy.IdentityError, legacy.isolated
TITLE = 'validation: lexical-class-factory-001'
MANIFEST = ROOT / 'tests/validation/lexical_class_factory.json'
parse, relative, complete = legacy.parse, legacy.relative, legacy.complete


# 固定旧74键；ABI-RECHECK003仅批准3个检查器hash更新，旧71身份不变；不锁新生产实现。
PROTECTED = {'runtime/RyuPy.Runtime.Tests/PyTraceStateArtifactTests.cs': '46e66ef1bdd0cf491c70f1996b6c6d08bb029609702359512e9d67e3d04e651d',
    'runtime/RyuPy.Runtime.Tests/PyTraceStateTreeBuilderMutationTests.cs': 'd216e71cb8da2f26b39833408e4bfce910c91d1373e99adf337960ef98272bc0',
    'runtime/RyuPy.Runtime.Tests/PyTraceStateV1ArtifactContract.cs': '604ec5fb9641807f8fd48f1bd3e2210b8be77bd5996c867e8aadad3463cbf6c9',
    'runtime/RyuPy.Runtime.Tests/PyTraceStateV1ArtifactContractTests.cs': 'd474e7e18cc33163aefe6866711df84bf91fda4eeb4b1d1ec3676adb7ee3acd8',
    'scripts/default_source_deployment_validation_plans.py': '49751b7c3ef2c17d260edc359e030fb93b8a4dd31b2e6793bba2258343882639',
    'tests/aot/wcwidth_ci/full_check.py': 'b8c1d53eecdb3dd0aee3859bb551e19a5ddf103452f417c92d445845aade4018',
    'tests/aot/wcwidth_ci/project-pilot-plans.json': '84dfede68ca23bc33a55a1dd763cbf5cca696a731d66f4a34ca84f8c569a6486',
    'tests/aot/wcwidth_ci/project-source-plan-contract.json': '96346fdde59d6cb4fc8f6e7a0c9efb2089b5b6b4c15431763a12dc528167cb3d',
    'tests/aot/wcwidth_ci/project_gate.py': 'e38a5f45a4fa826e9fd71f4f578160f196357b86381cfb5d7ec904cb5a5ba8b9',
    'tests/aot/wcwidth_ci/trace_state_s1.py': '5d409552e39b77393c18d7729fe5a09354b6449359531db5e692d088de771af4',
    'tests/pinned-python.txt': 'aa0d6581054e6e4ff3f91839deca7a854ad37221b8784d060b42d0f847ff1a3b',
    'tests/projects/baselines/attrs/meta.txt': '8f432221a4adda373fb29ebd4fc7bbc41dbae6500a653bfe10e804c6b05cbfe1',
    'tests/projects/baselines/attrs/stdout.txt': '449b98a8ed008395e15be3486f162c5539ccf59e742e3ba00470bc55c49055f8',
    'tests/projects/baselines/binaryornot/meta.txt': '62ef1bcec3a6279ce24e8b9b657619eb516622cd4a02c7e98645e52df7a145db',
    'tests/projects/baselines/binaryornot/stdout.txt': '6bb666d23e647f130cc672857ec2159870a2d1998551d96fd46d21ff81ddb651',
    'tests/projects/baselines/chardet/meta.txt': '35615620a26fec1e1fb6c9dbb7ac39eea1c56b6ff4bd35a74b1eda61b25b06bd',
    'tests/projects/baselines/chardet/stdout.txt': '2ac46b754effd0e2746634a311e925ad16797d643f1823ca31c39eb41df0d059',
    'tests/projects/baselines/click/meta.txt': '47b780b3a0d1e61754b64d23083b83c13df699af637ada8b493e30b469482d13',
    'tests/projects/baselines/click/stdout.txt': 'dce030fec84918a0e07d8e99e89faf7fceb1961f46974095fe3630869ff9332e',
    'tests/projects/baselines/colorama/meta.txt': '56b65fd7e83336a0ddc4b6b7d4af8b476504d305f082e008e416731c2cb93969',
    'tests/projects/baselines/colorama/stdout.txt': '1df534d4f8fba932c800bffc88be40a1beda505c5d69a9f552f241747719ecf5',
    'tests/projects/baselines/docopt/meta.txt': 'b7d4887cc31ccc593449ee8692228f0848b64138f4ec615d045d1ac38a9a92aa',
    'tests/projects/baselines/docopt/stdout.txt': '04b5ae134f7bd7fc7d01cd651c2cef60c887fc130dcc5518e90bf63eb414f6bb',
    'tests/projects/baselines/markdown/meta.txt': '50ea54b0651c44200e225d52f88a5fe888c7242d7755d781173346d88e3f5315',
    'tests/projects/baselines/markdown/stdout.txt': 'a824d6deda7b3f0a3f627045ea4046baa2ac6e49ec62e22ea4b367ae759923cf',
    'tests/projects/baselines/networkx/meta.txt': 'e131373ad0b881a244348b8ffd40692d1ba5d192ef2c343e8995c1d1dec279a6',
    'tests/projects/baselines/networkx/stdout.txt': '545e92bed156bbaf5ee36d9477441c7c734d03fc6f44017d5f642e878a5961b6',
    'tests/projects/baselines/progress/meta.txt': 'e8df7e8b8eceb9627cdff7e6587be20ac23057064541e2e51c38a234eac6c31b',
    'tests/projects/baselines/progress/stdout.txt': 'd3d49079a3edb1f88dab46bcca3586a1ac9bb41adcafe6a12dceb3e4e4d917e9',
    'tests/projects/baselines/pyfiglet/meta.txt': '1aba4d17c1f4386248f3bca7c5227ffaf90389ac7039967a860ac6b2dd10ddd0',
    'tests/projects/baselines/pyfiglet/stdout.txt': '0334adb06d9ec5d7fb71b2c3a2b7609bcd2a67ab320b3f0995a85d770ec2ecd8',
    'tests/projects/baselines/pyparsing/meta.txt': '41046cd366e38fbfc0eb593d5b044dffd5fd5bd17cd645ffbfc4da4ccac2622a',
    'tests/projects/baselines/pyparsing/stdout.txt': 'd94bf854fce6ba5eabd6d0b0fcf41bf71deded180dee90604beb4da001e39d8e',
    'tests/projects/baselines/python-dateutil/meta.txt': '4909c235755025e6649848c235277314965aa7304ea90e8a1090a6a7ad5169cd',
    'tests/projects/baselines/python-dateutil/stdout.txt': '91f743ebf7c36b62e0c6f56f7bbe1d7eb3fc7118db292eada9a406d6c5083d6c',
    'tests/projects/baselines/six/meta.txt': '540f20e2fb01627cf06d617edc3d86fc08d4f06c5554ebf2c0017d64da81dd0d',
    'tests/projects/baselines/six/stdout.txt': 'eea0d3f3f438e8f3a0f42f9a9ad48c4a9fdf70afa0e2bf9be10e017676708227',
    'tests/projects/baselines/sortedcontainers/meta.txt': 'c047ce9bcf7294405672f8394bb51e650336eed822ebf467a1b07a95d5e0eabf',
    'tests/projects/baselines/sortedcontainers/stdout.txt': 'd2d4fb2e49d5416a3500ed16822ffe3fd7b14f1736ee9d60a6745dcf3be85f45',
    'tests/projects/baselines/tabulate/meta.txt': 'f5234f2742117e44fdb77da39bb8eb2d838caab3bf8065bd98ed5fdd8ee675fc',
    'tests/projects/baselines/tabulate/stdout.txt': '95db25d24eea531e43aa75dcb084c7422afa0364d4e46ad15c45de813d45cf95',
    'tests/projects/baselines/termcolor/meta.txt': 'c7ebc87741fb44d07a8fb995f0ec5f95acac5f8047711d2803420c8bbcf2c083',
    'tests/projects/baselines/termcolor/stdout.txt': '55be439195979f8df4e28deb7495757e2d26bb6dd9d9935273055f65d9f13cd6',
    'tests/projects/baselines/texttable/meta.txt': 'e16437ae413d07625c6b19154867a6de675e025d94ec7d51264fc3831f8690a2',
    'tests/projects/baselines/texttable/stdout.txt': '5d992141feb46ad90b254a2e6670da578d2f3ddee1c08ec4c170bb3200426634',
    'tests/projects/baselines/toml/meta.txt': '2052dd1fcf85b1ba0681a7d65847640ebfbc84ba028f65fb2b0d6461b0385c09',
    'tests/projects/baselines/toml/stdout.txt': '669cf37f535b7d7d8dff8e2efd4637ec07d8907ee01a8807e690fbf134522df1',
    'tests/projects/baselines/tomli/meta.txt': 'b80e5243b036f14552e31bc27bb98bc313b80c879ef0737d56fd8317fd0a4ae8',
    'tests/projects/baselines/tomli/stdout.txt': 'd4a68402aa675c8b3d0f839bac01c87346f9c01baf49c2a27ce9e54f27b1b3bd',
    'tests/projects/baselines/wcwidth/meta.txt': '63104ea458e1d02e5b894f8891b6e506cf4878f55aac486ab11bdb0fe4483963',
    'tests/projects/baselines/wcwidth/stdout.txt': 'c29e28be95be47b3502159b1369abf04f0da3a460af33f92358a941444e1ff6e',
    'tests/projects/entries/attrs_sample.py': '60d381f0618f73918588f70355992688335a039b2561afbe505f7902ef9411d5',
    'tests/projects/entries/binaryornot_sample.py': '2038e69256ca8d95152a5ec0fded79f97905bb80d806b274f40da9d11652da02',
    'tests/projects/entries/chardet_sample.py': '7cf5db1e477193f1890872cb1809ea8e503e0a6d36ae1392603f4f9940bfdebd',
    'tests/projects/entries/click_sample.py': '24188f49ee6c51fc3fec0e9e0eb295ba5163005fcb9c46f61d9df6bea1667eb9',
    'tests/projects/entries/colorama_sample.py': '5f815693704a858086d99701bc03b801f5f972272a3ca42ae131c99ea2553b13',
    'tests/projects/entries/dateutil_sample.py': '993a1294c5039724127b9587d1006550b451f1052c62ab6914d99e349a37afa5',
    'tests/projects/entries/docopt_sample.py': 'c6b5acfc37ef0cf42ea6f6070602c2c42d95a7c748f6962825b47965e83640c3',
    'tests/projects/entries/markdown_sample.py': '78a03254033372f140726edaa44e1b9b70238c591b28403cdd4fdf30a45c0777',
    'tests/projects/entries/networkx_sample.py': 'c3f37c255021e8236cbec71132ce2b09bcc2899c7cf18e5f52a91762271eb658',
    'tests/projects/entries/progress_sample.py': 'a383879547ddd560cdf00a63d0ac103da2932aefa3da4ce1ab8996ea5b5833a8',
    'tests/projects/entries/pyfiglet_sample.py': '8cea52bf78c0c504dc2b7b5638c709d906ab240469c8d991d357828dd5baa2e5',
    'tests/projects/entries/pyparsing_sample.py': 'a8a097e28016d41efe5aca02b8a62615dee5da648b3f7a06119363c9ea121584',
    'tests/projects/entries/six_sample.py': 'dd4a8d034ad6d74d78e1bf3f96a57f8d30ccced78c6b7ff2b33ac91d09968b4a',
    'tests/projects/entries/sortedcontainers_sample.py': '80e97651fc30814964599269ca202a626b6ed9d923d79de1c0e930f96138491a',
    'tests/projects/entries/tabulate_sample.py': '00977b83256c2f95d931ac1699dd4f40bce89ad74eab89123e30092b0af378d0',
    'tests/projects/entries/termcolor_sample.py': 'd6623fef2f9cb4b48343589a1757410a61fb6a28d7e72dcefdc06fab0bdf6b67',
    'tests/projects/entries/texttable_sample.py': 'cde08ba76684fbb69c8ef78e743f8a1d6653fc6f586f15d18d988c8be791661f',
    'tests/projects/entries/toml_sample.py': 'e43b25b4a42ab0f398fd3cb6d8250c8c2427a04e22ec5eef466d786b5f9aebb5',
    'tests/projects/entries/tomli_sample.py': 'b3c71a8cd016775969afabb64aa9763c094cd37f9a7a575e6afa1d29f808d890',
    'tests/projects/entries/wcwidth_sample.py': '2c537d4bd16d4c33316a28ac74155917ee986b91ab90060bee2875b3ac0c7a55',
    'tests/projects/lock.tsv': '108a09f5f7d506e2dc4cb5157dc51b0c3e1891334ebfaa20ae0ad9800fe34f78',
    'tests/projects/manifest.tsv': '29fc8ebb2744d366134e0ad527a206b6f76e0533f7bc037879b0955060814adb',
    'tests/projects/protected.json': 'a45e4f70d7dc18323a6866dd448c9794d67c72aa963a9c88096fd4a80b800686'}
MULTI_ROOT = 'tests/cases/lexical_classes/module_owner'
FIXTURE_GLOB = 'tests/e2e/phase3/m3_11_lexical_class*.py'
SIDECAR_GLOB = 'tests/e2e/phase3/m3_11_lexical_class*.case.txt'
SIDECAR_STEMS = {'m3_11_lexical_class_body_error', 'm3_11_lexical_class_legacy_outer_cell_ni'}
PRIVATE_PENDING = 'tests/e2e/phase3/m3_11_lexical_class_namespace.py'
PENDING_REASON = 'private-name-mangling-not-supported'


def check_context(env, event):
    require(env.get('RUNNER_ENVIRONMENT') == 'github-hosted' and env.get('RUNNER_OS') == 'Linux'
            and env.get('RUNNER_ARCH') == 'X64', '仅hosted Linux x64 runner')
    require(env.get('GITHUB_REPOSITORY') == 'laull9/RyuPy'
            and event.get('repository', {}).get('full_name') == 'laull9/RyuPy', '仓库身份不符')
    require(event.get('head_commit', {}).get('message') == TITLE, 'class工厂标题必须精确相等')
    adjusted = dict(event, head_commit=dict(event.get('head_commit', {}), message=old.shared.TITLE))
    old.shared.check_context(env, adjusted)


def expectation(value, refusal=False):
    require(type(value) is dict and set(value) == {'expected_exit', 'stdout', 'stderr', 'diagnostic'}, '输出合同字段不完整')
    require(type(value['expected_exit']) is int and value['expected_exit'] in (0, 1), '只接受明确exit0或expectedexit1')
    require(all(type(value[k]) is str for k in ('stdout', 'diagnostic'))
            and (type(value['stderr']) is str or (refusal and value['stderr'] is None)), '未知stderr只允许明确NI合同')
    if value['expected_exit'] == 1:
        require(value['diagnostic'] and (value['stderr'] is None or value['diagnostic'] in value['stderr']), 'expectedexit1必须有准确诊断')
    else:
        require(value['stderr'] == value['diagnostic'] == '', '正常成功不允许吞异常')


def validate(data, root=ROOT, ready=True):
    require(type(data) is dict and set(data) == {'schema_version', 'title', 'frozen', 'default_project_delta',
        'cp_version', 'sdk_version', 'oracle', 'sources', 'cases', 'facts', 'rust_tests', 'pending_only', 'old_protected'}, '清单字段缺失/越界')
    require(data['schema_version'] == 1 and data['title'] == TITLE and data['default_project_delta'] == 0,
            '清单版本/标题/分子错误')
    require(data['cp_version'] == '3.12.13' and data['sdk_version'] == '10.0.403', '工具合同改变')
    require(type(data['frozen']) is bool, 'frozen类型错误')
    if ready: require(data['frozen'], 'writer清单未冻结：禁止prepare/CP/构建/产品')
    if data['frozen']:
        require(data['old_protected'] == PROTECTED, '固定旧20/Markdown/ABI保护完整键或哈希改变')
        actual = {str(p.relative_to(root)) for p in root.glob(FIXTURE_GLOB)}
        actual.update(str(p.relative_to(root)) for p in root.glob(SIDECAR_GLOB))
        actual.update(str(p.relative_to(root)) for p in (root / MULTI_ROOT).rglob('*') if not p.is_dir() or p.is_symlink())
        require(actual and set(data['sources']) == actual, 'class fixture完整源域有漏项/增项')
        require({row.get('kind') for row in data['cases']} == {'positive', 'cp_error', 'ni'}, '冻结必须齐备positive/CPerror/NI三类')
    for key in ('sources', 'old_protected'):
        require(type(data[key]) is dict, '来源清单必须是映射')
        for name, digest in data[key].items():
            path = root / relative(name)
            require(type(digest) is str and re.fullmatch('[0-9a-f]{64}', digest), '缺真实SHA：' + name)
            require(path.is_file() and path.resolve() == path.absolute() and not path.is_symlink()
                    and sha(path) == digest, '输入身份改变：' + name)
    for key in ('cases', 'facts', 'rust_tests', 'pending_only'):
        require(type(data[key]) is list, key + '必须是list')
    if data['frozen']:
        require(data['sources'] and data['cases'] and data['facts'] and data['rust_tests'] and data['old_protected'], '冻结清单不可为空')
    require(all(type(x) is str and x for x in data['facts']) and len(set(data['facts'])) == len(data['facts']), 'Fact重复/空')
    ids, used = [], set()
    for case in data['cases']:
        require(type(case) is dict and set(case) == {'id', 'source', 'kind', 'product_stage', 'cp', 'product',
            'include', 'modules', 'support', 'instance_modules', 'native', 'native_refusal'}, 'case字段缺失/越界')
        require(re.fullmatch('[a-z][a-z0-9_-]*', case['id']), 'case id非法')
        ids.append(case['id'])
        require(case['kind'] in ('positive', 'cp_error', 'ni'), '未知语义分类')
        require(case['product_stage'] in ('compile', 'run'), '未知拒绝阶段')
        expectation(case['cp']); expectation(case['product'], refusal=case['kind'] == 'ni')
        require(case['kind'] == 'ni' or case['cp']['expected_exit'] == (1 if case['kind'] == 'cp_error' else 0), 'CP语义类别矛盾')
        require(case['product']['expected_exit'] == (0 if case['kind'] == 'positive' else 1), 'NI/CPerror必须明确exit1')
        require(case['kind'] != 'positive' or case['product_stage'] == 'run', '正例须真实运行')
        require(case['kind'] != 'positive' or case['cp'] == case['product'], '正例须同源完整CP差分')
        require(case['kind'] != 'cp_error' or (case['product_stage'] == 'run'
                and case['cp'] == case['product']), 'CP运行错误必须真实run并保副作用；静态能力拒绝应归NI')
        require(type(case['include']) is list and len(set(case['include'])) == len(case['include']), 'include重复')
        require(set(case['modules']) == {'__main__', *case['include']}
                and case['modules']['__main__'] == case['source'], '真实模块图缺失')
        require(all(p in data['sources'] for p in case['modules'].values()), '存在未冻结编译源')
        for name in case['include']:
            require(re.fullmatch(r'[A-Za-z_]\w*', name) and
                    case['modules'][name] == str(Path(case['source']).parent / (name + '.py')), 'include不在同根')
        support = ([MULTI_ROOT + '/case.txt'] if case['source'] == MULTI_ROOT + '/entry.py' else
                   [str(Path(case['source']).with_suffix('.case.txt'))] if Path(case['source']).stem in SIDECAR_STEMS else [])
        require(case['support'] == support, 'support合同集合不符')
        require(all(p in data['sources'] for p in case['support']), 'case.txt未冻结')
        require(case['instance_modules'] == (['owner'] if case['source'] == MULTI_ROOT + '/entry.py' else []), '实例owner必须显式且只限真实跨模块case')
        used.update(case['modules'].values()); used.update(case['support'])
        require(case['native'] in ('none', 'positive', 'refusal'), 'Native计划非法')
        require(case['native'] != 'positive' or case['kind'] == 'positive', 'Native正门必须CP正例')
        if case['native_refusal'] is not None:
            expectation(case['native_refusal'], refusal=True)
            require(case['native_refusal']['expected_exit'] == 1, 'Native能力拒绝只exit1')
        require(case['native'] != 'refusal' or case['native_refusal'] is not None, '负门缺具体诊断')
    require(len(ids) == len(set(ids)), '重复case')
    pending = []
    for item in data['pending_only']:
        require(type(item) is dict and set(item) == {'source', 'reason', 'cp'}, 'pending必须逐项记录source与固定原因')
        path = item['source']; pending.append(path)
        expectation(item['cp']); require(item['cp']['expected_exit'] == 0, '私名原正目标CP必须通过')
        require(item['reason'] == PENDING_REASON and path in data['sources'] and path not in used
                and b'# ryupy: pending' in (root / path).read_bytes(), '私名原正例必须保pending，不当NI通过')
    require(len(pending) == len(set(pending)), 'pending源重复')
    if data['frozen']: require(PRIVATE_PENDING in pending, '原私名CP正目标必须单列pending')
    require(used | set(pending) == set(data['sources']), '来源有未记账成员')
    native = collections.Counter(x['native'] for x in data['cases'])
    require(native['positive'] <= 1 and native['refusal'] <= 1, 'Native最多一个主正例+一个必要负例')
    if data['frozen']: require(native['positive'] == 1, '必须有一个主positive Native尝试')
    for row in data['rust_tests']:
        require(set(row) == {'package', 'name'} and re.fullmatch('ryupy-[a-z-]+', row['package'])
                and re.fullmatch('[A-Za-z0-9_:]+', row['name']), 'Rust精确测试名无效')
    if data['frozen']:
        require(data['oracle']['python_sha256'] == old.shared.PYTHON_SHA and data['oracle']['version'] == '3.12.13', 'oracle工具身份不符')
        require(set(data['oracle']['records']) == {x['source'] for x in data['cases']} | set(pending), 'oracle入口全集不符')
        for case in [*data['cases'], *data['pending_only']]:
            cp = case['cp']; row = data['oracle']['records'][case['source']]
            require(row['source_sha256'] == data['sources'][case['source']] and row['exit'] == cp['expected_exit'], 'oracle源/退出身份不符')
            require(row['stdout']['bytes'] == len(cp['stdout'].encode())
                    and row['stdout']['sha256'] == hashlib.sha256(cp['stdout'].encode()).hexdigest()
                    and row['normalized_stderr_sha256'] == hashlib.sha256(cp['stderr'].encode()).hexdigest(), 'oracle完整输出哈希不符')
            require(type(row['stderr']['bytes']) is int and row['stderr']['bytes'] >= 0
                    and re.fullmatch('[0-9a-f]{64}', row['stderr']['sha256']), 'oracle原stderr证据缺失')
        verify_test_names(data, root)
    return data


def load_manifest(ready=True):
    return validate(parse(MANIFEST.read_bytes()), ready=ready)


def normalize_trace(raw, paths):
    """仅trace段内的完整File头映射；异常消息/源码正文/未核路径原字节保留。"""
    patterns = [(re.compile(rb'^(  File ")' + re.escape(str(absolute).encode())
                            + rb'(", line [1-9][0-9]*, in [^\r\n]+)(\r?\n)?$'),
                 ('<source:' + logical + '>').encode()) for logical, absolute in paths.items()]
    result, in_trace = [], False
    for line in raw.splitlines(keepends=True):
        if line.rstrip(b'\r\n') == b'Traceback (most recent call last):':
            in_trace = True
        elif in_trace and line and not line.startswith((b' ', b'\t', b'\r', b'\n')):
            in_trace = False
        if in_trace:
            for pattern, replacement in patterns:
                match = pattern.fullmatch(line)
                if match:
                    line = match[1] + replacement + match[2] + (match[3] or b'')
                    break
        result.append(line)
    return b''.join(result)


def reject_native_success_products(target, stem):
    """静态拒绝不得留成功入口PE或dist；调用者必须先归档原件再检查。"""
    candidates = list(target.rglob(stem + '.dll')) if target.exists() else []
    distributions = [p for p in target.rglob('*.dist') if p.is_dir() or p.is_symlink()] if target.exists() else []
    require(not candidates and not distributions, '静态Native拒绝仍产出入口PE/dist，原件已保留')


def verify_output(record, prefix, expected, paths=None, refusal=False):
    expectation(expected, refusal)
    require(record.get('exit') == expected['expected_exit'] and record.get('failure') is None
            and record.get('raw_streams_complete') is True, '退出/信号/超时/截流不符合合同')
    require(prefix.with_suffix('.stdout').read_bytes() == expected['stdout'].encode(), 'stdout副作用不等')
    stderr = prefix.with_suffix('.stderr').read_bytes()
    if refusal:
        text = stderr.decode('utf-8', errors='strict'); diagnostic = expected['diagnostic']
        require(not re.search(r'panicked at|Unhandled exception|Segmentation fault|core dumped|SIGSEGV|SIGABRT', text), 'NI不得吞crash')
        if diagnostic.startswith('E0100 '):
            require(re.findall(r'\bE\d{4}\b', text) == ['E0100'] and
                    any('E0100' in line and line.rstrip().endswith(diagnostic[6:]) for line in text.splitlines()), '静态NI代码/具体消息不符')
        else:
            require(text.splitlines() and text.splitlines()[-1] == diagnostic, '运行NI最后异常行不符')
        return {'expected_exit': 1, 'cp_equivalent': False, 'expected_capability_refusal': True}
    require(normalize_trace(stderr, paths or {}) == expected['stderr'].encode(), '完整stderr不等；不删帧匹配')
    return {'expected_exit': expected['expected_exit']}


def compare_cp_error(record, prefix, expected, paths):
    require(record['exit'] == 1 and record['failure'] is None and record['raw_streams_complete'] is True,
            'CPerror退出/超时/信号不符')
    require(prefix.with_suffix('.stdout').read_bytes() == expected['stdout'].encode(), 'CPerror副作用不等')
    actual = normalize_trace(prefix.with_suffix('.stderr').read_bytes(), paths)
    return {'aligned': actual == expected['stderr'].encode(), 'normalization': 'exact-known-source-path-only',
            'expected': expected['stderr'], 'actual': actual.decode('utf-8', errors='replace')}


def verify_test_names(data, root):
    facts = []
    for name in ('PyLexicalClassFactoryTests', 'PyLexicalClassMutationTests', 'PyLexicalClassAbiContractTests'):
        text = (root / ('runtime/RyuPy.Runtime.Tests/' + name + '.cs')).read_text()
        facts += ['RyuPy.Runtime.Tests.' + name + '.' + fn for fn in re.findall(r'\[Fact\]\s+public void (\w+)\(', text)]
    require(len(facts) == 16 and collections.Counter(facts) == collections.Counter(data['facts']), '原12+新4个C#实际Fact全名集合不符')
    require(len(data['rust_tests']) == 20 and len({(r['package'], r['name']) for r in data['rust_tests']}) == 20, 'candidate003的20个Rust精确测试必须完整且唯一')
    discovered = set()
    for row in data['rust_tests']:
        module, function = row['name'].split('::')
        base = root / 'crates' / row['package'] / 'src'
        require(re.search(r'\bmod ' + re.escape(module) + r'\s*;', (base / 'lib.rs').read_text()), 'Rust测试模块未接lib')
        text = (base / (module + '.rs')).read_text()
        require(re.search(r'#\[test\]\s*fn ' + re.escape(function) + r'\(', text), 'Rust精确测试函数不存在/未标test')
        discovered.update((row['package'], module + '::' + fn) for fn in re.findall(r'#\[test\]\s*fn (\w+)\(', text))
    require(discovered == {(r['package'], r['name']) for r in data['rust_tests']}, '专用Rust模块实际测试集合有遗漏/越界')


def verify_trx(path, facts):
    rows = ET.parse(path).findall('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult')
    require(rows and all(r.get('outcome') == 'Passed' for r in rows), '零测试/失败/skip')
    require(collections.Counter(r.attrib['testName'] for r in rows) == collections.Counter(facts), 'Fact集合漏项/重复/越界')
    return {'total': len(rows), 'skipped': 0}


def build_argv(runner, case, target, aot=False):
    argv = legacy.build_argv(runner, case, target, aot)
    for name in case['instance_modules']: argv += ['--instance-module', name]
    return argv


def verify_plan(path, case, aot=False):
    plan = legacy.verify_plan(path, case, aot)
    require(all(type(row.get('compiled')) is bool and row['execution'] in ('InstanceGlobals', 'LegacySingleton')
                and type(row.get('reason')) is str for row in plan['modules']), '实际模块执行计划非法')
    rows = {row['module']: row for row in plan['modules']}
    for name in case['instance_modules']:
        require(rows[name]['execution'] == 'InstanceGlobals' and rows[name]['reason'] == 'explicit-instance', '真实owner没有用显式实例后端')
    return plan


def verify_native(runner, case, layout):
    result = legacy.verify_native(runner, case, layout)
    verify_plan(layout.managed.parent / 'module-environment.json', case, True)
    return result


if __name__ == '__main__':
    load_manifest()
