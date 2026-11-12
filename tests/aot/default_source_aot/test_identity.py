"""同源身份纯反例：真实文件mode/blob与push事件，不启动产品。"""
import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
ROOT=Path(__file__).resolve().parents[3]
sys.path[:0]=[str(ROOT/'tests/aot'),str(ROOT/'tests/validation'),
              str(ROOT/'tests/aot/instance_class'),str(ROOT/'tests/aot/wcwidth_ci')]
sys.path.insert(0,str(Path(__file__).resolve().parent))
from run import verify_blob, context, TITLE


class IdentityTests(unittest.TestCase):
    def test_real_blob_and_mode(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'source.py';p.write_bytes(b'x=1\n');p.chmod(0o644)
            oid=hashlib.sha1(b'blob 4\0x=1\n').hexdigest()
            verify_blob(p,'100644',oid)
            p.chmod(0o755);verify_blob(p,'100755',oid)
            with self.assertRaises(ValueError):verify_blob(p,'100644',oid)
            p.write_bytes(b'x=2\n')
            with self.assertRaises(ValueError):verify_blob(p,'100755',oid)
            alias=Path(d)/'alias';alias.symlink_to(p)
            with self.assertRaises(ValueError):verify_blob(alias,'100755',oid)

    def test_exact_event_and_attempt(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'event.json';ref='refs/heads/ci/wcwidth-legacy-aot-2909056';sha='a'*40
            event={'head_commit':{'message':TITLE,'id':sha},'after':sha,'ref':ref}
            env={'GITHUB_EVENT_PATH':str(p),'GITHUB_SHA':sha,'GITHUB_REF':ref,
                 'GITHUB_ACTIONS':'true','GITHUB_RUN_ATTEMPT':'1',
                 'GITHUB_EVENT_NAME':'push','RUNNER_ENVIRONMENT':'github-hosted'}
            p.write_text(json.dumps(event))
            with patch.dict(os.environ,env,clear=True):context()
            for key,value in [('GITHUB_SHA','b'*40),('GITHUB_ACTIONS','false'),
                              ('GITHUB_RUN_ATTEMPT','2'),('GITHUB_EVENT_NAME','workflow_dispatch')]:
                with patch.dict(os.environ,{**env,key:value},clear=True), self.assertRaises(ValueError):context()
            for key in ('after','ref'):
                p.write_text(json.dumps({**event,key:'wrong'}))
                with patch.dict(os.environ,env,clear=True), self.assertRaises(ValueError):context()
            event['head_commit']['id']='b'*40;p.write_text(json.dumps(event))
            with patch.dict(os.environ,env,clear=True), self.assertRaises(ValueError):context()


if __name__=='__main__':unittest.main()
