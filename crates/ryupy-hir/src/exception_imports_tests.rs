//! 模块异常解析的身份与诊断边界；不依赖运行时解析 JSON 或正则。
use crate::{ModuleInfo, lower_module_with_native_exceptions};
use ryupy_base::SourceDb;

fn lower(source: &str, native: &[&str]) -> (crate::Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = SourceDb::default();
    let file = db.add("exceptions.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    lower_module_with_native_exceptions(
        &parsed,
        &ModuleInfo {
            instance_capabilities: Default::default(),
            instance_globals: false,
            resolve_file_at_runtime: false,
            name: "__main__".into(),
            package: String::new(),
            file: "exceptions.py".into(),
        },
        source,
        native,
    )
}

#[test]
fn authentic_module_and_from_aliases_lower_to_runtime_names() {
    let source = "import json as codec\nfrom re import error as PatternError\ndef decode():\n    try:\n        pass\n    except (codec.JSONDecodeError, PatternError):\n        pass\n";
    let (module, diagnostics) = lower(source, &["json", "re"]);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let types: Vec<_> = module
        .functions
        .iter()
        .flat_map(|(_, function)| {
            function
                .body
                .handlers
                .iter()
                .filter_map(|(_, handler)| handler.types.as_ref())
        })
        .flatten()
        .map(|name| name.as_str())
        .collect();
    assert_eq!(types, ["json.JSONDecodeError", "re.error"]);
    assert_eq!(module.protected_exports, ["PatternError", "codec"]);
}

#[test]
fn binascii_exception_aliases_preserve_module_identity() {
    let source = "import binascii as binary\nfrom binascii import Error as BinaryError\ntry:\n    pass\nexcept (binary.Error, BinaryError):\n    pass\n";
    let (module, diagnostics) = lower(source, &["binascii"]);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let types: Vec<_> = module
        .functions
        .iter()
        .flat_map(|(_, function)| function.body.handlers.iter())
        .filter_map(|(_, handler)| handler.types.as_ref())
        .flatten()
        .map(|name| name.as_str())
        .collect();
    assert_eq!(types, ["binascii.Error", "binascii.Error"]);
    assert_eq!(module.protected_exports, ["BinaryError", "binary"]);
}

#[test]
fn shadowing_rebinding_unknown_tuple_members_use_real_dynamic_targets() {
    for source in [
        "class json:\n    JSONDecodeError = ValueError\ntry:\n    pass\nexcept json.JSONDecodeError:\n    pass\n",
        "import json\njson = 1\ntry:\n    pass\nexcept json.JSONDecodeError:\n    pass\n",
        "import json\ndef f(json):\n    try:\n        pass\n    except json.JSONDecodeError:\n        pass\n",
        "import json\ndef outer(json):\n    def inner():\n        try:\n            pass\n        except json.JSONDecodeError:\n            pass\n    return inner\n",
        "from re import error as E\ndef change():\n    global E\n    E = ValueError\ntry:\n    pass\nexcept E:\n    pass\n",
        "import re\ndel re\ntry:\n    pass\nexcept re.error:\n    pass\n",
        "import re\nfrom json import loads as re\ntry:\n    pass\nexcept re.error:\n    pass\n",
        "if True:\n    import json\ntry:\n    pass\nexcept json.JSONDecodeError:\n    pass\n",
        "try:\n    pass\nexcept json.JSONDecodeError:\n    pass\nimport json\n",
        "import json\ntry:\n    pass\nexcept (ValueError, json.other):\n    pass\n",
        "import json\ntry:\n    pass\nexcept getattr(json, 'JSONDecodeError'):\n    pass\n",
    ] {
        if source.contains("getattr(") {
            // Call目标仍未开放；删除模块名已经独立接入真实动态目标读取。
            assert!(
                lower(source, &["json", "re"])
                    .1
                    .iter()
                    .any(|d| d.code == Some("E0100"))
            );
        } else {
            assert_dynamic(source, &["json", "re"]);
        }
    }
}

