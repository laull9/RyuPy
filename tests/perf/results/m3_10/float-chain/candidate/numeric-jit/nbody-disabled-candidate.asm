; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 28 single block inlinees; 86 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 0x498
       lea      rbp, [rsp+0x4C0]
       xor      eax, eax
       mov      qword ptr [rbp-0x368], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqa32 xmmword ptr [rbp-0x360], xmm8
       mov      rax, -672
       vmovdqa32 xmmword ptr [rbp+rax-0xB0], xmm8
       vmovdqa32 xmmword ptr [rbp+rax-0xA0], xmm8
       vmovdqa32 xmmword ptr [rbp+rax-0x90], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      r15, rdi
       mov      rbx, rsi
       mov      r14, rdx
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r13, rax
       mov      edi, 79
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x10]
       mov      rsi, r15
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0xC8]
       mov      rsi, r12
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0xB8]
       mov      rsi, gword ptr [rbp-0xC8]
       mov      rdx, gword ptr [rbp-0xC0]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      r12, rax
       mov      edi, 80
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, r14
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r14, rax
       mov      rax, qword ptr [rbp-0x30]
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      rax, qword ptr [rbp-0x38]
       add      rax, 1
       jo       G_M000_IG317
 
G_M000_IG04:
       mov      qword ptr [rbp-0x38], rax
       cmp      rax, r14
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG314
 
G_M000_IG05:
       mov      edi, 81
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x40], rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x40]
       mov      qword ptr [rbp-0x50], rax
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rax, qword ptr [rbp-0x48]
       add      rax, 1
       jo       G_M000_IG317
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x50]
 
G_M000_IG07:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG235
 
G_M000_IG08:
       mov      qword ptr [rbp-0x48], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG10
 
G_M000_IG09:
       test     rdi, rdi
       jl       G_M000_IG317
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG316
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       mov      rcx, rsi
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:
       mov      r8, 0xD1FFAB1E
       mov      rdi, r8
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rax, qword ptr [rbp-0x48]
       mov      qword ptr [rsi+0x08], rax
       mov      rcx, rsi
 
G_M000_IG11:
       mov      gword ptr [rbp-0x390], rcx
       mov      edi, 82
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x390]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x398], rax
       mov      edi, 83
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      rax, gword ptr [rbp-0x390]
       test     rax, rax
       je       G_M000_IG32
 
G_M000_IG12:
       mov      rcx, qword ptr [rax]
       mov      r8, 0xD1FFAB1E
       mov      rsi, r8
       cmp      rcx, rsi
       jne      G_M000_IG22
 
G_M000_IG13:
       mov      rcx, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG32
 
G_M000_IG14:
       cmp      qword ptr [rdx], rsi
       jne      G_M000_IG20
 
G_M000_IG15:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0xE8], rax
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0xF0], rdx
       mov      rdi, rcx
       xor      rdi, rdx
       mov      r8, rax
       xor      r8, rdx
       and      rdi, r8
       jl       SHORT G_M000_IG18
       lea      rdi, [rdx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG16
       test     rdi, rdi
       jl       G_M000_IG317
       mov      rsi, 0xD1FFAB1E
       mov      rdx, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG316
       mov      rdi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG17
 
G_M000_IG16:
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, rax
       mov      rsi, qword ptr [rbp-0xF0]
       mov      qword ptr [rdi+0x08], rsi
 
G_M000_IG17:
       jmp      G_M000_IG19
 
G_M000_IG18:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x100], xmm0
       lea      rdi, [rbp-0x100]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x100]
       mov      gword ptr [rbp-0x4A0], rax
       mov      ecx, dword ptr [rbp-0xF8]
       mov      dword ptr [rbp-0x36C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x110], xmm0
       lea      rdi, [rbp-0x110]
       mov      rsi, qword ptr [rbp-0xE8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x4A0]
       mov      esi, dword ptr [rbp-0x36C]
       mov      rdx, gword ptr [rbp-0x110]
       mov      rcx, qword ptr [rbp-0x108]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdi, rax
 
G_M000_IG19:
       jmp      G_M000_IG33
 
G_M000_IG20:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG32
 
G_M000_IG21:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0x118], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x118]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       jmp      G_M000_IG33
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG28
 
G_M000_IG23:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm0
       test     rdx, rdx
       je       G_M000_IG32
 
G_M000_IG24:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG26
 
G_M000_IG25:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       jmp      SHORT G_M000_IG33
 
G_M000_IG26:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG32
 
G_M000_IG27:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x120], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x120]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xD0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       jmp      SHORT G_M000_IG33
 
G_M000_IG28:
       mov      r8, 0xD1FFAB1E
       cmp      rcx, r8
       jne      SHORT G_M000_IG32
 
G_M000_IG29:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG32
 
G_M000_IG30:
       cmp      qword ptr [rdx], r8
       jne      SHORT G_M000_IG32
 
G_M000_IG31:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rdi, rax
       jmp      SHORT G_M000_IG33
 
G_M000_IG32:
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdi, rax
 
