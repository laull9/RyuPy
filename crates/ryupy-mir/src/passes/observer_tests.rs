//! 观察入口与原入口同一调度语义，覆盖两类pass、失败短路与重复名称。
use super::*;
use crate::mir::*;
use la_arena::Arena;
use std::cell::Cell;
use std::rc::Rc;

fn sample_function() -> Function {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(None),
    });
    Function {
        module_state: None,
        module_state_source: None,
        name: "main".into(),
        codegen_name: "main".into(),
        source_file: "main.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Unit,
        body: Body {
            locals: Arena::default(),
            blocks,
            entry,
            regions: vec![],
        },
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
        dynamic_origin: None,
    }
}

fn sample() -> Program {
    let mut functions = Arena::default();
    let main = functions.alloc(sample_function());
    Program {
        functions,
        classes: Arena::default(),
        globals: Arena::default(),
        entry: main,
        source_name: "main.py".into(),
    }
}

struct Change {
    calls: Rc<Cell<usize>>,
    invalid: bool,
    forbid_name: bool,
}
impl MirPass for Change {
    fn name(&self) -> &'static str {
        assert!(!self.forbid_name, "默认成功路径不可求值诊断名称");
        "same_pass"
    }
    fn run(&self, func: &mut Function) {
        self.calls.set(self.calls.get() + 1);
        func.body.blocks[func.body.entry]
            .stmts
            .push(Statement::Line(1));
        if self.invalid {
            func.body.blocks[func.body.entry].terminator = Terminator::Unreachable;
        }
    }
}
impl ProgramPass for Change {
    fn name(&self) -> &'static str {
        MirPass::name(self)
    }
    fn run(&self, program: &mut Program) {
        MirPass::run(self, &mut program.functions[program.entry]);
    }
}
fn pass(program: bool, invalid: bool, calls: Rc<Cell<usize>>) -> PipelinePass {
    let change = Change {
        calls,
        invalid,
        forbid_name: false,
    };
    if program {
        PipelinePass::Program(Box::new(change))
    } else {
        PipelinePass::Function(Box::new(change))
    }
}
#[derive(Default)]
struct Events(Vec<PipelineEvent>);
impl PipelineObserver for Events {
    fn observe(&mut self, event: PipelineEvent) {
        self.0.push(event);
    }
}
fn expected(ordinal: usize) -> Vec<PipelineEvent> {
    [
        (PipelinePhase::Run, PipelineStep::Begin),
        (PipelinePhase::Run, PipelineStep::End),
        (PipelinePhase::Validate, PipelineStep::Begin),
        (PipelinePhase::Validate, PipelineStep::End),
    ]
    .map(|(phase, step)| PipelineEvent {
        ordinal,
        pass_name: "same_pass",
        phase,
        step,
    })
    .to_vec()
}

#[test]
fn both_kinds_repeat_names_and_preserve_program() {
    let calls = Rc::new(Cell::new(0));
    let passes = [
        pass(false, false, calls.clone()),
        pass(true, false, calls.clone()),
    ];
    let mut plain = sample();
    let mut observed = sample();
    // 函数pass遍历两个函数，程序pass只改入口；每pass仍仅一组事件。
    for program in [&mut plain, &mut observed] {
        let mut second = sample_function();
        second.name = "second".into();
        second.codegen_name = "second".into();
        program.functions.alloc(second);
    }
    let mut events = Events::default();
    run_pipeline(&mut plain, &passes).unwrap();
    run_pipeline_observed(&mut observed, &passes, &mut events).unwrap();
    assert_eq!(format!("{plain:?}"), format!("{observed:?}"));
    assert_eq!(calls.get(), 6);
    assert_eq!(events.0, [expected(1), expected(2)].concat());
}

#[test]
fn both_kinds_fail_without_false_end_or_next_pass() {
    for program_pass in [false, true] {
        let calls = Rc::new(Cell::new(0));
        let next = Rc::new(Cell::new(0));
        let passes = [
            pass(program_pass, true, calls.clone()),
            pass(!program_pass, false, next.clone()),
        ];
        let mut plain = sample();
        let mut observed = sample();
        let mut events = Events::default();
        let old_error = run_pipeline(&mut plain, &passes).unwrap_err();
        let new_error = run_pipeline_observed(&mut observed, &passes, &mut events).unwrap_err();
        let validation_errors = crate::validate::validate(&observed).unwrap_err();
        assert_eq!(
            new_error,
            format!(
                "内部错误：Pass `same_pass` 产出了非法 MIR:\n{}",
                validation_errors.join("\n")
            )
        );
        assert_eq!(old_error, new_error);
        assert_eq!(format!("{plain:?}"), format!("{observed:?}"));
        assert_eq!(events.0, expected(1)[..3]);
        assert_eq!(calls.get(), 2);
        assert_eq!(next.get(), 0);
    }
}

#[test]
fn empty_pipeline_does_not_emit_or_validate() {
    let mut events = Events::default();
    let mut invalid = sample();
    let entry = invalid.functions[invalid.entry].body.entry;
    invalid.functions[invalid.entry].body.blocks[entry].terminator = Terminator::Unreachable;
    assert!(crate::validate::validate(&invalid).is_err());
    run_pipeline(&mut invalid, &[]).unwrap();
    run_pipeline_observed(&mut invalid, &[], &mut events).unwrap();
    assert!(events.0.is_empty());
}

#[test]
fn plain_success_never_reads_diagnostic_name() {
    assert_eq!(std::mem::size_of::<observer::NoopObserver>(), 0);
    let passes = [PipelinePass::Function(Box::new(Change {
        calls: Rc::new(Cell::new(0)),
        invalid: false,
        forbid_name: true,
    }))];
    run_pipeline(&mut sample(), &passes).unwrap();
}
