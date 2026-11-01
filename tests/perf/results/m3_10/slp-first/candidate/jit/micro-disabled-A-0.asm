; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 9 inlinees with PGO data; 43 single block inlinees; 128 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 0x558
       lea      rbp, [rsp+0x580]
       xor      eax, eax
       mov      qword ptr [rbp-0x518], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -0x4B0
       vmovdqa  xmmword ptr [rbp+rax-0x60], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r12, rdi
       mov      r13, rsi
       mov      rbx, rdx
       mov      r15, rcx
       mov      r14, r8
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x30], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x560], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x568], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x560]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x568]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x560]
       mov      gword ptr [rbp-0x538], rax
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x38], rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rax, qword ptr [rbp-0x38]
       jmp      G_M000_IG247
 
G_M000_IG03:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      G_M000_IG263
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG12
 
G_M000_IG05:
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
       jmp      G_M000_IG13
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG12
 
G_M000_IG07:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG12
 
G_M000_IG08:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG10
 
G_M000_IG09:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG13
 
G_M000_IG10:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG12
 
G_M000_IG11:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xB0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG13:
       test     rsi, rsi
       je       G_M000_IG34
 
G_M000_IG14:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG24
 
G_M000_IG15:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG34
 
G_M000_IG16:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG22
 
G_M000_IG17:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0xD0], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0xD8], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG20
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG18
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0xD8]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG19:
       jmp      G_M000_IG21
 
G_M000_IG20:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xE8], xmm0
       lea      rdi, [rbp-0xE8]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x578], rax
       mov      ecx, dword ptr [rbp-0xE0]
       mov      dword ptr [rbp-0x520], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xF8], xmm0
       lea      rdi, [rbp-0xF8]
       mov      rsi, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x578]
       mov      esi, dword ptr [rbp-0x520]
       mov      rdx, gword ptr [rbp-0xF8]
       mov      rcx, qword ptr [rbp-0xF0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG21:
       jmp      G_M000_IG35
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG34
 
G_M000_IG23:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x100], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x100]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG35
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG30
 
G_M000_IG25:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       test     r15, r15
       je       G_M000_IG34
 
G_M000_IG26:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG28
 
G_M000_IG27:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG35
 
G_M000_IG28:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG29:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x108]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG35
 
G_M000_IG30:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG31:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG34
 
G_M000_IG32:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG33:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG35
 
G_M000_IG34:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG35:
       test     rsi, rsi
       je       G_M000_IG62
 
G_M000_IG36:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG56
 
G_M000_IG37:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG62
 
G_M000_IG38:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG54
 
G_M000_IG39:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x128], rax
       lea      rdi, [rbp-0x140]
       mulx     r8, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      rsi, qword ptr [rbp-0x140]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rax
       sub      r8, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, rsi
       sar      rdi, 63
       cmp      r8, rdi
       jne      SHORT G_M000_IG42
       mov      rax, rsi
       mov      qword ptr [rbp-0x178], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG40
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG41
 
G_M000_IG40:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x178]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG41:
       jmp      G_M000_IG46
 
G_M000_IG42:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x138], xmm0
       lea      rdi, [rbp-0x138]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x580], rax
       mov      ecx, dword ptr [rbp-0x130]
       mov      dword ptr [rbp-0x524], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x150], xmm0
       lea      rdi, [rbp-0x150]
       mov      rsi, qword ptr [rbp-0x128]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x150]
       mov      r9d, dword ptr [rbp-0x148]
       mov      rcx, gword ptr [rbp-0x580]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG47
 
G_M000_IG43:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x170], xmm0
       movsxd   rsi, dword ptr [rbp-0x524]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x170]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG44:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x170]
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
 
G_M000_IG45:
       mov      rdi, gword ptr [rbp-0x160]
       mov      rsi, qword ptr [rbp-0x158]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG46:
       jmp      G_M000_IG63
 
G_M000_IG47:
       test     rcx, rcx
       je       SHORT G_M000_IG52
 
G_M000_IG48:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG49:
       test     rax, rax
       je       SHORT G_M000_IG53
 
G_M000_IG50:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG51:
       mov      edx, dword ptr [rbp-0x524]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x160], rax
       mov      qword ptr [rbp-0x158], rdx
       jmp      SHORT G_M000_IG45
 
G_M000_IG52:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG49
 
G_M000_IG53:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG51
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG62
 
G_M000_IG55:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x120], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x180], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x180]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x120]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG63
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG62
 
G_M000_IG57:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG62
 
G_M000_IG58:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG60
 
G_M000_IG59:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x118], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x110]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x118]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG63
 
G_M000_IG60:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG62
 
G_M000_IG61:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x188]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG63
 
G_M000_IG62:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG63:
       test     rsi, rsi
       je       G_M000_IG86
 
G_M000_IG64:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG76
 
G_M000_IG65:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG86
 
G_M000_IG66:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG74
 
G_M000_IG67:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x1A8], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x1B0], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG70
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG68
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG69
 
G_M000_IG68:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x1B0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG69:
       jmp      G_M000_IG73
 
G_M000_IG70:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
       lea      rdi, [rbp-0x1C0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG71:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1C0]
       vmovdqu32 xmmword ptr [rbp-0x1D0], xmm0
 
G_M000_IG72:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1F0], xmm0
       lea      rdi, [rbp-0x1F0]
       mov      rsi, qword ptr [rbp-0x1A8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1C8]
       mov      rdx, gword ptr [rbp-0x1F0]
       mov      rcx, qword ptr [rbp-0x1E8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1E0], rax
       mov      qword ptr [rbp-0x1D8], rdx
       mov      rdi, gword ptr [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x1D8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG73:
       jmp      G_M000_IG87
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG86
 
G_M000_IG75:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x1F8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG87
 
G_M000_IG76:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG82
 
G_M000_IG77:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       test     r15, r15
       je       G_M000_IG86
 
G_M000_IG78:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG80
 
G_M000_IG79:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x198], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG87
 
G_M000_IG80:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG86
 
G_M000_IG81:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x200]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG87
 
G_M000_IG82:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG86
 
G_M000_IG83:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG86
 
G_M000_IG84:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG86
 
G_M000_IG85:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG87
 
G_M000_IG86:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG87:
       test     rsi, rsi
       je       G_M000_IG106
 
G_M000_IG88:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG100
 
G_M000_IG89:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG106
 
G_M000_IG90:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG98
 
G_M000_IG91:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x220], rax
       lea      rdi, [rbp-0x258]
       mulx     r8, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      rsi, qword ptr [rbp-0x258]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rax
       sub      r8, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, rsi
       sar      rdi, 63
       cmp      r8, rdi
       jne      SHORT G_M000_IG94
       mov      rax, rsi
       mov      qword ptr [rbp-0x270], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG92
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG93
 
G_M000_IG92:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x270]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG93:
       jmp      G_M000_IG97
 
G_M000_IG94:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x230], xmm0
       lea      rdi, [rbp-0x230]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG95:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x230]
       vmovdqu32 xmmword ptr [rbp-0x240], xmm0
 
G_M000_IG96:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x268], xmm0
       lea      rdi, [rbp-0x268]
       mov      rsi, qword ptr [rbp-0x220]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x240]
       mov      rsi, qword ptr [rbp-0x238]
       mov      rdx, gword ptr [rbp-0x268]
       mov      rcx, qword ptr [rbp-0x260]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x250], rax
       mov      qword ptr [rbp-0x248], rdx
       mov      rdi, gword ptr [rbp-0x250]
       mov      rsi, qword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG97:
       jmp      G_M000_IG107
 
G_M000_IG98:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG106
 
G_M000_IG99:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x218], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x278], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x278]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x218]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG107
 
G_M000_IG100:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG106
 
G_M000_IG101:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG106
 
G_M000_IG102:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG104
 
G_M000_IG103:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x208]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG107
 
G_M000_IG104:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG106
 
G_M000_IG105:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x280], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x280]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG107
 
G_M000_IG106:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG107:
       test     rsi, rsi
       je       G_M000_IG130
 
G_M000_IG108:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG120
 
G_M000_IG109:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG130
 
G_M000_IG110:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG118
 
G_M000_IG111:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x2A0], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x2A8], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG114
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG112
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rcx, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG113
 
G_M000_IG112:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      rdi, qword ptr [rbp-0x2A8]
       mov      qword ptr [rcx+0x08], rdi
 
G_M000_IG113:
       jmp      G_M000_IG117
 
G_M000_IG114:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2B8], xmm0
       lea      rdi, [rbp-0x2B8]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG115:
       vmovdqu  xmm0, xmmword ptr [rbp-0x2B8]
       vmovdqu  xmmword ptr [rbp-0x2C8], xmm0
 
