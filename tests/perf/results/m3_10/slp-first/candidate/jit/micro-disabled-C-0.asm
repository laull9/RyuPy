; Assembly listing for method RyuPy.Generated.Module:m0_list_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 9 inlinees with PGO data; 49 single block inlinees; 112 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 0x528
       lea      rbp, [rsp+0x550]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x4D0], xmm8
       vmovdqa32 xmmword ptr [rbp-0x4C0], xmm8
       mov      rax, -0x420
       vmovdqa32 xmmword ptr [rbp+rax-0x90], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x80], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x70], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r12, rdi
       mov      r14, rsi
       mov      r15, rdx
       mov      rbx, rcx
       mov      r13, r8
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x30], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x520], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x528], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x520]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x528]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x520]
       mov      gword ptr [rbp-0x4F8], rax
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
       jmp      G_M000_IG12
 
G_M000_IG03:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r14], rax
       jne      G_M000_IG14
 
G_M000_IG04:
       test     r15, r15
       je       G_M000_IG14
 
G_M000_IG05:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG14
 
G_M000_IG06:
       test     rbx, rbx
       je       G_M000_IG14
 
G_M000_IG07:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG14
 
G_M000_IG08:
       test     r13, r13
       je       G_M000_IG14
 
G_M000_IG09:
       cmp      qword ptr [r13], rax
       jne      G_M000_IG14
 
G_M000_IG10:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   xmm2, qword ptr [rbx+0x08]
       vmovsd   xmm3, qword ptr [r13+0x08]
       vunpcklpd xmm0, xmm0, xmm3
       vmovaps  xmm3, xmm1
       vunpcklpd xmm1, xmm3, xmm1
       vmovaps  xmm3, xmm2
       vunpcklpd xmm2, xmm3, xmm2
       vmulpd   xmm0, xmm1, xmm0
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmovaps  xmmword ptr [rbp-0x50], xmm0
       vmovaps  xmm1, xmm0
       vmovsd   qword ptr [rbp-0x58], xmm1
       mov      qword ptr [rbp-0x4E8], rax
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm1
       mov      gword ptr [rbp-0x500], rax
       vmovaps  xmm0, xmmword ptr [rbp-0x50]
       vunpckhpd xmm0, xmm0, xmm0
       vmovsd   qword ptr [rbp-0x60], xmm0
       mov      rdi, qword ptr [rbp-0x4E8]
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x60]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x510], rax
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x518], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp-0x500]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x518]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp-0x510]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x530], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x538], rax
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x518]
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x530]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x538]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x530]
       mov      gword ptr [rbp-0x4F8], rax
       mov      rdi, qword ptr [rbp-0x40]
       add      rdi, 1
       jo       G_M000_IG268
       mov      rax, rdi
 
G_M000_IG12:
       mov      qword ptr [rbp-0x40], rax
       cmp      rax, r12
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG265
 
G_M000_IG13:
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r14, r14
       jne      G_M000_IG03
 
G_M000_IG14:
       mov      rdi, r14
       test     rdi, rdi
       je       SHORT G_M000_IG21
 
G_M000_IG15:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      SHORT G_M000_IG21
 
G_M000_IG16:
       test     r15, r15
       je       SHORT G_M000_IG21
 
G_M000_IG17:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG21
 
G_M000_IG18:
       test     rbx, rbx
       je       SHORT G_M000_IG21
 
G_M000_IG19:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG21
 
G_M000_IG20:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   xmm2, qword ptr [rbx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp-0x68], xmm0
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG166
 
G_M000_IG21:
       test     r14, r14
       je       G_M000_IG47
 
G_M000_IG22:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r14], rcx
       jne      G_M000_IG41
 
G_M000_IG23:
       mov      rsi, qword ptr [r14+0x08]
       test     r15, r15
       je       G_M000_IG47
 
G_M000_IG24:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG39
 
G_M000_IG25:
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x90], rax
       lea      rdi, [rbp-0xA8]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0xA8]
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
       mov      rax, r8
       mov      qword ptr [rbp-0xD0], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG26
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0xD0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG27:
       jmp      G_M000_IG31
 
G_M000_IG28:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xA0], xmm0
       lea      rdi, [rbp-0xA0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA0]
       mov      gword ptr [rbp-0x540], rax
       mov      ecx, dword ptr [rbp-0x98]
       mov      dword ptr [rbp-0x4D4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       lea      rdi, [rbp-0xB8]
       mov      rsi, qword ptr [rbp-0x90]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      r9d, dword ptr [rbp-0xB0]
       mov      rcx, gword ptr [rbp-0x540]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG32
 
G_M000_IG29:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       movsxd   rsi, dword ptr [rbp-0x4D4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xC8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xC8]
       mov      esi, dword ptr [rbp-0xC0]
 
G_M000_IG30:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG31:
       jmp      G_M000_IG48
 
G_M000_IG32:
       test     rcx, rcx
       je       SHORT G_M000_IG37
 
G_M000_IG33:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG34:
       test     rax, rax
       je       SHORT G_M000_IG38
 
G_M000_IG35:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG36:
       mov      edx, dword ptr [rbp-0x4D4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG30
 
G_M000_IG37:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG34
 
G_M000_IG38:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG36
 
G_M000_IG39:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      G_M000_IG47
 
G_M000_IG40:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xD8], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xD8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG48
 
G_M000_IG41:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r14], rax
       jne      SHORT G_M000_IG47
 
G_M000_IG42:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x78], xmm0
       test     r15, r15
       je       SHORT G_M000_IG47
 
G_M000_IG43:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG45
 
G_M000_IG44:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x80], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x78]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG48
 
G_M000_IG45:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG47
 
G_M000_IG46:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x78]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG48
 
G_M000_IG47:
       mov      rdx, r15
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG48:
       test     rsi, rsi
       je       G_M000_IG69
 
G_M000_IG49:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG59
 
G_M000_IG50:
       mov      rax, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG69
 
G_M000_IG51:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG57
 
G_M000_IG52:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x100], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x108], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG55
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG53
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG54
 
G_M000_IG53:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x108]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG54:
       jmp      G_M000_IG56
 
G_M000_IG55:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x118], xmm0
       lea      rdi, [rbp-0x118]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x118]
       mov      gword ptr [rbp-0x548], rax
       mov      ecx, dword ptr [rbp-0x110]
       mov      dword ptr [rbp-0x4D8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       mov      rsi, qword ptr [rbp-0x100]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x548]
       mov      esi, dword ptr [rbp-0x4D8]
       mov      rdx, gword ptr [rbp-0x128]
       mov      rcx, qword ptr [rbp-0x120]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG56:
       jmp      G_M000_IG70
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG69
 
G_M000_IG58:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x130], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x130]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xF8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG70
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      G_M000_IG65
 
G_M000_IG60:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm0
       test     rbx, rbx
       je       G_M000_IG69
 
G_M000_IG61:
       cmp      qword ptr [rbx], rdx
       jne      SHORT G_M000_IG63
 
G_M000_IG62:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG70
 
G_M000_IG63:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG69
 
G_M000_IG64:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x138], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x138]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG70
 
G_M000_IG65:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG69
 
G_M000_IG66:
       mov      rdi, rsi
       test     rbx, rbx
       je       SHORT G_M000_IG69
 
G_M000_IG67:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG69
 
G_M000_IG68:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG70
 
G_M000_IG69:
       mov      rdx, rbx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG70:
       test     rsi, rsi
       je       G_M000_IG97
 
G_M000_IG71:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG91
 
G_M000_IG72:
       mov      rdx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG97
 
G_M000_IG73:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG89
 
G_M000_IG74:
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x158], rax
       lea      rdi, [rbp-0x170]
       mulx     r8, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      rsi, qword ptr [rbp-0x170]
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
       jne      SHORT G_M000_IG77
       mov      rax, rsi
       mov      qword ptr [rbp-0x1A8], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG75
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG76
 
G_M000_IG75:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x1A8]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG76:
       jmp      G_M000_IG81
 
G_M000_IG77:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       lea      rdi, [rbp-0x168]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x168]
       mov      gword ptr [rbp-0x550], rax
       mov      ecx, dword ptr [rbp-0x160]
       mov      dword ptr [rbp-0x4DC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x180], xmm0
       lea      rdi, [rbp-0x180]
       mov      rsi, qword ptr [rbp-0x158]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x180]
       mov      r9d, dword ptr [rbp-0x178]
       mov      rcx, gword ptr [rbp-0x550]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG82
 
G_M000_IG78:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1A0], xmm0
       movsxd   rsi, dword ptr [rbp-0x4DC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x1A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG79:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1A0]
       vmovdqu32 xmmword ptr [rbp-0x190], xmm0
 
G_M000_IG80:
       mov      rdi, gword ptr [rbp-0x190]
       mov      rsi, qword ptr [rbp-0x188]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG81:
       jmp      G_M000_IG98
 
G_M000_IG82:
       test     rcx, rcx
       je       SHORT G_M000_IG87
 
G_M000_IG83:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG84:
       test     rax, rax
       je       SHORT G_M000_IG88
 
G_M000_IG85:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG86:
       mov      edx, dword ptr [rbp-0x4DC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x190], rax
       mov      qword ptr [rbp-0x188], rdx
       jmp      SHORT G_M000_IG80
 
G_M000_IG87:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG84
 
G_M000_IG88:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG86
 
G_M000_IG89:
       mov      rdi, 0xD1FFAB1E
       mov      rax, rdi
       cmp      qword ptr [r15], rax
       jne      G_M000_IG97
 
G_M000_IG90:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x1B0], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1B0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x150]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG98
 
G_M000_IG91:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      SHORT G_M000_IG97
 
G_M000_IG92:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x140], xmm0
       test     r15, r15
       je       SHORT G_M000_IG97
 
G_M000_IG93:
       cmp      qword ptr [r15], rdx
       jne      SHORT G_M000_IG95
 
G_M000_IG94:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x148], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x140]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG98
 
G_M000_IG95:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG97
 
G_M000_IG96:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x1B8], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1B8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x140]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG98
 
G_M000_IG97:
       mov      rdx, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG98:
       test     rsi, rsi
       je       G_M000_IG121
 
G_M000_IG99:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG111
 
G_M000_IG100:
       mov      rax, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG121
 
G_M000_IG101:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG109
 
