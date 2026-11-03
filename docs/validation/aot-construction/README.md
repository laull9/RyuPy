# 普通单继承构造的有限 NativeAOT 验收

候选 `8bdb496f4e87cf5ececd134aaf4718d51fdb3033` 的[固定三平台组](https://github.com/laull9/RyuPy/actions/runs/41583520724)全部成功，原工件已独立只读核验。精简原值见[summary.json](summary.json)，不包含原始日志或宿主私有路径。

每host实际runtime准备1、CLI构建1、公开AOT发布1、最终ed97源CP positive参考1；同AOT输入DLL执行JIT两模式，完整搬迁原生包执行同两模式。positive全部marker及297→295→292完整帧/ValueError、边界全部marker及422→420完整帧/NotImplementedError均吻合；四应用有意exit1，不能仅凭非零相同判通过。每模式JIT/native原stdout和stderr逐字相同，CP保完整帧/异常与stdout。

| 宿主 | 原生文件字节 | 整组秒数（含构建，非性能对比） |
| --- | ---: | ---: |
| Linux x64 | 4,732,256 | 59.961 |
| macOS arm64 14.8.9 | 4,538,136 | 89.496 |
| Windows x64 | 4,692,480 | 172.500 |

三平台同次SDK10.0.403、真实Generating native code与严格warning设置已核。schema1的源码、IL/runtime输入、完整发布集合及四运行副本前后size/SHA均核；依赖保系统库。原source/out-il改名保留，原路径失效，不说删除。每host一个隐藏SDK生成AssemblyAttributes.cs未随artifact上传，只有runner当时记录；其它保留文件独立重hash。CLI本体未归档，其SHA仅runner记录。原生包及运行副本没有这一遗漏。

首99ac组的Mac/Win路径文字断言失败（正式计数0）和Linux发布后条件导入special-base拒绝（JIT2/native0）保留，见[完整执行合同与失败历史](../../../tests/aot/CONSTRUCTION.md)。新组只修测试身份与fixture无条件imports，不扩大动态基类支持、不改原比较器。新ed97正式CP参考与先前开发预检分开。

这是有限fixture的真实原生覆盖，不是完整元类/多继承/槽历史协议、全库或全部项目兼容。SDK仍安装，不是干净机；Mac不证明12实机。manifest四scope仍false，不冒完整动态依赖闭包/ILC保根证明。WIP未main，旧matmul禁tier约23%与模块调用约32MB成本仍开放，本轮没有性能采样。