G_M000_IG33:
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x58], rax
       mov      rdi, r12
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0x68], rax
       jmp      G_M000_IG232
 
G_M000_IG34:
       mov      rsi, 0xD1FFAB1E
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rax, qword ptr [rbp-0x60]
       mov      qword ptr [rdx+0x08], rax
       mov      gword ptr [rbp-0x498], rdx
 
G_M000_IG35:
       mov      edi, 84
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x498]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3A0], rax
       mov      edi, 85
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x398]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x428], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x3A0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp-0x428]
       test     rcx, rcx
       je       G_M000_IG52
 
G_M000_IG36:
       mov      rdx, qword ptr [rcx]
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
       jne      G_M000_IG46
 
G_M000_IG37:
       mov      rdx, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG52
 
G_M000_IG38:
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG44
 
G_M000_IG39:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x140], rax
       mov      rcx, rdx
       sub      rcx, rax
       mov      qword ptr [rbp-0x148], rcx
       mov      rdi, rdx
       xor      rdi, rax
       mov      r8, rdx
       xor      r8, rcx
       and      rdi, r8
       jl       SHORT G_M000_IG42
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG40
       test     rdi, rdi
       jl       G_M000_IG317
       mov      rsi, 0xD1FFAB1E
       mov      rdx, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG316
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG41
 
G_M000_IG40:
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x148]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG41:
       jmp      G_M000_IG43
 
G_M000_IG42:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x158], xmm0
       lea      rdi, [rbp-0x158]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x158]
       mov      gword ptr [rbp-0x4A8], rax
       mov      ecx, dword ptr [rbp-0x150]
       mov      dword ptr [rbp-0x370], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       lea      rdi, [rbp-0x168]
       mov      rsi, qword ptr [rbp-0x140]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x4A8]
       mov      esi, dword ptr [rbp-0x370]
       mov      rdx, gword ptr [rbp-0x168]
       mov      rcx, qword ptr [rbp-0x160]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG43:
       mov      rax, rsi
       jmp      G_M000_IG53
 
G_M000_IG44:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG52
 
G_M000_IG45:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x138], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x170], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x170]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0x138]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG53
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      SHORT G_M000_IG52
 
G_M000_IG47:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x128], xmm0
       test     rax, rax
       je       SHORT G_M000_IG52
 
G_M000_IG48:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG50
 
G_M000_IG49:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x130], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x128]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x130]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG53
 
G_M000_IG50:
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG52
 
G_M000_IG51:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x178], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x128]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x178]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG53
 
G_M000_IG52:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG53:
       mov      gword ptr [rbp-0x3A8], rax
       mov      edi, 86
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x398]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x430], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x3A0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp-0x430]
       test     rcx, rcx
       je       G_M000_IG70
 
G_M000_IG54:
       mov      rdx, qword ptr [rcx]
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
       jne      G_M000_IG64
 
G_M000_IG55:
       mov      rdx, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG70
 
G_M000_IG56:
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG62
 
G_M000_IG57:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x198], rax
       mov      rcx, rdx
       sub      rcx, rax
       mov      qword ptr [rbp-0x1A0], rcx
       mov      rdi, rdx
       xor      rdi, rax
       mov      r8, rdx
       xor      r8, rcx
       and      rdi, r8
       jl       SHORT G_M000_IG60
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG58
       test     rdi, rdi
       jl       G_M000_IG317
       mov      rsi, 0xD1FFAB1E
       mov      rdx, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG316
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG59
 
G_M000_IG58:
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x1A0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG59:
       jmp      G_M000_IG61
 
G_M000_IG60:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1B0], xmm0
       lea      rdi, [rbp-0x1B0]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x1B0]
       mov      gword ptr [rbp-0x4B0], rax
       mov      ecx, dword ptr [rbp-0x1A8]
       mov      dword ptr [rbp-0x374], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1C0], xmm0
       lea      rdi, [rbp-0x1C0]
       mov      rsi, qword ptr [rbp-0x198]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x4B0]
       mov      esi, dword ptr [rbp-0x374]
       mov      rdx, gword ptr [rbp-0x1C0]
       mov      rcx, qword ptr [rbp-0x1B8]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG61:
       mov      rax, rsi
       jmp      G_M000_IG71
 
G_M000_IG62:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG70
 
G_M000_IG63:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x190], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x1C8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x1C8]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0x190]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG71
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      SHORT G_M000_IG70
 
G_M000_IG65:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x180], xmm0
       test     rax, rax
       je       SHORT G_M000_IG70
 
G_M000_IG66:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG68
 
G_M000_IG67:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x188], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x188]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG71
 
G_M000_IG68:
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG70
 
G_M000_IG69:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1D0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x180]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x1D0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG71
 
G_M000_IG70:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG71:
       mov      gword ptr [rbp-0x3B0], rax
       mov      edi, 87
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x398]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x438], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x3A0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp-0x438]
       test     rcx, rcx
       je       G_M000_IG93
 
G_M000_IG72:
       mov      rdx, qword ptr [rcx]
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
       jne      G_M000_IG84
 
