; Assembly listing for method RyuPy.Runtime.PyUnittestAssertions:Add(RyuPy.Runtime.PyClass,System.String,System.Func`3[System.Object,System.Object,bool],System.String) (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 1 inlinees with PGO data; 6 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       push     rax
       lea      rbp, [rsp+0x30]
       mov      rbx, rdi
       mov      r15, rsi
       mov      r14, rdx
       mov      r13, rcx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x08]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       lea      rdi, bword ptr [r12+0x10]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       mov      esi, 4
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r14, rax
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [r14+0x10], rdi
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [r14+0x18], rdi
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [r14+0x20], rdi
       mov      rdi, 0xD1FFAB1E
       mov      gword ptr [r14+0x28], rdi
       mov      rdi, 0xD1FFAB1E
       mov      esi, 4
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r13, rax
       lea      rdi, bword ptr [r13+0x28]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x30], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       mov      r12, gword ptr [rbp-0x30]
       mov      qword ptr [r12+0x18], rdi
       mov      rdi, r15
       mov      rsi, r14
       mov      rdx, r13
       mov      r8, r12
       mov      ecx, 3
       xor      r9, r9
       call     [RyuPy.Runtime.PyNativeFunctions:Create(System.String,System.String[],System.Object[],int,System.Func`2[System.Object[],System.Object],System.String[]):RyuPy.Runtime.PyFunction]
       mov      r14, rax
       cmp      byte  ptr [rbx], bl
       mov      rdi, rbx
       mov      rsi, r15
       call     [RyuPy.Runtime.PyEnum:GuardClassMutation(RyuPy.Runtime.PyClass,System.String)]
       mov      rdi, gword ptr [rbx+0x20]
       cmp      byte  ptr [rdi], dil
       mov      rsi, r15
       mov      rdx, r14
       mov      ecx, 1
       call     [System.Collections.Generic.Dictionary`2[System.__Canon,System.__Canon]:TryInsert(System.__Canon,System.__Canon,byte):bool:this]
       mov      rdi, rbx
 
G_M000_IG03:
       add      rsp, 8
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       tail.jmp [RyuPy.Runtime.PyClass:Touch():this]
 
; Total bytes of code 336

