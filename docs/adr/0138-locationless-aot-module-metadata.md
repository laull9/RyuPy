# ADR-0138：无资源AOT的模块元数据初值与来源能力分离

状态：候选；本地仅脚本/CP语义对照，完整编译、C#、JIT、Native与项目门禁等待同提交GitHub CI。

## 原因与最小改动

首轮公开CI `42360612957` 已真实publish ELF，但首个native-run因模块元数据初值不存在失败，
搬移未执行。无资源AOT编译器仍生成四个元数据前言，RegisterSourceLocated标记
metadataEnabled=true；运行时却只给位置kind1/2建立初值。具体首模块未知，首轮失败不能补认。

修复只在运行时登记的每次Import尝试创建初值，不改HIR/MIR/CIL、默认模块选择、Instance AOT
守卫、缓存算法、裁剪或SDK警告配置。元数据初值可用不等于有文件来源能力。

## 可观察合同

- 新RegisterSourceLocated的kind0（LegacyDiagnostic）：spec/loader/cached初值均为None。
  普通模块path未绑定；包path为本次尝试新建空list；__main__始终无path。诊断__file__保留
  原合同，但不是可打开来源文件承诺。绝不从basename猜目录、创建ModuleSpec或loader。
- 生成前言在缓存发布后、用户正文前复制初值至真实全局槽。内部裸名、外部属性、任意值
  重绑定和删除共用槽；未绑定不能回退初值。循环导入仍见同一初始化中对象；失败清失败模块
  缓存，成功依赖不清，重试得到新模块及新的空path。
- 手工新注册fileBackedByGlobal=false沿原getter→初值fallback合同，不冒充编译器真实槽；
  其动态属性仍覆盖初值。旧Register、RegisterLocated和RegisterResourceLocated不获得新能力。
  未注册或无初值的ModuleMetadata仍抛NotImplementedError。
- 普通已编译包/子模块Import不依赖文件搜索path，可以运行；资源files和未缓存finder仍只认
  真实位置kind1/2。DiscoveryParentPath显式检查HasSourceLocation，不能因有None初值就认作
  有来源的非包。缓存find_spec自身依旧读取公开spec，None产生ValueError。
- 这不是CPython文件源码模块完整元数据：CP3.12.13空path内存包查未知子模块返回None、普通
  内存非包查子模块产生ModuleNotFoundError；RyuPy无来源查找仍明确NI，不承诺负存在性。
- kind1绝对源码、kind2部署相对来源仍使用原CreateSpec，has_location、loader、cached、
  包路径及资源/查找守卫不变。HasSourceLocation不能用Spec非空代替：真实来源__main__的
  spec仍为None，但原来源合同必须保留。

## 验证分层与证据

新增C#单测覆盖kind0初值、四槽变更/删除、循环发布、失败重试新path、成功依赖保留、
普通子模块导入、旧十参ABI、false手工注册、资源/finder负能力。既有旧ABI异常与getter
顺序、kind1/2来源、实例注册等单测随完整make check，不以新增测试代替原集。

Native门禁有限聚合：hello无资源最小入口、main的kind0普通/包/子模块/双模块循环/失败重试/四槽变化删除、
同一main进程仅为located_pkg显式资源声明，逐项断言kind2规格/loader/cached/path和资源资格；
原样wcwidth0.2.13四调用。各自产物生成即检查ELF/manifest并保存完整dist与哈希，然后运行
和独立搬移。另复用resource_smoke原fixture做kind1 JIT及kind2资源AOT/搬移/缺资源反例。
无来源fixture断言登记偏差，不混入CP文件模块stdout差分；wcwidth和资源仍使用原CP oracle。

独立Cargo-default与pilot完整make check；项目runner明确编入qualified pilot，Session为默认JIT。
恢复固定20份官方源、SHA校验并调用现P0 preserve
门禁，显式传冻结runtime，核20份实际plan和原11条自动选择（仅4条compiled）。只保护九项，不把progress显式super已知失败或其它unsupported
算通过，不据此宣称20项目AOT、更多API兼容或性能改善。所有新门禁实际结果另行登记。
