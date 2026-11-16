"""以包中共享状态制造首次导入失败，检验 sys.modules 回滚。"""
import bridge_fixture

bridge_fixture.import_attempts += 1
attempt = bridge_fixture.import_attempts
if attempt == 1:
    raise RuntimeError('首次导入失败')