G_M000_IG102:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x1D8], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x1E0], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG105
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG103
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG104
 
G_M000_IG103:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x1E0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG104:
       jmp      G_M000_IG108
 
G_M000_IG105:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1F0], xmm0
       lea      rdi, [rbp-0x1F0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG106:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1F0]
       vmovdqu32 xmmword ptr [rbp-0x200], xmm0
 
G_M000_IG107:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x220], xmm0
       lea      rdi, [rbp-0x220]
       mov      rsi, qword ptr [rbp-0x1D8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x200]
       mov      rsi, qword ptr [rbp-0x1F8]
       mov      rdx, gword ptr [rbp-0x220]
       mov      rcx, qword ptr [rbp-0x218]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x210], rax
       mov      qword ptr [rbp-0x208], rdx
       mov      rdi, gword ptr [rbp-0x210]
       mov      rsi, qword ptr [rbp-0x208]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG108:
       jmp      G_M000_IG122
 
G_M000_IG109:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG121
 
G_M000_IG110:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x1D0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x228], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x228]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG122
 
G_M000_IG111:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      G_M000_IG117
 
G_M000_IG112:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x1C0], xmm0
       test     rbx, rbx
       je       G_M000_IG121
 
G_M000_IG113:
       cmp      qword ptr [rbx], rdx
       jne      SHORT G_M000_IG115
 
G_M000_IG114:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x1C8], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1C0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1C8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG122
 
G_M000_IG115:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG121
 
G_M000_IG116:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x230], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x230]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1C0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG122
 
G_M000_IG117:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG121
 
G_M000_IG118:
       mov      rdi, rsi
       test     rbx, rbx
       je       SHORT G_M000_IG121
 
G_M000_IG119:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG121
 
G_M000_IG120:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG122
 
G_M000_IG121:
       mov      rdx, rbx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG122:
       test     rsi, rsi
       je       G_M000_IG141
 
G_M000_IG123:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG135
 
G_M000_IG124:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x248], rax
       test     r15, r15
       je       G_M000_IG141
 
G_M000_IG125:
       mov      qword ptr [rbp-0x4F0], rcx
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG133
 
G_M000_IG126:
       mov      r8, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x258], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x260], rdx
       lea      rdx, [rbp-0x260]
       mov      rdi, rax
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x260]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG129
       mov      rax, qword ptr [rbp-0x260]
       mov      qword ptr [rbp-0x2A8], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG127
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG128
 
G_M000_IG127:
       mov      rdi, qword ptr [rbp-0x4F0]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x2A8]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG128:
       jmp      G_M000_IG132
 
G_M000_IG129:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x270], xmm0
       lea      rdi, [rbp-0x270]
       mov      rsi, qword ptr [rbp-0x248]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG130:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x270]
       vmovdqu32 xmmword ptr [rbp-0x280], xmm0
 
G_M000_IG131:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2A0], xmm0
       lea      rdi, [rbp-0x2A0]
       mov      rsi, qword ptr [rbp-0x258]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x280]
       mov      rsi, qword ptr [rbp-0x278]
       mov      rdx, gword ptr [rbp-0x2A0]
       mov      rcx, qword ptr [rbp-0x298]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x290], rax
       mov      qword ptr [rbp-0x288], rdx
       mov      rdi, gword ptr [rbp-0x290]
       mov      rsi, qword ptr [rbp-0x288]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG132:
       jmp      G_M000_IG142
 
G_M000_IG133:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG141
 
G_M000_IG134:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x250], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x2B0], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2B0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x250]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG142
 
G_M000_IG135:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      SHORT G_M000_IG141
 
G_M000_IG136:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x238], xmm0
       test     r15, r15
       je       SHORT G_M000_IG141
 
G_M000_IG137:
       cmp      qword ptr [r15], rdx
       jne      SHORT G_M000_IG139
 
G_M000_IG138:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x240], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x238]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x240]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG142
 
G_M000_IG139:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG141
 
G_M000_IG140:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2B8], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2B8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x238]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG142
 
G_M000_IG141:
       mov      rdx, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG142:
       test     rsi, rsi
       je       G_M000_IG165
 
G_M000_IG143:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG155
 
G_M000_IG144:
       mov      rax, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG165
 
G_M000_IG145:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG153
 
G_M000_IG146:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x2D8], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x2E0], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG149
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG147
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rax, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG148
 
G_M000_IG147:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x2E0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG148:
       jmp      G_M000_IG152
 
G_M000_IG149:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2F0], xmm0
       lea      rdi, [rbp-0x2F0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG150:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x2F0]
       vmovdqu32 xmmword ptr [rbp-0x300], xmm0
 
G_M000_IG151:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x320], xmm0
       lea      rdi, [rbp-0x320]
       mov      rsi, qword ptr [rbp-0x2D8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x300]
       mov      rsi, qword ptr [rbp-0x2F8]
       mov      rdx, gword ptr [rbp-0x320]
       mov      rcx, qword ptr [rbp-0x318]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x310], rax
       mov      qword ptr [rbp-0x308], rdx
       mov      rdi, gword ptr [rbp-0x310]
       mov      rsi, qword ptr [rbp-0x308]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG152:
       jmp      G_M000_IG166
 
G_M000_IG153:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG165
 
G_M000_IG154:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x2D0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x328], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x328]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x2D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG166
 
G_M000_IG155:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      SHORT G_M000_IG161
 
G_M000_IG156:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x2C0], xmm0
       test     rbx, rbx
       je       G_M000_IG165
 
G_M000_IG157:
       cmp      qword ptr [rbx], rdx
       jne      SHORT G_M000_IG159
 
G_M000_IG158:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x2C8], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2C0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2C8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG166
 
G_M000_IG159:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG165
 
G_M000_IG160:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x330], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x330]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x2C0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG166
 
G_M000_IG161:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG165
 
G_M000_IG162:
       mov      rdi, rsi
       test     rbx, rbx
       je       SHORT G_M000_IG165
 
G_M000_IG163:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG165
 
G_M000_IG164:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG166
 
G_M000_IG165:
       mov      rdx, rbx
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG166:
       mov      gword ptr [rbp-0x500], rax
       mov      rdi, r13
       test     rdi, rdi
       je       SHORT G_M000_IG173
 
G_M000_IG167:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [rdi], rcx
       jne      SHORT G_M000_IG173
 
G_M000_IG168:
       test     r15, r15
       je       SHORT G_M000_IG173
 
G_M000_IG169:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG173
 
G_M000_IG170:
       test     rbx, rbx
       je       SHORT G_M000_IG173
 
G_M000_IG171:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG173
 
G_M000_IG172:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   xmm2, qword ptr [rbx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp-0x70], xmm0
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG264
 
G_M000_IG173:
       test     r13, r13
       je       G_M000_IG192
 
G_M000_IG174:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r13], rcx
       jne      G_M000_IG186
 
G_M000_IG175:
       mov      r8, qword ptr [r13+0x08]
       mov      qword ptr [rbp-0x348], r8
       test     r15, r15
       je       G_M000_IG192
 
G_M000_IG176:
       mov      qword ptr [rbp-0x4F0], rcx
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG184
 
G_M000_IG177:
       mov      r9, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x358], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x360], rdx
       lea      rdx, [rbp-0x360]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x360]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG180
       mov      rax, qword ptr [rbp-0x360]
       mov      qword ptr [rbp-0x3A8], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG178
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
       jmp      SHORT G_M000_IG179
 
G_M000_IG178:
       mov      rdi, qword ptr [rbp-0x4F0]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x3A8]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG179:
       jmp      G_M000_IG183
 
G_M000_IG180:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x370], xmm0
       lea      rdi, [rbp-0x370]
       mov      rsi, qword ptr [rbp-0x348]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG181:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x370]
       vmovdqu32 xmmword ptr [rbp-0x380], xmm0
 
G_M000_IG182:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3A0], xmm0
       lea      rdi, [rbp-0x3A0]
       mov      rsi, qword ptr [rbp-0x358]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x380]
       mov      rsi, qword ptr [rbp-0x378]
       mov      rdx, gword ptr [rbp-0x3A0]
       mov      rcx, qword ptr [rbp-0x398]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x390], rax
       mov      qword ptr [rbp-0x388], rdx
       mov      rdi, gword ptr [rbp-0x390]
       mov      rsi, qword ptr [rbp-0x388]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG183:
       jmp      G_M000_IG193
 
G_M000_IG184:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG192
 
G_M000_IG185:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x350], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x3B0], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3B0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x350]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG193
 
G_M000_IG186:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      qword ptr [r13], rdx
       jne      SHORT G_M000_IG192
 
G_M000_IG187:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x338], xmm0
       test     r15, r15
       je       SHORT G_M000_IG192
 
G_M000_IG188:
       cmp      qword ptr [r15], rdx
       jne      SHORT G_M000_IG190
 
G_M000_IG189:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x340], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x338]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x340]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG193
 
G_M000_IG190:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG192
 
G_M000_IG191:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x3B8], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3B8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x338]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG193
 
G_M000_IG192:
       mov      rdx, r15
       mov      rsi, r13
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG193:
       test     rsi, rsi
       je       G_M000_IG216
 
G_M000_IG194:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG206
 
G_M000_IG195:
       mov      rcx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG216
 
G_M000_IG196:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG204
 
G_M000_IG197:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x3D8], rdx
       lea      rsi, [rcx+rdx]
       mov      qword ptr [rbp-0x3E0], rsi
       mov      rdi, rcx
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG200
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG198
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
       jmp      SHORT G_M000_IG199
 
G_M000_IG198:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x3E0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG199:
       jmp      G_M000_IG203
 
G_M000_IG200:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3F0], xmm0
       lea      rdi, [rbp-0x3F0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG201:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x3F0]
       vmovdqu32 xmmword ptr [rbp-0x400], xmm0
 
G_M000_IG202:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x420], xmm0
       lea      rdi, [rbp-0x420]
       mov      rsi, qword ptr [rbp-0x3D8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x400]
       mov      rsi, qword ptr [rbp-0x3F8]
       mov      rdx, gword ptr [rbp-0x420]
       mov      rcx, qword ptr [rbp-0x418]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x410], rax
       mov      qword ptr [rbp-0x408], rdx
       mov      rdi, gword ptr [rbp-0x410]
       mov      rsi, qword ptr [rbp-0x408]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG203:
       jmp      G_M000_IG217
 
