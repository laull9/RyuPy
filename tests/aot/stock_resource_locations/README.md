# provider 与显式资源来源位置

两套手写输入保留独立入口名、资源名及读取方式：`orchard.py` 使用 pathlib 的相邻文件，
`estuary.py` 使用 os.path 与 open 读取子目录。显式 `--resources` 模块锚点必须同时驱动
发布清单与 provider 权威模块字典的 `__file__`；薄函数读取、临时重绑和恢复该键使用
同一字典。没有资源声明时仍采用原 provider bundle 的 kind3 来源；Rust 结构门同时覆盖。
薄函数 `co_filename` 的诊断来源合同保持原值。

```bash
python3 -B tests/aot/stock_resource_locations/run.py \
  --cli /绝对路径/ryupy --python /绝对路径/python3.12
```

runner 同次运行 CPython，发布两个真正 NativeAOT 产品，严格核对 marker、kind2 清单与
物理资源位置，删除原始源码、venv、资源声明及构建树，再从任意工作目录、空 PATH
执行搬迁产品。三流必须逐项一致；每个产品的三个静态薄函数调用与关闭终态也直接核验。
命令、完整三流和宿主报告只写仓外 `~/.cache/ryupy/validation/stock-resource-locations/`。

macOS arm64、CPython 3.12.13、冻结 internal-layout-v5 的两个产品通过，证据目录
`20261117T153147.911475Z`。首次 harness 在执行前比较了 `/var` 与 `/private/var`
两个路径别名而失败，原日志 `20261117T153025.388182Z` 保留；统一规范工作路径后重跑。
