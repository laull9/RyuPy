"""纯观察用户态采样，绝不修改内核安全设置。失败精确记录，不当作样本。"""
import hashlib, json, os, shutil, subprocess
from pathlib import Path

def profile(exe, cp, expected, out, env, cwd):
    out.mkdir(); rows=[]
    # 保留原生文件和已有独立符号，不修改构建选项或二进制。
    files=[exe,*exe.parent.rglob('*.dbg'),*exe.parent.rglob('*.debug')]
    symbols=out/'symbols'; symbols.mkdir()
    index=[]
    for i,path in enumerate(dict.fromkeys(files)):
        dest=symbols/path.relative_to(exe.parent); dest.parent.mkdir(parents=True,exist_ok=True); shutil.copyfile(path,dest)
        index.append({'path':str(path),'saved':dest.name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
    (out/'files.json').write_text(json.dumps(index,indent=2))
    for name,command in [('elf',['readelf','-n',str(exe)]),('sections',['readelf','-S',str(exe)]),('perf-version',['perf','version'])]:
        r=subprocess.run(command,capture_output=True,env=env)
        (out/(name+'.stdout')).write_bytes(r.stdout); (out/(name+'.stderr')).write_bytes(r.stderr)
    (out/'perf_event_paranoid.txt').write_text(Path('/proc/sys/kernel/perf_event_paranoid').read_text())
    for iteration in range(12):
        d=out/str(iteration); d.mkdir()
        oracle=subprocess.run(cp,cwd=cwd,env=env,capture_output=True,timeout=120)
        (d/'cp.stdout').write_bytes(oracle.stdout); (d/'cp.stderr').write_bytes(oracle.stderr)
        assert oracle.returncode==0 and oracle.stdout==expected and not oracle.stderr
        # shell只做stderr重定向并exec，采样目标仍是同一原始AOT文件。
        command=['perf','record','-e','cycles:u','-F','499','--call-graph','dwarf,8192','-o',str(d/'perf.data'),'--','bash','-c','exec "$1" 2>"$2"','_',str(exe),str(d/'program.stderr')]
        (d/'command.json').write_text(json.dumps(command))
        r=subprocess.run(command,cwd=cwd,env=env,capture_output=True,timeout=120)
        (d/'program.stdout').write_bytes(r.stdout); (d/'perf.stderr').write_bytes(r.stderr)
        row={'iteration':iteration,'record_exit':r.returncode}; rows.append(row)
        if r.returncode:
            row['status']='record-blocked-or-failed'; (out/'rows.json').write_text(json.dumps(rows,indent=2))
            assert not (d/'program.stderr').exists(), '目标已启动但非零退出，不能算作perf权限阻碍'
            break
        assert r.stdout==expected and (d/'program.stderr').read_bytes()==b''
        for label,args in [('stacks',['script']),('ips',['script','--no-call-graph','-F','ip']),('report',['report','--stdio','--children','--show-nr-samples'])]:
            command=['perf',*args,'-i',str(d/'perf.data')]
            q=subprocess.run(command,cwd=cwd,env=env,capture_output=True,timeout=120)
            (d/(label+'.txt')).write_bytes(q.stdout); (d/(label+'.stderr')).write_bytes(q.stderr)
            row[label+'_exit']=q.returncode
            if label=='ips' and q.returncode==0:
                ips=q.stdout.decode().splitlines(); row['sample_count']=sum(bool(x.strip()) for x in ips)
        row['status']='recorded'; (out/'rows.json').write_text(json.dumps(rows,indent=2))
    assert hashlib.sha256(exe.read_bytes()).hexdigest()==index[0]['sha256']