G_M000_IG204:
       mov      rdx, 0xD1FFAB1E
       mov      rax, rdx
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG216
 
G_M000_IG205:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x3D0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x428], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x428]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x3D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG217
 
G_M000_IG206:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rsi], rcx
       jne      G_M000_IG212
 
G_M000_IG207:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x3C0], xmm0
       test     rbx, rbx
       je       G_M000_IG216
 
G_M000_IG208:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG210
 
G_M000_IG209:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x3C8], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x3C0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x3C8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG217
 
G_M000_IG210:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG216
 
G_M000_IG211:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x430], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x430]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x3C0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG217
 
G_M000_IG212:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG216
 
G_M000_IG213:
       mov      rdi, rsi
       test     rbx, rbx
       je       SHORT G_M000_IG216
 
G_M000_IG214:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG216
 
G_M000_IG215:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG217
 
G_M000_IG216:
       mov      rdx, rbx
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG217:
       test     rsi, rsi
       je       G_M000_IG236
 
G_M000_IG218:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG230
 
G_M000_IG219:
       mov      rcx, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x448], rcx
       test     r15, r15
       je       G_M000_IG236
 
G_M000_IG220:
       mov      qword ptr [rbp-0x4F0], rax
       cmp      qword ptr [r15], rax
       jne      G_M000_IG228
 
G_M000_IG221:
       mov      r8, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x458], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x460], rdx
       lea      rdx, [rbp-0x460]
       mov      rdi, rcx
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x460]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG224
       mov      rax, qword ptr [rbp-0x460]
       mov      qword ptr [rbp-0x4A8], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG222
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
       jmp      SHORT G_M000_IG223
 
G_M000_IG222:
       mov      rdi, qword ptr [rbp-0x4F0]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x4A8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG223:
       jmp      G_M000_IG227
 
G_M000_IG224:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x470], xmm0
       lea      rdi, [rbp-0x470]
       mov      rsi, qword ptr [rbp-0x448]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG225:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x470]
       vmovdqu32 xmmword ptr [rbp-0x480], xmm0
 
G_M000_IG226:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x4A0], xmm0
       lea      rdi, [rbp-0x4A0]
       mov      rsi, qword ptr [rbp-0x458]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x480]
       mov      rsi, qword ptr [rbp-0x478]
       mov      rdx, gword ptr [rbp-0x4A0]
       mov      rcx, qword ptr [rbp-0x498]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x490], rax
       mov      qword ptr [rbp-0x488], rdx
       mov      rdi, gword ptr [rbp-0x490]
       mov      rsi, qword ptr [rbp-0x488]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG227:
       jmp      G_M000_IG237
 
G_M000_IG228:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      qword ptr [r15], rdx
       jne      G_M000_IG236
 
G_M000_IG229:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x450], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x4B0], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x4B0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x450]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG237
 
G_M000_IG230:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG236
 
G_M000_IG231:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x438], xmm0
       test     r15, r15
       je       SHORT G_M000_IG236
 
G_M000_IG232:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG234
 
G_M000_IG233:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x440], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x438]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x440]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG237
 
G_M000_IG234:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG236
 
G_M000_IG235:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x4B8], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x4B8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x438]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG237
 
G_M000_IG236:
       mov      rdx, r15
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG237:
       mov      gword ptr [rbp-0x508], rax
       xor      rdi, rdi
       mov      gword ptr [rbp-0x4D0], rdi
 
G_M000_IG238:
       mov      gword ptr [rbp-0x4C8], rdi
 
G_M000_IG239:
       mov      gword ptr [rbp-0x4C0], rdi
       test     rax, rax
       je       G_M000_IG248
 
G_M000_IG240:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG249
 
G_M000_IG241:
       mov      rdi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG247
 
G_M000_IG242:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG244
 
G_M000_IG243:
       mov      rsi, qword ptr [rbx+0x08]
       call     [RyuPy.Runtime.PyInt:AddI64(long,long):System.Object]
       mov      rdx, rax
       jmp      G_M000_IG261
 
G_M000_IG244:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG246
 
G_M000_IG245:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rdi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x4D0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x508]
       je       SHORT G_M000_IG248
       mov      rdx, gword ptr [rbp-0x4D0]
       jmp      G_M000_IG261
 
G_M000_IG246:
       mov      rax, gword ptr [rbp-0x508]
       jmp      SHORT G_M000_IG248
 
G_M000_IG247:
       mov      rax, gword ptr [rbp-0x508]
 
G_M000_IG248:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       jmp      G_M000_IG261
 
G_M000_IG249:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG257
 
G_M000_IG250:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG256
 
G_M000_IG251:
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG253
 
G_M000_IG252:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x4C8]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x508]
       je       SHORT G_M000_IG248
       mov      rdx, gword ptr [rbp-0x4C8]
       jmp      G_M000_IG261
 
G_M000_IG253:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG255
 
G_M000_IG254:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x4C0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x508]
       je       G_M000_IG248
       mov      rdx, gword ptr [rbp-0x4C0]
       jmp      SHORT G_M000_IG261
 
G_M000_IG255:
       mov      rax, gword ptr [rbp-0x508]
       jmp      G_M000_IG248
 
G_M000_IG256:
       mov      rax, gword ptr [rbp-0x508]
       jmp      G_M000_IG248
 
G_M000_IG257:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG248
 
G_M000_IG258:
       mov      rdi, rax
       test     rbx, rbx
       je       G_M000_IG248
 
G_M000_IG259:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG248
 
G_M000_IG260:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rdx, rax
 
G_M000_IG261:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x4D0], rcx
 
G_M000_IG262:
       mov      gword ptr [rbp-0x4C8], rcx
 
G_M000_IG263:
       mov      gword ptr [rbp-0x4C0], rcx
       mov      rcx, r15
       mov      edi, 10
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 11
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG264:
       mov      gword ptr [rbp-0x510], rax
       jmp      G_M000_IG11
 
G_M000_IG265:
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, qword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x4F8]
 
G_M000_IG266:
       add      rsp, 0x528
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
 
; Total bytes of code 7243

; Assembly listing for method RyuPy.Generated.Module:m0_tuple_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 9 inlinees with PGO data; 49 single block inlinees; 112 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 0x518
       lea      rbp, [rsp+0x540]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x4D0], xmm8
       vmovdqa32 xmmword ptr [rbp-0x4C0], xmm8
       mov      rax, -0x420
       vmovdqa32 xmmword ptr [rbp+rax-0x90], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x80], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x70], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r12, rdi
       mov      r14, rsi
       mov      r15, rdx
       mov      rbx, rcx
       mov      r13, r8
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x30], rax
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x520], rax
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       lea      rdi, bword ptr [rax+0x08]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x520]
       mov      gword ptr [rbp-0x4F8], rax
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
       jmp      G_M000_IG12
 
G_M000_IG03:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r14], rax
       jne      G_M000_IG14
 
G_M000_IG04:
       test     r15, r15
       je       G_M000_IG14
 
G_M000_IG05:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG14
 
G_M000_IG06:
       test     rbx, rbx
       je       G_M000_IG14
 
G_M000_IG07:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG14
 
G_M000_IG08:
       test     r13, r13
       je       G_M000_IG14
 
G_M000_IG09:
       cmp      qword ptr [r13], rax
       jne      G_M000_IG14
 
G_M000_IG10:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   xmm2, qword ptr [rbx+0x08]
       vmovsd   xmm3, qword ptr [r13+0x08]
       vunpcklpd xmm0, xmm0, xmm3
       vmovaps  xmm3, xmm1
       vunpcklpd xmm1, xmm3, xmm1
       vmovaps  xmm3, xmm2
       vunpcklpd xmm2, xmm3, xmm2
       vmulpd   xmm0, xmm1, xmm0
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmovaps  xmmword ptr [rbp-0x50], xmm0
       vmovaps  xmm1, xmm0
       vmovsd   qword ptr [rbp-0x58], xmm1
       mov      qword ptr [rbp-0x4E8], rax
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm1
       mov      gword ptr [rbp-0x500], rax
       vmovaps  xmm0, xmmword ptr [rbp-0x50]
       vunpckhpd xmm0, xmm0, xmm0
       vmovsd   qword ptr [rbp-0x60], xmm0
       mov      rdi, qword ptr [rbp-0x4E8]
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x60]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x510], rax
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 2
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x518], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp-0x500]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x518]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp-0x510]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x528], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x518]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x528]
       mov      gword ptr [rbp-0x4F8], rax
       mov      rdi, qword ptr [rbp-0x40]
       add      rdi, 1
       jo       G_M000_IG268
       mov      rax, rdi
 
G_M000_IG12:
       mov      qword ptr [rbp-0x40], rax
       cmp      rax, r12
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG265
 
G_M000_IG13:
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r14, r14
       jne      G_M000_IG03
 
G_M000_IG14:
       mov      rdi, r14
       test     rdi, rdi
       je       SHORT G_M000_IG21
 
G_M000_IG15:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      SHORT G_M000_IG21
 
G_M000_IG16:
       test     r15, r15
       je       SHORT G_M000_IG21
 
G_M000_IG17:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG21
 
G_M000_IG18:
       test     rbx, rbx
       je       SHORT G_M000_IG21
 
G_M000_IG19:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG21
 
G_M000_IG20:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   xmm2, qword ptr [rbx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp-0x68], xmm0
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG166
 
G_M000_IG21:
       test     r14, r14
       je       G_M000_IG47
 
G_M000_IG22:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r14], rcx
       jne      G_M000_IG41
 
G_M000_IG23:
       mov      rsi, qword ptr [r14+0x08]
       test     r15, r15
       je       G_M000_IG47
 
G_M000_IG24:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG39
 
G_M000_IG25:
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x90], rax
       lea      rdi, [rbp-0xA8]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0xA8]
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
       mov      rax, r8
       mov      qword ptr [rbp-0xD0], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG26
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG27
 
G_M000_IG26:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0xD0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG27:
       jmp      G_M000_IG31
 
G_M000_IG28:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xA0], xmm0
       lea      rdi, [rbp-0xA0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA0]
       mov      gword ptr [rbp-0x530], rax
       mov      ecx, dword ptr [rbp-0x98]
       mov      dword ptr [rbp-0x4D4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       lea      rdi, [rbp-0xB8]
       mov      rsi, qword ptr [rbp-0x90]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      r9d, dword ptr [rbp-0xB0]
       mov      rcx, gword ptr [rbp-0x530]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG32
 
G_M000_IG29:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       movsxd   rsi, dword ptr [rbp-0x4D4]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xC8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xC8]
       mov      esi, dword ptr [rbp-0xC0]
 
