"""真实 FastAPI 项目合同：依赖注入、异步会话、校验、缓存与数据库 CRUD。

驱动将本文件复制到项目的私有副本；数据库只写当前临时工作目录。
"""
import asyncio
import logging
import os
from pathlib import Path

os.environ['DATABASE_URL'] = 'sqlite+aiosqlite:///' + str(Path.cwd() / 'players.db')

from fastapi.testclient import TestClient
from databases.player_database import Base, async_engine
from main import app

# SQLAlchemy 日志包含随机 UUID 和耗时；关闭日志后验证实际响应和持久化。
async_engine.echo = False
logging.disable(logging.CRITICAL)


async def prepare():
    async with async_engine.begin() as connection:
        await connection.run_sync(Base.metadata.create_all)
    await async_engine.dispose()


asyncio.run(prepare())
with TestClient(app) as client:
    assert client.get('/health').json() == {'status': 'ok'}
    assert client.get('/openapi.json').status_code == 200
    assert client.post('/players/', json={}).status_code == 422
    player = dict(firstName='兼容', lastName='测试', squadNumber=91, position='Forward')
    created = client.post('/players/', json=player)
    assert created.status_code == 201, created.text
    identity = created.json()['id']
    assert client.get('/players/' + identity).json()['firstName'] == '兼容'
    assert client.post('/players/', json=player).status_code == 409
    first = client.get('/players/')
    second = client.get('/players/')
    assert first.headers['X-Cache'] == 'MISS'
    assert second.headers['X-Cache'] == 'HIT'
    assert len(second.json()) == 1
    player['firstName'] = '更新'
    assert client.put('/players/squadnumber/91', json=player).status_code == 204
    assert client.get('/players/squadnumber/91').json()['firstName'] == '更新'
    assert client.delete('/players/squadnumber/91').status_code == 204
    assert client.get('/players/squadnumber/91').status_code == 404
    assert client.get('/players/').json() == []
print('fastapi: health schema validation async-session cache CRUD passed')
