#!/usr/bin/env python3
"""确定性生产固定发行源资源包；只接收已锁证据包，不支持任意tar提取。"""
import hashlib
import io
import json
from pathlib import Path
import tarfile

ROOT = Path(__file__).resolve().parent
EVIDENCE = ROOT.parents[2] / "tests/validation/markdown_closure_inputs.tar.gz"
EVIDENCE_SHA = "fcf1782cee6fa881a9f2e26b55b901dd89393857ddfc97735d77ad3515644eea"
MEMBERS = ["_markupbase.py", "html/__init__.py", "html/entities.py", "html/parser.py"]


def package_bytes():
    """整族固定次序、普通文件、0644、uid/gid/mtime=0、空owner、USTAR。"""
    assert hashlib.sha256(EVIDENCE.read_bytes()).hexdigest() == EVIDENCE_SHA
    records = {}
    payloads = {}
    with tarfile.open(EVIDENCE) as source:
        for name in MEMBERS + ["LICENSE.txt"]:
            origin = "inputs/stdlib/" + name if name != "LICENSE.txt" else "licenses/CPython-LICENSE.txt"
            data = source.extractfile(origin).read()
            payloads[name] = data
            records[name] = {"bytes": len(data), "sha256": hashlib.sha256(data).hexdigest(),
                             "lines": len(data.splitlines()), "evidence_member": origin}
    manifest = {"schema_version": 1, "evidence_sha256": EVIDENCE_SHA,
                "source_identity": "锁定CI42932089715源码字节；CP参考基线3.12.13，源码upstream tag未另作网络核验",
                "format": "USTAR; regular files only; mode 0644; uid/gid/mtime 0; empty uname/gname",
                "members": records}
    manifest_bytes = (json.dumps(manifest, ensure_ascii=False, indent=2) + "\n").encode()
    payloads["manifest.json"] = manifest_bytes
    output = io.BytesIO()
    with tarfile.open(fileobj=output, mode="w", format=tarfile.USTAR_FORMAT) as archive:
        for name, data in payloads.items():
            item = tarfile.TarInfo(name)
            item.size = len(data)
            item.mode = 0o644
            archive.addfile(item, io.BytesIO(data))
    return output.getvalue(), manifest_bytes, payloads["LICENSE.txt"]


def main():
    raw, manifest, license_bytes = package_bytes()
    assert package_bytes()[0] == raw
    (ROOT / "cpython-sources.tar").write_bytes(raw)
    (ROOT / "manifest.json").write_bytes(manifest)
    (ROOT / "LICENSE.txt").write_bytes(license_bytes)
    print(hashlib.sha256(raw).hexdigest())


if __name__ == "__main__":
    main()
