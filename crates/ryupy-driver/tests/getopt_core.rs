//! 有限getopt真实默认编译差分：原双流留证，静态拒绝另标，完整链仅精确投影已知库帧。
use ryupy_driver::{BuildOptions, Session};
use sha2::{Digest, Sha256};
use std::path::{Path, PathBuf};
use std::process::{Command, Output};
use std::sync::atomic::{AtomicU64, Ordering};
#[path = "m3_5_dynamic_support/mod.rs"]
mod dynamic_support;

static NEXT: AtomicU64 = AtomicU64::new(0);
// 固定官方CPython3.12.13源码身份；安装位置可变，字节与精确库帧不可变。
const LIBRARY_SHA256: &str = "0ce875700c8798193b8e2748f7a27fc542cc4d525a1e6fc403767450ec92be99";
struct Root(PathBuf);
impl Root {
    fn new(label: &str) -> Self {
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let path = std::env::temp_dir().join(format!(
            "ryupy-getopt-{label}-{}-{stamp}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        // create成功才取得清理权；失败/断言panic保留证据，不能删除别人的目录。
        std::fs::create_dir(&path).unwrap();
        Self(path)
    }
}
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn save(root: &Path, label: &str, output: &Output) {
    std::fs::write(root.join(format!("{label}.stdout")), &output.stdout).unwrap();
    std::fs::write(root.join(format!("{label}.stderr")), &output.stderr).unwrap();
    std::fs::write(
        root.join(format!("{label}.exit")),
        format!("{:?}\n", output.status.code()),
    )
    .unwrap();
}
// 完整异常链的精确库帧与用户投影契约，只简化测试签名。
type ChainOracle<'a> = (&'a [(&'a str, &'a str)], &'a [&'a str]);

