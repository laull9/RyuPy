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
fn shadowing_rebinding_unknown_tuple_members_and_dynamic_types_are_rejected() {
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
        let (_, diagnostics) = lower(source, &["json", "re"]);
        assert!(
            diagnostics
                .iter()
                .any(|diagnostic| diagnostic.code == Some("E0100")),
            "应拒绝动态异常身份：{source}\n{diagnostics:?}"
        );
    }
}

#[test]
fn driver_identity_is_required_and_unrelated_local_names_do_not_poison_imports() {
    let source = "import json\ndef ordinary(json):\n    return json\ntry:\n    pass\nexcept json.JSONDecodeError:\n    pass\n";
    assert!(lower(source, &["json"]).1.is_empty());
    assert!(!lower(source, &[]).1.is_empty());
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