#[test]
fn driver_identity_is_required_and_unrelated_local_names_do_not_poison_imports() {
    let source = "import json\ndef ordinary(json):\n    return json\ntry:\n    pass\nexcept json.JSONDecodeError:\n    pass\n";
    assert!(lower(source, &["json"]).1.is_empty());
    assert_dynamic(source, &[]);
}

#[test]
fn imports_not_used_by_static_handlers_remain_mutable_exports() {
    let (module, diagnostics) = lower(
        "import json\nfrom re import error as E\nprint(json, E)\n",
        &["json", "re"],
    );
    assert!(diagnostics.is_empty());
    assert!(module.protected_exports.is_empty());
}

#[test]
fn zipfile_aliases_use_real_exception_identity_and_user_subclass_base() {
    let source = "import zipfile as archive\nfrom zipfile import BadZipFile as Bad\nclass Child(Bad):\n    pass\ntry:\n    raise Child('bad')\nexcept archive.BadZipFile:\n    pass\n";
    let (module, diagnostics) = lower(source, &["zipfile"]);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let types: Vec<_> = module
        .functions
        .iter()
        .flat_map(|(_, f)| f.body.handlers.iter().filter_map(|(_, h)| h.types.as_ref()))
        .flatten()
        .map(|s| s.as_str())
        .collect();
    assert_eq!(types, ["zipfile.BadZipFile"]);
    assert!(module.protected_exports.iter().any(|s| s == "archive"));
    assert!(module.protected_exports.iter().any(|s| s == "Bad"));
}
#[test]
fn zipfile_source_shadow_and_rebinding_do_not_gain_native_exception_identity() {
    let source = "import zipfile\ntry:\n    pass\nexcept zipfile.BadZipFile:\n    pass\n";
    assert_dynamic(source, &[]);
    for source in [
        "import zipfile\nzipfile = None\ntry:\n    pass\nexcept zipfile.BadZipFile:\n    pass\n",
        "from zipfile import BadZipFile as Bad\nBad = ValueError\ntry:\n    pass\nexcept Bad:\n    pass\n",
        "import zipfile\ndef f(zipfile):\n    try:\n        pass\n    except zipfile.BadZipFile:\n        pass\n",
    ] {
        assert_dynamic(source, &["zipfile"]);
    }
}

#[test]
fn optparse_six_aliases_are_native_typed_handlers_not_user_bases() {
    for name in [
        "OptParseError",
        "OptionError",
        "OptionConflictError",
        "OptionValueError",
        "BadOptionError",
        "AmbiguousOptionError",
    ] {
        let source = format!(
            "import optparse as options\nfrom optparse import {name} as E\ntry:\n    pass\nexcept (options.{name}, E):\n    pass\n"
        );
        let (module, diagnostics) = lower(&source, &["optparse"]);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let types: Vec<_> = module
            .functions
            .iter()
            .flat_map(|(_, f)| f.body.handlers.iter().filter_map(|(_, h)| h.types.as_ref()))
            .flatten()
            .map(|n| n.as_str())
            .collect();
        assert_eq!(
            types,
            [format!("optparse.{name}"), format!("optparse.{name}")]
        );
        for source in [
            format!("from optparse import {name} as E\nclass Child(E):\n    pass\n"),
            format!("import optparse\nclass Child(optparse.{name}):\n    pass\n"),
        ] {
            assert!(
                lower(&source, &["optparse"])
                    .1
                    .iter()
                    .any(|d| d.code == Some("E0100")),
                "{source}"
            );
        }
    }
}
#[test]
fn optparse_source_and_rebound_names_do_not_gain_typed_identity() {
    let source = "import optparse\ntry:\n    pass\nexcept optparse.OptionError:\n    pass\n";
    assert_dynamic(source, &[]);
    for source in [
        "from optparse import OptionError as E\nE = ValueError\ntry:\n    pass\nexcept E:\n    pass\n",
        "import optparse\ndef f(optparse):\n    try:\n        pass\n    except optparse.OptionError:\n        pass\n",
    ] {
        assert_dynamic(source, &["optparse"]);
    }
    let user = "class OptionError:\n    pass\nclass Child(OptionError):\n    pass\n";
    assert!(lower(user, &["optparse"]).1.is_empty());
}

