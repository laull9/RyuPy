"""手写独立交付验证应用：涵盖 sqlite3、ssl、中文默认文件 IO、第三方包与 metadata 资源。"""
from pathlib import Path
import sqlite3
import ssl
from importlib.metadata import version
import standalone_pkg


def main():
    # 1. 验证标准库 sqlite3
    conn = sqlite3.connect(":memory:")
    cur = conn.cursor()
    cur.execute("CREATE TABLE standalone_test (id INTEGER PRIMARY KEY, note TEXT)")
    cur.execute("INSERT INTO standalone_test (note) VALUES (?)", ("SQLite3 正常操作",))
    cur.execute("SELECT note FROM standalone_test WHERE id = 1")
    db_text = cur.fetchone()[0]
    conn.close()

    # 2. 验证标准库 ssl
    ssl_name = ssl.OPENSSL_VERSION.split()[0]
    ctx = ssl.create_default_context()
    assert ctx is not None

    # 3. 验证第三方包 metadata 与随包资源
    pkg_ver = version("standalone_pkg")
    pkg_res = standalone_pkg.get_resource_content()

    # 4. 验证默认文件 IO（不传显式 encoding，验证嵌入宿主 UTF-8 Mode 真正生效）
    side_file = Path("业务 副作用.txt")
    side_file.write_text("默认编码中文测试：独立交付搬迁验证成功\n")
    side_read = side_file.read_text().strip()

    # 打印确定性输出，用于与同次 CPython oracle 严格比对
    print(f"SQLITE={db_text}")
    print(f"SSL={ssl_name}")
    print(f"METADATA_VERSION={pkg_ver}")
    print(f"RESOURCE={pkg_res}")
    print(f"DEFAULT_IO={side_read}")


if __name__ == "__main__":
    main()
