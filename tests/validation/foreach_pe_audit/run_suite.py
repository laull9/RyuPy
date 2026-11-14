#!/usr/bin/env python3
"""批量运行真实PE审计并变异PE字节验证拒绝能力；保留每轮独立证据。"""
import argparse
import copy
import hashlib
import json
import pathlib
import audit


def save(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2))


def controls(case, out):
    a = audit.decode(case / 'off/foreach_fixture.dll')
    on_path = case / 'on/foreach_fixture.dll'
    b = audit.decode(on_path)
    result = [{'control': 'off_self', 'result': audit.compare(a, a)['status']},
              {'control': 'on_self', 'result': audit.compare(b, b)['status']}]
    methods = b['Methods']
    # 三类负控制均直接修改真实PE IL字节后重新解码，而非伪造JSON。
    changes = []
    for mi, method in enumerate(methods):
        for i, ins in enumerate(method['IL']):
            if audit.call_is(ins, 'PyTraceback', 'SetLine'):
                previous = method['IL'][i - 1]
                if len(bytes.fromhex(previous['Raw'])) == 1:
                    changes.append(('nonallowed_constant', mi, i - 1, bytes([0x1e])))
                    break
        if changes:
            break
    for mi, method in enumerate(methods):
        if not any(audit.call_is(x, 'PyNumericAccumulator', 'Create') for x in method['IL']):
            continue
        current = next((i for i, x in enumerate(method['IL']) if audit.call_is(x, 'PyOps', 'IterValue')), None)
        start = next((x for x in method['IL'] if audit.call_is(x, 'PyOps', 'IterStart')), None)
        if current is not None and start:
            changes.append(('current_call', mi, current, bytes.fromhex(start['Raw'])))
            break
    for mi, si, site, ci in audit.pic_sites(b):
        ins = methods[mi]['IL'][si]
        raw = bytes.fromhex(ins['Raw'])
        if len(raw) == 1:
            changes.append(('wrong_pic_id', mi, si, bytes([0x1e if site != 8 else 0x16])))
            break
    audit.require(len(changes) == 3, '未找到全部负控制变异位置')
    for name, mi, si, replacement in changes:
        method, ins = methods[mi], methods[mi]['IL'][si]
        data = bytearray(on_path.read_bytes())
        offset = method['ILFileOffset'] + ins['Offset']
        before = bytes.fromhex(ins['Raw'])
        audit.require(data[offset:offset + len(before)] == before and len(before) == len(replacement) and before != replacement, 'PE原字节变异校验失败')
        data[offset:offset + len(before)] = replacement
        path = out / (name + '.dll')
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        modified = audit.decode(path)
        try:
            audit.compare(a, modified)
        except AssertionError as error:
            result.append({'control': name, 'result': 'rejected', 'reason': str(error), 'file': str(path), 'sha256': hashlib.sha256(data).hexdigest(), 'file_offset': offset, 'before': before.hex(), 'after': replacement.hex()})
        else:
            raise AssertionError('负控制被错误接受：' + name)
    save(out / 'controls.json', result)
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('evidence', type=pathlib.Path)
    parser.add_argument('output', type=pathlib.Path)
    args = parser.parse_args()
    audit.require(not args.output.exists(), '报告目录已存在，拒绝混写')
    reports, failures, skipped = [], [], []
    for case in sorted(args.evidence.iterdir()):
        if not case.is_dir() or not (case / 'source').exists():
            continue
        if not all((case / d / 'foreach_fixture.dll').exists() for d in ['off', 'on']):
            skipped.append(case.name)
            continue
        out = args.output / case.name
        decoded = []
        for mode in ('off', 'on'):
            pe = audit.decode(case / mode / 'foreach_fixture.dll')
            save(out / (mode + '.decoded.json'), pe)
            decoded.append(pe)
        try:
            report = audit.compare(*decoded)
            report.update(case=case.name, off_sha256=decoded[0]['Sha256'], on_sha256=decoded[1]['Sha256'])
            reports.append(report)
            save(out / 'audit.json', report)
        except Exception as error:
            failures.append({'case': case.name, 'error': str(error)})
    typed = args.evidence / 'planner_static'
    typed_cil = [(typed / mode / 'linked.cil').read_bytes() for mode in ('off', 'on')]
    audit.require(typed_cil[0] == typed_cil[1], 'typed控制CIL不相同')
    typed_report = {'case': 'planner_static', 'cil_identical': True, 'cil_sha256': hashlib.sha256(typed_cil[0]).hexdigest(), 'source_sha256': hashlib.sha256((typed / 'source/subject.py').read_bytes()).hexdigest()}
    save(args.output / 'typed-control.json', typed_report)
    negative = controls(args.evidence / 'empty_seed_identity', args.output / 'controls')
    summary = {'evidence': str(args.evidence.resolve()), 'reports': reports, 'failures': failures, 'skipped_no_pair': skipped, 'controls': negative, 'typed_control': typed_report}
    save(args.output / 'summary.json', summary)
    print(json.dumps({'pairs': len(reports), 'hits': sum(len(r['proofs']) for r in reports), 'failures': failures, 'skipped': skipped, 'controls': negative, 'typed_control': typed_report}, ensure_ascii=False))
    if failures:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
