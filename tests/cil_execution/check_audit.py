#!/usr/bin/env python3
"""ADR-0208 生命周期执行门：覆盖完整且三阶段零 Python 帧才允许通过。"""
from __future__ import annotations

import argparse
import json
from pathlib import Path


PHASES = ('startup', 'runtime', 'shutdown')


def audit_failures(report: object) -> list[str]:
    """拒绝缺失、截断和覆盖不足的观测；零计数也须同时满足覆盖合同。"""
    if not isinstance(report, dict) or not isinstance(report.get('execution_audit'), dict):
        return ['缺少 execution_audit 生命周期审计']
    audit = report['execution_audit']
    failures = []
    if report.get('state') != 'closed':
        failures.append('宿主尚未关闭，缺少最终快照')
    for name in ('enabled', 'complete', 'startup_complete', 'hook_installed', 'finalized'):
        if audit.get(name) is not True:
            failures.append(f'{name} 必须为 true')
    for name in ('dropped_frames', 'coverage_failures'):
        value = audit.get(name)
        if type(value) is not int or value != 0:
            failures.append(f'{name} 必须为整数零')
    totals = {}
    for phase in PHASES:
        value = audit.get(phase + '_frames')
        if type(value) is not int or value < 0:
            failures.append(f'{phase}_frames 必须为非负整数')
        else:
            totals[phase] = value
            if value:
                failures.append(f'{phase} 仍执行 {value} 次 Python 帧')
    rows = audit.get('frames')
    sums = dict.fromkeys(PHASES, 0)
    if not isinstance(rows, list):
        failures.append('缺少 frames 明细数组')
    else:
        for index, row in enumerate(rows):
            if (not isinstance(row, dict) or row.get('phase') not in PHASES
                    or type(row.get('calls')) is not int or row['calls'] <= 0
                    or not isinstance(row.get('file'), str)
                    or not isinstance(row.get('function'), str)):
                failures.append(f'frames[{index}] 格式无效')
                continue
            sums[row['phase']] += row['calls']
        for phase, total in totals.items():
            if sums[phase] != total:
                failures.append(f'{phase} 帧计数与明细不一致')
    return failures


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('report', type=Path, help='真实产品进程关闭后的宿主报告')
    args = parser.parse_args()
    try:
        failures = audit_failures(json.loads(args.report.read_text(encoding='utf-8')))
    except (OSError, ValueError) as error:
        failures = [f'无法读取审计报告：{error}']
    if failures:
        print('生命周期执行门失败：' + '；'.join(failures))
        return 1
    print('生命周期执行门通过：完整覆盖，启动/运行/关闭均零 Python 帧')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
