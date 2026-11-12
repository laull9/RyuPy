"""旧metadata synthetic的schema4深检；只接受固定三源Legacy图，不泛开新版schema。"""
import hashlib
import json
from pathlib import Path
import sys
ROOT=Path(__file__).resolve().parents[2]
sys.path[:0]=[str(ROOT/'tests/aot'),str(ROOT/'tests/aot/default_source_aot')]
from schema4 import _validate_schema4
from code_source_checks import source_blocks, source_types
from pe_checks import validate_cold_abi
from manifest_verify import require, no_duplicate_keys

SOURCES=(('__main__','','entry/main.py'),('probe_pkg','probe_pkg','modules/probe_pkg/__init__.py'),
         ('probe_pkg.target','probe_pkg','modules/probe_pkg/target.py'))
PROTECTED=['json','zipfile','optparse','getopt','re','dataclasses','unittest','enum','io','typing','xml.etree.ElementTree','collections.abc']


def expected_candidate():
    locations={name:{'file':Path(path).name,'is_package':name==package,'kind':2,'package':package,
        'path':'_internal/resources/v1/'+path,'registration':'active'} for name,package,path in SOURCES}
    return {'schema_version':3,'roots':[],'dependencies':{},'locations':locations,'deferred':{},
        'outside':[],'protected':PROTECTED,'closed_unplanned':['absent_path_key','absent_target'],
        'external_dependencies':[]}


def validate_candidate(payload):
    require(json.loads(payload, object_pairs_hook=no_duplicate_keys) == expected_candidate(), '实际PE来源载荷不等于固定fixture合同')


def validate_plan_contract(data,sdk,report,source_map):
    require(set(source_map)=={x[0] for x in SOURCES}, 'schema4 metadata仅准入固定synthetic三源图')
    _validate_schema4(data,'linux-x64',sdk,expected_automatic=(),metadata_report=report,
        declared_request=False,expected_policy='ordinary-dynamic-module-pilot')
    expected=[{'module_id':i,'module_name':name,'package':package,'logical_source':path,
        'compiled_text_sha256':source_map[name]} for i,(name,package,path) in enumerate(SOURCES)]
    require(data['sources']==expected,'synthetic源码身份/次序/字节不符')
    require(all(m['environment_reason']=='aot-not-migrated' for m in data['modules']), 'synthetic Legacy理由变化')


def validate_synthetic_schema4(data,sdk,report,pe,source_map,decoded):
    validate_plan_contract(data,sdk,report,source_map)
    require(decoded['sha256']==hashlib.sha256(Path(pe).read_bytes()).hexdigest(), 'metadata实际PE解码身份不符')
    types=source_types(pe);validate_cold_abi(decoded,types)
    _,blocks=source_blocks(decoded,types)
    require(len(blocks)==3 and {b['module_name'] for b in blocks}==set(source_map)
            and all(b['legacy'] for b in blocks),'实际PE来源不是固定Legacy三源')
    registrations=[]; configured=[]
    for method in decoded['methods']:
        ins=(method.get('body') or {}).get('instructions',[])
        for at,item in enumerate(ins):
            target=item.get('target',{})
            if target.get('owner')!='RyuPy.Runtime.PyModules':continue
            name=target.get('name','')
            if name=='ConfigureOptionalSources':
                require(item['opcode']=='0x28' and at>0 and ins[at-1]['opcode']=='0x72','缺真实Configure字符串')
                configured.append(ins[at-1]['string'])
            elif name.startswith('Register'):
                require(name=='RegisterSourceLocated' and item['opcode']=='0x28' and at>=16,'非固定Legacy注册')
                block=ins[at-16:at]
                require([block[i]['opcode'] for i in (0,1,2,4)]==['0x72']*4,'注册参数非原始字符串')
                module=block[0]['string']; row=next((m for m in data['modules'] if m['name']==module),None)
                require(row is not None,'注册未知模块')
                loc=expected_candidate()['locations'][module]
                require([block[i]['string'] for i in (1,2,4)]==[loc['package'],loc['file'],loc['path']], '实际注册锚不符')
                require(block[3]['opcode']=='0x21' and int.from_bytes(bytes.fromhex(block[3]['operand_hex']),'little')==2,'非可搬迁kind2')
                for offset,role in [(8,'init'),(11,'getter'),(14,'setter')]:
                    require(block[offset]['opcode']=='0xfe06' and block[offset]['target']['name']==row[role]['name']
                            and block[offset]['target']['owner']==row[role]['owner'],'实际Legacy委托与清单不符')
                registrations.append(module)
    require(sorted(registrations)==sorted(source_map) and len(configured)==1,'注册/Configure全集不符')
    payload=configured[0]
    validate_candidate(payload)
    require(data['default_source_plan']=={'schema_version':1,'mode':'automatic_locations',
        'candidate_payload_sha256':hashlib.sha256(payload.encode()).hexdigest(),
        'automatic_instance_modules':[],'deferred_sources':[]},'清单计划未绑定真实PE')