G_M000_IG73:
       mov      rdx, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG93
 
G_M000_IG74:
       cmp      qword ptr [rax], rsi
       jne      G_M000_IG82
 
G_M000_IG75:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x1F0], rax
       mov      rcx, rdx
       sub      rcx, rax
       mov      qword ptr [rbp-0x1F8], rcx
       mov      rdi, rdx
       xor      rdi, rax
       mov      r8, rdx
       xor      r8, rcx
       and      rdi, r8
       jl       SHORT G_M000_IG78
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG76
       test     rdi, rdi
       jl       G_M000_IG317
       mov      rsi, 0xD1FFAB1E
       mov      rdx, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG316
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x1F8]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG77:
       jmp      G_M000_IG81
 
G_M000_IG78:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x208], xmm0
       lea      rdi, [rbp-0x208]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG79:
       vmovdqu  xmm0, xmmword ptr [rbp-0x208]
       vmovdqu  xmmword ptr [rbp-0x218], xmm0
 
G_M000_IG80:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x238], xmm0
       lea      rdi, [rbp-0x238]
       mov      rsi, qword ptr [rbp-0x1F0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x218]
       mov      rsi, qword ptr [rbp-0x210]
       mov      rdx, gword ptr [rbp-0x238]
       mov      rcx, qword ptr [rbp-0x230]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x228], rax
       mov      qword ptr [rbp-0x220], rdx
       mov      rdi, gword ptr [rbp-0x228]
       mov      rsi, qword ptr [rbp-0x220]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG81:
       mov      rax, rdx
       jmp      G_M000_IG94
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG93
 
G_M000_IG83:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1E8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x240], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x240]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0x1E8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG94
 
G_M000_IG84:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      SHORT G_M000_IG90
 
G_M000_IG85:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x1D8], xmm0
       test     rax, rax
       je       SHORT G_M000_IG93
 
G_M000_IG86:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG88
 
G_M000_IG87:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1E0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1D8]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x1E0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG94
 
G_M000_IG88:
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG93
 
G_M000_IG89:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x248], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1D8]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x248]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG94
 
G_M000_IG90:
       mov      r8, 0xD1FFAB1E
       cmp      rdx, r8
       jne      SHORT G_M000_IG93
 
G_M000_IG91:
       test     rax, rax
       je       SHORT G_M000_IG93
 
G_M000_IG92:
       mov      rdx, qword ptr [rax]
 
G_M000_IG93:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG94:
       mov      gword ptr [rbp-0x3B8], rax
       mov      edi, 88
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x3A8]
       mov      rdi, rax
       test     rdi, rdi
       je       SHORT G_M000_IG104
 
G_M000_IG95:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdi], rcx
       jne      SHORT G_M000_IG104
 
G_M000_IG96:
       mov      rdx, gword ptr [rbp-0x3B0]
       test     rdx, rdx
       je       SHORT G_M000_IG104
 
G_M000_IG97:
       cmp      qword ptr [rdx], rcx
       je       SHORT G_M000_IG99
 
G_M000_IG98:
       jmp      SHORT G_M000_IG104
 
G_M000_IG99:
       mov      rsi, gword ptr [rbp-0x3B8]
       test     rsi, rsi
       je       SHORT G_M000_IG103
 
G_M000_IG100:
       cmp      qword ptr [rsi], rcx
       je       SHORT G_M000_IG102
 
G_M000_IG101:
       jmp      SHORT G_M000_IG104
 
G_M000_IG102:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   xmm2, qword ptr [rsi+0x08]
       vmulsd   xmm0, xmm0, xmm0
       vmulsd   xmm1, xmm1, xmm1
       vaddsd   xmm0, xmm0, xmm1
       vmulsd   xmm1, xmm2, xmm2
       vaddsd   xmm0, xmm0, xmm1
       vmovsd   qword ptr [rbp-0x70], xmm0
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG105
 
G_M000_IG103:
 
G_M000_IG104:
       mov      rdx, rax
       mov      rcx, rax
       mov      edi, 57
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4B8], rax
       mov      rdx, gword ptr [rbp-0x3B0]
       mov      rcx, rdx
       mov      edi, 58
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x4B8]
       mov      edi, 59
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4C0], rax
       mov      rdx, gword ptr [rbp-0x3B8]
       mov      rcx, rdx
       mov      edi, 60
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x4C0]
       mov      edi, 61
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
 
G_M000_IG105:
       mov      gword ptr [rbp-0x3C0], rax
       mov      edi, 89
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rax, gword ptr [rbp-0x3C0]
       test     rax, rax
       je       SHORT G_M000_IG115
 
G_M000_IG106:
       mov      rsi, 0xD1FFAB1E
       mov      rcx, rsi
       cmp      qword ptr [rax], rcx
       jne      SHORT G_M000_IG109
 
G_M000_IG107:
       test     rdx, rdx
       je       SHORT G_M000_IG115
 
