"""普通MI有限域原流合同；拒绝把崩溃/缺流或原红例当支持通过。"""
import re
TITLE = 'validation: ordinary-mi-c3-001'


def require(value, message):
    if not value: raise ValueError(message)


def complete(record, code):
    require(record['exit'] == code and not record['failure'] and record['raw_streams_complete'], '退出/原流不完整')


def positive(record, stdout, stderr, golden):
    complete(record, 0)
    require(stdout == golden and stderr == b'', 'CP/产品双流不符')


def rejected(record, stdout, stderr, diagnostic, pe_exists):
    complete(record, 1)
    require(not pe_exists and not stdout and b'[E0100]' in stderr and diagnostic.encode() in stderr, '不是指定发射前拒绝')
    require(not re.search(rb'(?i)panic|segmentation|core dumped|fatal|stack overflow',stderr), '崩溃不是NI')


def original_failure(record, stdout, stderr, expected_out, expected_err):
    complete(record, 1)
    require(stdout == expected_out and stderr == expected_err, '原JIT红例变化须独立裁定')


def context(env, event):
    require(env.get('GITHUB_ACTIONS') == 'true' and env.get('GITHUB_EVENT_NAME') == 'push'
            and env.get('RUNNER_ENVIRONMENT') == 'github-hosted' and env.get('GITHUB_RUN_ATTEMPT') == '1'
            and env.get('GITHUB_REF') == event.get('ref') == 'refs/heads/ci/wcwidth-legacy-aot-2909056'
            and bool(re.fullmatch('[0-9a-f]{40}',env.get('GITHUB_SHA','')))
            and event.get('after') == event.get('head_commit',{}).get('id') == env['GITHUB_SHA']
            and event.get('head_commit',{}).get('message') == TITLE, '非精确同源hosted窗口')
