"""仅测试侧来源冷初始化审核；MIR声明是期望，PE字段与实际调用是被检查对象。"""
from collections import Counter
from pathlib import Path
import runpy
import sys
from checking import require, sha
from pe_checks import signature

PREFIX = 'RyuPy.Runtime.'
FACTORIES = {'CreateCodeSource', 'CreateLegacyCodeSourceV1'}


def object_tokens(meta):
    """仅现有后端System.Runtime 10.0.0.0签名引用可与ELEMENT_TYPE_OBJECT等价。"""
    tables=meta['tables']; string=meta['string']
    require(not any(string(row[1])=='Object' and string(row[2])=='System' for row in tables[2]), '拒绝同名System.Object TypeDef')
    tokens=[]
    for rid,row in enumerate(tables[1],1):
        if (string(row[2]),string(row[1])) != ('System','Object'): continue
        scope=row[0]; assembly=scope >> 2
        require(scope & 3 == 2 and 1 <= assembly <= len(tables[35]), 'Object不是外部AssemblyRef归属')
        ref=tables[35][assembly-1]
        require(string(ref[6])=='System.Runtime' and ref[:5]==[10,0,0,0,0] and
                string(ref[7])=='' and meta['signature'](ref[5])=='b03f5f7f11d50a3a', 'Object不是现有System.Runtime强名引用')
        tokens.append(hex(0x01000000|rid))
    require(tokens, '缺System.Runtime Object引用')
    return tokens


def source_types(pe):
    import distribution_metadata_contract as metadata
    types=metadata.pe_structure(pe)
    types['__trusted_object_tokens__']=pe_layout(pe)['trusted_object_tokens']
    return types


def pe_layout(pe):
    """复用现有只读表，不新增后端ABI；FieldList独立确定owner及slot。"""
    parser = Path(__file__).resolve().parents[3]/'tests/validation/source_provenance/ci/pe_metadata.py'
    require(sha(parser) == 'a43bf4bb9e89ceb169b89a67c8817e8cf2312429cb251a0f36f1b9da8f469bc3', 'PE表工具身份变化')
    argv = sys.argv[:]
    try:
        sys.argv = [str(parser), str(pe)]
        meta = runpy.run_path(str(parser))
    finally:
        sys.argv = argv
    tables = meta['tables']; string = meta['string']; fields = {}
    for index, row in enumerate(tables[2]):
        owner = (string(row[2])+'.' if string(row[2]) else '')+string(row[1])
        end = tables[2][index+1][4] if index+1 < len(tables[2]) else len(tables[4])+1
        for slot, rid in enumerate(range(row[4], end)):
            f = tables[4][rid-1]
            fields[hex(0x04000000 | rid)] = {'owner':owner, 'slot':slot,
                'name':string(f[1]), 'flags':f[0], 'signature_hex':meta['signature'](f[2])}
    return {'fields':fields, 'entry_token':hex(meta['u'](meta['cli']+20)),
            'trusted_object_tokens':object_tokens(meta)}


def static_void(method, types):
    require(method.get('flags', 0) & 0x10 and method.get('body'), '来源初始化必须static有body')
    require(signature(method['method']['signature_hex'], types) == (0,'void',()), '来源初始化ABI错误')


def calls(method, token):
    return [i for i,x in enumerate((method.get('body') or {}).get('instructions', [])) if x.get('token') == token and x['opcode'] == '0x28']


def prefix_calls(method, stop, types, by_token, allowed, local_literal=False):
    """入口前缀仅现有框架准备；任意业务call即失败，不做通用CFG推理。"""
    safe={'0x0','0x2','0x72','0x28','0xd0'}
    prefix=method['body']['instructions'][:stop]
    require(all(x['opcode'] in safe for x in prefix), '初始化前缀含跳转/未知操作')
    seen=set()
    for ins in prefix:
        if ins['opcode']!='0x28': continue
        token=ins.get('token');target=ins.get('target',{});key=(target.get('owner'),target.get('name'))
        require(key not in seen, '初始化准备重复调用');seen.add(key)
        if token in by_token:
            actual=by_token[token]
            require(local_literal and actual['method']['owner']==method['method']['owner'] and
                    actual['method']['name']=='__init_literals' and target==actual['method'], '提前调用业务/非既定冷literal')
            static_void(actual,types)
            for x in actual['body']['instructions']:
                if x['opcode']=='0x28':
                    ref=x.get('target',{});name=ref.get('name')
                    args={'BoxInt':('long',),'BoxBool':('bool',),'FromDecimalString':('string',)}
                    require(ref.get('owner')==PREFIX+'PyOps' and name in args and
                            int(x.get('token','0'),16)>>24==10 and
                            signature(ref['signature_hex'],types)==(0,'object',args[name]), 'literal含非纯装箱调用')
                else:
                    require(x['opcode'] in {'0x2a','0x80','0x21','0x23','0x8c','0x72','0x16','0x17'}, 'literal含隐藏回调/未知操作')
        else:
            require(key in allowed and int(token or '0',16)>>24==10 and
                    signature(target['signature_hex'],types)==allowed[key], '提前调用未知框架/业务或错误ABI')


