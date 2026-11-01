; Assembly listing for method RyuPy.Generated.Module:m0_repeat(System.Object,System.Object):System.Object (FullOpts)
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
       mov      r15, rdi
       mov      rbx, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       jmp      G_M000_IG08
 
G_M000_IG03:
       test     rbx, rbx
       je       G_M000_IG10
 
G_M000_IG04:
       mov      rax, 0xD1FFAB1E
       mov      qword ptr [rbp-0x38], rax
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG10
 
G_M000_IG05:
       mov      rcx, qword ptr [rbx+0x08]
       mov      rdx, rcx
       add      rcx, rdx
       mov      qword ptr [rbp-0x30], rcx
       xor      rdx, rcx
       jl       G_M000_IG10
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      r13, qword ptr [rbp-0x30]
       lea      rdi, [r13+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG06
       test     rdi, rdi
       jl       G_M000_IG16
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG15
       mov      r13, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rdi, qword ptr [rbp-0x38]
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r13
       mov      r13, rax
 
G_M000_IG07:
       add      r12, 1
       jo       G_M000_IG16
 
G_M000_IG08:
       cmp      r12, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG13
 
G_M000_IG09:
       mov      edi, 13
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyErrors:LoadName(System.Object,System.String):System.Object]
       mov      r13, rax
       mov      rax, 0xD1FFAB1E
       cmp      r13, gword ptr [rax]
       je       G_M000_IG03
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x48], rax
       mov      rcx, gword ptr [rax+0x18]
       test     rcx, rcx
       je       SHORT G_M000_IG11
       cmp      dword ptr [rcx+0x08], 1
       je       SHORT G_M000_IG12
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x48]
       lea      rdi, bword ptr [rdi+0x18]
       mov      gword ptr [rbp-0x40], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, gword ptr [rbp-0x40]
 
G_M000_IG12:
       mov      gword ptr [rbp-0x40], rcx
       mov      rdi, rcx
       mov      rdx, rbx
       xor      esi, esi
       call     CORINFO_HELP_ARRADDR_ST
       mov      rdx, gword ptr [rbp-0x40]
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:CallValueFast(int,System.Object,System.Object[]):System.Object]
       mov      r13, rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
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
 
; Total bytes of code 524

