# 第二真实应用：tabulate 官方 console 入口

候选合同，不是Native通过报告。锁定tabulate 0.9.0、wcwidth 0.2.13原始sdist及生产包/egg-info逐文件hash。
`console_entry.py`通过真实`console_scripts`找到官方`tabulate:_main`并调用；不是自写表格算法或仅导入探针。
所有上游Python文件已与锁定归档逐字核验，不修改源码。

与pyfiglet组织不同：两个vendor搜索根、tools目录启动器、封闭metadata；没有资源请求。
表格输入是运行时用户文件，不塞入资源清单。公开构建仅为：

```sh
python3 tests/aot/deployment_tabulate/prepare.py /absolute/new-project
ryupy build --project /absolute/new-project/ryupy.json --target table
```

CP完整任务为ASCII grid输出及Unicode HTML文件输出。`check_cp.py --project P --python CP --output O`
从不同cwd执行官方入口，逐字比较stdout/stderr/文件，不截取行数或过滤错误。
项目可在其他机器由prepare的`--tabulate`、`--wcwidth`参数传入各自原包目录，不依赖仓库third_party存在。

后续Native验收：同配置公开构建，校验独立oracle/PE及metadata-only schema4；搬迁完整dist、隐藏源码，
从外部提供用户输入并核两完整输出。Native/clean-container/跨平台状态须分别报告；目前这些步骤待执行。
多搜索根oracle只属于测试出口，不给产品额外源码或模块权限。

## 已知边界，不能声称全CLI兼容

- 本地CP与RyuPy managed：grid stdout、Unicode HTML文件均完整一致；不是Native证据。
- 直接执行上游`tabulate/__init__.py`的`.version`相对导入被E0100拒绝；选择包官方注册console入口，
  不删除该导入、不把直接脚本失败标为通过。
- `-f pipe`在普通function的`__call__`属性判定后报TypeError；`-F .2f`报有限float format NI。
  两个真实失败保留，不在本部署竖切修runtime，也不纳入支持声明。
- 首个仓库外项目草稿把metadata置于子目录并使用`../vendor`，被既有分发路径合同拒绝。
  正式配置将metadata置于项目根并用`vendor/table`、`vendor/width`；嵌套路径政策未放宽。