G_M000_IG108:
       cmp      qword ptr [rdx], rcx
       je       SHORT G_M000_IG115
       mov      rsi, qword ptr [rdx]
       jmp      SHORT G_M000_IG115
 
G_M000_IG109:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      SHORT G_M000_IG112
 
G_M000_IG110:
       test     rdx, rdx
       je       SHORT G_M000_IG115
 
G_M000_IG111:
       cmp      qword ptr [rdx], rcx
       je       SHORT G_M000_IG115
       mov      rsi, qword ptr [rdx]
       jmp      SHORT G_M000_IG115
 
G_M000_IG112:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       jne      SHORT G_M000_IG115
 
G_M000_IG113:
       test     rdx, rdx
       je       SHORT G_M000_IG115
 
G_M000_IG114:
       mov      rsi, qword ptr [rdx]
 
G_M000_IG115:
       mov      rsi, rax
       mov      edi, 7
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x440], rax
       xor      rdx, rdx
       mov      gword ptr [rbp-0x368], rdx
 
G_M000_IG116:
       mov      gword ptr [rbp-0x360], rdx
 
G_M000_IG117:
       mov      gword ptr [rbp-0x358], rdx
 
G_M000_IG118:
       mov      gword ptr [rbp-0x350], rdx
       mov      r8, gword ptr [rbp-0x3C0]
       test     r8, r8
       je       G_M000_IG138
 
G_M000_IG119:
       mov      rcx, 0xD1FFAB1E
       mov      r9, rcx
       cmp      qword ptr [r8], r9
       jne      G_M000_IG127
 
G_M000_IG120:
       mov      rsi, qword ptr [r8+0x08]
       test     rax, rax
       je       G_M000_IG126
 
G_M000_IG121:
       cmp      qword ptr [rax], r9
       jne      SHORT G_M000_IG123
 
G_M000_IG122:
       mov      rdx, qword ptr [rax+0x08]
       lea      rcx, [rbp-0x368]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryLongBinary(int,long,long,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x440]
       mov      r8, gword ptr [rbp-0x3C0]
       je       G_M000_IG138
       mov      rcx, gword ptr [rbp-0x368]
       jmp      G_M000_IG139
 
G_M000_IG123:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      SHORT G_M000_IG125
 
G_M000_IG124:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rsi
       vmovsd   xmm1, qword ptr [rax+0x08]
       lea      rsi, [rbp-0x360]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x440]
       mov      r8, gword ptr [rbp-0x3C0]
       je       G_M000_IG138
       mov      rcx, gword ptr [rbp-0x360]
       jmp      G_M000_IG139
 
G_M000_IG125:
       mov      r8, gword ptr [rbp-0x3C0]
       jmp      G_M000_IG138
 
G_M000_IG126:
       mov      r8, gword ptr [rbp-0x3C0]
       jmp      G_M000_IG138
 
G_M000_IG127:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r8], rcx
       jne      G_M000_IG135
 
G_M000_IG128:
       vmovsd   xmm0, qword ptr [r8+0x08]
       test     rax, rax
       je       G_M000_IG134
 
G_M000_IG129:
       cmp      qword ptr [rax], rcx
       jne      SHORT G_M000_IG131
 
G_M000_IG130:
       vmovsd   xmm1, qword ptr [rax+0x08]
       lea      rsi, [rbp-0x358]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x440]
       mov      r8, gword ptr [rbp-0x3C0]
       je       SHORT G_M000_IG138
       mov      rcx, gword ptr [rbp-0x358]
       jmp      SHORT G_M000_IG139
 
G_M000_IG131:
       cmp      qword ptr [rax], r9
       jne      SHORT G_M000_IG133
 
G_M000_IG132:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       lea      rsi, [rbp-0x350]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x440]
       mov      r8, gword ptr [rbp-0x3C0]
       je       SHORT G_M000_IG138
       mov      rcx, gword ptr [rbp-0x350]
       jmp      SHORT G_M000_IG139
 
G_M000_IG133:
       mov      r8, gword ptr [rbp-0x3C0]
       jmp      SHORT G_M000_IG138
 
G_M000_IG134:
       mov      r8, gword ptr [rbp-0x3C0]
       jmp      SHORT G_M000_IG138
 
G_M000_IG135:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [r8], rdx
       jne      SHORT G_M000_IG138
 
G_M000_IG136:
       test     rax, rax
       je       SHORT G_M000_IG138
 
G_M000_IG137:
       mov      rdx, qword ptr [rax]
 
G_M000_IG138:
       mov      rdx, rax
       mov      rsi, r8
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rcx, rax
 
G_M000_IG139:
       xor      rdx, rdx
       mov      gword ptr [rbp-0x368], rdx
 
G_M000_IG140:
       mov      gword ptr [rbp-0x360], rdx
 
G_M000_IG141:
       mov      gword ptr [rbp-0x358], rdx
 
