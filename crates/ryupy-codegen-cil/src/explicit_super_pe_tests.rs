//! 同一待执行PE的窄合同：五个真实FnId/core、canonical读取、实参前捕获及局部数据流。
use crate::instance_pe_tests::{decode, locate, verify};
use dotnetdll::prelude::*;

fn calls(res: &Resolution<'_>, instructions: &[Instruction], needle: &str) -> Vec<usize> {
    instructions
        .iter()
        .enumerate()
        .filter_map(|(index, instruction)| {
            if matches!(instruction, Instruction::Call { .. })
                && instruction.show(res).contains(needle)
            {
                Some(index)
            } else {
                None
            }
        })
        .collect()
}
fn once(res: &Resolution<'_>, instructions: &[Instruction], needle: &str) -> usize {
    let offsets = calls(res, instructions, needle);
    assert_eq!(offsets.len(), 1, "唯一实际调用：{needle}");
    offsets[0]
}
fn stored(instructions: &[Instruction], call: usize) -> u16 {
    match instructions[call + 1] {
        Instruction::StoreLocal(slot) => slot,
        _ => panic!("调用结果必须写独立局部"),
    }
}
// caller只允许一次proxy快照；这里不追踪任意别名图或控制流。
fn proxy_load(instructions: &[Instruction], flag: usize, capture: usize, slot: u16) -> usize {
    let Instruction::LoadLocal(receiver) = instructions[flag - 1] else {
        panic!("predicate必须读取proxy快照")
    };
    assert_eq!(instructions[capture - 2], Instruction::LoadLocal(receiver));
    let stores: Vec<_> = instructions[..flag]
        .iter()
        .enumerate()
        .filter_map(|(index, instruction)| match instruction {
            Instruction::StoreLocal(value) if *value == receiver => Some(index),
            _ => None,
        })
        .collect();
    assert_eq!(stores.len(), 1);
    let store = stores[0];
    assert_eq!(instructions[store - 1], Instruction::LoadArgument(slot));
    assert!(!instructions[store + 1..capture].iter().any(|instruction| {
        matches!(instruction, Instruction::StoreLocal(value) if *value == receiver)
    }));
    store - 1
}

