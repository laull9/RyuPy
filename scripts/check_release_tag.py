"""版本矩阵入口守卫：仅接受已进入main且与当前检出一致的v*推送tag。"""
import os
import subprocess
import sys


def verify(environment, directory=None):
    ref = environment.get('GITHUB_REF', '')
    if environment.get('GITHUB_EVENT_NAME') != 'push' or not ref.startswith('refs/tags/v'):
        raise ValueError('三平台发布验收只接受v* tag的push事件')

    def revision(value):
        return subprocess.check_output(['git', 'rev-parse', '--verify', value], cwd=directory, text=True).strip()

    head = revision('HEAD^{commit}')
    tag = revision(ref + '^{commit}')
    if head != tag:
        raise ValueError('当前检出与版本tag提交不一致')
    revision('refs/remotes/origin/main^{commit}')
    result = subprocess.run(['git', 'merge-base', '--is-ancestor', tag, 'refs/remotes/origin/main'], cwd=directory)
    if result.returncode == 1:
        raise ValueError('版本tag提交尚未进入main，三平台验收已阻止')
    result.check_returncode()
    return ref.removeprefix('refs/tags/')


if __name__ == '__main__':
    try:
        print('main版本tag已确认：' + verify(os.environ))
    except (ValueError, subprocess.CalledProcessError) as error:
        sys.exit(str(error))
