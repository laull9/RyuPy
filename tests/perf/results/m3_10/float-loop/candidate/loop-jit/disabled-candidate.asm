; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 6 inlinees with PGO data; 16 single block inlinees; 29 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 328
       lea      rbp, [rsp+0x170]
       xor      r15d, r15d
       mov      qword ptr [rbp-0x118], r15
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x110], zmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       mov      rbx, rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rax, 0xD1FFAB1E
       mov      r13, gword ptr [rax]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       jmp      G_M000_IG79
 
G_M000_IG03:
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r15, gword ptr [rdi]
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x138], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x158], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x68]
       mov      rsi, gword ptr [rbp-0x158]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x68]
       mov      rdx, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x140], rax
       mov      rdi, gword ptr [rbp-0x138]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, gword ptr [rbp-0x140]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x40], rax
       vxorps   xmm0, xmm0, xmm0
       test     r15, r15
       je       G_M000_IG12
 
G_M000_IG04:
       mov      rdi, qword ptr [r15]
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jne      G_M000_IG11
 
G_M000_IG05:
       mov      esi, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       jmp      SHORT G_M000_IG08
 
G_M000_IG06:
       mov      ecx, 1
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
 
G_M000_IG07:
       mov      rax, qword ptr [rbp-0x38]
       add      rax, 1
       jo       G_M000_IG83
       mov      esi, ecx
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x40]
 
G_M000_IG08:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG76
 
G_M000_IG09:
       mov      qword ptr [rbp-0x38], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       jbe      SHORT G_M000_IG13
 
G_M000_IG10:
       mov      dword ptr [rbp-0x4C], esi
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rax, qword ptr [rbp-0x38]
       mov      qword ptr [rdx+0x08], rax
       mov      gword ptr [rbp-0x160], rdx
       jmp      SHORT G_M000_IG14
 
G_M000_IG11:
       xor      esi, esi
       vmovsd   qword ptr [rbp-0x48], xmm0
       jmp      SHORT G_M000_IG08
 
G_M000_IG12:
       xor      esi, esi
       mov      dword ptr [rbp-0x4C], esi
       vmovsd   qword ptr [rbp-0x48], xmm0
       mov      esi, dword ptr [rbp-0x4C]
       jmp      SHORT G_M000_IG08
 
G_M000_IG13:
       test     rdi, rdi
       jl       G_M000_IG83
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG82
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       mov      dword ptr [rbp-0x4C], esi
       mov      gword ptr [rbp-0x160], rdx
 
G_M000_IG14:
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x160]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x148], rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp-0x150], rcx
       mov      edx, dword ptr [rbp-0x4C]
       test     edx, edx
       je       SHORT G_M000_IG20
 
G_M000_IG15:
       test     rax, rax
       je       SHORT G_M000_IG20
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG20
 
G_M000_IG17:
       test     rcx, rcx
       je       SHORT G_M000_IG20
 
G_M000_IG18:
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG20
 
G_M000_IG19:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmulsd   xmm0, xmm0, qword ptr [rcx+0x08]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rbp-0x48], xmm0
       xor      r15, r15
       mov      ecx, edx
       jmp      G_M000_IG07
 
G_M000_IG20:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     edx, edi
       je       SHORT G_M000_IG21
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [r15+0x08], xmm0
 
G_M000_IG21:
       mov      rax, gword ptr [rbp-0x148]
       test     rax, rax
       je       G_M000_IG49
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG42
 
G_M000_IG23:
       mov      rsi, qword ptr [rax+0x08]
       mov      rcx, gword ptr [rbp-0x150]
       test     rcx, rcx
       jne      SHORT G_M000_IG25
 
G_M000_IG24:
       jmp      G_M000_IG49
 
G_M000_IG25:
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG39
 
G_M000_IG26:
       mov      rax, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x88], rax
       lea      rcx, [rbp-0xA8]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rcx], r8
       mov      rcx, qword ptr [rbp-0xA8]
       mov      qword ptr [rbp-0xA0], rcx
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
       jne      SHORT G_M000_IG29
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG27
       test     rax, rax
       jl       G_M000_IG83
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG82
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG28
 
G_M000_IG27:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0xA0]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG28:
       jmp      G_M000_IG32
 
