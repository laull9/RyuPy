"""只分块已完成性能实验的原始 ZIP 字节；不解包、不重压缩、不重跑性能。"""
import hashlib
import json
import os
from pathlib import Path
import urllib.error
import urllib.request
from urllib.parse import urlparse

RUN_ID = 43372683973
PART_SIZE = 480 * 1024 * 1024
ARTIFACTS = {
    'a': {'id': 14013497465, 'size': 1462090829,
          'sha256': 'c45344ae95628505c4e5aca9a4553b2fbc94f93da0c7608b9b79b41193358024'},
    'b': {'id': 14012969781, 'size': 1169671686,
          'sha256': '0105fcc850d1159525454bb7e4c889d0eb5dda4213daea716ddd6f25f24dfb22'},
}


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def requests_for_artifact(artifact_id, token):
    """授权只附于固定 GitHub API 请求，不复用于下载请求。"""
    if artifact_id not in {v['id'] for v in ARTIFACTS.values()}:
        raise ValueError('未知原件 ID')
    return urllib.request.Request(
        f'https://api.github.com/repos/laull9/RyuPy/actions/artifacts/{artifact_id}/zip',
        headers={'Authorization': 'Bearer '+token, 'Accept': 'application/vnd.github+json',
                 'X-GitHub-Api-Version': '2022-11-28'})


def cdn_request(location):
    parsed = urlparse(location)
    # 不接受任意重定向主机、用户信息、端口或非 HTTPS；完整 URL 只留在内存中。
    if (parsed.scheme != 'https' or not parsed.hostname
            or not parsed.hostname.endswith('.blob.core.windows.net')
            or parsed.username is not None or parsed.password is not None
            or parsed.port not in (None, 443) or parsed.fragment):
        raise ValueError('非预期原件下载目的地')
    return urllib.request.Request(location)


def open_artifact(artifact_id, token):
    # 两次请求均禁止自动跳转，避免 Authorization 或临时签名跨目的地传播。
    opener = urllib.request.build_opener(NoRedirect())
    try:
        response = opener.open(requests_for_artifact(artifact_id, token), timeout=120)
    except urllib.error.HTTPError as error:
        try:
            if error.code not in (301, 302, 303, 307, 308):
                raise ValueError('GitHub 未返回原件跳转') from None
            location = error.headers.get('Location')
            if not location:
                raise ValueError('原件跳转缺失')
        finally:
            error.close()
    else:
        response.close()
        raise ValueError('GitHub 未返回预期跳转')
    return opener.open(cdn_request(location), timeout=120)


def split_stream(stream, output, expected_size, expected_sha256, record, part_size=PART_SIZE):
    """单遍限量读取；原件校验失败时保留分块，但绝不标记 verified。"""
    if part_size <= 0:
        raise ValueError('分块尺寸必须为正')
    output.mkdir(exist_ok=False)
    record.update(verified=False, parts=[], observed_size=0)
    whole = hashlib.sha256()
    while True:
        first = stream.read(min(1024 * 1024, part_size))
        if not first:
            break
        path = output/f'part-{len(record["parts"]):03}.bin'
        part = hashlib.sha256(); size = 0
        with path.open('xb') as target:
            data = first
            while data:
                target.write(data); part.update(data); whole.update(data)
                size += len(data); record['observed_size'] += len(data)
                if record['observed_size'] > expected_size:
                    raise ValueError('下载超过锁定原件长度')
                if size == part_size:
                    break
                data = stream.read(min(1024 * 1024, part_size-size))
        record['parts'].append({'file': str(path.relative_to(output.parent)), 'size': size,
                                'sha256': part.hexdigest()})
    record['observed_sha256'] = whole.hexdigest()
    if record['observed_size'] != expected_size or whole.hexdigest() != expected_sha256:
        raise ValueError('原 ZIP 长度或摘要不匹配')
    record['verified'] = True


def main():
    if os.environ.get('GITHUB_ACTIONS') != 'true':
        raise ValueError('仅用于隔离 CI 证据转存')
    token = os.environ.get('GH_TOKEN')
    if not token:
        raise ValueError('缺少 GitHub job 授权')
    base = Path(os.environ['RUNNER_TEMP']).resolve()/'direct-entry-build-evidence'
    base.mkdir(exist_ok=False)
    index = {'schema_version': 1, 'original_run_id': RUN_ID, 'verified': False,
             'scope': '原ZIP连续字节分块，按parts顺序拼接恢复原件；未重新执行性能实验',
             'part_size': PART_SIZE, 'artifacts': {}}
    try:
        for key, lock in ARTIFACTS.items():
            row = {'original_artifact_id': lock['id'], 'original_run_id': RUN_ID,
                   'expected_size': lock['size'], 'expected_sha256': lock['sha256'], 'verified': False}
            index['artifacts'][key] = row
            with open_artifact(lock['id'], token) as stream:
                split_stream(stream, base/key, lock['size'], lock['sha256'], row)
            if len(row['parts']) != 3:
                raise ValueError('分块数不符合冻结布局')
        index['verified'] = True
    except Exception as error:
        # HTTP 错误文本可能含临时 URL；只记录异常类型，不打印第三方异常内容。
        index['failure_type'] = type(error).__name__
        raise RuntimeError('证据分块未完成；见未验证 index') from None
    finally:
        (base/'index.json').write_text(json.dumps(index, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    print('两份原 ZIP 摘要通过，六个连续字节分块已生成')


if __name__ == '__main__':
    main()
