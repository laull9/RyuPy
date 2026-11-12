"""公开项目已构建之后的独立审计；oracle不参与产品命令或提供产品构建输入。"""
import json
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
sys.path.insert(0,str(HERE.parent/'standalone_app'))
sys.path.insert(0,str(HERE.parent/'standalone_app/benchmark'))
from measure import capture, require, save
from hosted import ORACLE_TEST, check_oracle_test
from validate_artifact import validate_app_artifact


def audit(layout, project, target, output, cli, runtime, python, dotnet, env):
    oracle=output/'oracle'; output.mkdir()
    entry=project/'package/pyfiglet/__main__.py' if target=='cli' else project/'console_entry.py'
    values=dict(ENTRY=entry,ROOT=project/'package',OUTPUT=oracle,RESOURCES=project/'resources.json',
                METADATA=project/'metadata-request.json',INCLUDE='["pyfiglet","pyfiglet.fonts"]',CLI=cli,RUNTIME=runtime)
    prefix=output/'independent-oracle'
    record=capture(prefix,['cargo','test','--locked','-p','ryupy-driver','--lib','-j','1',ORACLE_TEST,
                         '--','--exact','--ignored','--nocapture'],HERE.parents[2],
                   dict(env,**{'RYUPY_DEFAULT_AOT_'+k:str(v) for k,v in values.items()}),600)
    require(record['exit']==0 and record['failure'] is None,'独立oracle失败')
    check_oracle_test(prefix.with_suffix('.stdout').read_text())
    from measure import sha
    tools={str(p.resolve()):sha(p) for p in (cli,runtime,python,dotnet)}
    receipt=validate_app_artifact(layout,project,tools,case=target,python=python,dotnet=dotnet,
                                 cli=cli,runtime=runtime,oracle_dir=oracle,output=output/'receipt')
    require(receipt['structure_validated'],'独立receipt未通过')
    save(output/'validated.json',{'oracle_after_public_build':True,'receipt':receipt})
    return receipt
