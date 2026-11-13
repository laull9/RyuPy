#!/bin/sh
# 使用指定发行的头文件与libpython构建shim，输出始终由调用方指定到仓库外/构建缓存。
set -eu
python_home=$1
output=$2
mkdir -p "$output"
case "$(uname -s)" in
  Darwin) cc -dynamiclib -O2 -Wall -Wextra -Werror -I"$python_home/include/python3.12" "$(dirname "$0")/ryupy_cpython.c" -L"$python_home/lib" -lpython3.12 -Wl,-rpath,@loader_path/python/lib -o "$output/libryupy_cpython.dylib" ;;
  Linux) cc -shared -fPIC -O2 -Wall -Wextra -Werror -I"$python_home/include/python3.12" "$(dirname "$0")/ryupy_cpython.c" -L"$python_home/lib" -lpython3.12 '-Wl,-rpath,$ORIGIN/python/lib' -o "$output/libryupy_cpython.so" ;;
  *) echo '当前构建脚本只支持macOS/Linux；Windows需要独立工具链验收' >&2; exit 1 ;;
esac
