"""只读实际driver原流：阶段begin/end与单点results分开，准备/旧模块完成均不能冒充产物证书。"""
import re
import product_contract as c


def stages(stderr, selected, no_need=False, rejected=False):
    text = stderr.decode('utf-8')
    names = ('trace-v1-prepare', 'trace-v1-lower', 'mir-build')
    expected = dict(zip(names, (1, 1, 0) if selected else (1, 0, 0) if rejected else (0 if no_need else 1, 0, 1)))
    counts = {event: {name: len(re.findall(r'event=' + event + ' stage=' + name + r' ', text)) for name in names} for event in ('begin', 'end')}
    c.require(all(row == expected for row in counts.values()), '实际prepare/lower/Legacy阶段begin/end次数不符')
    # 两个产品门都只收实际单源码__main__；核顺序和模块，不能只凭跨模块总数。
    ordered = re.findall(r'event=(begin|end) stage=(' + '|'.join(names) + r') module="__main__"(?: |\n|$)', text)
    wanted = [(event, name) for name in names if expected[name] for event in ('begin', 'end')]
    c.require(ordered == wanted, '实际prepare/lower/Legacy阶段顺序或所属模块不符')
    result_names = ('trace-v1-not-in-domain', 'trace-legacy')
    wanted_results = dict(zip(result_names, (0, 0) if selected or rejected else (0 if no_need else 1, 1)))
    results = dict.fromkeys(result_names, 0)
    positions = {}
    offset = 0
    for line in text.splitlines(keepends=True):
        tag = re.search(r'\bstage=(' + '|'.join(result_names) + r')(?:\s|$)', line)
        if tag:
            # 即使有一条正确result，旁边残留的假begin/end/其它event也必须拒绝。
            match = re.fullmatch(r'\[ryupy-stage\] event=result stage=(' + '|'.join(result_names)
                                 + r') module="__main__" elapsed_us=[0-9]+\n?', line)
            c.require(match is not None, '单点结果必须仅用result事件并属于__main__')
            name = match.group(1)
            results[name] += 1
            positions[name] = offset
        offset += len(line)
    c.require(results == wanted_results, '实际单点结果缺失、重复或选路范围不符')

    def boundary(event, stage, module='__main__'):
        matches = list(re.finditer(r'event=' + event + ' stage=' + stage + ' module="' + module + r'"(?: |\n|$)', text))
        c.require(len(matches) == 1, '结果相邻阶段边界缺失或不唯一：' + stage)
        return matches[0].start()

    if results['trace-v1-not-in-domain']:
        c.require(boundary('end', 'trace-v1-prepare') < positions['trace-v1-not-in-domain']
                  < boundary('begin', 'mir-build'), '域外结果不在prepare返回与旧MIR开始之间')
    if results['trace-legacy']:
        c.require(boundary('end', 'mir-build') < boundary('end', 'final-validate')
                  < positions['trace-legacy'] < boundary('begin', 'link', '-'),
                  'Legacy结果不在模块旧MIR校验完成与link开始之间')
    if rejected:
        c.require('[E0310]' in text, '强制拒绝缺E0310')
        c.require(not re.search(r'event=(?:begin|end) stage=(?:emit|output-write) ', text), '强制拒绝不应发射或写产物')
    else:
        c.require('[E0310]' not in text, '普通成功/域外不应产生强制产品诊断')
    # 保留原两个区间计数映射；结果只在独立兄弟字段，不混入begin/end阶段。
    return {**counts, 'results': results}
