; Assembly listing for method RyuPy.Generated.Module:m0_choose_unknown(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 3 inlinees with PGO data; 6 single block inlinees; 14 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     rbx
       sub      rsp, 80
       lea      rbp, [rsp+0x70]
       xor      eax, eax
       mov      qword ptr [rbp-0x58], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x50], xmm8
       mov      qword ptr [rbp-0x40], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 29
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       jne      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      edi, 32
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rbx, gword ptr [rdi]
       jmp      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      edi, 30
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rbx, r15
 
G_M000_IG05:
       mov      edi, 33
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       test     rbx, rbx
       je       G_M000_IG26
 
G_M000_IG06:
       mov      r15, qword ptr [rbx]
       mov      rdi, 0xD1FFAB1E
       cmp      r15, rdi
       jne      G_M000_IG16
 
G_M000_IG07:
       mov      rsi, qword ptr [rbx+0x08]
       test     rdx, rdx
       je       G_M000_IG26
 
G_M000_IG08:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG14
 
G_M000_IG09:
       mov      r15, qword ptr [rdx+0x08]
       lea      rbx, [rsi+r15]
       mov      rax, rsi
       xor      rax, rbx
       mov      rcx, r15
       xor      rcx, rbx
       and      rax, rcx
       jl       SHORT G_M000_IG12
       lea      rax, [rbx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG10
       test     rax, rax
       jl       G_M000_IG30
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG29
       mov      r13, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      qword ptr [r13+0x08], rbx
 
G_M000_IG11:
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:
       lea      rdi, [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0x48]
       mov      ebx, dword ptr [rbp-0x40]
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x58], xmm0
       lea      rdi, [rbp-0x58]
       mov      rsi, r15
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r13
       mov      esi, ebx
       mov      rdx, gword ptr [rbp-0x58]
       mov      rcx, qword ptr [rbp-0x50]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r13, rax
 
G_M000_IG13:
       jmp      G_M000_IG27
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG26
 
G_M000_IG15:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x60], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x60]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      G_M000_IG27
 
G_M000_IG16:
       mov      rax, 0xD1FFAB1E
       cmp      r15, rax
       jne      SHORT G_M000_IG22
 
G_M000_IG17:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x28], xmm0
       test     rdx, rdx
       je       G_M000_IG26
 
G_M000_IG18:
       cmp      qword ptr [rdx], rax
       jne      SHORT G_M000_IG20
 
G_M000_IG19:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x28]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x30]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG27
 
G_M000_IG20:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG26
 
G_M000_IG21:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x68], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x68]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x28]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG27
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      r15, rdi
       jne      SHORT G_M000_IG26
 
G_M000_IG23:
       mov      rdi, rbx
       test     rdx, rdx
       je       SHORT G_M000_IG26
 
G_M000_IG24:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG26
 
G_M000_IG25:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r13, rax
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:
       mov      rsi, rbx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r13, rax
 
G_M000_IG27:
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG28:
       add      rsp, 80
       pop      rbx
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
 
; Total bytes of code 690
