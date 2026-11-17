"""只管理本次新建 PostgreSQL cluster 和 Redis 子进程，退出保留日志、清理服务。"""
from contextlib import contextmanager
import os
import json
from pathlib import Path
import shutil
import socket
import subprocess
import time

PG = Path('/Library/PostgreSQL/18/bin')


def port():
    """只分配临时端口，禁止采用共享服务默认端口。"""
    with socket.socket() as connection:
        connection.bind(('127.0.0.1', 0))
        result = connection.getsockname()[1]
    assert result not in (5432, 6379)
    return result


@contextmanager
def services(work, evidence):
    """Pg 仅监听私有 Unix socket；Redis 仅监听本轮随机 loopback 端口。"""
    data, sock = work / 'pg', work / 'socket'
    sock.mkdir(mode=0o700)
    pg_port, redis_port = port(), port()
    redis = None
    pg_attempted = False
    log = (evidence / 'services.log').open('w')
    def run(command):
        subprocess.run(list(map(str, command)), check=True, stdout=log,
                       stderr=subprocess.STDOUT, timeout=40)
    try:
        run([PG / 'initdb', '-D', data, '-U', 'ryupy', '--auth-local=trust',
             '--auth-host=reject', '--no-locale', '--encoding=UTF8'])
        pg_attempted = True
        run([PG / 'pg_ctl', '-D', data, '-l', evidence / 'postgres.log', '-w', '-t', '20',
             '-o', f"-k {sock} -p {pg_port} -c listen_addresses='' -c unix_socket_permissions=0700", 'start'])
        for database in ('ryupy_large', 'ryupy_large_test'):
            run([PG / 'createdb', '-h', sock, '-p', pg_port, '-U', 'ryupy', database])
        executable = shutil.which('redis-server') or '/opt/homebrew/bin/redis-server'
        redis = subprocess.Popen([executable, '--bind', '127.0.0.1', '--port', str(redis_port),
                                  '--save', '', '--appendonly', 'no', '--dir', str(work),
                                  '--logfile', str(evidence / 'redis.log')], stdout=log, stderr=log)
        for _ in range(100):
            if redis.poll() is not None:
                raise RuntimeError('私有 Redis 提前退出')
            try:
                with socket.create_connection(('127.0.0.1', redis_port), timeout=.2) as connection:
                    connection.sendall(b'*1\r\n$4\r\nPING\r\n')
                    assert connection.recv(32) == b'+PONG\r\n'
                break
            except OSError:
                time.sleep(.05)
        else:
            raise TimeoutError('私有 Redis 启动超时')
        (evidence / 'services-ready.json').write_text(json.dumps({
            'pg_socket': str(sock), 'pg_port': pg_port, 'redis_port': redis_port,
            'redis_pid': redis.pid, 'pg_data': str(data), 'pg_tcp_listener': False}))
        yield dict(DATABASE_HOST=str(sock), DATABASE_PORT=str(pg_port), DATABASE_USER='ryupy',
                   DATABASE_PASSWORD='', DATABASE_SCHEMA='ryupy_large', DATABASE_SOURCES='{}',
                   REDIS_HOST='127.0.0.1', REDIS_PORT=str(redis_port), REDIS_PASSWORD='',
                   REDIS_DATABASE='0', CELERY_BROKER_REDIS_DATABASE='1')
    finally:
        if redis is not None:
            redis.terminate()
            try:
                redis.wait(timeout=10)
            except subprocess.TimeoutExpired:
                redis.kill()
                redis.wait(timeout=5)
            print(f'redis reaped pid={redis.pid} exit={redis.returncode}', file=log, flush=True)
        if pg_attempted:
            result = subprocess.run([str(PG / 'pg_ctl'), '-D', str(data), '-m', 'immediate',
                                     '-w', '-t', '20', 'stop'], stdout=log, stderr=log, timeout=30)
            print(f'postgres stop exit={result.returncode}', file=log, flush=True)
            if (data / 'postmaster.pid').exists():
                raise RuntimeError('私有 PostgreSQL 尚未停止，保留路径检查')
        log.close()
