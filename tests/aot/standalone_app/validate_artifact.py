"""真实发布独立验收：缺发射前oracle即关闭；结构receipt不授权性能计时。"""
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
from standalone_manifest import validate_standalone_schema4, bind_candidate
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


def locked_sources(prepared,case,oracle_dir,tools):
    """锁定应用源和独立driver实际source plan，不从artifact反造预期。"""
    contract=read(HERE/'contract.json'); inputs=read(HERE/'inputs.json'); package=prepared/'package'
    for name,digest in contract['files'].items(): require(sha(HERE/name)==digest,'合同文件变化：'+name)
    require({k:v['sha256'] for k,v in inventory(package).items()}==inputs['files'],'完整包输入变化')
    require(sha(prepared/'console_entry.py')==contract['files']['console_entry.py'],'console源变化')
    require(read(prepared/'metadata-request.json') == {'schema_version':1,'mode':'closed_snapshot',
        'roots':[{'id':'pyfiglet-1.0.2','path':'package'}]}, 'metadata请求不是锁定同包根')
    entry=package/'pyfiglet/__main__.py' if case=='cli' else prepared/'console_entry.py'
    oracle=read(oracle_dir/'mir-oracle.json'); identity=read(oracle_dir/'source-inputs.json')
    require(Path(identity['entry']).resolve()==entry.resolve(),'独立oracle不是当前入口')
    require(identity['cli_sha256'] in tools.values() and identity['runtime_sha256'] in tools.values(),'oracle工具未冻结')
    rows=identity['files']; require(len(rows)==len({r['module_name'] for r in rows}),'独立来源重名')
    emitted={r['module_name']:r for r in oracle['sources']}; seen=set()
    for row in rows:
        path=Path(row['path']).resolve(strict=True); name=row['module_name']; seen.add(name)
        require(sha(path)==row['sha256'],'实际源已变化')
        if name=='__main__': require(path==entry.resolve(),'入口源不符')
        elif name=='pyfiglet' or name.startswith('pyfiglet.'):
            require(path.is_relative_to(package.resolve()),'pyfiglet越界')
            require(inputs['files'].get(path.relative_to(package.resolve()).as_posix())==row['sha256'],'非锁定pyfiglet源')
        if name in emitted: require(emitted[name]['compiled_text_sha256']==row['sha256'],'编译文本非冻结源字节')
    require(set(emitted)<=seen and {'__main__','pyfiglet','pyfiglet.fonts'}<=emitted.keys(),'真实app来源缺项')
    return oracle,identity


def expected_resources(prepared,oracle):
    """由声明、冻结文件及独立emitted映射推导资源，不读取manifest资源字段。"""
    sources={r['module_name']:r for r in oracle['sources']}; expected={}
    for module in read(HERE/'resources.json')['modules']:
        name=module['module']; source=sources[name]
        root=prepared/'package'/Path(*name.split('.'))
        anchor=PurePosixPath('_internal/resources/v1')/source['logical_source']
        for relative in module['files']:
            path=root/relative; require(path.is_file() and not path.is_symlink(),'资源文件缺失')
            target=str(anchor.parent/relative)
            row=expected.setdefault(target,{'relative_path':target,'size':path.stat().st_size,'sha256':sha(path),'consumer_module_ids':[]})
            require(row['size']==path.stat().st_size and row['sha256']==sha(path),'共享目标异字节')
            row['consumer_module_ids'].append(source['module_id'])
    for row in expected.values(): row['consumer_module_ids'].sort()
    return [expected[k] for k in sorted(expected)]


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


def validate_app_artifact(layout,prepared,frozen_tools,*,case,python,dotnet,oracle_dir,output):
    """结构验证后仍需独立clean-container完整六run，receipt不会直接允许measure。"""
    require(case in ('cli','metadata'),'未知case')
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
    oracle,identity=locked_sources(prepared,case,oracle_dir,tools)
    plan=read(oracle_dir/'default_source_plan.json'); payload=(oracle_dir/'candidate.json').read_text()
    candidate=read(oracle_dir/'candidate.json'); roots=read(oracle_dir/'emission-roots.json')
    probe=Probe(output,python,dotnet); decoder=REPO/'tests/validation/source_provenance/ci/read_pe_decoder.py'
    probe.checked('actual-pe',[python,'-B',decoder,layout.managed,'old',case],90)
    decoded=read(output/'actual-pe.stdout'); require(decoded['sha256']==pe_hash,'实际PE解码身份不同')
    types=source_types(layout.managed); validate_cold_abi(decoded,types)
    save(output/'actual-pe-roots.json',cold_registration(decoded,oracle,roots,payload,types,pe_layout(layout.managed)))
    report,_=metadata.audit_pe(probe,'metadata',layout.managed,layout.managed.parent/'distribution-metadata.json',prepared/'metadata-request.json')
    data=read(layout.manifest); sdk=read_sdk_version(layout.host/'sdk.version'); require(sdk=='10.0.403','SDK未锁定')
    expected=expected_resources(prepared,oracle)
    validate_standalone_schema4(data,'linux-x64',sdk,expected_automatic=tuple(plan['automatic_instance_modules']),metadata_report=report,expected_resources=expected)
    require(data['sources']==oracle['sources'] and data['default_source_plan']==plan,'清单与独立source/plan不符')
    bind_candidate(data,payload,candidate)
    actual=check_artifacts(layout.dist,data['artifacts'],'ryupy-artifact.json',windows=False)
    executable=next(r for r in data['artifacts'] if r['role']=='native_executable')
    require(actual[executable['relative_path']].samefile(layout.native),'非本次Native'); image_header(layout.native,'linux-x64')
    inputs={r['role']:r for r in data['build_inputs']}
    require(inputs['compiled_managed_input']['sha256']==pe_hash,'实际托管入口不同')
    require(inputs['runtime_managed_input']['sha256']==identity['runtime_sha256']==sha(layout.managed.parent/'RyuPy.Runtime.dll'),'真实runtime不符')
    require(source_before==inventory(prepared) and dist_before==inventory(layout.dist),'验收时源/dist变化')
    require(sha(layout.manifest)==manifest_hash and sha(layout.managed)==pe_hash,'产物变化')
    require(all(sha(k)==v for k,v in tools.items()) and all(sha(oracle_dir/k)==v for k,v in hashes.items()),'工具/oracle变化')
    receipt={'schema_version':1,'case':case,'structure_validated':True,'clean_container_validated':False,'performance_permitted':False,
        'manifest_sha256':manifest_hash,'managed_pe_sha256':pe_hash,'native_sha256':sha(layout.native),
        'tools':tools,'oracle':hashes,'prepared_files':source_before,'dist_files':dist_before,
        'resource_count':len(expected),'metadata_payload_sha256':report['payload_sha256'],'validator_sha256':sha(__file__)}
    save(output/'receipt.json',receipt); return receipt
