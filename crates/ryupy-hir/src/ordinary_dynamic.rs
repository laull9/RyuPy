//! 普通动态模块试点的语法充分条件；只提供事实，不决定部署或读取文件。
//! 复用显式实例预检；完整签名即使后续会动态化也保守留旧路，不复制类型层模式推导。
use ryupy_syntax::ast::visitor::{Visitor, walk_stmt};
use ryupy_syntax::{ParsedModule, ast};

/// 资格失败不是编译错误；自动策略应保留原能力，而显式实例仍执行原预检。
#[derive(Debug, Clone, PartialEq, Eq)]
pub enum OrdinaryDynamicEligibility {
    Eligible { functions: usize },
    InstanceSyntaxBoundary,
    ClassDefinition,
    CompleteSignature,
    NoFunction,
}

pub fn ordinary_dynamic_eligibility(parsed: &ParsedModule) -> OrdinaryDynamicEligibility {
    if !instance_ast_supported(parsed, Default::default()) {
        return OrdinaryDynamicEligibility::InstanceSyntaxBoundary;
    }
    let scan = shape(parsed);
    if scan.has_class {
        OrdinaryDynamicEligibility::ClassDefinition
    } else if scan.complete_signature {
        OrdinaryDynamicEligibility::CompleteSignature
    } else if scan.functions == 0 {
        OrdinaryDynamicEligibility::NoFunction
    } else {
        OrdinaryDynamicEligibility::Eligible {
            functions: scan.functions,
        }
    }
}

/// 来源认证不等于ABI资格；此纯查询复用显式lower的唯一完整AST验证器，不发布诊断。
pub fn instance_ast_supported(
    parsed: &ParsedModule,
    capabilities: ryupy_base::InstanceCapabilities,
) -> bool {
    crate::instance_globals::validate(&parsed.module.body, parsed.file, capabilities).is_empty()
}

/// 只回答源码角色事实，不授予class或实例能力；包角色由driver真实resolver判断。
pub fn has_defined_functions(parsed: &ParsedModule) -> bool {
    shape(parsed).functions != 0
}

fn shape(parsed: &ParsedModule) -> Scan {
    let mut scan = Scan::default();
    for statement in &parsed.module.body {
        scan.visit_stmt(statement);
    }
    scan
}

#[derive(Default)]
struct Scan {
    functions: usize,
    has_class: bool,
    complete_signature: bool,
}
impl<'a> Visitor<'a> for Scan {
    fn visit_stmt(&mut self, statement: &'a ast::Stmt) {
        match statement {
            ast::Stmt::ClassDef(_) => self.has_class = true,
            ast::Stmt::FunctionDef(def) => {
                self.functions += 1;
                // 缺任一标注或含变长参数是初始Dynamic的充分条件；不推断闭包/类型。
                // 所有嵌套def也必须满足，避免仅检查模块外壳漏掉typed热函数。
                self.complete_signature |= def.returns.is_some()
                    && def.parameters.vararg.is_none()
                    && def.parameters.kwarg.is_none()
                    && def
                        .parameters
                        .iter()
                        .all(|parameter| parameter.annotation().is_some());
            }
            _ => {}
        }
        walk_stmt(self, statement);
    }
}
