"""默认计划/实际PE/Native身份联合验证；schema4直接验，不投影旧schema。"""
import copy
import os
import shutil
from pathlib import Path
from checking import require, sha, parse
from support import ROOT, old
from schema4 import validate_schema4, bind_candidate
from pe_checks import cold_registration, validate_cold_abi
import manifest_verify as manifest
import distribution_metadata_contract as metadata
from contract import pe_timestamp_equivalence
from instance_pe import envelope, unchanged


def decode(r, label, pe):
    decoder = ROOT/'tests/validation/source_provenance/ci/read_pe_decoder.py'
    r.checked(label, [r.python, '-B', decoder, pe, 'old', label], 90)
    decoded = parse((r.out/(label+'.stdout')).read_bytes())
    require(decoded['sha256'] == sha(pe), '解码PE身份不符')
    validate_cold_abi(decoded, metadata.pe_structure(pe))
    return decoded


def rust_owner_gate(r, case, folder, label):
    name = case['name']
    if not (folder/(name+'-expected.json')).is_file():
        require(not case['plan']['automatic_instance_modules'], '选中Instance却无owner期望')
        return
    identity = envelope(folder, name, os.environ['GITHUB_SHA'])
    expected = parse((folder/(name+'-expected.json')).read_bytes())
    require(expected['selected'] == case['plan']['automatic_instance_modules'], 'owner选择与来源计划不等')
    r.env.update(RYUPY_INSTANCE_AOT_STRUCTURE_OUT=str(folder), RYUPY_INSTANCE_CLASS_CASES=name)
    r.named_test(label, ['cargo', 'test', '--locked', '-p', 'ryupy-codegen-cil',
                         'instance_class_actual_pe_gate', '--', '--ignored', '--nocapture'],
                 'instance_class_actual_pe_gate', 900)
    unchanged(folder, identity)
    r.env.pop('RYUPY_INSTANCE_AOT_STRUCTURE_OUT'); r.env.pop('RYUPY_INSTANCE_CLASS_CASES')


def audit_structure(r, case):
    folder = case['structure']; name = case['name']
    case['oracle'] = parse((folder/'mir-oracle.json').read_bytes())
    case['roots'] = parse((folder/'emission-roots.json').read_bytes())
    case['payload'] = (folder/'candidate.json').read_text()
    case['candidate'] = parse(case['payload'])
    case['plan'] = parse((folder/'default_source_plan.json').read_bytes())
    oracle = case['oracle']; sources = oracle['sources']
    require([s['module_id'] for s in sources] == list(range(len(sources))), '编译来源ID不连续')
    mapping = oracle['source_to_emitted']
    require(sorted(x for x in mapping if x is not None) == list(range(len(sources))), '双索引非双射')
    require(sum(x is None for x in mapping) == len(case['plan']['deferred_sources']), '孔洞/延期不等')
    require(oracle['candidate_payload_sha256'] == sha(folder/'candidate.json') == case['plan']['candidate_payload_sha256'], '发射前载荷身份不同')
    if name == 'native_entry':
        require('probe_pkg.outside' in case['candidate']['outside'] and
                'probe_pkg.outside' not in case['candidate']['locations'], '真实未选group没有形成Outside')
        require('probe_pkg.deferred' in case['candidate']['deferred'], '真实async-with未形成Deferred')
        require(case['candidate']['closed_unplanned'] == [], '默认不授closed权限')
    pe = folder/(name+'.dll')
    r.save(name+'-structure-roots.json', cold_registration(decode(r, name+'-decode', pe), oracle,
                                                         case['roots'], case['payload'], metadata.pe_structure(pe)))
    rust_owner_gate(r, case, folder, name+'-owner-signatures')
    r.freeze([p for p in folder.iterdir() if p.is_file()])


def audit_native(r, case, layout):
    name = case['name']; source = case['structure']
    r.save(name+'-coff-equivalence.json', pe_timestamp_equivalence(
        (source/(name+'.dll')).read_bytes(), layout.managed.read_bytes()))
    decoded = decode(r, name+'-actual-cli-decode', layout.managed)
    r.save(name+'-actual-cli-roots.json', cold_registration(decoded, case['oracle'], case['roots'], case['payload'], metadata.pe_structure(layout.managed)))
    folder = r.out/(name+'-actual-cli-pe'); folder.mkdir()
    shutil.copy2(layout.managed, folder/(name+'.dll'))
    expected_file = source/(name+'-expected.json')
    if expected_file.is_file():
        expected = parse(expected_file.read_bytes()); original = copy.deepcopy(expected)
        expected['pe'] = {'name': name+'.dll', 'size': layout.managed.stat().st_size, 'sha256': sha(layout.managed)}
        require({k:v for k,v in original.items() if k!='pe'} == {k:v for k,v in expected.items() if k!='pe'}, '改变预言')
        old.save(folder/(name+'-expected.json'), expected)
    rust_owner_gate(r, case, folder, name+'-actual-cli-owner-signatures')
    before = sha(layout.manifest); data = parse(layout.manifest.read_bytes())
    sdk = manifest.read_sdk_version(layout.host/'sdk.version')
    require(sdk == '10.0.403', '固定SDK变化')
    report_path = layout.managed.parent/'distribution-metadata.json'
    require(report_path.is_file(), '真实默认metadata报告缺失')
    roots = [case['entry'].parent, case['root']]
    request = r.out/(name+'-metadata-audit-request.json')
    old.save(request, {'schema_version': 1, 'mode': 'closed_snapshot',
                      'roots': [{'id': 'search-'+str(i), 'path': str(p)} for i,p in enumerate(roots)]})
    report, _ = metadata.audit_pe(r, name+'-metadata', layout.managed, report_path, request)
    validate_schema4(data, 'linux-x64', sdk, expected_automatic=tuple(case['plan']['automatic_instance_modules']), metadata_report=report)
    require(data['default_source_plan'] == case['plan'], '原默认计划与CLI清单不同')
    require(data['sources'] == case['oracle']['sources'], '编译源码全集不同')
    bind_candidate(data, case['payload'], case['candidate'])
    actual = manifest.check_artifacts(layout.dist, data['artifacts'], manifest.MANIFEST_NAME, windows=False)
    entry = next(x for x in data['artifacts'] if x['role'] == 'native_executable')
    native = actual[entry['relative_path']]
    require(native.samefile(layout.native) and native.parent.samefile(layout.native.parent) and native.name == layout.native.name, '非唯一ELF')
    manifest.image_header(native, 'linux-x64')
    inputs = {x['role']:x for x in data['build_inputs']}
    require(inputs['runtime_managed_input']['sha256'] == sha(r.runtime) and
            inputs['compiled_managed_input']['sha256'] == sha(layout.managed), '真实托管输入不符')
    require(sha(layout.manifest) == before, '检查器修改manifest')
    r.save(name+'-schema4-verified.json', {'manifest_sha256': before, 'native_sha256':sha(native),
                                         'scope':'位置闭包，不是全部资源/trim证明'})
