"""共用既有有界原流采集；命令前后核输入，失败不重试。纯检查不等于产品验收。"""
import os
from contextlib import contextmanager
from pathlib import Path
import shutil
import sys
from checking import require, sha, positive

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tests/validation'))
sys.path.insert(0, str(ROOT / 'tests/aot'))
import markdown_preflight as old
from aot_layout import reported_aot
from native_evidence import preserve_dist, snapshot


class Runner:
    def __init__(self):
        self.out = ROOT / 'target/default-source-aot-evidence'
        self.out.mkdir(parents=True, exist_ok=False)
        self.python = Path(os.environ['PYTHON']).resolve(strict=True)
        self.dotnet = Path(os.environ['DOTNET']).resolve(strict=True)
        require(sys.version_info[:3] == (3, 12, 13), '固定CP3.12.13')
        require(sha(self.python) == '021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7', 'CP身份变化')
        self.env = dict(os.environ, PYTHONDONTWRITEBYTECODE='1', PYTHONHASHSEED='0')
        self.labels, self.frozen = set(), {}
        self.result = {'passed': False, 'default_project_delta': None, 'retries': 0, 'stages': {}}
        self.save('result.json', self.result)

    def save(self, name, data):
        old.save(self.out / name, data)

    def command(self, label, argv, timeout=120, env=None):
        require(label not in self.labels, '禁止重复命令标签')
        self.labels.add(label)
        self.barrier()
        record = old.capture(self.out / label, argv, ROOT, timeout, self.env if env is None else env)
        self.barrier()
        return record

    def checked(self, label, argv, timeout=120):
        record = self.command(label, argv, timeout)
        require(record['exit'] == 0 and not record['failure'] and record['raw_streams_complete'], label + '失败')
        return record

    def run_positive(self, label, argv, golden):
        record = self.command(label, argv, 30,
                              {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC'})
        positive(record, (self.out / (label + '.stdout')).read_bytes(),
                 (self.out / (label + '.stderr')).read_bytes(), golden)

    def freeze(self, paths):
        for path in paths:
            path = Path(path).resolve(strict=True)
            require(path.is_file(), '冻结输入不是普通文件')
            self.frozen[str(path)] = sha(path)
        self.save('frozen-inputs.json', self.frozen)

    def barrier(self):
        for path, digest in self.frozen.items():
            require(sha(path) == digest, '冻结输入改变: ' + path)

    def phase(self, name, function):
        self.result['stages'][name] = {'status': 'started', 'passed': False}
        self.save('result.json', self.result)
        try:
            function()
        except Exception as error:
            self.result['stages'][name].update(status='failed', error=str(error))
            self.save('result.json', self.result)
            raise
        self.result['stages'][name].update(status='complete', passed=True)
        self.save('result.json', self.result)

    def named_test(self, label, argv, name, timeout=900):
        self.checked(label, argv, timeout)
        lines = (self.out / (label + '.stdout')).read_text().splitlines()
        require(sum(line.startswith('test ') and line.endswith(name + ' ... ok') for line in lines) == 1,
                '缺失/重复具名测试: ' + name)

    def inputs(self):
        return old.Runner.inputs(self, self.python)

    def native_layout(self, label, target, entry):
        layout = reported_aot((self.out / (label + '.stderr')).read_bytes(), target, entry.stem, 'linux-x64')
        saved = self.out / (label + '-native'); saved.mkdir()
        preserve_dist(layout.dist, saved)
        with layout.native.open('rb') as stream:
            require(stream.read(4) == b'\x7fELF', '没有实际ELF')
        require(sha(layout.managed.parent / 'RyuPy.Runtime.dll') == sha(self.runtime), 'Native输入runtime变化')
        self.freeze([layout.native, layout.managed, layout.manifest])
        return layout

    def native_pair(self, label, layout, golden, arguments=()):
        before = snapshot(layout.dist)
        self.run_positive(label + '-original', [layout.native, *arguments], golden)
        require(snapshot(layout.dist) == before, '原位运行改变dist')
        moved = self.out / 'relocated' / label
        shutil.copytree(layout.dist, moved)
        require(snapshot(moved) == before, '搬迁丢失文件/身份')
        self.run_positive(label + '-relocated', [moved / layout.native.name, *arguments], golden)
        require(snapshot(moved) == before, '搬迁运行改变dist')

    @contextmanager
    def isolated_sources(self, roots, label):
        """仅暂时搬移本门自己的输入副本；冻结键随物理位置重映射，finally原位还原。"""
        self.barrier()
        saved = dict(self.frozen)
        moved = []
        try:
            for root in roots:
                hidden = root.with_name(root.name + '-isolated-' + label)
                require(root.is_dir() and not hidden.exists(), '隔离位置冲突')
                root.rename(hidden)
                moved.append((root, hidden))
                self.frozen = {str(hidden / Path(p).relative_to(root)) if Path(p).is_relative_to(root) else p: digest
                               for p, digest in self.frozen.items()}
            self.save(label + '-source-isolation.json', {'moves': [[str(a),str(b)] for a,b in moved],
                                                        'restored':False})
            self.barrier()
            yield
        finally:
            for root, hidden in reversed(moved):
                require(not root.exists(), '原输入位置被产品创建，不覆盖')
                hidden.rename(root)
            self.frozen = saved
            self.barrier()
            self.save(label + '-source-isolation.json', {'moves': [[str(a),str(b)] for a,b in moved],
                                                        'restored':True})
