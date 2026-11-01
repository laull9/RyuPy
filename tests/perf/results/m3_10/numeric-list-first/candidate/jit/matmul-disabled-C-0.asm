; Assembly listing for method RyuPy.Generated.Module:m0_matmul(System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 6 inlinees with PGO data; 24 single block inlinees; 32 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 424
       lea      rbp, [rsp+0x1D0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x140], zmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqa32 xmmword ptr [rbp-0xC0], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0xB0], rax
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
       mov      gword ptr [rbp-0x198], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, rax
       call     [System.Collections.Generic.List`1[System.__Canon]:.ctor(System.Collections.Generic.IEnumerable`1[System.__Canon]):this]
       lea      rdi, bword ptr [r12+0x08]
       mov      rsi, gword ptr [rbp-0x198]
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
       mov      rcx, gword ptr [rbp-0x168]
       mov      rsi, r12
       mov      edi, 7
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       mov      rax, qword ptr [rbp-0x38]
       add      rax, 1
       jo       G_M000_IG98
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x40]
 
G_M000_IG04:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG94
 
G_M000_IG05:
       mov      qword ptr [rbp-0x38], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG07
 
G_M000_IG06:
       test     rdi, rdi
       jl       G_M000_IG98
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG97
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       mov      gword ptr [rbp-0x1A0], rdx
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rax, qword ptr [rbp-0x38]
       mov      qword ptr [rdx+0x08], rax
       mov      gword ptr [rbp-0x1A0], rdx
 
G_M000_IG08:
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       mov      rsi, gword ptr [rbp-0x1A0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x160], rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1A8], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x1B0], rax
       test     byte  ptr [(reloc)], 1
       je       G_M000_IG96
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, gword ptr [rdi]
       mov      rax, gword ptr [rbp-0x1B0]
       lea      rdi, bword ptr [rax+0x08]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x1A8]
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, gword ptr [rbp-0x1B0]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rax, gword ptr [rbp-0x1A8]
       mov      gword ptr [rbp-0x168], rax
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
       jmp      G_M000_IG91
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x50]
       mov      qword ptr [rsi+0x08], rax
       mov      rcx, rsi
 
G_M000_IG11:
       mov      gword ptr [rbp-0x170], rcx
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x178], rax
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
       vxorps   xmm0, xmm0, xmm0
       mov      rdx, gword ptr [rbp-0x178]
       test     rdx, rdx
       je       G_M000_IG20
 
G_M000_IG12:
       mov      rdi, qword ptr [rdx]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       jne      G_M000_IG19
 
G_M000_IG13:
       mov      r8d, 1
       mov      gword ptr [rbp-0x178], rdx
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x78], xmm0
       jmp      SHORT G_M000_IG16
 
G_M000_IG14:
       mov      ecx, 1
       mov      gword ptr [rbp-0x178], rax
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x78], xmm0
       mov      dword ptr [rbp-0x7C], ecx
       mov      rdx, gword ptr [rbp-0x178]
 
G_M000_IG15:
       mov      rax, qword ptr [rbp-0x68]
       add      rax, 1
       jo       G_M000_IG98
       mov      gword ptr [rbp-0x178], rdx
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x70]
       mov      r8d, dword ptr [rbp-0x7C]
 
G_M000_IG16:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       jne      SHORT G_M000_IG21
 
G_M000_IG17:
       mov      dword ptr [rbp-0x7C], r8d
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x178]
       test     rax, rax
       sete     dil
       movzx    rdi, dil
       test     dword ptr [rbp-0x7C], edi
       jne      G_M000_IG89
 
G_M000_IG18:
       mov      rcx, rax
       mov      rsi, gword ptr [rbp-0x168]
       mov      edi, 10
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       jmp      G_M000_IG90
 
G_M000_IG19:
       xor      r8d, r8d
       mov      gword ptr [rbp-0x178], rdx
       vmovsd   qword ptr [rbp-0x78], xmm0
       jmp      SHORT G_M000_IG16
 
G_M000_IG20:
       xor      r8d, r8d
       mov      dword ptr [rbp-0x7C], r8d
       mov      gword ptr [rbp-0x178], rdx
       vmovsd   qword ptr [rbp-0x78], xmm0
       mov      r8d, dword ptr [rbp-0x7C]
       jmp      G_M000_IG16
 
G_M000_IG21:
       mov      qword ptr [rbp-0x68], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG23
 
G_M000_IG22:
       test     rdi, rdi
       jl       G_M000_IG98
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG97
       mov      rsi, gword ptr [rsi+8*rdi+0x10]
       mov      gword ptr [rbp-0x1B8], rsi
       mov      dword ptr [rbp-0x7C], r8d
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:
       mov      dword ptr [rbp-0x7C], r8d
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rsi+0x08], rax
       mov      gword ptr [rbp-0x1B8], rsi
 
