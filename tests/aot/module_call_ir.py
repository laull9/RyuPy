"""限定两冻结源的 MIR/CIL 结构门禁；不将非空审查文件当优化证明。"""
import re


def mir_function(text, name):
    parts=re.split(r'(?=^fn )',text,flags=re.M)
    found=[p for p in parts if re.match(r'fn '+re.escape(name)+r'\(',p)]
    if len(found)!=1:raise ValueError('MIR函数身份不唯一：'+name)
    return found[0]


def cil_function(text, name):
    found=re.findall(r'^    \.method[^\n]*\bm\d+_'+re.escape(name)+r'\([^\n]*\n    \{\n(.*?)^    \}',text,re.M|re.S)
    if len(found)!=1:raise ValueError('CIL函数身份不唯一：'+name)
    return found[0]


def cil_blocks(method):
    rows=[(int(i,16),op) for i,op in re.findall(r'^\s+IL_([0-9a-f]+): (.*)$',method,re.M)]
    if not rows:raise ValueError('空CIL指令')
    starts={rows[0][0]}
    for k,(i,op) in enumerate(rows):
        match=re.match(r'(?:Branch\w*|Leave)\((\d+)\)',op)
        if match:
            starts.add(int(match.group(1)))
            if k+1<len(rows):starts.add(rows[k+1][0])
        elif op in ('Return','Throw','Rethrow') and k+1<len(rows):starts.add(rows[k+1][0])
    blocks=[]
    for i,op in rows:
        if i in starts:blocks.append([])
        blocks[-1].append(op)
    return ['\n'.join(b) for b in blocks]



def captured_pair(block, name):
    """冻结源中必须是同一接收者的标志/目标，且按定义顺序消费。"""
    calls=list(re.finditer(r'dyn\.captured_method\((_[0-9]+), (_[0-9]+), "'+re.escape(name)+r'", \[([^\n]*)\]\)',block))
    if len(calls)!=1:raise ValueError('捕获调用身份不唯一：'+name)
    call=calls[0];flag,target=call.group(1,2)
    flags=list(re.finditer(r'^\s*'+re.escape(flag)+r' = RuntimeFn::IsModule\((_[0-9]+)\);',block,re.M))
    targets=list(re.finditer(r'^\s*'+re.escape(target)+r' = RuntimeFn::CaptureModuleTarget\((_[0-9]+), const "'+re.escape(name)+r'"\);',block,re.M))
    if len(flags)!=1 or len(targets)!=1:raise ValueError('捕获标志/目标缺失')
    f,t=flags[0],targets[0]
    if f.group(1)!=t.group(1) or not f.start()<t.start()<call.start():
        raise ValueError('捕获接收者或时序不一致')
    def assignments(local):
        return list(re.finditer(r'^\s*'+re.escape(local)+r' = ',block,re.M))
    for local,start,end in ((f.group(1),f.end(),t.start()),(flag,f.end(),call.start()),(target,t.end(),call.start())):
        if any(start<=d.start()<end for d in assignments(local)):
            raise ValueError('捕获局部在读取后被改写')
    # 当前冻结源的参数均为局部；在块内定义者必须晚于目标读取。
    for arg in re.findall(r'_[0-9]+',call.group(3)):
        defs=assignments(arg)
        if defs and not all(t.end()<d.start()<call.start() for d in defs):
            raise ValueError('捕获参数提前求值')
    return flag,target


def cil_pair(c, name):
    """只针对冻结的一参调用核实际 CIL 局部绑定，不以数量代替数据流。"""
    ops=[op for _,op in re.findall(r'^\s+IL_([0-9a-f]+): (.*)$',c,re.M)]
    def unique(fragment):
        ids=[i for i,op in enumerate(ops) if fragment in op]
        if len(ids)!=1:raise ValueError('CIL捕获入口计数不符')
        return ids[0]
    f=unique('.PyOps.IsModule(');t=unique('.PyOps.CaptureModuleTarget(')
    k=unique('.PyPic.CallCapturedMethod1(')
    if not 0<f<t<k or f+1>=len(ops) or t<2:raise ValueError('CIL捕获顺序不符')
    def local(op,kind):
        m=re.fullmatch(kind+r'\((\d+)\)',op)
        if not m:raise ValueError('CIL捕获局部形态不符')
        return m.group(1)
    receiver=local(ops[f-1],'LoadLocal');flag=local(ops[f+1],'StoreLocal')
    if local(ops[t-2],'LoadLocal')!=receiver or ops[t-1]!='LoadString("'+name+'")':
        raise ValueError('CIL目标不是同一接收者')
    target=local(ops[t+1],'StoreLocal')
    if ops[k-4:k-1]!=['LoadLocal('+flag+')','LoadLocal('+target+')','LoadString("'+name+'")']:
        raise ValueError('CIL调用未消费原捕获对')
    for slot,start,end in ((receiver,f+1,t),(flag,f+2,k),(target,t+2,k)):
        if 'StoreLocal('+slot+')' in ops[start:end]:
            raise ValueError('CIL捕获局部在消费前被改写')
    arg=local(ops[k-1],'LoadLocal')
    stores=[i for i in range(t+2,k-4) if ops[i]=='StoreLocal('+arg+')']
    if len(stores)!=1:raise ValueError('CIL实参没有在捕获后冻结')
    if any('CaptureModuleMethod' in op or 'TryCapturedModuleMethod' in op for op in ops):
        raise ValueError('CIL残留旧token协议')
    return {'is_module':1,'target':1,'captured_call1':1}

