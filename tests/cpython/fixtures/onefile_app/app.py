#!/usr/bin/env python3
"""OneFile 独立单文件真实验收业务入口应用。

本应用涵盖：
1. 标准库 sqlite3 内存/文件数据库与事务
2. 标准库 ssl 上下文构建与算法套件探测
3. importlib.metadata 读取已安装包元数据
4. importlib.resources / 包路径读取内嵌资源文本
5. 默认 UTF-8 文件输入输出与中文特殊路径
6. 命令行参数（正常、空字符串、含换行符）与标准输入流处理
7. 标准输出、标准错误流分离
8. 退出码精确返回（--exit-code N 参数驱动）
9. 独立工作目录下的业务副作用文件持久化
"""
import argparse
import importlib.metadata
import importlib.resources
import os
from pathlib import Path
import sqlite3
import ssl
import sys
import time

# 引入测试安装的手写包
import onefile_pkg


def run_sqlite_check(db_path: Path) -> int:
    """验证 sqlite3 数据库读写与聚合。"""
    if db_path.exists():
        db_path.unlink()
    conn = sqlite3.connect(str(db_path))
    try:
        cur = conn.cursor()
        cur.execute("CREATE TABLE records (id INTEGER PRIMARY KEY, title TEXT, count INTEGER);")
        cur.executemany(
            "INSERT INTO records (title, count) VALUES (?, ?);",
            [("条目甲", 10), ("条目乙", 25), ("条目丙 (含特殊字符: ⚡)", 65)],
        )
        conn.commit()
        cur.execute("SELECT SUM(count) FROM records;")
        row = cur.fetchone()
        return row[0] if row else 0
    finally:
        conn.close()


def run_ssl_check() -> str:
    """验证 ssl 上下文初始化与证书校验配置。"""
    ctx = ssl.create_default_context()
    return f"protocol={ctx.protocol},verify={ctx.verify_mode}"


def run_metadata_check() -> str:
    """验证 importlib.metadata 提取包信息。"""
    pkg_ver = importlib.metadata.version("onefile_pkg")
    pkg_meta = importlib.metadata.metadata("onefile_pkg")
    name = pkg_meta["Name"]
    return f"{name}=={pkg_ver}"


def run_resource_check() -> str:
    """验证包资源读取。"""
    try:
        # Python 3.9+ 推荐用法
        res_file = importlib.resources.files("onefile_pkg").joinpath("资源.txt")
        content = res_file.read_text(encoding="utf-8")
    except Exception:
        # 回退模式兼容
        content = importlib.resources.read_text("onefile_pkg", "资源.txt", encoding="utf-8")
    first_line = content.strip().splitlines()[0]
    return first_line


def main():
    parser = argparse.ArgumentParser(description="OneFile Acceptance App")
    parser.add_argument("--exit-code", type=int, default=0, help="指定退出码")
    parser.add_argument("--hold", action="store_true", help="等待中断以验证启动器清理")
    parser.add_argument("--read-stdin", action="store_true", help="是否从标准输入读取一行")
    parser.add_argument("extra_args", nargs="*", help="额外透传参数（测试空串与换行）")

    args, unknown = parser.parse_known_args()

    # 1. 打印基础参数信息到 stdout
    print(f"[app:info] Python版本={sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}")
    print(f"[app:info] 额外参数数量={len(args.extra_args)}")
    for idx, extra in enumerate(args.extra_args):
        # repr 打印可精确表达空串和换行
        print(f"[app:arg:{idx}]={repr(extra)}")

    # 2. 读取 stdin（若要求）
    stdin_content = ""
    if args.read_stdin:
        stdin_content = sys.stdin.read().strip()
        print(f"[app:stdin]={repr(stdin_content)}")

    # 3. 业务功能验证
    cwd = Path.cwd()
    db_file = cwd / "onefile_test.db"
    total_count = run_sqlite_check(db_file)
    print(f"[app:sqlite] 汇总统计={total_count}")

    ssl_info = run_ssl_check()
    print(f"[app:ssl] {ssl_info}")

    meta_info = run_metadata_check()
    print(f"[app:meta] {meta_info}")

    res_info = run_resource_check()
    print(f"[app:res] {res_info}")

    greeting = onefile_pkg.get_greeting("AOT单文件宿主")
    print(f"[app:pkg] {greeting}")

    # 4. 写入业务副作用文件到当前工作目录（UTF-8 编码，包含中文与特殊字符）
    side_effect_file = cwd / "业务副作用_onefile.txt"
    side_effect_content = (
        f"执行成功\n"
        f"sqlite_total={total_count}\n"
        f"meta={meta_info}\n"
        f"extra_count={len(args.extra_args)}\n"
        f"stdin={stdin_content}\n"
    )
    side_effect_file.write_text(side_effect_content)

    # 5. 输出一条诊断日志到 stderr
    sys.stderr.write("[app:stderr] OneFile 业务应用生命周期正常完成。\n")
    sys.stderr.flush()

    if args.hold:
        print("[app:hold]", flush=True)
        time.sleep(120)

    if args.exit_code != 0:
        sys.exit(args.exit_code)


if __name__ == "__main__":
    main()
