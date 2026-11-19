//! 真实源码的双局部形态与数值岛：普通dump优化必须看到新表示，不靠手造PE冒命中。
#[path = "../src/test_temp.rs"]
mod test_temp;
use ryupy_driver::{Session, Stage};
mod support;

fn function<'a>(mir: &'a str, name: &str) -> &'a str {
    mir.split(&format!("fn {name}("))
        .nth(1)
        .unwrap_or_else(|| panic!("missing {name}: {mir}"))
        .split("\nfn ")
        .next()
        .unwrap()
}
#[test]
fn actual_method_calls_freeze_two_values_before_arguments_without_new_cfg() {
    let root = test_temp::Root::new("module-call-pair");
    let input = root.join("main.py");
    std::fs::write(&input,"def fixed(module, value):\n    return module.f(value)\ndef arrays(module, args, kwargs):\n    return module.f(*args, **kwargs)\n").unwrap();
    let mir = support::dump(&mut Session::default(), &input, Stage::Mir).unwrap();
    for name in ["fixed", "arrays"] {
        let body = function(&mir, name);
        assert_eq!(body.matches("IsModule(").count(), 1, "{body}");
        assert_eq!(body.matches("CaptureModuleTarget(").count(), 1, "{body}");
        assert!(!body.contains("CaptureModuleMethod"));
        let lines: Vec<_> = body.lines().map(str::trim).collect();
        let test = lines
            .iter()
            .position(|line| line.contains("IsModule("))
            .unwrap();
        // 三条独立赋值对比旧一条token赋值，增量准确+2语句/+2局部；没有分支扩张。
        assert!(
            lines[test - 1].starts_with('_') && lines[test - 1].contains(" = _"),
            "{body}"
        );
        assert!(lines[test + 1].contains("CaptureModuleTarget("), "{body}");
        let flag = lines[test].split(" = ").next().unwrap();
        let target = lines[test + 1].split(" = ").next().unwrap();
        let receiver = lines[test - 1].split(" = ").next().unwrap();
        assert_ne!(flag, target);
        assert_ne!(receiver, target);
        assert_ne!(receiver, flag);
        assert!(body.contains(&format!("let {flag}: bool;")));
        assert!(body.contains(&format!("let {target}: dyn;")));
        assert!(body.contains(&format!("let {receiver}: dyn;")));

        let dispatch = if name == "fixed" {
            format!("dyn.captured_method({flag}, {target},")
        } else {
            format!("CallCapturedMethod({flag}, {target},")
        };
        assert!(body.contains(&dispatch), "{body}");
        assert_eq!(
            body.lines()
                .filter(|line| line.trim_start().starts_with("bb") && line.contains(": {"))
                .count(),
            3, // 既有正文/未达块/统一返回块；捕获不新增CFG。
            "{body}"
        );
        assert!(!body.contains("branch "), "{body}");
    }
}
#[test]
fn actual_numeric_island_keeps_raw_append_without_pair_on_typed_edge() {
    let input = ryupy_driver::runtime::workspace_root().join("tests/benchmarks/matmul.py");
    let mir = support::dump(&mut Session::default(), &input, Stage::Mir).unwrap();
    let body = function(&mir, "matmul");
    for marker in [
        "NumericListCreate",
        "NumericListReadCheckedF64",
        "NumericListAppendF64",
    ] {
        assert!(body.contains(marker), "{marker}: {body}");
    }
    let mut blocks = 0;
    for block in body.split("    bb").skip(1) {
        let block = block.split("\n    }").next().unwrap();
        if block.contains("NumericListAppendF64") {
            blocks += 1;
            assert!(
                !block.contains("CaptureModuleTarget")
                    && !block.contains("IsModule")
                    && !block.contains("dyn.captured_method"),
                "{block}"
            );
        }
    }
    assert!(blocks > 0);
    assert!(
        body.contains("CaptureModuleTarget") && body.contains("dyn.captured_method"),
        "普通盒fallback丢失: {body}"
    );
}