G_M000_IG30:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG31:
       jmp      G_M000_IG48
 
G_M000_IG32:
       test     rcx, rcx
       je       SHORT G_M000_IG37
 
G_M000_IG33:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG34:
       test     rax, rax
       je       SHORT G_M000_IG38
 
G_M000_IG35:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG36:
       mov      edx, dword ptr [rbp-0x4D4]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG30
 
G_M000_IG37:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG34
 
G_M000_IG38:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG36
 
G_M000_IG39:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r15], rax
       jne      G_M000_IG47
 
G_M000_IG40:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xD8], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xD8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG48
 
G_M000_IG41:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r14], rax
       jne      SHORT G_M000_IG47
 
G_M000_IG42:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x78], xmm0
       test     r15, r15
       je       SHORT G_M000_IG47
 
G_M000_IG43:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG45
 
G_M000_IG44:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x80], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x78]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG48
 
G_M000_IG45:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG47
 
G_M000_IG46:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x78]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG48
 
G_M000_IG47:
       mov      rdx, r15
       mov      rsi, r14
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG48:
       test     rsi, rsi
       je       G_M000_IG69
 
G_M000_IG49:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG59
 
G_M000_IG50:
       mov      rax, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG69
 
G_M000_IG51:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG57
 
G_M000_IG52:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x100], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x108], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG55
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG53
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG54
 
G_M000_IG53:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x108]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG54:
       jmp      G_M000_IG56
 
G_M000_IG55:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x118], xmm0
       lea      rdi, [rbp-0x118]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x118]
       mov      gword ptr [rbp-0x538], rax
       mov      ecx, dword ptr [rbp-0x110]
       mov      dword ptr [rbp-0x4D8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       mov      rsi, qword ptr [rbp-0x100]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x538]
       mov      esi, dword ptr [rbp-0x4D8]
       mov      rdx, gword ptr [rbp-0x128]
       mov      rcx, qword ptr [rbp-0x120]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG56:
       jmp      G_M000_IG70
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG69
 
G_M000_IG58:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x130], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x130]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xF8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG70
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      G_M000_IG65
 
G_M000_IG60:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm0
       test     rbx, rbx
       je       G_M000_IG69
 
G_M000_IG61:
       cmp      qword ptr [rbx], rdx
       jne      SHORT G_M000_IG63
 
G_M000_IG62:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG70
 
G_M000_IG63:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG69
 
G_M000_IG64:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x138], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x138]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG70
 
G_M000_IG65:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG69
 
G_M000_IG66:
       mov      rdi, rsi
       test     rbx, rbx
       je       SHORT G_M000_IG69
 
G_M000_IG67:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG69
 
G_M000_IG68:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG70
 
G_M000_IG69:
       mov      rdx, rbx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG70:
       test     rsi, rsi
       je       G_M000_IG97
 
G_M000_IG71:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG91
 
G_M000_IG72:
       mov      rdx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG97
 
G_M000_IG73:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG89
 
G_M000_IG74:
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x158], rax
       lea      rdi, [rbp-0x170]
       mulx     r8, rsi, rax
       mov      qword ptr [rdi], rsi
       mov      rsi, qword ptr [rbp-0x170]
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
       jne      SHORT G_M000_IG77
       mov      rax, rsi
       mov      qword ptr [rbp-0x1A8], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG75
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG76
 
G_M000_IG75:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x1A8]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG76:
       jmp      G_M000_IG81
 
G_M000_IG77:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       lea      rdi, [rbp-0x168]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x168]
       mov      gword ptr [rbp-0x540], rax
       mov      ecx, dword ptr [rbp-0x160]
       mov      dword ptr [rbp-0x4DC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x180], xmm0
       lea      rdi, [rbp-0x180]
       mov      rsi, qword ptr [rbp-0x158]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x180]
       mov      r9d, dword ptr [rbp-0x178]
       mov      rcx, gword ptr [rbp-0x540]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG82
 
G_M000_IG78:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1A0], xmm0
       movsxd   rsi, dword ptr [rbp-0x4DC]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x1A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG79:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1A0]
       vmovdqu32 xmmword ptr [rbp-0x190], xmm0
 
G_M000_IG80:
       mov      rdi, gword ptr [rbp-0x190]
       mov      rsi, qword ptr [rbp-0x188]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG81:
       jmp      G_M000_IG98
 
G_M000_IG82:
       test     rcx, rcx
       je       SHORT G_M000_IG87
 
G_M000_IG83:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG84:
       test     rax, rax
       je       SHORT G_M000_IG88
 
G_M000_IG85:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG86:
       mov      edx, dword ptr [rbp-0x4DC]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x190], rax
       mov      qword ptr [rbp-0x188], rdx
       jmp      SHORT G_M000_IG80
 
G_M000_IG87:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG84
 
G_M000_IG88:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG86
 
G_M000_IG89:
       mov      rdi, 0xD1FFAB1E
       mov      rax, rdi
       cmp      qword ptr [r15], rax
       jne      G_M000_IG97
 
G_M000_IG90:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x1B0], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1B0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x150]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG98
 
G_M000_IG91:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      SHORT G_M000_IG97
 
G_M000_IG92:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x140], xmm0
       test     r15, r15
       je       SHORT G_M000_IG97
 
G_M000_IG93:
       cmp      qword ptr [r15], rdx
       jne      SHORT G_M000_IG95
 
G_M000_IG94:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x148], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x140]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG98
 
G_M000_IG95:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG97
 
G_M000_IG96:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x1B8], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1B8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x140]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG98
 
G_M000_IG97:
       mov      rdx, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG98:
       test     rsi, rsi
       je       G_M000_IG121
 
G_M000_IG99:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG111
 
G_M000_IG100:
       mov      rax, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG121
 
G_M000_IG101:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG109
 
G_M000_IG102:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x1D8], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x1E0], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG105
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG103
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG104
 
G_M000_IG103:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x1E0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG104:
       jmp      G_M000_IG108
 
G_M000_IG105:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1F0], xmm0
       lea      rdi, [rbp-0x1F0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG106:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1F0]
       vmovdqu32 xmmword ptr [rbp-0x200], xmm0
 
G_M000_IG107:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x220], xmm0
       lea      rdi, [rbp-0x220]
       mov      rsi, qword ptr [rbp-0x1D8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x200]
       mov      rsi, qword ptr [rbp-0x1F8]
       mov      rdx, gword ptr [rbp-0x220]
       mov      rcx, qword ptr [rbp-0x218]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x210], rax
       mov      qword ptr [rbp-0x208], rdx
       mov      rdi, gword ptr [rbp-0x210]
       mov      rsi, qword ptr [rbp-0x208]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG108:
       jmp      G_M000_IG122
 
G_M000_IG109:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG121
 
G_M000_IG110:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x1D0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x228], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x228]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG122
 
G_M000_IG111:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      G_M000_IG117
 
G_M000_IG112:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x1C0], xmm0
       test     rbx, rbx
       je       G_M000_IG121
 
G_M000_IG113:
       cmp      qword ptr [rbx], rdx
       jne      SHORT G_M000_IG115
 
G_M000_IG114:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x1C8], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1C0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x1C8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG122
 
G_M000_IG115:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG121
 
G_M000_IG116:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x230], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x230]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x1C0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG122
 
G_M000_IG117:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG121
 
G_M000_IG118:
       mov      rdi, rsi
       test     rbx, rbx
       je       SHORT G_M000_IG121
 
G_M000_IG119:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG121
 
G_M000_IG120:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG122
 
G_M000_IG121:
       mov      rdx, rbx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG122:
       test     rsi, rsi
       je       G_M000_IG141
 
G_M000_IG123:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG135
 
G_M000_IG124:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x248], rax
       test     r15, r15
       je       G_M000_IG141
 
G_M000_IG125:
       mov      qword ptr [rbp-0x4F0], rcx
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG133
 
G_M000_IG126:
       mov      r8, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x258], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x260], rdx
       lea      rdx, [rbp-0x260]
       mov      rdi, rax
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x260]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG129
       mov      rax, qword ptr [rbp-0x260]
       mov      qword ptr [rbp-0x2A8], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG127
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG267
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG128
 
G_M000_IG127:
       mov      rdi, qword ptr [rbp-0x4F0]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x2A8]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG128:
       jmp      G_M000_IG132
 
G_M000_IG129:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x270], xmm0
       lea      rdi, [rbp-0x270]
       mov      rsi, qword ptr [rbp-0x248]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG130:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x270]
       vmovdqu32 xmmword ptr [rbp-0x280], xmm0
 
G_M000_IG131:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2A0], xmm0
       lea      rdi, [rbp-0x2A0]
       mov      rsi, qword ptr [rbp-0x258]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x280]
       mov      rsi, qword ptr [rbp-0x278]
       mov      rdx, gword ptr [rbp-0x2A0]
       mov      rcx, qword ptr [rbp-0x298]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x290], rax
       mov      qword ptr [rbp-0x288], rdx
       mov      rdi, gword ptr [rbp-0x290]
       mov      rsi, qword ptr [rbp-0x288]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG132:
       jmp      G_M000_IG142
 
G_M000_IG133:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG141
 
G_M000_IG134:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x250], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x2B0], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2B0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x250]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG142
 
G_M000_IG135:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      SHORT G_M000_IG141
 
G_M000_IG136:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x238], xmm0
       test     r15, r15
       je       SHORT G_M000_IG141
 
G_M000_IG137:
       cmp      qword ptr [r15], rdx
       jne      SHORT G_M000_IG139
 
G_M000_IG138:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x240], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x238]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x240]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG142
 
G_M000_IG139:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG141
 
G_M000_IG140:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x2B8], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2B8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x238]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG142
 
G_M000_IG141:
       mov      rdx, r15
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG142:
       test     rsi, rsi
       je       G_M000_IG165
 
G_M000_IG143:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG155
 
G_M000_IG144:
       mov      rax, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG165
 
G_M000_IG145:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG153
 