#[test]
fn getopt_aliases_share_identity_and_user_derivation_is_rejected() {
    let source = "import getopt as opts\nfrom getopt import error as E\ntry:\n    pass\nexcept (opts.GetoptError, opts.error, E):\n    pass\n";
    let (module, diagnostics) = lower(source, &["getopt"]);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let types: Vec<_> = module
        .functions
        .iter()
        .flat_map(|(_, f)| f.body.handlers.iter().filter_map(|(_, h)| h.types.as_ref()))
        .flatten()
        .map(|s| s.as_str())
        .collect();
    assert_eq!(types, ["getopt.GetoptError"; 3]);
    assert_eq!(module.protected_exports, ["E", "opts"]);
    assert_dynamic(source, &[]);
    for name in ["GetoptError", "error"] {
        for source in [
            format!("import getopt\nclass Child(getopt.{name}):\n    pass\n"),
            format!("from getopt import {name} as E\nclass Child(E):\n    pass\n"),
        ] {
            assert!(
                lower(&source, &["getopt"])
                    .1
                    .iter()
                    .any(|d| d.code == Some("E0100"))
            );
        }
    }
}

// 原拒绝案例现在保真实表达式，仍不得借同名导入获得typed身份。
fn assert_dynamic(source: &str, native: &[&str]) {
    let (module, diagnostics) = lower(source, native);
    assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
    let handlers: Vec<_> = module
        .functions
        .iter()
        .flat_map(|(_, f)| f.body.handlers.iter().map(|(_, h)| h))
        .collect();
    assert!(!handlers.is_empty(), "{source}");
    assert!(
        handlers
            .iter()
            .all(|h| h.types.is_none() && h.type_expr.is_some()),
        "{source}"
    );
    assert!(module.protected_exports.is_empty(), "{source}");
}

#[test]
fn xml_parse_error_uses_only_verified_import_identity() {
    let source = "import xml.etree.ElementTree as ET\nfrom xml.etree.ElementTree import ParseError as P\ntry:\n    pass\nexcept (ET.ParseError, P, SyntaxError):\n    pass\n";
    let (module, diagnostics) = lower(source, &["xml.etree.ElementTree"]);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let types: Vec<_> = module
        .functions
        .iter()
        .flat_map(|(_, f)| f.body.handlers.iter().filter_map(|(_, h)| h.types.as_ref()))
        .flatten()
        .map(|n| n.as_str())
        .collect();
    assert_eq!(
        types,
        [
            "xml.etree.ElementTree.ParseError",
            "xml.etree.ElementTree.ParseError",
            "SyntaxError"
        ]
    );
    assert_dynamic(
        "import xml.etree.ElementTree as ET\ntry:\n    pass\nexcept ET.ParseError:\n    pass\n",
        &[],
    );
    assert_dynamic(
        "import xml.etree.ElementTree as ET\nP = ET.ParseError\ntry:\n    pass\nexcept P:\n    pass\n",
        &["xml.etree.ElementTree"],
    );
    assert_dynamic(
        "def f(ET):\n    try:\n        pass\n    except ET.ParseError:\n        pass\n",
        &["xml.etree.ElementTree"],
    );
}

#[test]
fn syntax_and_xml_user_subclasses_reject_before_any_clr_constructor() {
    for source in [
        "class Child(SyntaxError):\n    pass\n",
        "import xml.etree.ElementTree as ET\nclass Child(ET.ParseError):\n    pass\n",
        "from xml.etree.ElementTree import ParseError as P\nclass Child(P):\n    pass\n",
    ] {
        let (module, diagnostics) = lower(source, &["xml.etree.ElementTree"]);
        assert!(
            diagnostics
                .iter()
                .any(|d| d.code == Some("E0100") && d.message.contains("用户派生")),
            "{source}\n{diagnostics:?}"
        );
        assert!(
            !module.classes.iter().any(|(_, c)| c.is_exception),
            "不得生成新族用户异常工厂"
        );
    }
    let (_, diagnostics) = lower(
        "class SyntaxError:\n    pass\nclass Child(SyntaxError):\n    pass\n",
        &[],
    );
    assert!(
        diagnostics.is_empty(),
        "同名用户类不受新族守卫影响：{diagnostics:?}"
    );
}