fn verify_core(res: &Resolution<'_>, instructions: &[Instruction], role: &str, slots: &[u16]) {
    assert!(calls(res, instructions, "CallBuiltin(").is_empty());
    assert!(calls(res, instructions, "LegacyConstruct(").is_empty());
    match role {
        "canonical" | "construct" | "direct" => {
            let load = once(res, instructions, "PyModuleInstances.LoadName(");
            assert_eq!(instructions[load - 2], Instruction::LoadArgument(slots[0]));
            let name = if role == "direct" { "super" } else { "alias" };
            assert_eq!(instructions[load - 1], Instruction::load_string(name));
            if role != "canonical" {
                let invoke = once(res, instructions, "PyPic.CallValue2(");
                let slot = stored(instructions, load);
                assert_eq!(instructions[invoke - 2], Instruction::LoadArgument(slots[1]));
                assert_eq!(instructions[invoke - 1], Instruction::LoadArgument(slots[2]));
                assert!(load < invoke);
                // 固定二参ABI为site/callee/arg0/arg1，无装箱或动态star形状。
                assert!(matches!(instructions[invoke - 3], Instruction::LoadLocal(s) if s == slot));
                assert!(!instructions[load + 2..invoke].iter().any(|instruction| {
                    matches!(instruction, Instruction::StoreLocal(s) if *s == slot)
                }));
            }
        }
        "invoke_read" | "invoke_callback" => {
            let flag = once(res, instructions, "PyOps.IsModule(");
            let capture = once(res, instructions, "PyOps.CaptureModuleTarget(");
            proxy_load(instructions, flag, capture, slots[1]);
            let attribute = if role == "invoke_callback" {
                "call"
            } else {
                "read"
            };
            assert_eq!(instructions[capture - 1], Instruction::load_string(attribute));
            let method = if role == "invoke_callback" {
                "PyPic.CallCapturedMethod1("
            } else {
                "PyPic.CallCapturedMethod0("
            };
            let invoke = once(res, instructions, method);
            assert!(flag < capture && capture < invoke);
            let flag_slot = stored(instructions, flag);
            let target_slot = stored(instructions, capture);
            assert_ne!(flag_slot, target_slot);
            assert!(!instructions[flag + 2..invoke].iter().any(|instruction| {
                matches!(instruction, Instruction::StoreLocal(slot) if *slot == flag_slot)
            }));
            assert!(!instructions[capture + 2..invoke].iter().any(|instruction| {
                matches!(instruction, Instruction::StoreLocal(slot)
                    if *slot == flag_slot || *slot == target_slot)
            }));
            // 只核固定ABI连续后缀；callback的其它frame局部不混入该对目标。
            let width = if role == "invoke_callback" { 4 } else { 3 };
            assert!(
                matches!(instructions[invoke - width], Instruction::LoadLocal(s) if s == flag_slot)
            );
            assert_eq!(
                instructions[invoke - width + 1],
                Instruction::LoadLocal(target_slot)
            );
            if role == "invoke_callback" {
                let callback = once(res, instructions, "PyOps.CallZeroWithFrame(");
                assert!(capture < callback && callback < invoke);
                // 零参frame ABI：site/callee/environment/index/first/kind。
                assert_eq!(instructions[callback - 5], Instruction::LoadArgument(slots[2]));
                let result = stored(instructions, callback);
                assert!(!instructions[callback + 2..invoke].iter().any(|instruction| {
                    matches!(instruction, Instruction::StoreLocal(slot) if *slot == result)
                }));
                assert!(
                    matches!(instructions[invoke - 1], Instruction::LoadLocal(s) if s == result)
                );
            }
        }
        _ => panic!("未知调用者角色"),
    }
}

fn assert_rejected(check: impl FnOnce()) {
    assert!(std::panic::catch_unwind(std::panic::AssertUnwindSafe(check)).is_err());
}

fn reject_mutations(res: &Resolution<'_>, instructions: &[Instruction], role: &str, slots: &[u16]) {
    let mut offsets = Vec::new();
    match role {
        "canonical" | "construct" | "direct" => {
            let load = once(res, instructions, "PyModuleInstances.LoadName(");
            offsets.push(load - 2);
            if role != "canonical" {
                let invoke = once(res, instructions, "PyPic.CallValue2(");
                offsets.extend([invoke - 2, invoke - 1]);
            }
        }
        "invoke_read" | "invoke_callback" => {
            let flag = once(res, instructions, "PyOps.IsModule(");
            let capture = once(res, instructions, "PyOps.CaptureModuleTarget(");
            offsets.push(proxy_load(instructions, flag, capture, slots[1]));
            let method = if role == "invoke_read" {
                "PyPic.CallCapturedMethod0("
            } else {
                "PyPic.CallCapturedMethod1("
            };
            let invoke = once(res, instructions, method);
            let width = if role == "invoke_callback" { 4 } else { 3 };
            offsets.extend([invoke - width, invoke - width + 1]);
            if role == "invoke_callback" {
                let callback = once(res, instructions, "PyOps.CallZeroWithFrame(");
                offsets.extend([callback - 5, invoke - 1]);
            }
        }
        _ => panic!("未知反例角色"),
    }
    for offset in offsets {
        let mut changed = instructions.to_vec();
        changed[offset] = Instruction::LoadNull;
        assert_rejected(|| verify_core(res, &changed, role, slots));
    }
}

