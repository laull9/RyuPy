# ADR-0190：以项目入口和交付形态组织 CLI

- 日期：2026-11-14
- 状态：已实现，本机多文件真实搬迁通过；三平台复验中。

## 决定

用户交给工具的是一个项目入口，期望得到可以交付的程序。Python版本、依赖收集、CIL、
AOT、CPython宿主都是实现过程，普通发布命令不要求用户逐层拼接这些过程。

```sh
ryupy main.py                         # 默认单文件，自动选择项目Python环境
ryupy main.py --onefile               # 显式表达相同交付形态
ryupy main.py --mode standalone       # 独立运行目录
ryupy main.py --mode jit              # 托管产物
ryupy main.py --native                # 显式选择纯RyuPy执行域
ryupy main.py -o release              # 指定输出根
```

发布形态只有 `onefile`、`standalone`、`jit`，互相冲突的选择直接诊断。单文件隐含AOT，
新入口默认自动环境适配，默认输出根为当前目录的 `dist`。`--onefile` 与 `--standalone`
是常用形态的便捷拼写。`--native` 是独立的执行域选择，与发布形态正交。
旧 `build/run/probe/dump` 子命令保留原来的执行域、输出和实验开关行为；默认原生试验与
旧脚本继续可复验。新应用入口与旧开发入口的默认值在帮助和文档中明确列出。

常用帮助保留项目、输出和执行域选项，实验参数及底层provider声明移到
`--help-advanced`，原参数名保持可用。解析与意图归一化放独立CLI模块；模块发现、
编译、依赖冻结、发布及子进程执行继续由driver完成。

设计参考 [Nuitka部署指南](https://nuitka.net/tours/deployment.html) 的入口文件加
`--mode=onefile/standalone`，采用适合RyuPy的默认单文件入口与显式原生执行域。

## 构建工具归工具所有

自动模式所需的固定 `packaging==25.0` 在用户缓存中的私有构建工具目录准备，使用已核验
基础发行的pip，仅安装构建工具；不写入项目venv，不修改应用依赖版本，不纳入应用闭包。
工具目录持内核锁发布完整候选，成功缓存可离线复用；中断释放占用，残缺旧目录隔离保存。
旧显式provider保留原来的构建环境合同。

## 普通多文件验收

`tests/cpython/multifile_run.py` 使用五个手写源码文件：入口、包初始化、业务组合、
计价和标签模块；包含多层相对导入。tomli只在深层模块导入，资源由第三方包读取。
测试只安装业务依赖，不安装packaging，也不激活venv；实际调用只有
`ryupy main.py --onefile`，由入口目录发现项目环境。

本机macOS arm64通过：清单逐项确认五模块进入CIL，第三方包及中文资源随包；用户venv
在构建前后都没有packaging。搬走唯一AOT二进制后删除整个项目、源码、venv、wheel及
所有中间目录，在空PATH、污染Python变量和独立cwd下执行，退出码、stdout、stderr与
文件副作用逐字节等于同次CPython。正常退出后独占解包目录为空。
CI三平台增加同一驱动，Linux追加无Python/.NET/SDK的断网目标容器。
