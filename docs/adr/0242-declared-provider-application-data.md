# ADR-0242：声明与编译源码相邻的随包应用数据

- 日期：2026-11-19。
- 状态：实现通过定向验证，完整项目运行另验。
- 接续：[ADR-0239](0239-canonical-internal-application-layout.md)。

## 背景与决定

应用会使用 `Path(__file__)` 读取项目根的版本、配置或图标。只按模块名铺平编译源码会丢失
这种目录关系；复制整棵源码树则会携带未声明的数据、开发配置和不可达源码。原有 AOT
资源声明使用不同的锚点和发布树，不变更其优先级。

在启用 CPython provider 的应用构建中，使用 `--bundle-data <清单>` 显式选择文件：

```json
{
  "schema_version": 1,
  "root": "application",
  "files": [
    {"source": "pyproject.toml"},
    {"source": "read-only/catalog.json", "destination": "locales/catalog.json"}
  ]
}
```

`root` 缺省为清单所在目录；相对值也以清单所在目录为基准。JIT、目录 NativeAOT 和
OneFile 将数据放在各自依赖根的 `python/ryupy_compiled/` 下。声明根内的已编译源码
锚点保留实际相对目录，入口文件名仍为 `__main__.py`；根外或未使用本选项时沿用按
模块名部署的路径。`-I` 只参与名称解析，不用来推断数据根。显式 AOT 资源位置仍优先。

构建期只读取清单列出的普通文件，拒绝根下符号链接、路径越界、Python 源码或字节码
后缀、已声明路径的大小写别名和文件/目录冲突。清单上限 1 MiB，最多 1024 个文件，
单文件 16 MiB、总量 64 MiB；以冻结的内存快照独占写入候选发布树，不跟踪原文件后续
修改。目标路径使用有界 ASCII 便携规则；宿主输入/产物根仍可含 Unicode。清单输出的
`bundle_data` 记录部署相对路径，不增加内容摘要。

该目录不加入 provider 的初始 `sys.path`，数据声明不改变源码发现、委派或函数体归属；
它不是可写用户数据协议，也不以检测内容来防止可信构建者把任意文件声明成数据。

## 验收

macOS arm64、固定 CPython 3.12.13：两套独立目录布局的 JIT、目录 NativeAOT 和
OneFile 共六个产品均与同次 CPython 一致，删除源码、venv 和构建树后从中文空格路径
运行。另有链接、路径穿越、源码后缀、大小写及文件/目录冲突的负例。重跑入口：

```sh
python3.12 -B tests/general_projects/bundle_data/products.py \
  --cli target/debug/ryupy --python "$(uv python find 3.12.13)"
```

ExplorerPro 使用同一机制部署固定上游的版本、翻译和图标，原入口构建和真实业务运行
分别登记在[大型项目验收](../../tests/general_projects/github_large/README.md)；不能用
本资源合同通过代替窗口或完整应用通过。