G_M000_IG29:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0x98], xmm1
       lea      rdi, [rbp-0x98]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x98]
       mov      gword ptr [rbp-0x168], rax
       mov      ecx, dword ptr [rbp-0x90]
       mov      dword ptr [rbp-0x12C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       lea      rdi, [rbp-0xB8]
       mov      rsi, qword ptr [rbp-0x88]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      r9d, dword ptr [rbp-0xB0]
       mov      rcx, gword ptr [rbp-0x168]
       test     rcx, rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG30:
       test     rax, rax
       jne      SHORT G_M000_IG33
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       movsxd   rsi, dword ptr [rbp-0x12C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xC8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xC8]
       mov      esi, dword ptr [rbp-0xC0]
 
G_M000_IG31:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG32:
       jmp      G_M000_IG50
 
G_M000_IG33:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG35
 
G_M000_IG34:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG35:
       test     rax, rax
       je       SHORT G_M000_IG38
 
G_M000_IG36:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG37:
       mov      edx, dword ptr [rbp-0x12C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG31
 
G_M000_IG38:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG37
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG41
 
G_M000_IG40:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x80], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0xD0], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xD0]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG50
 
G_M000_IG41:
       jmp      G_M000_IG49
 
G_M000_IG42:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      SHORT G_M000_IG49
 
G_M000_IG43:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       mov      rsi, gword ptr [rbp-0x150]
       test     rsi, rsi
       je       SHORT G_M000_IG49
 
G_M000_IG44:
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG46
 
G_M000_IG45:
       vmovsd   xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x78], xmm2
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x70]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x78]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG50
 
G_M000_IG46:
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG48
 
G_M000_IG47:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm2
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xD8]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG50
 
G_M000_IG48:
 
G_M000_IG49:
       mov      rdx, gword ptr [rbp-0x150]
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG50:
       test     r15, r15
       je       G_M000_IG71
 
G_M000_IG51:
       mov      rax, qword ptr [r15]
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      G_M000_IG61
 
G_M000_IG52:
       mov      rsi, qword ptr [r15+0x08]
       test     rdx, rdx
       je       G_M000_IG71
 
G_M000_IG53:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG59
 
G_M000_IG54:
       mov      r15, qword ptr [rdx+0x08]
       lea      rax, [rsi+r15]
       mov      qword ptr [rbp-0xF8], rax
       mov      rcx, rsi
       xor      rcx, rax
       mov      rdx, r15
       xor      rdx, rax
       and      rcx, rdx
       jl       SHORT G_M000_IG57
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG55
       test     rcx, rcx
       jl       G_M000_IG83
       mov      rdi, rcx
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG82
       mov      r15, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG56
 
G_M000_IG55:
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       mov      rdi, qword ptr [rbp-0xF8]
       mov      qword ptr [r15+0x08], rdi
 
G_M000_IG56:
       jmp      G_M000_IG58
 
G_M000_IG57:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x108], xmm0
       lea      rdi, [rbp-0x108]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x108]
       mov      gword ptr [rbp-0x170], rax
       mov      ecx, dword ptr [rbp-0x100]
       mov      dword ptr [rbp-0x130], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x118], xmm0
       lea      rdi, [rbp-0x118]
       mov      rsi, r15
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x170]
       mov      esi, dword ptr [rbp-0x130]
       mov      rdx, gword ptr [rbp-0x118]
       mov      rcx, qword ptr [rbp-0x110]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r15, rax
 
G_M000_IG58:
       jmp      G_M000_IG72
 
G_M000_IG59:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG71
 
G_M000_IG60:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x120], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x120]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG72
 
G_M000_IG61:
       mov      rcx, 0xD1FFAB1E
       cmp      rax, rcx
       jne      G_M000_IG67
 
G_M000_IG62:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm0
       test     rdx, rdx
       je       G_M000_IG71
 
G_M000_IG63:
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG65
 
G_M000_IG64:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      SHORT G_M000_IG72
 
G_M000_IG65:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG71
 
G_M000_IG66:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x128], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x128]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      SHORT G_M000_IG72
 
G_M000_IG67:
       mov      rdi, 0xD1FFAB1E
       cmp      rax, rdi
       jne      SHORT G_M000_IG71
 
G_M000_IG68:
       mov      rdi, r15
       test     rdx, rdx
       je       SHORT G_M000_IG71
 
