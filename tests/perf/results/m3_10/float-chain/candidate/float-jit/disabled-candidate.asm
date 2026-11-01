; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mul_add(System.Object,System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 6 inlinees with PGO data; 12 single block inlinees; 28 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 248
       lea      rbp, [rsp+0x120]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqa  xmmword ptr [rbp-0x70], xmm8
       mov      r14, rdi
       mov      r15, rsi
       mov      rbx, rdx
       mov      r13, rcx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r12, rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r14, rax
       mov      rax, qword ptr [rbp-0x30]
       jmp      SHORT G_M000_IG10
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG12
 
G_M000_IG04:
       test     rbx, rbx
       je       G_M000_IG13
 
G_M000_IG05:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG13
 
G_M000_IG06:
       test     r13, r13
       je       SHORT G_M000_IG13
 
G_M000_IG07:
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG13
 
G_M000_IG08:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
 
G_M000_IG09:
       mov      r13, rax
       mov      rax, qword ptr [rbp-0x38]
       add      rax, 1
       jo       G_M000_IG64
 
G_M000_IG10:
       mov      qword ptr [rbp-0x38], rax
       cmp      rax, r14
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG61
 
G_M000_IG11:
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r15, r15
       jne      G_M000_IG03
 
G_M000_IG12:
       test     r15, r15
       je       G_M000_IG37
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG31
 
G_M000_IG14:
       mov      rsi, qword ptr [r15+0x08]
       test     rbx, rbx
       je       G_M000_IG37
 
G_M000_IG15:
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG29
 
G_M000_IG16:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x60], rax
       lea      rcx, [rbp-0x80]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rcx], r8
       mov      rcx, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x78], rcx
       mov      r8, rsi
       sar      r8, 63
       and      r8, rax
       sub      rdx, r8
       mov      r8, rax
       sar      r8, 63
       and      r8, rsi
       sub      rdx, r8
       mov      r8, rcx
       sar      r8, 63
       cmp      rdx, r8
       jne      SHORT G_M000_IG19
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG17
       test     rax, rax
       jl       G_M000_IG64
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG63
       mov      rsi, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG18
 
G_M000_IG17:
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x78]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG18:
       jmp      G_M000_IG22
 
G_M000_IG19:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x110], rax
       mov      ecx, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0x104], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       lea      rdi, [rbp-0x90]
       mov      rsi, qword ptr [rbp-0x60]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x90]
       mov      r9d, dword ptr [rbp-0x88]
       mov      rcx, gword ptr [rbp-0x110]
       test     rcx, rcx
       jne      SHORT G_M000_IG24
 
G_M000_IG20:
       test     rax, rax
       jne      SHORT G_M000_IG23
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xA0], xmm0
       movsxd   rsi, dword ptr [rbp-0x104]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xA0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xA0]
       mov      esi, dword ptr [rbp-0x98]
 
G_M000_IG21:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG22:
       jmp      G_M000_IG38
 
G_M000_IG23:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG25
 
G_M000_IG24:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG25:
       test     rax, rax
       je       SHORT G_M000_IG28
 
G_M000_IG26:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG27:
       mov      edx, dword ptr [rbp-0x104]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG21
 
G_M000_IG28:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG27
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG37
 
G_M000_IG30:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xA8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG38
 
G_M000_IG31:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG37
 
G_M000_IG32:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG37
 
G_M000_IG33:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG35
 
G_M000_IG34:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG38
 
G_M000_IG35:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG37
 
G_M000_IG36:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xB0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG38
 
G_M000_IG37:
       mov      rdx, rbx
       mov      rsi, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG38:
       test     rsi, rsi
       je       G_M000_IG59
 
G_M000_IG39:
       mov      rax, qword ptr [rsi]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG49
 
G_M000_IG40:
       mov      rax, qword ptr [rsi+0x08]
       test     r13, r13
       je       G_M000_IG59
 
G_M000_IG41:
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG47
 
G_M000_IG42:
       mov      r13, qword ptr [r13+0x08]
       lea      rcx, [rax+r13]
       mov      qword ptr [rbp-0xD0], rcx
       mov      rdx, rax
       xor      rdx, rcx
       mov      rsi, r13
       xor      rsi, rcx
       and      rdx, rsi
       jl       SHORT G_M000_IG45
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG43
       test     rax, rax
       jl       G_M000_IG64
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG63
       mov      rax, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG44
 
