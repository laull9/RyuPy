#!/usr/bin/env python3
"""隔离的 loopback HTTP/TLS 差分及 NativeAOT 搬迁；证书、服务器、子进程均有期限与清理。"""
import argparse
import contextlib
import http.server
import json
import os
from pathlib import Path
import shutil
import signal
import socket
import ssl
import subprocess
import sys
import tempfile
import threading
import time

ROOT = Path(__file__).resolve().parents[2]


def run(command, *, cwd, env, timeout=90):
    """每个进程独立设期限，超时直接结束并由顶层 finally 清理测试服务器。"""
    with subprocess.Popen(command, cwd=cwd, env=env, text=True, stdout=subprocess.PIPE,
                          stderr=subprocess.PIPE, start_new_session=os.name == "posix") as process:
        try:
            stdout, stderr = process.communicate(timeout=timeout)
        except subprocess.TimeoutExpired:
            # 编译器可能启动 dotnet；超时清整个独立进程组，禁止留下后台编译器或客户端。
            if os.name == "posix":
                os.killpg(process.pid, signal.SIGKILL)
            else:
                process.kill()
            process.communicate()
            raise RuntimeError(f"子进程超过 {timeout}s: {command}") from None
        result = subprocess.CompletedProcess(command, process.returncode, stdout, stderr)
    if result.returncode:
        raise RuntimeError(f"{command}\n{result.stdout}\n{result.stderr}")
    return result


class Handler(http.server.BaseHTTPRequestHandler):
    """只绑定 loopback 的确定性服务器，避免日期、随机端口进入差分输出。"""
    protocol_version = "HTTP/1.1"
    cross = ""
    plain = ""

    def log_message(self, *_):
        pass

    def do_HEAD(self):
        self.do_GET()

    def respond(self, code, body=b"ok", location=None):
        self.send_response(code)
        self.send_header("X-Test", "value")
        self.send_header("X-Multi", "one")
        self.send_header("X-Multi", "two")
        self.send_header("Content-Length", str(len(body)))
        if location:
            self.send_header("Location", location)
        self.end_headers()
        if self.command != "HEAD":
            self.wfile.write(body)

    def do_POST(self):
        data = self.rfile.read(int(self.headers.get("Content-Length", "0")))
        if self.path.startswith("/redirect/"):
            self.respond(int(self.path.rsplit("/", 1)[1]), location="/get")
        else:
            self.respond(200, b"POST|" + self.headers.get("X-Test", "").encode() + b"|" + data)

    def do_GET(self):
        try:
            if self.path.startswith("/redirect/"):
                self.respond(int(self.path.rsplit("/", 1)[1]), location="/get")
            elif self.path == "/cross":
                self.respond(302, location=self.cross + "/credentials")
            elif self.path == "/credentials":
                body = "|".join(self.headers.get(name, "-") for name in ["Authorization", "Cookie", "X-Safe"])
                self.respond(200, body.encode())
            elif self.path.startswith("/target/"):
                self.respond(200, self.path.encode())
            elif self.path == "/downgrade":
                self.respond(302, location=self.plain + "/get")
            elif self.path == "/error":
                self.respond(404, b"missing")
            elif self.path == "/loop":
                self.respond(302, b"loop", "/loop")
            elif self.path == "/slow-header":
                time.sleep(0.3)
                self.respond(200)
            elif self.path == "/slow-body":
                self.send_response(200)
                self.send_header("Content-Length", "5")
                self.end_headers()
                self.wfile.write(b"a")
                self.wfile.flush()
                time.sleep(0.3)
                self.wfile.write(b"bcde")
            elif self.path == "/truncated":
                self.send_response(200)
                self.send_header("Content-Length", "8")
                self.send_header("Connection", "close")
                self.end_headers()
                self.wfile.write(b"abc")
                self.close_connection = True
            else:
                self.respond(200, b"hello")
        except (BrokenPipeError, ConnectionResetError, ssl.SSLError):
            pass


class Server(http.server.ThreadingHTTPServer):
    daemon_threads = False

    def get_request(self):
        connection, address = super().get_request()
        connection.settimeout(3)
        if isinstance(connection, ssl.SSLSocket):
            try:
                connection.do_handshake()
            except (ssl.SSLError, OSError):
                connection.close()
                raise
        return connection, address

    def handle_error(self, *_):
        # 主动超时/断连是测试输入；其它服务器异常必须使验收失败。
        error = sys.exception()
        if not isinstance(error, (BrokenPipeError, ConnectionResetError, TimeoutError, ssl.SSLError)):
            self.failures.append(repr(error))


