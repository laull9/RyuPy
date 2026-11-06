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
