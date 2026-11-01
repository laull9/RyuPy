; Assembly listing for method RyuPy.Generated.Module:m0_tuple_swap(System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 6 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x40]
       mov      r14, rdi
       mov      rbx, rsi
       mov      r15, rdx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r13, rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      r12, 0xD1FFAB1E
       mov      rdi, gword ptr [r12]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r14, rax
       mov      rax, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x38], rax
       cmp      rax, r14
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG04
 
G_M000_IG03:
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x40], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      r15, gword ptr [rbp-0x40]
       lea      rdi, bword ptr [r15+0x18]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       lea      rdi, bword ptr [rbx+0x08]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:Len(System.Object):long]
       mov      rdi, rax
       mov      esi, 2
       xor      edx, edx
       call     [RyuPy.Runtime.PyOps:UnpackCheck(long,long,bool)]
       mov      rsi, gword ptr [r12]
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      r15, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, r15
       mov      r15, rax
       mov      rax, qword ptr [rbp-0x38]
       add      rax, 1
       jo       G_M000_IG06
       cmp      rax, r14
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       mov      qword ptr [rbp-0x38], rax
       jne      G_M000_IG03
 
G_M000_IG04:
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r14, rax
       lea      rdi, bword ptr [r14+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       lea      rdi, bword ptr [r14+0x18]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rbx, rax
       lea      rdi, bword ptr [rbx+0x08]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, r13
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, rbx
 
G_M000_IG05:
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 451

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_read(System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 3 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 40
       lea      rbp, [rsp+0x50]
       mov      r14, rdi
       mov      rbx, rsi
       mov      r15, rdx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r13, rax
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      r12, rbx
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      qword ptr [rbp-0x40], rax
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r14, rax
       mov      rax, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x38], rax
       cmp      rax, r14
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       mov      gword ptr [rbp-0x48], r12
       je       G_M000_IG04
 
G_M000_IG03:
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       lea      rdi, bword ptr [r12+0x18]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x50], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r12
       call     CORINFO_HELP_ASSIGN_REF
       mov      r12, qword ptr [rbp-0x40]
       mov      rsi, gword ptr [r12]
       mov      rdi, gword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x48], rax
       mov      rdi, qword ptr [rbp-0x38]
       add      rdi, 1
       jo       SHORT G_M000_IG06
       cmp      rdi, r14
       setl     cl
       movzx    rcx, cl
       test     ecx, ecx
       mov      qword ptr [rbp-0x38], rdi
       jne      G_M000_IG03
 
G_M000_IG04:
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r13
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x48]
 
G_M000_IG05:
       add      rsp, 40
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 341

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_escape(System.Object,System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 3 single block inlinees; 0 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 40
       lea      rbp, [rsp+0x50]
       mov      r13, rdi
       mov      r15, rsi
       mov      r14, rdx
       mov      rbx, rcx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r12, rax
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rax, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x38], rax
       cmp      rax, r13
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG04
 
G_M000_IG03:
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x40], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x40]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x48], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x40]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, gword ptr [rbp-0x48]
       mov      rsi, rbx
       xor      edi, edi
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      rax, qword ptr [rbp-0x38]
       add      rax, 1
       jo       SHORT G_M000_IG06
       cmp      rax, r13
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       mov      qword ptr [rbp-0x38], rax
       jne      G_M000_IG03
 
G_M000_IG04:
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Unary(int,System.Object):System.Object]
       mov      rsi, rax
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rbx, rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, rbx
 
G_M000_IG05:
       add      rsp, 40
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG06:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 369

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_tuple_swap(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data

G_M000_IG01:
       push     rax
       mov      rax, rsi
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rax]
       cmp      edx, 1
       jbe      SHORT G_M000_IG04
       mov      rsi, gword ptr [rax+0x08]
       cmp      edx, 2
       jbe      SHORT G_M000_IG04
       mov      rdx, gword ptr [rax+0x10]
       call     [RyuPy.Generated.Module:m0_tuple_swap(System.Object,System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 47

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_tuple_read(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data

G_M000_IG01:
       push     rax
       mov      rax, rsi
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rax]
       cmp      edx, 1
       jbe      SHORT G_M000_IG04
       mov      rsi, gword ptr [rax+0x08]
       cmp      edx, 2
       jbe      SHORT G_M000_IG04
       mov      rdx, gword ptr [rax+0x10]
       call     [RyuPy.Generated.Module:m0_tuple_read(System.Object,System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 47

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_tuple_escape(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data

G_M000_IG01:
       push     rax
       mov      rcx, rsi
       mov      eax, edx
 
G_M000_IG02:
       test     eax, eax
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rcx]
       cmp      eax, 1
       jbe      SHORT G_M000_IG04
       mov      rsi, gword ptr [rcx+0x08]
       cmp      eax, 2
       jbe      SHORT G_M000_IG04
       mov      rdx, gword ptr [rcx+0x10]
       cmp      eax, 3
       jbe      SHORT G_M000_IG04
       mov      rcx, gword ptr [rcx+0x18]
       call     [RyuPy.Generated.Module:m0_tuple_escape(System.Object,System.Object,System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 58

