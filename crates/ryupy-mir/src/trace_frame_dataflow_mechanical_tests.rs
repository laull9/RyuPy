//! 机械表示与查找的独立oracle；不作为第二条frame资格路径。
use super::*;
use crate::trace_frame_tests::{SOURCE, build};

fn budget() -> Budget {
    Budget {
        diagnostics: Diagnostics::default(),
    }
}

fn cells(state: &State) {
    for cell in state.locals.iter().chain(&state.slots) {
        assert!(!cell.is_empty() && cell.len() <= 32);
    }
}

#[test]
fn bounded_cells_preserve_snapshots_and_failure_atomicity() {
    let mut budget = budget();
    assert!(Values::checked(Vec::new(), &mut budget).is_err());
    assert!(Values::checked((0..33).map(Value::Integer).collect(), &mut budget).is_err());
    let mut full = Values::checked((0..32).map(Value::Integer).collect(), &mut budget).unwrap();
    assert!(!full.insert(Value::Integer(0), &mut budget).unwrap());
    let snapshot = full.clone();
    assert!(full.insert(Value::None, &mut budget).is_err());
    assert_eq!(full, snapshot);
    budget.diagnostics.work_units = STEP_BUDGET;
    assert!(full.insert(Value::None, &mut budget).is_err());
    assert_eq!(full, snapshot);
    assert!(Values::single(Value::None, &mut budget).is_err());
    let (program, origins) = build(SOURCE);
    let (mut engine, mut state) = tests::seed_engine(&program, &origins);
    cells(&state);
    let before_eh = state.clone();
    state.locals[0] = Values::single(Value::Exception, &mut engine.budget).unwrap();
    assert!(!before_eh.locals[0].contains(&Value::Exception));
    domain::age(&mut state.locals, &mut engine.budget).unwrap();
    assert_eq!(state.locals[0].as_slice(), &[Value::StoredException]);
    cells(&state);
    let mut joined = before_eh.clone();
    domain::join(&mut joined, &state, &mut engine.budget).unwrap();
    cells(&joined);
    assert!(joined.locals[0].contains(&Value::Uninitialized));
    assert!(joined.locals[0].contains(&Value::StoredException));
    engine.enqueue(joined).unwrap();
    for node in &engine.nodes {
        cells(&node.input);
    }
}

fn mappings(body: &Body, layout: &GeneratorLayout) {
    let mut budget = budget();
    let lookup = LayoutIndex::build(body, layout, &mut budget).unwrap();
    for (block, _) in body.blocks.iter() {
        let dispatch = layout
            .pc_targets
            .iter()
            .find(|(_, id)| *id == block)
            .map(|(pc, _)| *pc);
        let exit = layout
            .exit_pcs
            .iter()
            .find(|(id, _)| *id == block)
            .map(|(_, pc)| *pc);
        let retry = layout
            .original_blocks
            .iter()
            .position(|id| *id == block)
            .and_then(|old| {
                layout.exception_routes.iter().find(|route| {
                    old as i64 >= route.start
                        && (old as i64) < route.start + route.length as i64
                })
            })
            .map(|route| route.handler_pc);
        assert_eq!(lookup.dispatch(block, &mut budget).unwrap(), dispatch);
        assert_eq!(lookup.exit(block, &mut budget).unwrap(), exit);
        assert_eq!(lookup.retry(block, &mut budget).unwrap(), retry);
    }
    let unknown = BlockId::from_raw(la_arena::RawIdx::from_u32(body.blocks.len() as u32));
    assert!(
        lookup
            .dispatch(unknown, &mut budget)
            .unwrap_err()
            .contains("目标缺失")
    );
    assert!(lookup.exit(unknown, &mut budget).is_err());
    assert!(lookup.retry(unknown, &mut budget).is_err());
    budget.diagnostics.work_units = STEP_BUDGET;
    assert!(LayoutIndex::build(body, layout, &mut budget).is_err());
}

#[test]
fn actual_layout_indices_match_first_match_linear_oracle() {
    for source in [SOURCE.to_string(), SOURCE.replace("generate", "renamed_factory")] {
        let (program, origins) = build(&source);
        assert!(validate::validate(&program).is_ok());
        let origin = &origins.generators[0];
        let layout = &origins.generator_layouts[0];
        crate::trace_frame::prepare(&program, origin, layout).unwrap();
        let body = &program.functions[origin.resume].body;
        mappings(body, layout);
        // 仅查找层oracle的重叠/空输入，不声称此变体具备生产模板资格。
        let mut changed = layout.clone();
        changed.pc_targets.push((999999, layout.pc_targets[0].1));
        changed.exit_pcs.push((layout.exit_pcs[0].0, 999999));
        changed.exception_routes.insert(
            0,
            crate::trace_lowering::FrameCatchRoute {
                start: 0,
                length: changed.original_blocks.len(),
                handler_pc: 999999,
                unconditional: false,
            },
        );
        mappings(body, &changed);
        changed.pc_targets.clear();
        changed.exit_pcs.clear();
        changed.exception_routes.clear();
        mappings(body, &changed);
    }
}
