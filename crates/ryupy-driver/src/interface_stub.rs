//! 源码与stub的静态接口提取：五类参数、原文注解、overload、类/属性/导入均保留来源。
//! 函数体不求值；条件分支全部记录，动态导出和语法缺口显式登记，供后续证据驱动推导。
use ryupy_base::Ranged;
use ryupy_syntax::ast::{
    self,
    visitor::{Visitor, walk_stmt},
};
use serde_json::{Value, json};
use std::collections::BTreeSet;

pub(super) fn extract(text: &str, path: &str, version: ast::PythonVersion, is_stub: bool) -> Value {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add(path, text);
    let (parsed, diagnostics) = ryupy_syntax::parse_for_version(
        file,
        text,
        if is_stub {
            ast::PySourceType::Stub
        } else {
            ast::PySourceType::Python
        },
        version,
    );
    let mut scan = Scan {
        text,
        line_starts: std::iter::once(0)
            .chain(text.match_indices('\n').map(|(i, _)| i + 1))
            .collect(),
        scope: String::new(),
        conditional: 0,
        declarations: vec![],
        unresolved: vec![],
        overloads: BTreeSet::from(["overload".into(), "typing.overload".into()]),
    };
    // overload别名是语法来源事实；不会导入typing或执行装饰器。
    for statement in &parsed.module.body {
        if let ast::Stmt::ImportFrom(import) = statement
            && import
                .module
                .as_ref()
                .is_some_and(|m| m.as_str() == "typing")
        {
            for name in &import.names {
                if name.name.as_str() == "overload" {
                    scan.overloads
                        .insert(name.asname.as_ref().unwrap_or(&name.name).to_string());
                }
            }
        }
    }
    for statement in &parsed.module.body {
        scan.visit_stmt(statement);
    }
    let diagnostics: Vec<_> = diagnostics.iter().map(|d| format!("{d:?}")).collect();
    json!({"role":if is_stub {"declaration_only"} else {"source_declarations"},
        "business_body":if is_stub {json!(false)}else{Value::Null},"contains_body_source":!is_stub,"analysis":"static_ast_without_import_or_evaluation",
        "python_target":version.to_string(),"parse_status":if diagnostics.is_empty(){"parsed"}else{"partial"},
        "diagnostics":diagnostics,"declarations":scan.declarations,"unresolved":scan.unresolved,
        "declaration_scope":"module_and_class_all_branches","dynamic_export_set":"not_resolved",
        "complete":false,"machine_abi":{"status":"not_derived","evidence":"Python级声明与原文注解；本次提取未消费C头文件或布局证据"},
        "ownership":{"status":"not_derived","evidence":"Python级调用形状；后续推导需登记额外证据及验证条件"}})
}
struct Scan<'a> {
    text: &'a str,
    line_starts: Vec<usize>,
    scope: String,
    conditional: usize,
    declarations: Vec<Value>,
    unresolved: Vec<Value>,
    overloads: BTreeSet<String>,
}
impl Scan<'_> {
    fn text(&self, node: &impl Ranged) -> &str {
        let range = node.range();
        self.text
            .get(usize::from(range.start())..usize::from(range.end()))
            .unwrap_or("")
    }
    fn line(&self, node: &impl Ranged) -> usize {
        let offset = usize::from(node.range().start());
        self.line_starts.partition_point(|start| *start <= offset)
    }
    fn qualified(&self, name: &str) -> String {
        if self.scope.is_empty() {
            name.into()
        } else {
            format!("{}.{name}", self.scope)
        }
    }
    fn entry(&self, node: &impl Ranged, kind: &str, name: &str) -> Value {
        json!({"kind":kind,"name":self.qualified(name),"line":self.line(node),
            "conditional":self.conditional>0,"private":name.starts_with('_')})
    }
    fn parameter(
        &self,
        parameter: &ast::Parameter,
        default: Option<&ast::Expr>,
        kind: &str,
    ) -> Value {
        json!({"name":parameter.name.as_str(),"kind":kind,
            "annotation":parameter.annotation.as_deref().map(|n| self.text(n)),
            "required":default.is_none() && !kind.starts_with("var_"),"default":default.map(|n|self.text(n))})
    }
    fn function(&self, function: &ast::StmtFunctionDef) -> Value {
        let mut parameters = vec![];
        for (group, kind) in [
            (&function.parameters.posonlyargs, "positional_only"),
            (&function.parameters.args, "positional_or_keyword"),
        ] {
            parameters.extend(
                group
                    .iter()
                    .map(|p| self.parameter(&p.parameter, p.default.as_deref(), kind)),
            );
        }
        if let Some(p) = &function.parameters.vararg {
            parameters.push(self.parameter(p, None, "var_positional"));
        }
        parameters.extend(
            function
                .parameters
                .kwonlyargs
                .iter()
                .map(|p| self.parameter(&p.parameter, p.default.as_deref(), "keyword_only")),
        );
        if let Some(p) = &function.parameters.kwarg {
            parameters.push(self.parameter(p, None, "var_keyword"));
        }
        let decorators: Vec<_> = function
            .decorator_list
            .iter()
            .map(|d| self.text(&d.expression))
            .collect();
        let mut entry = self.entry(function, "function", function.name.as_str());
        entry["declaration_line"] = entry["line"].clone();
        entry["line"] = json!(self.line(&function.name));
        entry["parameters"] = json!(parameters);
        entry["returns"] = json!(function.returns.as_deref().map(|r| self.text(r)));
        entry["async"] = json!(function.is_async);
        entry["overload"] = json!(decorators.iter().any(|d| self.overloads.contains(*d)));
        entry["decorators"] = json!(decorators);
        entry["type_parameters"] = json!(function.type_params.as_deref().map(|t| self.text(t)));
        entry["body_inspected"] = false.into();
        entry
    }
}
impl<'a> Visitor<'a> for Scan<'_> {
    fn visit_stmt(&mut self, statement: &'a ast::Stmt) {
        match statement {
            ast::Stmt::FunctionDef(f) => self.declarations.push(self.function(f)),
            ast::Stmt::ClassDef(c) => {
                let mut entry=self.entry(c,"class",c.name.as_str());
                entry["bases"]=json!(c.arguments.as_deref().map(|a|self.text(a)));
                entry["type_parameters"]=json!(c.type_params.as_deref().map(|t|self.text(t)));
                entry["decorators"]=json!(c.decorator_list.iter().map(|d|self.text(&d.expression)).collect::<Vec<_>>());
                self.declarations.push(entry);
                let qualified=self.qualified(c.name.as_str());
                let previous=std::mem::replace(&mut self.scope,qualified);
                for child in &c.body { self.visit_stmt(child); }
                self.scope=previous;
            }
            ast::Stmt::AnnAssign(a) => {
                let mut entry=self.entry(a,"annotated_binding",self.text(&*a.target));
                entry["annotation"]=json!(self.text(&*a.annotation));
                entry["value"]=json!(a.value.as_deref().map(|v|self.text(v))); self.declarations.push(entry);
            }
            ast::Stmt::Assign(a) => for target in &a.targets {
                let mut entry=self.entry(a,"binding",self.text(target));
                entry["value"]=json!(self.text(&*a.value)); self.declarations.push(entry);
            },
            ast::Stmt::TypeAlias(a) => {
                let mut entry=self.entry(a,"type_alias",self.text(&*a.name));
                entry["declaration"]=json!(self.text(a));self.declarations.push(entry);
            }
            ast::Stmt::Import(i) => for alias in &i.names {
                let mut entry=self.entry(i,"import",alias.asname.as_ref().unwrap_or(&alias.name).as_str());
                entry["target"]=json!(alias.name.as_str());self.declarations.push(entry);
            },
            ast::Stmt::ImportFrom(i) => for alias in &i.names {
                let mut entry=self.entry(i,"import",alias.asname.as_ref().unwrap_or(&alias.name).as_str());
                entry["target"]=json!(format!("{}{}{}", ".".repeat(i.level as usize),i.module.as_ref().map_or("",|m|m.as_str()),format_args!(".{}",alias.name)));
                self.declarations.push(entry);
            },
            ast::Stmt::If(_) | ast::Stmt::Try(_) | ast::Stmt::Match(_) | ast::Stmt::For(_) | ast::Stmt::While(_) | ast::Stmt::With(_) => {
                self.conditional+=1; walk_stmt(self,statement);self.conditional-=1;
            }
            ast::Stmt::Pass(_) | ast::Stmt::Expr(_) => {}
            other => self.unresolved.push(json!({"line":self.entry(other,"unresolved","")["line"],"syntax":self.text(other),"reason":"运行语句可改变导出；静态声明目录不求值"})),
        }
    }
}
