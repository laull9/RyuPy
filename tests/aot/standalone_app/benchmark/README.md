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
当前本地没有clang、ILCompiler包或Nuitka，未得双方可运行证据，不产生速度比。

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
每次构建wall与10ms /proc后代进程树RSS之和采样，报告名明确为sampled_peak。
共享页按进程重复计入、短命进程可能漏采，不能冒精确cgroup峰值或把RUSAGE_CHILDREN当总峰值。
构建tree峰值采样与短进程启动wall批分离；后者阻塞wait+alarm deadline而非timeout轮询量化。

只有双方两入口全部真实ELF、完整dist/资源原字节、原位和搬迁、隐藏源码执行全部通过，
才产生启动样本与速度比。失败保存原流和result，compatible=false且无速度比。
字体数据和egg-info必须在完整dist中保全原字节；系统共享库依赖不算成“零依赖”。
原源目录finally恢复，工具/源码/产物前后hash不变；干净运行环境不继承Python/.NET开发定位。
此隔离不是容器级“系统未安装Python/.NET”证明，后续交付验收仍应在洁净运行镜像复核。

磁盘同时列完整dist逻辑大小、实际占用、文件数及入口SHA；不能只比可执行单体大小。
启动每工具/入口2warmup+30测量，四配置轮转顺序，保所有stdout/stderr、argv、env、wall。
这是新进程启动且可能有OS页缓存，不称冷磁盘；不设更快目标、不删除离群点。

## 当前状态与后续接线

纯checker可运行：`python3 -B -m unittest discover -s tests/aot/standalone_app/benchmark`。
CLI参数见run.py；禁止在准备阶段自动安装或自动触发CI。
父合同正升级完整字体及slant产品验收；接线前须并入其新锁定输入和独立schema4资源预期验证，
并保存安装树来源。完整真实Native/Nuitka基准目前pending，job绿或纯checker不算产物成功。
