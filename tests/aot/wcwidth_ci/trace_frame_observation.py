"""只核一次frame观察artifact；不运行Rust或修复缺失/截断的证据。"""
import hashlib
import json
import re
from pathlib import Path

LIMITS = {"states": 8192, "events": 16384, "dynamic_bytes": 16 * 1024**2, "ir_bytes": 2 * 1024**2}
FIELDS = {"pc", "block", "cursor", "locals", "slots", "physical_catch", "dispatching", "pending",
          "expected_retry_pc", "expected_return_pc", "written", "stored_current", "current_sources"}
FIXTURE_SHA = "3cc2200aae4adf22b3a8156767f1a162c351e2dd5fa43f25f3eba3849b2283f4"
VALUE = re.compile(r"(?:Uninitialized|None|Opaque|Frame|Exception|StoredException|Retry|Integer\(-?\d+\)|Bool\((?:true|false)\)|Parameter\(\d+\))\Z")


def read_limited(path, limit):
    with path.open("rb") as stream:
        raw = stream.read(limit + 1)
    if len(raw) > limit:
        raise ValueError(f"artifact byte limit: {path.name}")
    return raw


def verify(directory):
    meta_raw = read_limited(directory / "metadata.json", 8192)
    meta = json.loads(meta_raw)
    if meta["schema"] != 1 or meta["limits"] != LIMITS:
        raise ValueError("schema or limits mismatch")
    raw_states = read_limited(directory / "states.jsonl", LIMITS["dynamic_bytes"])
    raw_events = read_limited(directory / "events.jsonl", LIMITS["dynamic_bytes"] - len(raw_states))
    raw_ir = read_limited(directory / "ir.txt", LIMITS["ir_bytes"])
    states = [json.loads(line) for line in raw_states.splitlines()]
    events = [json.loads(line) for line in raw_events.splitlines()]
    if len(states) != meta["retained_states"] or len(events) != meta["retained_events"]:
        raise ValueError("retained count mismatch")
    if len(states) > LIMITS["states"] or len(events) > LIMITS["events"]:
        raise ValueError("record limit")
    positions = {}
    pc_counts = {}
    content = 0
    unique = set()
    for i, record in enumerate(states):
        state = record["state"]
        if record["id"] != i or set(state) != FIELDS:
            raise ValueError("state schema/id")
        if any(not VALUE.fullmatch(value) for value in state["locals"] + state["slots"]):
            raise ValueError("unknown Value encoding")
        if len(state["locals"]) != len(state["current_sources"]) or len(state["slots"]) != len(state["written"]) or len(state["slots"]) != len(state["stored_current"]):
            raise ValueError("state dimensions")
        if state["pending"] not in ("Empty", "AwaitingRead", "Read"):
            raise ValueError("pending encoding")
        canonical = json.dumps(state, sort_keys=True)
        if canonical in unique:
            raise ValueError("duplicate unique-state record")
        unique.add(canonical)
        pc = str(state["pc"])
        pc_counts[pc] = pc_counts.get(pc, 0) + 1
        content += 4 * len(state["locals"]) + 3 * len(state["slots"]) + 2 * sum(map(len, state["current_sources"]))
        position = f'{state["pc"]}/{state["block"]}/{state["cursor"]}'
        positions[position] = positions.get(position, 0) + 1
    seen = set()
    for i, event in enumerate(events):
        identity = event["id"]
        if event["dequeue"] != i + 1 or not 0 <= identity < len(states):
            raise ValueError("dequeue/id")
        if event["inserted"] != (identity not in seen):
            raise ValueError("duplicate identity")
        seen.add(identity)
    complete = meta["observer_status"] == meta["ir_status"] == "complete"
    if complete:
        diagnostics = meta.get("analysis_diagnostics")
        if not states or not events or not isinstance(diagnostics, dict):
            raise ValueError("complete observation missing independent analysis or states")
        recomputed = {"processed": len(events), "unique_states": len(states),
                      "duplicate_states": sum(not event["inserted"] for event in events),
                      "value_cells": content, "states_by_pc": pc_counts}
        if any(diagnostics.get(key) != value for key, value in recomputed.items()):
            raise ValueError("independent analysis/record mismatch")
        if len(events) != meta["attempted"] or len(seen) != len(states):
            raise ValueError("complete record missing events")
        ir = raw_ir.decode("utf-8")
        required = ("stage=audit-build-output-not-linked", "origin=GeneratorOrigin {",
                    "layout=GeneratorLayout {", "program=Program {", "pc_targets:",
                    "catch_reads:", "saved_locals:", "functions:")
        if any(token not in ir for token in required):
            raise ValueError("complete static IR missing required objects/stage")
    files = {name: {"sha256": hashlib.sha256(raw).hexdigest(), "bytes": len(raw)} for name, raw in (
        ("metadata.json", meta_raw), ("states.jsonl", raw_states), ("events.jsonl", raw_events), ("ir.txt", raw_ir))}
    return {"complete": complete, "metadata": meta, "files": files,
            "states_by_actual_position": positions, "predecessor_attribution": "not collected"}


def collect(root, output, head, tree):
    directory = output / "frame-observation"
    result = {"complete": False}
    try:
        result = verify(directory)
        if result["complete"]:
            diagnostics = result["metadata"]["analysis_diagnostics"]
            expected = {"processed": 7004, "unique_states": 7000, "duplicate_states": 4,
                        "self_edges": 0, "value_cells": 4000256,
                        "states_by_pc": {"0": 147, "2": 282, "3": 1888, "8": 792, "11": 800, "12": 3091}}
            if diagnostics != expected:
                raise ValueError("analysis differs from frozen baseline; investigate, not optimization success")
        fixture = root / "tests/cases/generator_dynamic/frames/entry.py"
        digest = hashlib.sha256(fixture.read_bytes()).hexdigest()
        if digest != FIXTURE_SHA:
            raise ValueError("original fixture identity changed")
        result["input"] = {"head": head.strip(), "tree": tree.strip(), "fixture_sha256": digest,
                           "stage": "audit-build-output-not-linked"}
    except (OSError, ValueError, KeyError, TypeError) as error:
        result.update(complete=False, error=str(error))
    # 采集状态独立于原analysis_result；失败也留原文件和明确缺口，不补跑。
    (output / "frame-observation-result.json").write_text(json.dumps(result, indent=2) + "\n")
    return result
