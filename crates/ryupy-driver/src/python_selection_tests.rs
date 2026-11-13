//! 纯内存候选决策测试。合成3.9+候选绝不构成真实CPython运行支持的证据。
use super::*;

fn version(minor: u16, patch: u16) -> Version {
    Version {
        major: 3,
        minor,
        patch,
    }
}
fn target() -> Target {
    Target {
        rid: "osx-arm64".into(),
        architecture: "arm64".into(),
        platform_abi: "macos-14".into(),
    }
}
fn request() -> Request {
    Request {
        target: target(),
        requires_python: vec![],
        explicit: None,
        project_pin: None,
        lock: None,
        update_lock: false,
        allow_download: false,
        allow_experimental: false,
    }
}
fn candidate(id: &str, minor: u16, patch: u16) -> Candidate {
    Candidate {
        binding: Binding {
            distribution_id: id.into(),
            version: version(minor, patch),
            target: target(),
            soabi: format!("cpython-3{minor}-darwin"),
            shim_adapter: format!("test-only-cp3{minor}"),
            layout_id: "test-only-layout".into(),
            source_id: "test-only-source".into(),
            dependency_set_id: "test-only-dependencies".into(),
        },
        facts: Facts {
            cpython: true,
            stable_release: true,
            gil_enabled: true,
            debug_build: false,
            shared_library: true,
            headers: true,
            licenses: true,
            dependency_abi_matches: true,
            complete_base_distribution: true,
        },
        support: Support::Verified,
        origin: Origin::ManagedCache,
        interpreter_id: None,
    }
}
fn selected(request: &Request, candidates: &[Candidate]) -> String {
    resolve(request, candidates)
        .unwrap()
        .binding
        .distribution_id
}
fn rejected(candidate: Candidate, reason: Rejection) {
    let error = resolve(&request(), &[candidate]).unwrap_err();
    assert_eq!(error.code, FailureCode::NoCandidate);
    assert!(error.rejected[0].reasons.contains(&reason), "{error:?}");
}