G_M000_IG116:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2E8], xmm0
       lea      rdi, [rbp-0x2E8]
       mov      rsi, qword ptr [rbp-0x2A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2C8]
       mov      rsi, qword ptr [rbp-0x2C0]
       mov      rdx, gword ptr [rbp-0x2E8]
       mov      rcx, qword ptr [rbp-0x2E0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2D8], rax
       mov      qword ptr [rbp-0x2D0], rdx
       mov      rdi, gword ptr [rbp-0x2D8]
       mov      rsi, qword ptr [rbp-0x2D0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rcx, rax
 
G_M000_IG117:
       mov      rax, rcx
       jmp      G_M000_IG131
 
G_M000_IG118:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG130
 
G_M000_IG119:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x298], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x2F0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2F0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x298]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG131
 
G_M000_IG120:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG126
 
G_M000_IG121:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm0
       test     r15, r15
       je       G_M000_IG130
 
G_M000_IG122:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG124
 
G_M000_IG123:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x290], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG131
 
G_M000_IG124:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG130
 
G_M000_IG125:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2F8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG131
 
G_M000_IG126:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG130
 
G_M000_IG127:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG130
 
G_M000_IG128:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG130
 
G_M000_IG129:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG131
 
G_M000_IG130:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG131:
       mov      gword ptr [rbp-0x540], rax
       test     r14, r14
       je       G_M000_IG151
 
G_M000_IG132:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r14], rcx
       jne      G_M000_IG144
 
G_M000_IG133:
       mov      r8, qword ptr [r14+0x08]
       mov      qword ptr [rbp-0x310], r8
       test     rbx, rbx
       je       G_M000_IG151
 
G_M000_IG134:
       mov      qword ptr [rbp-0x530], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG142
 
G_M000_IG135:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x320], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x328], rdx
       lea      rdx, [rbp-0x328]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x328]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG138
       mov      rax, qword ptr [rbp-0x328]
       mov      qword ptr [rbp-0x370], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG136
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG137
 
G_M000_IG136:
       mov      rdi, qword ptr [rbp-0x530]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x370]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG137:
       jmp      G_M000_IG141
 
G_M000_IG138:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x338], xmm0
       lea      rdi, [rbp-0x338]
       mov      rsi, qword ptr [rbp-0x310]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG139:
       vmovdqu  xmm0, xmmword ptr [rbp-0x338]
       vmovdqu  xmmword ptr [rbp-0x348], xmm0
 
G_M000_IG140:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x368], xmm0
       lea      rdi, [rbp-0x368]
       mov      rsi, qword ptr [rbp-0x320]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x348]
       mov      rsi, qword ptr [rbp-0x340]
       mov      rdx, gword ptr [rbp-0x368]
       mov      rcx, qword ptr [rbp-0x360]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x358], rax
       mov      qword ptr [rbp-0x350], rdx
       mov      rdi, gword ptr [rbp-0x358]
       mov      rsi, qword ptr [rbp-0x350]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG141:
       jmp      G_M000_IG152
 
G_M000_IG142:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG151
 
G_M000_IG143:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x318], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x378], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x378]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x318]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG152
 
G_M000_IG144:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG150
 
G_M000_IG145:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x300], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG151
 
G_M000_IG146:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG148
 
G_M000_IG147:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x308], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x300]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x308]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG152
 
G_M000_IG148:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG151
 
G_M000_IG149:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x380], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x380]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x300]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG152
 
G_M000_IG150:
       mov      rdx, qword ptr [r14]
 
G_M000_IG151:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG152:
       test     rsi, rsi
       je       G_M000_IG175
 
G_M000_IG153:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG165
 
G_M000_IG154:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG175
 
G_M000_IG155:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG163
 
G_M000_IG156:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x3A0], rdx
       lea      rsi, [rcx+rdx]
       mov      qword ptr [rbp-0x3A8], rsi
       mov      rdi, rcx
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG159
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG157
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG267
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG158
 
G_M000_IG157:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x3A8]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG158:
       jmp      G_M000_IG162
 
G_M000_IG159:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x3B8], xmm0
       lea      rdi, [rbp-0x3B8]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG160:
       vmovdqu  xmm0, xmmword ptr [rbp-0x3B8]
       vmovdqu  xmmword ptr [rbp-0x3C8], xmm0
 
G_M000_IG161:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x3E8], xmm0
       lea      rdi, [rbp-0x3E8]
       mov      rsi, qword ptr [rbp-0x3A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3C8]
       mov      rsi, qword ptr [rbp-0x3C0]
       mov      rdx, gword ptr [rbp-0x3E8]
       mov      rcx, qword ptr [rbp-0x3E0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3D8], rax
       mov      qword ptr [rbp-0x3D0], rdx
       mov      rdi, gword ptr [rbp-0x3D8]
       mov      rsi, qword ptr [rbp-0x3D0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG162:
       jmp      G_M000_IG176
 
G_M000_IG163:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG175
 
G_M000_IG164:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x398], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x3F0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3F0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x398]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG176
 
G_M000_IG165:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG171
 
G_M000_IG166:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x388], xmm0
       test     r15, r15
       je       G_M000_IG175
 
G_M000_IG167:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG169
 
G_M000_IG168:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x390], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x388]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x390]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG176
 
G_M000_IG169:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG175
 
G_M000_IG170:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x3F8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x388]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG176
 
G_M000_IG171:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG175
 
G_M000_IG172:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG175
 
G_M000_IG173:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG175
 
G_M000_IG174:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG176
 
G_M000_IG175:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG176:
       test     rsi, rsi
       je       G_M000_IG196
 
G_M000_IG177:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG189
 
G_M000_IG178:
       mov      rcx, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x410], rcx
       test     rbx, rbx
       je       G_M000_IG196
 
G_M000_IG179:
       mov      qword ptr [rbp-0x530], rax
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG187
 
G_M000_IG180:
       mov      r8, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x420], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x428], rdx
       lea      rdx, [rbp-0x428]
       mov      rdi, rcx
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x428]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG183
       mov      rax, qword ptr [rbp-0x428]
       mov      qword ptr [rbp-0x470], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG181
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG267
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG182
 
G_M000_IG181:
       mov      rdi, qword ptr [rbp-0x530]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x470]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG182:
       jmp      G_M000_IG186
 
G_M000_IG183:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x438], xmm0
       lea      rdi, [rbp-0x438]
       mov      rsi, qword ptr [rbp-0x410]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG184:
       vmovdqu  xmm0, xmmword ptr [rbp-0x438]
       vmovdqu  xmmword ptr [rbp-0x448], xmm0
 
G_M000_IG185:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x468], xmm0
       lea      rdi, [rbp-0x468]
       mov      rsi, qword ptr [rbp-0x420]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x448]
       mov      rsi, qword ptr [rbp-0x440]
       mov      rdx, gword ptr [rbp-0x468]
       mov      rcx, qword ptr [rbp-0x460]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x458], rax
       mov      qword ptr [rbp-0x450], rdx
       mov      rdi, gword ptr [rbp-0x458]
       mov      rsi, qword ptr [rbp-0x450]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG186:
       jmp      G_M000_IG197
 
G_M000_IG187:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG196
 
G_M000_IG188:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x418], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x478], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x478]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x418]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG197
 
G_M000_IG189:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG195
 
G_M000_IG190:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x400], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG196
 
G_M000_IG191:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG193
 
G_M000_IG192:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x408], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x400]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x408]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG197
 
G_M000_IG193:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG196
 
G_M000_IG194:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x480], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x480]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x400]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG197
 
G_M000_IG195:
       mov      rdx, qword ptr [rsi]
 
G_M000_IG196:
       mov      rdx, rbx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG197:
       test     rsi, rsi
       je       G_M000_IG220
 
G_M000_IG198:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG210
 
G_M000_IG199:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG220
 
G_M000_IG200:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG208
 
G_M000_IG201:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x4A0], rdx
       lea      rsi, [rcx+rdx]
       mov      qword ptr [rbp-0x4A8], rsi
       mov      rdi, rcx
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG204
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG202
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG267
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG203
 
G_M000_IG202:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x4A8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG203:
       jmp      G_M000_IG207
 
G_M000_IG204:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x4B8], xmm0
       lea      rdi, [rbp-0x4B8]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG205:
       vmovdqu  xmm0, xmmword ptr [rbp-0x4B8]
       vmovdqu  xmmword ptr [rbp-0x4C8], xmm0
 
G_M000_IG206:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x4E8], xmm0
       lea      rdi, [rbp-0x4E8]
       mov      rsi, qword ptr [rbp-0x4A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x4C8]
       mov      rsi, qword ptr [rbp-0x4C0]
       mov      rdx, gword ptr [rbp-0x4E8]
       mov      rcx, qword ptr [rbp-0x4E0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x4D8], rax
       mov      qword ptr [rbp-0x4D0], rdx
       mov      rdi, gword ptr [rbp-0x4D8]
       mov      rsi, qword ptr [rbp-0x4D0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG207:
       jmp      G_M000_IG221
 
G_M000_IG208:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG220
 
G_M000_IG209:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x498], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x4F0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x4F0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x498]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG221
 
