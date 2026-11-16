#!/bin/sh
# 构建 CPython 3.12.13 严格零字节码内核专属 shim（libryupy_cpython_strict）。
# 仅使用携带 Include/python3.12/ryupy_kernel.h 的严格内核前缀，输出到仓库外指定目录。
set -eu
if [ "$#" -lt 2 ]; then
  echo "用法: $0 <strict_kernel_prefix> <output_dir> [python_version]" >&2
  exit 1
fi
python_home=$1
output=$2
py_version=${3:-3.12}

kernel_header="$python_home/include/python$py_version/ryupy_kernel.h"
if [ ! -f "$kernel_header" ]; then
  echo "选中前缀缺少严格内核头文件: $kernel_header" >&2
  exit 1
fi

mkdir -p "$output"
script_dir=$(cd "$(dirname "$0")" && pwd)

case "$(uname -s)" in
  Darwin)
    cc -dynamiclib -O2 -Wall -Wextra -Werror \
      -I"$python_home/include/python$py_version" -I"$python_home/include" \
      "$script_dir/ryupy_cpython_strict.c" \
      -L"$python_home/lib" -l"python$py_version" \
      -Wl,-rpath,@loader_path/python/lib \
      -o "$output/libryupy_cpython_strict.dylib"
    ;;
  Linux)
    so_lib=""
    for candidate in "$python_home/lib/libpython$py_version.so.1.0" "$python_home/lib/libpython$py_version.so"; do
      if [ -f "$candidate" ]; then
        so_lib="$candidate"
        break
      fi
    done
    if [ -z "$so_lib" ]; then
      echo "选中严格内核前缀缺少共享库 libpython$py_version" >&2
      exit 1
    fi
    cc -shared -fPIC -O2 -Wall -Wextra -Werror \
      -I"$python_home/include/python$py_version" -I"$python_home/include" \
      "$script_dir/ryupy_cpython_strict.c" \
      "$so_lib" \
      '-Wl,-rpath,$ORIGIN/python/lib' \
      -o "$output/libryupy_cpython_strict.so"
    ;;
  *)
    echo "build_strict.sh 目前仅支持 Darwin 与 Linux（严格内核已限定 3.12.13）" >&2
    exit 1
    ;;
esac
