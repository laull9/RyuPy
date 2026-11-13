"""透明同步观察原用户runner：保真实命令/返回对象，在销毁资源前核验并保存原件。"""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]/'distribution_verify'))
from common import digest, require, save, tree
from cases import verify
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from aot_layout import reported_aot


class Observer:
    """不替换loader、命令、环境或结果；自身verify用保存的真实run防递归。"""
    def __init__(self, cli, verifier, root, rid, env, marker):
        self.cli = cli; self.verifier = verifier; self.root = root; self.rid = rid
        self.env = env; self.marker = marker; self.real_run = subprocess.run
        self.calls = []; self.observed = {}; self.layout = None

    def __call__(self, *args, **kwargs):
        if kwargs.get('capture_output') is not True or 'cwd' not in kwargs:
            # platform等标准库辅助调用不属于用户runner的run合同，仍原样直通。
            return self.real_run(*args, **kwargs)
        argv = args[0] if args else kwargs['args']
        values = list(map(str, argv)); index = len(self.calls)
        # 将原参数和关键字原样传入，并返回同一个CompletedProcess对象。
        result = self.real_run(*args, **kwargs)
        prefix = self.root/'raw'/'observer-calls'/f'{index:03}'
        prefix.parent.mkdir(parents=True, exist_ok=True)
        prefix.with_suffix('.stdout').write_bytes(result.stdout)
        prefix.with_suffix('.stderr').write_bytes(result.stderr)
        environment = kwargs.get('env') or os.environ
        row = {'argv': values, 'cwd': str(kwargs.get('cwd')), 'exit': result.returncode,
               'environment_subset': {k: v for k, v in environment.items()
                    if k.upper() in {'PATH','PYTHON','PYTHONPATH','PYTHONHOME','PYTHONDONTWRITEBYTECODE',
                        'PYTHONNOUSERSITE','PYTHONOPTIMIZE','DOTNET','DOTNET_ROOT','RYUPY_RUNTIME_DIR',
                        'LD_LIBRARY_PATH','DYLD_LIBRARY_PATH','DYLD_FALLBACK_LIBRARY_PATH'}}}
        self.calls.append(row); save(prefix.with_suffix('.json'), row)
        is_cli = Path(values[0]).resolve() == self.cli.resolve()
        if is_cli and values[1:2] == ['build'] and '--aot' in values:
            output = Path(values[values.index('-o')+1])
            if result.returncode == 0:
                require('original' not in self.observed, '重复成功AOT构建观察')
                self.layout = reported_aot(result.stderr, output, 'app', self.rid)
                self.observe_package('original', self.layout.dist, index)
                work = output.parent
                shutil.copytree(work/'中文 & source', self.root/'raw'/'inputs-before-deletion')
                shutil.copytree(work/'jit', self.root/'raw'/'jit-before-deletion')
                shutil.copytree(self.layout.build, self.root/'sdk'/'successful-build')
            elif self.layout is not None:
                # 用户缺资源失败构建后仍在销毁之前保全两次attempt，不丢失败证据。
                shutil.copytree(self.layout.build, self.root/'sdk'/'after-failed-build')
        if (result.returncode == 0 and Path(values[0]).parent.name == '中文 & relocated'
                and Path(values[0]).name in ('app', 'app.exe')):
            require('relocated' not in self.observed, '重复成功搬迁观察')
            self.observe_package('relocated', Path(values[0]).parent, index)
        return result

    def observe_package(self, label, package, index):
        before = tree(package)
        manifest = json.loads((package/'ryupy-artifact.json').read_text())
        require(manifest['schema_version'] == 9, '必须是真实schema9，不能用旧包替代')
        require(manifest['scope']['declared_resources_complete'] and
                manifest['scope']['automatic_module_locations_complete'], '联合来源/资源声明不完整')
        require(len(manifest['resources']) == 1 and manifest['module_locations'] and
                manifest.get('default_source_plan'), '联合资源/位置/来源计划缺失')
        require(any(s['module_name'] == 'colorsys' for s in manifest['sources']), '缺真实catalog源')
        require(len(manifest['native_bindings']) == 1 and
                manifest['native_bindings'][0]['module'] == 'native_probe' and
                manifest['native_bindings'][0]['linkage'] == 'aot_static', '不是实际C ABI联合包')
        subprocess.run = self.real_run
        try:
            report = verify(self.verifier, package, label, self.root/'raw'/'verify-logs',
                            self.root, self.env, self.marker, True)
        finally:
            subprocess.run = self
        snapshot = self.root/'raw'/(label+'-package')
        shutil.copytree(package, snapshot)
        require(tree(snapshot) == before == tree(package), '观察/复制改变原包或复制不完整')
        self.observed[label] = {'call_index': index, 'actual_package': str(package),
                                'snapshot': str(snapshot), 'verification': report}
        save(self.root/'raw'/'observations.json', self.observed)
