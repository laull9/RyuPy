#!/bin/bash
set -e
source /workspace/shared/toolchains/activate.sh
export XDG_DATA_HOME=/workspace/shared/toolchains/data XDG_CACHE_HOME=/workspace/shared/toolchains/cache
D=$(cd "$(dirname "$0")" && pwd)
mkdir -p "$D/preparation/runtime"
cp "$D/RyuPy.Runtime.dll" "$D/preparation/runtime/"
dotnet build "$D/preparation/core-harness/core.csproj" -c Release >"$D/core-build.log" 2>&1
mkdir "$D/core-validation"
for which in e1 current; do
 folder=e1-default; test "$which" != current || folder=current-default
 for name in fib mandelbrot matmul nbody; do
  dotnet "$D/preparation/core-harness/bin/Release/net10.0/core.dll" "$D/$folder/bench/$name/$name.dll" "$name" "$D/expected/$name.stdout" time 1 1 >"$D/core-validation/$which-$name.stdout" 2>"$D/core-validation/$which-$name.stderr"
  echo "$which $name validation passed"
 done
done