G_M000_IG24:
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       cmp      dword ptr [rbp-0x7C], 0
       je       G_M000_IG26
 
G_M000_IG25:
       mov      rdi, gword ptr [rbp-0x160]
       mov      rsi, gword ptr [rbp-0x1B8]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       test     eax, eax
       je       SHORT G_M000_IG26
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x1B8]
       call     [RyuPy.Runtime.PyNumericListOps:PeekList(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x190], rax
       test     rax, rax
       je       SHORT G_M000_IG26
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x170]
       call     [RyuPy.Runtime.PyNumericListOps:CanReadF64(System.Object,System.Object):bool]
       test     eax, eax
       je       SHORT G_M000_IG26
       mov      rdi, gword ptr [rbp-0x160]
       mov      rsi, gword ptr [rbp-0x1B8]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      rdi, gword ptr [rbp-0x190]
       mov      rsi, gword ptr [rbp-0x170]
       call     [RyuPy.Runtime.PyNumericListOps:ReadF64(System.Object,System.Object):double]
       jmp      G_M000_IG31
 
G_M000_IG26:
       mov      rdi, gword ptr [rbp-0x160]
       mov      rsi, gword ptr [rbp-0x1B8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x180], rax
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x1B8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rax
       mov      rsi, gword ptr [rbp-0x170]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x188], rax
       mov      ecx, dword ptr [rbp-0x7C]
       test     ecx, ecx
       je       SHORT G_M000_IG32
       mov      rdx, gword ptr [rbp-0x180]
       test     rdx, rdx
       je       SHORT G_M000_IG33
 
G_M000_IG27:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG33
 
G_M000_IG28:
       test     rax, rax
       je       SHORT G_M000_IG33
 
G_M000_IG29:
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG33
 
G_M000_IG30:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   xmm1, qword ptr [rax+0x08]
       mov      dword ptr [rbp-0x7C], ecx
       vmovsd   qword ptr [rbp-0x88], xmm0
       vmovaps  xmm0, xmm1
 
G_M000_IG31:
       vmovsd   xmm1, qword ptr [rbp-0x88]
       vmulsd   xmm0, xmm1, xmm0
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x78]
       vmovsd   qword ptr [rbp-0x78], xmm0
       xor      rdx, rdx
       jmp      G_M000_IG15
 
G_M000_IG32:
       mov      rdx, gword ptr [rbp-0x180]
 
G_M000_IG33:
       mov      r8, gword ptr [rbp-0x178]
       test     r8, r8
       sete     sil
       movzx    rsi, sil
       test     ecx, esi
       mov      gword ptr [rbp-0x178], r8
       je       SHORT G_M000_IG34
       mov      rsi, 0xD1FFAB1E
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      r8, rax
       vmovsd   xmm0, qword ptr [rbp-0x78]
       vmovsd   qword ptr [r8+0x08], xmm0
       mov      gword ptr [rbp-0x178], r8
 
G_M000_IG34:
       mov      rcx, gword ptr [rbp-0x180]
       test     rcx, rcx
       je       G_M000_IG62
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG55
 
G_M000_IG36:
       mov      rsi, qword ptr [rcx+0x08]
       mov      r8, gword ptr [rbp-0x188]
       test     r8, r8
       jne      SHORT G_M000_IG38
 
G_M000_IG37:
       jmp      G_M000_IG62
 
G_M000_IG38:
       cmp      qword ptr [r8], rdi
       jne      G_M000_IG52
 
G_M000_IG39:
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
       jne      SHORT G_M000_IG42
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG40
       test     rcx, rcx
       jl       G_M000_IG98
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rsi, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG97
       mov      rdx, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG41
 
G_M000_IG40:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0xC0]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG41:
       jmp      G_M000_IG45
 
G_M000_IG42:
       vxorps   xmm1, xmm1, xmm1
       vmovdqu  xmmword ptr [rbp-0xB8], xmm1
       lea      rdi, [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0x1C0], rax
       mov      ecx, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0x154], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xD8], xmm0
       lea      rdi, [rbp-0xD8]
       mov      rsi, qword ptr [rbp-0xA8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD8]
       mov      r9d, dword ptr [rbp-0xD0]
       mov      rcx, gword ptr [rbp-0x1C0]
       test     rcx, rcx
       jne      SHORT G_M000_IG47
 
G_M000_IG43:
       test     rax, rax
       jne      SHORT G_M000_IG46
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xE8], xmm0
       movsxd   rsi, dword ptr [rbp-0x154]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xE8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xE8]
       mov      esi, dword ptr [rbp-0xE0]
 
