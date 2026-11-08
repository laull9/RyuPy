//! 只验证观察边界与非干扰；不是新的frame语义内核。
use super::*;

#[test]
fn bounded_encoding_checks_actual_bytes_and_errors() {
    for limit in [0, 2, 3, 4] {
        let mut output = Bounded {
            text: String::new(),
            limit,
        };
        let result = quoted(&mut output, "é");
        assert_eq!(result.is_ok(), limit >= 4);
        assert!(output.text.len() <= limit);
    }
    let mut exact = Bounded {
        text: String::new(),
        limit: 6,
    };
    assert!(quoted(&mut exact, "\n\"").is_ok());
    assert_eq!(exact.text, "\"\\n\\\"\"");
    assert!(exact.write_char('x').is_err());
    struct Broken;
    impl fmt::Display for Broken {
        fn fmt(&self, _: &mut fmt::Formatter<'_>) -> fmt::Result {
            Err(fmt::Error)
        }
    }
    assert!(write!(exact, "{Broken}").is_err());
}

#[test]
fn failed_export_is_separate_from_analysis_result() {
    let directory =
        std::env::temp_dir().join(format!("ryupy-observer-file-{}", std::process::id()));
    std::fs::write(&directory, b"not a directory").expect("测试障碍文件");
    let original: Result<(), String> = Err("original analysis failure".into());
    let observation = Observation::new(0);
    let failure = export(&observation, &directory, "original analysis failure");
    assert!(failure.is_err());
    assert_eq!(original, Err("original analysis failure".into()));
    std::fs::remove_file(directory).expect("清理测试文件");
}

#[test]
fn actual_inspection_is_unchanged_when_observation_stops() {
    let source = include_str!("../../../tests/cases/generator_dynamic/frames/entry.py");
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("observer_input.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diagnostics.is_empty());
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty());
    let (program, _, diagnostics, origins) = crate::trace_lowering::build_for_audit(&hir, &types);
    assert!(diagnostics.is_empty());
    assert!(crate::validate::validate(&program).is_ok());
    let run = || {
        TEST_SEQUENCE.with(|sequence| *sequence.borrow_mut() = Some(Vec::new()));
        let mut diagnostics = FrameDiagnostics::default();
        let result = inspect_with_diagnostics(
            &program,
            &origins.generators[0],
            &origins.generator_layouts[0],
            &mut diagnostics,
        );
        let sequence = TEST_SEQUENCE
            .with(|sequence| sequence.borrow_mut().take())
            .expect("自测序列已配置");
        assert_eq!(sequence.len(), diagnostics.processed);
        (result.map(|_| ()), format!("{diagnostics:?}"), sequence)
    };
    let baseline = run();
    ACTIVE.with(|active| *active.borrow_mut() = Some(Observation::new(DYNAMIC_LIMIT)));
    let observed = run();
    let complete = ACTIVE
        .with(|active| active.borrow_mut().take())
        .expect("观察已配置");
    assert_eq!(observed, baseline);
    assert!(!complete.states.is_empty());
    assert_eq!(complete.attempted, 7004);
    assert!(complete.bytes <= DYNAMIC_LIMIT);
    // 零限额触发记录失败但检查器继续；原Result和全部已有诊断必须一致。
    ACTIVE.with(|active| *active.borrow_mut() = Some(Observation::new(0)));
    let stopped = run();
    let truncated = ACTIVE
        .with(|active| active.borrow_mut().take())
        .expect("观察已配置");
    assert_eq!(stopped, baseline);
    assert_eq!(truncated.status, "byte_limit");
    assert_eq!(truncated.attempted, complete.attempted);
    assert!(truncated.states.is_empty());
}

#[test]
fn record_and_event_limits_keep_decodable_prefixes() {
    let state = State {
        pc: 0,
        block: BlockId::from_raw(la_arena::RawIdx::from_u32(0)),
        cursor: 0,
        locals: Vec::new(),
        slots: Vec::new(),
        physical_catch: false,
        dispatching: true,
        written: Vec::new(),
        expected_retry_pc: None,
        expected_return_pc: None,
        current_sources: Vec::new(),
        stored_current: Vec::new(),
        pending: Pending::Empty,
    };
    let mut records = Observation::new(DYNAMIC_LIMIT);
    for pc in 0..RECORD_LIMIT {
        let mut next = state.clone();
        next.pc = pc as i64;
        records.observe(&next, true);
    }
    assert_eq!(records.status, "complete");
    assert_eq!(records.states.len(), RECORD_LIMIT);
    records.observe(&state, true);
    assert_eq!(records.status, "record_limit");
    assert_eq!(records.states.len(), RECORD_LIMIT);
    let mut events = Observation::new(DYNAMIC_LIMIT);
    events.observe(&state, true);
    for _ in 1..EVENT_LIMIT {
        events.observe(&state, false);
    }
    assert_eq!(events.status, "complete");
    assert_eq!(events.events.len(), EVENT_LIMIT);
    events.observe(&state, false);
    assert_eq!(events.status, "record_limit");
    assert_eq!(events.events.len(), EVENT_LIMIT);
}
