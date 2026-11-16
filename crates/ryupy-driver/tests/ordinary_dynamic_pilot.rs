//! 首包原源CP/真实PE差分：候选普通import与同源公开loader均复用唯一环境。
//! 不覆盖已有case；feature关闭的发行默认保持原路径，测试用显式Session隔离候选。
#[path = "../src/test_temp.rs"]
mod test_temp;
use ryupy_driver::{BuildOptions, ModuleEnvironmentPolicy, Session};
use std::{path::Path, process::Command};
struct Root(test_temp::Root);
fn compare(directory: &Path, entry: &str, selected: &[&str]) {
    let root = Root(test_temp::Root::new("ordinary-dynamic"));
    let input = directory.join(entry);
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let expected = Command::new(python)
        .arg("-B")
        .arg(&input)
        .current_dir(directory)
        .output()
        .unwrap();
    std::fs::write(root.0.join("cpython.stdout"), &expected.stdout).unwrap();
    std::fs::write(root.0.join("cpython.stderr"), &expected.stderr).unwrap();
    assert!(
        expected.status.success() && expected.stderr.is_empty(),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    for explicit in [false, true] {
        let mut session = Session {
            module_environment_policy: if explicit {
                ModuleEnvironmentPolicy::ExplicitOnly
            } else {
                ModuleEnvironmentPolicy::OrdinaryDynamicPilot
            },
            instance_modules: if explicit {
                selected.iter().map(|name| (*name).into()).collect()
            } else {
                vec![]
            },
            ..Default::default()
        };
        let artifact = ryupy_driver::build(
            &mut session,
            &BuildOptions {
                input: input.clone(),
                out_dir: root.0.join(if explicit { "explicit" } else { "automatic" }),
                aot: false,
                search_paths: vec![],
            },
        )
        .unwrap_or_else(|error| panic!("{error}: {}", session.render_diagnostics(false)));
        for target in selected {
            let decision = session
                .module_environment_report
                .iter()
                .find(|decision| decision.module == *target)
                .unwrap();
            assert!(decision.compiled);
            assert_eq!(decision.execution, "InstanceGlobals");
            assert!(if explicit {
                decision.reason == "explicit-instance"
            } else {
                decision.reason.starts_with("ordinary-dynamic-pilot:")
            });
        }
        std::fs::write(
            root.0.join(if explicit {
                "explicit-plan.json"
            } else {
                "automatic-plan.json"
            }),
            session.module_environment_report_json().unwrap(),
        )
        .unwrap();
        let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
        let label = if explicit { "explicit" } else { "automatic" };
        std::fs::write(root.0.join(format!("{label}.stdout")), &actual.stdout).unwrap();
        std::fs::write(root.0.join(format!("{label}.stderr")), &actual.stderr).unwrap();
        assert_eq!(
            actual.status.code(),
            Some(0),
            "{}",
            String::from_utf8_lossy(&actual.stderr)
        );
        assert_eq!(actual.stdout, expected.stdout);
        assert_eq!(actual.stderr, expected.stderr);
    }
}
#[test]
fn ordinary_dynamic_complete_new_contracts() {
    let root =
        ryupy_driver::runtime::workspace_root().join("tests/fixtures/module_environment_pilot");
    for (directory, entry) in [
        ("probe", "entry.py"),
        ("probe", "loader_entry.py"),
        ("ordinary_retry_escape", "entry.py"),
        ("callee_binding_owner", "entry.py"),
    ] {
        compare(&root.join(directory), entry, &["subject"]);
    }
}
#[test]
fn ordinary_dynamic_preserves_complete_original_consumers() {
    let root = ryupy_driver::runtime::workspace_root().join("tests/cases");
    for (directory, target) in [
        ("phase3/m3_7_unbound_exports", "helper"),
        ("module_owners/function_globals", "subject"),
        ("module_owners/builtins_capture", "subject"),
        ("module_owners/generator_owner", "subject"),
        ("module_from_spec/direct_failure", "subject"),
    ] {
        compare(&root.join(directory), "entry.py", &[target]);
    }
}

#[test]
fn ordinary_dynamic_preserves_complete_live_defaults_and_owner() {
    // 原完整见证已经逐项断言默认tuple/元素身份、默认对象原地变更、
    // tuple替换后的下次调用，以及callee求值顺序和真实模块owner。
    // 两种实例选择必须同次对照CPython跑到文件末尾，不能停在旧AttributeError。
    let directory = ryupy_driver::runtime::workspace_root()
        .join("tests/fixtures/module_environment_pilot/callee_defaults_owner");
    compare(&directory, "entry.py", &["subject"]);
}

#[test]
fn ordinary_dynamic_live_keyword_defaults_keep_alias_identity_and_next_binding() {
    let root = Root(test_temp::Root::new("ordinary-defaults"));
    std::fs::write(
        root.0.join("subject.py"),
        "seed = []\ndef defaulted(value=seed, *, option=seed):\n    return value, option\n",
    )
    .unwrap();
    std::fs::write(
        root.0.join("entry.py"),
        r#"import subject
function = subject.defaulted
positional = function.__defaults__
keywords = function.__kwdefaults__
assert positional is function.__defaults__ and keywords is function.__kwdefaults__
assert function()[0] is subject.seed and function()[1] is subject.seed
replacement = []
keywords['option'] = replacement
assert function()[1] is replacement
replacement.append('shared')
assert function()[1] == ['shared']
new_positional = ['positional']
new_defaults = (new_positional,)
function.__defaults__ = new_defaults
assert function.__defaults__ is new_defaults and function()[0] is new_positional
del keywords['option']
try:
    function()
except TypeError as error:
    print(type(error).__name__, str(error))
else:
    raise AssertionError('deleted keyword default still used')
replacement_keywords = {'option': None}
function.__kwdefaults__ = replacement_keywords
assert function.__kwdefaults__ is replacement_keywords and function()[1] is None
del function.__defaults__
assert function.__defaults__ is None
try:
    function()
except TypeError as error:
    print(type(error).__name__, str(error))
else:
    raise AssertionError('deleted positional default still used')
print('live positional and keyword defaults preserve identity')
"#,
    )
    .unwrap();
    compare(&root.0, "entry.py", &["subject"]);
}

#[test]
fn native_textwrapper_defaults_feed_real_methods_and_instances() {
    let root = Root(test_temp::Root::new("textwrapper-defaults"));
    std::fs::write(
        root.0.join("entry.py"),
        r#"from textwrap import TextWrapper
original = TextWrapper.__init__.__defaults__
for width in (3, 8):
    replacement = (width,) + original[1:]
    TextWrapper.__init__.__defaults__ = replacement
    assert TextWrapper.__init__.__defaults__ is replacement
    instance = TextWrapper()
    assert instance.width == width
    print(instance.wrap('one two'))
keywords = TextWrapper.__init__.__kwdefaults__
token = []
keywords['placeholder'] = token
assert TextWrapper().placeholder is token
keywords['placeholder'] = 'updated'
assert TextWrapper().placeholder == 'updated'
instance = TextWrapper(4)
for text in ('one two', 'red blue'):
    TextWrapper.wrap.__defaults__ = (text,)
    TextWrapper.fill.__defaults__ = (text,)
    print(instance.wrap(), instance.fill())
del TextWrapper.wrap.__defaults__
try:
    instance.wrap()
except TypeError as error:
    print(type(error).__name__, str(error))
else:
    raise AssertionError('deleted wrap default still used')
"#,
    )
    .unwrap();
    compare(&root.0, "entry.py", &[]);
}

#[test]
fn native_scanner_and_namedtuple_binding_use_current_default_counts() {
    let root = Root(test_temp::Root::new("native-default-counts"));
    std::fs::write(
        root.0.join("entry.py"),
        r#"from re import Scanner
from typing import NamedTuple
scanner = Scanner([('a', 'old')])
lexicon = [('a', 'A')]
Scanner.__init__.__defaults__ = (scanner, lexicon, 0)
assert Scanner.__init__() is None
Scanner.scan.__defaults__ = (scanner, 'aa!')
print(Scanner.scan())
try:
    Scanner.__init__(scanner, lexicon, 0, 0)
except TypeError as error:
    print(type(error).__name__, str(error))
NamedTuple.__defaults__ = ('Fallback', [])
try:
    NamedTuple(1, 2, 3)
except TypeError as error:
    print(type(error).__name__, str(error))
"#,
    )
    .unwrap();
    compare(&root.0, "entry.py", &[]);
}

#[test]
fn ordinary_dynamic_keeps_unopened_code_and_closure_reflection_boundaries() {
    // 默认值已开放，代码对象与closure读取仍属于显式未实现合同；
    // closure写删保持readonly，不能把同名普通字典键当成真实元数据。
    for (statement, kind, message) in [
        (
            "print(subject.read.__code__)",
            "NotImplementedError",
            "function code reflection is not supported",
        ),
        (
            "subject.read.__code__ = None",
            "NotImplementedError",
            "function code reflection is not supported",
        ),
        (
            "del subject.read.__code__",
            "NotImplementedError",
            "function __code__ deletion is not supported",
        ),
        (
            "print(subject.read.__closure__)",
            "NotImplementedError",
            "instance function closure reflection is not supported",
        ),
        (
            "subject.read.__closure__ = None",
            "AttributeError",
            "readonly attribute",
        ),
        (
            "del subject.read.__closure__",
            "AttributeError",
            "readonly attribute",
        ),
        (
            "subject.read.__dict__['__code__'] = None\nprint(subject.read.__code__)",
            "NotImplementedError",
            "function code reflection is not supported",
        ),
        (
            "subject.read.__dict__['__closure__'] = None\nprint(subject.read.__closure__)",
            "NotImplementedError",
            "instance function closure reflection is not supported",
        ),
    ] {
        for automatic in [false, true] {
            let root = Root(test_temp::Root::new("ordinary-dynamic"));
            let input = root.0.join("entry.py");
            std::fs::write(root.0.join("subject.py"), "def read():\n    return 1\n").unwrap();
            std::fs::write(&input, format!("import subject\n{statement}\n")).unwrap();
            let mut session = Session {
                module_environment_policy: if automatic {
                    ModuleEnvironmentPolicy::OrdinaryDynamicPilot
                } else {
                    ModuleEnvironmentPolicy::ExplicitOnly
                },
                instance_modules: if automatic {
                    vec![]
                } else {
                    vec!["subject".into()]
                },
                ..Default::default()
            };
            let artifact = ryupy_driver::build(
                &mut session,
                &BuildOptions {
                    input: input.clone(),
                    out_dir: root.0.join("out"),
                    aot: false,
                    search_paths: vec![],
                },
            )
            .unwrap();
            let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
            std::fs::write(root.0.join("actual.stdout"), &actual.stdout).unwrap();
            std::fs::write(root.0.join("actual.stderr"), &actual.stderr).unwrap();
            assert_eq!(actual.status.code(), Some(1));
            assert!(actual.stdout.is_empty(), "{statement}");
            assert!(
                String::from_utf8_lossy(&actual.stderr).contains(&format!("{kind}: {message}")),
                "{statement}: {}",
                String::from_utf8_lossy(&actual.stderr)
            );
        }
    }
}