G_M000_IG146:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x2D8], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x2E0], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG149
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG147
       test     rdi, rdi
       jl       G_M000_IG268
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG267
       mov      rax, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG148
 
G_M000_IG147:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x2E0]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG148:
       jmp      G_M000_IG152
 
G_M000_IG149:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2F0], xmm0
       lea      rdi, [rbp-0x2F0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG150:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x2F0]
       vmovdqu32 xmmword ptr [rbp-0x300], xmm0
 
G_M000_IG151:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x320], xmm0
       lea      rdi, [rbp-0x320]
       mov      rsi, qword ptr [rbp-0x2D8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x300]
       mov      rsi, qword ptr [rbp-0x2F8]
       mov      rdx, gword ptr [rbp-0x320]
       mov      rcx, qword ptr [rbp-0x318]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x310], rax
       mov      qword ptr [rbp-0x308], rdx
       mov      rdi, gword ptr [rbp-0x310]
       mov      rsi, qword ptr [rbp-0x308]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG152:
       jmp      G_M000_IG166
 
G_M000_IG153:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG165
 
G_M000_IG154:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x2D0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x328], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x328]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x2D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG166
 
G_M000_IG155:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      SHORT G_M000_IG161
 
G_M000_IG156:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x2C0], xmm0
       test     rbx, rbx
       je       G_M000_IG165
 
G_M000_IG157:
       cmp      qword ptr [rbx], rdx
       jne      SHORT G_M000_IG159
 
G_M000_IG158:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x2C8], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2C0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2C8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG166
 
G_M000_IG159:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG165
 
G_M000_IG160:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x330], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x330]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x2C0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG166
 
G_M000_IG161:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG165
 
G_M000_IG162:
       mov      rdi, rsi
       test     rbx, rbx
       je       SHORT G_M000_IG165
 
G_M000_IG163:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG165
 
G_M000_IG164:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG166
 
G_M000_IG165:
       mov      rdx, rbx
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG166:
       mov      gword ptr [rbp-0x500], rax
       mov      rdi, r13
       test     rdi, rdi
       je       SHORT G_M000_IG173
 
G_M000_IG167:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [rdi], rcx
       jne      SHORT G_M000_IG173
 
G_M000_IG168:
       test     r15, r15
       je       SHORT G_M000_IG173
 
G_M000_IG169:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG173
 
G_M000_IG170:
       test     rbx, rbx
       je       SHORT G_M000_IG173
 
G_M000_IG171:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG173
 
G_M000_IG172:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   xmm2, qword ptr [rbx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp-0x70], xmm0
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG264
 
G_M000_IG173:
       test     r13, r13
       je       G_M000_IG192
 
G_M000_IG174:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r13], rcx
       jne      G_M000_IG186
 
G_M000_IG175:
       mov      r8, qword ptr [r13+0x08]
       mov      qword ptr [rbp-0x348], r8
       test     r15, r15
       je       G_M000_IG192
 
G_M000_IG176:
       mov      qword ptr [rbp-0x4F0], rcx
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG184
 
G_M000_IG177:
       mov      r9, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x358], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x360], rdx
       lea      rdx, [rbp-0x360]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x360]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG180
       mov      rax, qword ptr [rbp-0x360]
       mov      qword ptr [rbp-0x3A8], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG178
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
       jmp      SHORT G_M000_IG179
 
G_M000_IG178:
       mov      rdi, qword ptr [rbp-0x4F0]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x3A8]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG179:
       jmp      G_M000_IG183
 
G_M000_IG180:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x370], xmm0
       lea      rdi, [rbp-0x370]
       mov      rsi, qword ptr [rbp-0x348]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG181:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x370]
       vmovdqu32 xmmword ptr [rbp-0x380], xmm0
 
G_M000_IG182:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3A0], xmm0
       lea      rdi, [rbp-0x3A0]
       mov      rsi, qword ptr [rbp-0x358]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x380]
       mov      rsi, qword ptr [rbp-0x378]
       mov      rdx, gword ptr [rbp-0x3A0]
       mov      rcx, qword ptr [rbp-0x398]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x390], rax
       mov      qword ptr [rbp-0x388], rdx
       mov      rdi, gword ptr [rbp-0x390]
       mov      rsi, qword ptr [rbp-0x388]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG183:
       jmp      G_M000_IG193
 
G_M000_IG184:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG192
 
G_M000_IG185:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x350], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x3B0], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3B0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x350]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG193
 
G_M000_IG186:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      qword ptr [r13], rdx
       jne      SHORT G_M000_IG192
 
G_M000_IG187:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x338], xmm0
       test     r15, r15
       je       SHORT G_M000_IG192
 
G_M000_IG188:
       cmp      qword ptr [r15], rdx
       jne      SHORT G_M000_IG190
 
G_M000_IG189:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x340], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x338]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x340]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG193
 
G_M000_IG190:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG192
 
G_M000_IG191:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x3B8], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x3B8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x338]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG193
 
G_M000_IG192:
       mov      rdx, r15
       mov      rsi, r13
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG193:
       test     rsi, rsi
       je       G_M000_IG216
 
G_M000_IG194:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG206
 
G_M000_IG195:
       mov      rcx, qword ptr [rsi+0x08]
       test     rbx, rbx
       je       G_M000_IG216
 
G_M000_IG196:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG204
 
G_M000_IG197:
       mov      rdx, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x3D8], rdx
       lea      rsi, [rcx+rdx]
       mov      qword ptr [rbp-0x3E0], rsi
       mov      rdi, rcx
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG200
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG198
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
       jmp      SHORT G_M000_IG199
 
G_M000_IG198:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x3E0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG199:
       jmp      G_M000_IG203
 
G_M000_IG200:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3F0], xmm0
       lea      rdi, [rbp-0x3F0]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG201:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x3F0]
       vmovdqu32 xmmword ptr [rbp-0x400], xmm0
 
G_M000_IG202:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x420], xmm0
       lea      rdi, [rbp-0x420]
       mov      rsi, qword ptr [rbp-0x3D8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x400]
       mov      rsi, qword ptr [rbp-0x3F8]
       mov      rdx, gword ptr [rbp-0x420]
       mov      rcx, qword ptr [rbp-0x418]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x410], rax
       mov      qword ptr [rbp-0x408], rdx
       mov      rdi, gword ptr [rbp-0x410]
       mov      rsi, qword ptr [rbp-0x408]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG203:
       jmp      G_M000_IG217
 
G_M000_IG204:
       mov      rdx, 0xD1FFAB1E
       mov      rax, rdx
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG216
 
G_M000_IG205:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x3D0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x428], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x428]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x3D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG217
 
G_M000_IG206:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rsi], rcx
       jne      G_M000_IG212
 
G_M000_IG207:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x3C0], xmm0
       test     rbx, rbx
       je       G_M000_IG216
 
G_M000_IG208:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG210
 
G_M000_IG209:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x3C8], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x3C0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x3C8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG217
 
G_M000_IG210:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG216
 
G_M000_IG211:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x430], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x430]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x3C0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG217
 
G_M000_IG212:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG216
 
G_M000_IG213:
       mov      rdi, rsi
       test     rbx, rbx
       je       SHORT G_M000_IG216
 
G_M000_IG214:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG216
 
G_M000_IG215:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG217
 
G_M000_IG216:
       mov      rdx, rbx
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG217:
       test     rsi, rsi
       je       G_M000_IG236
 
G_M000_IG218:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG230
 
G_M000_IG219:
       mov      rcx, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x448], rcx
       test     r15, r15
       je       G_M000_IG236
 
G_M000_IG220:
       mov      qword ptr [rbp-0x4F0], rax
       cmp      qword ptr [r15], rax
       jne      G_M000_IG228
 
G_M000_IG221:
       mov      r8, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x458], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x460], rdx
       lea      rdx, [rbp-0x460]
       mov      rdi, rcx
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x460]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG224
       mov      rax, qword ptr [rbp-0x460]
       mov      qword ptr [rbp-0x4A8], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG222
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
       jmp      SHORT G_M000_IG223
 
G_M000_IG222:
       mov      rdi, qword ptr [rbp-0x4F0]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x4A8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG223:
       jmp      G_M000_IG227
 
G_M000_IG224:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x470], xmm0
       lea      rdi, [rbp-0x470]
       mov      rsi, qword ptr [rbp-0x448]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG225:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x470]
       vmovdqu32 xmmword ptr [rbp-0x480], xmm0
 
G_M000_IG226:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x4A0], xmm0
       lea      rdi, [rbp-0x4A0]
       mov      rsi, qword ptr [rbp-0x458]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x480]
       mov      rsi, qword ptr [rbp-0x478]
       mov      rdx, gword ptr [rbp-0x4A0]
       mov      rcx, qword ptr [rbp-0x498]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x490], rax
       mov      qword ptr [rbp-0x488], rdx
       mov      rdi, gword ptr [rbp-0x490]
       mov      rsi, qword ptr [rbp-0x488]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG227:
       jmp      G_M000_IG237
 
G_M000_IG228:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      qword ptr [r15], rdx
       jne      G_M000_IG236
 
G_M000_IG229:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x450], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x4B0], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x4B0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x450]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG237
 
G_M000_IG230:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG236
 
G_M000_IG231:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x438], xmm0
       test     r15, r15
       je       SHORT G_M000_IG236
 
G_M000_IG232:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG234
 
G_M000_IG233:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x440], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x438]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x440]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG237
 
G_M000_IG234:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG236
 
G_M000_IG235:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x4B8], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x4B8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x438]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG237
 
G_M000_IG236:
       mov      rdx, r15
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG237:
       mov      gword ptr [rbp-0x508], rax
       xor      rdi, rdi
       mov      gword ptr [rbp-0x4D0], rdi
 
G_M000_IG238:
       mov      gword ptr [rbp-0x4C8], rdi
 
G_M000_IG239:
       mov      gword ptr [rbp-0x4C0], rdi
       test     rax, rax
       je       G_M000_IG248
 
G_M000_IG240:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG249
 
G_M000_IG241:
       mov      rdi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG247
 
G_M000_IG242:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG244
 
G_M000_IG243:
       mov      rsi, qword ptr [rbx+0x08]
       call     [RyuPy.Runtime.PyInt:AddI64(long,long):System.Object]
       mov      rdx, rax
       jmp      G_M000_IG261
 
G_M000_IG244:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG246
 