G_M000_IG69:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG71
 
G_M000_IG70:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r15, rax
       jmp      SHORT G_M000_IG72
 
G_M000_IG71:
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
 
G_M000_IG72:
       test     r15, r15
       je       SHORT G_M000_IG75
 
G_M000_IG73:
       mov      rdi, qword ptr [r15]
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       je       G_M000_IG06
 
G_M000_IG74:
       xor      ecx, ecx
       xor      edi, edi
       mov      ecx, edi
       jmp      G_M000_IG07
 
G_M000_IG75:
       xor      edi, edi
       mov      ecx, edi
       jmp      G_M000_IG07
 
G_M000_IG76:
       test     r15, r15
       sete     dil
       movzx    rdi, dil
       test     esi, edi
       je       SHORT G_M000_IG78
 
G_M000_IG77:
       mov      rcx, 0xD1FFAB1E
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      r15, rax
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [r15+0x08], xmm0
 
G_M000_IG78:
       add      r12, 1
       jo       SHORT G_M000_IG83
 
G_M000_IG79:
       cmp      r12, r13
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       jne      G_M000_IG03
 
G_M000_IG80:
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r15
 
G_M000_IG81:
       add      rsp, 328
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG82:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG83:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2228

; Assembly listing for method RyuPy.Generated.Module:m0_repeated(System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 6 inlinees with PGO data; 24 single block inlinees; 30 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 408
       lea      rbp, [rsp+0x1C0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x140], zmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu  ymmword ptr [rbp-0x80], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x60], rax
       mov      rbx, rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r15, rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      rsi, 0xD1FFAB1E
       mov      r12, gword ptr [rsi]
       mov      rsi, r12
       mov      rdi, r13
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       lea      rdi, bword ptr [r14+0x08]
       mov      rsi, r13
       call     CORINFO_HELP_ASSIGN_REF
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x198], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, r12
       mov      rdx, r12
       lea      rdi, [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x198]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x60]
       mov      rsi, gword ptr [rbp-0x70]
       mov      rdx, gword ptr [rbp-0x68]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x170], rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, gword ptr [rbp-0x170]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       jmp      G_M000_IG16
 
G_M000_IG03:
       lea      rdi, [r13+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG05
 
G_M000_IG04:
       test     rdi, rdi
       jl       G_M000_IG87
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG86
       mov      rcx, gword ptr [rcx+8*rdi+0x10]
       mov      gword ptr [rbp-0x1A8], rcx
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      qword ptr [rcx+0x08], r13
       mov      gword ptr [rbp-0x1A8], rcx
 
G_M000_IG06:
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x1A8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x160], rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x168], rax
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x178], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x1A0], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp-0x160]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, r12
       mov      rdx, r12
       lea      rdi, [rbp-0x88]
       mov      rsi, gword ptr [rbp-0x1A0]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x78]
       mov      rsi, gword ptr [rbp-0x88]
       mov      rdx, gword ptr [rbp-0x80]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x180], rax
       mov      rdi, gword ptr [rbp-0x178]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x38], rax
       mov      rdi, gword ptr [rbp-0x180]
 
G_M000_IG07:
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x38]
       mov      qword ptr [rbp-0x48], rax
       vxorps   xmm0, xmm0, xmm0
       mov      rdx, gword ptr [rbp-0x168]
       test     rdx, rdx
       je       G_M000_IG20
 
G_M000_IG08:
       mov      rdi, qword ptr [rdx]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       jne      G_M000_IG19
 
G_M000_IG09:
       mov      r8d, 1
       mov      gword ptr [rbp-0x168], rdx
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm0
       jmp      SHORT G_M000_IG12
 
G_M000_IG10:
       mov      esi, 1
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm0
 
G_M000_IG11:
       mov      rdi, qword ptr [rbp-0x40]
       add      rdi, 1
       jo       G_M000_IG87
       mov      gword ptr [rbp-0x168], rax
       mov      rcx, rdi
       mov      r8d, esi
       mov      rax, qword ptr [rbp-0x48]
 
G_M000_IG12:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       jne      G_M000_IG21
 
