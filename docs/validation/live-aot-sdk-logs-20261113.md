# AOT SDK 实时日志：三平台真实应用验收

## 可用变化

普通AOT现在可用既有选项查看SDK发布开始、结束/错误及本轮日志位置：

```sh
ryupy build app.py --aot --diagnostic-stages
ryupy build --project ryupy.json --target app --diagnostic-stages
```

SDK双流直接写入该次build工作根的 `publish.stdout.log` / `publish.stderr.log`，
不用等SDK退出才出现内容。默认CLI输出不变；失败仍带同次完整原流，成功不再把全部SDK日志
读回父进程内存。stdin继续关闭。绑定也共用实时日志执行器，但未解除绑定的阶段诊断CLI限制。
SDK elapsed单独起算；end仅表示SDK退出成功，后续校验或dist安装仍可能失败。

## 身份与结果

独立分支 `integration/aot-live-publish-20261113`，受测代码
`a6af31ee40b04e79878f968a1bf9d9b429a85da6`，树
`c1dd19a4e1b42b85a0773d7926a7dd84b4af98b3`，基于schema9已验整合
`eac8a5bb1b5b92fd7cd808b438eec94b1496ea75`。收尾仅更新文档，不改变受测代码/测试/workflow。
最后核远端dev仍为 `a851eab14c9c38a37ee0e510e8ca728ea77786a5`；未修改dev/main或强推。

[CI43393221814](https://github.com/laull9/RyuPy/actions/runs/43393221814) 三个平台全成功，
15份归档的size/SHA/代码身份及实际内容独审通过。
[完整机器可读收据](live-aot-sdk-logs-20261113.json)列出各归档ID/摘要、现场记录与检查范围。
固定Rust1.99.1、CPython3.12.13、SDK10.0.403。

Linux x64、macOS arm64、Windows x64各公开构建未修改上游的pyfiglet与binaryornot应用。
六次构建均在CLI仍运行且采样stderr尚无SDK end/error时，取得真实SDK非空stdout文件：
Unix各37字节、Windows各38字节，原文为 `Determining projects to restore...` 含前导空格和平台换行。
采样字节是各本轮最终SDK stdout的精确前缀，CLI样本也是最终stderr前缀；日志路径与实际
attempt/SDK根一致，最终logs/begin/end各1、error0。所见是恢复阶段日志，不能声称此时ILC已开始。

原有真实应用门保留：三平台21组CP/Native原流一致，553字体与metadata完整；90次公开verify
（30成功/60拒绝），各类实际坏包/自洽允许、前后字节和模式快照均核，无功能skip。
真实SDK等待体验改善不替代原包搬迁/完整性合同，也未重新声称ABI兼容范围增加。

## 测试与失败留证合同

本地 `make check` 首轮退出0：C#3116通过/6跳过，driver284通过/5ignored，
CP参考740通过，RyuPy e2e458通过/286ignored。新增ignored是被父测试通过真实子进程
显式调用的helper，不是跳过产品功能。CP基线及旧ignored不算新增RyuPy兼容。

三平台另实际执行发布进程与阶段定向测试：Linux/macOS各6通过+1 helper ignored，
Windows5通过+1 helper ignored（Unix symlink专属测试不编译），阶段测试每平台7通过。
真实阻塞子进程证明它未退出时双流已可读；另核stdin EOF、非零状态、启动失败、已有日志
及Unix已有链接不覆盖、成功后日志读取失败仍保状态、关闭诊断不取sink与转义路径。
首个开发期测试使用不同构建profile，最后当前树统一配置完整check覆盖，不用旧结果替代。

## 限定

这是实际构建等待反馈和诊断落盘改进，不是Native运行或总构建耗时加速证据；成功路径减少
父进程读回日志是实现事实，未测量峰值内存收益。只保存SDK已经写出的字节，不保证其未flush
缓冲或掉电持久性，不新增取消/孤儿进程清理合同。顺序读文件与poll不是原子并发快照。

verify仍只证明内容自洽，非签名、完整schema语义或执行兼容认证。现场权限以前后快照为证，
ZIP不证明原权限/链接；同宿主受限PATH搬迁不是无SDK干净机。归档保留7天而非永久，
另保小原流和摘要；本地ZIP缓存只在核远端身份一致且未过期后清理。