G_M000_IG210:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG216
 
G_M000_IG211:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x488], xmm0
       test     r15, r15
       je       G_M000_IG220
 
G_M000_IG212:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG214
 
G_M000_IG213:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x490], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x488]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x490]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG221
 
G_M000_IG214:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG220
 
G_M000_IG215:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x4F8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x4F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x488]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG221
 
G_M000_IG216:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG220
 
G_M000_IG217:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG220
 
G_M000_IG218:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG220
 
G_M000_IG219:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG221
 
G_M000_IG220:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG221:
       mov      gword ptr [rbp-0x548], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x518], rdx
 
G_M000_IG222:
       mov      gword ptr [rbp-0x510], rdx
 
G_M000_IG223:
       mov      gword ptr [rbp-0x508], rdx
 
G_M000_IG224:
       mov      gword ptr [rbp-0x500], rdx
       test     rax, rax
       je       G_M000_IG242
 
G_M000_IG225:
       mov      rdx, 0xD1FFAB1E
       mov      r8, rdx
       cmp      qword ptr [rax], r8
       jne      G_M000_IG233
 
G_M000_IG226:
       mov      rsi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       G_M000_IG232
 
G_M000_IG227:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG229
 
G_M000_IG228:
       mov      rdx, qword ptr [rbx+0x08]
       lea      rcx, [rbp-0x518]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryLongBinary(int,long,long,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x548]
       je       G_M000_IG242
       mov      rdx, gword ptr [rbp-0x518]
       jmp      G_M000_IG243
 
G_M000_IG229:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG231
 
G_M000_IG230:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rsi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x510]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x548]
       je       G_M000_IG242
       mov      rdx, gword ptr [rbp-0x510]
       jmp      G_M000_IG243
 
G_M000_IG231:
       mov      rax, gword ptr [rbp-0x548]
       jmp      G_M000_IG242
 
G_M000_IG232:
       mov      rax, gword ptr [rbp-0x548]
       jmp      G_M000_IG242
 
G_M000_IG233:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG241
 
G_M000_IG234:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG240
 
G_M000_IG235:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG237
 
G_M000_IG236:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x508]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x548]
       je       SHORT G_M000_IG242
       mov      rdx, gword ptr [rbp-0x508]
       jmp      SHORT G_M000_IG243
 
G_M000_IG237:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG239
 
G_M000_IG238:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x500]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x548]
       je       SHORT G_M000_IG242
       mov      rdx, gword ptr [rbp-0x500]
       jmp      SHORT G_M000_IG243
 
G_M000_IG239:
       mov      rax, gword ptr [rbp-0x548]
       jmp      SHORT G_M000_IG242
 
G_M000_IG240:
       mov      rax, gword ptr [rbp-0x548]
       jmp      SHORT G_M000_IG242
 
G_M000_IG241:
       mov      rdx, qword ptr [rax]
 
G_M000_IG242:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG243:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x518], rcx
 
G_M000_IG244:
       mov      gword ptr [rbp-0x510], rcx
 
G_M000_IG245:
       mov      gword ptr [rbp-0x508], rcx
 
G_M000_IG246:
       mov      gword ptr [rbp-0x500], rcx
       mov      rcx, r15
       mov      edi, 11
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x550], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x558], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp-0x540]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x558]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp-0x550]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, gword ptr [rbp-0x558]
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x538], rax
       mov      rdi, qword ptr [rbp-0x40]
       add      rdi, 1
       jo       G_M000_IG268
       mov      rax, rdi
 
G_M000_IG247:
       mov      qword ptr [rbp-0x40], rax
       cmp      rax, r12
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG265
 
G_M000_IG248:
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r13, r13
       je       G_M000_IG12
 
G_M000_IG249:
       mov      rax, qword ptr [r13]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG06
 
G_M000_IG250:
       mov      rsi, qword ptr [r13+0x08]
       test     rbx, rbx
       je       G_M000_IG12
 
G_M000_IG251:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG04
 
G_M000_IG252:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x60], rax
       lea      rdi, [rbp-0x78]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0x78]
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
       jne      SHORT G_M000_IG255
       mov      rax, r8
       mov      qword ptr [rbp-0xA0], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG253
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG254
 
G_M000_IG253:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0xA0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG254:
       jmp      G_M000_IG258
 
G_M000_IG255:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x570], rax
       mov      ecx, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0x51C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       lea      rdi, [rbp-0x88]
       mov      rsi, qword ptr [rbp-0x60]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      r9d, dword ptr [rbp-0x80]
       mov      rcx, gword ptr [rbp-0x570]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG259
 
G_M000_IG256:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x98], xmm0
       movsxd   rsi, dword ptr [rbp-0x51C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x98]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x98]
       mov      esi, dword ptr [rbp-0x90]
 
G_M000_IG257:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG258:
       jmp      G_M000_IG13
 
G_M000_IG259:
       test     rcx, rcx
       je       SHORT G_M000_IG264
 
G_M000_IG260:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG261:
       test     rax, rax
       je       G_M000_IG03
 
G_M000_IG262:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG263:
       mov      edx, dword ptr [rbp-0x51C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG257
 
G_M000_IG264:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG261
 
G_M000_IG265:
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, qword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x538]
 
G_M000_IG266:
       add      rsp, 0x558
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG267:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG268:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7121

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 9 inlinees with PGO data; 43 single block inlinees; 128 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 0x558
       lea      rbp, [rsp+0x580]
       xor      eax, eax
       mov      qword ptr [rbp-0x518], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -0x4B0
       vmovdqa  xmmword ptr [rbp+rax-0x60], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r12, rdi
       mov      r13, rsi
       mov      rbx, rdx
       mov      r15, rcx
       mov      r14, r8
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x30], rax
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x560], rax
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       lea      rdi, bword ptr [rax+0x08]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x560]
       mov      gword ptr [rbp-0x538], rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x38], rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rax, qword ptr [rbp-0x38]
       jmp      G_M000_IG247
 
G_M000_IG03:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      G_M000_IG263
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG12
 
G_M000_IG05:
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
       jmp      G_M000_IG13
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG12
 
G_M000_IG07:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG12
 
G_M000_IG08:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG10
 
G_M000_IG09:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG13
 
G_M000_IG10:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG12
 
G_M000_IG11:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xB0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG13:
       test     rsi, rsi
       je       G_M000_IG34
 
G_M000_IG14:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG24
 
G_M000_IG15:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG34
 
G_M000_IG16:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG22
 
G_M000_IG17:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0xD0], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0xD8], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG20
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG18
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0xD8]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG19:
       jmp      G_M000_IG21
 
G_M000_IG20:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xE8], xmm0
       lea      rdi, [rbp-0xE8]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x570], rax
       mov      ecx, dword ptr [rbp-0xE0]
       mov      dword ptr [rbp-0x520], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xF8], xmm0
       lea      rdi, [rbp-0xF8]
       mov      rsi, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x570]
       mov      esi, dword ptr [rbp-0x520]
       mov      rdx, gword ptr [rbp-0xF8]
       mov      rcx, qword ptr [rbp-0xF0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG21:
       jmp      G_M000_IG35
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG34
 
G_M000_IG23:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x100], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x100]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG35
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG30
 
G_M000_IG25:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       test     r15, r15
       je       G_M000_IG34
 
G_M000_IG26:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG28
 
G_M000_IG27:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG35
 
G_M000_IG28:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG29:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x108]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG35
 
G_M000_IG30:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG31:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG34
 
G_M000_IG32:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG33:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG35
 
G_M000_IG34:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG35:
       test     rsi, rsi
       je       G_M000_IG62
 
G_M000_IG36:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG56
 
G_M000_IG37:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG62
 
G_M000_IG38:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG54
 
G_M000_IG39:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x128], rax
       lea      rdi, [rbp-0x140]
       mulx     r8, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      rsi, qword ptr [rbp-0x140]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rax
       sub      r8, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, rsi
       sar      rdi, 63
       cmp      r8, rdi
       jne      SHORT G_M000_IG42
       mov      rax, rsi
       mov      qword ptr [rbp-0x178], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG40
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG41
 
G_M000_IG40:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x178]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG41:
       jmp      G_M000_IG46
 
G_M000_IG42:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x138], xmm0
       lea      rdi, [rbp-0x138]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x578], rax
       mov      ecx, dword ptr [rbp-0x130]
       mov      dword ptr [rbp-0x524], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x150], xmm0
       lea      rdi, [rbp-0x150]
       mov      rsi, qword ptr [rbp-0x128]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x150]
       mov      r9d, dword ptr [rbp-0x148]
       mov      rcx, gword ptr [rbp-0x578]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG47
 