G_M000_IG43:
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0xD0]
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG44:
       jmp      SHORT G_M000_IG46
 
G_M000_IG45:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xE0]
       mov      gword ptr [rbp-0x118], rax
       mov      ecx, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0x108], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xF0], xmm0
       lea      rdi, [rbp-0xF0]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x118]
       mov      esi, dword ptr [rbp-0x108]
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rcx, qword ptr [rbp-0xE8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG46:
       jmp      G_M000_IG60
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG59
 
G_M000_IG48:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xF8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xF8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG60
 
G_M000_IG49:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG55
 
G_M000_IG50:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       test     r13, r13
       je       G_M000_IG59
 
G_M000_IG51:
       cmp      qword ptr [r13], rcx
       jne      SHORT G_M000_IG53
 
G_M000_IG52:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG60
 
G_M000_IG53:
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG59
 
G_M000_IG54:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x100]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG60
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG59
 
G_M000_IG56:
       mov      rdi, rsi
       test     r13, r13
       je       SHORT G_M000_IG59
 
G_M000_IG57:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r13], rax
       jne      SHORT G_M000_IG59
 
G_M000_IG58:
       mov      rsi, r13
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG60
 
G_M000_IG59:
       mov      rdx, r13
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG60:
       jmp      G_M000_IG09
 
G_M000_IG61:
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r12
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG62:
       add      rsp, 248
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG63:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG64:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1567

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_norm(System.Object,System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 9 inlinees with PGO data; 25 single block inlinees; 70 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 536
       lea      rbp, [rsp+0x240]
       xor      eax, eax
       mov      qword ptr [rbp-0x1D8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x1D0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x190], zmm8
       vmovdqu32 zmmword ptr [rbp-0x150], zmm8
       vmovdqu32 zmmword ptr [rbp-0x110], zmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xA0], zmm8
       mov      qword ptr [rbp-0x60], rax
       mov      r13, rdi
       mov      rbx, rsi
       mov      r15, rdx
       mov      r14, rcx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r12, rax
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x208], rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rax, qword ptr [rbp-0x30]
       jmp      G_M000_IG10
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG12
 
G_M000_IG04:
       test     r15, r15
       je       G_M000_IG13
 
G_M000_IG05:
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG13
 
G_M000_IG06:
       test     r14, r14
       je       G_M000_IG13
 
G_M000_IG07:
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG13
 
G_M000_IG08:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   xmm2, qword ptr [r14+0x08]
       vmulsd   xmm0, xmm0, xmm0
       vmulsd   xmm1, xmm1, xmm1
       vaddsd   xmm0, xmm0, xmm1
       vmulsd   xmm1, xmm2, xmm2
       vaddsd   xmm0, xmm0, xmm1
       vmovsd   qword ptr [rbp-0x40], xmm0
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
 
G_M000_IG09:
       mov      gword ptr [rbp-0x208], rax
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x38]
       add      rax, 1
       jo       G_M000_IG108
       mov      rcx, rbx
       mov      rbx, r15
       mov      r15, r14
       mov      r14, rcx
 
G_M000_IG10:
       mov      qword ptr [rbp-0x38], rax
       cmp      rax, r13
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG105
 
G_M000_IG11:
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     rbx, rbx
       jne      G_M000_IG03
 
G_M000_IG12:
       test     rbx, rbx
       je       G_M000_IG29
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG27
 
G_M000_IG14:
       mov      rax, qword ptr [rbx+0x08]
       mov      rsi, rax
       mov      qword ptr [rbp-0x58], rax
       lea      rcx, [rbp-0x78]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rcx], r8
       mov      rcx, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x70], rcx
       mov      r8, rsi
       sar      r8, 63
       and      r8, rax
       sub      rdx, r8
       sub      rdx, r8
       mov      r8, rcx
       sar      r8, 63
       cmp      rdx, r8
       jne      SHORT G_M000_IG17
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG15
       test     rax, rax
       jl       G_M000_IG108
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG107
       mov      rdx, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG16
 
G_M000_IG15:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x70]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG16:
       mov      rax, rdx
       jmp      G_M000_IG20
 
