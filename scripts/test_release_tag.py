"""使用真实临时Git提交图验证发布守卫，覆盖main祖先、注解tag和未合并分支拒绝。"""
from pathlib import Path
import subprocess
import tempfile
import unittest
from check_release_tag import verify


class ReleaseTagTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix='ryupy-tag-guard-')
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.git('init', '--initial-branch=main')
        self.git('config', 'user.name', '手写发布守卫测试')
        self.git('config', 'user.email', 'fixture@example.invalid')
        self.git('commit', '--allow-empty', '-m', '共同起点')
        self.git('update-ref', 'refs/remotes/origin/main', 'HEAD')
        self.git('tag', 'v1.0.0')
        self.environment = {'GITHUB_EVENT_NAME': 'push', 'GITHUB_REF': 'refs/tags/v1.0.0'}

    def git(self, *arguments):
        return subprocess.check_output(['git', *arguments], cwd=self.root, stderr=subprocess.STDOUT, text=True).strip()

    def test_main_tag_is_accepted(self):
        self.assertEqual(verify(self.environment, self.root), 'v1.0.0')

    def test_annotated_tag_on_main_ancestor_is_accepted(self):
        self.git('tag', '-a', 'v1.0.1', '-m', '注解版本')
        self.git('commit', '--allow-empty', '-m', 'main后续提交')
        self.git('update-ref', 'refs/remotes/origin/main', 'HEAD')
        self.git('checkout', 'v1.0.1')
        self.environment['GITHUB_REF'] = 'refs/tags/v1.0.1'
        self.assertEqual(verify(self.environment, self.root), 'v1.0.1')

    def test_unmerged_dev_tag_is_rejected(self):
        self.git('checkout', '-b', 'dev')
        self.git('commit', '--allow-empty', '-m', '尚未合入main')
        self.git('tag', 'v2.0.0')
        self.environment['GITHUB_REF'] = 'refs/tags/v2.0.0'
        with self.assertRaisesRegex(ValueError, '尚未进入main'):
            verify(self.environment, self.root)

    def test_branch_other_tag_and_manual_event_are_rejected(self):
        for event, ref in [('push', 'refs/heads/main'), ('push', 'refs/tags/M.3.10'),
                           ('workflow_dispatch', 'refs/tags/v1.0.0')]:
            with self.subTest(event=event, ref=ref), self.assertRaisesRegex(ValueError, '只接受'):
                verify({'GITHUB_EVENT_NAME': event, 'GITHUB_REF': ref}, self.root)

    def test_wrong_checkout_is_rejected(self):
        self.git('commit', '--allow-empty', '-m', '其他检出')
        with self.assertRaisesRegex(ValueError, '检出与版本tag'):
            verify(self.environment, self.root)


if __name__ == '__main__':
    unittest.main()