G_M000_IG44:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG45:
       jmp      G_M000_IG63
 
G_M000_IG46:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG48
 
G_M000_IG47:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG48:
       test     rax, rax
       je       SHORT G_M000_IG51
 
G_M000_IG49:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG50:
       mov      edx, dword ptr [rbp-0x154]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG44
 
G_M000_IG51:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG50
 
G_M000_IG52:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       jne      SHORT G_M000_IG54
 
G_M000_IG53:
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
       jmp      G_M000_IG63
 
G_M000_IG54:
       jmp      G_M000_IG62
 
G_M000_IG55:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      G_M000_IG62
 
G_M000_IG56:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rsi, gword ptr [rbp-0x188]
       test     rsi, rsi
       je       SHORT G_M000_IG62
 
G_M000_IG57:
       cmp      qword ptr [rsi], rdx
       jne      SHORT G_M000_IG59
 
G_M000_IG58:
       vmovsd   xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x90]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG63
 
G_M000_IG59:
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG61
 
G_M000_IG60:
       vxorps   xmm2, xmm2, xmm2
       vcvtsi2sd xmm2, xmm2, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm2
       mov      rdi, rdx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm2, qword ptr [rbp-0xF8]
       vmulsd   xmm0, xmm2, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG63
 
G_M000_IG61:
 
G_M000_IG62:
       mov      rdx, gword ptr [rbp-0x188]
       mov      rsi, rcx
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG63:
       mov      rax, gword ptr [rbp-0x178]
       test     rax, rax
       je       G_M000_IG84
 
G_M000_IG64:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG74
 
G_M000_IG65:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG84
 
G_M000_IG66:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG72
 
G_M000_IG67:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x118], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x120], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG70
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG68
       test     rax, rax
       jl       G_M000_IG98
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG97
       mov      rax, gword ptr [rsi+8*rdi+0x10]
       jmp      SHORT G_M000_IG69
 
G_M000_IG68:
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x120]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG69:
       jmp      G_M000_IG71
 
G_M000_IG70:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x130], xmm0
       lea      rdi, [rbp-0x130]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x130]
       mov      gword ptr [rbp-0x1C8], rax
       mov      ecx, dword ptr [rbp-0x128]
       mov      dword ptr [rbp-0x158], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x140], xmm0
       lea      rdi, [rbp-0x140]
       mov      rsi, qword ptr [rbp-0x118]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1C8]
       mov      esi, dword ptr [rbp-0x158]
       mov      rdx, gword ptr [rbp-0x140]
       mov      rcx, qword ptr [rbp-0x138]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG71:
       jmp      G_M000_IG85
 
G_M000_IG72:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG84
 
G_M000_IG73:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x110], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x148], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x148]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x110]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG85
 
G_M000_IG74:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG80
 
G_M000_IG75:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm0
       test     rdx, rdx
       je       G_M000_IG84
 
G_M000_IG76:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG78
 
G_M000_IG77:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x100]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG85
 
G_M000_IG78:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG84
 
G_M000_IG79:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x150], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x150]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG85
 
G_M000_IG80:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG84
 
G_M000_IG81:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG84
 
G_M000_IG82:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG84
 
G_M000_IG83:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG85
 
G_M000_IG84:
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG85:
       test     rax, rax
       je       SHORT G_M000_IG88
 
G_M000_IG86:
       mov      rdi, qword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       cmp      rdi, rsi
       je       G_M000_IG14
 
G_M000_IG87:
       xor      ecx, ecx
       mov      dword ptr [rbp-0x7C], ecx
       mov      rdx, rax
       jmp      G_M000_IG15
 
G_M000_IG88:
       xor      ecx, ecx
       mov      dword ptr [rbp-0x7C], ecx
       mov      rdx, rax
       jmp      G_M000_IG15
 
G_M000_IG89:
       mov      rdi, gword ptr [rbp-0x168]
       vmovsd   xmm0, qword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyNumericListOps:AppendF64(System.Object,double):System.Object]
 
G_M000_IG90:
       mov      rax, qword ptr [rbp-0x50]
       add      rax, 1
       jo       G_M000_IG98
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x58]
 
G_M000_IG91:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG03
 
G_M000_IG92:
       mov      qword ptr [rbp-0x50], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       G_M000_IG10
 
G_M000_IG93:
       test     rdi, rdi
       jl       SHORT G_M000_IG98
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      SHORT G_M000_IG97
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       mov      rcx, rsi
       jmp      G_M000_IG11
 
G_M000_IG94:
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r13
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r12
 
G_M000_IG95:
       add      rsp, 424
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG96:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_GET_GCSTATIC_BASE
       jmp      G_M000_IG09
 
G_M000_IG97:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG98:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3059

