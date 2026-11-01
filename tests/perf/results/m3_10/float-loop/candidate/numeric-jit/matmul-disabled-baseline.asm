; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 6 inlinees with PGO data; 23 single block inlinees; 31 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 392
       lea      rbp, [rsp+0x1B0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x130], zmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqa32 xmmword ptr [rbp-0xB0], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0xA0], rax
       mov      r14, rdi
       mov      rbx, rsi
       mov      r15, rdx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r13, rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x178], rax
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       mov      gword ptr [rbp-0x1B0], rcx
       mov      rsi, rcx
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       lea      rdi, bword ptr [r12+0x08]
       mov      rsi, gword ptr [rbp-0x178]
       call     CORINFO_HELP_ASSIGN_REF
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x40], rax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x158]
       mov      rsi, r12
       mov      edi, 7
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      rax, qword ptr [rbp-0x38]
       add      rax, 1
       jo       G_M000_IG82
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x40]
 
G_M000_IG04:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG79
 
G_M000_IG05:
       mov      qword ptr [rbp-0x38], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG07
 
G_M000_IG06:
       test     rdi, rdi
       jl       G_M000_IG82
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG81
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       mov      gword ptr [rbp-0x180], rdx
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rax, qword ptr [rbp-0x38]
       mov      qword ptr [rdx+0x08], rax
       mov      gword ptr [rbp-0x180], rdx
 
G_M000_IG08:
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x180]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x150], rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x188], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x190], rax
       mov      rsi, gword ptr [rbp-0x1B0]
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       mov      rax, gword ptr [rbp-0x188]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x190]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x188]
       mov      gword ptr [rbp-0x158], rax
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x48], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x48]
       mov      qword ptr [rbp-0x58], rax
 
G_M000_IG09:
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x168]
       mov      rsi, gword ptr [rbp-0x158]
       mov      edi, 8
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      rax, qword ptr [rbp-0x50]
       add      rax, 1
       jo       G_M000_IG82
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x58]
 
G_M000_IG11:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG03
 
G_M000_IG12:
       mov      qword ptr [rbp-0x50], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG14
 
G_M000_IG13:
       test     rdi, rdi
       jl       G_M000_IG82
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG81
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       mov      rcx, rsi
       jmp      SHORT G_M000_IG15
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x50]
       mov      qword ptr [rsi+0x08], rax
       mov      rcx, rsi
 
G_M000_IG15:
       mov      gword ptr [rbp-0x160], rcx
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x168], rax
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x60], rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x60]
       mov      qword ptr [rbp-0x70], rax
       jmp      G_M000_IG26
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rdx+0x08], rax
       mov      gword ptr [rbp-0x198], rdx
 
G_M000_IG17:
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x150]
       mov      rsi, gword ptr [rbp-0x198]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x170], rax
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x198]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x160]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp-0x170]
       test     rcx, rcx
       je       G_M000_IG30
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG30
 
G_M000_IG19:
       test     rax, rax
       je       G_M000_IG31
 
G_M000_IG20:
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG31
 
G_M000_IG21:
       mov      rdx, gword ptr [rbp-0x168]
       test     rdx, rdx
       je       G_M000_IG29
 
G_M000_IG22:
       cmp      qword ptr [rdx], rdi
       je       SHORT G_M000_IG24
 
G_M000_IG23:
       mov      gword ptr [rbp-0x168], rdx
       jmp      G_M000_IG31
 
G_M000_IG24:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x78], xmm0
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x78]
       vmovsd   qword ptr [rax+0x08], xmm0
 
G_M000_IG25:
       mov      gword ptr [rbp-0x168], rax
       mov      rdi, qword ptr [rbp-0x68]
       add      rdi, 1
       jo       G_M000_IG82
       mov      rcx, rdi
       mov      rax, qword ptr [rbp-0x70]
 
G_M000_IG26:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG10
 
G_M000_IG27:
       mov      qword ptr [rbp-0x68], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       G_M000_IG16
 
G_M000_IG28:
       test     rdi, rdi
       jl       G_M000_IG82
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG81
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       mov      gword ptr [rbp-0x198], rdx
       jmp      G_M000_IG17
 
