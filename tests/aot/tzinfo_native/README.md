# tzinfo普通用户单基独立Native图

此图不修改Decimal原脚本、样例、黄金或结果统计。现有reusable job内先执行Decimal，
再核验同一完整源码及其冻结工具SHA，复制默认CLI/runtime而不重建。没有普通动态模块pilot。

固定CPython3.12.13黄金覆盖源模块用户Zone(tzinfo)、canonical根、真实type/继承关系、
实例字段、正负零偏移、datetime原对象回调、time的None回调、offset/dst/name与isoformat。
此图不声明MI、完整datetime、fromutc或真实toml已经通过。

每图分别保留CP一次、双PIC JIT两次、唯一真实ELF原位/搬迁两次。tzinfo复用既有Native
执行器的真实ELF/清单/SDK/输入核验、发布原流与归档、源码隔离finally恢复、dist前后快照，
但输出独立目录、独立result与artifact；不合并分母，不把检查器通过当成产品通过。

job上限40分钟；两图验证共享25分钟外层硬deadline。各Native发布最多600秒，tzinfo
每条命令额外受共享剩余时间限制，并预留30秒恢复和落盘。超预算明确失败，不跳过Native
转绿。Decimal失败则不会启动依赖其完成状态的新图，但已有原件仍由always上传保留。

本地仅运行检查器和固定CP；产品JIT须等待冻结runtime，真实Native须同提交hosted验证。