G_M000_IG43:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x170], xmm0
       movsxd   rsi, dword ptr [rbp-0x524]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x170]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG44:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x170]
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
 
G_M000_IG45:
       mov      rdi, gword ptr [rbp-0x160]
       mov      rsi, qword ptr [rbp-0x158]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG46:
       jmp      G_M000_IG63
 
G_M000_IG47:
       test     rcx, rcx
       je       SHORT G_M000_IG52
 
G_M000_IG48:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG49:
       test     rax, rax
       je       SHORT G_M000_IG53
 
G_M000_IG50:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG51:
       mov      edx, dword ptr [rbp-0x524]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x160], rax
       mov      qword ptr [rbp-0x158], rdx
       jmp      SHORT G_M000_IG45
 
G_M000_IG52:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG49
 
G_M000_IG53:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG51
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG62
 
G_M000_IG55:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x120], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x180], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x180]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x120]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG63
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG62
 
G_M000_IG57:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG62
 
G_M000_IG58:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG60
 
G_M000_IG59:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x118], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x110]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x118]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG63
 
G_M000_IG60:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG62
 
G_M000_IG61:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x188]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG63
 
G_M000_IG62:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG63:
       test     rsi, rsi
       je       G_M000_IG86
 
G_M000_IG64:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG76
 
G_M000_IG65:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG86
 
G_M000_IG66:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG74
 
G_M000_IG67:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x1A8], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x1B0], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG70
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG68
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG69
 
G_M000_IG68:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x1B0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG69:
       jmp      G_M000_IG73
 
G_M000_IG70:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
       lea      rdi, [rbp-0x1C0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG71:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1C0]
       vmovdqu32 xmmword ptr [rbp-0x1D0], xmm0
 
G_M000_IG72:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1F0], xmm0
       lea      rdi, [rbp-0x1F0]
       mov      rsi, qword ptr [rbp-0x1A8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1C8]
       mov      rdx, gword ptr [rbp-0x1F0]
       mov      rcx, qword ptr [rbp-0x1E8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1E0], rax
       mov      qword ptr [rbp-0x1D8], rdx
       mov      rdi, gword ptr [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x1D8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG73:
       jmp      G_M000_IG87
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG86
 
G_M000_IG75:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x1F8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG87
 
G_M000_IG76:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG82
 
G_M000_IG77:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       test     r15, r15
       je       G_M000_IG86
 
G_M000_IG78:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG80
 
G_M000_IG79:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x198], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG87
 
G_M000_IG80:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG86
 
G_M000_IG81:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x200]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG87
 
G_M000_IG82:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG86
 
G_M000_IG83:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG86
 
G_M000_IG84:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG86
 
G_M000_IG85:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG87
 
G_M000_IG86:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG87:
       test     rsi, rsi
       je       G_M000_IG106
 
G_M000_IG88:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG100
 
G_M000_IG89:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG106
 
G_M000_IG90:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG98
 
G_M000_IG91:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x220], rax
       lea      rdi, [rbp-0x258]
       mulx     r8, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      rsi, qword ptr [rbp-0x258]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rax
       sub      r8, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, rsi
       sar      rdi, 63
       cmp      r8, rdi
       jne      SHORT G_M000_IG94
       mov      rax, rsi
       mov      qword ptr [rbp-0x270], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG92
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG93
 
G_M000_IG92:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x270]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG93:
       jmp      G_M000_IG97
 
G_M000_IG94:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x230], xmm0
       lea      rdi, [rbp-0x230]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG95:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x230]
       vmovdqu32 xmmword ptr [rbp-0x240], xmm0
 
G_M000_IG96:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x268], xmm0
       lea      rdi, [rbp-0x268]
       mov      rsi, qword ptr [rbp-0x220]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x240]
       mov      rsi, qword ptr [rbp-0x238]
       mov      rdx, gword ptr [rbp-0x268]
       mov      rcx, qword ptr [rbp-0x260]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x250], rax
       mov      qword ptr [rbp-0x248], rdx
       mov      rdi, gword ptr [rbp-0x250]
       mov      rsi, qword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG97:
       jmp      G_M000_IG107
 
G_M000_IG98:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG106
 
G_M000_IG99:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x218], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x278], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x278]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x218]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG107
 
G_M000_IG100:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG106
 
G_M000_IG101:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG106
 
G_M000_IG102:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG104
 
G_M000_IG103:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x208]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG107
 
G_M000_IG104:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG106
 
G_M000_IG105:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x280], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x280]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG107
 
G_M000_IG106:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG107:
       test     rsi, rsi
       je       G_M000_IG130
 
G_M000_IG108:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG120
 
G_M000_IG109:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG130
 
G_M000_IG110:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG118
 
G_M000_IG111:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x2A0], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x2A8], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG114
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG112
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rcx, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG113
 
G_M000_IG112:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      rdi, qword ptr [rbp-0x2A8]
       mov      qword ptr [rcx+0x08], rdi
 
G_M000_IG113:
       jmp      G_M000_IG117
 
G_M000_IG114:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2B8], xmm0
       lea      rdi, [rbp-0x2B8]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG115:
       vmovdqu  xmm0, xmmword ptr [rbp-0x2B8]
       vmovdqu  xmmword ptr [rbp-0x2C8], xmm0
 
G_M000_IG116:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2E8], xmm0
       lea      rdi, [rbp-0x2E8]
       mov      rsi, qword ptr [rbp-0x2A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2C8]
       mov      rsi, qword ptr [rbp-0x2C0]
       mov      rdx, gword ptr [rbp-0x2E8]
       mov      rcx, qword ptr [rbp-0x2E0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2D8], rax
       mov      qword ptr [rbp-0x2D0], rdx
       mov      rdi, gword ptr [rbp-0x2D8]
       mov      rsi, qword ptr [rbp-0x2D0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rcx, rax
 
G_M000_IG117:
       mov      rax, rcx
       jmp      G_M000_IG131
 
G_M000_IG118:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG130
 
G_M000_IG119:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x298], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x2F0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2F0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x298]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG131
 
G_M000_IG120:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG126
 
G_M000_IG121:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm0
       test     r15, r15
       je       G_M000_IG130
 
G_M000_IG122:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG124
 
G_M000_IG123:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x290], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG131
 
G_M000_IG124:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG130
 
G_M000_IG125:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2F8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG131
 
G_M000_IG126:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG130
 
G_M000_IG127:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG130
 
G_M000_IG128:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG130
 
G_M000_IG129:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG131
 
G_M000_IG130:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG131:
       mov      gword ptr [rbp-0x540], rax
       test     r14, r14
       je       G_M000_IG151
 
G_M000_IG132:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r14], rcx
       jne      G_M000_IG144
 
G_M000_IG133:
       mov      r8, qword ptr [r14+0x08]
       mov      qword ptr [rbp-0x310], r8
       test     rbx, rbx
       je       G_M000_IG151
 
G_M000_IG134:
       mov      qword ptr [rbp-0x530], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG142
 
G_M000_IG135:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x320], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x328], rdx
       lea      rdx, [rbp-0x328]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x328]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG138
       mov      rax, qword ptr [rbp-0x328]
       mov      qword ptr [rbp-0x370], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG136
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG137
 
G_M000_IG136:
       mov      rdi, qword ptr [rbp-0x530]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x370]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG137:
       jmp      G_M000_IG141
 
G_M000_IG138:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x338], xmm0
       lea      rdi, [rbp-0x338]
       mov      rsi, qword ptr [rbp-0x310]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG139:
       vmovdqu  xmm0, xmmword ptr [rbp-0x338]
       vmovdqu  xmmword ptr [rbp-0x348], xmm0
 
G_M000_IG140:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x368], xmm0
       lea      rdi, [rbp-0x368]
       mov      rsi, qword ptr [rbp-0x320]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x348]
       mov      rsi, qword ptr [rbp-0x340]
       mov      rdx, gword ptr [rbp-0x368]
       mov      rcx, qword ptr [rbp-0x360]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x358], rax
       mov      qword ptr [rbp-0x350], rdx
       mov      rdi, gword ptr [rbp-0x358]
       mov      rsi, qword ptr [rbp-0x350]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG141:
       jmp      G_M000_IG152
 
G_M000_IG142:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG151
 
G_M000_IG143:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x318], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x378], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x378]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x318]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG152
 
G_M000_IG144:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG150
 
G_M000_IG145:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x300], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG151
 
G_M000_IG146:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG148
 
G_M000_IG147:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x308], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x300]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x308]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG152
 
G_M000_IG148:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG151
 
G_M000_IG149:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x380], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x380]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x300]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG152
 
G_M000_IG150:
       mov      rdx, qword ptr [r14]
 
