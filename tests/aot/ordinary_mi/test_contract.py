"""仅检查器模型和精确selector回归；不执行编译器/Runtime/CP业务。"""
import ast
import re
from unittest.mock import patch
import copy
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from contract import TITLE, context, positive, rejected, original_failure
ROOT=Path(__file__).resolve().parents[3]


def record(code=0):return {'exit':code,'failure':None,'raw_streams_complete':True}


class Contracts(unittest.TestCase):
    def test_positive_exact_both_streams(self):
        positive(record(),b'ok',b'',b'ok')
        for args in [(record(),b'bad',b'',b'ok'),(record(),b'ok',b'warning',b'ok'),(record(1),b'ok',b'',b'ok')]:
            with self.assertRaises(ValueError):positive(*args)
    def test_compile_rejection_never_crash_or_partial(self):
        for code in (-11,-6,134,139,2,0):
            with self.assertRaises(ValueError):rejected(record(code),b'',b'[E0100] guard','guard',False)
        rejected(record(1),b'',b'[E0100] guard','guard',False)
        for change in [{'failure':'timeout'},{'raw_streams_complete':False}]:
            r=record(1);r.update(change)
            with self.assertRaises(ValueError):rejected(r,b'',b'[E0100] guard','guard',False)
        for stdout,stderr,pe in [(b'x',b'[E0100] guard',False),(b'',b'[E0100] panic guard',False),(b'',b'[E0100] guard',True),(b'',b'[E0100] other',False)]:
            with self.assertRaises(ValueError):rejected(record(1),stdout,stderr,'guard',pe)
    def test_original_red_is_exact_failure_not_success(self):
        original_failure(record(1),b'prefix',b'AttributeError',b'prefix',b'AttributeError')
        for r in (record(),record(-11)):
            with self.assertRaises(ValueError):original_failure(r,b'prefix',b'AttributeError',b'prefix',b'AttributeError')
    def test_context(self):
        env={'GITHUB_ACTIONS':'true','GITHUB_EVENT_NAME':'push','RUNNER_ENVIRONMENT':'github-hosted','GITHUB_RUN_ATTEMPT':'1','GITHUB_REF':'refs/heads/ci/wcwidth-legacy-aot-2909056','GITHUB_SHA':'a'*40}
        event={'ref':env['GITHUB_REF'],'after':env['GITHUB_SHA'],'head_commit':{'id':env['GITHUB_SHA'],'message':TITLE}}
        context(env,event)
        for key,value in [('GITHUB_RUN_ATTEMPT','2'),('GITHUB_REF','refs/heads/main'),('RUNNER_ENVIRONMENT','self-hosted')]:
            bad=dict(env);bad[key]=value
            with self.assertRaises(ValueError):context(bad,event)
        event['head_commit']['message']=TITLE.upper()
        with self.assertRaises(ValueError):context(env,event)
    def test_selector_exact_and_independent(self):
        workflow=(ROOT/'.github/retired-workflows/wcwidth-legacy-aot.yml').read_text()
        lines=workflow.splitlines();start=next(i for i,s in enumerate(lines) if "python3 - <<'PY'" in s)+1;end=next(i for i in range(start,len(lines)) if lines[i].strip()=='PY')
        code='\n'.join(s[10:] for s in lines[start:end])
        for title,ref,expected in [(TITLE,'ci/wcwidth-legacy-aot-2909056',True),(TITLE,'main',False),(TITLE.upper(),'ci/wcwidth-legacy-aot-2909056',False),(TITLE+' ','ci/wcwidth-legacy-aot-2909056',False),('validation: default-source-aot-001','ci/wcwidth-legacy-aot-2909056',False)]:
            with tempfile.TemporaryDirectory() as tmp:
                p=Path(tmp);(p/'event').write_text(json.dumps({'ref':'refs/heads/'+ref,'head_commit':{'message':title}}))
                env=dict(os.environ,GITHUB_EVENT_PATH=str(p/'event'),GITHUB_OUTPUT=str(p/'out'),GITHUB_EVENT_NAME='push',GITHUB_REF='refs/heads/'+ref)
                result=subprocess.run([os.sys.executable,'-B','-c',code],env=env,capture_output=True)
                self.assertEqual(result.returncode,0,result.stderr)
                out=(p/'out').read_text();self.assertEqual('ordinary_mi=true' in out,expected)
                if expected:
                    for flag in ('default_source_aot','instance_class','metadata'):self.assertIn(flag+'=true',out)
        self.assertLessEqual(len(lines),600)
        self.assertIn("  ordinary-mi:\n    needs: select-mode\n",workflow)
        self.assertNotIn('needs: ordinary-mi',workflow)
    def test_existing_contexts_and_head_guard(self):
        # 提取实际函数AST执行，避免导入不同目录同名support造成伪接线。
        from contract import require
        import sys
        sys.path.insert(0,str(ROOT/'tests/validation'))
        import typing_noreturn as old
        env={'GITHUB_ACTIONS':'true','GITHUB_EVENT_NAME':'push','RUNNER_ENVIRONMENT':'github-hosted','GITHUB_RUN_ATTEMPT':'1','GITHUB_REF':'refs/heads/ci/wcwidth-legacy-aot-2909056','GITHUB_SHA':'a'*40,'RYUPY_VALIDATION_MODE':'distribution-metadata'}
        event={'ref':env['GITHUB_REF'],'after':env['GITHUB_SHA'],'head_commit':{'id':env['GITHUB_SHA'],'message':TITLE}}
        for relative in ('tests/aot/default_source_aot/run.py','tests/aot/instance_class/run.py','tests/validation/distribution_metadata.py'):
            tree=ast.parse((ROOT/relative).read_text());name='check_context' if 'distribution_metadata' in relative else 'context'
            fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name==name)
            ns={'os':os,'json':json,'Path':Path,'re':re,'require':require,'parse':json.loads,'TITLE':'old-title','old':old,'DATA':{'title':'old-title'}}
            exec(compile(ast.Module(body=[fn],type_ignores=[]),relative,'exec'),ns)
            with tempfile.TemporaryDirectory() as tmp:
                ep=Path(tmp)/'event';ep.write_text(json.dumps(event))
                with patch.dict(os.environ,dict(env,GITHUB_EVENT_PATH=str(ep)),clear=True):
                    if name=='check_context':ns[name](dict(env),event)
                    else:ns[name]()
                    bad=copy.deepcopy(event);bad['head_commit']['message']=TITLE.upper();ep.write_text(json.dumps(bad))
                    with self.assertRaises((ValueError,RuntimeError,AssertionError)):
                        if name=='check_context':ns[name](dict(env),bad)
                        else:ns[name]()
                    if 'instance_class' not in relative:
                        bad=copy.deepcopy(event);bad['head_commit']['id']='b'*40;ep.write_text(json.dumps(bad))
                        with self.assertRaises((ValueError,RuntimeError,AssertionError)):
                            if name=='check_context':ns[name](dict(env),bad)
                            else:ns[name]()
            if 'instance_class' in relative:
                # 旧门HEAD检查在prepare，不冒称其context已核event SHA。
                prep=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='prepare')
                class Dummy:
                    def checked(self,*args):pass
                with tempfile.TemporaryDirectory() as tmp:
                    d=Dummy();d.out=Path(tmp);(d.out/'head.stdout').write_text('b'*40)
                    guard=ast.Module(body=prep.body[:2],type_ignores=[])
                    with patch.dict(os.environ,env,clear=True),self.assertRaises(ValueError):
                        exec(compile(guard,relative,'exec'),{'r':d,'os':os,'require':require})

    def test_reachable_workflow_budget_and_old_dag(self):
        # YAML语法解析另在冻结前实跑；hosted纯门不引入额外PyYAML网络依赖。
        root=ROOT/'.github/retired-workflows';todo=['wcwidth-legacy-aot.yml'];seen=set()
        while todo:
            name=todo.pop()
            if name in seen:continue
            seen.add(name);raw=(root/name).read_text()
            self.assertLessEqual(len(raw.splitlines()),600)
            todo.extend(re.findall(r'uses: \./\.github/workflows/([^\s]+)',raw))
        def needs(raw):
            result={};job=None
            for line in raw.splitlines():
                if re.fullmatch(r'  [a-zA-Z][a-zA-Z0-9_-]*:',line):job=line.strip()[:-1];result[job]=[]
                if line.startswith('    needs:'):result[job].append(line)
            return result
        current=needs((root/'wcwidth-legacy-aot.yml').read_text())
        from hashlib import sha256
        raw=(Path(__file__).parent/'dag_baseline.json').read_bytes()
        self.assertEqual(sha256(raw).hexdigest(),'de10f8bce352ccf52e8ce6e293d7515767bef8ca438f3859de8591a1b23b70c1')
        baseline=json.loads(raw)
        self.assertEqual(baseline['base_commit'],'d0565b8d539ba50cc145e08f49b0f30fa2ff1e2f')
        for name,edges in baseline['needs'].items():self.assertEqual(edges,current[name])

    def test_fixture_manifest_exact(self):
        from hashlib import sha256
        fixture=Path(__file__).parent/'fixtures';manifest=json.loads((fixture/'SOURCE.json').read_text())
        self.assertEqual({p.name for p in fixture.iterdir() if p.name!='SOURCE.json'},{x['path'] for x in manifest['files']})
        for row in manifest['files']:
            b=(fixture/row['path']).read_bytes();self.assertEqual(len(b),row['size']);self.assertEqual(sha256(b).hexdigest(),row['sha256'])


if __name__=='__main__':unittest.main()