G_M000_IG13:
       mov      rdx, gword ptr [rbp-0x168]
       test     rdx, rdx
       sete     dil
       movzx    rdi, dil
       test     r8d, edi
       mov      gword ptr [rbp-0x168], rdx
       je       SHORT G_M000_IG15
 
G_M000_IG14:
       mov      rsi, 0xD1FFAB1E
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rdx+0x08], xmm0
       mov      gword ptr [rbp-0x168], rdx
 
G_M000_IG15:
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x168]
       mov      rsi, r14
       mov      edi, 4
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       add      r13, 1
       jo       G_M000_IG87
       mov      rax, qword ptr [rbp-0x30]
 
G_M000_IG16:
       cmp      r13, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       jne      G_M000_IG03
 
G_M000_IG17:
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r14
 
G_M000_IG18:
       add      rsp, 408
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG19:
       xor      r8d, r8d
       mov      gword ptr [rbp-0x168], rdx
       vmovsd   qword ptr [rbp-0x50], xmm0
       jmp      G_M000_IG12
 
G_M000_IG20:
       xor      r8d, r8d
       mov      dword ptr [rbp-0x54], r8d
       mov      gword ptr [rbp-0x168], rdx
       vmovsd   qword ptr [rbp-0x50], xmm0
       mov      r8d, dword ptr [rbp-0x54]
       jmp      G_M000_IG12
 
G_M000_IG21:
       mov      qword ptr [rbp-0x40], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG23
 
G_M000_IG22:
       test     rdi, rdi
       jl       G_M000_IG87
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG86
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       mov      dword ptr [rbp-0x54], r8d
       mov      gword ptr [rbp-0x1B0], rsi
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:
       mov      dword ptr [rbp-0x54], r8d
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x40]
       mov      qword ptr [rsi+0x08], rax
       mov      gword ptr [rbp-0x1B0], rsi
 
G_M000_IG24:
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x160]
       mov      rsi, gword ptr [rbp-0x1B0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x188], rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp-0x190], rcx
       mov      edx, dword ptr [rbp-0x54]
       test     edx, edx
       je       SHORT G_M000_IG30
 
G_M000_IG25:
       test     rax, rax
       je       SHORT G_M000_IG30
 
G_M000_IG26:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG30
 
G_M000_IG27:
       test     rcx, rcx
       je       SHORT G_M000_IG30
 
G_M000_IG28:
       cmp      qword ptr [rcx], rsi
       jne      SHORT G_M000_IG30
 
G_M000_IG29:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmulsd   xmm0, xmm0, qword ptr [rcx+0x08]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rbp-0x50], xmm0
       xor      rsi, rsi
       mov      rax, rsi
       mov      esi, edx
       jmp      G_M000_IG11
 
G_M000_IG30:
       mov      rsi, gword ptr [rbp-0x168]
       test     rsi, rsi
       sete     dil
       movzx    rdi, dil
       test     edx, edi
       mov      gword ptr [rbp-0x168], rsi
       je       SHORT G_M000_IG31
       mov      rsi, 0xD1FFAB1E
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       vmovsd   xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rsi+0x08], xmm0
       mov      gword ptr [rbp-0x168], rsi
 
G_M000_IG31:
       mov      rcx, gword ptr [rbp-0x188]
       test     rcx, rcx
       je       G_M000_IG59
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG52
 
G_M000_IG33:
       mov      rsi, qword ptr [rcx+0x08]
       mov      r8, gword ptr [rbp-0x190]
       test     r8, r8
       jne      SHORT G_M000_IG35
 
G_M000_IG34:
       jmp      G_M000_IG59
 
G_M000_IG35:
       cmp      qword ptr [r8], rdi
       jne      G_M000_IG49
 
G_M000_IG36:
       mov      rcx, qword ptr [r8+0x08]
       mov      qword ptr [rbp-0xA8], rcx
       lea      r8, [rbp-0xC8]
       mov      rdx, rsi
       mulx     rdx, r9, rcx
       mov      qword ptr [r8], r9
       mov      r8, qword ptr [rbp-0xC8]
       mov      qword ptr [rbp-0xC0], r8
       mov      r9, rsi
       sar      r9, 63
       and      r9, rcx
       sub      rdx, r9
       mov      r9, rcx
       sar      r9, 63
       and      r9, rsi
       sub      rdx, r9
       mov      r9, r8
       sar      r9, 63
       cmp      rdx, r9
       jne      SHORT G_M000_IG39
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG37
       test     rcx, rcx
       jl       G_M000_IG87
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rsi, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG86
       mov      rdx, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG38
 
