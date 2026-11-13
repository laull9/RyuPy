# schema9 联合分发与离线verify独立观察

保留用户 `tests/aot/stdlib_distribution/run.py` 及其全部fixture原字节，执行原11步合同。
默认CLI冻结后另编 `ordinary-dynamic-module-pilot` CLI，仅用户联合分发构建使用pilot；
额外离线verify使用同提交默认CLI，不把pilot当默认兼容能力。

## 为什么需要同步观察

用户原runner在成功搬迁后删除资源以测试FileNotFoundError，此前已删除源码/build/JIT。
不能事后填回资源再冒称完整原包。因此专用测试observer用runpy执行原文件，保存真实
`subprocess.run`，把原命令、参数对象、环境、超时和返回的CompletedProcess原样传递。
仅在成功AOT构建返回和成功搬迁Native返回之后、用户runner继续之前，同步调用默认CLI
verify并复制当时真实包。自身verify用保存的真实run防止递归。任何观察错误令验收失败，
不会吞掉原错误或伪造结果。标准库辅助进程不算用户原11步，仍原样直通。

这有额外同步观察成本，不宣称完全无观察的原执行或性能测量。观察不替换loader，
不改原runner/fixture/命令/stdout/stderr/退出码。前后fixture树、工具身份、原包复制前后
内容和权限快照必须一致。保存当时源码/C输入、JIT/来源报告、成功SDK和失败构建后的SDK树，
避免删除后重建证据。完整snapshot仍须与用户搬迁后包逐字节和权限相同。

## 附加验包

真实包必须schema9，具备colorsys源记录、声明资源及自动module locations两完整标志、
来源计划与静态C ABI native_probe。原包和搬迁包分别verify成功。
用户最终删资源的实际包再次verify失败，不修复它。另从保留副本创建五独立坏包：

- 资源同尺寸改字节；
- 删除声明资源；
- 增加未列文件；
- 资源改为非ASCII文件名，同时更新artifact/resource路径，保持文件集合/大小/hash自洽；
- 未知schema999。

第四例须触发schema9沿用的portable资源路径规则；若实现仅放宽版本而漏掉资源校验，
它会错误通过。不是把完整资源语义校验加入verify承诺。全部调用仍在空工具PATH和
DOTNET/PYTHON失败哨兵下核对前后树字节/权限。报告的authenticity、runtime_compatibility、
schema_semantics仍为not_checked；清单自洽不等于签名或运行资格，非原子并发快照。

## CI与证据

新整合分支触发旧多导出/通用ABI/catalog/资源三平台门，以及旧1–8两真实应用verify门。
schema9观察放在前一workflow末段；raw和SDK独立分包，旧原件包排除该目录。
GitHub ZIP不保证权限/链接还原，实际权限以同步前后lstat快照记录并限定，不能把ZIP属性
当原宿主权限。相同宿主空PATH搬迁不是无SDK全新目标机证明。

轻测命令：

```sh
python3 -B -m unittest discover -s tests/aot/schema9_verify -p test_observer.py
```
