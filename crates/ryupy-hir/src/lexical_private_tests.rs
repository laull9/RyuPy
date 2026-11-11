//! 新工厂私名边界：静态拒绝只覆盖需要改写的词法位置，不误伤旧类或字符串。
fn diagnostics(source: &str) -> Vec<ryupy_base::Diagnostic> {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("private.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    crate::lexical_private::validate(file, &parsed.module.body)
}
#[test]
fn lexical_class_private_names_reject_all_lexical_binding_positions() {
    for statement in [
        "__field = 1",
        "value = __free",
        "value = obj.__attr",
        "def __method(self): pass",
        "def method(self, __param): pass",
        "def method(self):\n            global __global",
        "def method(self):\n            nonlocal __nonlocal",
        "def method(self):\n            import math as __module",
        "def method(self):\n            from math import pi as __pi",
        "def method(self):\n            def nested(): return __free",
    ] {
        let source = format!("def make():\n    class C:\n        {statement}\n");
        assert!(!diagnostics(&source).is_empty(), "{source}");
    }
}
#[test]
fn lexical_class_private_names_keep_old_domain_and_non_mangled_tokens() {
    for source in [
        "class Old:\n    __field = 1\n    def method(self): return self.__field\n",
        "def make():\n    class C:\n        value = '__private'\n        def f(self): return __class__\n",
        "def make():\n    class ___:\n        __field = 1\n",
        "def make():\n    class C:\n        value = f(__keyword=1)\n",
        "def make():\n    class __C:\n        value = 1\n",
    ] {
        assert!(diagnostics(source).is_empty(), "{source}");
    }
}
#[test]
fn lexical_class_private_names_use_outer_header_and_inner_body_contexts() {
    for source in [
        "class Outer:\n    class __Inner: pass\n",
        "class Outer:\n    class Inner(__base): pass\n",
        "class Outer:\n    @__deco\n    class Inner: pass\n",
        "def make():\n    class Outer:\n        class ___:\n            def f(self): return self.__plain\n        class Inner:\n            __private = 1\n",
    ] {
        assert!(!diagnostics(source).is_empty(), "{source}");
    }
    assert!(diagnostics("class ___:\n    class Inner(__base): pass\n").is_empty());
}

#[test]
fn lexical_private_except_bindings_are_checked_even_without_name_reads() {
    for body in ["pass", "return _C__e"] {
        let source = format!(
            "def make():\n    class C:\n        def f(self):\n            try: raise ValueError()\n            except ValueError as __e: {body}\n"
        );
        assert!(!diagnostics(&source).is_empty());
    }
    for source in [
        "class Old:\n    def f(self):\n        try: pass\n        except ValueError as __e: pass\n",
        "def make():\n    class ___:\n        def f(self):\n            try: pass\n            except ValueError as __e: pass\n",
        "def make():\n    class C:\n        def f(self):\n            try: pass\n            except ValueError as __e__: pass\n",
    ] {
        assert!(diagnostics(source).is_empty(), "{source}");
    }
}
