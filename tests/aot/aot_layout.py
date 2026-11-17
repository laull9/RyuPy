"""活跃验收的公开 AOT 报告解析；不改变冻结历史工具的旧解析器。"""
from dataclasses import dataclass
import os
from pathlib import Path
from manifest_files import plain_stat
from manifest_verify import MANIFEST_NAME
from application_layout import application_layout


@dataclass(frozen=True)
class AotLayout:
    managed: Path
    native: Path
    manifest: Path
    build: Path
    dist: Path
    attempt: Path
    sdk_root: Path

    @property
    def host(self):
        return self.sdk_root/'host'


def reported_aot(stderr, output, stem, rid):
    """只从本次成功报告取得入口；限定当前 managed 所在 attempt 的 SDK 根。"""
    assert stem and Path(stem).name==stem and stem not in ('.','..')
    output=Path(output); plain_stat(output,directory=True)
    lines=stderr.decode('utf-8',errors='strict').splitlines()
    generated=[Path(line[len('已生成 '):]) for line in lines if line.startswith('已生成 ')]
    intermediate=[Path(line[len('CIL 中间产物：'):]) for line in lines if line.startswith('CIL 中间产物：')]
    # 新CLI只报告一个公开入口；中间输入显式列出，旧冻结报告保留独立解析。
    assert (len(generated),len(intermediate)) in ((1,1),(2,0)), '必须报告唯一入口与明确CIL输入'
    managed=[p for p in intermediate or generated if p.name==stem+'.dll']
    entry_name = stem + ('.exe' if rid == 'win-x64' else '.bin' if stem.lower() == 'internal' else '')
    native=[p for p in generated if p.name==entry_name]
    assert len(managed)==len(native)==1 and managed[0]!=native[0]
    managed,native=managed[0],native[0]
    manifests=[Path(line[len('AOT 清单：'):]) for line in lines if line.startswith('AOT 清单：')]
    assert len(manifests)<=1
    # 无诊断选项时，标记协议已明确清单位置；不递归搜索或挑选历史attempt。
    manifest=manifests[0] if manifests else application_layout(
        output/(stem+'.dist'), require_canonical=True).manifest
    for path in (managed,native,manifest):
        assert path.is_absolute(); plain_stat(path)
    build=output/(stem+'.build'); dist=output/(stem+'.dist')
    for directory in (build,dist): plain_stat(directory,directory=True)
    assert native.name==entry_name
    assert os.path.samefile(native.parent,dist)
    layout=application_layout(dist, require_canonical=True)
    layout.check_public_root(native)
    assert manifest.name==MANIFEST_NAME and os.path.samefile(manifest,layout.manifest)
    assert managed.name==stem+'.dll'
    # 按真实父目录身份向上核归属，不裁剪 Windows 扩展前缀，也不挑旧 attempt。
    current=managed.parent; chain=[]
    while not os.path.samefile(current,build):
        plain_stat(current,directory=True); chain.append(current)
        assert len(chain)<=4 and current!=current.parent, 'managed不属于本次build'
        current=current.parent
    assert len(chain) in (1,3), 'managed必须属于普通或binding attempt'
    attempt=chain[-1]
    assert attempt.name.startswith('attempt-')
    if len(chain)==3:
        assert chain[0].name=='bundle' and chain[1].name.startswith('ryupy-binding-')
    # 当前目录至多存在一个实际 SDK 工作根；不遍历 build 历史或旧 backup。
    roots=[p for p in managed.parent.iterdir() if p.name.startswith('ryupy-aot-')]
    assert len(roots)==1, 'SDK工作根必须唯一'
    sdk_root=roots[0]; plain_stat(sdk_root,directory=True)
    for path in (sdk_root/'host',sdk_root/'publish'): plain_stat(path,directory=True)
    return AotLayout(managed,native,manifest,build,dist,attempt,sdk_root)
