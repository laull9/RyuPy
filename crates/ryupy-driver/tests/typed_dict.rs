//! TypedDict专用构建只接受真实基值；条件导入与可变别名不能绕过运行时检查。
use ryupy_driver::{Session, Stage};
mod support;

#[test]
fn snapshot_typed_dict_uses_private_shell_then_atomic_binding() {
    let path = std::env::temp_dir().join("ryupy_typed_dict_snapshot.py");
    std::fs::write(
        &path,
        "from typing import TypedDict\nclass Record(TypedDict, total=False):\n    x: int\n",
    )
    .unwrap();
    for (stage, name) in [
        (Stage::Hir, "typed_dict_hir"),
        (Stage::Types, "typed_dict_types"),
        (Stage::Mir, "typed_dict_mir"),
    ] {
        let mut session = Session::default();
        let output = support::dump(&mut session, &path, stage).unwrap();
        assert!(session.diagnostics.is_empty());
        match stage {
            Stage::Hir => {
                assert!(output.contains("class Record(TypedDict"), "{name}");
                assert!(output.contains("total=false"));
                assert!(output.contains("x: int"));
            }
            Stage::Types => {
                assert_eq!(output.matches(": Dynamic").count(), 5, "{name}");
                assert!(output.contains("[Dynamic]: [] -> None"));
            }
            Stage::Mir => {
                let begin = output.find("TypedDictBegin(const \"Record\"").unwrap();
                let annotations = output.find("const \"__annotations__\"").unwrap();
                let member = output.find("RuntimeFn::SetItem").unwrap();
                let finish = output.find("RuntimeFn::TypedDictFinish").unwrap();
                assert!(
                    begin < annotations && annotations < member && member < finish,
                    "{name}"
                );
                assert!(output[begin..annotations].contains("const false"));
                let line = output
                    .lines()
                    .find(|line| line.contains(" = RuntimeFn::TypedDictFinish"))
                    .unwrap();
                let result = line.trim().split(" = ").next().unwrap();
                assert!(
                    output[finish..]
                        .lines()
                        .any(|line| line.trim().starts_with('g')
                            && line.ends_with(&format!(" = {result};"))),
                    "只能公开完成后的类型"
                );
            }
            _ => unreachable!(),
        }
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn rebound_root_and_unexecuted_import_do_not_fabricate_types() {
    let root = ryupy_driver::runtime::workspace_root().join("target/test_typed_dict_guard");
    std::fs::create_dir_all(&root).unwrap();
    let input = root.join("main.py");
    std::fs::write(&input, "from typing import TypedDict as Root\nRoot = dict\nExisting = 'old'\nevents = []\ndef annotation():\n    events.append('body')\n    return int\ntry:\n    class Existing(Root):\n        x: annotation()\nexcept NotImplementedError:\n    print(Existing, events)\nif False:\n    from typing import TypedDict as Missing\ntry:\n    class Absent(Missing):\n        x: int\nexcept NameError:\n    print('missing import')\n").unwrap();
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let result = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    assert_eq!(
        result.status.code(),
        Some(0),
        "{}",
        String::from_utf8_lossy(&result.stderr)
    );
    assert!(result.stderr.is_empty());
    assert_eq!(
        String::from_utf8_lossy(&result.stdout),
        "old []\nmissing import\n"
    );
}