G_M000_IG17:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x68], xmm0
       lea      rdi, [rbp-0x68]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x68]
       mov      gword ptr [rbp-0x220], rax
       mov      ecx, dword ptr [rbp-0x60]
       mov      dword ptr [rbp-0x1EC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       lea      rdi, [rbp-0x88]
       mov      rsi, qword ptr [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      r9d, dword ptr [rbp-0x80]
       mov      rcx, gword ptr [rbp-0x220]
       test     rcx, rcx
       jne      SHORT G_M000_IG22
 
G_M000_IG18:
       test     rax, rax
       jne      SHORT G_M000_IG21
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x98], xmm0
       movsxd   rsi, dword ptr [rbp-0x1EC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x98]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x98]
       mov      esi, dword ptr [rbp-0x90]
 
G_M000_IG19:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG20:
       jmp      G_M000_IG30
 
G_M000_IG21:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG23
 
G_M000_IG22:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG23:
       test     rax, rax
       je       SHORT G_M000_IG26
 
G_M000_IG24:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG25:
       mov      edx, dword ptr [rbp-0x1EC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG19
 
G_M000_IG26:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG25
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG29
 
G_M000_IG28:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       vmovsd   qword ptr [rbp-0x50], xmm0
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG30
 
G_M000_IG29:
       mov      rdx, rbx
       mov      rsi, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      gword ptr [rbp-0x210], rax
       mov      rax, gword ptr [rbp-0x210]
 
G_M000_IG30:
       test     r15, r15
       jne      SHORT G_M000_IG33
 
G_M000_IG31:
       mov      gword ptr [rbp-0x210], rax
 
G_M000_IG32:
       mov      rdx, r15
       mov      rsi, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
       jmp      G_M000_IG49
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG47
 
G_M000_IG34:
       mov      rcx, qword ptr [r15+0x08]
       mov      rsi, rcx
       mov      qword ptr [rbp-0xB0], rcx
       lea      r8, [rbp-0xD0]
       mov      rdx, rsi
       mulx     rdx, r9, rcx
       mov      qword ptr [r8], r9
       mov      r8, qword ptr [rbp-0xD0]
       mov      qword ptr [rbp-0xC8], r8
       mov      r9, rsi
       sar      r9, 63
       and      r9, rcx
       sub      rdx, r9
       sub      rdx, r9
       mov      r9, r8
       sar      r9, 63
       cmp      rdx, r9
       jne      SHORT G_M000_IG37
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG35
       test     rcx, rcx
       jl       G_M000_IG108
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG107
       mov      rdx, gword ptr [rcx+8*rdi+0x10]
       mov      gword ptr [rbp-0x210], rax
       jmp      SHORT G_M000_IG36
 
G_M000_IG35:
       mov      gword ptr [rbp-0x210], rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0xC8]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG36:
       jmp      G_M000_IG40
 
G_M000_IG37:
       mov      gword ptr [rbp-0x210], rax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xC0], xmm0
       lea      rdi, [rbp-0xC0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC0]
       mov      gword ptr [rbp-0x228], rax
       mov      ecx, dword ptr [rbp-0xB8]
       mov      dword ptr [rbp-0x1F0], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, qword ptr [rbp-0xB0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xE0]
       mov      r9d, dword ptr [rbp-0xD8]
       mov      rcx, gword ptr [rbp-0x228]
       test     rcx, rcx
       jne      SHORT G_M000_IG42
 
G_M000_IG38:
       test     rax, rax
       jne      SHORT G_M000_IG41
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xF0], xmm0
       movsxd   rsi, dword ptr [rbp-0x1F0]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xF0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xF0]
       mov      esi, dword ptr [rbp-0xE8]
 
G_M000_IG39:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG40:
       jmp      G_M000_IG49
 
G_M000_IG41:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG43
 
G_M000_IG42:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG43:
       test     rax, rax
       je       SHORT G_M000_IG46
 
G_M000_IG44:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG45:
       mov      edx, dword ptr [rbp-0x1F0]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG39
 
G_M000_IG46:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG45
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       mov      gword ptr [rbp-0x210], rax
       jne      G_M000_IG32
 
G_M000_IG48:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       vmovsd   qword ptr [rbp-0xA8], xmm0
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
 
G_M000_IG49:
       mov      rax, gword ptr [rbp-0x210]
       test     rax, rax
       je       G_M000_IG70
 
G_M000_IG50:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG60
 
G_M000_IG51:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG70
 
G_M000_IG52:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG58
 
