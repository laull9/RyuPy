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
    if len(typed)!=1 or len(typed_cil)!=1 or any('CaptureModuleMethod' in b for b in typed+typed_cil):
        raise ValueError('typedAppend块不能保留capture')
    if candidate and not any('CaptureModuleMethod' in b and '"append"' in b and 'dyn.method' in b for b in blocks):
        raise ValueError('候选动态append fallback缺真实capture')
    if not candidate and ('CaptureModuleMethod' in m or 'CaptureModuleMethod' in c):
        raise ValueError('baseline出现新capture')
    return counts


def math_module(mir, cil, candidate):
    m=mir_function(mir,'run');c=cil_function(cil,'run')
    calls=re.findall(r'dyn\.method\((_[0-9]+), "sqrt",',m)
    if len(calls)!=1:raise ValueError('math必须真实单次循环体sqrt动态调用')
    captures=re.findall(r'(_[0-9]+) = RuntimeFn::CaptureModuleMethod\([^\n]*const "sqrt"\);',m)
    if candidate:
        if captures!=calls or m.index('CaptureModuleMethod')>m.index('dyn.method'):
            raise ValueError('math capture必须先于sqrt且token为实际receiver')
        if c.count('.PyOps.CaptureModuleMethod(')!=1 or c.count('.PyPic.CallMethod1(')!=1:
            raise ValueError('math CIL真实capture/call计数不符')
        if c.index('.PyOps.CaptureModuleMethod(')>c.index('.PyPic.CallMethod1('):
            raise ValueError('math CIL capture晚于调用')
    elif captures or 'CaptureModuleMethod' in c:
        raise ValueError('baseline math出现capture')
    return {'mir_sqrt_calls':1,'mir_sqrt_captures':len(captures),
            'cil_captures':c.count('.PyOps.CaptureModuleMethod(')}


def verify(dumps):
    return {v:{'matmul':matmul(dumps[v]['matmul']['mir'],dumps[v]['matmul']['cil'],v=='C'),
               'math_module':math_module(dumps[v]['math_module']['mir'],dumps[v]['math_module']['cil'],v=='C')}
            for v in ('B','C')}
