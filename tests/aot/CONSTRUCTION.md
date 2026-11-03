# 普通单继承构造：有限 NativeAOT 验收

当前已接线并通过固定解释器纯 runner 轻测，未执行本专项 JIT/NativeAOT、未推广 main。
初版 d03e7149 的一次 CPython 3.12.13 positive 本机校准已通过（独立前置，不算正式三 host group）。
随后仅补强Data边界的generated-init快路断言，positive段1–294逐字未变；新全源由正式group的唯一CP参考核验，不追加本机校准。
旧 smoke、七配置 ILC、C# 与 driver 专项不能代替本 fixture 的原生执行。

## 固定范围

- RID 恰 linux-x64、osx-arm64、win-x64，同真实架构宿主，不交叉发布。
- 每 host 一次 runtime rebuild、一次 CLI cargo build、一次公开 `build --aot`。
- 本次 AOT 留下的同一个 DLL 分别运行 positive / ryupy-boundaries；同 native 的完整搬迁副本各跑一次。
- 每 host 被测应用恰 JIT 2 + native 2；CPython 3.12.13 仅 positive 参考1次。
- 不加第二 JIT build、Docker、bare exe、缺文件负 publish、warmup、性能或项目集采样。
- Windows 只在生成期望 stdout 时用 CRLF，实际输出不规范化。
- macOS 报真实系统版本；14.8 的结果不代表 12。三 host 均不声称干净无 SDK 机器。

## 执行接口

`python tests/aot/construction_smoke.py --repo <绝对checkout> --revision <40位clean-HEAD> --out <证据父目录> --python <CPython3.12.13绝对路径> --dotnet <dotnet绝对路径> --rid <本host RID>`

参数无 skip-prep、额外模式、保根或重试开关。外部编排只准备工具链与运行纯轻测，不能预先替 runner 构建 runtime/CLI 后跳过计数。
现 .NET SDK 固定10.0.403，Rust固定1.99.1。owned global.json不约束repo cwd，故两cwd各读实际SDK版本；publish自己的sdk.version也独立核10.0.403。

入口总截止1800秒包含准备/版本/CP参考/文件核验。阶段上限：preflight120、CP30、runtime180、CLI600、AOT600、搬迁审计120、四应用各30且累计120，总1770；保30秒余量。
单个Runner对每阶段和总剩余取较小截止，不重置总预算。任何首次失败即停，不追测或补publish；32分钟外层仅供失败回收/工件收尾。
现POSIX进程组和Windows Job终止复用共享runner，本切不另建生命周期框架或宣称任意外部强杀也原子无孤儿。

## 两模式及冻结证据

唯一源 `construction_fixtures/construction.py` SHA256：
`dd9077fd43bb744b0098f95990ccd6721aa9ab71a03e4baa7edf0bbeb96aa0b3`。

positive覆盖sentinel身份、own函数new隐式静态化、继承super、foreign/None跳init、cached每次init、返回Child选实际init、new中late init、参数顺序和异常context。
完整成功marker之后故意调用caller，由new抛ValueError。精确exit1，完整帧294 `<module>` →292 `caller` →289 `__new__`；末行 `ValueError: new-body`。

ryupy-boundaries覆盖ordinary own new删除/late root reset的可观察原子拒绝、special自定义new受控拒绝。
Enum在late赋值处拒绝；Data/Error/TestCase的拒绝点为构造，不冒充同一点，也不修改共享TestCase。
完整成功marker之后故意通过boundary_caller删除ordinary own new。精确exit1，完整帧422 `<module>` →420 `boundary_caller`；末行 `NotImplementedError: deleting finalized __new__ is not supported`。

两个mode不是“非零即通过”：stdout全部marker、exit、完整单链帧与最终错误均先独立比固定常量。
CP positive只允许源码展示和路径前缀不同，保完整帧和异常行；CP原双流仍归档。
JIT/native同模式stdout与stderr逐字节一致，不strip、替换路径/换行或删帧。边界不执行CP、不计CP兼容通过。

## 同DLL及搬迁证明

唯一public `build --aot`产生construction.dll/runtimeconfig/runtime并把同DLL交ILC；核CLI唯一产物、真实ILC日志、严格警告、SDK、schema1及build_inputs size/hash。
源hash须等于清单compiled_text_sha256。build_revision当前为null，不伪造；clean HEAD由runner另记。
不修改生产保根，不加全程序集root，不关闭warning-as-error，不改Runtime ABI或观察API。

保留native原包及manifest列出的完整树，另建两份native运行副本与两份三文件JIT副本。每副本普通文件集合/size/hash在前后核验，不硬链接/跟随symlink或reparse。
source和out-il仅改名到build-evidence，原路径确实失效；证据仍可由宿主访问，不写“删除”或“源码不可访问”。
每应用独立cwd；native精简PATH，JIT使用相同精简环境加固定CLI语言/无logo设置及绝对dotnet，实际环境白名单写入结果。native依赖审计复用既有平台工具；Linux只允许系统/随包解析依赖，拒绝托管host导入、缺依赖及逃逸RPATH。
保所有原日志、原工程、sdk.version、manifest、输入与原包；复制体不能取代或污染original。

## 轻测与结果解释

`python -m unittest discover -s tests/aot -p 'test_construction_*.py'`

轻测不启动任何SDK、ILC、fixture或被测应用，覆盖固定计数/首失败停止、deadline不重置、SDK实际cwd、严格输出/完整帧、独立副本/全树、同IL/runtime哈希与改名归属。
隔离开发21项在3.12.14通过；统一接线后固定CPython3.12.13的21项再次通过。以上均为纯工具轻测，不是本专项JIT/NativeAOT或最终全源CP参考验收。

result.json记录每阶段、命令、尝试计数、source/CLI/runtime/IL/native/manifest哈希、原路径失效、各副本前后核验、参考及四应用原双流hash和完整帧。失败保failed_stage/首次错误/未执行项，不清场重跑。
只有三host全部本专项成功，才可记有限fixture真实NativeAOT通过；不增加项目分子、不关闭matmul旧性能负项、不声称任意descriptor/metaclass支持或main已准入。
