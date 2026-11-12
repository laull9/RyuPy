"""binaryornot/chardet普通consumer、metadata-only的独立实际产物审计；不修改共享消费者或宣称性能。"""
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import subprocess
import sys
HERE=Path(__file__).resolve().parent
REPO=HERE.parents[2]
sys.path[:0]=[str(REPO/'tests/aot'),str(REPO/'tests/aot/default_source_aot'),str(REPO/'tests/validation')]
from manifest_verify import no_duplicate_keys, check_artifacts, image_header, read_sdk_version
from schema4 import _validate_schema4, bind_candidate
from code_source_checks import source_types, pe_layout
from pe_checks import cold_registration, validate_cold_abi
import distribution_metadata_contract as metadata


def require(ok, reason):
    if not ok: raise ValueError(reason)

def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def read(path): return json.loads(Path(path).read_text(),object_pairs_hook=no_duplicate_keys)
def save(path,value): Path(path).write_text(json.dumps(value,indent=2,sort_keys=True)+'\n')

def inventory(root):
    result={}
    for p in sorted(Path(root).rglob('*')):
        require(not p.is_symlink(),'拒绝symlink')
        if p.is_file(): result[p.relative_to(root).as_posix()]={'sha256':sha(p),'size':p.stat().st_size}
    return result


def check_tool_roles(identity, tools, cli, runtime):
    """角色绑定到明确路径；交换两个合法摘要也必须失败。"""
    for role, path in [('cli', cli), ('runtime', runtime)]:
        key = str(Path(path).resolve())
        require(key in tools and sha(key) == tools[key], role + '角色工具未冻结')
        require(identity[role + '_sha256'] == tools[key], role + '角色摘要不匹配')


EXPECTED_MODULES = read(HERE/'inputs.json')['expected_modules']


def check_source_contract(rows,emitted,expected):
    """精确47来源来自独立归档锁，不能接受oracle自给摘要扩张来源。"""
    names=[r['module_name'] for r in rows]
    require(len(names)==len(set(names)) and set(names)==set(EXPECTED_MODULES),'来源不是精确47模块')
    emitted_names=[r['module_name'] for r in emitted]
    require(len(emitted_names)==len(set(emitted_names)) and set(emitted_names)==set(names),'发射来源缺失/重复/新增')
    require([r['module_id'] for r in emitted]==list(range(47)),'发射ID非连续原顺序')
    by_name={r['module_name']:r for r in emitted}
    for row in rows:
        path,digest=expected[row['module_name']]
        require(Path(row['path']).resolve()==Path(path).resolve(),'锁定来源路径不符')
        require(row['sha256']==digest==by_name[row['module_name']]['compiled_text_sha256'],'锁定源码摘要不符')


def check_request_identity(identity,prepared):
    require(Path(identity['entry']).resolve()==(prepared/'scanner.py').resolve(),'oracle不是consumer入口')
    require([Path(x).resolve() for x in identity['search_roots']]==[(prepared/'vendor').resolve()],'oracle搜索根改变')
    require(identity['include_modules']==[],'不得引入显式includes')
    require(identity['resources'] is None,'metadata-only不得引入资源声明')
    require(Path(identity['metadata']).resolve()==(prepared/'distribution.json').resolve(),'oracle metadata请求不符')


def locked_sources(prepared,oracle_dir,tools,cli,runtime):
    from prepare import configuration,metadata_request
    contract=read(HERE/'contract.json');inputs=read(HERE/'inputs.json');files={}
    for name,digest in contract['files'].items():require(sha(HERE/name)==digest,'合同输入改变:'+name)
    for row in inputs['packages'].values():
        require(not (files.keys() & row['files'].keys()),'vendor来源重叠');files.update(row['files'])
    require({k:v['sha256'] for k,v in inventory(prepared/'vendor').items()}==files,'vendor完整锁改变')
    expected={}
    for name,relative in EXPECTED_MODULES.items():
        path=prepared/relative
        digest=contract['files']['scanner.py'] if name=='__main__' else files[relative.removeprefix('vendor/')]
        require(path.is_file() and not path.is_symlink() and sha(path)==digest,'锁定真实源码改变')
        expected[name]=(path,digest)
    require(read(prepared/'distribution.json')==metadata_request(),'metadata单vendor根合同改变')
    require(read(prepared/'ryupy.json')==configuration(),'项目配置改变')
    oracle=read(oracle_dir/'mir-oracle.json');identity=read(oracle_dir/'source-inputs.json')
    check_tool_roles(identity,tools,cli,runtime);check_request_identity(identity,prepared)
    check_source_contract(identity['files'],oracle['sources'],expected)
    return oracle,identity


def validate_metadata_only(data,sdk,report):
    _validate_schema4(data,'linux-x64',sdk,expected_automatic=(),metadata_report=report,
        declared_request=False,expected_policy='explicit-only')
    require(data['resources']==[],'metadata-only不得携数据资源')
    require(len(data['sources'])==47 and {s['module_name'] for s in data['sources']}==set(EXPECTED_MODULES),'manifest不是47源合同')
    require(all(m['environment_reason']=='policy-disabled' for m in data['modules']),'默认Legacy理由改变')
    require([(r['metadata_name'],r['metadata_version']) for r in report['distributions']]==
            [('binaryornot','0.4.4'),('chardet','5.2.0')],'metadata分发全集/版本不符')


