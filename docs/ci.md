# CI 与版本验收

2026-11-14起，日常只保留一个Linux检查作业；三平台发布仅由main上的`v*`版本tag触发。

## 日常检查

`ci.yml`响应dev/main推送和以dev/main为目标的PR，同一分支或PR的新提交取消旧检查。
同仓库同来源分支的push/PR共享并发槽位，自动取消重复执行；fork按来源仓库隔离。
只使用Ubuntu 24.04，执行发布tag守卫回归、发行下载回归和`make check`。
不自动启动NativeAOT跨平台发布、Python七版本矩阵、历史性能配对或专项实验。
完整本地开发门继续使用`make check`。

## 版本tag

例如`v0.1.0`、`v0.1.0-rc.1`。`release-validation.yml`仅监听`v*` tag的push事件。
先获取main提交图，核对当前检出与tag相同，并确认tag提交已经进入main；未合并分支的tag
在入口拒绝，后续三平台作业不会启动。普通分支推送和手动运行都不会触发这套矩阵。

入口验证通过后调用三个可复用套件：

- `cpython-onefile-versions.yml`：三平台Release单文件CLI、onefile、普通/src多文件和动态插件。
- `cpython-standalone.yml`：固定发行的三平台NativeAOT、输入删除搬迁及缺失组件负例。
- `cpython-versions.yml`：Python 3.9–3.15 × 三平台，共21项NumPy、JIT、NativeAOT与搬迁。

三个套件只暴露`workflow_call`，均由通过main-tag检查的入口调用。tag本身不自动创建GitHub
Release；通过的单文件CLI作为Actions产物上传。发布步骤为先合入main，再创建并推送所需版本tag。

GitHub的tag过滤与可复用工作流规则见[触发语法](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#onpushbranchestagsbranches-ignoretags-ignore)及[可复用工作流](https://docs.github.com/en/actions/how-tos/reuse-automations/reuse-workflows)。

## 历史专项门

31份旧实验YAML移至`.github/retired-workflows`，保留原步骤、依赖图和人工合同供源码测试读取。
该目录不会被GitHub当作工作流。原产品测试驱动和用例保留，历史文件读取路径同步调整，
不修改归档YAML正文。新的自动门不会调用这些旧实验。

本次产品收尾的候选`f9f474f4`已完成[21项版本矩阵](https://github.com/laull9/RyuPy/actions/runs/43610181757)、
[三平台单文件/普通项目](https://github.com/laull9/RyuPy/actions/runs/43610181667)与
[三平台standalone](https://github.com/laull9/RyuPy/actions/runs/43610181631)，均全部通过；
随后收敛触发方式，保留这些已验收的套件步骤。
