# 当前完整源码补丁，不是历史性能候选

此补丁是在三组测量结束、恢复holder默认并增加显式实验模式后，从当前工作树捕获。
具体UTC时点、基提交、所有文件hash与新增文件清单见manifest.json。它**不能**用于声称重建
历史first/cold/null的相同DLL；那些历史production.diff仅tracked差异，新增源码当时漏存。

本补丁由 `git diff HEAD -- crates runtime` 加每个未跟踪新文件的
`git diff --no-index --binary /dev/null <文件>` 拼接，不改原始历史diff。
覆盖当前crates/runtime内编译器、运行时及其测试/快照；不含.pending、不递归包含results，
不含本轮外部性能脚本/文档。最终里程碑commit会是更权威的完整源码入口，此处尚不冒写SHA。

在独立干净的基提交工作树：

```sh
git apply --check current-source.patch
git apply current-source.patch
```

随后按仓库工具链与显式实验模式编译。此归档动作仅收集文本和hash，未构建或测量这份新快照，
没有将其结果混入旧性能表。不可在正在工作的共享树直接重复应用补丁。

已用独立临时 `GIT_INDEX_FILE` 执行 `git read-tree HEAD` 与
`git apply --cached --check current-source.patch`，退出0；没有修改共享索引/工作树。
这仅证明补丁可应用到基提交，不等于该捕获快照已构建验收。
