//! 真实候选输入必须生成数值生产入口；结构证据不冒充动态命中计数。
use ryupy_driver::{Session, Stage};

#[test]
#[ignore = "独立数值生命周期实验，必须提供证据目录"]
fn lifecycle_sources_emit_numeric_producer() {
    let root = ryupy_driver::runtime::workspace_root();
    let evidence = std::env::var_os("NUMERIC_EVIDENCE").expect("需要独立证据目录");
    let evidence = std::path::PathBuf::from(evidence);
    std::fs::create_dir_all(&evidence).unwrap();
    for name in ["semantic", "reverse", "clear"] {
        let input = root.join(format!("tests/perf/numeric-list/{name}.py"));
        let mir = ryupy_driver::dump(&mut Session::default(), &input, Stage::Mir).unwrap();
        std::fs::write(evidence.join(format!("{name}.mir.txt")), &mir).unwrap();
        let body = mir
            .split("fn produce(")
            .nth(1)
            .expect("缺少生产函数")
            .split("\nfn ")
            .next()
            .unwrap();
        for marker in [
            "NumericListCreate",
            "NumericListAppendF64",
            "NumericListReadCheckedF64",
        ] {
            assert!(body.contains(marker), "{name}缺少{marker}");
        }
    }
}