G_M000_IG53:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x110], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x118], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG56
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG54
       test     rax, rax
       jl       G_M000_IG108
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rcx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG107
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       jmp      SHORT G_M000_IG55
 
G_M000_IG54:
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x118]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG55:
       jmp      G_M000_IG57
 
G_M000_IG56:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x230], rax
       mov      ecx, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x1F4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x138], xmm0
       lea      rdi, [rbp-0x138]
       mov      rsi, qword ptr [rbp-0x110]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x230]
       mov      esi, dword ptr [rbp-0x1F4]
       mov      rdx, gword ptr [rbp-0x138]
       mov      rcx, qword ptr [rbp-0x130]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG57:
       jmp      G_M000_IG71
 
G_M000_IG58:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG70
 
G_M000_IG59:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x140], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x140]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG71
 
G_M000_IG60:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG66
 
G_M000_IG61:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm0
       test     rdx, rdx
       je       G_M000_IG70
 
G_M000_IG62:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG64
 
G_M000_IG63:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xF8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG71
 
G_M000_IG64:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG70
 
G_M000_IG65:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x148], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x148]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xF8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG71
 
G_M000_IG66:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG70
 
G_M000_IG67:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG70
 
G_M000_IG68:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG70
 
G_M000_IG69:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG71
 
G_M000_IG70:
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      gword ptr [rbp-0x218], rax
       mov      rax, gword ptr [rbp-0x218]
 
G_M000_IG71:
       test     r14, r14
       jne      SHORT G_M000_IG74
 
G_M000_IG72:
       mov      gword ptr [rbp-0x218], rax
 
G_M000_IG73:
       mov      rdx, r14
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
       jmp      G_M000_IG82
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG80
 
G_M000_IG75:
       mov      rcx, qword ptr [r14+0x08]
       mov      rsi, rcx
       mov      qword ptr [rbp-0x160], rcx
       lea      r8, [rbp-0x180]
       mov      rdx, rsi
       mulx     rdx, r9, rcx
       mov      qword ptr [r8], r9
       mov      r8, qword ptr [rbp-0x180]
       mov      qword ptr [rbp-0x178], r8
       mov      r9, rsi
       sar      r9, 63
       and      r9, rcx
       sub      rdx, r9
       sub      rdx, r9
       mov      r9, r8
       sar      r9, 63
       cmp      rdx, r9
       jne      SHORT G_M000_IG78
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG76
       test     rcx, rcx
       jl       G_M000_IG108
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG107
       mov      rdx, gword ptr [rcx+8*rdi+0x10]
       mov      gword ptr [rbp-0x218], rax
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:
       mov      gword ptr [rbp-0x218], rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x178]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG77:
       jmp      G_M000_IG79
 
G_M000_IG78:
       mov      gword ptr [rbp-0x218], rax
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x170], xmm0
       lea      rdi, [rbp-0x170]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x170]
       mov      gword ptr [rbp-0x238], rax
       mov      ecx, dword ptr [rbp-0x168]
       mov      dword ptr [rbp-0x1F8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x190], xmm0
       lea      rdi, [rbp-0x190]
       mov      rsi, qword ptr [rbp-0x160]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x238]
       mov      esi, dword ptr [rbp-0x1F8]
       mov      rdx, gword ptr [rbp-0x190]
       mov      rcx, qword ptr [rbp-0x188]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG79:
       jmp      SHORT G_M000_IG82
 
G_M000_IG80:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [r14], rsi
       mov      gword ptr [rbp-0x218], rax
       jne      G_M000_IG73
 
G_M000_IG81:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm0
       vmovsd   qword ptr [rbp-0x158], xmm0
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x150]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x158]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
 
G_M000_IG82:
       mov      rax, gword ptr [rbp-0x218]
       test     rax, rax
       je       G_M000_IG103
 
G_M000_IG83:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG93
 
G_M000_IG84:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG103
 
G_M000_IG85:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG91
 
G_M000_IG86:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x1B0], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x1B8], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG89
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG87
       test     rax, rax
       jl       G_M000_IG108
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rcx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG107
       mov      rax, gword ptr [rcx+8*rdi+0x10]
       jmp      SHORT G_M000_IG88
 
G_M000_IG87:
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x1B8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG88:
       jmp      G_M000_IG90
 