#[test]
fn numeric_order_not_lexical() {
    assert_eq!(
        selected(
            &request(),
            &[candidate("nine", 9, 99), candidate("ten", 10, 1)]
        ),
        "ten"
    );
}
#[test]
fn highest_patch_in_same_origin() {
    assert_eq!(
        selected(
            &request(),
            &[candidate("old", 12, 9), candidate("new", 12, 13)]
        ),
        "new"
    );
}
#[test]
fn exact_choice_does_not_fallback() {
    let mut r = request();
    r.explicit = Some(Selector::Exact(version(11, 7)));
    assert_eq!(
        resolve(&r, &[candidate("other", 12, 13)]).unwrap_err().code,
        FailureCode::NoCandidate
    );
}
#[test]
fn minor_choice_selects_patch() {
    let mut r = request();
    r.explicit = Some(Selector::Minor {
        major: 3,
        minor: 11,
    });
    assert_eq!(
        selected(
            &r,
            &[candidate("eleven", 11, 7), candidate("twelve", 12, 13)]
        ),
        "eleven"
    );
}
#[test]
fn distribution_choice_beats_environment() {
    let mut env = candidate("env", 12, 13);
    env.origin = Origin::ProjectEnvironment;
    let mut r = request();
    r.explicit = Some(Selector::Distribution("cache".into()));
    assert_eq!(selected(&r, &[env, candidate("cache", 11, 7)]), "cache");
}
#[test]
fn interpreter_choice_uses_probe_identity() {
    let mut c = candidate("base", 11, 7);
    c.interpreter_id = Some("verified-executable".into());
    let mut r = request();
    r.explicit = Some(Selector::Interpreter("verified-executable".into()));
    assert_eq!(selected(&r, &[c]), "base");
}
#[test]
fn constraints_are_always_hard() {
    let mut r = request();
    r.explicit = Some(Selector::Exact(version(12, 13)));
    r.requires_python = vec![
        Constraint::AtLeast(version(9, 0)),
        Constraint::Less(version(12, 0)),
    ];
    let e = resolve(&r, &[candidate("twelve", 12, 13)]).unwrap_err();
    assert!(e.rejected[0].reasons.contains(&Rejection::RequiresPython));
}
#[test]
fn expanded_compatible_release_and_wildcard_exclusion() {
    let mut r = request();
    r.requires_python = vec![
        Constraint::AtLeast(version(9, 0)),
        Constraint::Less(version(10, 0)),
    ];
    assert_eq!(
        selected(&r, &[candidate("nine", 9, 9), candidate("ten", 10, 1)]),
        "nine"
    );
    r.requires_python = vec![Constraint::ExcludeMinor {
        major: 3,
        minor: 10,
    }];
    assert_eq!(
        selected(&r, &[candidate("nine", 9, 9), candidate("ten", 10, 1)]),
        "nine"
    );
}
#[test]
fn all_constraint_boundaries() {
    let v = version(11, 3);
    for constraint in [
        Constraint::Equal(v),
        Constraint::AtLeast(v),
        Constraint::AtMost(v),
    ] {
        assert!(constraint.accepts(v));
    }
    for constraint in [
        Constraint::NotEqual(v),
        Constraint::Less(v),
        Constraint::Greater(v),
    ] {
        assert!(!constraint.accepts(v));
    }
    assert!(Constraint::Greater(v).accepts(version(11, 4)));
    assert!(Constraint::Less(v).accepts(version(11, 2)));
}
#[test]
fn empty_constraint_intersection_fails() {
    let mut r = request();
    r.requires_python = vec![
        Constraint::Less(version(9, 0)),
        Constraint::AtLeast(version(9, 0)),
    ];
    assert!(resolve(&r, &[candidate("nine", 9, 0)]).is_err());
}
#[test]
fn valid_lock_never_drifts() {
    let old = candidate("old", 12, 13);
    let mut r = request();
    r.lock = Some(old.binding.clone());
    let result = resolve(&r, &[old, candidate("new", 13, 1)]).unwrap();
    assert_eq!(result.binding.distribution_id, "old");
    assert_eq!(result.reason, SelectionReason::Locked);
}
#[test]
fn unavailable_lock_requires_update() {
    let mut r = request();
    r.lock = Some(candidate("old", 12, 13).binding);
    assert_eq!(
        resolve(&r, &[candidate("new", 13, 1)]).unwrap_err().code,
        FailureCode::LockNeedsUpdate
    );
}
#[test]
fn explicit_lock_conflict_requires_update() {
    let old = candidate("old", 12, 13);
    let new = candidate("new", 13, 1);
    let mut r = request();
    r.lock = Some(old.binding.clone());
    r.explicit = Some(Selector::Exact(version(13, 1)));
    assert_eq!(
        resolve(&r, &[old.clone(), new.clone()]).unwrap_err().code,
        FailureCode::LockNeedsUpdate
    );
    r.update_lock = true;
    assert_eq!(selected(&r, &[old, new]), "new");
}
#[test]
fn changed_lock_abi_requires_update() {
    let mut c = candidate("same", 12, 13);
    let mut r = request();
    r.lock = Some(c.binding.clone());
    c.binding.soabi = "changed".into();
    assert_eq!(
        resolve(&r, &[c]).unwrap_err().code,
        FailureCode::LockNeedsUpdate
    );
}
#[test]
fn new_project_constraint_invalidates_lock() {
    let c = candidate("old", 12, 13);
    let mut r = request();
    r.lock = Some(c.binding.clone());
    r.requires_python.push(Constraint::AtLeast(version(13, 0)));
    assert_eq!(
        resolve(&r, &[c]).unwrap_err().code,
        FailureCode::LockNeedsUpdate
    );
}
#[test]
fn pin_limits_candidates() {
    let mut r = request();
    r.project_pin = Some(Selector::Minor {
        major: 3,
        minor: 10,
    });
    assert_eq!(
        selected(&r, &[candidate("ten", 10, 7), candidate("twelve", 12, 13)]),
        "ten"
    );
}
#[test]
fn explicit_choice_overrides_project_pin_not_constraints() {
    let mut r = request();
    r.project_pin = Some(Selector::Exact(version(10, 7)));
    r.explicit = Some(Selector::Exact(version(12, 13)));
    assert_eq!(selected(&r, &[candidate("twelve", 12, 13)]), "twelve");
}
#[test]
fn project_environment_precedes_newer_cache() {
    let mut env = candidate("env", 10, 7);
    env.origin = Origin::ProjectEnvironment;
    assert_eq!(
        selected(&request(), &[env, candidate("cache", 12, 13)]),
        "env"
    );
}
#[test]
fn cache_precedes_download_and_does_not_download() {
    let mut remote = candidate("remote", 13, 1);
    remote.origin = Origin::ManagedCatalog;
    let mut r = request();
    r.allow_download = true;
    let result = resolve(&r, &[candidate("cache", 12, 13), remote]).unwrap();
    assert_eq!(result.binding.distribution_id, "cache");
    assert!(!result.needs_download);
}
#[test]
fn downloads_are_opt_in_plans_only() {
    let mut remote = candidate("remote", 12, 13);
    remote.origin = Origin::ManagedCatalog;
    rejected(remote.clone(), Rejection::DownloadDisabled);
    let mut r = request();
    r.allow_download = true;
    let result = resolve(&r, &[remote]).unwrap();
    assert!(result.needs_download);
    assert_eq!(result.reason, SelectionReason::ManagedDownload);
}
#[test]
fn rejects_unsupported_implementations_and_releases() {
    let mut c = candidate("pypy", 12, 13);
    c.facts.cpython = false;
    rejected(c, Rejection::NotCPython);
    rejected(candidate("old", 8, 99), Rejection::VersionBelow39);
    let mut c = candidate("future-major", 9, 0);
    c.binding.version.major = 4;
    rejected(c, Rejection::VersionBelow39);
    let mut c = candidate("pre", 14, 0);
    c.facts.stable_release = false;
    rejected(c, Rejection::NotStable);
}
#[test]
fn rejects_target_and_cpu_mismatch() {
    let mut c = candidate("cpu", 12, 13);
    c.binding.target.architecture = "x64".into();
    rejected(c, Rejection::TargetMismatch);
    let mut c = candidate("rid", 12, 13);
    c.binding.target.rid = "linux-arm64".into();
    rejected(c, Rejection::TargetMismatch);
    let mut c = candidate("os", 12, 13);
    c.binding.target.platform_abi = "macos-15".into();
    rejected(c, Rejection::TargetMismatch);
}
#[test]
fn rejects_threading_and_debug_modes() {
    let mut c = candidate("ft", 13, 1);
    c.facts.gil_enabled = false;
    rejected(c, Rejection::FreeThreaded);
    let mut c = candidate("debug", 12, 13);
    c.facts.debug_build = true;
    rejected(c, Rejection::DebugBuild);
}
#[test]
fn requires_complete_redistributable_base() {
    let mut c = candidate("lib", 12, 13);
    c.facts.shared_library = false;
    rejected(c, Rejection::MissingSharedLibrary);
    let mut c = candidate("headers", 12, 13);
    c.facts.headers = false;
    rejected(c, Rejection::MissingHeaders);
    let mut c = candidate("license", 12, 13);
    c.facts.licenses = false;
    rejected(c, Rejection::MissingLicenses);
    let mut c = candidate("venv", 12, 13);
    c.facts.complete_base_distribution = false;
    rejected(c, Rejection::IncompleteDistribution);
    let mut c = candidate("abi", 12, 13);
    c.facts.dependency_abi_matches = false;
    rejected(c, Rejection::DependencyAbiMismatch);
}
#[test]
fn unknown_adapter_rejected_even_in_explicit_experiment() {
    let mut c = candidate("future", 99, 0);
    c.support = Support::Unsupported;
    let mut r = request();
    r.allow_experimental = true;
    r.explicit = Some(Selector::Exact(version(99, 0)));
    assert!(
        resolve(&r, &[c]).unwrap_err().rejected[0]
            .reasons
            .contains(&Rejection::UnsupportedAdapter)
    );
}
#[test]
fn experimental_requires_explicit_selection_and_preserves_status() {
    let mut c = candidate("untested-patch", 12, 99);
    c.support = Support::Implemented;
    rejected(c.clone(), Rejection::NotVerified);
    let mut r = request();
    r.allow_experimental = true;
    assert!(resolve(&r, &[c.clone()]).is_err());
    r.explicit = Some(Selector::Exact(version(12, 99)));
    assert_eq!(resolve(&r, &[c]).unwrap().support, Support::Implemented);
}
#[test]
fn missing_identity_rejected() {
    let mut c = candidate("source", 12, 13);
    c.binding.source_id.clear();
    rejected(c, Rejection::InvalidIdentity);
}
#[test]
fn empty_request_target_rejected() {
    let mut r = request();
    r.target.platform_abi.clear();
    assert_eq!(
        resolve(&r, &[]).unwrap_err().code,
        FailureCode::InvalidRequest
    );
}
#[test]
fn candidate_order_is_irrelevant() {
    let a = candidate("a", 10, 1);
    let b = candidate("b", 11, 1);
    let c = candidate("c", 8, 1);
    assert_eq!(
        resolve(&request(), &[a.clone(), b.clone(), c.clone()]),
        resolve(&request(), &[c, b, a])
    );
}
#[test]
fn exact_duplicate_candidates_are_deduplicated() {
    let c = candidate("one", 12, 13);
    assert_eq!(selected(&request(), &[c.clone(), c]), "one");
}
#[test]
fn conflicting_same_identity_is_error() {
    assert_eq!(
        resolve(
            &request(),
            &[candidate("same", 11, 1), candidate("same", 12, 13)]
        )
        .unwrap_err()
        .code,
        FailureCode::ConflictingCandidateIdentity
    );
}
#[test]
fn same_version_different_distribution_is_ambiguous() {
    assert_eq!(
        resolve(
            &request(),
            &[candidate("a", 12, 13), candidate("b", 12, 13)]
        )
        .unwrap_err()
        .code,
        FailureCode::AmbiguousDistribution
    );
}
#[test]
fn rejection_diagnostics_stable_and_complete() {
    let mut c = candidate("a", 8, 0);
    c.facts.headers = false;
    c.facts.shared_library = false;
    let e = resolve(&request(), &[candidate("z", 7, 0), c]).unwrap_err();
    assert_eq!(e.rejected[0].distribution_id, "a");
    assert_eq!(
        e.rejected[0].reasons,
        vec![
            Rejection::VersionBelow39,
            Rejection::MissingSharedLibrary,
            Rejection::MissingHeaders
        ]
    );
}
#[test]
fn invalid_explicit_candidate_does_not_fallback() {
    let mut c = candidate("selected", 12, 13);
    c.facts.shared_library = false;
    let mut r = request();
    r.explicit = Some(Selector::Distribution("selected".into()));
    assert_eq!(
        resolve(&r, &[c, candidate("other", 12, 13)])
            .unwrap_err()
            .code,
        FailureCode::NoCandidate
    );
}

