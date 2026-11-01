#!/bin/bash
# 原26项不删规模；各进程完整raw，由父进程阶段检查后归档。
source /workspace/shared/toolchains/activate.sh
export XDG_DATA_HOME=/workspace/shared/toolchains/data XDG_CACHE_HOME=/workspace/shared/toolchains/cache
D=$(cd "$(dirname "$0")" && pwd)
PY=/workspace/shared/toolchains/data/uv/python/cpython-3.12.13-linux-x86_64-gnu/bin/python3.12
for entry in e1-default:e1-debug:debug:default e1-disabled:e1-debug:debug:disabled current-disabled:current:release:disabled; do
 IFS=: read -r out cli profile tier <<< "$entry"
 "$PY" -u "$D/preparation/run_saved_suite.py" --root /workspace/shared/RyuPy --cli "$D/ryupy-$cli" --runtime "$D/RyuPy.Runtime.dll" --out "$D/$out" --python "$PY" --tier "$tier" --cli-profile "$profile" >"$D/$out.log" 2>&1
 code=$?; printf '%s exit=%s\n' "$out" "$code"
 # 非零继续保其它分母，但实际失败须由代理即时报告并诊断，不自动改生产。
done