def initialization_chain(decoded, types, layout, host, source):
    """真实CLI入口→冷初始化→来源表；不把同名方法存在当可达。"""
    by_token = {m['token']:m for m in decoded['methods']}
    require(layout['entry_token'] in by_token, 'CLI入口缺失')
    main = by_token[layout['entry_token']]
    static_void(host, types); static_void(source, types)
    require(main.get('flags',0) & 0x10, 'CLI入口非static')
    first = calls(main,host['token']); second = calls(host,source['token'])
    require(len(first)==len(second)==1, '来源初始化非唯一真实调用链')
    prefix_calls(main,first[0],types,by_token,{
        (PREFIX+'PyTraceback','SetFile'):(0,'void',('string',)),
        (PREFIX+'PySystem','Initialize'):(0,'void',('string','string[]'))})
    prefix_calls(host,second[0],types,by_token,{
        ('System.Type','GetTypeFromHandle'):(0,'System.Type',('valuetype:System.RuntimeTypeHandle',)),
        (PREFIX+'PyDistributionSnapshot','Configure'):(0,'void',('System.Type','string'))},local_literal=True)
    wrappers = []
    for method in decoded['methods']:
        ins = (method.get('body') or {}).get('instructions',[])
        if any(x.get('target',{}).get('owner')==PREFIX+'PyModules' and
               x.get('target',{}).get('name')=='RunMain' for x in ins):
            require(len(ins)==2 and ins[0]['opcode']=='0x28' and ins[1]['opcode']=='0x2a' and
                    signature(ins[0]['target']['signature_hex'],types)==(0,'void',()), '业务wrapper不是精确RunMain')
            static_void(method, types); wrappers.append(method)
    require(len(wrappers)==1, '业务wrapper不唯一')
    business = calls(main,wrappers[0]['token'])
    require(len(business)==1 and business[0]>first[0], '业务先于来源初始化')
    # 来源表初始化不得在另一个位置重复调用。
    all_source_calls = [(m['token'],i) for m in decoded['methods'] for i in calls(m,source['token']) if m.get('body')]
    require(all_source_calls == [(host['token'],second[0])], '来源表被重复或旁路调用')


def source_blocks(decoded, types):
    """只解析已存在PE实际块；返回事实，不制造缺失的MIR oracle。"""
    found = []
    for method in decoded['methods']:
        for i,x in enumerate((method.get('body') or {}).get('instructions',[])):
            target = x.get('target',{})
            if target.get('owner')==PREFIX+'PyTraceback' and target.get('name') in FACTORIES:
                require(x['opcode']=='0x28' and signature(target['signature_hex'],types)==
                        (0,'object',('string','string',PREFIX+'PyModuleGet')), '来源工厂真实call/ABI错误')
                found.append((method,i,target['name']))
    require(found and len({m['token'] for m,_,_ in found})==1, '来源工厂不在唯一初始化体')
    host = found[0][0]; static_void(host,types); ins=host['body']['instructions']; result=[]; cursor=0
    for method,at,factory in found:
        legacy=factory=='CreateLegacyCodeSourceV1'; start=at-(5 if legacy else 3)
        require(start==cursor and at+1<len(ins), '来源块遗漏/额外指令')
        require(ins[start]['opcode']==ins[start+1]['opcode']=='0x72', '来源字符串不真实')
        require(ins[start+2]['opcode']=='0x14', '来源getter/null错误')
        getter=None; position=None
        if legacy:
            target=ins[start+3];ctor=ins[start+4]
            require(target['opcode']=='0xfe06' and ctor['opcode']=='0x73' and
                    ctor.get('target',{}).get('owner')==PREFIX+'PyModuleGet' and
                    ctor['target']['name']=='.ctor' and
                    signature(ctor['target']['signature_hex'],types)==(32,'void',('object','nint')), '来源getter真实委托错误')
            getter=target['token'];position=(host['token'],start+3)
        store=ins[at+1]
        require(store['opcode']=='0x80' and 'field_token' in store, '来源未紧接stsfld')
        result.append({'module_name':ins[start]['string'],'source_file':ins[start+1]['string'],
                       'legacy':legacy,'getter_token':getter,'root':position,'field_token':store['field_token']})
        cursor=at+2
    require(cursor==len(ins)-1 and ins[-1]['opcode']=='0x2a', '来源体不是完整块全集')
    require(len({x['field_token'] for x in result})==len(result), '来源字段重复存储')
    return host,result