G_M000_IG151:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG152:
       test     rsi, rsi
       je       G_M000_IG175
 
G_M000_IG153:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG165
 
G_M000_IG154:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG175
 
G_M000_IG155:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG163
 
G_M000_IG156:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x3A0], rdx
       lea      rsi, [rcx+rdx]
       mov      qword ptr [rbp-0x3A8], rsi
       mov      rdi, rcx
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG159
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG157
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG267
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG158
 
G_M000_IG157:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x3A8]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG158:
       jmp      G_M000_IG162
 
G_M000_IG159:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x3B8], xmm0
       lea      rdi, [rbp-0x3B8]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG160:
       vmovdqu  xmm0, xmmword ptr [rbp-0x3B8]
       vmovdqu  xmmword ptr [rbp-0x3C8], xmm0
 
G_M000_IG161:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x3E8], xmm0
       lea      rdi, [rbp-0x3E8]
       mov      rsi, qword ptr [rbp-0x3A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3C8]
       mov      rsi, qword ptr [rbp-0x3C0]
       mov      rdx, gword ptr [rbp-0x3E8]
       mov      rcx, qword ptr [rbp-0x3E0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3D8], rax
       mov      qword ptr [rbp-0x3D0], rdx
       mov      rdi, gword ptr [rbp-0x3D8]
       mov      rsi, qword ptr [rbp-0x3D0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG162:
       jmp      G_M000_IG176
 
G_M000_IG163:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG175
 
G_M000_IG164:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x398], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x3F0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3F0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x398]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG176
 
G_M000_IG165:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG171
 
G_M000_IG166:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x388], xmm0
       test     r15, r15
       je       G_M000_IG175
 
G_M000_IG167:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG169
 
G_M000_IG168:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x390], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x388]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x390]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG176
 
G_M000_IG169:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG175
 
G_M000_IG170:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x3F8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x388]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG176
 
G_M000_IG171:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG175
 
G_M000_IG172:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG175
 
G_M000_IG173:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG175
 
G_M000_IG174:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG176
 
G_M000_IG175:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG176:
       test     rsi, rsi
       je       G_M000_IG196
 
G_M000_IG177:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG189
 
G_M000_IG178:
       mov      rcx, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x410], rcx
       test     rbx, rbx
       je       G_M000_IG196
 
G_M000_IG179:
       mov      qword ptr [rbp-0x530], rax
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG187
 
G_M000_IG180:
       mov      r8, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x420], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x428], rdx
       lea      rdx, [rbp-0x428]
       mov      rdi, rcx
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x428]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG183
       mov      rax, qword ptr [rbp-0x428]
       mov      qword ptr [rbp-0x470], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG181
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG267
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG182
 
G_M000_IG181:
       mov      rdi, qword ptr [rbp-0x530]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x470]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG182:
       jmp      G_M000_IG186
 
G_M000_IG183:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x438], xmm0
       lea      rdi, [rbp-0x438]
       mov      rsi, qword ptr [rbp-0x410]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG184:
       vmovdqu  xmm0, xmmword ptr [rbp-0x438]
       vmovdqu  xmmword ptr [rbp-0x448], xmm0
 
G_M000_IG185:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x468], xmm0
       lea      rdi, [rbp-0x468]
       mov      rsi, qword ptr [rbp-0x420]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x448]
       mov      rsi, qword ptr [rbp-0x440]
       mov      rdx, gword ptr [rbp-0x468]
       mov      rcx, qword ptr [rbp-0x460]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x458], rax
       mov      qword ptr [rbp-0x450], rdx
       mov      rdi, gword ptr [rbp-0x458]
       mov      rsi, qword ptr [rbp-0x450]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG186:
       jmp      G_M000_IG197
 
G_M000_IG187:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG196
 
G_M000_IG188:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x418], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x478], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x478]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x418]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG197
 
G_M000_IG189:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG195
 
G_M000_IG190:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x400], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG196
 
G_M000_IG191:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG193
 
G_M000_IG192:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x408], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x400]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x408]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG197
 
G_M000_IG193:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG196
 
G_M000_IG194:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x480], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x480]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x400]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG197
 
G_M000_IG195:
       mov      rdx, qword ptr [rsi]
 
G_M000_IG196:
       mov      rdx, rbx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG197:
       test     rsi, rsi
       je       G_M000_IG220
 
G_M000_IG198:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG210
 
G_M000_IG199:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG220
 
G_M000_IG200:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG208
 
G_M000_IG201:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x4A0], rdx
       lea      rsi, [rcx+rdx]
       mov      qword ptr [rbp-0x4A8], rsi
       mov      rdi, rcx
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG204
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG202
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG267
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG203
 
G_M000_IG202:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x4A8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG203:
       jmp      G_M000_IG207
 
G_M000_IG204:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x4B8], xmm0
       lea      rdi, [rbp-0x4B8]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG205:
       vmovdqu  xmm0, xmmword ptr [rbp-0x4B8]
       vmovdqu  xmmword ptr [rbp-0x4C8], xmm0
 
G_M000_IG206:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x4E8], xmm0
       lea      rdi, [rbp-0x4E8]
       mov      rsi, qword ptr [rbp-0x4A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x4C8]
       mov      rsi, qword ptr [rbp-0x4C0]
       mov      rdx, gword ptr [rbp-0x4E8]
       mov      rcx, qword ptr [rbp-0x4E0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x4D8], rax
       mov      qword ptr [rbp-0x4D0], rdx
       mov      rdi, gword ptr [rbp-0x4D8]
       mov      rsi, qword ptr [rbp-0x4D0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG207:
       jmp      G_M000_IG221
 
G_M000_IG208:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG220
 
G_M000_IG209:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x498], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x4F0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x4F0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x498]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG221
 
G_M000_IG210:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG216
 
G_M000_IG211:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x488], xmm0
       test     r15, r15
       je       G_M000_IG220
 
G_M000_IG212:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG214
 
G_M000_IG213:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x490], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x488]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x490]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG221
 
G_M000_IG214:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG220
 
G_M000_IG215:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x4F8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x4F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x488]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG221
 
G_M000_IG216:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG220
 
G_M000_IG217:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG220
 
G_M000_IG218:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG220
 
G_M000_IG219:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG221
 
G_M000_IG220:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG221:
       mov      gword ptr [rbp-0x548], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x518], rdx
 
G_M000_IG222:
       mov      gword ptr [rbp-0x510], rdx
 
G_M000_IG223:
       mov      gword ptr [rbp-0x508], rdx
 
G_M000_IG224:
       mov      gword ptr [rbp-0x500], rdx
       test     rax, rax
       je       G_M000_IG242
 
G_M000_IG225:
       mov      rdx, 0xD1FFAB1E
       mov      r8, rdx
       cmp      qword ptr [rax], r8
       jne      G_M000_IG233
 
G_M000_IG226:
       mov      rsi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       G_M000_IG232
 
G_M000_IG227:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG229
 
G_M000_IG228:
       mov      rdx, qword ptr [rbx+0x08]
       lea      rcx, [rbp-0x518]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryLongBinary(int,long,long,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x548]
       je       G_M000_IG242
       mov      rdx, gword ptr [rbp-0x518]
       jmp      G_M000_IG243
 
G_M000_IG229:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG231
 
G_M000_IG230:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rsi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x510]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x548]
       je       G_M000_IG242
       mov      rdx, gword ptr [rbp-0x510]
       jmp      G_M000_IG243
 
G_M000_IG231:
       mov      rax, gword ptr [rbp-0x548]
       jmp      G_M000_IG242
 
G_M000_IG232:
       mov      rax, gword ptr [rbp-0x548]
       jmp      G_M000_IG242
 
G_M000_IG233:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG241
 
G_M000_IG234:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG240
 
G_M000_IG235:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG237
 
G_M000_IG236:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x508]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x548]
       je       SHORT G_M000_IG242
       mov      rdx, gword ptr [rbp-0x508]
       jmp      SHORT G_M000_IG243
 
G_M000_IG237:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG239
 
G_M000_IG238:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x500]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x548]
       je       SHORT G_M000_IG242
       mov      rdx, gword ptr [rbp-0x500]
       jmp      SHORT G_M000_IG243
 
G_M000_IG239:
       mov      rax, gword ptr [rbp-0x548]
       jmp      SHORT G_M000_IG242
 
G_M000_IG240:
       mov      rax, gword ptr [rbp-0x548]
       jmp      SHORT G_M000_IG242
 
G_M000_IG241:
       mov      rdx, qword ptr [rax]
 
G_M000_IG242:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG243:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x518], rcx
 
G_M000_IG244:
       mov      gword ptr [rbp-0x510], rcx
 
G_M000_IG245:
       mov      gword ptr [rbp-0x508], rcx
 