#[test]
fn stable_leaf_xml_imports_share_one_exception_identity() {
    for (import, target) in [
        ("import xml.etree.ElementTree as ET", "ET.ParseError"),
        ("import xml.etree.ElementTree as tree", "tree.ParseError"),
        (
            "from xml.etree.ElementTree import ParseError as Base",
            "Base",
        ),
        ("from xml.etree.ElementTree import ParseError", "ParseError"),
    ] {
        let source = format!("{import}\nclass Child({target}):\n    pass\n");
        let (_, diagnostics) = lower(&source, &["xml.etree.ElementTree"]);
        let diagnostic = diagnostics
            .iter()
            .find(|d| d.code == Some("E0100") && d.message.contains("用户派生"))
            .expect("稳定原生身份必须精确拒绝");
        assert_eq!(
            usize::from(diagnostic.primary.span.range.start()),
            import.len() + 1
        );
        let source = format!("{import}\ntry:\n    pass\nexcept {target}:\n    pass\n");
        let (module, diagnostics) = lower(&source, &["xml.etree.ElementTree"]);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        let types: Vec<_> = module
            .functions
            .iter()
            .flat_map(|(_, f)| f.body.handlers.iter().filter_map(|(_, h)| h.types.as_ref()))
            .flatten()
            .map(|n| n.as_str())
            .collect();
        assert_eq!(types, ["xml.etree.ElementTree.ParseError"]);
    }
}

