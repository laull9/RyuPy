; Assembly listing for method RyuPy.Generated.Module:m0_twice(System.Object):System.Object (FullOpts)
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
       sub      rsp, 48
       lea      rbp, [rsp+0x50]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      rbx, rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r15, rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     rbx, rbx
       je       G_M000_IG13
 
G_M000_IG03:
       mov      r14, qword ptr [rbx]
       mov      rdi, 0xD1FFAB1E
       cmp      r14, rdi
       jne      G_M000_IG09
 
G_M000_IG04:
       mov      r14, qword ptr [rbx+0x08]
       mov      rsi, r14
       lea      rbx, [rsi+r14]
       mov      rax, rsi
       xor      rax, rbx
       jl       SHORT G_M000_IG07
       lea      rax, [rbx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG05
       test     rax, rax
       jl       G_M000_IG17
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG16
       mov      r13, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      qword ptr [r13+0x08], rbx
 
G_M000_IG06:
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:
       lea      rdi, [rbp-0x40]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r13, gword ptr [rbp-0x40]
       mov      ebx, dword ptr [rbp-0x38]
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x50], xmm0
       lea      rdi, [rbp-0x50]
       mov      rsi, r14
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, r13
       mov      esi, ebx
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r13, rax
 
G_M000_IG08:
       jmp      SHORT G_M000_IG14
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       cmp      r14, rdi
       jne      SHORT G_M000_IG11
 
G_M000_IG10:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x28], xmm0
       vmovsd   qword ptr [rbp-0x30], xmm0
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x28]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x30]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r13, rax
       jmp      SHORT G_M000_IG14
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       cmp      r14, rdi
       jne      SHORT G_M000_IG13
 
G_M000_IG12:
       mov      rsi, rbx
       mov      rdi, rsi
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r13, rax
       jmp      SHORT G_M000_IG14
 
G_M000_IG13:
       mov      rdx, rbx
       mov      rsi, rbx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      r13, rax
 
G_M000_IG14:
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG15:
       add      rsp, 48
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG16:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG17:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 411