class Probe:
    def __init__(self,out,python,dotnet):
        self.out=out; self.python=python; self.frozen={}
        self.env=dict(os.environ,DOTNET=str(dotnet),DOTNET_ROOT=str(Path(dotnet).parent),PYTHONDONTWRITEBYTECODE='1')
    def checked(self,name,argv,timeout):
        command=list(map(str,argv)); save(self.out/(name+'.command.json'),command)
        p=subprocess.run(command,cwd=REPO,env=self.env,capture_output=True,timeout=timeout)
        (self.out/(name+'.stdout')).write_bytes(p.stdout); (self.out/(name+'.stderr')).write_bytes(p.stderr)
        save(self.out/(name+'.result.json'),{'exit':p.returncode})
        require(p.returncode==0,'实际PE探针失败：'+name)


def validate_binaryornot_artifact(layout,prepared,frozen_tools,*,python,dotnet,cli,runtime,oracle_dir,output):
    """结构验证后仍需独立clean-container完整八run，receipt不会直接允许measure。"""
    expected_rid='linux-x64'; case='metadata'
    prepared,oracle_dir,output=Path(prepared),Path(oracle_dir),Path(output)
    output.mkdir(parents=True,exist_ok=False)
    tools={str(Path(k).resolve()):v for k,v in frozen_tools.items()}
    require(all(sha(k)==v for k,v in tools.items()),'冻结工具变化')
    require(str(Path(python).resolve()) in tools and str(Path(dotnet).resolve()) in tools,'探针工具未冻结')
    save(output/'tools.json',tools)
    names=['mir-oracle.json','emission-roots.json','candidate.json','default_source_plan.json','source-inputs.json']
    require(all((oracle_dir/n).is_file() for n in names),'缺独立发射前oracle，不能从manifest造预期')
    hashes={n:sha(oracle_dir/n) for n in names}
    source_before=inventory(prepared); dist_before=inventory(layout.dist)
    manifest_hash,pe_hash=sha(layout.manifest),sha(layout.managed)
    oracle,identity=locked_sources(prepared,oracle_dir,tools,cli,runtime)
    plan=read(oracle_dir/'default_source_plan.json'); payload=(oracle_dir/'candidate.json').read_text()
    candidate=read(oracle_dir/'candidate.json'); roots=read(oracle_dir/'emission-roots.json')
    probe=Probe(output,python,dotnet); decoder=REPO/'tests/validation/source_provenance/ci/read_pe_decoder.py'
    probe.checked('actual-pe',[python,'-B',decoder,layout.managed,'old',case],90)
    decoded=read(output/'actual-pe.stdout'); require(decoded['sha256']==pe_hash,'实际PE解码身份不同')
    types=source_types(layout.managed); validate_cold_abi(decoded,types)
    save(output/'actual-pe-roots.json',cold_registration(decoded,oracle,roots,payload,types,pe_layout(layout.managed)))
    report,_=metadata.audit_pe(probe,'metadata',layout.managed,layout.managed.parent/'distribution-metadata.json',prepared/'distribution.json')
    data=read(layout.manifest); sdk=read_sdk_version(layout.host/'sdk.version'); require(sdk=='10.0.403','SDK未锁定')
    require(plan['automatic_instance_modules'] == [], '默认app不得自动实例选择')
    validate_metadata_only(data,sdk,report)
    require(data['sources']==oracle['sources'] and data['default_source_plan']==plan,'清单与独立source/plan不符')
    bind_candidate(data,payload,candidate)
    actual=check_artifacts(layout.dist,data['artifacts'],'ryupy-artifact.json',windows=expected_rid=='win-x64')
    executable=next(r for r in data['artifacts'] if r['role']=='native_executable')
    require(actual[executable['relative_path']].samefile(layout.native),'非本次Native'); image_header(layout.native,expected_rid)
    inputs={r['role']:r for r in data['build_inputs']}
    require(inputs['compiled_managed_input']['sha256']==pe_hash,'实际托管入口不同')
    require(inputs['runtime_managed_input']['sha256']==identity['runtime_sha256']==sha(layout.managed.parent/'RyuPy.Runtime.dll'),'真实runtime不符')
    require(source_before==inventory(prepared) and dist_before==inventory(layout.dist),'验收时源/dist变化')
    require(sha(layout.manifest)==manifest_hash and sha(layout.managed)==pe_hash,'产物变化')
    require(all(sha(k)==v for k,v in tools.items()) and all(sha(oracle_dir/k)==v for k,v in hashes.items()),'工具/oracle变化')
    receipt={'schema_version':1,'expected_rid':expected_rid,'case':case,'structure_validated':True,'clean_container_validated':False,'performance_permitted':False,
        'manifest_sha256':manifest_hash,'managed_pe_sha256':pe_hash,'native_sha256':sha(layout.native),
        'tools':tools,'oracle':hashes,'prepared_files':source_before,'dist_files':dist_before,
        'resource_count':0,'metadata_payload_sha256':report['payload_sha256'],'validator_sha256':sha(__file__)}
    save(output/'receipt.json',receipt); return receipt
