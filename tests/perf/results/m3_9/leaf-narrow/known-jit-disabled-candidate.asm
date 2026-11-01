; Assembly listing for method RyuPy.Generated.Module:m0_repeat_index(System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 4 single block inlinees; 3 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x40]
       mov      rbx, rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r15, rax
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r14, gword ptr [rdi]
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rbx, rax
       jmp      G_M000_IG10
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      qword ptr [r12+0x08], r13
 
G_M000_IG04:
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyErrors:LoadName(System.Object,System.String):System.Object]
       mov      r14, rax
       mov      rax, 0xD1FFAB1E
       cmp      r14, gword ptr [rax]
       jne      G_M000_IG13
 
G_M000_IG05:
       test     r12, r12
       je       G_M000_IG13
 
G_M000_IG06:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r12], rax
       jne      G_M000_IG13
 
G_M000_IG07:
       mov      rax, qword ptr [r12+0x08]
       mov      rcx, rax
       add      rax, rcx
       mov      qword ptr [rbp-0x30], rax
       xor      rcx, rax
       jl       G_M000_IG13
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      r12, qword ptr [rbp-0x30]
       lea      rdi, [r12+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG08
       test     rdi, rdi
       jl       G_M000_IG19
       mov      rax, 0xD1FFAB1E
       mov      r14, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG18
       mov      r14, gword ptr [r14+8*rdi+0x10]
       jmp      SHORT G_M000_IG09
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      qword ptr [r14+0x08], r12
 
G_M000_IG09:
       add      r13, 1
       jo       G_M000_IG19
 
G_M000_IG10:
       cmp      r13, rbx
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG16
 
G_M000_IG11:
       lea      rdi, [r13+0x05]
       cmp      rdi, 261
       ja       G_M000_IG03
 
G_M000_IG12:
       test     rdi, rdi
       jl       G_M000_IG19
       mov      rax, 0xD1FFAB1E
       mov      r14, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG18
       mov      r12, gword ptr [r14+8*rdi+0x10]
       jmp      G_M000_IG04
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x40], rax
       mov      rcx, gword ptr [rax+0x20]
       test     rcx, rcx
       je       SHORT G_M000_IG14
       cmp      dword ptr [rcx+0x08], 1
       je       SHORT G_M000_IG15
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x40]
       lea      rdi, bword ptr [rdi+0x20]
       mov      gword ptr [rbp-0x38], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, gword ptr [rbp-0x38]
 
G_M000_IG15:
       mov      gword ptr [rbp-0x38], rcx
       mov      rdi, rcx
       mov      rdx, r12
       xor      esi, esi
       call     CORINFO_HELP_ARRADDR_ST
       mov      rdx, gword ptr [rbp-0x38]
       mov      rsi, r14
       mov      edi, 2
       call     [RyuPy.Runtime.PyPic:CallValueFast(int,System.Object,System.Object[]):System.Object]
       mov      r14, rax
       jmp      G_M000_IG09
 
G_M000_IG16:
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r14
 
G_M000_IG17:
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG18:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG19:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 611

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_constant(System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 3 single block inlinees; 2 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 40
       lea      rbp, [rsp+0x50]
       mov      rbx, rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r15, rax
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r14, gword ptr [rdi]
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rbx, rax
       jmp      G_M000_IG08
 
G_M000_IG03:
       test     r12, r12
       je       G_M000_IG10
 
G_M000_IG04:
       mov      rax, 0xD1FFAB1E
       mov      qword ptr [rbp-0x38], rax
       cmp      qword ptr [r12], rax
       jne      G_M000_IG10
 
G_M000_IG05:
       mov      rcx, qword ptr [r12+0x08]
       mov      rdx, rcx
       add      rcx, rdx
       mov      qword ptr [rbp-0x30], rcx
       xor      rdx, rcx
       jl       G_M000_IG10
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      r12, qword ptr [rbp-0x30]
       lea      rdi, [r12+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG06
       test     rdi, rdi
       jl       G_M000_IG16
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG15
       mov      r14, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rdi, qword ptr [rbp-0x38]
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      qword ptr [r14+0x08], r12
 
G_M000_IG07:
       add      r13, 1
       jo       G_M000_IG16
 
G_M000_IG08:
       cmp      r13, rbx
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG13
 
G_M000_IG09:
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyErrors:LoadName(System.Object,System.String):System.Object]
       mov      r14, rax
       mov      rax, 0xD1FFAB1E
       mov      r12, gword ptr [rax]
       mov      rax, 0xD1FFAB1E
       cmp      r14, gword ptr [rax]
       je       G_M000_IG03
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x48], rax
       mov      rcx, gword ptr [rax+0x28]
       test     rcx, rcx
       je       SHORT G_M000_IG11
       cmp      dword ptr [rcx+0x08], 1
       je       SHORT G_M000_IG12
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x48]
       lea      rdi, bword ptr [rdi+0x28]
       mov      gword ptr [rbp-0x40], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, gword ptr [rbp-0x40]
 
G_M000_IG12:
       mov      gword ptr [rbp-0x40], rcx
       mov      rdi, rcx
       mov      rdx, r12
       xor      esi, esi
       call     CORINFO_HELP_ARRADDR_ST
       mov      rdx, gword ptr [rbp-0x40]
       mov      rsi, r14
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:CallValueFast(int,System.Object,System.Object[]):System.Object]
       mov      r14, rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r14
 
G_M000_IG14:
       add      rsp, 40
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG15:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG16:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 537

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_repeat_index(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data

G_M000_IG01:
       push     rax
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rsi]
       call     [RyuPy.Generated.Module:m0_repeat_index(System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 26

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_repeat_constant(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data

G_M000_IG01:
       push     rax
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rsi]
       call     [RyuPy.Generated.Module:m0_repeat_constant(System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 26

