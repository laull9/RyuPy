"""固定新dev的records归因；原始输出在仓库外，绝不改产品。"""
import argparse, ast, hashlib, itertools, json, os, shutil, subprocess, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from benchlib import measure, suite
P = argparse.ArgumentParser()
P.add_argument('--product', type=Path, required=True)
P.add_argument('--cli', type=Path, required=True)
P.add_argument('--out', type=Path, required=True)
P.add_argument('--round', type=int, required=True)
a = P.parse_args(); a.out.mkdir(parents=True)
assert not a.out.resolve().is_relative_to(ROOT)
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=a.product,text=True).strip() == 'a1419ac5c41376e984969fea1eda435acc672a5e'
assert sys.version_info[:3] == (3,12,13)
original=a.product/'tests/benchmarks/records.py'; phase=Path(__file__).with_name('phases.py')
# 函数和类体逐AST相同，不以删减算法产生阶段收益。
def definitions(p):
    return [ast.dump(x,include_attributes=False) for x in ast.parse(p.read_text()).body if isinstance(x,(ast.FunctionDef,ast.ClassDef))]
class RemoveInstrumentation(ast.NodeTransformer):
    def visit_Assign(self,node):
        if len(node.targets)==1 and isinstance(node.targets[0],ast.Name) and node.targets[0].id.startswith('_phase_'): return None
        return self.generic_visit(node)
    def visit_Expr(self,node):
        if isinstance(node.value,ast.Call) and isinstance(node.value.func,ast.Attribute) and isinstance(node.value.func.value,ast.Name) and node.value.func.value.id=='phase_times': return None
        return self.generic_visit(node)
clean=RemoveInstrumentation().visit(ast.parse(phase.read_text()))
assert definitions(original)==[ast.dump(x,include_attributes=False) for x in clean.body if isinstance(x,(ast.FunctionDef,ast.ClassDef))]
env=measure._base_env(); env.pop('RYUPY_RUNTIME_DIR',None); os.environ.pop('RYUPY_RUNTIME_DIR',None)
rows=[]
profile_args=None
cleanup=[]
def semantic(data, phased):
    lines=data.splitlines(keepends=True); phases=[]; output=[]
    for line in lines:
        if line.startswith(b'PHASE '):
            _,name,value=line.split(); value=float(value); assert 0<=value<3600
            phases.append({'name':name.decode(),'seconds':value})
        else: output.append(line)
    assert [p['name'] for p in phases]==(['aggregate','sort','report']*4 if phased else [])
    return b''.join(output),phases