G_M000_IG37:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0xC0]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG38:
       jmp      G_M000_IG42
 
G_M000_IG39:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0xB8], xmm1
       lea      rdi, [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0x1B8], rax
       mov      ecx, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0x154], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, qword ptr [rbp-0xA8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD8]
       mov      r9d, dword ptr [rbp-0xD0]
       mov      rcx, gword ptr [rbp-0x1B8]
       test     rcx, rcx
       jne      SHORT G_M000_IG44
 
G_M000_IG40:
       test     rax, rax
       jne      SHORT G_M000_IG43
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xE8], xmm0
       movsxd   rsi, dword ptr [rbp-0x154]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xE8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xE8]
       mov      esi, dword ptr [rbp-0xE0]
 
G_M000_IG41:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG42:
       jmp      G_M000_IG60
 
G_M000_IG43:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG45
 
G_M000_IG44:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG45:
       test     rax, rax
       je       SHORT G_M000_IG48
 
G_M000_IG46:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG47:
       mov      edx, dword ptr [rbp-0x154]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG41
 
G_M000_IG48:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG47
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       jne      SHORT G_M000_IG51
 
G_M000_IG50:
       vmovsd   xmm1, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm1
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, rsi
       vmovsd   qword ptr [rbp-0xF0], xmm2
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xF0]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG60
 
G_M000_IG51:
       jmp      G_M000_IG59
 
G_M000_IG52:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      G_M000_IG59
 
G_M000_IG53:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rsi, gword ptr [rbp-0x190]
       test     rsi, rsi
       je       SHORT G_M000_IG59
 
G_M000_IG54:
       cmp      qword ptr [rsi], rdx
       jne      SHORT G_M000_IG56
 
G_M000_IG55:
       vmovsd   xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x90]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG60
 
G_M000_IG56:
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG58
 
G_M000_IG57:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xF8]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG60
 
G_M000_IG58:
 
G_M000_IG59:
       mov      rdx, gword ptr [rbp-0x190]
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG60:
       mov      rax, gword ptr [rbp-0x168]
       test     rax, rax
       je       G_M000_IG81
 
G_M000_IG61:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG71
 
G_M000_IG62:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG81
 
G_M000_IG63:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG69
 
G_M000_IG64:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x118], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x120], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG67
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG65
       test     rax, rax
       jl       G_M000_IG87
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG86
       mov      rax, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG66
 
G_M000_IG65:
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x120]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG66:
       jmp      G_M000_IG68
 
G_M000_IG67:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x130], xmm0
       lea      rdi, [rbp-0x130]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x130]
       mov      gword ptr [rbp-0x1C0], rax
       mov      ecx, dword ptr [rbp-0x128]
       mov      dword ptr [rbp-0x158], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       lea      rdi, [rbp-0x140]
       mov      rsi, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1C0]
       mov      esi, dword ptr [rbp-0x158]
       mov      rdx, gword ptr [rbp-0x140]
       mov      rcx, qword ptr [rbp-0x138]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG68:
       jmp      G_M000_IG82
 
G_M000_IG69:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG81
 
G_M000_IG70:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x148], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x148]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG82
 
G_M000_IG71:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG77
 
G_M000_IG72:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm0
       test     rdx, rdx
       je       G_M000_IG81
 
G_M000_IG73:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG75
 
G_M000_IG74:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG82
 
G_M000_IG75:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG81
 
G_M000_IG76:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x150]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG82
 
G_M000_IG77:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG81
 
G_M000_IG78:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG81
 
G_M000_IG79:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG81
 
G_M000_IG80:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG82
 
G_M000_IG81:
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG82:
       test     rax, rax
       je       SHORT G_M000_IG85
 
G_M000_IG83:
       mov      rdi, qword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       je       G_M000_IG10
 
G_M000_IG84:
       xor      esi, esi
       xor      edi, edi
       mov      esi, edi
       jmp      G_M000_IG11
 
G_M000_IG85:
       xor      edi, edi
       mov      esi, edi
       jmp      G_M000_IG11
 
G_M000_IG86:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG87:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2681

