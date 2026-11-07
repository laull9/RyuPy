"""分离容器镜像准备原流；只按本轮验证的不可变ID运行，禁止隐式拉取。"""
import json
import re

IMAGE = 'mcr.microsoft.com/dotnet/runtime-deps:10.0.14-noble'
INSPECT = ('{"id":{{json .Id}},"repo_digests":{{json .RepoDigests}},'
           '"os":{{json .Os}},"architecture":{{json .Architecture}}}')


def prepare_image(run, docker, work, env=None):
    # pull输出属于准备过程，不过滤或拼入应用程序的stdout/stderr。
    run.run('docker-pull', [docker, 'pull', IMAGE], cwd=work, env=env,
            check=True, timeout=120)
    inspected = run.run('docker-inspect', [docker, 'image', 'inspect', '--format', INSPECT,
                        IMAGE], cwd=work, env=env, check=True, timeout=30)
    if inspected.stderr:
        raise AssertionError('Docker inspect产生非空stderr；见原流')
    record = json.loads(inspected.stdout)
    if (type(record) is not dict or set(record) != {'id', 'repo_digests', 'os', 'architecture'}
            or record['os'] != 'linux' or record['architecture'] != 'amd64'
            or type(record['id']) is not str
            or not re.fullmatch(r'sha256:[0-9a-f]{64}', record['id'])):
        raise AssertionError('Docker镜像身份/平台不符合linux-x64合同')
    digests = record['repo_digests']
    repository = IMAGE.rsplit(':', 1)[0]
    if (type(digests) is not list or not digests
            or any(type(value) is not str or not re.fullmatch(r'[^@\s]+@sha256:[0-9a-f]{64}', value)
                   for value in digests)
            or not any(value.startswith(repository + '@') for value in digests)):
        raise AssertionError('Docker镜像缺少目标repository的有效digest')
    record['requested_tag'] = IMAGE
    (run.logs/'docker-image.json').write_text(json.dumps(record, indent=2)+'\n', encoding='utf-8')
    return record['id']