for name,source in [('records',original),('phases',phase)]:
    directory=a.out/name; directory.mkdir(); shutil.copyfile(source,directory/'source.py')
    (directory/'source.sha256').write_text(hashlib.sha256(source.read_bytes()).hexdigest())
    for emit in ['mir','cil']:
        command=[str(a.cli),'dump',str(source),'--emit',emit,'--native']
        r=subprocess.run(command,cwd=a.product,capture_output=True)
        (directory/(emit+'-command.json')).write_text(json.dumps({'command':command,'cwd':str(a.product),'exit':r.returncode}))
        (directory/(emit+'.txt')).write_bytes(r.stdout); (directory/(emit+'.stderr')).write_bytes(r.stderr); assert r.returncode==0
    # 逐case源码与发射保存供独审；静态标记不当作动态命中。
    bench=suite.Benchmark(name,source)
    commands={'CP':[sys.executable,'-B',str(source)]}
    for backend in ['jit','aot']:
        exe,_,error,status=measure.build_case(a.product,a.cli,bench,directory/backend,backend=backend,native=True,command_evidence=directory/(backend+'-command.json'))
        assert exe is not None,(status,error)
        commands[backend.upper()]=([os.environ['DOTNET_HOST_PATH'],str(exe)] if backend=='jit' else [str(exe)])
        if backend=='jit':
            files=[exe,*exe.parent.glob('*.runtimeconfig.json'),*exe.parent.glob('RyuPy.Runtime.dll')]
            (directory/'jit-hashes.json').write_text(json.dumps({str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in files},indent=2))
            for p in exe.parent.glob('*.runtimeconfig.json'): shutil.copyfile(p,directory/p.name)
        if backend=='aot':
            manifests=list(exe.parent.rglob('*.json')); found=[]
            for path in manifests:
                try: m=json.loads(path.read_text())
                except (ValueError,UnicodeError): continue
                if isinstance(m,dict) and m.get('kind')=='ryupy-native-aot-artifact': found.append((path,m))
            assert len(found)==1
            path,m=found[0]; assert m['toolchain']['sdk_version']=='10.0.403'
            options=m['compiler']['options']
            assert not options['experimental_records_increment'] and not options['experimental_direct_accumulator']
            shutil.copyfile(path,directory/'manifest.json')
            (directory/'native.sha256').write_text(hashlib.sha256(exe.read_bytes()).hexdigest())
    oracle=measure.run_once(commands['CP'],env,a.product,None)
    assert oracle.exit_code==0 and not oracle.stderr
    expected,_=semantic(oracle.stdout,name=='phases')
    if name=='records': anchor=expected
    else: assert expected==anchor
    for iteration in range(12):
        order=list(itertools.permutations(commands))[iteration%6]
        if a.round%2: order=tuple(reversed(order))
        for arm in order:
            result=measure.run_once(commands[arm],env,a.product,None)
            stem=directory/f'{iteration}-{arm}'
            stem.with_suffix('.stdout').write_bytes(result.stdout); stem.with_suffix('.stderr').write_bytes(result.stderr)
            actual,phases=semantic(result.stdout,name=='phases')
            assert result.exit_code==0 and result.stderr==b'' and actual==expected
            rows.append({'round':a.round,'case':name,'iteration':iteration,'arm':arm,'order':order,'wall_ms':result.wall_ms,'max_rss_bytes':result.max_rss_bytes,'phases':phases})
            (a.out/'rows.json').write_text(json.dumps(rows,indent=2))
    for arm in ['JIT','AOT']:
        result=measure.run_once(commands[arm],env,a.product,'RYUPY_STATS')
        actual,phases=semantic(result.stdout,name=='phases')
        assert result.exit_code==0 and result.stderr==b'' and actual==expected
        assert {'alloc_bytes','main_ms'}<=result.stats.keys()
        (directory/(arm+'-stats.json')).write_text(json.dumps(result.stats,indent=2))
        (directory/(arm+'-stats.stdout')).write_bytes(result.stdout)
    trace_env=env | {'COREHOST_TRACE':'1','COREHOST_TRACEFILE':str(directory/'jit-host-trace.txt')}
    traced=measure.run_once(commands['JIT'],trace_env,a.product,None)
    traced_output,_=semantic(traced.stdout,name=='phases')
    assert traced.exit_code==0 and traced.stderr==b'' and traced_output==expected
    assert (directory/'jit-host-trace.txt').stat().st_size>0
    if name=='records':
        profile_args=(Path(commands['AOT'][0]),commands['CP'],expected,directory/'profile',env,a.product)
    cleanup.extend([directory/'jit',directory/'aot'])
assert len(rows)==72
# 两个输入的全部正式计时/统计/trace完成后才运行可选采样。
from profile_native import profile
try:
    profile(*profile_args)
except (FileNotFoundError, subprocess.TimeoutExpired) as error:
    for stream in ['stdout','stderr']:
        raw=getattr(error,stream,None)
        if raw: (a.out/('profile-failure.'+stream)).write_bytes(raw if isinstance(raw,bytes) else raw.encode())
    (a.out/'profile-optional-failure.json').write_text(json.dumps({'kind':type(error).__name__,'error':str(error)},indent=2))
for directory in cleanup:
    shutil.rmtree(directory)