G_M000_IG142:
       mov      gword ptr [rbp-0x350], rdx
       mov      rdx, rbx
       mov      edi, 54
       mov      esi, 4
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3C8], rax
       mov      edi, 90
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x398]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, gword ptr [rbp-0x3C8]
       mov      edi, 55
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3D0], rax
       mov      edi, 91
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x3A0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, gword ptr [rbp-0x3C8]
       mov      edi, 56
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x3D8], rax
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      qword ptr [rbp-0x388], rax
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
 
G_M000_IG143:
       je       G_M000_IG158
 
G_M000_IG144:
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x398]
       mov      gword ptr [rbp-0x3E0], rax
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x3E0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x448], rax
       mov      rcx, gword ptr [rbp-0x3A8]
       mov      rdi, rcx
       mov      rdx, rax
       test     rdi, rdi
       je       SHORT G_M000_IG156
 
G_M000_IG145:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG147
 
G_M000_IG146:
       jmp      SHORT G_M000_IG156
 
G_M000_IG147:
       mov      r8, gword ptr [rbp-0x3D8]
       test     r8, r8
       je       SHORT G_M000_IG155
 
G_M000_IG148:
       cmp      qword ptr [r8], rsi
       je       SHORT G_M000_IG150
 
G_M000_IG149:
       jmp      SHORT G_M000_IG156
 
G_M000_IG150:
       test     rax, rax
       je       SHORT G_M000_IG154
 
G_M000_IG151:
       cmp      qword ptr [rax], rsi
       je       SHORT G_M000_IG153
 
G_M000_IG152:
       jmp      SHORT G_M000_IG156
 
G_M000_IG153:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   xmm1, qword ptr [r8+0x08]
       vmovsd   xmm2, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vsubsd   xmm0, xmm2, xmm0
       vmovsd   qword ptr [rbp-0x78], xmm0
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp-0x78]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG157
 
G_M000_IG154:
       jmp      SHORT G_M000_IG156
 
G_M000_IG155:
 
G_M000_IG156:
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp-0x3D8]
       mov      edi, 62
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x448]
       mov      edi, 63
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG157:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x398]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG158:
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x388]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG173
 
G_M000_IG159:
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x398]
       mov      gword ptr [rbp-0x3E8], rax
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x3E8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x450], rax
       mov      rcx, gword ptr [rbp-0x3B0]
       mov      rdi, rcx
       mov      rdx, rax
       test     rdi, rdi
       je       SHORT G_M000_IG171
 
G_M000_IG160:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG162
 
G_M000_IG161:
       jmp      SHORT G_M000_IG171
 
G_M000_IG162:
       mov      r8, gword ptr [rbp-0x3D8]
       test     r8, r8
       je       SHORT G_M000_IG170
 
G_M000_IG163:
       cmp      qword ptr [r8], rsi
       je       SHORT G_M000_IG165
 
G_M000_IG164:
       jmp      SHORT G_M000_IG171
 
G_M000_IG165:
       test     rax, rax
       je       SHORT G_M000_IG169
 
G_M000_IG166:
       cmp      qword ptr [rax], rsi
       je       SHORT G_M000_IG168
 
G_M000_IG167:
       jmp      SHORT G_M000_IG171
 
G_M000_IG168:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   xmm1, qword ptr [r8+0x08]
       vmovsd   xmm2, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vsubsd   xmm0, xmm2, xmm0
       vmovsd   qword ptr [rbp-0x80], xmm0
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG172
 
G_M000_IG169:
       jmp      SHORT G_M000_IG171
 
G_M000_IG170:
 
G_M000_IG171:
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp-0x3D8]
       mov      edi, 64
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x450]
       mov      edi, 65
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG172:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x398]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG173:
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x388]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG187
 
G_M000_IG174:
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x398]
       mov      gword ptr [rbp-0x3F0], rax
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x3F0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x458], rax
       mov      rcx, gword ptr [rbp-0x3B8]
       mov      rdi, rcx
       mov      rdx, gword ptr [rbp-0x3D8]
       mov      rsi, rdx
       mov      r8, rax
       test     rdi, rdi
       je       SHORT G_M000_IG185
 
G_M000_IG175:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdi], r9
       je       SHORT G_M000_IG177
 
G_M000_IG176:
       jmp      SHORT G_M000_IG185
 
G_M000_IG177:
       test     rdx, rdx
       je       SHORT G_M000_IG184
 
G_M000_IG178:
       cmp      qword ptr [rdx], r9
       je       SHORT G_M000_IG180
 
G_M000_IG179:
       jmp      SHORT G_M000_IG185
 
G_M000_IG180:
       test     rax, rax
       je       SHORT G_M000_IG183
 
G_M000_IG181:
       cmp      qword ptr [rax], r9
       jne      SHORT G_M000_IG185
 
G_M000_IG182:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmovsd   xmm2, qword ptr [r8+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vsubsd   xmm0, xmm2, xmm0
       vmovsd   qword ptr [rbp-0x88], xmm0
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG186
 
G_M000_IG183:
       jmp      SHORT G_M000_IG185
 
G_M000_IG184:
 
G_M000_IG185:
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp-0x3D8]
       mov      edi, 66
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x458]
       mov      edi, 67
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG186:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x398]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG187:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x388]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG202
 