#[test]
fn same_distribution_across_sources_is_normal() {
    let cache = candidate("one", 12, 13);
    let mut remote = cache.clone();
    remote.origin = Origin::ManagedCatalog;
    let result = resolve(&request(), &[remote, cache]).unwrap();
    assert_eq!(result.reason, SelectionReason::ManagedCache);
    assert!(!result.needs_download);
}
#[test]
fn interpreter_aliases_do_not_conflict() {
    let mut first = candidate("one", 12, 13);
    first.interpreter_id = Some("first".into());
    let mut second = first.clone();
    second.interpreter_id = Some("second".into());
    let mut r = request();
    r.explicit = Some(Selector::Interpreter("second".into()));
    assert_eq!(selected(&r, &[first, second]), "one");
}
#[test]
fn unrequested_explicit_origin_does_not_block_cache() {
    let mut explicit = candidate("explicit", 13, 1);
    explicit.origin = Origin::Explicit;
    assert_eq!(
        selected(&request(), &[explicit, candidate("cache", 12, 13)]),
        "cache"
    );
}
#[test]
fn dependency_change_invalidates_lock() {
    let mut c = candidate("same", 12, 13);
    let mut r = request();
    r.lock = Some(c.binding.clone());
    c.binding.dependency_set_id = "other-dependencies".into();
    assert_eq!(
        resolve(&r, &[c]).unwrap_err().code,
        FailureCode::LockNeedsUpdate
    );
}
#[test]
fn project_pin_must_be_a_version() {
    let mut r = request();
    r.project_pin = Some(Selector::Distribution("id".into()));
    assert_eq!(
        resolve(&r, &[]).unwrap_err().code,
        FailureCode::InvalidRequest
    );
}

#[test]
fn ambiguity_and_conflict_identify_candidates() {
    let error = resolve(
        &request(),
        &[candidate("b", 12, 13), candidate("a", 12, 13)],
    )
    .unwrap_err();
    assert_eq!(error.candidate_ids, vec!["a", "b"]);
    let error = resolve(
        &request(),
        &[candidate("same", 11, 1), candidate("same", 12, 13)],
    )
    .unwrap_err();
    assert_eq!(error.candidate_ids, vec!["same"]);
}

#[test]
fn multiple_identity_conflicts_have_deterministic_diagnostic() {
    let values = vec![
        candidate("z", 11, 1),
        candidate("z", 12, 1),
        candidate("a", 11, 1),
        candidate("a", 12, 1),
    ];
    let mut reversed = values.clone();
    reversed.reverse();
    assert_eq!(resolve(&request(), &values), resolve(&request(), &reversed));
}
