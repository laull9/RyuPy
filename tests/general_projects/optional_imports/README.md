# 可选导入分发合同

ExplorerPro 原入口的静态依赖收集首先被 `core/file_index.py` 中可选的 PyPDF2、pypdf
及快捷方式模块中的 win32com 阻断。同次原 CPython 应用可以使用既有回退分支，构建期
必须保留这种能力选择，不能要求安装所有可选包。

收集器只准入词法 `try` 主体中明确捕获内建 `ImportError` 或 `ModuleNotFoundError`
的导入。handler、else、finally 使用进入当前 try 前的保护；类体在定义时执行而保留
保护，普通/异步函数体清除定义处保护。嵌套函数内部自己的 try 仍有效。
异常名在源码任意位置被绑定时保守按必需处理；裸 except、Exception/BaseException、
限定属性异常名和 except* 继续按必需处理。此切片不求值条件、不执行应用导入。

标准库与本地源码继续走原解析规则。确实缺席的可选第三方导入写入
`cpython-provider.json` 的 `dependency_collection.optional_imports`，有安装登记时正常
收集完整 Requires-Dist/RECORD 闭包。同根名任何必需出现或显式动态声明仍强制要求依赖；
已存在但没有 metadata 的模块也必须报错，避免漏包后改变原环境行为。

静态六组边界测试已加入 `make python-baseline`：

```sh
python3 -B tests/general_projects/optional_imports/test_scan.py
python3 -B tests/general_projects/optional_imports/run.py \
  --ryupy /absolute/path/to/ryupy \
  --python /absolute/path/to/cpython-3.12.13/bin/python3.12
```

产品驱动默认覆盖缺可选包/有可选包各 JIT 和 standalone 两种形态。每臂用同次 CPython
生成 oracle；已装分支含真实传递依赖、metadata 与导入文件副作用。构建阶段不得执行
该副作用。所有产品构建完后删除源码、venv、wheel 和构建树，再从中文空格目录、
空 PATH 和污染 Python 环境运行，比较完整 stdout/stderr/退出码及副作用内容。
入口由 `--cpython-entry` 显式委派，这组结果归属兼容 provider 分发能力。
另验证同根必需导入、显式动态依赖及已装无 metadata 三类构建期拒绝。
运行日志和 provider 审计记录只写仓外时间戳目录；临时产品在驱动结束时清理。

本轮最终 macOS arm64 实跑：六组静态边界通过，缺席/已装两个分支的 JIT/NativeAOT
四个产品全部删除输入搬迁通过，三个构建期负例全部通过。持久证据为
`~/.cache/ryupy/validation/optional-imports/20261117T074035.768240Z/`，包含同次 oracle、
构建日志、随包 provider 清单和精简结果。原 FastAPI 显式委派及 SanBudget
`--compiled-app` 业务 CIL 两个 standalone 合同在相同最终 CLI 上复验通过，分别记录于
同证据父目录的 `final-fastapi-standalone.log` 和 `final-sanbudget-standalone.log`；SanBudget
继续运行真实 macOS Tk 窗口。所有原 fixtures 保留原位置且未修改。
