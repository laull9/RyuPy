#!/bin/sh
# 使用指定发行的头文件与libpython构建shim，支持 CPython 3.9+ 动态版本探测。
# 输出始终由调用方指定到仓库外/构建缓存。
set -eu
if [ "$#" -lt 2 ]; then
  echo "用法: $0 <python_home> <output_dir> [python_version]" >&2
  exit 1
fi
python_home=$1
output=$2
py_version=${3:-}

if [ -z "$py_version" ]; then
  # 优先从 include 目录探测 python3.x
  for inc in "$python_home"/include/python3.*; do
    if [ -d "$inc" ]; then
      py_version=$(basename "$inc" | sed 's/python//')
      break
    fi
  done
fi

if [ -z "$py_version" ]; then
  # 其次从 lib 目录查找 libpython3.x 动态库
  for lib in "$python_home"/lib/libpython3.*.dylib "$python_home"/lib/libpython3.*.so*; do
    if [ -f "$lib" ]; then
      py_version=$(basename "$lib" | sed -E 's/libpython([0-9]+\.[0-9]+).*/\1/')
      break
    fi
  done
fi

if [ -z "$py_version" ]; then
  # 默认回退 3.12
  py_version="3.12"
fi

mkdir -p "$output"
script_dir=$(cd "$(dirname "$0")" && pwd)

case "$(uname -s)" in
  Darwin)
    cc -dynamiclib -O2 -Wall -Wextra -Werror \
      -I"$python_home/include/python$py_version" -I"$python_home/include" \
      "$script_dir/ryupy_cpython.c" \
      -L"$python_home/lib" -l"python$py_version" \
      -Wl,-rpath,@loader_path/python/lib \
      -o "$output/libryupy_cpython.dylib"
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
      echo "选中发行缺少共享库 libpython$py_version" >&2
      exit 1
    fi
    cc -shared -fPIC -O2 -Wall -Wextra -Werror \
      -I"$python_home/include/python$py_version" -I"$python_home/include" \
      "$script_dir/ryupy_cpython.c" \
      "$so_lib" \
      '-Wl,-rpath,$ORIGIN/python/lib' \
      -o "$output/libryupy_cpython.so"
    ;;
  MINGW*|MSYS*|CYGWIN*)
    if [ -f "$script_dir/build.cmd" ]; then
      cmd.exe /c "$script_dir\\build.cmd" "$python_home" "$output" "$py_version"
    else
      echo "Windows 环境请在 cmd/pwsh 中运行 build.cmd 或 build.ps1" >&2
      exit 1
    fi
    ;;
  *)
    echo "未知系统类型 $(uname -s)，支持 Darwin、Linux 或在 Windows 下使用 build.cmd/build.ps1" >&2
    exit 1
    ;;
esac