def verify_sources(decoded, oracle, types, layout, getters, hosts):
    require('static_globals' in oracle and 'code_sources' in oracle, '缺独立发射前CodeSource声明')
    modules={m['module_id']:m for m in oracle['modules']}; declarations=oracle['code_sources']
    require(Counter(d['module_id'] for d in declarations)==Counter(modules.keys()), '模块来源不唯一/不完整')
    static=oracle['static_globals']; ids=[g['global_id'] for g in static]
    require(ids==sorted(set(ids)), '静态global映射不唯一有序')
    source_global={g['global_id']:g for g in static if g['storage']=='CodeSource'}
    require(set(source_global)=={d['global_id'] for d in declarations} and
            len(source_global)==len(declarations), 'CodeSource globals全集不符')
    require(len(hosts)==1, '冷注册host不唯一')
    host=next(m for m in decoded['methods'] if m['token'] in hosts); owner=host['method']['owner']
    fields=sorted([(t,f) for t,f in layout['fields'].items() if f['owner']==owner],key=lambda v:v[1]['slot'])
    require(len(fields)>=len(static), '缺MIR静态字段')
    mapped={}
    for slot,g in enumerate(static):
        token,field=fields[slot]
        require(field['slot']==slot and field['name']==g['name'] and field['flags']&0x17==0x11, '字段次序/owner/name/private-static不符')
        mapped[g['global_id']]=(token,field)
    source,blocks=source_blocks(decoded,types)
    require(len(blocks)==len(declarations), '来源构造全集不符')
    result=set()
    for declaration,block in zip(declarations,blocks):
        module=modules[declaration['module_id']]; legacy=declaration['legacy_entry']
        require(declaration['entry_fn']==module['init_fn'] and declaration['name']==source_global[declaration['global_id']]['name'], '来源不属于module init/global')
        require((legacy is not None)==(module['execution']=='LegacySingleton')==block['legacy'], 'Legacy/Instance来源域不符')
        if legacy is not None:
            require(legacy['initial_name']==module['name'] and legacy['name_global'] in ids, 'Legacy初值声明错误')
        require(block['module_name']==module['name'] and block['source_file']==declaration['source_file'], '来源名/真实文件不符')
        token,field=mapped[declaration['global_id']]
        require(token==block['field_token'], '来源写入错误GlobalId字段')
        sig=field['signature_hex']; require(sig.startswith('06'), '非Field签名')
        require(signature('0000'+sig[2:],types)==(0,'object',()), '来源字段不是解析后的object')
        if block['legacy']:
            getter=getters[declaration['module_id']]
            require(block['getter_token']==getter['token'] and getter['flags']&0x10 and
                    signature(getter['method']['signature_hex'],types)==(0,'object',('string',)), '来源getter不等真实注册getter')
            result.add(block['root'])
    source_fields={b['field_token'] for b in blocks}
    actual=Counter((m['token'],x.get('field_token')) for m in decoded['methods'] for x in (m.get('body') or {}).get('instructions',[]) if x['opcode']=='0x80' and x.get('field_token') in source_fields)
    require(actual==Counter((source['token'],t) for t in source_fields), '来源字段另处重写')
    initialization_chain(decoded,types,layout,host,source)
    return result
