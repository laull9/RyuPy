//! 高层边拒绝的有界失败见证。原图摘要在capture时取得，永不参与准入或反推旧图；
//! 仅打印结构ID/操作数类别，不包含函数名、路径、环境或字符串常量内容。
use crate::{BlockId, Body, Constant, FnId, Operand, Terminator};

const MAX_CHAIN_NODES: usize = 8;
const MAX_ERROR_BYTES: usize = 8192;
const TRUNCATED: &str = "\n[edge-witness-v1 total_truncated=true]\n";

pub(super) struct EdgeWitness {
    terminator: String,
    chains: Vec<Chain>,
}

struct Chain {
    target: u32,
    nodes: Vec<Node>,
    stop: &'static str,
}

struct Node {
    block: u32,
    statements: usize,
    terminator: String,
}

fn number(id: BlockId) -> u32 {
    id.into_raw().into_u32()
}

fn operand(value: &Operand) -> String {
    match value {
        Operand::Local(id) => format!("local#{}", id.into_raw().into_u32()),
        Operand::Global(id) => format!("global#{}", id.into_raw().into_u32()),
        Operand::Array(values) => format!("array(len={},contents=redacted)", values.len()),
        Operand::Const(value) => match value {
            Constant::I64(_) => "const(i64,redacted)".into(),
            Constant::F64(_) => "const(f64,redacted)".into(),
            Constant::Bool(value) => format!("const(bool,{value})"),
            Constant::Str(_) => "const(str,redacted)".into(),
            Constant::BigInt(_) => "const(bigint,redacted)".into(),
            Constant::Null => "const(null)".into(),
            Constant::None => "const(none)".into(),
        },
    }
}

fn optional_operand(value: &Option<Operand>) -> String {
    value.as_ref().map_or_else(|| "absent".into(), operand)
}

fn terminator(value: &Terminator) -> String {
    match value {
        Terminator::Goto(to) => format!("Goto(bb{})", number(*to)),
        Terminator::Branch { cond, then_, else_ } => format!(
            "Branch(cond={},then=bb{},else=bb{})",
            operand(cond),
            number(*then_),
            number(*else_)
        ),
        Terminator::Suspend {
            value,
            resume,
            input,
            delegated,
        } => format!(
            "Suspend(value={},resume=bb{},input={:?},delegated={delegated})",
            operand(value),
            number(*resume),
            input.map(|id| id.into_raw().into_u32())
        ),
        Terminator::Return(value) => format!("Return({})", optional_operand(value)),
        Terminator::Raise { value, cause } => format!(
            "Raise(value={},cause={})",
            optional_operand(value),
            optional_operand(cause)
        ),
        Terminator::Unreachable => "Unreachable".into(),
    }
}

impl EdgeWitness {
    pub(super) fn capture(body: &Body, edge: &Terminator) -> Self {
        // 数组的类型保证每个terminator最多两个目标，不递归展开分支子图。
        let targets = match edge {
            Terminator::Goto(to) | Terminator::Suspend { resume: to, .. } => [Some(*to), None],
            Terminator::Branch { then_, else_, .. } => [Some(*then_), Some(*else_)],
            _ => [None, None],
        };
        Self {
            terminator: terminator(edge),
            chains: targets
                .into_iter()
                .flatten()
                .map(|id| chain(body, id))
                .collect(),
        }
    }

    fn render(&self, label: &str, output: &mut String) {
        output.push_str(&format!("{label}.terminator={}\n", self.terminator));
        for (index, chain) in self.chains.iter().enumerate() {
            output.push_str(&format!(
                "{label}.chain[{index}] target=bb{} stop={} chain_truncated={}\n",
                chain.target,
                chain.stop,
                chain.stop == "node-limit"
            ));
            for node in &chain.nodes {
                output.push_str(&format!(
                    "  bb{} statements={} empty={} {}\n",
                    node.block,
                    node.statements,
                    node.statements == 0,
                    node.terminator
                ));
            }
        }
    }
}

fn chain(body: &Body, start: BlockId) -> Chain {
    let mut result = Chain {
        target: number(start),
        nodes: vec![],
        stop: "node-limit",
    };
    let mut id = start;
    for _ in 0..MAX_CHAIN_NODES {
        if result.nodes.iter().any(|node| node.block == number(id)) {
            result.stop = "cycle";
            break;
        }
        if number(id) as usize >= body.blocks.len() {
            result.stop = "missing-block";
            break;
        }
        let block = &body.blocks[id];
        result.nodes.push(Node {
            block: number(id),
            statements: block.stmts.len(),
            terminator: terminator(&block.terminator),
        });
        if !block.stmts.is_empty() {
            result.stop = "non-empty";
            break;
        }
        if let Terminator::Goto(next) = block.terminator {
            id = next;
        } else {
            result.stop = "terminal";
            break;
        }
    }
    result
}

pub(super) fn failure(
    function: FnId,
    block: BlockId,
    original: &EdgeWitness,
    final_body: &Body,
    original_edge: &Terminator,
    final_edge: &Terminator,
) -> String {
    let mut result = format!(
        "pass改变了实际源effect/行号/控制边\n[edge-witness-v1 fn={} block=bb{}]\n",
        function.into_raw().into_u32(),
        number(block)
    );
    original.render("captured", &mut result);
    EdgeWitness::capture(final_body, final_edge).render("final", &mut result);
    // 展示当前比较器如何解释旧边，不把这一结果伪称为原图事实。
    EdgeWitness::capture(final_body, original_edge).render("old-target-in-final", &mut result);
    result.push_str("[edge-witness-v1 total_truncated=false]\n");
    bound_error(result)
}

fn bound_error(mut value: String) -> String {
    if value.len() > MAX_ERROR_BYTES {
        let mut end = MAX_ERROR_BYTES - TRUNCATED.len();
        while !value.is_char_boundary(end) {
            end -= 1;
        }
        value.truncate(end);
        value.push_str(TRUNCATED);
    }
    value
}

#[cfg(test)]
#[path = "generator_product_edge_witness_tests.rs"]
mod tests;