G_M000_IG246:
       mov      gword ptr [rbp-0x500], rcx
       mov      rcx, r15
       mov      edi, 23
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x550], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x558], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp-0x540]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x558]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp-0x550]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, gword ptr [rbp-0x558]
       call     [RyuPy.Runtime.PyOps:MakeTuple(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x538], rax
       mov      rdi, qword ptr [rbp-0x40]
       add      rdi, 1
       jo       G_M000_IG268
       mov      rax, rdi
 
G_M000_IG247:
       mov      qword ptr [rbp-0x40], rax
       cmp      rax, r12
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG265
 
G_M000_IG248:
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r13, r13
       je       G_M000_IG12
 
G_M000_IG249:
       mov      rax, qword ptr [r13]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG06
 
G_M000_IG250:
       mov      rsi, qword ptr [r13+0x08]
       test     rbx, rbx
       je       G_M000_IG12
 
G_M000_IG251:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG04
 
G_M000_IG252:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x60], rax
       lea      rdi, [rbp-0x78]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0x78]
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
       jne      SHORT G_M000_IG255
       mov      rax, r8
       mov      qword ptr [rbp-0xA0], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG253
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG254
 
G_M000_IG253:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0xA0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG254:
       jmp      G_M000_IG258
 
G_M000_IG255:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x568], rax
       mov      ecx, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0x51C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       lea      rdi, [rbp-0x88]
       mov      rsi, qword ptr [rbp-0x60]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      r9d, dword ptr [rbp-0x80]
       mov      rcx, gword ptr [rbp-0x568]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG259
 
G_M000_IG256:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x98], xmm0
       movsxd   rsi, dword ptr [rbp-0x51C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x98]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x98]
       mov      esi, dword ptr [rbp-0x90]
 
G_M000_IG257:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG258:
       jmp      G_M000_IG13
 
G_M000_IG259:
       test     rcx, rcx
       je       SHORT G_M000_IG264
 
G_M000_IG260:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG261:
       test     rax, rax
       je       G_M000_IG03
 
G_M000_IG262:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG263:
       mov      edx, dword ptr [rbp-0x51C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG257
 
G_M000_IG264:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG261
 
G_M000_IG265:
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, qword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x538]
 
G_M000_IG266:
       add      rsp, 0x558
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG267:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG268:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7076

; Assembly listing for method RyuPy.Generated.Module:m0_tail_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 9 inlinees with PGO data; 43 single block inlinees; 128 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 0x568
       lea      rbp, [rsp+0x590]
       xor      eax, eax
       mov      qword ptr [rbp-0x518], rax
       vxorps   xmm8, xmm8, xmm8
       mov      rax, -0x4B0
       vmovdqa  xmmword ptr [rbp+rax-0x60], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x40], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r12, rdi
       mov      r13, rsi
       mov      rbx, rdx
       mov      r15, rcx
       mov      r14, r8
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x30], rax
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x568], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x570], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x568]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x570]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x568]
       mov      gword ptr [rbp-0x538], rax
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x38], rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rax, qword ptr [rbp-0x38]
       jmp      G_M000_IG247
 
G_M000_IG03:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      G_M000_IG263
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG12
 
G_M000_IG05:
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
       jmp      G_M000_IG13
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG12
 
G_M000_IG07:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG12
 
G_M000_IG08:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG10
 
G_M000_IG09:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG13
 
G_M000_IG10:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG12
 
G_M000_IG11:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xB0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG13
 
G_M000_IG12:
       mov      rdx, rbx
       mov      rsi, r13
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG13:
       test     rsi, rsi
       je       G_M000_IG34
 
G_M000_IG14:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG24
 
G_M000_IG15:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG34
 
G_M000_IG16:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG22
 
G_M000_IG17:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0xD0], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0xD8], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG20
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG18
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0xD8]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG19:
       jmp      G_M000_IG21
 
G_M000_IG20:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xE8], xmm0
       lea      rdi, [rbp-0xE8]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x580], rax
       mov      ecx, dword ptr [rbp-0xE0]
       mov      dword ptr [rbp-0x520], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xF8], xmm0
       lea      rdi, [rbp-0xF8]
       mov      rsi, qword ptr [rbp-0xD0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x580]
       mov      esi, dword ptr [rbp-0x520]
       mov      rdx, gword ptr [rbp-0xF8]
       mov      rcx, qword ptr [rbp-0xF0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG21:
       jmp      G_M000_IG35
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG34
 
G_M000_IG23:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x100], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x100]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG35
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG30
 
G_M000_IG25:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       test     r15, r15
       je       G_M000_IG34
 
G_M000_IG26:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG28
 
G_M000_IG27:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xC0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xB8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xC0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG35
 
G_M000_IG28:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG29:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x108]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG35
 
G_M000_IG30:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG31:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG34
 
G_M000_IG32:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG33:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG35
 
G_M000_IG34:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG35:
       test     rsi, rsi
       je       G_M000_IG62
 
G_M000_IG36:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG56
 
G_M000_IG37:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG62
 
G_M000_IG38:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG54
 
G_M000_IG39:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x128], rax
       lea      rdi, [rbp-0x140]
       mulx     r8, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      rsi, qword ptr [rbp-0x140]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rax
       sub      r8, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, rsi
       sar      rdi, 63
       cmp      r8, rdi
       jne      SHORT G_M000_IG42
       mov      rax, rsi
       mov      qword ptr [rbp-0x178], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG40
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG41
 
G_M000_IG40:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x178]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG41:
       jmp      G_M000_IG46
 
G_M000_IG42:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x138], xmm0
       lea      rdi, [rbp-0x138]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x138]
       mov      gword ptr [rbp-0x588], rax
       mov      ecx, dword ptr [rbp-0x130]
       mov      dword ptr [rbp-0x524], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x150], xmm0
       lea      rdi, [rbp-0x150]
       mov      rsi, qword ptr [rbp-0x128]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x150]
       mov      r9d, dword ptr [rbp-0x148]
       mov      rcx, gword ptr [rbp-0x588]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG47
 
G_M000_IG43:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x170], xmm0
       movsxd   rsi, dword ptr [rbp-0x524]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x170]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG44:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x170]
       vmovdqu32 xmmword ptr [rbp-0x160], xmm0
 
G_M000_IG45:
       mov      rdi, gword ptr [rbp-0x160]
       mov      rsi, qword ptr [rbp-0x158]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG46:
       jmp      G_M000_IG63
 
G_M000_IG47:
       test     rcx, rcx
       je       SHORT G_M000_IG52
 
G_M000_IG48:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG49:
       test     rax, rax
       je       SHORT G_M000_IG53
 
G_M000_IG50:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG51:
       mov      edx, dword ptr [rbp-0x524]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x160], rax
       mov      qword ptr [rbp-0x158], rdx
       jmp      SHORT G_M000_IG45
 
G_M000_IG52:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG49
 
G_M000_IG53:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG51
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG62
 
G_M000_IG55:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x120], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x180], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x180]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x120]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG63
 
G_M000_IG56:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG62
 
G_M000_IG57:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG62
 
G_M000_IG58:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG60
 
G_M000_IG59:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x118], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x110]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x118]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG63
 
G_M000_IG60:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG62
 
G_M000_IG61:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x188]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG63
 
G_M000_IG62:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG63:
       test     rsi, rsi
       je       G_M000_IG86
 
G_M000_IG64:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG76
 
G_M000_IG65:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG86
 
G_M000_IG66:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG74
 
G_M000_IG67:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x1A8], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x1B0], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG70
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG68
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG69
 
G_M000_IG68:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x1B0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG69:
       jmp      G_M000_IG73
 
G_M000_IG70:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
       lea      rdi, [rbp-0x1C0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG71:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1C0]
       vmovdqu32 xmmword ptr [rbp-0x1D0], xmm0
 
G_M000_IG72:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1F0], xmm0
       lea      rdi, [rbp-0x1F0]
       mov      rsi, qword ptr [rbp-0x1A8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1C8]
       mov      rdx, gword ptr [rbp-0x1F0]
       mov      rcx, qword ptr [rbp-0x1E8]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1E0], rax
       mov      qword ptr [rbp-0x1D8], rdx
       mov      rdi, gword ptr [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x1D8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG73:
       jmp      G_M000_IG87
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG86
 
G_M000_IG75:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x1F8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG87
 
G_M000_IG76:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG82
 
G_M000_IG77:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       test     r15, r15
       je       G_M000_IG86
 
G_M000_IG78:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG80
 
G_M000_IG79:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x198], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x190]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG87
 
G_M000_IG80:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG86
 
G_M000_IG81:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x200], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x200]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG87
 
G_M000_IG82:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG86
 
G_M000_IG83:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG86
 
G_M000_IG84:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG86
 
G_M000_IG85:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG87
 
G_M000_IG86:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG87:
       test     rsi, rsi
       je       G_M000_IG106
 