G_M000_IG188:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x3A0]
       mov      gword ptr [rbp-0x3F8], rax
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x3F8]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x460], rax
       mov      rcx, gword ptr [rbp-0x3A8]
       mov      rdi, rcx
       mov      rdx, rax
       test     rdi, rdi
       je       SHORT G_M000_IG200
 
G_M000_IG189:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdi], r9
       je       SHORT G_M000_IG191
 
G_M000_IG190:
       jmp      SHORT G_M000_IG200
 
G_M000_IG191:
       mov      rsi, gword ptr [rbp-0x3D0]
       test     rsi, rsi
       je       SHORT G_M000_IG199
 
G_M000_IG192:
       cmp      qword ptr [rsi], r9
       je       SHORT G_M000_IG194
 
G_M000_IG193:
       jmp      SHORT G_M000_IG200
 
G_M000_IG194:
       test     rax, rax
       je       SHORT G_M000_IG198
 
G_M000_IG195:
       cmp      qword ptr [rax], r9
       je       SHORT G_M000_IG197
 
G_M000_IG196:
       jmp      SHORT G_M000_IG200
 
G_M000_IG197:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm0
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG201
 
G_M000_IG198:
       jmp      SHORT G_M000_IG200
 
G_M000_IG199:
 
G_M000_IG200:
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp-0x3D0]
       mov      edi, 68
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x460]
       mov      edi, 69
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG201:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x3A0]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG202:
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x388]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG217
 
G_M000_IG203:
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x3A0]
       mov      gword ptr [rbp-0x400], rax
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x400]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x468], rax
       mov      rcx, gword ptr [rbp-0x3B0]
       mov      rdi, rcx
       mov      rdx, rax
       test     rdi, rdi
       je       SHORT G_M000_IG215
 
G_M000_IG204:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdi], r9
       je       SHORT G_M000_IG206
 
G_M000_IG205:
       jmp      SHORT G_M000_IG215
 
G_M000_IG206:
       mov      rsi, gword ptr [rbp-0x3D0]
       test     rsi, rsi
       je       SHORT G_M000_IG214
 
G_M000_IG207:
       cmp      qword ptr [rsi], r9
       je       SHORT G_M000_IG209
 
G_M000_IG208:
       jmp      SHORT G_M000_IG215
 
G_M000_IG209:
       test     rax, rax
       je       SHORT G_M000_IG213
 
G_M000_IG210:
       cmp      qword ptr [rax], r9
       je       SHORT G_M000_IG212
 
G_M000_IG211:
       jmp      SHORT G_M000_IG215
 
G_M000_IG212:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm0
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG216
 
G_M000_IG213:
       jmp      SHORT G_M000_IG215
 
G_M000_IG214:
 
G_M000_IG215:
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp-0x3D0]
       mov      edi, 70
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x468]
       mov      edi, 71
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG216:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x3A0]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG217:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x388]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG231
 
G_M000_IG218:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x3A0]
       mov      gword ptr [rbp-0x408], rax
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x408]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x470], rax
       mov      rcx, gword ptr [rbp-0x3B8]
       mov      rdi, rcx
       mov      rdx, gword ptr [rbp-0x3D0]
       mov      rsi, rdx
       mov      r8, rax
       test     rdi, rdi
       je       SHORT G_M000_IG229
 
G_M000_IG219:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdi], r9
       je       SHORT G_M000_IG221
 
G_M000_IG220:
       jmp      SHORT G_M000_IG229
 
G_M000_IG221:
       test     rdx, rdx
       je       SHORT G_M000_IG228
 
G_M000_IG222:
       cmp      qword ptr [rdx], r9
       je       SHORT G_M000_IG224
 
G_M000_IG223:
       jmp      SHORT G_M000_IG229
 
G_M000_IG224:
       test     rax, rax
       je       SHORT G_M000_IG227
 
G_M000_IG225:
       cmp      qword ptr [rax], r9
       jne      SHORT G_M000_IG229
 
G_M000_IG226:
       vmovsd   xmm0, qword ptr [rdi+0x08]
       vmovsd   xmm1, qword ptr [rsi+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [r8+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG230
 
G_M000_IG227:
       jmp      SHORT G_M000_IG229
 
G_M000_IG228:
 
G_M000_IG229:
       mov      rdx, rcx
       mov      rcx, gword ptr [rbp-0x3D0]
       mov      edi, 72
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x470]
       mov      edi, 73
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG230:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x408]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG231:
       mov      rax, qword ptr [rbp-0x60]
       add      rax, 1
       jo       G_M000_IG317
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x68]
 
G_M000_IG232:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG06
 
G_M000_IG233:
       mov      qword ptr [rbp-0x60], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       G_M000_IG34
 
G_M000_IG234:
       test     rdi, rdi
       jl       G_M000_IG317
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG316
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       mov      gword ptr [rbp-0x498], rdx
       jmp      G_M000_IG35
 
