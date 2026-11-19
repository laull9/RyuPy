"""生成器归因源合同：三臂保留原脚本函数/循环/规模，不允许用简化算法替代热点。"""
from __future__ import annotations

import ast
from pathlib import Path
import subprocess
import sys
import unittest

ROOT = Path(__file__).resolve().parents[2]
SOURCES = ROOT / "tests/perf/generator_consumers"
ORIGINAL = ROOT / "tests/benchmarks/generators.py"


def tree(path):
    return ast.parse(path.read_text(encoding="utf-8"))


def shape(nodes):
    return ast.dump(ast.Module(body=nodes, type_ignores=[]), include_attributes=False)


class GeneratorConsumerSources(unittest.TestCase):
    def test_three_arms_preserve_original_ast(self):
        # 函数、初始化、消费语句来自原程序；每臂只追加相同的最终print。
        original = tree(ORIGINAL).body
        self.assertEqual(len(original), 7)
        definitions = {
            "ordinary_for": [original[0], original[2], original[3], original[6]],
            "delegated_for": original[:3] + [original[4], original[6]],
            "genexpr_sum": [original[2], original[5], original[6]],
        }
        self.assertEqual({p.stem for p in SOURCES.glob("*.py")}, set(definitions))
        for name, expected in definitions.items():
            with self.subTest(name=name):
                self.assertEqual(shape(tree(SOURCES / f"{name}.py").body), shape(expected))

    def test_three_results_sum_to_original(self):
        # 使用调用此自检的同一个CPython；这只是手写输入自检，不是RyuPy差分。
        def run(path):
            result = subprocess.run([sys.executable, "-B", str(path)],
                                    capture_output=True, text=True, check=True)
            self.assertEqual(result.stderr, "")
            self.assertEqual(len(result.stdout.splitlines()), 1)
            return int(result.stdout)

        self.assertEqual(sum(run(path) for path in sorted(SOURCES.glob("*.py"))), run(ORIGINAL))


if __name__ == "__main__":
    unittest.main()