def matmul(mir, cil, candidate):
    m=mir_function(mir,'matmul'); c=cil_function(cil,'matmul')
    counts={}
    for label,mt,ct,wanted in [('create','RuntimeFn::NumericListCreate(','.PyNumericListOps.Create(',1),
            ('read','RuntimeFn::NumericListReadCheckedF64(','.PyNumericListOps.ReadCheckedF64(',2),
            ('append','RuntimeFn::NumericListAppendF64(','.PyNumericListOps.AppendF64(',1)]:
        counts[label]={'mir':m.count(mt),'cil':c.count(ct)}
        if counts[label]!={'mir':wanted,'cil':wanted}:raise ValueError('matmul numeric list '+label+'计数退化')
    blocks=re.findall(r'^    bb\d+: \{\n(.*?)^    \}',m,re.M|re.S)
    typed=[b for b in blocks if 'RuntimeFn::NumericListAppendF64(' in b]
    typed_cil=[b for b in cil_blocks(c) if '.PyNumericListOps.AppendF64(' in b]
    if len(typed)!=1 or len(typed_cil)!=1 or any(any(x in b for x in ('CaptureModuleMethod','CaptureModuleTarget','IsModule','dyn.captured_method','CallCapturedMethod')) for b in typed+typed_cil):
        raise ValueError('typedAppend块不能保留capture')
    if candidate:
        fallbacks=[b for b in blocks if 'dyn.captured_method' in b and '"append"' in b]
        if not fallbacks:raise ValueError('候选动态append fallback缺真实捕获对')
        for block in fallbacks:captured_pair(block,'append')
        if 'CaptureModuleMethod' in m+c:raise ValueError('候选残留旧token捕获')
    if not candidate and ('CaptureModuleMethod' in m or 'CaptureModuleMethod' in c):
        raise ValueError('baseline出现新capture')
    return counts


def math_module(mir, cil, candidate):
    m=mir_function(mir,'run');c=cil_function(cil,'run')
    if candidate:
        blocks=re.findall(r'^    bb\d+: \{\n(.*?)^    \}',m,re.M|re.S)
        matches=[b for b in blocks if 'dyn.captured_method' in b and '"sqrt"' in b]
        if len(matches)!=1:raise ValueError('math必须单次循环体sqrt捕获调用')
        captured_pair(matches[0],'sqrt')
        if m.count('RuntimeFn::IsModule(')!=1 or m.count('RuntimeFn::CaptureModuleTarget(')!=1:
            raise ValueError('math多余捕获入口')
        if 'CaptureModuleMethod' in m or 'dyn.method(' in m:raise ValueError('math残留旧捕获/普通调用')
        return {'mir_sqrt_calls':1,'mir_sqrt_captures':1,'cil_pair':cil_pair(c,'sqrt')}
    calls=re.findall(r'dyn\.method\((_[0-9]+), "sqrt",',m)
    if len(calls)!=1:raise ValueError('math必须真实单次循环体sqrt动态调用')
    captures=re.findall(r'(_[0-9]+) = RuntimeFn::CaptureModuleMethod\([^\n]*const "sqrt"\);',m)
    if captures or 'CaptureModuleMethod' in c:raise ValueError('baseline math出现capture')
    return {'mir_sqrt_calls':1,'mir_sqrt_captures':0,'cil_captures':0}


def verify(dumps):
    return {v:{'matmul':matmul(dumps[v]['matmul']['mir'],dumps[v]['matmul']['cil'],v=='C'),
               'math_module':math_module(dumps[v]['math_module']['mir'],dumps[v]['math_module']['cil'],v=='C')}
            for v in ('B','C')}