G_M000_IG88:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG100
 
G_M000_IG89:
       mov      rdx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG106
 
G_M000_IG90:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG98
 
G_M000_IG91:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x220], rax
       lea      rdi, [rbp-0x258]
       mulx     r8, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      rsi, qword ptr [rbp-0x258]
       mov      rdi, rdx
       sar      rdi, 63
       and      rdi, rax
       sub      r8, rdi
       mov      rdi, rax
       sar      rdi, 63
       and      rdi, rdx
       sub      r8, rdi
       mov      rdi, rsi
       sar      rdi, 63
       cmp      r8, rdi
       jne      SHORT G_M000_IG94
       mov      rax, rsi
       mov      qword ptr [rbp-0x270], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG92
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG93
 
G_M000_IG92:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x270]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG93:
       jmp      G_M000_IG97
 
G_M000_IG94:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x230], xmm0
       lea      rdi, [rbp-0x230]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG95:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x230]
       vmovdqu32 xmmword ptr [rbp-0x240], xmm0
 
G_M000_IG96:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x268], xmm0
       lea      rdi, [rbp-0x268]
       mov      rsi, qword ptr [rbp-0x220]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x240]
       mov      rsi, qword ptr [rbp-0x238]
       mov      rdx, gword ptr [rbp-0x268]
       mov      rcx, qword ptr [rbp-0x260]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x250], rax
       mov      qword ptr [rbp-0x248], rdx
       mov      rdi, gword ptr [rbp-0x250]
       mov      rsi, qword ptr [rbp-0x248]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG97:
       jmp      G_M000_IG107
 
G_M000_IG98:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG106
 
G_M000_IG99:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x218], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x278], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x278]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x218]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG107
 
G_M000_IG100:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG106
 
G_M000_IG101:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x208], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG106
 
G_M000_IG102:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG104
 
G_M000_IG103:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x208]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG107
 
G_M000_IG104:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG106
 
G_M000_IG105:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x280], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x280]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x208]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG107
 
G_M000_IG106:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG107:
       test     rsi, rsi
       je       G_M000_IG130
 
G_M000_IG108:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG120
 
G_M000_IG109:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG130
 
G_M000_IG110:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG118
 
G_M000_IG111:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x2A0], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x2A8], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG114
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG112
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rcx, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG113
 
G_M000_IG112:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      rdi, qword ptr [rbp-0x2A8]
       mov      qword ptr [rcx+0x08], rdi
 
G_M000_IG113:
       jmp      G_M000_IG117
 
G_M000_IG114:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2B8], xmm0
       lea      rdi, [rbp-0x2B8]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG115:
       vmovdqu  xmm0, xmmword ptr [rbp-0x2B8]
       vmovdqu  xmmword ptr [rbp-0x2C8], xmm0
 
G_M000_IG116:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2E8], xmm0
       lea      rdi, [rbp-0x2E8]
       mov      rsi, qword ptr [rbp-0x2A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2C8]
       mov      rsi, qword ptr [rbp-0x2C0]
       mov      rdx, gword ptr [rbp-0x2E8]
       mov      rcx, qword ptr [rbp-0x2E0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2D8], rax
       mov      qword ptr [rbp-0x2D0], rdx
       mov      rdi, gword ptr [rbp-0x2D8]
       mov      rsi, qword ptr [rbp-0x2D0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rcx, rax
 
G_M000_IG117:
       mov      rax, rcx
       jmp      G_M000_IG131
 
G_M000_IG118:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG130
 
G_M000_IG119:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x298], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x2F0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2F0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x298]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG131
 
G_M000_IG120:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG126
 
G_M000_IG121:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm0
       test     r15, r15
       je       G_M000_IG130
 
G_M000_IG122:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG124
 
G_M000_IG123:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x290], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG131
 
G_M000_IG124:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG130
 
G_M000_IG125:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2F8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x288]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG131
 
G_M000_IG126:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG130
 
G_M000_IG127:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG130
 
G_M000_IG128:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG130
 
G_M000_IG129:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG131
 
G_M000_IG130:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG131:
       mov      gword ptr [rbp-0x540], rax
       test     r14, r14
       je       G_M000_IG151
 
G_M000_IG132:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r14], rcx
       jne      G_M000_IG144
 
G_M000_IG133:
       mov      r8, qword ptr [r14+0x08]
       mov      qword ptr [rbp-0x310], r8
       test     rbx, rbx
       je       G_M000_IG151
 
G_M000_IG134:
       mov      qword ptr [rbp-0x530], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG142
 
G_M000_IG135:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x320], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x328], rdx
       lea      rdx, [rbp-0x328]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x328]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG138
       mov      rax, qword ptr [rbp-0x328]
       mov      qword ptr [rbp-0x370], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG136
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG137
 
G_M000_IG136:
       mov      rdi, qword ptr [rbp-0x530]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x370]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG137:
       jmp      G_M000_IG141
 
G_M000_IG138:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x338], xmm0
       lea      rdi, [rbp-0x338]
       mov      rsi, qword ptr [rbp-0x310]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG139:
       vmovdqu  xmm0, xmmword ptr [rbp-0x338]
       vmovdqu  xmmword ptr [rbp-0x348], xmm0
 
G_M000_IG140:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x368], xmm0
       lea      rdi, [rbp-0x368]
       mov      rsi, qword ptr [rbp-0x320]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x348]
       mov      rsi, qword ptr [rbp-0x340]
       mov      rdx, gword ptr [rbp-0x368]
       mov      rcx, qword ptr [rbp-0x360]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x358], rax
       mov      qword ptr [rbp-0x350], rdx
       mov      rdi, gword ptr [rbp-0x358]
       mov      rsi, qword ptr [rbp-0x350]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG141:
       jmp      G_M000_IG152
 
G_M000_IG142:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG151
 
G_M000_IG143:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x318], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x378], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x378]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x318]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG152
 
G_M000_IG144:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG150
 
G_M000_IG145:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x300], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG151
 
G_M000_IG146:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG148
 
G_M000_IG147:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x308], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x300]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x308]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG152
 
G_M000_IG148:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG151
 
G_M000_IG149:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x380], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x380]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x300]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG152
 
G_M000_IG150:
       mov      rdx, qword ptr [r14]
 
G_M000_IG151:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG152:
       test     rsi, rsi
       je       G_M000_IG175
 
G_M000_IG153:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG165
 
G_M000_IG154:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG175
 
G_M000_IG155:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG163
 
G_M000_IG156:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x3A0], rdx
       lea      rsi, [rcx+rdx]
       mov      qword ptr [rbp-0x3A8], rsi
       mov      rdi, rcx
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG159
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG157
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG267
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG158
 
G_M000_IG157:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x3A8]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG158:
       jmp      G_M000_IG162
 
G_M000_IG159:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x3B8], xmm0
       lea      rdi, [rbp-0x3B8]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG160:
       vmovdqu  xmm0, xmmword ptr [rbp-0x3B8]
       vmovdqu  xmmword ptr [rbp-0x3C8], xmm0
 
G_M000_IG161:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x3E8], xmm0
       lea      rdi, [rbp-0x3E8]
       mov      rsi, qword ptr [rbp-0x3A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3C8]
       mov      rsi, qword ptr [rbp-0x3C0]
       mov      rdx, gword ptr [rbp-0x3E8]
       mov      rcx, qword ptr [rbp-0x3E0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3D8], rax
       mov      qword ptr [rbp-0x3D0], rdx
       mov      rdi, gword ptr [rbp-0x3D8]
       mov      rsi, qword ptr [rbp-0x3D0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG162:
       jmp      G_M000_IG176
 
G_M000_IG163:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG175
 
G_M000_IG164:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x398], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x3F0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3F0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x398]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG176
 
G_M000_IG165:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG171
 
G_M000_IG166:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x388], xmm0
       test     r15, r15
       je       G_M000_IG175
 
G_M000_IG167:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG169
 
G_M000_IG168:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x390], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x388]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x390]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG176
 
G_M000_IG169:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG175
 
G_M000_IG170:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x3F8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x388]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG176
 
G_M000_IG171:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG175
 
G_M000_IG172:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG175
 
G_M000_IG173:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG175
 
G_M000_IG174:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG176
 
G_M000_IG175:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG176:
       test     rsi, rsi
       je       G_M000_IG196
 
G_M000_IG177:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG189
 
G_M000_IG178:
       mov      rcx, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x410], rcx
       test     rbx, rbx
       je       G_M000_IG196
 
G_M000_IG179:
       mov      qword ptr [rbp-0x530], rax
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG187
 
G_M000_IG180:
       mov      r8, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x420], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x428], rdx
       lea      rdx, [rbp-0x428]
       mov      rdi, rcx
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x428]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG183
       mov      rax, qword ptr [rbp-0x428]
       mov      qword ptr [rbp-0x470], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG181
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG267
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG182
 
