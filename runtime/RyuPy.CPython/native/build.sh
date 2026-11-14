#!/bin/sh
# 使用指定发行的头文件与libpython构建shim，输出始终由调用方指定到仓库外/构建缓存。
set -eu
if [ "$#" -lt 2 ]; then
  echo "用法: $0 <python_home> <output_dir>" >&2
  exit 1
fi
python_home=$1
output=$2
mkdir -p "$output"
script_dir=$(cd "$(dirname "$0")" && pwd)

case "$(uname -s)" in
  Darwin)
    cc -dynamiclib -O2 -Wall -Wextra -Werror \
      -I"$python_home/include/python3.12" -I"$python_home/include" \
      "$script_dir/ryupy_cpython.c" \
      -L"$python_home/lib" -lpython3.12 \
      -Wl,-rpath,@loader_path/python/lib \
      -o "$output/libryupy_cpython.dylib"
    ;;
  Linux)
    cc -shared -fPIC -O2 -Wall -Wextra -Werror \
      -I"$python_home/include/python3.12" -I"$python_home/include" \
      "$script_dir/ryupy_cpython.c" \
      "$python_home/lib/libpython3.12.so.1.0" \
      '-Wl,-rpath,$ORIGIN/python/lib' \
      -o "$output/libryupy_cpython.so"
    ;;
  MINGW*|MSYS*|CYGWIN*)
    if [ -f "$script_dir/build.cmd" ]; then
      cmd.exe /c "$script_dir\\build.cmd" "$python_home" "$output"
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