fn execute(
    source: &str,
    filename: &str,
    code: i32,
    helper: bool,
    chain_oracle: Option<ChainOracle<'_>>,
) -> (Root, Output, Output, PathBuf, PathBuf) {
    let root = Root::new(filename);
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let probe = Command::new(&python).args(["-B", "-c", "import sys, getopt; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3]))); print(getopt.__file__)"]).output().unwrap();
    save(&root.0, "probe", &probe);
    assert!(probe.status.success());
    let probe_text = String::from_utf8(probe.stdout).unwrap();
    let lines: Vec<_> = probe_text.lines().collect();
    let pinned = std::fs::read_to_string(
        ryupy_driver::runtime::workspace_root().join("tests/pinned-python.txt"),
    )
    .unwrap();
    assert_eq!(lines.len(), 2);
    assert_eq!(lines[0], pinned.trim());
    let stdlib = PathBuf::from(lines[1]);
    assert!(stdlib.is_absolute() && stdlib.is_file());
    let library_bytes = std::fs::read(&stdlib).unwrap();
    assert_eq!(
        format!("{:x}", Sha256::digest(&library_bytes)),
        LIBRARY_SHA256
    );
    std::fs::write(root.0.join("library.sha256"), LIBRARY_SHA256).unwrap();
    let input = root.0.join(filename);
    std::fs::write(&input, source).unwrap();
    if helper {
        std::fs::write(
            root.0.join("probe_side_effect.py"),
            include_str!("../../../tests/fixtures/getopt_core/probe_side_effect.py"),
        )
        .unwrap();
    }
    let cp_cwd = root.0.join("cp");
    let jit_cwd = root.0.join("jit");
    std::fs::create_dir(&cp_cwd).unwrap();
    std::fs::create_dir(&jit_cwd).unwrap();
    let expected = Command::new(python)
        .arg("-B")
        .arg(&input)
        .current_dir(cp_cwd)
        .env("LC_ALL", "C")
        .output()
        .unwrap();
    save(&root.0, "python", &expected);
    // CP先守门：失败形态与预计退出码不符不得进入编译/JIT，不能运行旧DLL。
    assert_eq!(
        expected.status.code(),
        Some(code),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    // 未捕获错误必须先确认CP完整链，而非任意exit1都允许进入编译/JIT。
    if let Some((library, fixed)) = chain_oracle {
        let (projected, removed) = projection(&expected, &input, source, library, true, &stdlib);
        std::fs::write(root.0.join("python.projection"), projected.join("\n")).unwrap();
        std::fs::write(root.0.join("python.removed-frames"), removed.join("\n")).unwrap();
        assert_eq!(projected, fixed);
    }
    let mut session = Session::default();
    let built = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input: input.clone(),
            out_dir: root.0.join("out"),
            aot: false,
            search_paths: vec![],
        },
    );
    std::fs::write(
        root.0.join("build.diagnostics"),
        format!("{:?}", session.diagnostics),
    )
    .unwrap();
    let artifacts = built.unwrap();
    let actual = ryupy_driver::dotnet::run_dll_with(
        &artifacts.dll,
        &ryupy_driver::dotnet::RunOptions {
            cwd: Some(jit_cwd),
            env: vec![("LC_ALL".to_owned(), "C".to_owned())],
            ..Default::default()
        },
    )
    .unwrap();
    save(&root.0, "jit", &actual);
    assert_eq!(
        actual.status.code(),
        Some(code),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    (root, expected, actual, input, stdlib)
}
fn raw(source: &str, name: &str, helper: bool) {
    let (_root, cp, jit, _, _) = execute(source, name, 0, helper, None);
    assert!(cp.stderr.is_empty());
    assert_eq!(jit.stdout, cp.stdout);
    assert_eq!(jit.stderr, cp.stderr);
}
#[test]
fn parsing_binding_order_unicode_and_tail_identity() {
    raw(
        include_str!("../../../tests/fixtures/getopt_core/parse.py"),
        "getopt_parse.py",
        false,
    );
}
#[test]
fn error_objects_fields_and_fresh_callback_context() {
    raw(
        include_str!("../../../tests/fixtures/getopt_core/objects.py"),
        "getopt_objects.py",
        false,
    );
}
#[test]
fn local_identity_no_hoist_and_rejected_shadowing() {
    raw(
        include_str!("../../../tests/fixtures/getopt_core/local_imports.py"),
        "getopt_locals.py",
        true,
    );
    // 保全部原输入：局部删除现已支持，但handler仍必须读取真实动态绑定。
    let root = Root::new("negative");
    let cases = [
        "def f(Error):\n    from getopt import GetoptError as Error\n    try:\n        pass\n    except Error:\n        pass\n",
        "def f():\n    try:\n        pass\n    except Error:\n        pass\n    from getopt import GetoptError as Error\n",
        "def f():\n    from getopt import GetoptError as Error\n    from getopt import GetoptError as Error\n    try:\n        pass\n    except Error:\n        pass\n",
        "def f():\n    from getopt import GetoptError as Error\n    Error = ValueError\n    try:\n        pass\n    except Error:\n        pass\n",
        "def f():\n    from getopt import GetoptError as Error\n    del Error\n    try:\n        pass\n    except Error:\n        pass\n",
        "def f(flag):\n    if flag:\n        from getopt import GetoptError as Error\n    try:\n        pass\n    except Error:\n        pass\n",
        "def f():\n    global Error\n    from getopt import GetoptError as Error\n    try:\n        pass\n    except Error:\n        pass\n",
        "def f():\n    from getopt import GetoptError as Error\n    def inner():\n        return Error\n    try:\n        pass\n    except Error:\n        pass\n",
        "def f(Unknown):\n    from getopt import GetoptError as Error\n    try:\n        pass\n    except (Error, Unknown):\n        pass\n",
    ];
    for (index, source) in cases.iter().enumerate() {
        let input = root.0.join(format!("negative_{index}.py"));
        std::fs::write(&input, source).unwrap();
        let target = match index {
            6 => "global.Error",
            8 => "(local.Error,local.Unknown)",
            _ => "local.Error",
        };
        accept_dynamic(&root.0, &input, &format!("dynamic_{index}"), None, target);
    }
    // 原全局删除源码逐字保留为正例；未调用f，CP/JIT均应成功且双流为空。
    // execute使用独立目录，不向后续source_shadow的原生导入判定注入额外源码。
    let (_deletion_root, cp, jit, _, _) = execute(
        "def f():\n    global Error\n    del Error\n",
        "global_delete.py",
        0,
        false,
        None,
    );
    assert!(cp.stdout.is_empty());
    assert!(cp.stderr.is_empty());
    assert!(jit.stdout.is_empty());
    assert!(jit.stderr.is_empty());
    // 同目录真实源码必须胜过原生名单，不可把源码Error猜成原生GetoptError。
    std::fs::write(root.0.join("getopt.py"), "GetoptError = ValueError\n").unwrap();
    let input = root.0.join("source_shadow.py");
    std::fs::write(&input, "def f():\n    import getopt\n    try:\n        pass\n    except getopt.GetoptError:\n        pass\n").unwrap();
    accept_dynamic(
        &root.0,
        &input,
        "source_shadow",
        Some(("getopt", "GetoptError = ValueError\n")),
        "local.getopt.GetoptError",
    );
}
fn accept_dynamic(
    root: &Path,
    input: &Path,
    label: &str,
    shadow: Option<(&str, &str)>,
    target: &str,
) {
    let mut session = Session::default();
    let mir = ryupy_driver::dump(&mut session, input, ryupy_driver::Stage::Mir)
        .expect("有限动态目标应保真实来源降级，不计CP/JIT成功");
    assert!(!session.diagnostics.has_errors());
    std::fs::write(root.join(format!("{label}.pipeline.mir")), mir).unwrap();
    dynamic_support::check(input, shadow, &[target]);
    for extension in ["hir", "mir"] {
        std::fs::rename(
            root.join(format!("dynamic.{extension}")),
            root.join(format!("{label}.{extension}")),
        )
        .unwrap();
    }
}
// 库帧精确到路径、行号、函数、源码；首次CP运行若不符合冻结预测即失败，不动态学习。
const PARSE_LIBRARY: &[(&str, &str)] = &[
    (
        "93, in getopt",
        "opts, args = do_longs(opts, args[0][2:], longopts, args[1:])",
    ),
    (
        "157, in do_longs",
        "has_arg, opt = long_has_args(opt, longopts)",
    ),
    (
        "174, in long_has_args",
        "raise GetoptError(_('option --%s not recognized') % opt, opt)",
    ),
];
const TRUTH_LIBRARY: &[(&str, &str)] = &[
    (
        "93, in getopt",
        "opts, args = do_longs(opts, args[0][2:], longopts, args[1:])",
    ),
    (
        "165, in do_longs",
        "opts.append(('--' + opt, optarg or ''))",
    ),
];
fn projection(
    output: &Output,
    input: &Path,
    source: &str,
    library: &[(&str, &str)],
    python: bool,
    stdlib: &Path,
) -> (Vec<String>, Vec<String>) {
    let text = String::from_utf8(output.stderr.clone()).unwrap();
    assert!(!text.contains("Unhandled exception"), "{text}");
    let lines: Vec<_> = text.lines().collect();
    let source_lines: Vec<_> = source.lines().collect();
    let name = input.file_name().unwrap().to_str().unwrap();
    let mut result = Vec::new();
    let mut removed = Vec::new();
    let mut index = 0;
    while index < lines.len() {
        let line = lines[index];
        if let Some(rest) = line.strip_prefix("  File \"") {
            let (path, position) = rest.rsplit_once("\", line ").unwrap();
            let (number, _) = position.split_once(", in ").unwrap();
            let number: usize = number.parse().unwrap();
            let library_frame = python && path == stdlib.to_str().unwrap();
            let expected_source;
            if library_frame {
                let (fixed_position, fixed_source) = library.get(removed.len()).expect("额外库帧");
                assert_eq!(position, *fixed_position);
                expected_source = *fixed_source;
                removed.push(position.to_owned());
            } else {
                assert!(
                    Path::new(path) == input || (!python && path == name),
                    "未知帧{path}"
                );
                expected_source = source_lines[number - 1].trim();
                result.push(format!("File {name}, line {position}"));
            }
            index += 1;
            // 明确的显示投影：验证而后移除源码/caret显示行，保留每个用户帧头与整条异常链。
            // 原始stderr始终留存；native若无显示行不补造，绝不按任意缩进整段过滤。
            if index < lines.len() && lines[index].starts_with("    ") {
                assert_eq!(lines[index].trim(), expected_source, "源码显示不匹配");
                index += 1;
                if index < lines.len() && lines[index].starts_with("    ") {
                    let marker = lines[index].trim();
                    assert!(
                        !marker.is_empty()
                            && marker.chars().all(|c| c == '^' || c == '~' || c == ' '),
                        "未知帧后内容"
                    );
                    index += 1;
                }
            } else {
                assert!(!python, "CP帧缺少源码显示");
            }
        } else {
            // 仅空行是展示层；所有类型、消息、连接词必须逐行进入投影。
            if !line.is_empty() {
                assert!(!line.starts_with(char::is_whitespace), "未知缩进内容{line}");
                result.push(line.to_owned());
            }
            index += 1;
        }
    }
    let expected_removed: Vec<_> = if python {
        library.iter().map(|(p, _)| (*p).to_owned()).collect()
    } else {
        vec![]
    };
    assert_eq!(removed, expected_removed);
    (result, removed)
}
fn chain(source: &str, filename: &str, library: &[(&str, &str)], fixed: &[&str]) {
    let (root, cp, jit, input, stdlib) =
        execute(source, filename, 1, false, Some((library, fixed)));
    assert_eq!(jit.stdout, cp.stdout);
    let (expected, removed) = projection(&cp, &input, source, library, true, &stdlib);
    let (actual, native_removed) = projection(&jit, &input, source, library, false, &stdlib);
    std::fs::write(root.0.join("python.projection"), expected.join("\n")).unwrap();
    std::fs::write(root.0.join("jit.projection"), actual.join("\n")).unwrap();
    std::fs::write(root.0.join("python.removed-frames"), removed.join("\n")).unwrap();
    assert!(native_removed.is_empty());
    assert_eq!(expected, fixed);
    assert_eq!(actual, fixed);
}
#[test]
fn parse_error_preserves_complete_context_and_cause_chain() {
    chain(
        include_str!("../../../tests/fixtures/getopt_core/parse_chain.py"),
        "getopt_parse_chain.py",
        PARSE_LIBRARY,
        &[
            "Traceback (most recent call last):",
            "File getopt_parse_chain.py, line 11, in <module>",
            "ValueError: outer-context",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File getopt_parse_chain.py, line 14, in <module>",
            "File getopt_parse_chain.py, line 7, in fail",
            "getopt.GetoptError: option --bad not recognized",
            "The above exception was the direct cause of the following exception:",
            "Traceback (most recent call last):",
            "File getopt_parse_chain.py, line 16, in <module>",
            "RuntimeError: outer-wrapper",
        ],
    );
}
#[test]
fn long_value_callback_preserves_complete_user_context_chain() {
    chain(
        include_str!("../../../tests/fixtures/getopt_core/long_truth_chain.py"),
        "getopt_long_truth.py",
        TRUTH_LIBRARY,
        &[
            "Traceback (most recent call last):",
            "File getopt_long_truth.py, line 17, in <module>",
            "RuntimeError: outer-context",
            "During handling of the above exception, another exception occurred:",
            "Traceback (most recent call last):",
            "File getopt_long_truth.py, line 19, in <module>",
            "File getopt_long_truth.py, line 13, in fail",
            "File getopt_long_truth.py, line 5, in __bool__",
            "ValueError: long-value-error",
        ],
    );
}

#[test]
fn deleted_exception_binding_is_read_only_when_matching() {
    raw(
        "def normal():\n    from getopt import GetoptError as Error\n    del Error\n    try:\n        pass\n    except Error:\n        pass\nnormal()\nprint('deleted.normal')\ndef failed():\n    from getopt import GetoptError as Error\n    del Error\n    try:\n        raise ValueError('original')\n    except Error:\n        pass\ntry:\n    failed()\nexcept UnboundLocalError as error:\n    print('deleted.match', str(error))\n",
        "getopt_deleted_binding.py",
        false,
    );
}