fn verify_init(res: &Resolution<'_>, instructions: &[Instruction], state: u16) {
    let load = once(res, instructions, "PyModuleInstances.LoadName(");
    assert_eq!(instructions[load - 2], Instruction::LoadArgument(state));
    assert_eq!(instructions[load - 1], Instruction::load_string("super"));
    let value = stored(instructions, load);
    let stores: Vec<_> = calls(res, instructions, "PyModuleInstances.StoreName(")
        .into_iter()
        .filter(|offset| instructions[offset - 2] == Instruction::load_string("alias"))
        .collect();
    assert_eq!(stores.len(), 1);
    let store = stores[0];
    assert!(load < store);
    assert_eq!(instructions[store - 3], Instruction::LoadArgument(state));
    assert_eq!(instructions[store - 1], Instruction::LoadLocal(value));
    assert!(!instructions[load + 2..store].iter().any(|instruction| {
        matches!(instruction, Instruction::StoreLocal(slot) if *slot == value)
    }));
}

#[test]
#[ignore = "同次driver产物经Python身份封装；环境缺失必须失败"]
fn explicit_super_caller_actual_pe_gate() {
    let output = std::path::PathBuf::from(std::env::var("RYUPY_SUPER_CALLER_OUT").unwrap());
    let bytes = std::fs::read(output.join("super_caller.pe-envelope")).unwrap();
    let commit = std::env::var("GITHUB_SHA").unwrap();
    let (expected, pe) = decode(&bytes, &commit, "super_caller").unwrap();
    verify(pe, &expected).unwrap();
    assert_eq!(expected.owners.len(), 5);
    assert_eq!(expected.modules.len(), 1);
    let res = Resolution::parse(pe, ReadOptions::default()).unwrap();
    let roles = std::fs::read_to_string(output.join("roles.tsv")).unwrap();
    let mut names = std::collections::BTreeSet::new();
    let mut ids = std::collections::BTreeSet::new();
    for line in roles.lines() {
        let fields: Vec<_> = line.split('\t').collect();
        assert_eq!(fields.len(), 5);
        let role = fields[0];
        let id = fields[1];
        let slots: Vec<u16> = fields[2..]
            .iter()
            .map(|value| value.parse().unwrap())
            .collect();
        assert!(names.insert(role));
        let id: u32 = id.parse().unwrap();
        assert!(ids.insert(id));
        let owners: Vec<_> = expected
            .owners
            .iter()
            .filter(|owner| owner.id == id)
            .collect();
        assert_eq!(owners.len(), 1);
        let core = locate(&res, &owners[0].core).unwrap();
        let instructions = &res[core].body.as_ref().unwrap().instructions;
        verify_core(&res, instructions, role, &slots);
        reject_mutations(&res, instructions, role, &slots);
    }
    assert_eq!(names.len(), 5);
    let init = locate(&res, &expected.modules[0].target).unwrap();
    let instructions = &res[init].body.as_ref().unwrap().instructions;
    let state: u16 = std::fs::read_to_string(output.join("init-state.txt"))
        .unwrap()
        .trim()
        .parse()
        .unwrap();
    verify_init(&res, instructions, state);
    // 初始化alias的数据源也独立施加反例，不拿通用envelope错误替代。
    let load = once(&res, instructions, "PyModuleInstances.LoadName(");
    let mut changed = instructions.clone();
    changed[load - 2] = Instruction::LoadNull;
    assert_rejected(|| verify_init(&res, &changed, state));
    let stores = calls(&res, instructions, "PyModuleInstances.StoreName(");
    let alias = stores
        .into_iter()
        .find(|offset| instructions[offset - 2] == Instruction::load_string("alias"))
        .unwrap();
    let mut changed = instructions.clone();
    changed[alias - 1] = Instruction::LoadNull;
    assert_rejected(|| verify_init(&res, &changed, state));
    let coverage = concat!(
        "context start receiver proxy flag target callback callback-result ",
        "init-context init-alias: rejected\n"
    );
    std::fs::write(output.join("negative-checks.txt"), coverage).unwrap();
}
