# 本轮命令

```bash
source /workspace/shared/toolchains/activate.sh
export XDG_DATA_HOME=/workspace/shared/toolchains/data XDG_CACHE_HOME=/workspace/shared/toolchains/cache
ROOT=/workspace/shared/RyuPy
D=/tmp/ryupy-m3_9-full-suite-eGJqTT
PY=/workspace/shared/toolchains/data/uv/python/cpython-3.12.13-linux-x86_64-gnu/bin/python3.12
cd "$ROOT"
cargo build --release -p ryupy-cli  # 保存为 $D/ryupy-current，不覆盖历史CLI
# 历史CLI来自 /tmp/ryupy-m3_9-accumulator-baseline-e1c1ee1-B0aEmS/ryupy-baseline
"$PY" -u "$D/preparation/run_saved_suite.py" --root "$ROOT" --cli "$D/ryupy-current" --runtime "$D/RyuPy.Runtime.dll" --out "$D/current-default" --python "$PY" --tier default --cli-profile release
"$D/run-remaining.sh"  # e1-default, e1-disabled, current-disabled，均1warmup+3run
"$PY" -u "$D/run_paired_suite.py" --root "$ROOT" --run "$D"
"$D/build-core.sh"  # 只构建/tmp harness；初次stdout捕获错误及修正记录在README
"$PY" -u "$D/preparation/run_core_kernels.py" --root "$ROOT" --baseline "$D/e1-default" --candidate "$D/current-default" --harness "$D/preparation/core-harness/bin/Release/net10.0/core.dll" --python "$PY" --expected "$D/expected" --out "$D/core-kernels"
"$PY" -u "$D/collect_core_jit.py" "$D"
python3 "$D/summarize_suite.py" "$D"
python3 "$D/summarize_paired.py" "$D"
python3 "$D/summarize_kernels.py" "$D"
```

所有输出目录首次创建，不覆盖旧证据。上述目录已存在时，复现应换一个全新D，并按来源说明保存CLI/同runtime；二进制不入仓。每个运行进程实际command另保raw JSON；编译命令另保build-*.json。
