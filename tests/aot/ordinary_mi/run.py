"""四个Legacy MI真实图，PIC开关对照；原三红与新MI AOT拒绝分账，不冒全兼容。"""
import json
import os
import shutil
import sys
import traceback
from pathlib import Path
from contract import context, complete, positive, rejected, original_failure, require

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests/validation'))
sys.path.insert(0,str(ROOT/'tests/aot'))
import markdown_preflight as old


class Runner:
    # 复用既有有界capture；只保存本域工具/PE/原流，不引入另一进程后端。
    def __init__(self):
        self.out=ROOT/'target/ordinary-mi-evidence';self.out.mkdir(exist_ok=False,parents=True)
        self.env=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',PYTHONHASHSEED='0')
        self.python=Path(os.environ['PYTHON']).resolve(strict=True);self.dotnet=Path(os.environ['DOTNET']).resolve(strict=True)
        require(sys.version_info[:3]==(3,12,13) and old.sha(self.python)==old.PYTHON_SHA,'固定CP身份不符')
        self.labels=set();self.frozen={};self.result={'supported_contract_pass':False,'overall_compatibility':False,'stages':{},'original_reds':[],'new_mi_native_supported':False}
        self.save('result.json',self.result)

    def save(self,name,data):old.save(self.out/name,data)
    def freeze(self,paths):
        for p in paths:self.frozen[str(p)]=old.sha(p)
        self.save('frozen.json',self.frozen)
    def barrier(self):
        for p,h in self.frozen.items():require(old.sha(p)==h,'冻结输入变化')
    def command(self,label,args,timeout=180,env=None):
        require(label not in self.labels,'禁止重跑');self.labels.add(label);self.barrier()
        record=old.capture(self.out/label,args,ROOT,timeout,self.env if env is None else env);self.barrier()
        return record
    def checked(self,label,args,timeout=180):
        record=self.command(label,args,timeout);complete(record,0);return record
    def streams(self,label):return ((self.out/(label+'.stdout')).read_bytes(),(self.out/(label+'.stderr')).read_bytes())
    def phase(self,name,fn):
        self.result['stages'][name]={'status':'started'};self.save('result.json',self.result)
        try:fn()
        except Exception:
            self.result['stages'][name]={'status':'failed','error':traceback.format_exc()};raise
        else:self.result['stages'][name]={'status':'complete'}
        finally:self.save('result.json',self.result)


def source_identity(r,label):
    r.checked(label,['git','ls-files','-s','-z']);rows=[]
    for row in (r.out/(label+'.stdout')).read_bytes().split(b'\0'):
        if not row:continue
        left,name=row.split(b'\t');mode,blob,stage=left.decode().split();path=ROOT/os.fsdecode(name)
        require(stage=='0' and path.is_file() and not path.is_symlink(),'非法源索引')
        import hashlib,stat
        data=path.read_bytes();require(hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()==blob and stat.S_IMODE(path.stat().st_mode)==int(mode[-3:],8),'源字节/mode改变')
        rows.append({'path':os.fsdecode(name),'mode':mode,'blob':blob,'sha256':old.sha(path)})
    r.save(label+'-files.json',rows);return rows


