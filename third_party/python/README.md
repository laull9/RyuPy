# 固定 CPython 数据来源

`tests/generate_regex_unicode.py` 通过 CPython 3.12.13 / Unicode 15.0.0 的公开字符分类
及 `_sre.unicode_tolower`、`re._casefix` 生成正则字符/大小写事实表。
原始项目：https://github.com/python/cpython/tree/v3.12.13 。
本目录保留该固定解释器随附 `LICENSE.txt` 的完整原文，不修改或翻译许可文本。
运行时不加载 CPython，不复制 Python 正则执行器；生成器、产物与使用边界见 ADR-0029。
