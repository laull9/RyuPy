"""原重绑定fixture必须先对固定dev实测；候选关闭feature不冒充历史基线。"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT/'tests'))
from benchlib import measure, suite

DEV = '81758bb477729aee4636f2ca6d6b0815b949849a'
ORIGINAL = '4d831f4de3ff10033bd63e774067a6f82980e283'
UNSUPPORTED = (1, b'', b"NotImplementedError: builtin fallback for unbound module name 'sum' is not supported\n")


def classify(cp, dev, off, on):
    assert cp[0] == 0 and cp[1] and cp[2] == b'', '固定CP原输入必须成功且有输出'
    if dev == cp:
        assert off == cp and on == cp, 'dev支持而候选回归，不得归入known'
        return {'classification':'cp-supported','unsupported_cases':0}
    assert dev == UNSUPPORTED, '固定dev不是登记的精确失败，禁止扩大known'
    assert off == dev and on == dev, '候选新增差异或修复，必须重新分类'
    return {'classification':'baseline-exact-unsupported','unsupported_cases':1}


def main():
    p=argparse.ArgumentParser()
    for key in ('output','python','dev','off','on','dev-source'):p.add_argument('--'+key,type=Path,required=True)
    args=p.parse_args();output=args.output.resolve();assert not output.is_relative_to(ROOT)
    output.mkdir(parents=True,exist_ok=False)
    report={'ok':False,'fixed_dev':DEV,'scope':'original-fixture CP/dev/off/on JIT; not product supported'}
    try:
        actual=subprocess.check_output(['git','rev-parse','HEAD'],cwd=args.dev_source,text=True).strip()
        assert actual == DEV
        assert subprocess.check_output([str(args.python),'-c','import platform;print(platform.python_version())'],text=True).strip()=='3.12.13'
        source=ROOT/'tests/fixtures/generator_sum/original_module_rebinding.py'
        original=subprocess.check_output(['git','show',ORIGINAL+':tests/e2e/phase3/m3_generator_sum_fusion.py'],cwd=ROOT)
        assert source.read_bytes()==original, '原始失败输入必须逐字节保留'
        (output/'source-reference.txt').write_text(ORIGINAL+'\n'+DEV+'\n')
        os.environ.pop('RYUPY_RUNTIME_DIR',None);env=measure._base_env();env.pop('RYUPY_RUNTIME_DIR',None)
        def run(label,command):
            r=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,timeout=300)
            for ext,value in [('exit',str(r.returncode).encode()),('stdout',r.stdout),('stderr',r.stderr)]:
                (output/(label+'.'+ext)).write_bytes(value)
            return r.returncode,r.stdout,r.stderr
        outcomes={'CP':run('CP',[str(args.python),'-B',str(source)])};build_failures=[]
        with tempfile.TemporaryDirectory(prefix='generator-fixed-dev-') as temp:
            bench=suite.Benchmark(name=source.stem,path=source)
            for label,cli in [('dev',args.dev),('off',args.off),('on',args.on)]:
                exe,_,error,status=measure.build_case(ROOT,cli,bench,Path(temp)/label,backend='jit')
                (output/(label+'-build.log')).write_text(error)
                if exe is None:build_failures.append([label,status]);continue
                outcomes[label]=run(label,['dotnet',str(exe)])
        assert not build_failures, build_failures
        report.update(classify(outcomes['CP'],outcomes['dev'],outcomes['off'],outcomes['on']))
        report['ok']=True
    finally:
        (output/'classification.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report))


if __name__=='__main__':main()
