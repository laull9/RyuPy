# 同应用Native/Nuitka公平对照（独立于产品验收，尚未真实执行）

本目录独占对照执行器，不修改app合同、源码或CI。Linux x64首平台，原入口仍为
上游pyfiglet1.0.2 `__main__.py -f standard hi`及metadata entry加载main；
CP、RyuPy standalone目录、Nuitka standalone目录必须输出完整同一黄金，stderr空且exit0。
短CLI不是稳态吞吐测试，因此本轮明确不测稳态。单文件输入不是onefile部署。

## 已核固定工具

官方实时PyPI版本4.3.5（2026-11-08上传、非yank），归档SHA/URL见nuitka-lock.json。
已下载归档核SHA，仅静态读取setup.py和nuitka/options/OptionParsing.py，未安装或运行源码。
standalone模式默认install_requires为空；构建后端仍需setuptools/wheel，hosted安装步骤应另锁版本/hash。
不能仅拿sdist hash当安装树身份；真正接线时需保安装来源、实际版本输出、工具链和包树清单。
当前本地没有clang或Nuitka。后续真实AOT尝试已restore成功，首失败为MSB4216/4027任务宿主；
不能继续把早期缺ILCompiler缓存当成已证阻塞。未有双方可运行证据，不产生速度比。

## 执行流程

先由上一级prepare_cp.py准备仓库外OUT，完整原py、声明字体和同包egg-info逐文件锁定。
run.py再次核contract/inputs所有hash和prepared集合，使用同一个CP3.12.13及非-O环境。
RyuPy沿现有公开--distribution-metadata/build/--aot/--resources接口；Nuitka显式standalone、
pyfiglet包、distribution metadata、同一字体目录及完整egg-info，拒绝用onefile解包模式混算。
Nuitka原生配置由已核源码选项组成，但在安装后仍需真实--version/编译报告及运行确认。
默认字体集合取共享resources.json，不为Nuitka裁掉资源或为RyuPy隐藏兼容失败。

工具准备/安装不在测量中；4个entry×tool分别全新输出、全新Nuitka cache，禁ccache，
同一host串行构建，交替首工具。Nuitka jobs=1，RyuPy DOTNET_PROCESSOR_COUNT=1，记录请求；
这不等于已证明两编译器内部并行度完全相同，仍保完整环境与原流。
编译口径是依赖恢复后fresh-output/cached-dependencies，不是cold-build；NuGet/SDK恢复状态可共享。
每次构建wall与10ms /proc后代进程树RSS之和采样，报告名明确为sampled_peak。
共享页按进程重复计入、短命进程可能漏采，不能冒精确cgroup峰值或把RUSAGE_CHILDREN当总峰值。
构建tree峰值采样与短进程启动wall批分离；后者阻塞wait+alarm deadline而非timeout轮询量化。

只有双方两入口全部真实ELF、完整dist/资源原字节、原位和搬迁、隐藏源码执行全部通过，
才产生启动样本与速度比。失败保存原流和result，compatible=false且无速度比。
字体数据和egg-info必须在完整dist中保全原字节；系统共享库依赖不算成“零依赖”。
原源目录finally恢复，工具/源码/产物前后hash不变；干净运行环境不继承Python/.NET开发定位。
随后强制同一个镜像ID：基于固定官方Ubuntu摘要，仅安装明确系统共享库，无Python/.NET宿主，
禁网、只读root、仅挂本工具dist；standard/slant/list_fonts完整6语义运行两工具均过才允许测量。
镜像构建与系统库版本记录为setup，不计应用编译。启动性能在同host直接运行已验真实ELF，不混Docker启动开销。

磁盘同时列完整dist逻辑大小、实际占用、文件数及入口SHA；不能只比可执行单体大小。
启动每工具/入口2warmup+30测量，四配置轮转顺序，保所有stdout/stderr、argv、env、wall。
这是新进程启动且可能有OS页缓存，不称冷磁盘；不设更快目标、不删除离群点。

## 当前状态与后续接线

纯checker可运行：`python3 -B -m unittest discover -s tests/aot/standalone_app/benchmark`。
CLI参数见run.py；禁止在准备阶段自动安装或自动触发CI。
现接完整553字体、standard/slant/list_fonts正常site非-O CP与禁user-site，独立artifact oracle函数
先核PE/metadata/资源并签结构receipt。完整安装树由setup记录并在测量前后冻结。完整真实Native/Nuitka基准目前pending，job绿或纯checker不算产物成功。

独立工作流standalone-app-native-benchmark.yml上限120分钟，手动或专用ci分支+精确commit标题触发。
不修改旧600行controller；当前只纯检查器通过，工作流尚未实际执行，所有Native/Nuitka结果pending。

RyuPy测量用cargo --release CLI与dotnet Release runtime；两者构建均属于setup。
独立oracle测试可debug，但source-inputs绑定同一冻结releaseCLI及runtime，不混用调试CLI编译耗时。
应用计时前独立setup-restore恢复Native NuGet依赖；各次实际app发布仍保真实原流，不隐去二次恢复耗时。

日志环境安全策略：measure.capture只序列化精确非秘密键允许清单（见RECORDED_ENV_KEYS），
无GITHUB_/ACTIONS_/TOKEN前缀通配；其余环境仍可传给子进程，但日志仅记省略键数量，
不记键名、值或值摘要。伪GitHub/ACTIONS运行令牌与任意SECRET的真实capture负测必须通过。