G_M000_IG89:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1C8], xmm0
       lea      rdi, [rbp-0x1C8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x1C8]
       mov      gword ptr [rbp-0x240], rax
       mov      ecx, dword ptr [rbp-0x1C0]
       mov      dword ptr [rbp-0x1FC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x1D8], xmm0
       lea      rdi, [rbp-0x1D8]
       mov      rsi, qword ptr [rbp-0x1B0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x240]
       mov      esi, dword ptr [rbp-0x1FC]
       mov      rdx, gword ptr [rbp-0x1D8]
       mov      rcx, qword ptr [rbp-0x1D0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG90:
       jmp      G_M000_IG104
 
G_M000_IG91:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG103
 
G_M000_IG92:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1A8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x1E0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1E0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1A8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG104
 
G_M000_IG93:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG99
 
G_M000_IG94:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x198], xmm0
       test     rdx, rdx
       je       G_M000_IG103
 
G_M000_IG95:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG97
 
G_M000_IG96:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1A0], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x198]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG104
 
G_M000_IG97:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG103
 
G_M000_IG98:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x1E8], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1E8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG104
 
G_M000_IG99:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG103
 
G_M000_IG100:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG103
 
G_M000_IG101:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG103
 
G_M000_IG102:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG104
 
G_M000_IG103:
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG104:
       jmp      G_M000_IG09
 
G_M000_IG105:
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r12
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x208]
 
G_M000_IG106:
       add      rsp, 536
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG107:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG108:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3119

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_mixed(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 9 inlinees with PGO data; 19 single block inlinees; 43 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 264
       lea      rbp, [rsp+0x130]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqa  xmmword ptr [rbp-0x70], xmm8
       mov      r15, rdi
       mov      rbx, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       jmp      G_M000_IG18
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      gword ptr [rbp-0x118], rax
 
G_M000_IG04:
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      rax, gword ptr [rbp-0x118]
       test     rax, rax
       je       SHORT G_M000_IG09
 
G_M000_IG05:
       mov      rdi, qword ptr [rax]
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jne      SHORT G_M000_IG09
 
G_M000_IG06:
       mov      rsi, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x38], rsi
       test     rdx, rdx
       je       SHORT G_M000_IG09
 
G_M000_IG07:
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG09
 
G_M000_IG08:
       mov      r8, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x40], r8
       test     r8, r8
       jle      SHORT G_M000_IG09
       test     rsi, rsi
       jl       SHORT G_M000_IG09
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x38]
       cqo      
       idiv     rdx:rax, qword ptr [rbp-0x40]
       mov      qword ptr [rsi+0x08], rdx
       jmp      SHORT G_M000_IG10
 
G_M000_IG09:
       mov      rsi, rax
       mov      edi, 6
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG10:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x110], rax
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      rax, gword ptr [rbp-0x110]
       test     rax, rax
       je       G_M000_IG21
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG21
 
G_M000_IG12:
       test     rdx, rdx
       je       G_M000_IG22
 
G_M000_IG13:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG22
 
G_M000_IG14:
       test     r13, r13
       je       G_M000_IG22
 
G_M000_IG15:
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG22
 
G_M000_IG16:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmovsd   qword ptr [rax+0x08], xmm0
 
G_M000_IG17:
       mov      r13, rax
       add      r12, 1
       jo       G_M000_IG73
 
G_M000_IG18:
       cmp      r12, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG70
 
G_M000_IG19:
       lea      rdi, [r12+0x05]
       cmp      rdi, 261
       ja       G_M000_IG03
 
G_M000_IG20:
       test     rdi, rdi
       jl       G_M000_IG73
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG72
       mov      rax, gword ptr [rax+8*rdi+0x10]
       mov      gword ptr [rbp-0x118], rax
       jmp      G_M000_IG04
 
G_M000_IG21:
       test     rax, rax
       je       G_M000_IG46
 
G_M000_IG22:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG40
 
G_M000_IG23:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG46
 
G_M000_IG24:
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG38
 
G_M000_IG25:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x60], rax
       lea      rdi, [rbp-0x80]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0x80]
       mov      qword ptr [rbp-0x78], r8
       mov      rdi, rsi
       sar      rdi, 63
       and      rdi, rax
       sub      rdx, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rsi
       sub      rdx, rdi
       mov      rdi, r8
       sar      rdi, 63
       cmp      rdx, rdi
       jne      SHORT G_M000_IG28
       lea      rdi, [r8+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG26
       test     rdi, rdi
       jl       G_M000_IG73
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG72
       mov      rsi, gword ptr [rax+8*rcx+0x10]
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x78]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG27:
       jmp      G_M000_IG31
 
