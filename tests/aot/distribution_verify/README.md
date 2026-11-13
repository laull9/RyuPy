# 普通真实项目的离线发行包核验

仅测试公开 `ryupy build --project` 与 `ryupy verify <dist>`，不新增产品策略或特殊入口。

- 固定 Rust 1.99.1、SDK 10.0.403、CPython 3.12.13。独占 `RUNNER_TEMP/distribution-verify`，所有SDK宿主继承本轮global.json；不修上游源码。
- 复用锁定项目准备器：pyfiglet 1.0.2 metadata入口与完整553字体；binaryornot 0.4.4/chardet 5.2.0四文件分类。每项目一次真实构建；CP原流先与冻结平台黄金比较，再与搬迁Native完整stdout/stderr/exit比较。
- 两项目原包先verify，随后同时隐藏源码、上游源及build根，再验搬迁包并运行。verify和Native均使用空工具PATH与DOTNET/PYTHON失败哨兵；这不是断网或系统调用沙箱证明。
- verify前后记录完整普通文件/目录/链接目标、摘要、权限及Windows属性，要求不变。失败保留所有原流及包副本，不重跑或筛选成功样本。
- 每项目独立副本覆盖：同尺寸改字节、缺文件、额外文件、未知schema、JIT kind、路径逃逸、重复条目、零/两个native角色、链接，以及允许的额外空目录和只读权限包。仅Windows创建链接明确返回1314时记权限skip，不把其它错误算跳过。
- 额外正例故意修改native并同步清单摘要，仍应报告自洽且authenticity=`not_checked`，不会执行该副本。这明确证明没有把清单自报hash当作签名认证。Windows chmod只读属性不等价目录ACL拒写；POSIX去写位也不声称特权用户无法绕过。
- 报告只核文件闭集、长度/hash与唯一native角色，不证明完整schema语义、二进制格式、执行权限或运行兼容性；需调用者避免并发修改，不宣称原子快照或无竞态沙箱。

```sh
python3 -B -m unittest discover -s tests/aot/distribution_verify -p test_contracts.py
# CI通过固定CP执行setup.py --base <owned-root>，再run.py --base <owned-root> --rid <rid>
```

CI每平台分共享原件、两应用raw及两应用SDK五包；raw保留所有验包副本和前后快照，SDK保留实际构建/发布原件。失败前若尚未隐藏树，仍上传原路径。无未运行平台成功声明，不将本验收当性能测量。

GitHub artifact ZIP可能转换权限或跟随链接；实际验证时的模式、Windows属性和链接目标以每轮before/after快照JSON记录，不能把下载后的ZIP解包权限冒充原运行宿主权限。这里只构造符号链接负例，不声称测试了所有Windows junction/reparse类别。
