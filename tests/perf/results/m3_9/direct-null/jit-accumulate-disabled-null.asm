; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 6 single block inlinees; 4 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 56
       lea      rbp, [rsp+0x60]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x50], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      r15, rdi
       mov      rbx, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x50]
       mov      rsi, r12
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x40]
       mov      rsi, gword ptr [rbp-0x50]
       mov      rdx, gword ptr [rbp-0x48]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      r12, rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       xor      eax, eax
       test     r15, r15
       je       G_M000_IG11
 
G_M000_IG03:
       mov      rdi, qword ptr [r15]
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jne      SHORT G_M000_IG10
 
G_M000_IG04:
       mov      edx, 1
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x30], rax
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:
       mov      eax, 1
       mov      rdi, qword ptr [r15+0x08]
       mov      rcx, rdi
       mov      rdx, rcx
       mov      ecx, eax
 
G_M000_IG06:
       add      r13, 1
       jo       G_M000_IG30
       mov      qword ptr [rbp-0x30], rdx
       mov      edx, ecx
 
G_M000_IG07:
       cmp      r13, r12
       setl     cl
       movzx    rcx, cl
       test     ecx, ecx
       je       G_M000_IG24
 
G_M000_IG08:
       lea      rdi, [r13+0x05]
       cmp      rdi, 261
       jbe      SHORT G_M000_IG12
 
G_M000_IG09:
       mov      dword ptr [rbp-0x34], edx
       mov      rcx, 0xD1FFAB1E
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      qword ptr [rcx+0x08], r13
       mov      gword ptr [rbp-0x60], rcx
       jmp      SHORT G_M000_IG13
 
G_M000_IG10:
       xor      edx, edx
       mov      qword ptr [rbp-0x30], rax
       jmp      SHORT G_M000_IG07
 
G_M000_IG11:
       xor      edx, edx
       mov      dword ptr [rbp-0x34], edx
       mov      qword ptr [rbp-0x30], rax
       mov      edx, dword ptr [rbp-0x34]
       jmp      SHORT G_M000_IG07
 
G_M000_IG12:
       test     rdi, rdi
       jl       G_M000_IG30
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG29
       mov      rcx, gword ptr [rcx+8*rdi+0x10]
       mov      dword ptr [rbp-0x34], edx
       mov      gword ptr [rbp-0x60], rcx
 
G_M000_IG13:
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x58], rax
       mov      ecx, dword ptr [rbp-0x34]
       test     ecx, ecx
       je       SHORT G_M000_IG17
 
G_M000_IG14:
       test     rax, rax
       je       SHORT G_M000_IG17
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG17
 
G_M000_IG16:
       mov      rdi, qword ptr [rax+0x08]
       mov      rdx, qword ptr [rbp-0x30]
       lea      rsi, [rdx+rdi]
       mov      r8, rdx
       xor      r8, rsi
       xor      rdi, rsi
       and      rdi, r8
       mov      qword ptr [rbp-0x30], rdx
       mov      rax, gword ptr [rbp-0x58]
       jl       SHORT G_M000_IG17
       mov      rdx, rsi
       xor      r15, r15
       jmp      G_M000_IG06
 
G_M000_IG17:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       je       G_M000_IG21
       mov      rdx, qword ptr [rbp-0x30]
       lea      rdi, [rdx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG18
       test     rdi, rdi
       jl       G_M000_IG30
       mov      rsi, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG29
       mov      r15, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rax, qword ptr [rbp-0x30]
       mov      qword ptr [r15+0x08], rax
 
G_M000_IG19:
       mov      rdx, gword ptr [rbp-0x58]
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
       test     r15, r15
       jne      SHORT G_M000_IG22
 
G_M000_IG20:
       xor      edi, edi
       mov      ecx, edi
       mov      rdx, qword ptr [rbp-0x30]
       jmp      G_M000_IG06
 
G_M000_IG21:
       jmp      SHORT G_M000_IG19
 
G_M000_IG22:
       mov      rdi, qword ptr [r15]
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       je       G_M000_IG05
 
G_M000_IG23:
       xor      eax, eax
       xor      edi, edi
       mov      ecx, edi
       mov      rdx, qword ptr [rbp-0x30]
       jmp      G_M000_IG06
 
G_M000_IG24:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     edx, edi
       je       SHORT G_M000_IG27
 
G_M000_IG25:
       mov      rax, qword ptr [rbp-0x30]
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG26
       test     rdi, rdi
       jl       SHORT G_M000_IG30
       mov      rsi, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      SHORT G_M000_IG29
       mov      r15, gword ptr [rcx+8*rsi+0x10]
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rbx, qword ptr [rbp-0x30]
       mov      qword ptr [r15+0x08], rbx
 
G_M000_IG27:
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r15
 
G_M000_IG28:
       add      rsp, 56
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG29:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG30:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 865

