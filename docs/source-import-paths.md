# 普通JIT source-bound首次Import路径（候选）

状态：CP3.12.13十案及单成员caller四臂已实测；C#/真实JIT/完整门/旧Native待验。默认资源发现仍关闭，不改变固定20项目结果。

## 问题与范围

旧显式include可使父path空、child未缓存时首次错误成功。本候选修复通用Import，不仅给未来自动模块加标记，不特殊识别项目名。

仅实际父对象与当前子注册均有新source元数据、SourceAbsolute(kind1)时检查。父来源取既有对象弱表，子取当前注册，不从可写name/module/file或替代父登记反推。父为普通__init__.py包，子为同父目录child.py或child/__init__.py；子名必须为单一路径分量，host登记不能用rooted或含路径分隔符的名字绕过父目录。

这是已编译字节的路径准入，不是通用文件系统finder；不重编译变化文件，不证明磁盘没有其它文件，不处理源码遮蔽/扩展模块。未登记名称保旧闭世界分支，不当全文件系统负证明。

## 精确顺序

1. 目标cache含State1直接返回；miss递归导父，父返回后二查目标cache。
2. 取当前子注册，再读实际父当前path一次。父正文/getter非AttributeError原身份传播。缺path转普通import非包MNFE，name为子全名，保原AttributeError context、cause=None、suppress=True。
3. 真查找阶段先拒显式sys.meta_path/path_hooks/path_importer_cache，再证明父子来源相干，再判断路径。未知finder不执行；空path不能越过此检查。
4. 相干域内精确空list/tuple真实MNFE，不执行/cache/挂父；普通非except上下文cause/context=None、suppress=False。非空精确list/tuple每项均为同一父目录精确str时接受，含同值新容器及[d,d]。每次miss重读，无正/负路径缓存，恢复后可重试。
5. None、自定义协议、其它目录、混合多路径、规范化别名NI，不执行iter/eq/str/fspath。来源不相干也NI，包括空path，不能假报不存在。

未知非空路径从原忽略路径成功变NI是公开支持域收窄，不是全finder兼容。正常项目原入口若回归，必须阻止验收并调查，不据域外标签豁免。

## 保护与未完成

ordinary与find_spec父导入共用唯一创建/资源绑定/cache/State1/init/失败清理/挂父尾段。find_spec保原strict路径/hooks检查与末次cache短路，再直接初始化，不重入ordinary第二次读getter。相对fromlist保两阶段，无skip布尔、全局缓存或TLS。

初始化失败只删本次对象，成功依赖保留；旧Legacy失败对象共享静态槽未修。缓存命中不查path/hooks，不随父登记替换漂移来源。

不增父spec/loader完整性guard。默认ModuleSpec、spec=None及普通loader/file/name改写保持；坏truthy spec、自定义spec getter与私有tracking是旧未修差异。CP坏spec正常path先AttributeError、空path先MNFE，不合成统一guard。

kind0、kind2、native、旧ABI及混合来源保旧闭世界，不宣称CP路径已修。kind2父空files声明不证明实际部署相干；新kind2延期，旧真实Native配方仍须验。

## 单成员Legacy from-import调用方

属性缺失且子名已登记时，ImportAttr消费普通Import。只抑制name精确等于该子全名的ModuleNotFoundError；异名或None原身份传播。复用既有ReadImportErrorName读取边界，不执行未知descriptor/比较协议。同名错误的handler退出后，再读实际父属性、查现有子缓存，最后才构造ImportError；正文先挂父值再失败时返回该值，不复活已清理子缓存。

正常已完成初始化的kind1源父，最终ImportError有父name、真实父源path及含路径消息；非except上下文无残留cause/context/suppress标记。来源取对象弱表，不为格式新增公开name/spec/file getter，也不新增metadata完整性guard。可写name/file/spec造成的诊断差异、循环导入特殊消息、Legacy多成员预处理/绑定顺序及公开name选路仍未修。kind0/native/旧ABI的缺名格式保持，不借源诊断开放新来源能力。实例Relative入口及其诊断次序不变。

## 回归与成本

PySourceImportPath/Lifecycle/Discovery/CallerTests使用真实槽或实例globals；虚拟来源不替代JIT/Native。四组tests/cases/source_import_paths原样CP源及独立empty-from三源保持pending。caller正文三臂保原CP与12条C#，本轮无编译Python JIT/AOT证据；既有MNFE keyword构造/普通内建.name写入不扩。旧parent_path_first必须原样显式include验收，不用新捕获异常源替代。CP-only删除metadata/module getattr/自定义finder事实不要求扩大compiler。

最终同source/head验C#、实际JIT、default/pilot完整make check、20逐ID与旧Native。warning/TypedDict来源歧义、默认pyfiglet失败/自动纳图不变。冷miss新增检查/路径字符串/配置列表分配未测，不宣称零性能回退。