@contextlib.contextmanager
def servers(directory, environment):
    """临时 CA 只交给显式 SSLContext，保持机器信任库与默认验证设置不变。"""
    openssl = shutil.which("openssl")
    if not openssl:
        raise RuntimeError("需要 openssl 生成测试 CA")
    commands = [
        ["req", "-x509", "-newkey", "rsa:2048", "-nodes", "-days", "1", "-subj", "/CN=RyuPy test CA", "-keyout", "ca.key", "-out", "ca.pem", "-addext", "basicConstraints=critical,CA:TRUE"],
        ["req", "-newkey", "rsa:2048", "-nodes", "-subj", "/CN=localhost", "-keyout", "server.key", "-out", "server.csr"],
        ["x509", "-req", "-in", "server.csr", "-CA", "ca.pem", "-CAkey", "ca.key", "-CAcreateserial", "-days", "1", "-extfile", "extensions.cnf", "-out", "server.pem"],
    ]
    (directory / "extensions.cnf").write_text("subjectAltName=DNS:localhost\nextendedKeyUsage=serverAuth\nbasicConstraints=CA:FALSE\n")
    for command in commands:
        run([openssl, *command], cwd=directory, env=environment, timeout=30)
    listeners, workers = [], []
    try:
        for encrypted in [False, False, True]:
            server = Server(("127.0.0.1", 0), Handler)
            server.failures = []
            listeners.append(server)
            if encrypted:
                context = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
                context.load_cert_chain(directory / "server.pem", directory / "server.key")
                server.socket = context.wrap_socket(server.socket, server_side=True, do_handshake_on_connect=False)
            worker = threading.Thread(target=server.serve_forever, kwargs={"poll_interval": 0.05})
            worker.start()
            workers.append(worker)
        plain = f"http://127.0.0.1:{listeners[0].server_port}"
        secure = f"https://localhost:{listeners[2].server_port}"
        Handler.plain, Handler.cross = plain, f"http://127.0.0.1:{listeners[1].server_port}"
        # 保持绑定但不 listen，确定性产生拒绝连接，避免空闲端口被抢占。
        with socket.socket() as refused:
            refused.bind(("127.0.0.1", 0))
            yield [plain, secure, secure.replace("localhost", "127.0.0.1"), str(directory / "ca.pem"), f"http://127.0.0.1:{refused.getsockname()[1]}"]
    finally:
        for index, listener in enumerate(listeners):
            if index < len(workers):
                listener.shutdown()
            listener.server_close()
        for worker in workers:
            worker.join(timeout=5)
            if worker.is_alive():
                raise RuntimeError("测试服务器未按期退出")
        failures = [error for listener in listeners for error in listener.failures]
        if failures:
            raise RuntimeError(f"测试服务器异常: {failures}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ryupy", required=True, type=Path)
    parser.add_argument("--aot", action="store_true")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    environment = dict(os.environ, PYTHONDONTWRITEBYTECODE="1", RUST_TEST_THREADS="1")
    for key in ["HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY", "http_proxy", "https_proxy", "all_proxy"]:
        environment.pop(key, None)
    environment["NO_PROXY"] = "*"
    ryupy = str(args.ryupy.resolve())
    with tempfile.TemporaryDirectory(prefix="network-", dir=environment["TMPDIR"]) as temporary:
        work = Path(temporary)
        source = work / "source"
        source.mkdir()
        shutil.copy(ROOT / "tests/network/fixtures/client.py", source / "client.py")
        output = work / "build"
        build = run([ryupy, "build", str(source / "client.py"), "--out-dir", str(output)], cwd=work, env=environment, timeout=600)
        print(build.stderr, end="", file=sys.stderr)
        with servers(work, environment) as parameters:
            expected = run([sys.executable, str(source / "client.py"), *parameters, "positive"], cwd=work, env=environment).stdout
            managed = run(["dotnet", str(output / "client.dll"), *parameters, "positive"], cwd=work, env=environment).stdout
            if managed != expected:
                raise AssertionError(f"CIL 差分失败\nCPython:\n{expected}\nRyuPy:\n{managed}")
            boundaries = run(["dotnet", str(output / "client.dll"), *parameters, "boundaries"], cwd=work, env=environment).stdout
            assert "cross origin b'-|-|yes'" in boundaries, boundaries
            report = {"python": sys.version.split()[0], "cil": "passed", "boundaries": "passed", "stdout": managed}
            if args.aot:
                native_output = work / "native-build"
                build = run([ryupy, "build", str(source / "client.py"), "--out-dir", str(native_output), "--aot"], cwd=work, env=environment, timeout=600)
                print(build.stderr, end="", file=sys.stderr)
                native_file = native_output / "client.dist" / "client"
                if not native_file.is_file() or not os.access(native_file, os.X_OK):
                    raise RuntimeError("NativeAOT 未生成预期的 client.dist/client")
                relocated = work / "搬迁 native with spaces"
                shutil.copytree(native_file.parent, relocated)
                # 删除本轮创建的源码与整个编译目录，搬迁副本仅保留原生可执行和发布资源。
                shutil.rmtree(source)
                shutil.rmtree(output)
                shutil.rmtree(native_output)
                clean = dict(environment)
                for key in list(clean):
                    if key.startswith(("DOTNET_", "RYUPY_")):
                        clean.pop(key)
                native = run([str(relocated / "client"), *parameters, "positive"], cwd=relocated, env=clean).stdout
                if native != expected:
                    raise AssertionError(f"NativeAOT 差分失败\nCPython:\n{expected}\nAOT:\n{native}")
                native_boundaries = run([str(relocated / "client"), *parameters, "boundaries"], cwd=relocated, env=clean).stdout
                assert native_boundaries == boundaries
                report["relocated_native_aot"] = "passed"
        if args.report:
            args.report.parent.mkdir(parents=True, exist_ok=True)
            args.report.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
        print(json.dumps({key: value for key, value in report.items() if key != "stdout"}, ensure_ascii=False))


if __name__ == "__main__":
    main()