G_M000_IG245:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rdi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x4D0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x508]
       je       SHORT G_M000_IG248
       mov      rdx, gword ptr [rbp-0x4D0]
       jmp      G_M000_IG261
 
G_M000_IG246:
       mov      rax, gword ptr [rbp-0x508]
       jmp      SHORT G_M000_IG248
 
G_M000_IG247:
       mov      rax, gword ptr [rbp-0x508]
 
G_M000_IG248:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       jmp      G_M000_IG261
 
G_M000_IG249:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG257
 
G_M000_IG250:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG256
 
G_M000_IG251:
       cmp      qword ptr [rbx], rsi
       jne      SHORT G_M000_IG253
 
G_M000_IG252:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x4C8]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x508]
       je       SHORT G_M000_IG248
       mov      rdx, gword ptr [rbp-0x4C8]
       jmp      G_M000_IG261
 
G_M000_IG253:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG255
 
G_M000_IG254:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x4C0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x508]
       je       G_M000_IG248
       mov      rdx, gword ptr [rbp-0x4C0]
       jmp      SHORT G_M000_IG261
 
G_M000_IG255:
       mov      rax, gword ptr [rbp-0x508]
       jmp      G_M000_IG248
 
G_M000_IG256:
       mov      rax, gword ptr [rbp-0x508]
       jmp      G_M000_IG248
 
G_M000_IG257:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG248
 
G_M000_IG258:
       mov      rdi, rax
       test     rbx, rbx
       je       G_M000_IG248
 
G_M000_IG259:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG248
 
G_M000_IG260:
       mov      rsi, rbx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rdx, rax
 
G_M000_IG261:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x4D0], rcx
 
G_M000_IG262:
       mov      gword ptr [rbp-0x4C8], rcx
 
G_M000_IG263:
       mov      gword ptr [rbp-0x4C0], rcx
       mov      rcx, r15
       mov      edi, 22
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 23
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG264:
       mov      gword ptr [rbp-0x510], rax
       jmp      G_M000_IG11
 
G_M000_IG265:
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, qword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x4F8]
 
G_M000_IG266:
       add      rsp, 0x518
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
 
; Total bytes of code 7153

; Assembly listing for method RyuPy.Generated.Module:m0_tail_pairs(System.Object,System.Object,System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 9 inlinees with PGO data; 49 single block inlinees; 112 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 0x508
       lea      rbp, [rsp+0x530]
       xor      eax, eax
       mov      qword ptr [rbp-0x4A8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x4A0], xmm8
       mov      rax, -0x3F0
       vmovdqa32 xmmword ptr [rbp+rax-0xA0], xmm8
       vmovdqa32 xmmword ptr [rbp+rax-0x90], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x80], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0xA0], rax
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
       mov      gword ptr [rbp-0x4F8], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x500], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x4F8]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x500]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x4F8]
       mov      gword ptr [rbp-0x4D0], rax
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
       jmp      G_M000_IG215
 
G_M000_IG03:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      G_M000_IG247
 
G_M000_IG04:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG12
 
G_M000_IG05:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x130], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x130]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG13
 
G_M000_IG06:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r13], rax
       jne      SHORT G_M000_IG12
 
G_M000_IG07:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG12
 
G_M000_IG08:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG10
 
G_M000_IG09:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG13
 
G_M000_IG10:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG12
 
G_M000_IG11:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x138], xmm1
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x138]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0xD0]
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
       mov      qword ptr [rbp-0x158], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x160], rsi
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
       jl       G_M000_IG252
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG251
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x160]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG19:
       jmp      G_M000_IG21
 
G_M000_IG20:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x170], xmm0
       lea      rdi, [rbp-0x170]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x170]
       mov      gword ptr [rbp-0x530], rax
       mov      ecx, dword ptr [rbp-0x168]
       mov      dword ptr [rbp-0x4B4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x180], xmm0
       lea      rdi, [rbp-0x180]
       mov      rsi, qword ptr [rbp-0x158]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x530]
       mov      esi, dword ptr [rbp-0x4B4]
       mov      rdx, gword ptr [rbp-0x180]
       mov      rcx, qword ptr [rbp-0x178]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG21:
       jmp      G_M000_IG35
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG34
 
G_M000_IG23:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x188], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x188]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x150]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG35
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      G_M000_IG30
 
G_M000_IG25:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x140], xmm0
       test     r15, r15
       je       G_M000_IG34
 
G_M000_IG26:
       cmp      qword ptr [r15], rdx
       jne      SHORT G_M000_IG28
 
G_M000_IG27:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x148], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x140]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG35
 
G_M000_IG28:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG29:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x190]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x140]
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
       je       G_M000_IG54
 
G_M000_IG36:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG48
 
G_M000_IG37:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x1A8], rax
       test     rbx, rbx
       je       G_M000_IG54
 
G_M000_IG38:
       mov      qword ptr [rbp-0x4C8], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG46
 
G_M000_IG39:
       mov      r8, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x1B8], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x1C0], rdx
       lea      rdx, [rbp-0x1C0]
       mov      rdi, rax
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x1C0]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG42
       mov      rax, qword ptr [rbp-0x1C0]
       mov      qword ptr [rbp-0x208], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG40
       test     rdi, rdi
       jl       G_M000_IG252
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG251
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG41
 
G_M000_IG40:
       mov      rdi, qword ptr [rbp-0x4C8]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x208]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG41:
       jmp      G_M000_IG45
 
G_M000_IG42:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1D0], xmm0
       lea      rdi, [rbp-0x1D0]
       mov      rsi, qword ptr [rbp-0x1A8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG43:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1D0]
       vmovdqu32 xmmword ptr [rbp-0x1E0], xmm0
 
G_M000_IG44:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x200], xmm0
       lea      rdi, [rbp-0x200]
       mov      rsi, qword ptr [rbp-0x1B8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x1D8]
       mov      rdx, gword ptr [rbp-0x200]
       mov      rcx, qword ptr [rbp-0x1F8]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1F0], rax
       mov      qword ptr [rbp-0x1E8], rdx
       mov      rdi, gword ptr [rbp-0x1F0]
       mov      rsi, qword ptr [rbp-0x1E8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG45:
       jmp      G_M000_IG55
 
G_M000_IG46:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG54
 
G_M000_IG47:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x1B0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x210], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x210]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x1B0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG55
 
G_M000_IG48:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      SHORT G_M000_IG54
 
G_M000_IG49:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x198], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG54
 
G_M000_IG50:
       cmp      qword ptr [rbx], rdx
       jne      SHORT G_M000_IG52
 
G_M000_IG51:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x1A0], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x198]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x1A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG55
 
G_M000_IG52:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG54
 
G_M000_IG53:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x218], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x218]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG55
 
G_M000_IG54:
       mov      rdx, rbx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG55:
       test     rsi, rsi
       je       G_M000_IG78
 
G_M000_IG56:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG68
 
G_M000_IG57:
       mov      rax, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG78
 
G_M000_IG58:
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG66
 
G_M000_IG59:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x238], rdx
       lea      rsi, [rax+rdx]
       mov      qword ptr [rbp-0x240], rsi
       mov      rdi, rax
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG62
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG60
       test     rdi, rdi
       jl       G_M000_IG252
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG251
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG61
 
G_M000_IG60:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x240]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG61:
       jmp      G_M000_IG65
 
G_M000_IG62:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x250], xmm0
       lea      rdi, [rbp-0x250]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG63:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x250]
       vmovdqu32 xmmword ptr [rbp-0x260], xmm0
 
G_M000_IG64:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x280], xmm0
       lea      rdi, [rbp-0x280]
       mov      rsi, qword ptr [rbp-0x238]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x260]
       mov      rsi, qword ptr [rbp-0x258]
       mov      rdx, gword ptr [rbp-0x280]
       mov      rcx, qword ptr [rbp-0x278]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x270], rax
       mov      qword ptr [rbp-0x268], rdx
       mov      rdi, gword ptr [rbp-0x270]
       mov      rsi, qword ptr [rbp-0x268]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG65:
       jmp      G_M000_IG79
 
G_M000_IG66:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [r15], rcx
       jne      G_M000_IG78
 
G_M000_IG67:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x230], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x288], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x288]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x230]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG79
 
G_M000_IG68:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      G_M000_IG74
 
G_M000_IG69:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x220], xmm0
       test     r15, r15
       je       G_M000_IG78
 
G_M000_IG70:
       cmp      qword ptr [r15], rdx
       jne      SHORT G_M000_IG72
 
G_M000_IG71:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x228], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x220]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x228]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG79
 
G_M000_IG72:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG78
 
G_M000_IG73:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x290], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x290]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG79
 
G_M000_IG74:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      SHORT G_M000_IG78
 
G_M000_IG75:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG78
 
G_M000_IG76:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG78
 
G_M000_IG77:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rsi, rax
       jmp      SHORT G_M000_IG79
 
G_M000_IG78:
       mov      rdx, r15
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rsi, rax
 
G_M000_IG79:
       test     rsi, rsi
       je       G_M000_IG98
 
G_M000_IG80:
       mov      rax, qword ptr [rsi]
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG92
 
G_M000_IG81:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x2A8], rax
       test     rbx, rbx
       je       G_M000_IG98
 
G_M000_IG82:
       mov      qword ptr [rbp-0x4C8], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG90
 
G_M000_IG83:
       mov      r8, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x2B8], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x2C0], rdx
       lea      rdx, [rbp-0x2C0]
       mov      rdi, rax
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x2C0]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG86
       mov      rax, qword ptr [rbp-0x2C0]
       mov      qword ptr [rbp-0x308], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG84
       test     rdi, rdi
       jl       G_M000_IG252
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG251
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG85
 
G_M000_IG84:
       mov      rdi, qword ptr [rbp-0x4C8]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x308]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG85:
       jmp      G_M000_IG89
 
G_M000_IG86:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x2D0], xmm0
       lea      rdi, [rbp-0x2D0]
       mov      rsi, qword ptr [rbp-0x2A8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG87:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x2D0]
       vmovdqu32 xmmword ptr [rbp-0x2E0], xmm0
 
