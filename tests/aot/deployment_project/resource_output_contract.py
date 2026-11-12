"""资源/输出边界真实平台测试计数合同：零匹配、忽略或错误测试集均拒绝。"""
def expected_tests(rid, section):
    if rid not in ('win-x64', 'osx-arm64') or section not in ('boundaries', 'transaction'):
        raise ValueError('未知资源输出测试平台/分组')
    if section == 'boundaries':
        prefix='resources::output::tests::'
        names=['same_ancestor_descendant_rejected_but_siblings_allowed',
               'all_selectors_preflight_before_any_expansion',
               'windows_component_case_is_conservative_not_string_prefix' if rid=='win-x64'
               else 'aliases_resolve_and_declared_symlink_is_not_followed']
    else:
        prefix='aot_layout::tests::resource_output_tests::'
        names=['overlapping_request_rejects_before_emit_and_preserves_dist',
               'project_output_root_with_sibling_assets_remains_usable']
        if rid!='win-x64':
            names+=['fake_sdk_never_started_in_isolated_process',
                    'trusted_output_alias_resolves_before_overlap_check']
    return {prefix+name for name in names}


def check_resource_output_tests(stdout, rid, section):
    expected=expected_tests(rid,section)
    actual=[line for line in stdout.splitlines() if line.startswith('test ') and not line.startswith('test result:')]
    if len(actual)!=len(expected) or set(actual)!={'test '+name+' ... ok' for name in expected}:
        raise ValueError('资源输出边界测试必须全部实际通过，拒绝零匹配/skip/替代测试')
    count=len(expected)
    if not any(line.startswith(f'test result: ok. {count} passed; 0 failed; 0 ignored;') for line in stdout.splitlines()):
        raise ValueError('资源输出边界测试计数不符')
    return count
