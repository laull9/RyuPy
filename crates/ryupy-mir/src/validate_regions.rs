//! 异常区域的结构校验；与值类型及特殊调用来源校验分别维护。
/// 校验异常区域：区间连续、处理入口在区间外、嵌套关系正确。
///
/// codegen 直接按「块下标区间」换算 CIL 异常表的指令区间，所以区间必须严格连续；
/// 一旦嵌套顺序错了（例如内层区域越过了外层边界），生成的异常表会被 CLR 判为非法，
/// 而这种错误在 IL 里非常难查，必须在验证阶段拦下来。
pub(crate) fn validate_regions(func: &crate::mir::Function, errors: &mut Vec<String>) {
    let block_count = func.body.blocks.len();
    let mut spans: Vec<(usize, usize)> = Vec::new();
    for region in &func.body.regions {
        let start = region.try_start.into_raw().into_u32() as usize;
        let end = start + region.try_len;
        if end > block_count {
            errors.push(format!(
                "函数 `{}` 的异常区域越界：块区间 [{start}, {end}) 超出 {} 个块",
                func.name, block_count
            ));
            continue;
        }
        if region.try_len == 0 {
            errors.push(format!("函数 `{}` 的异常区域块数为 0", func.name));
        }
        let handler = region.handler.into_raw().into_u32() as usize;
        if (start..end).contains(&handler) {
            errors.push(format!(
                "函数 `{}` 的异常处理入口块 {:?} 落在受保护区间 [{start}, {end}) 内",
                func.name, region.handler
            ));
        }
        if !func.body.blocks.iter().any(|(id, _)| id == region.handler) {
            errors.push(format!(
                "函数 `{}` 的异常处理入口块 {:?} 不在 blocks arena 中",
                func.name, region.handler
            ));
        }
        spans.push((start, end));
    }
    // 区域之间必须嵌套或不相交。
    // 起始块相同是正常情况（try 体的第一条语句既是内层 catch 的起点，也是外层 finally 通道的起点），
    // 所以排序时把较长（外层）的区间排在前面。
    spans.sort_unstable_by(|a, b| a.0.cmp(&b.0).then(b.1.cmp(&a.1)));
    for i in 0..spans.len() {
        for j in (i + 1)..spans.len() {
            let (s1, e1) = spans[i];
            let (s2, e2) = spans[j];
            let nested = s2 >= s1 && e2 <= e1;
            let disjoint = e1 <= s2;
            if !nested && !disjoint {
                errors.push(format!(
                    "函数 `{}` 的异常区域 [{s1}, {e1}) 与 [{s2}, {e2}) 交叉，CIL 不允许这种嵌套",
                    func.name
                ));
            }
        }
    }
}