G_M000_IG28:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x120], rax
       mov      ecx, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0x104], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       lea      rdi, [rbp-0x90]
       mov      rsi, qword ptr [rbp-0x60]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x90]
       mov      r9d, dword ptr [rbp-0x88]
       mov      rcx, gword ptr [rbp-0x120]
       test     rcx, rcx
       jne      SHORT G_M000_IG33
 
G_M000_IG29:
       test     rax, rax
       jne      SHORT G_M000_IG32
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xA0], xmm0
       movsxd   rsi, dword ptr [rbp-0x104]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xA0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xA0]
       mov      esi, dword ptr [rbp-0x98]
 
G_M000_IG30:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG31:
       jmp      G_M000_IG47
 
G_M000_IG32:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG34
 
G_M000_IG33:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG34:
       test     rax, rax
       je       SHORT G_M000_IG37
 
G_M000_IG35:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG36:
       mov      edx, dword ptr [rbp-0x104]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG30
 
G_M000_IG37:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG36
 
G_M000_IG38:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG46
 
G_M000_IG39:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xA8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG47
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG46
 
G_M000_IG41:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       test     rdx, rdx
       je       SHORT G_M000_IG46
 
G_M000_IG42:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG44
 
G_M000_IG43:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG47
 
G_M000_IG44:
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG46
 
G_M000_IG45:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xB0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG47
 
G_M000_IG46:
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG47:
       test     rsi, rsi
       je       G_M000_IG68
 
G_M000_IG48:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG58
 
G_M000_IG49:
       mov      rax, qword ptr [rsi+0x08]
       test     r13, r13
       je       G_M000_IG68
 
G_M000_IG50:
       cmp      qword ptr [r13], rcx
       jne      G_M000_IG56
 
G_M000_IG51:
       mov      r13, qword ptr [r13+0x08]
       lea      rdx, [rax+r13]
       mov      qword ptr [rbp-0xD0], rdx
       mov      rdi, rax
       xor      rdi, rdx
       mov      rsi, r13
       xor      rsi, rdx
       and      rdi, rsi
       jl       SHORT G_M000_IG54
       lea      rdi, [rdx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG52
       test     rdi, rdi
       jl       G_M000_IG73
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG72
       mov      rax, gword ptr [rax+8*rcx+0x10]
       jmp      SHORT G_M000_IG53
 
G_M000_IG52:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      r13, qword ptr [rbp-0xD0]
       mov      qword ptr [rax+0x08], r13
 
G_M000_IG53:
       jmp      SHORT G_M000_IG55
 
G_M000_IG54:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       lea      rdi, [rbp-0xE0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xE0]
       mov      gword ptr [rbp-0x128], rax
       mov      ecx, dword ptr [rbp-0xD8]
       mov      dword ptr [rbp-0x108], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xF0], xmm0
       lea      rdi, [rbp-0xF0]
       mov      rsi, r13
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x128]
       mov      esi, dword ptr [rbp-0x108]
       mov      rdx, gword ptr [rbp-0xF0]
       mov      rcx, qword ptr [rbp-0xE8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG55:
       jmp      G_M000_IG69
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r13], rdi
       jne      G_M000_IG68
 
G_M000_IG57:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0xF8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xF8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG69
 
G_M000_IG58:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG64
 
G_M000_IG59:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       test     r13, r13
       je       G_M000_IG68
 
G_M000_IG60:
       cmp      qword ptr [r13], rdi
       jne      SHORT G_M000_IG62
 
G_M000_IG61:
       vmovsd   xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG69
 
G_M000_IG62:
       cmp      qword ptr [r13], rcx
       jne      SHORT G_M000_IG68
 
G_M000_IG63:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x100]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG69
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG68
 
G_M000_IG65:
       mov      rdi, rsi
       test     r13, r13
       je       SHORT G_M000_IG68
 
G_M000_IG66:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r13], rax
       jne      SHORT G_M000_IG68
 
G_M000_IG67:
       mov      rsi, r13
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG69
 
G_M000_IG68:
       mov      rdx, r13
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG69:
       jmp      G_M000_IG17
 
G_M000_IG70:
       mov      edi, 27
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG71:
       add      rsp, 264
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG72:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG73:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1835