G_M000_IG181:
       mov      rdi, qword ptr [rbp-0x530]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x470]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG182:
       jmp      G_M000_IG186
 
G_M000_IG183:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x438], xmm0
       lea      rdi, [rbp-0x438]
       mov      rsi, qword ptr [rbp-0x410]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG184:
       vmovdqu  xmm0, xmmword ptr [rbp-0x438]
       vmovdqu  xmmword ptr [rbp-0x448], xmm0
 
G_M000_IG185:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x468], xmm0
       lea      rdi, [rbp-0x468]
       mov      rsi, qword ptr [rbp-0x420]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x448]
       mov      rsi, qword ptr [rbp-0x440]
       mov      rdx, gword ptr [rbp-0x468]
       mov      rcx, qword ptr [rbp-0x460]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x458], rax
       mov      qword ptr [rbp-0x450], rdx
       mov      rdi, gword ptr [rbp-0x458]
       mov      rsi, qword ptr [rbp-0x450]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG186:
       jmp      G_M000_IG197
 
G_M000_IG187:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG196
 
G_M000_IG188:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x418], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x478], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x478]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x418]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG197
 
G_M000_IG189:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG195
 
G_M000_IG190:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x400], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG196
 
G_M000_IG191:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG193
 
G_M000_IG192:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x408], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x400]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x408]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG197
 
G_M000_IG193:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG196
 
G_M000_IG194:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x480], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x480]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x400]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG197
 
G_M000_IG195:
       mov      rdx, qword ptr [rsi]
 
G_M000_IG196:
       mov      rdx, rbx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG197:
       test     rsi, rsi
       je       G_M000_IG220
 
G_M000_IG198:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG210
 
G_M000_IG199:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG220
 
G_M000_IG200:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG208
 
G_M000_IG201:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x4A0], rdx
       lea      rsi, [rcx+rdx]
       mov      qword ptr [rbp-0x4A8], rsi
       mov      rdi, rcx
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG204
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG202
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG267
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG203
 
G_M000_IG202:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x4A8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG203:
       jmp      G_M000_IG207
 
G_M000_IG204:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x4B8], xmm0
       lea      rdi, [rbp-0x4B8]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG205:
       vmovdqu  xmm0, xmmword ptr [rbp-0x4B8]
       vmovdqu  xmmword ptr [rbp-0x4C8], xmm0
 
G_M000_IG206:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x4E8], xmm0
       lea      rdi, [rbp-0x4E8]
       mov      rsi, qword ptr [rbp-0x4A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x4C8]
       mov      rsi, qword ptr [rbp-0x4C0]
       mov      rdx, gword ptr [rbp-0x4E8]
       mov      rcx, qword ptr [rbp-0x4E0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x4D8], rax
       mov      qword ptr [rbp-0x4D0], rdx
       mov      rdi, gword ptr [rbp-0x4D8]
       mov      rsi, qword ptr [rbp-0x4D0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG207:
       jmp      G_M000_IG221
 
G_M000_IG208:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG220
 
G_M000_IG209:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x498], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x4F0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x4F0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x498]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG221
 
G_M000_IG210:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG216
 
G_M000_IG211:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x488], xmm0
       test     r15, r15
       je       G_M000_IG220
 
G_M000_IG212:
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG214
 
G_M000_IG213:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x490], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x488]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x490]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG221
 
G_M000_IG214:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG220
 
G_M000_IG215:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x4F8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x4F8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x488]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG221
 
G_M000_IG216:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG220
 
G_M000_IG217:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG220
 
G_M000_IG218:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG220
 
G_M000_IG219:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG221
 
G_M000_IG220:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG221:
       mov      gword ptr [rbp-0x548], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x518], rdx
 
G_M000_IG222:
       mov      gword ptr [rbp-0x510], rdx
 
G_M000_IG223:
       mov      gword ptr [rbp-0x508], rdx
 
G_M000_IG224:
       mov      gword ptr [rbp-0x500], rdx
       test     rax, rax
       je       G_M000_IG242
 
G_M000_IG225:
       mov      rdx, 0xD1FFAB1E
       mov      r8, rdx
       cmp      qword ptr [rax], r8
       jne      G_M000_IG233
 
G_M000_IG226:
       mov      rsi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       G_M000_IG232
 
G_M000_IG227:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG229
 
G_M000_IG228:
       mov      rdx, qword ptr [rbx+0x08]
       lea      rcx, [rbp-0x518]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryLongBinary(int,long,long,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x548]
       je       G_M000_IG242
       mov      rdx, gword ptr [rbp-0x518]
       jmp      G_M000_IG243
 
G_M000_IG229:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG231
 
G_M000_IG230:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rsi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x510]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x548]
       je       G_M000_IG242
       mov      rdx, gword ptr [rbp-0x510]
       jmp      G_M000_IG243
 
G_M000_IG231:
       mov      rax, gword ptr [rbp-0x548]
       jmp      G_M000_IG242
 
G_M000_IG232:
       mov      rax, gword ptr [rbp-0x548]
       jmp      G_M000_IG242
 
G_M000_IG233:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG241
 
G_M000_IG234:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG240
 
G_M000_IG235:
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG237
 
G_M000_IG236:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x508]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x548]
       je       SHORT G_M000_IG242
       mov      rdx, gword ptr [rbp-0x508]
       jmp      SHORT G_M000_IG243
 
G_M000_IG237:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG239
 
G_M000_IG238:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x500]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x548]
       je       SHORT G_M000_IG242
       mov      rdx, gword ptr [rbp-0x500]
       jmp      SHORT G_M000_IG243
 
G_M000_IG239:
       mov      rax, gword ptr [rbp-0x548]
       jmp      SHORT G_M000_IG242
 
G_M000_IG240:
       mov      rax, gword ptr [rbp-0x548]
       jmp      SHORT G_M000_IG242
 
G_M000_IG241:
       mov      rdx, qword ptr [rax]
 
G_M000_IG242:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG243:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x518], rcx
 
G_M000_IG244:
       mov      gword ptr [rbp-0x510], rcx
 
G_M000_IG245:
       mov      gword ptr [rbp-0x508], rcx
 
G_M000_IG246:
       mov      gword ptr [rbp-0x500], rcx
       mov      rcx, r15
       mov      edi, 35
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x550], rax
       mov      rdx, r13
       mov      rcx, r14
       mov      edi, 36
       mov      esi, 2
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x558], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 3
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x560], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp-0x540]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x560]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp-0x550]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x560]
       lea      rdi, bword ptr [rax+0x20]
       mov      rsi, gword ptr [rbp-0x558]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, gword ptr [rbp-0x560]
       call     [RyuPy.Runtime.PyOps:MakeList(System.Object[]):System.Object]
       mov      gword ptr [rbp-0x538], rax
       mov      rdi, qword ptr [rbp-0x40]
       add      rdi, 1
       jo       G_M000_IG268
       mov      rax, rdi
 
G_M000_IG247:
       mov      qword ptr [rbp-0x40], rax
       cmp      rax, r12
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG265
 
G_M000_IG248:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r13, r13
       je       G_M000_IG12
 
G_M000_IG249:
       mov      rax, qword ptr [r13]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG06
 
G_M000_IG250:
       mov      rsi, qword ptr [r13+0x08]
       test     rbx, rbx
       je       G_M000_IG12
 
G_M000_IG251:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG04
 
G_M000_IG252:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x60], rax
       lea      rdi, [rbp-0x78]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0x78]
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
       jne      SHORT G_M000_IG255
       mov      rax, r8
       mov      qword ptr [rbp-0xA0], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG253
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG254
 
G_M000_IG253:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0xA0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG254:
       jmp      G_M000_IG258
 
G_M000_IG255:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x578], rax
       mov      ecx, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0x51C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x88], xmm0
       lea      rdi, [rbp-0x88]
       mov      rsi, qword ptr [rbp-0x60]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x88]
       mov      r9d, dword ptr [rbp-0x80]
       mov      rcx, gword ptr [rbp-0x578]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG259
 
G_M000_IG256:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x98], xmm0
       movsxd   rsi, dword ptr [rbp-0x51C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x98]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x98]
       mov      esi, dword ptr [rbp-0x90]
 
G_M000_IG257:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG258:
       jmp      G_M000_IG13
 
G_M000_IG259:
       test     rcx, rcx
       je       SHORT G_M000_IG264
 
G_M000_IG260:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG261:
       test     rax, rax
       je       G_M000_IG03
 
G_M000_IG262:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG263:
       mov      edx, dword ptr [rbp-0x51C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG257
 
G_M000_IG264:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG261
 
G_M000_IG265:
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, qword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x538]
 
G_M000_IG266:
       add      rsp, 0x568
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG267:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG268:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7173

