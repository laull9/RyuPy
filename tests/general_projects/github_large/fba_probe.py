"""原 fba app 的真实 lifespan/ASGI/OpenAPI 与原 Role ORM 提交回滚，无业务替身。"""
import asyncio
import atexit
import json
import os
from pathlib import Path
import sys
import threading

sys.path.insert(0, str(Path.cwd()))
assert sys.version_info[:3] == (3, 12, 13)
assert os.environ['DATABASE_HOST'].startswith('/private/') or os.environ['DATABASE_HOST'].startswith('/var/')
assert int(os.environ['REDIS_PORT']) != 6379
# 原入口自带的依赖准备必须已安装完；发生 subprocess 安装就失败，防止启动联网。
def audit(event, args):
    if event == 'socket.connect':
        address = args[1]
        if isinstance(address, str):
            assert address.startswith(os.environ['DATABASE_HOST'] + '/'), address
        elif isinstance(address, tuple):
            assert address[:2] == ('127.0.0.1', int(os.environ['REDIS_PORT'])), address
    if event == 'subprocess.Popen':
        raise RuntimeError(f'运行门禁止动态安装或启动额外进程: {args[0]}')
sys.addaudithook(audit)

def final_shutdown():
    # 最早登记，晚于上游 TaskRunner/loguru 自带 atexit 执行；不手动替它们清理。
    remaining = [t.name for t in threading.enumerate() if t is not threading.main_thread()]
    print(json.dumps({'shutdown': not remaining, 'remaining_threads': remaining}), flush=True)
    if remaining:
        os._exit(1)

atexit.register(final_shutdown)
from backend.main import app
from backend.core.conf import settings
from backend.app.admin.model.role import Role
from backend.database.db import async_db_session, get_database_engines
from sqlalchemy import select
from starlette.testclient import TestClient


async def transactions():
    """原 ORM/Session 的提交与异常回滚，记录真实持久化结果。"""
    async with async_db_session.begin() as session:
        session.add(Role(name='ryupy_committed'))
    try:
        async with async_db_session.begin() as session:
            session.add(Role(name='ryupy_rolled_back'))
            await session.flush()
            raise RuntimeError('private transaction rollback witness')
    except RuntimeError as exc:
        assert str(exc) == 'private transaction rollback witness'
    async with async_db_session() as session:
        names = (await session.scalars(select(Role.name))).all()
    assert 'ryupy_committed' in names and 'ryupy_rolled_back' not in names, names
    print(json.dumps({'orm_commit': True, 'orm_rollback': True, 'roles': sorted(names)}), flush=True)


with TestClient(app) as client:
    assert settings.DATABASE_SCHEMA == 'ryupy_large'
    assert settings.DATABASE_PORT != 5432
    assert settings.REDIS_PORT != 6379
    response = client.get(settings.FASTAPI_OPENAPI_URL)
    assert response.status_code == 200, response.text
    assert response.json()['paths']
    print(json.dumps({'startup': True, 'openapi': response.status_code,
                      'routes': len(response.json()['paths'])}), flush=True)
    client.portal.call(transactions)
# 原 lifespan 已退出；连接池须全部释放，不补手动 dispose 掩盖清理遗漏。
for name, engine in get_database_engines().items():
    assert engine.pool.checkedout() == 0, (name, engine.pool.status())
print(json.dumps({'lifespan_shutdown': True, 'pools_checkedout': 0}), flush=True)
