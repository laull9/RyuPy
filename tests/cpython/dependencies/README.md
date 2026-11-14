# 自动依赖闭包

provider schema 1增加可选配置（路径相对provider声明）：

```json
{
  "dependencies": {
    "environment": "./构建 venv",
    "requirements": ["应用包[feature]==1.0"],
    "dynamic_modules": ["应用包.plugins.example"]
  }
}
```

其余字段沿用上级`provider.example.json`。在明确构建环境安装`packaging==25.0`及应用依赖。
入口静态导入自动选择安装发行；Requires-Dist解析传递依赖、版本、extras和环境marker。
动态导入用dynamic_modules或requirements补足；缺失依赖会显示需求链与安装环境。
RECORD负责包数据、metadata、许可证和原生库，平台审计负责原生加载边；包外动态库仍用
既有payloads补足。源码依赖可沿旧modules/payloads声明。运行时无需构建环境。
自动模式须提供干净wheel安装输入，拒绝editable及.pth启动钩子，工具包不会自动进入产品。

固定macOS arm64重跑：

```sh
RUSTC_WRAPPER= cargo build -p ryupy-cli
python3 -B tests/cpython/dependencies/run.py \
  --python /明确路径/python3.12 \
  --distribution /明确路径/PBS20260623 \
  --metadata "$HOME/.cache/ryupy/cpython-provider/pbs-20260623-metadata/python" \
  --numpy-root "$HOME/.cache/ryupy/cpython-provider/numpy-2.3.5-cp312-osx-arm64" \
  --aot
```

驱动创建仓库外临时venv，固定安装构建工具，生成必要的手写wheel输入并安装；NumPy使用已有
2.3.5缓存。测试入口调用业务函数，覆盖extra传递依赖、显式动态插件、中文包资源、metadata版本、
真实NumPy运算与文件副作用。当次固定Python生成oracle，再构建JIT/NativeAOT，移动产品并删除
全部项目、venv、wheel和基础发行输入，污染PYTHONHOME/PYTHONPATH后比较三流和副作用。
缺少传递依赖在构建期失败。默认结束清理全部临时输出。

该驱动使用本机dotnet运行JIT；NativeAOT可执行入口独立运行。Linux/Windows由各自平台验收。

2026-11-14 macOS arm64实跑：上述完整命令加`--aot`退出0，JIT与NativeAOT搬迁的三流和文件
副作用均与同次CPython一致；额外通过缺传递依赖、版本冲突、RECORD越界/文件缺失、错误wheel ABI、
运行时缺资源和缺NumPy原生扩展负例。Rust provider定向测试12项、driver Clippy（`-D warnings`）、
fmt检查通过。测试发现嵌入宿主默认文本编码为ASCII；平台任务增加隔离UTF-8预初始化后，主代理重建shim，
将业务fixture改为默认read_text/write_text，再次完整执行JIT/NativeAOT输入删除搬迁及负例，
退出0，中文资源与文件副作用保持一致。完整make check由主代理在团队交付后统一执行。

主代理已将provider头文件/target triple、inventory标准库目录/扩展后缀、deployment复制目录/
site-packages/library/search_paths/archive_url/certificate_policy接入统一平台布局API。
2026-11-14接线后重新构建CLI，上述完整`--aot`命令退出0。默认中文文本读写复验亦已通过；
Linux/Windows保持各自平台待验状态。

本次实际命令（`$HOME`展开为`/Users/laull`）：

```sh
RUSTC_WRAPPER= cargo build -p ryupy-cli
RUSTC_WRAPPER= cargo test -p ryupy-driver cpython --lib
RUSTC_WRAPPER= cargo clippy -p ryupy-driver --lib -- -D warnings
cargo fmt --all -- --check
python3 -B tests/cpython/dependencies/run.py \
  --python "$HOME/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3.12" \
  --distribution "$HOME/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none" \
  --metadata "$HOME/.cache/ryupy/cpython-provider/pbs-20260623-metadata/python" \
  --numpy-root "$HOME/.cache/ryupy/cpython-provider/numpy-2.3.5-cp312-osx-arm64" --aot
```

2026-11-14异构审查后补充：包内相对导入的产品收集器回归通过；standalone驱动覆盖Windows式
反斜杠RECORD。Oracle显式使用`-X utf8`，与宿主冻结的隔离UTF-8模式一致。
