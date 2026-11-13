# 直接原生入口有限验收

```sh
python3 -B tests/aot/direct_entry/run.py \
  --ryupy /absolute/path/ryupy --python /absolute/path/python3.12 \
  --out /absolute/path/fresh-evidence --rid linux-x64
python3 -B -m unittest discover -s tests/aot/direct_entry -p 'test_*.py'
```

`--out` 必须不存在。调用者提供当前已构建 CLI、对应运行时以及固定 CPython；脚本不下载工具、不构建 CLI，不修改仓库。

一次公开源码 `build --aot`，六组行为分别比较原位与搬迁 Native 对 CPython 的退出码及双流：普通成功、退出码 7、字符串 SystemExit、两帧未捕获 ValueError、空串/中文/特殊字符参数、无换行缓冲输出和 atexit 回调。预期码、stdout、错误文本和异常帧另有固定断言；不允许“两端同样错”通过。只有 traceback 的源码目录和 CPython 源码展示行可以规范化。

Unix 必须实查本次 SDK 工作根唯一 `.ilc.rsp` 的唯一主输入与本次 managed 的完整字节摘要一致。Windows 必须保留精确薄宿主与双引用，不把其宿主输入冒充直接输入。两路都检查严格警告属性，保留公开构建完整输出和所有运行原流。搬迁后隐藏源目录，使用无关 cwd 与已有平台 helper 生成的最小环境。dist 完整字节集合执行前后必须不变。

`result.json` 只有最后全部成功才写 `passed: true`。轻量自测中的合成 SDK 目录只验证审核器，不计 Native 证明。脚本不测性能、资源/metadata 闭包、静态绑定、优化收据、调试符号或干净机部署；这些仍由各自既有门验证。
