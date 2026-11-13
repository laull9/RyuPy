//! 跨模块异常引用必须使用内部限定身份，不修改 Python 可见类名。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn builtin_and_user_exception_names_do_not_share_global_cil_key() {
    let root = std::path::Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../tests/cases/phase3/m3_7_exception_identity");
    // dump 是单模块视图；三模块实际链接与执行由同名目录 e2e 验收。
    for (file, snapshot) in [
        ("entry.py", "exception_identity_cil"),
        ("left.py", "exception_identity_left_cil"),
        ("right.py", "exception_identity_right_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &root.join(file), Stage::Cil)
            .expect("同名异常应保持模块限定的静态捕获身份");
        if file == "entry.py" {
            insta::assert_snapshot!(snapshot, output);
        } else {
            assert_exception_cil(&output, file == "left.py");
            let corrupted = output.replacen(
                "catch RyuPy.Generated._m0_ValueError handler",
                "catch [RyuPy.Runtime]RyuPy.Runtime.PyValueError handler",
                1,
            );
            assert_ne!(corrupted, output);
            assert!(
                std::panic::catch_unwind(|| assert_exception_cil(&corrupted, file == "left.py"))
                    .is_err()
            );
        }
    }
}

// 工厂返回限定生成类型，EH按同一类型捕获；Python展示名仍为原类名。
fn assert_exception_cil(output: &str, left: bool) {
    use support::cil_contract::{Cil, Method};
    let cil = Cil::parse(output);
    let factory = |id: usize, name: &str| {
        let method = cil.exact(&format!("__exc_factory_{id}"));
        let display = format!("LoadString(\"{name}\")");
        let constructor = format!(
            "NewObject(void RyuPy.Generated._m0_{name}..ctor([RyuPy.Runtime]RyuPy.Runtime.PyClass, [System.Runtime]System.Object[], string))"
        );
        assert_eq!(
            method.instructions,
            [
                "LoadArgument(0)",
                "LoadArgument(1)",
                &display,
                &constructor,
                "Return"
            ]
        );
    };
    factory(0, "ValueError");
    if left {
        factory(1, "Child");
    }
    let catches = |method: &Method<'_>| -> Vec<String> {
        method
            .eh
            .iter()
            .map(|line| {
                line.split_once(" catch ")
                    .unwrap()
                    .1
                    .split_once(" handler ")
                    .unwrap()
                    .0
                    .to_owned()
            })
            .collect()
    };
    let user = "RyuPy.Generated._m0_ValueError";
    let base = "[RyuPy.Runtime]RyuPy.Runtime.PyBaseException";
    let run = cil.method("run");
    if left {
        assert_eq!(catches(run), [user, base]);
        assert_eq!(catches(cil.method("inherited")), [user, base]);
        assert_eq!(
            cil.method("raise_error")
                .runtime_count("PyErrors.PrepareRaise"),
            1
        );
        let paused = cil.method("paused");
        assert_eq!(paused.opcode_count("LoadMethodPointer"), 1);
        let pointers: Vec<_> = paused.instructions.iter().filter_map(|i|
            i.strip_prefix("LoadMethodPointer(static [System.Runtime]System.Object RyuPy.Generated.Module.")
             .map(|rest| rest.split_once('(').unwrap().0)
        ).collect();
        assert_eq!(pointers.len(), 1);
        let resume = cil.exact(pointers[0]);
        let pointer = format!(
            "LoadMethodPointer(static [System.Runtime]System.Object RyuPy.Generated.Module.{}([System.Runtime]System.Object))",
            resume.name
        );
        assert_eq!(
            paused
                .instructions
                .iter()
                .filter(|i| **i == pointer)
                .count(),
            1
        );
        assert_eq!(resume.runtime_count("PyOps.GeneratorInput"), 2);
        assert_eq!(resume.runtime_count("PyErrors.EnterHandler"), 3);
        assert_eq!(resume.runtime_count("PyErrors.LeaveHandler"), 3);
        assert_eq!(catches(resume), [base]);
    } else {
        assert_eq!(
            catches(run),
            [
                user,
                "[RyuPy.Runtime]RyuPy.Runtime.PyException",
                base,
                base,
                user,
                base
            ]
        );
        assert_eq!(catches(cil.method("throw_error")), [user, base]);
    }
    let names: &[&str] = if left {
        &["run", "raise_error", "paused", "inherited"]
    } else {
        &["run", "throw_error"]
    };
    for (index, name) in names.iter().enumerate() {
        assert_eq!(
            cil.exact(&format!("__thunk_{index}"))
                .generated_call_count(cil.method(name)),
            2
        );
    }
}