G_M000_IG88:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x300], xmm0
       lea      rdi, [rbp-0x300]
       mov      rsi, qword ptr [rbp-0x2B8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2E0]
       mov      rsi, qword ptr [rbp-0x2D8]
       mov      rdx, gword ptr [rbp-0x300]
       mov      rcx, qword ptr [rbp-0x2F8]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2F0], rax
       mov      qword ptr [rbp-0x2E8], rdx
       mov      rdi, gword ptr [rbp-0x2F0]
       mov      rsi, qword ptr [rbp-0x2E8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG89:
       jmp      G_M000_IG99
 
G_M000_IG90:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG98
 
G_M000_IG91:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x2B0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rax
       vmovsd   qword ptr [rbp-0x310], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x310]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x2B0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG99
 
G_M000_IG92:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      rax, rdx
       jne      SHORT G_M000_IG98
 
G_M000_IG93:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x298], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG98
 
G_M000_IG94:
       cmp      qword ptr [rbx], rdx
       jne      SHORT G_M000_IG96
 
G_M000_IG95:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x2A0], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x298]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x2A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG99
 
G_M000_IG96:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG98
 
G_M000_IG97:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x318], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x318]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x298]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG99
 
G_M000_IG98:
       mov      rdx, rbx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG99:
       test     rsi, rsi
       je       G_M000_IG122
 
G_M000_IG100:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG112
 
G_M000_IG101:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG122
 
G_M000_IG102:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG110
 
G_M000_IG103:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x338], rdx
       lea      rsi, [rcx+rdx]
       mov      qword ptr [rbp-0x340], rsi
       mov      rdi, rcx
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG106
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG104
       test     rdi, rdi
       jl       G_M000_IG252
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG251
       test     rdi, rdi
       jl       G_M000_IG252
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG105
 
G_M000_IG104:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x340]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG105:
       jmp      G_M000_IG109
 
G_M000_IG106:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x350], xmm0
       lea      rdi, [rbp-0x350]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG107:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x350]
       vmovdqu32 xmmword ptr [rbp-0x360], xmm0
 
G_M000_IG108:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x380], xmm0
       lea      rdi, [rbp-0x380]
       mov      rsi, qword ptr [rbp-0x338]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x360]
       mov      rsi, qword ptr [rbp-0x358]
       mov      rdx, gword ptr [rbp-0x380]
       mov      rcx, qword ptr [rbp-0x378]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x370], rax
       mov      qword ptr [rbp-0x368], rdx
       mov      rdi, gword ptr [rbp-0x370]
       mov      rsi, qword ptr [rbp-0x368]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG109:
       jmp      G_M000_IG123
 
G_M000_IG110:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r15], rdx
       jne      G_M000_IG122
 
G_M000_IG111:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x330], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x388], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x388]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x330]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG123
 
G_M000_IG112:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG118
 
G_M000_IG113:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x320], xmm0
       test     r15, r15
       je       G_M000_IG122
 
G_M000_IG114:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG116
 
G_M000_IG115:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x328], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x320]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x328]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG123
 
G_M000_IG116:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG122
 
G_M000_IG117:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x390], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x390]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x320]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG123
 
G_M000_IG118:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG122
 
G_M000_IG119:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG122
 
G_M000_IG120:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG122
 
G_M000_IG121:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG123
 
G_M000_IG122:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG123:
       mov      gword ptr [rbp-0x4D8], rax
       mov      rdi, r14
       test     rdi, rdi
       je       SHORT G_M000_IG130
 
G_M000_IG124:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       jne      SHORT G_M000_IG130
 
G_M000_IG125:
       test     rbx, rbx
       je       SHORT G_M000_IG130
 
G_M000_IG126:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG130
 
G_M000_IG127:
       test     r15, r15
       je       SHORT G_M000_IG130
 
G_M000_IG128:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG130
 
G_M000_IG129:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp-0x70], xmm0
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG195
 
G_M000_IG130:
       test     r14, r14
       je       G_M000_IG149
 
G_M000_IG131:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [r14], rcx
       jne      G_M000_IG143
 
G_M000_IG132:
       mov      r8, qword ptr [r14+0x08]
       mov      qword ptr [rbp-0x3A8], r8
       test     rbx, rbx
       je       G_M000_IG149
 
G_M000_IG133:
       mov      qword ptr [rbp-0x4C8], rcx
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG141
 
G_M000_IG134:
       mov      r9, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x3B8], r9
       xor      edx, edx
       mov      qword ptr [rbp-0x3C0], rdx
       lea      rdx, [rbp-0x3C0]
       mov      rdi, r8
       mov      rsi, r9
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x3C0]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG137
       mov      rax, qword ptr [rbp-0x3C0]
       mov      qword ptr [rbp-0x408], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG135
       test     rdi, rdi
       jl       G_M000_IG252
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG251
       test     rdi, rdi
       jl       G_M000_IG252
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG136
 
G_M000_IG135:
       mov      rdi, qword ptr [rbp-0x4C8]
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x408]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG136:
       jmp      G_M000_IG140
 
G_M000_IG137:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3D0], xmm0
       lea      rdi, [rbp-0x3D0]
       mov      rsi, qword ptr [rbp-0x3A8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG138:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x3D0]
       vmovdqu32 xmmword ptr [rbp-0x3E0], xmm0
 
G_M000_IG139:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x400], xmm0
       lea      rdi, [rbp-0x400]
       mov      rsi, qword ptr [rbp-0x3B8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3E0]
       mov      rsi, qword ptr [rbp-0x3D8]
       mov      rdx, gword ptr [rbp-0x400]
       mov      rcx, qword ptr [rbp-0x3F8]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3F0], rax
       mov      qword ptr [rbp-0x3E8], rdx
       mov      rdi, gword ptr [rbp-0x3F0]
       mov      rsi, qword ptr [rbp-0x3E8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG140:
       jmp      G_M000_IG150
 
G_M000_IG141:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG149
 
G_M000_IG142:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x3B0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x410], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x410]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x3B0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      G_M000_IG150
 
G_M000_IG143:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      qword ptr [r14], rdx
       jne      SHORT G_M000_IG149
 
G_M000_IG144:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x398], xmm0
       test     rbx, rbx
       je       SHORT G_M000_IG149
 
G_M000_IG145:
       cmp      qword ptr [rbx], rdx
       jne      SHORT G_M000_IG147
 
G_M000_IG146:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x3A0], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x398]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x3A0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG150
 
G_M000_IG147:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG149
 
G_M000_IG148:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x418], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x418]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x398]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rsi, rax
       jmp      SHORT G_M000_IG150
 
G_M000_IG149:
       mov      rdx, rbx
       mov      rsi, r14
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
 
G_M000_IG150:
       test     rsi, rsi
       je       G_M000_IG173
 
G_M000_IG151:
       mov      rcx, 0xD1FFAB1E
       mov      rax, rcx
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG163
 
G_M000_IG152:
       mov      rcx, qword ptr [rsi+0x08]
       test     r15, r15
       je       G_M000_IG173
 
G_M000_IG153:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG161
 
G_M000_IG154:
       mov      rdx, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x438], rdx
       lea      rsi, [rcx+rdx]
       mov      qword ptr [rbp-0x440], rsi
       mov      rdi, rcx
       xor      rdi, rsi
       mov      r8, rdx
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG157
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG155
       test     rdi, rdi
       jl       G_M000_IG252
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG251
       test     rdi, rdi
       jl       G_M000_IG252
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG156
 
G_M000_IG155:
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x440]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG156:
       jmp      G_M000_IG160
 
G_M000_IG157:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x450], xmm0
       lea      rdi, [rbp-0x450]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG158:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x450]
       vmovdqu32 xmmword ptr [rbp-0x460], xmm0
 
G_M000_IG159:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x480], xmm0
       lea      rdi, [rbp-0x480]
       mov      rsi, qword ptr [rbp-0x438]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x460]
       mov      rsi, qword ptr [rbp-0x458]
       mov      rdx, gword ptr [rbp-0x480]
       mov      rcx, qword ptr [rbp-0x478]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x470], rax
       mov      qword ptr [rbp-0x468], rdx
       mov      rdi, gword ptr [rbp-0x470]
       mov      rsi, qword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG160:
       jmp      G_M000_IG174
 
G_M000_IG161:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r15], rdx
       jne      G_M000_IG173
 
G_M000_IG162:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x430], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x488], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x488]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x430]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG174
 
G_M000_IG163:
       mov      rdx, 0xD1FFAB1E
       mov      rcx, rdx
       cmp      qword ptr [rsi], rcx
       jne      G_M000_IG169
 
G_M000_IG164:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x420], xmm0
       test     r15, r15
       je       G_M000_IG173
 
G_M000_IG165:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG167
 
G_M000_IG166:
       vmovsd   xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x428], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x420]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x428]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG174
 
G_M000_IG167:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG173
 
G_M000_IG168:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x490], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x490]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x420]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG174
 
G_M000_IG169:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG173
 
G_M000_IG170:
       mov      rdi, rsi
       test     r15, r15
       je       SHORT G_M000_IG173
 
G_M000_IG171:
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG173
 
G_M000_IG172:
       mov      rsi, r15
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG174
 
G_M000_IG173:
       mov      rdx, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG174:
       mov      gword ptr [rbp-0x4E0], rax
       xor      rdi, rdi
       mov      gword ptr [rbp-0x4A8], rdi
 
G_M000_IG175:
       mov      gword ptr [rbp-0x4A0], rdi
 
G_M000_IG176:
       mov      gword ptr [rbp-0x498], rdi
       test     rax, rax
       je       G_M000_IG191
 
G_M000_IG177:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG185
 
G_M000_IG178:
       mov      rdi, qword ptr [rax+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG184
 
G_M000_IG179:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG181
 
G_M000_IG180:
       mov      rsi, qword ptr [rbx+0x08]
       call     [RyuPy.Runtime.PyInt:MultiplyI64(long,long):System.Object]
       mov      rdx, rax
       jmp      G_M000_IG192
 
G_M000_IG181:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG183
 
G_M000_IG182:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rdi
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x4A8]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x4E0]
       je       G_M000_IG191
       mov      rdx, gword ptr [rbp-0x4A8]
       jmp      G_M000_IG192
 
G_M000_IG183:
       mov      rax, gword ptr [rbp-0x4E0]
       jmp      G_M000_IG191
 
G_M000_IG184:
       mov      rax, gword ptr [rbp-0x4E0]
       jmp      G_M000_IG191
 
G_M000_IG185:
       mov      rsi, 0xD1FFAB1E
       mov      r8, rsi
       cmp      qword ptr [rax], r8
       jne      SHORT G_M000_IG191
 