def prepare(r):
    r.checked('head',['git','rev-parse','HEAD']);require(r.streams('head')[0].decode().strip()==os.environ['GITHUB_SHA'],'HEAD不同')
    r.before=source_identity(r,'source-before');r.freeze([ROOT/x['path'] for x in r.before])
    r.checked('sdk',[r.dotnet,'--version']);require(r.streams('sdk')[0].strip()==b'10.0.403','SDK版本不同')
    r.checked('runtime-build',[r.dotnet,'build','runtime/RyuPy.Runtime/RyuPy.Runtime.csproj','-c','Release','-nologo'],300)
    r.checked('cli-build',['cargo','build','--locked','-p','ryupy-cli','--features','ryupy-driver/ordinary-dynamic-module-pilot'],1200)
    tools=r.out/'tools';tools.mkdir();r.cli=tools/'ryupy';r.runtime=tools/'RyuPy.Runtime.dll'
    shutil.copy2(ROOT/'target/debug/ryupy',r.cli);shutil.copy2(ROOT/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll',r.runtime)
    r.env.update(RYUPY_RUNTIME_DIR=str(tools),DOTNET=str(r.dotnet),DOTNET_ROOT=str(r.dotnet.parent))
    r.freeze([r.cli,r.runtime,r.python,r.dotnet]);r.fixture=HERE/'fixtures'
    source=json.loads((r.fixture/'SOURCE.json').read_text())
    for row in source['files']:
        p=r.fixture/row['path'];require(p.stat().st_size==row['size'] and old.sha(p)==row['sha256'],'fixture原件变更')


def build(r,name,aot=False):
    target=r.out/('aot-' if aot else 'jit-')/name
    args=[r.cli,'build',r.fixture/(name+'.py'),'--out-dir',target]
    if aot:args.append('--aot')
    label=name+('-aot' if aot else '-build');record=r.command(label,args)
    return label,record,target,target/(name+'.dll')


def cp(r,name):
    label=name+'-cp';record=r.command(label,[r.python,'-B',r.fixture/(name+'.py')],30)
    golden=(r.fixture/(name+'.stdout')).read_bytes();positive(record,*r.streams(label),golden);return golden


def supported(r):
    for name in ('supported_positive','supported_failures','supported_publication','construction'):
        golden=cp(r,name);label,record,target,pe=build(r,name);complete(record,0)
        require(pe.is_file() and old.sha(target/'RyuPy.Runtime.dll')==old.sha(r.runtime),'JIT产物身份不符')
        r.freeze([pe,target/'RyuPy.Runtime.dll'])
        for pic in ('1','0'):
            label=name+'-pic'+pic;record=r.command(label,[r.dotnet,pe],30,dict(r.env,RYUPY_PIC=pic));positive(record,*r.streams(label),golden)
    r.result['four_graphs_pic_pass']=True


def original_reds(r):
    for name,diagnostic in [('original_positive',None),('original_failures','[E0100] Error: 类体内暂不支持：类体内暂不支持该语句'),('original_publication','[E0100] Error: 内建名称 `globals` 尚未实现')]:
        cp(r,name);label,record,target,pe=build(r,name)
        if diagnostic:
            rejected(record,*r.streams(label),diagnostic,any(target.rglob('*.dll')))
            stderr=r.streams(label)[1].decode()
            require(stderr.splitlines()[0]==diagnostic and stderr.count('[E0100]')==1,'原编译红首因变化')
            detail='类体目前支持属性赋值、方法与表达式语句' if name=='original_failures' else '不把标准内建伪装成未知全局变量'
            require(detail in stderr,'原编译红说明变化')
        else:
            complete(record,0);require(pe.is_file() and old.sha(target/'RyuPy.Runtime.dll')==old.sha(r.runtime),'原红JIT PE身份不符');r.freeze([pe,target/'RyuPy.Runtime.dll'])
            label=name+'-jit';record=r.command(label,[r.dotnet,pe],30,dict(r.env,RYUPY_PIC='1'))
            original_failure(record,*r.streams(label),(r.fixture/(name+'.jit.stdout')).read_bytes(),(r.fixture/(name+'.jit.stderr')).read_bytes())
        r.result['original_reds'].append({'name':name,'product_pass':False,'observed_boundary_verified':True});r.save('result.json',r.result)


def aot_boundary(r):
    label,record,target,pe=build(r,'supported_positive',True)
    rejected(record,*r.streams(label),'普通多继承C3当前仅支持Legacy JIT；NativeAOT尚未验证',any(target.rglob('*.dll')))
    r.result['new_mi_aot']={'product_pass':False,'pre_emission_rejection_verified':True}


def main():
    context(os.environ,json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text()));r=Runner()
    try:
        for name,fn in [('prepare',prepare),('supported-legacy',supported),('original-reds',original_reds),('new-mi-aot-negative',aot_boundary)]:r.phase(name,lambda fn=fn:fn(r))
    except Exception:
        r.result['primary_error']=traceback.format_exc()
        raise
    finally:
        try:
            if hasattr(r,'before'):require(source_identity(r,'source-after')==r.before,'全源前后不一致')
            r.result['source_after_verified']=hasattr(r,'before')
        except Exception:
            r.result['source_after_error']=traceback.format_exc()
            if 'primary_error' not in r.result:
                raise
        finally:
            r.result['supported_contract_pass']=bool(r.result.get('four_graphs_pic_pass') and r.result.get('source_after_verified') and r.result.get('new_mi_aot') and not r.result.get('primary_error') and not r.result.get('source_after_error'))
            r.result['original_red_count']=len(r.result['original_reds'])
            r.save('result.json',r.result)


if __name__=='__main__':main()
