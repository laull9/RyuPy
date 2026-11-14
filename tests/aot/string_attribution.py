"""字符串累加归因：固定局部/模块×append/join四输入，复用无统计wall协议。"""
import argparse
from pathlib import Path

from direct_baseline import run

CASES = ('append_module','append_local','join_module','join_local')

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--target',type=Path,required=True);parser.add_argument('--out',type=Path,required=True)
    args=parser.parse_args();root=Path(__file__).resolve().parents[2]
    run(root,args.target.resolve(),args.out.resolve(),cases=CASES,source_directory=root/'tests/aot/string_accum_fixtures',capture_mir=True)