G_M000_IG186:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rbx, rbx
       mov      rax, gword ptr [rbp-0x4E0]
       je       SHORT G_M000_IG191
 
G_M000_IG187:
       cmp      qword ptr [rbx], r8
       jne      SHORT G_M000_IG189
 
G_M000_IG188:
       vmovsd   xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x4A0]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x4E0]
       je       SHORT G_M000_IG191
       mov      rdx, gword ptr [rbp-0x4A0]
       jmp      SHORT G_M000_IG192
 
G_M000_IG189:
       cmp      qword ptr [rbx], rcx
       jne      SHORT G_M000_IG191
 
G_M000_IG190:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbx+0x08]
       lea      rsi, [rbp-0x498]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x4E0]
       je       SHORT G_M000_IG191
       mov      rdx, gword ptr [rbp-0x498]
       jmp      SHORT G_M000_IG192
 
G_M000_IG191:
       mov      rdx, rbx
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG192:
       xor      rcx, rcx
       mov      gword ptr [rbp-0x4A8], rcx
 
G_M000_IG193:
       mov      gword ptr [rbp-0x4A0], rcx
 
G_M000_IG194:
       mov      gword ptr [rbp-0x498], rcx
       mov      rcx, r15
       mov      edi, 34
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, rbx
       mov      edi, 35
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, r15
       mov      edi, 36
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG195:
       mov      gword ptr [rbp-0x4E8], rax
 
G_M000_IG196:
       test     r13, r13
       je       G_M000_IG213
 
G_M000_IG197:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r13], rcx
       jne      G_M000_IG207
 
G_M000_IG198:
       mov      rsi, qword ptr [r13+0x08]
       test     r14, r14
       je       G_M000_IG213
 
G_M000_IG199:
       cmp      qword ptr [r14], rcx
       jne      G_M000_IG205
 
G_M000_IG200:
       mov      rdx, qword ptr [r14+0x08]
       mov      qword ptr [rbp-0x90], rdx
       mov      r8, rsi
       sub      r8, rdx
       mov      qword ptr [rbp-0x98], r8
       mov      rdi, rsi
       xor      rdi, rdx
       mov      r9, rsi
       xor      r9, r8
       and      rdi, r9
       jl       SHORT G_M000_IG203
       lea      rdi, [r8+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG201
       test     rdi, rdi
       jl       G_M000_IG252
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG251
       mov      rcx, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG202
 
G_M000_IG201:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      rdi, qword ptr [rbp-0x98]
       mov      qword ptr [rcx+0x08], rdi
 
G_M000_IG202:
       jmp      G_M000_IG204
 
G_M000_IG203:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xA8], xmm0
       lea      rdi, [rbp-0xA8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x520], rax
       mov      ecx, dword ptr [rbp-0xA0]
       mov      dword ptr [rbp-0x4AC], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       lea      rdi, [rbp-0xB8]
       mov      rsi, qword ptr [rbp-0x90]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x520]
       mov      esi, dword ptr [rbp-0x4AC]
       mov      rdx, gword ptr [rbp-0xB8]
       mov      rcx, qword ptr [rbp-0xB0]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rcx, rax
 
G_M000_IG204:
       mov      rax, rcx
       mov      gword ptr [rbp-0x508], rax
       jmp      G_M000_IG214
 
G_M000_IG205:
       mov      r8, 0xD1FFAB1E
       mov      rcx, r8
       cmp      qword ptr [r14], rcx
       jne      G_M000_IG213
 
G_M000_IG206:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xC0], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xC0]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x508], rax
       jmp      G_M000_IG214
 
G_M000_IG207:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, rdi
       cmp      qword ptr [r13], rdx
       jne      SHORT G_M000_IG213
 
G_M000_IG208:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   qword ptr [rbp-0x78], xmm0
       test     r14, r14
       je       SHORT G_M000_IG213
 
G_M000_IG209:
       cmp      qword ptr [r14], rdx
       jne      SHORT G_M000_IG211
 
G_M000_IG210:
       vmovsd   xmm1, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x80], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x78]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x508], rax
       jmp      SHORT G_M000_IG214
 
G_M000_IG211:
       cmp      qword ptr [r14], rcx
       jne      SHORT G_M000_IG213
 
G_M000_IG212:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0xC8], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x78]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x508], rax
       jmp      SHORT G_M000_IG214
 
G_M000_IG213:
       mov      rdx, r14
       mov      rsi, r13
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      gword ptr [rbp-0x508], rax
 
G_M000_IG214:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 3
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x4F0], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp-0x4D8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x4F0]
       lea      rdi, bword ptr [rax+0x18]
       mov      rsi, gword ptr [rbp-0x4E8]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x4F0]
       lea      rdi, bword ptr [rax+0x20]
       mov      rsi, gword ptr [rbp-0x508]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x510], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x518], rax
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x4F0]
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x510]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x518]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x510]
       mov      gword ptr [rbp-0x4D0], rax
       mov      rdi, qword ptr [rbp-0x40]
       add      rdi, 1
       jo       G_M000_IG252
       mov      rax, rdi
 
G_M000_IG215:
       mov      qword ptr [rbp-0x40], rax
       cmp      rax, r12
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG249
 
G_M000_IG216:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     r13, r13
       je       G_M000_IG225
 
G_M000_IG217:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [r13], rax
       jne      G_M000_IG225
 
G_M000_IG218:
       test     rbx, rbx
       je       G_M000_IG225
 
G_M000_IG219:
       cmp      qword ptr [rbx], rax
       jne      G_M000_IG225
 
G_M000_IG220:
       test     r15, r15
       je       G_M000_IG225
 
G_M000_IG221:
       cmp      qword ptr [r15], rax
       jne      G_M000_IG225
 
G_M000_IG222:
       test     r14, r14
       je       G_M000_IG225
 
G_M000_IG223:
       cmp      qword ptr [r14], rax
       jne      G_M000_IG225
 
G_M000_IG224:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmovsd   xmm3, qword ptr [r14+0x08]
       vunpcklpd xmm0, xmm0, xmm3
       vmovaps  xmm3, xmm1
       vunpcklpd xmm1, xmm3, xmm1
       vmovaps  xmm3, xmm2
       vunpcklpd xmm2, xmm3, xmm2
       vmulpd   xmm0, xmm1, xmm0
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmulpd   xmm0, xmm0, xmm1
       vaddpd   xmm0, xmm0, xmm2
       vmovaps  xmmword ptr [rbp-0x50], xmm0
       vmovaps  xmm1, xmm0
       vmovsd   qword ptr [rbp-0x58], xmm1
       mov      qword ptr [rbp-0x4C0], rax
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm1
       mov      gword ptr [rbp-0x4D8], rax
       vmovaps  xmm0, xmmword ptr [rbp-0x50]
       vunpckhpd xmm0, xmm0, xmm0
       vmovsd   qword ptr [rbp-0x60], xmm0
       mov      rdi, qword ptr [rbp-0x4C0]
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x60]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      gword ptr [rbp-0x4E8], rax
       jmp      G_M000_IG196
 
G_M000_IG225:
       mov      rdi, r13
       test     rdi, rdi
       je       SHORT G_M000_IG232
 
G_M000_IG226:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      SHORT G_M000_IG232
 
G_M000_IG227:
       test     rbx, rbx
       je       SHORT G_M000_IG232
 
G_M000_IG228:
       cmp      qword ptr [rbx], rax
       jne      SHORT G_M000_IG232
 
G_M000_IG229:
       test     r15, r15
       je       SHORT G_M000_IG232
 
G_M000_IG230:
       cmp      qword ptr [r15], rax
       jne      SHORT G_M000_IG232
 
G_M000_IG231:
       vmovsd   xmm0, qword ptr [r13+0x08]
       vmovsd   xmm1, qword ptr [rbx+0x08]
       vmovsd   xmm2, qword ptr [r15+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, xmm2
       vmovsd   qword ptr [rbp-0x68], xmm0
       mov      rdi, rax
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG123
 
G_M000_IG232:
       test     r13, r13
       je       G_M000_IG12
 
G_M000_IG233:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r13], rcx
       jne      G_M000_IG06
 
G_M000_IG234:
       mov      rsi, qword ptr [r13+0x08]
       test     rbx, rbx
       je       G_M000_IG12
 
G_M000_IG235:
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG04
 
G_M000_IG236:
       mov      rax, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0xE8], rax
       lea      rdi, [rbp-0x100]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rdi], r8
       mov      r8, qword ptr [rbp-0x100]
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
       jne      SHORT G_M000_IG239
       mov      rax, r8
       mov      qword ptr [rbp-0x128], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG237
       test     rdi, rdi
       jl       G_M000_IG252
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rcx, 262
       jae      G_M000_IG251
       mov      rsi, gword ptr [rdx+8*rcx+0x10]
       jmp      SHORT G_M000_IG238
 
G_M000_IG237:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x128]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG238:
       jmp      G_M000_IG242
 
G_M000_IG239:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xF8], xmm0
       lea      rdi, [rbp-0xF8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xF8]
       mov      gword ptr [rbp-0x528], rax
       mov      ecx, dword ptr [rbp-0xF0]
       mov      dword ptr [rbp-0x4B0], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x110], xmm0
       lea      rdi, [rbp-0x110]
       mov      rsi, qword ptr [rbp-0xE8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x110]
       mov      r9d, dword ptr [rbp-0x108]
       mov      rcx, gword ptr [rbp-0x528]
       mov      rsi, rcx
       or       rsi, rax
       jne      SHORT G_M000_IG243
 
G_M000_IG240:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x120], xmm0
       movsxd   rsi, dword ptr [rbp-0x4B0]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0x120]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x120]
       mov      esi, dword ptr [rbp-0x118]
 
G_M000_IG241:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG242:
       jmp      G_M000_IG13
 
G_M000_IG243:
       test     rcx, rcx
       je       SHORT G_M000_IG248
 
G_M000_IG244:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG245:
       test     rax, rax
       je       G_M000_IG03
 
G_M000_IG246:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG247:
       mov      edx, dword ptr [rbp-0x4B0]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG241
 
G_M000_IG248:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG245
 
G_M000_IG249:
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, qword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x4D0]
 
G_M000_IG250:
       add      rsp, 0x508
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG251:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG252:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 7088