G_M000_IG29:
       mov      gword ptr [rbp-0x168], rdx
       jmp      SHORT G_M000_IG31
 
G_M000_IG30:
       test     rcx, rcx
       je       G_M000_IG55
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG49
 
G_M000_IG32:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG55
 
G_M000_IG33:
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG47
 
G_M000_IG34:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x98], rax
       lea      rcx, [rbp-0xB8]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rcx], r8
       mov      rcx, qword ptr [rbp-0xB8]
       mov      qword ptr [rbp-0xB0], rcx
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
       jne      SHORT G_M000_IG37
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG35
       test     rax, rax
       jl       G_M000_IG82
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG81
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG36
 
G_M000_IG35:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0xB0]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG36:
       jmp      G_M000_IG40
 
G_M000_IG37:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xA8], xmm0
       lea      rdi, [rbp-0xA8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x1A0], rax
       mov      ecx, dword ptr [rbp-0xA0]
       mov      dword ptr [rbp-0x144], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       mov      rsi, qword ptr [rbp-0x98]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xC8]
       mov      r9d, dword ptr [rbp-0xC0]
       mov      rcx, gword ptr [rbp-0x1A0]
       test     rcx, rcx
       jne      SHORT G_M000_IG42
 
G_M000_IG38:
       test     rax, rax
       jne      SHORT G_M000_IG41
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       movsxd   rsi, dword ptr [rbp-0x144]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xD8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xD8]
       mov      esi, dword ptr [rbp-0xD0]
 
G_M000_IG39:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG40:
       jmp      G_M000_IG56
 
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
       mov      edx, dword ptr [rbp-0x144]
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
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG55
 
G_M000_IG48:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xE0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG56
 
G_M000_IG49:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      SHORT G_M000_IG55
 
G_M000_IG50:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x80], xmm0
       test     rax, rax
       je       SHORT G_M000_IG55
 
G_M000_IG51:
       cmp      qword ptr [rax], rdx
       jne      SHORT G_M000_IG53
 
G_M000_IG52:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x80]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG56
 
G_M000_IG53:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG55
 
G_M000_IG54:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG56
 
G_M000_IG55:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG56:
       mov      rax, gword ptr [rbp-0x168]
       test     rax, rax
       je       G_M000_IG77
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG67
 
G_M000_IG58:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG77
 
G_M000_IG59:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG65
 
G_M000_IG60:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x108], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x110], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG63
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG61
       test     rax, rax
       jl       G_M000_IG82
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG81
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG62
 
G_M000_IG61:
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x110]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG62:
       jmp      G_M000_IG64
 
G_M000_IG63:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x120], xmm0
       lea      rdi, [rbp-0x120]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x120]
       mov      gword ptr [rbp-0x1A8], rax
       mov      ecx, dword ptr [rbp-0x118]
       mov      dword ptr [rbp-0x148], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x130], xmm0
       lea      rdi, [rbp-0x130]
       mov      rsi, qword ptr [rbp-0x108]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1A8]
       mov      esi, dword ptr [rbp-0x148]
       mov      rdx, gword ptr [rbp-0x130]
       mov      rcx, qword ptr [rbp-0x128]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG64:
       jmp      G_M000_IG78
 
G_M000_IG65:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG77
 
G_M000_IG66:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x138], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x138]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG78
 
G_M000_IG67:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      SHORT G_M000_IG73
 
G_M000_IG68:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm0
       test     rdx, rdx
       je       G_M000_IG77
 
G_M000_IG69:
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG71
 
G_M000_IG70:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xF0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xF8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG78
 
G_M000_IG71:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG77
 
G_M000_IG72:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x140], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x140]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG78
 
G_M000_IG73:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG77
 
G_M000_IG74:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG77
 
G_M000_IG75:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG77
 
G_M000_IG76:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG78
 
G_M000_IG77:
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG78:
       jmp      G_M000_IG25
 
G_M000_IG79:
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r13
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r12
 
G_M000_IG80:
       add      rsp, 392
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG81:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG82:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2569

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_matmul(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
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
       call     [RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 47