G_M000_IG235:
       mov      edi, 98
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyIter:Create(System.Object):RyuPy.Runtime.IPyIterator]
       mov      gword ptr [rbp-0x478], rax
       jmp      G_M000_IG308
 
G_M000_IG236:
       mov      rax, gword ptr [rbp-0x478]
       jmp      G_M000_IG308
 
G_M000_IG237:
       test     rax, rax
       je       SHORT G_M000_IG244
 
G_M000_IG238:
       cmp      qword ptr [rax], r9
       je       SHORT G_M000_IG240
 
G_M000_IG239:
       jmp      SHORT G_M000_IG245
 
G_M000_IG240:
       test     rdx, rdx
       je       SHORT G_M000_IG243
 
G_M000_IG241:
       cmp      qword ptr [rdx], r9
       jne      SHORT G_M000_IG245
 
G_M000_IG242:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG246
 
G_M000_IG243:
       jmp      SHORT G_M000_IG245
 
G_M000_IG244:
 
G_M000_IG245:
       mov      rdx, rbx
       mov      rcx, rax
       mov      edi, 74
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x480]
       mov      edi, 75
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG246:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x398]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG247:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x388]
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG261
 
G_M000_IG248:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x398]
       mov      gword ptr [rbp-0x418], rax
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x418]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x488], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x418]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rbx
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x488]
       mov      rsi, rdx
       test     rdi, rdi
       je       SHORT G_M000_IG259
 
G_M000_IG249:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdi], r9
       je       SHORT G_M000_IG251
 
G_M000_IG250:
       jmp      SHORT G_M000_IG259
 
G_M000_IG251:
       test     rax, rax
       je       SHORT G_M000_IG258
 
G_M000_IG252:
       cmp      qword ptr [rax], r9
       je       SHORT G_M000_IG254
 
G_M000_IG253:
       jmp      SHORT G_M000_IG259
 
G_M000_IG254:
       test     rdx, rdx
       je       SHORT G_M000_IG257
 
G_M000_IG255:
       cmp      qword ptr [rdx], r9
       jne      SHORT G_M000_IG259
 
G_M000_IG256:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       vmovsd   xmm0, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rdx+0x08], xmm0
       jmp      SHORT G_M000_IG260
 
G_M000_IG257:
       jmp      SHORT G_M000_IG259
 
G_M000_IG258:
 
G_M000_IG259:
       mov      rdx, rbx
       mov      rcx, rax
       mov      edi, 76
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x488]
       mov      edi, 77
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG260:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x398]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG261:
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, qword ptr [rbp-0x388]
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG236
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x398]
       mov      gword ptr [rbp-0x420], rax
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x420]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x490], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x420]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       test     rbx, rbx
       je       G_M000_IG283
 
G_M000_IG262:
       mov      r9, 0xD1FFAB1E
       mov      rcx, r9
       cmp      qword ptr [rbx], rcx
       jne      G_M000_IG274
 
G_M000_IG263:
       mov      r8, qword ptr [rbx+0x08]
       mov      qword ptr [rbp-0x260], r8
       test     rax, rax
       je       G_M000_IG283
 
G_M000_IG264:
       mov      qword ptr [rbp-0x380], rcx
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG272
 
G_M000_IG265:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x270], rax
       xor      edx, edx
       mov      qword ptr [rbp-0x278], rdx
       lea      rdx, [rbp-0x278]
       mov      rdi, r8
       mov      rsi, rax
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x278]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG268
       mov      rax, qword ptr [rbp-0x278]
       mov      qword ptr [rbp-0x2C0], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG266
       test     rdi, rdi
       jl       G_M000_IG317
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG316
       test     rdi, rdi
       jl       G_M000_IG317
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG267
 
G_M000_IG266:
       mov      rdi, qword ptr [rbp-0x380]
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x2C0]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG267:
       jmp      G_M000_IG271
 
G_M000_IG268:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x288], xmm0
       lea      rdi, [rbp-0x288]
       mov      rsi, qword ptr [rbp-0x260]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG269:
       vmovdqu  xmm0, xmmword ptr [rbp-0x288]
       vmovdqu  xmmword ptr [rbp-0x298], xmm0
 
G_M000_IG270:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2B8], xmm0
       lea      rdi, [rbp-0x2B8]
       mov      rsi, qword ptr [rbp-0x270]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x298]
       mov      rsi, qword ptr [rbp-0x290]
       mov      rdx, gword ptr [rbp-0x2B8]
       mov      rcx, qword ptr [rbp-0x2B0]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2A8], rax
       mov      qword ptr [rbp-0x2A0], rdx
       mov      rdi, gword ptr [rbp-0x2A8]
       mov      rsi, qword ptr [rbp-0x2A0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG271:
       jmp      G_M000_IG284
 
G_M000_IG272:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rax], r9
       jne      G_M000_IG283
 
G_M000_IG273:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x268], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r8
       vmovsd   qword ptr [rbp-0x2C8], xmm1
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2C8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x268]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG284
 
G_M000_IG274:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rbx], r9
       jne      G_M000_IG280
 
