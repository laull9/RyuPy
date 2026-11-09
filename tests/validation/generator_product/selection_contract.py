"""只读实际driver阶段原流；begin/end均须完整，准备请求不能冒充lower或输出。"""
import re
import product_contract as c


def stages(stderr, selected, no_need=False, rejected=False):
    text = stderr.decode('utf-8')
    names = ('trace-v1-prepare', 'trace-v1-lower', 'mir-build')
    expected = dict(zip(names, (1, 1, 0) if selected else (1, 0, 0) if rejected else (0 if no_need else 1, 0, 1)))
    counts = {event: {name: len(re.findall(r'event=' + event + ' stage=' + name + r' ', text)) for name in names} for event in ('begin', 'end')}
    c.require(all(row == expected for row in counts.values()), '实际prepare/lower/Legacy阶段begin/end次数不符')
    if rejected:
        c.require('[E0310]' in text, '强制拒绝缺E0310')
        c.require(not re.search(r'event=(?:begin|end) stage=(?:emit|output-write) ', text), '强制拒绝不应发射或写产物')
    else:
        c.require('[E0310]' not in text, '普通成功/域外不应产生强制产品诊断')
    return counts
