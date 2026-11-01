# M3.10 最终代码门禁

`final-check.log` 为最终生产代码 `make runtime && make check && make projects` 的完整输出，整体退出0。
434个C#、225个固定3.12.13基线、199个编译e2e/30 ignored；固定项目5 native、14 unsupported、1 failed。
项目报告exit0不代表20个项目全通过，兼容目标仍16/20，未达成。

`bounds-focused-first.log` 保留首次runtime16/MIR92通过后恰读并发补丁中间态的E0308失败。
`bounds-focused-final.log` 为源码完整冻结后重跑，MIR93、PE1、真实driver2及Release CLI全部通过。
不把第一份失败藏掉或归作通过，也不把专项性能进程混入正确性门禁。

最终26项一次冷覆盖另行归档；这些日志不等于整体性能提升或M3.11兼容验收完成。