G_M000_IG275:
       vmovsd   xmm0, qword ptr [rbx+0x08]
       vmovsd   qword ptr [rbp-0x250], xmm0
       test     rax, rax
       je       G_M000_IG283
 
G_M000_IG276:
       cmp      qword ptr [rax], r9
       jne      SHORT G_M000_IG278
 
G_M000_IG277:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x258], xmm1
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x250]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x258]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG284
 
G_M000_IG278:
       cmp      qword ptr [rax], rcx
       jne      SHORT G_M000_IG283
 
G_M000_IG279:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x2D0], xmm1
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x2D0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x250]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG284
 
G_M000_IG280:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdx
       jne      SHORT G_M000_IG283
 
G_M000_IG281:
       test     rax, rax
       je       SHORT G_M000_IG283
 
G_M000_IG282:
       mov      rdx, qword ptr [rax]
 
G_M000_IG283:
       mov      rdx, rax
       mov      rsi, rbx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG284:
       mov      rax, gword ptr [rbp-0x490]
       test     rax, rax
       je       G_M000_IG313
 
G_M000_IG285:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG297
 
G_M000_IG286:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG313
 
G_M000_IG287:
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG295
 
G_M000_IG288:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x2F0], rax
       lea      rdx, [rsi+rax]
       mov      qword ptr [rbp-0x2F8], rdx
       mov      rdi, rsi
       xor      rdi, rdx
       mov      r8, rax
       xor      r8, rdx
       and      rdi, r8
       jl       SHORT G_M000_IG291
       lea      rdi, [rdx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG289
       test     rdi, rdi
       jl       G_M000_IG317
       mov      rcx, rdi
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rcx, 262
       jae      G_M000_IG316
       test     rdi, rdi
       jl       G_M000_IG317
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG290
 
G_M000_IG289:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x2F8]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG290:
       jmp      G_M000_IG294
 
G_M000_IG291:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x308], xmm0
       lea      rdi, [rbp-0x308]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG292:
       vmovdqu  xmm0, xmmword ptr [rbp-0x308]
       vmovdqu  xmmword ptr [rbp-0x318], xmm0
 
G_M000_IG293:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x338], xmm0
       lea      rdi, [rbp-0x338]
       mov      rsi, qword ptr [rbp-0x2F0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x318]
       mov      rsi, qword ptr [rbp-0x310]
       mov      rdx, gword ptr [rbp-0x338]
       mov      rcx, qword ptr [rbp-0x330]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x328], rax
       mov      qword ptr [rbp-0x320], rdx
       mov      rdi, gword ptr [rbp-0x328]
       mov      rsi, qword ptr [rbp-0x320]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG294:
       jmp      G_M000_IG307
 
G_M000_IG295:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdx], r9
       jne      G_M000_IG313
 
G_M000_IG296:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x2E8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x340], xmm1
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x340]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x2E8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG307
 
G_M000_IG297:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rax], r9
       jne      G_M000_IG303
 
G_M000_IG298:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x2D8], xmm0
       test     rdx, rdx
       je       G_M000_IG313
 
G_M000_IG299:
       cmp      qword ptr [rdx], r9
       jne      SHORT G_M000_IG301
 
G_M000_IG300:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x2E0], xmm1
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2D8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x2E0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG307
 
G_M000_IG301:
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG313
 
G_M000_IG302:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x348], xmm1
       mov      rdi, r9
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x348]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x2D8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG307
 
G_M000_IG303:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG313
 
G_M000_IG304:
       mov      rsi, rax
       test     rdx, rdx
       je       G_M000_IG313
 
G_M000_IG305:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG313
 
G_M000_IG306:
       mov      rdi, rsi
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      rdx, rax
 
G_M000_IG307:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x420]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       mov      rax, gword ptr [rbp-0x478]
 
G_M000_IG308:
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:IterNextOk(System.Object):bool]
       test     eax, eax
       je       G_M000_IG03
 
G_M000_IG309:
       mov      rdi, gword ptr [rbp-0x478]
       call     [RyuPy.Runtime.PyOps:IterValue(System.Object):System.Object]
       mov      gword ptr [rbp-0x398], rax
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      qword ptr [rbp-0x388], rax
       mov      rdi, gword ptr [rax]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG247
 
G_M000_IG310:
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x398]
       mov      gword ptr [rbp-0x410], rax
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x410]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x480], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x410]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, rbx
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x480]
       mov      rsi, rdx
       test     rdi, rdi
       je       G_M000_IG245
 
G_M000_IG311:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdi], r9
       je       G_M000_IG237
 
G_M000_IG312:
       jmp      G_M000_IG245
 
G_M000_IG313:
       mov      rsi, rax
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       jmp      G_M000_IG307
 
G_M000_IG314:
       mov      rdi, 0xD1FFAB1E
       mov      rbx, gword ptr [rdi]
       mov      rdi, r13
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, rbx
 
G_M000_IG315:
       add      rsp, 0x498
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG316:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG317:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 8471

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_advance(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
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
       call     [RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 47