/// 直接验证身份证明，不让其它未支持语法的E0100掩盖假认证。
fn assert_no_native_base_identity(source: &str) {
    let mut db = SourceDb::default();
    let file = db.add("identity.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
    let globals = crate::scope::GlobalScope {
        exception_imports: crate::exception_imports::collect(
            &parsed.module.body,
            &["xml.etree.ElementTree"],
        ),
        ..Default::default()
    };
    let class = parsed
        .module
        .body
        .iter()
        .find_map(|stmt| match stmt {
            ryupy_syntax::ast::Stmt::ClassDef(class) if class.name.as_str() == "Child" => {
                Some(class)
            }
            _ => None,
        })
        .expect("每项都提供Child基类探针");
    assert!(
        !crate::native_bases::unsupported_custom_exception_base(class, &globals),
        "{source}"
    );
    assert!(crate::exception_imports::protected_names(&globals.exception_imports).is_empty());
}

#[test]
fn xml_wrong_paths_and_all_unstable_bindings_remain_unknown() {
    for source in [
        "import xml.etree.ElementTree\nclass Child(xml.ParseError): pass\n",
        "import xml.etree.ElementTree as ET\nclass Child(ET.ElementTree.ParseError): pass\n",
        "from xml.etree.ElementTree import ParseError as Base\nclass Ordinary: pass\nBase = Ordinary\nclass Child(Base): pass\n",
        "import xml.etree.ElementTree as ET\nclass Holder: pass\nET = Holder\nclass Child(ET.ParseError): pass\n",
        "import xml.etree.ElementTree as ET\ndel ET\nET = None\nclass Child(ET.ParseError): pass\n",
        "if True:\n    import xml.etree.ElementTree as ET\nclass Child(ET.ParseError): pass\n",
        "class Child(ET.ParseError): pass\nimport xml.etree.ElementTree as ET\n",
        "import xml.etree.ElementTree as ET\ndef change():\n    global ET\n    ET = None\nclass Child(ET.ParseError): pass\n",
        "import xml.etree.ElementTree\nxml.etree = None\nclass Child(xml.etree.ElementTree.ParseError): pass\n",
        "from xml.etree import ElementTree as ET\nclass Child(ET.ParseError): pass\n",
        "import xml.etree.ElementTree\ndel xml.etree.ElementTree\nclass Child(xml.etree.ElementTree.ParseError): pass\n",
        "import xml.etree.ElementTree as ET\nif (ET := None): pass\nclass Child(ET.ParseError): pass\n",
        "import xml.etree.ElementTree as ET\ndef change(value=(ET := None)): pass\nclass Child(ET.ParseError): pass\n",
        "import xml.etree.ElementTree as ET\ndef change():\n    ET.ParseError = None\nclass Child(ET.ParseError): pass\n",
        "import xml.etree.ElementTree as ET\nimport xml.etree.ElementTree as ET\nclass Child(ET.ParseError): pass\n",
        // 真实身份由后续运行时门判断；这里仅断言未凭赋值/容器传播静态身份。
        "Base = SyntaxError\nclass Child(Base): pass\n",
        "import xml.etree.ElementTree as ET\nBase = ET.ParseError\nclass Child(Base): pass\n",
        "import xml.etree.ElementTree as ET\nitems = [ET.ParseError]\nclass Child(items[0]): pass\n",
    ] {
        assert_no_native_base_identity(source);
    }
}

#[test]
fn wrong_xml_handler_paths_never_freeze_module_exports() {
    for (import, target) in [
        ("import xml.etree.ElementTree", "xml.ParseError"),
        (
            "import xml.etree.ElementTree as ET",
            "ET.ElementTree.ParseError",
        ),
        ("from xml.etree import ElementTree as ET", "ET.ParseError"),
        (
            "import xml.etree.ElementTree\nxml.etree = None",
            "xml.etree.ElementTree.ParseError",
        ),
    ] {
        assert_dynamic(
            &format!("{import}\ntry:\n    pass\nexcept {target}:\n    pass\n"),
            &["xml.etree.ElementTree"],
        );
    }
}

#[test]
fn ordinary_user_names_and_old_native_bases_keep_their_domains() {
    for source in [
        "class ParseError: pass\nclass Child(ParseError): pass\n",
        "class SyntaxError: pass\nclass Child(SyntaxError): pass\n",
        "import zipfile\nclass Child(zipfile.BadZipFile): pass\n",
        "import zipfile\nclass Holder:\n    zipfile = 1\nclass Child(zipfile.BadZipFile): pass\n",
        "import io\nclass Child(io.UnsupportedOperation): pass\n",
        "class Ordinary: pass\nclass Child(Ordinary): pass\n",
        "import unittest\nclass Child(unittest.TestCase): pass\n",
        "import enum\nclass Child(enum.Enum):\n    FIRST = 1\n",
    ] {
        let (_, diagnostics) = lower(
            source,
            &["xml.etree.ElementTree", "zipfile", "io", "unittest", "enum"],
        );
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
    }
}

#[test]
fn unrelated_attribute_writes_do_not_cancel_native_type_identity() {
    for (import, write, target, expected) in [
        (
            "import io",
            "io.unrelated = 1",
            "io.UnsupportedOperation",
            "io.UnsupportedOperation",
        ),
        (
            "from xml.etree.ElementTree import ParseError as Base",
            "Base.unrelated = 1",
            "Base",
            "xml.etree.ElementTree.ParseError",
        ),
    ] {
        let source = format!("{import}\n{write}\ntry:\n    pass\nexcept {target}:\n    pass\n");
        let (module, diagnostics) = lower(&source, &["xml.etree.ElementTree", "io"]);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        let types: Vec<_> = module
            .functions
            .iter()
            .flat_map(|(_, f)| f.body.handlers.iter().filter_map(|(_, h)| h.types.as_ref()))
            .flatten()
            .map(|n| n.as_str())
            .collect();
        assert_eq!(types, [expected]);
    }
}

#[test]
fn comprehension_scope_preserves_imports_but_not_outer_walrus_or_attribute_writes() {
    // 直接测候选：属性目标/赋值表达式若尚未开放lower，也不能靠其它诊断掩盖作用域错误。
    for (expression, stable) in [
        ("[0 for io in []]", true),
        ("{io for io in []}", true),
        ("{io: io for io in []}", true),
        ("(io for io in [])", true),
        ("[0 for (io, *rest) in []]", true),
        ("[0 for [io] in []]", true),
        ("[0 for io in [] for io.UnsupportedOperation in []]", true),
        ("[[0 for io.UnsupportedOperation in []] for io in []]", true),
        ("[[0 for io in []] for other in []]", true),
        ("[(lambda: (io := None)) for item in []]", true),
        ("[(io := None) for item in []]", false),
        ("[item for item in [] if (io := None)]", false),
        ("{(io := None): item for item in []}", false),
        ("[[(io := None) for inner in []] for outer in []]", false),
        ("[0 for io.UnsupportedOperation in []]", false),
        (
            "[0 for item in [] for io.UnsupportedOperation in []]",
            false,
        ),
        // 最左iterable还在外层，不能被后续io局部绑定遮住真实模块属性写。
        ("[0 for io in [0 for io.UnsupportedOperation in []]]", false),
    ] {
        let source = format!("import io\nvalues = {expression}\n");
        let mut db = SourceDb::default();
        let file = db.add("comprehension_scope.py", &source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, &source);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        let imports = crate::exception_imports::collect(&parsed.module.body, &["io"]);
        assert_eq!(
            imports.contains_key(&crate::hir::Name::from("io")),
            stable,
            "{source}"
        );
    }
    for (scope, stable) in [("", true), ("    global io\n", false)] {
        let source =
            format!("import io\ndef f():\n{scope}    values = [(io := None) for item in []]\n");
        let mut db = SourceDb::default();
        let file = db.add("walrus_scope.py", &source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, &source);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        let imports = crate::exception_imports::collect(&parsed.module.body, &["io"]);
        assert_eq!(
            imports.contains_key(&crate::hir::Name::from("io")),
            stable,
            "{source}"
        );
    }
}

#[test]
fn comprehension_locals_do_not_downgrade_old_native_class_domains() {
    for (module_name, base, body) in [
        ("io", "UnsupportedOperation", "pass"),
        ("zipfile", "BadZipFile", "pass"),
        ("unittest", "TestCase", "pass"),
        ("enum", "Enum", "FIRST = 1"),
    ] {
        let source = format!(
            "import {module_name}\nvalues = [0 for {module_name} in []]\nclass Child({module_name}.{base}):\n    {body}\n"
        );
        let (module, diagnostics) = lower(&source, &[module_name]);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        assert_eq!(module.classes.len(), 1, "{source}");
        assert_eq!(
            module.classes.iter().next().unwrap().1.name.as_str(),
            "Child"
        );
        assert!(
            module
                .protected_exports
                .iter()
                .any(|name| name == module_name),
            "{source}"
        );
        assert!(
            module
                .classes
                .iter()
                .all(|(_, class)| !matches!(class.base, Some(crate::ClassBase::Runtime(_)))),
            "{source}"
        );
    }
}

#[test]
fn unaliased_mutable_parent_paths_are_explicitly_outside_static_proof() {
    // 002合同修正：完整无别名链也保留Runtime，不能只过滤已见到的同根属性写。
    for prefix in [
        "import xml.etree.ElementTree\n",
        "import xml.etree.ElementTree\nclass Ordinary: pass\nclass FakeET:\n    ParseError = Ordinary\nalias = xml.etree\nalias.ElementTree = FakeET\n",
    ] {
        assert_no_native_base_identity(&format!(
            "{prefix}class Child(xml.etree.ElementTree.ParseError): pass\n"
        ));
        assert_dynamic(
            &format!(
                "{prefix}try:\n    pass\nexcept xml.etree.ElementTree.ParseError:\n    pass\n"
            ),
            &["xml.etree.ElementTree"],
        );
    }
}
