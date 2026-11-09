"""同一read_pe接口：完整原解码＋固定fixture关键链；不执行PE，不做产品总验收。"""
import contextlib
import hashlib
import io
import json
from pathlib import Path
import runpy
import sys

HERE = Path(__file__).resolve().parent
need = lambda ok, message: None if ok else (_ for _ in ()).throw(ValueError(message))
need(len(sys.argv) == 4, '参数为 PE old|new case-id')
pe, role, case_id = sys.argv[1:]
need(role in ('old', 'new'), '未知角色')
decoder = HERE / 'read_pe_decoder.py'
need(hashlib.sha256(decoder.read_bytes()).hexdigest() == '3c20e0dbeef6fd07b4cd7246bd69e6dd356ca0c94a90c4d4101dca9921a0d1b9', '原解码器字节变化')
text = io.StringIO()
status = 0
with contextlib.redirect_stdout(text):
    try:
        runpy.run_path(str(decoder), run_name='__main__')
    except SystemExit as error:
        status = error.code
need(status == 0, '原PE有限解码失败')
decoded = json.loads(text.getvalue())
table_path = HERE / 'fixed_expectations.json'
table = json.loads(table_path.read_text())
gate = runpy.run_path(str(HERE / 'fixed_gate.py'))
try:
    need(case_id in table['cases'], '不属于固定fixture集合')
    if role == 'new':
        result = gate['check'](decoded, table['cases'][case_id], HERE.parent, role)
    else:
        _, _, _, _, facts = gate['locate'](decoded, table['cases'][case_id], role)
        result = {'admitted': False, 'role': role, 'pe_sha256': decoded['sha256'],
                  'old_locator_verified': True, 'definitions': facts,
                  'verification_scope': '旧角色仅固定词法/thunk/core定位控制；不是新source协议准入。'}
    decoded.update(result)
    decoded['fixed_expectations_sha256'] = hashlib.sha256(table_path.read_bytes()).hexdigest()
    decoded['fixed_gate_sha256'] = hashlib.sha256((HERE / 'fixed_gate.py').read_bytes()).hexdigest()
    decoded['product_accepted'] = False
except (ValueError, KeyError, IndexError, StopIteration) as error:
    decoded.update(admitted=False, pe_sha256=decoded['sha256'], role=role,
                   fixed_gate_error=str(error), product_accepted=False)
    status = 2
print(json.dumps(decoded, ensure_ascii=False, indent=2))
sys.exit(status)
