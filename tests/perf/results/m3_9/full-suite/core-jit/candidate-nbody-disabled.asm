; Assembly listing for method RyuPy.Generated.Module:m0_advance(System.Object,System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 6 inlinees with PGO data; 38 single block inlinees; 104 inlinees without PGO data

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
       vmovdqa32 xmmword ptr [rbp-0x440], xmm8
       mov      rax, -960
       vmovdqa  xmmword ptr [rbp+rax-0x70], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x60], xmm8
       vmovdqa  xmmword ptr [rbp+rax-0x50], xmm8
       add      rax, 48
       jne      SHORT  -5 instr
       mov      qword ptr [rbp-0x70], rax
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
       lea      rdi, [rbp-0x80]
       mov      rsi, r12
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x80]
       mov      rdx, gword ptr [rbp-0x78]
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
       jo       G_M000_IG218
 
G_M000_IG04:
       mov      qword ptr [rbp-0x38], rax
       cmp      rax, r14
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG215
 
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
       jo       G_M000_IG218
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x50]
 
G_M000_IG07:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG206
 
G_M000_IG08:
       mov      qword ptr [rbp-0x48], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG10
 
G_M000_IG09:
       test     rdi, rdi
       jl       G_M000_IG218
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG217
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
       mov      gword ptr [rbp-0x460], rcx
       mov      edi, 82
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x460]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x468], rax
       mov      edi, 83
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      rax, gword ptr [rbp-0x460]
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
       mov      qword ptr [rbp-0xA0], rax
       lea      rdx, [rcx+rax]
       mov      qword ptr [rbp-0xA8], rdx
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
       jl       G_M000_IG218
       mov      rsi, 0xD1FFAB1E
       mov      rdx, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG217
       mov      rdi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG17
 
G_M000_IG16:
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, rax
       mov      rsi, qword ptr [rbp-0xA8]
       mov      qword ptr [rdi+0x08], rsi
 
G_M000_IG17:
       jmp      G_M000_IG19
 
G_M000_IG18:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       lea      rdi, [rbp-0xB8]
       mov      rsi, rcx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0x4F0], rax
       mov      ecx, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0x444], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       mov      rsi, qword ptr [rbp-0xA0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x4F0]
       mov      esi, dword ptr [rbp-0x444]
       mov      rdx, gword ptr [rbp-0xC8]
       mov      rcx, qword ptr [rbp-0xC0]
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
       vmovsd   qword ptr [rbp-0x98], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rcx
       vmovsd   qword ptr [rbp-0xD0], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       jmp      G_M000_IG33
 
G_M000_IG22:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG28
 
G_M000_IG23:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       test     rdx, rdx
       je       G_M000_IG32
 
G_M000_IG24:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG26
 
G_M000_IG25:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       jmp      SHORT G_M000_IG33
 
G_M000_IG26:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG32
 
G_M000_IG27:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xD8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdi, rax
       jmp      SHORT G_M000_IG33
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      SHORT G_M000_IG32
 
G_M000_IG29:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG32
 
G_M000_IG30:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
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
       jmp      G_M000_IG203
 
G_M000_IG34:
       mov      rsi, 0xD1FFAB1E
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rax, qword ptr [rbp-0x60]
       mov      qword ptr [rdx+0x08], rax
       mov      gword ptr [rbp-0x4E0], rdx
 
G_M000_IG35:
       mov      edi, 84
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       mov      rsi, gword ptr [rbp-0x4E0]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x470], rax
       mov      edi, 85
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4A8], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x470]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp-0x4A8]
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
       mov      qword ptr [rbp-0xF8], rax
       mov      rcx, rdx
       sub      rcx, rax
       mov      qword ptr [rbp-0x100], rcx
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
       jl       G_M000_IG218
       mov      rsi, 0xD1FFAB1E
       mov      rdx, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG217
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG41
 
G_M000_IG40:
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x100]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG41:
       jmp      G_M000_IG43
 
G_M000_IG42:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x110], xmm0
       lea      rdi, [rbp-0x110]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x110]
       mov      gword ptr [rbp-0x4F8], rax
       mov      ecx, dword ptr [rbp-0x108]
       mov      dword ptr [rbp-0x448], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x120], xmm0
       lea      rdi, [rbp-0x120]
       mov      rsi, qword ptr [rbp-0xF8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x4F8]
       mov      esi, dword ptr [rbp-0x448]
       mov      rdx, gword ptr [rbp-0x120]
       mov      rcx, qword ptr [rbp-0x118]
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
       vmovsd   qword ptr [rbp-0xF0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x128], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x128]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0xF0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG53
 
G_M000_IG46:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      SHORT G_M000_IG52
 
G_M000_IG47:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm0
       test     rax, rax
       je       SHORT G_M000_IG52
 
G_M000_IG48:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG50
 
G_M000_IG49:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE0]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG53
 
G_M000_IG50:
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG52
 
G_M000_IG51:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x130], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xE0]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x130]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG53
 
G_M000_IG52:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG53:
       mov      gword ptr [rbp-0x478], rax
       mov      edi, 86
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4B0], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x470]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp-0x4B0]
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
       mov      qword ptr [rbp-0x150], rax
       mov      rcx, rdx
       sub      rcx, rax
       mov      qword ptr [rbp-0x158], rcx
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
       jl       G_M000_IG218
       mov      rsi, 0xD1FFAB1E
       mov      rdx, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG217
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG59
 
G_M000_IG58:
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x158]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG59:
       jmp      G_M000_IG61
 
G_M000_IG60:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x168], xmm0
       lea      rdi, [rbp-0x168]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x168]
       mov      gword ptr [rbp-0x500], rax
       mov      ecx, dword ptr [rbp-0x160]
       mov      dword ptr [rbp-0x44C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x188], xmm0
       lea      rdi, [rbp-0x188]
       mov      rsi, qword ptr [rbp-0x150]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x500]
       mov      esi, dword ptr [rbp-0x44C]
       mov      rdx, gword ptr [rbp-0x188]
       mov      rcx, qword ptr [rbp-0x180]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x178], rax
       mov      qword ptr [rbp-0x170], rdx
       mov      rdi, gword ptr [rbp-0x178]
       mov      rsi, qword ptr [rbp-0x170]
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
       vmovsd   qword ptr [rbp-0x148], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x190], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x190]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0x148]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG71
 
G_M000_IG64:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      SHORT G_M000_IG70
 
G_M000_IG65:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x138], xmm0
       test     rax, rax
       je       SHORT G_M000_IG70
 
G_M000_IG66:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG68
 
G_M000_IG67:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x140], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x138]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x140]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG71
 
G_M000_IG68:
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG70
 
G_M000_IG69:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x198], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x138]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x198]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG71
 
G_M000_IG70:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 2
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG71:
       mov      gword ptr [rbp-0x480], rax
       mov      edi, 87
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4B8], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x470]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, gword ptr [rbp-0x4B8]
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
       mov      qword ptr [rbp-0x1B8], rax
       mov      rcx, rdx
       sub      rcx, rax
       mov      qword ptr [rbp-0x1C0], rcx
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
       jl       G_M000_IG218
       mov      rsi, 0xD1FFAB1E
       mov      rdx, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG217
       mov      rsi, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG77
 
G_M000_IG76:
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      rdi, qword ptr [rbp-0x1C0]
       mov      qword ptr [rsi+0x08], rdi
 
G_M000_IG77:
       jmp      G_M000_IG81
 
G_M000_IG78:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x1D0], xmm0
       lea      rdi, [rbp-0x1D0]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG79:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x1D0]
       vmovdqu32 xmmword ptr [rbp-0x1E0], xmm0
 
G_M000_IG80:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x200], xmm0
       lea      rdi, [rbp-0x200]
       mov      rsi, qword ptr [rbp-0x1B8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1E0]
       mov      rsi, qword ptr [rbp-0x1D8]
       mov      rdx, gword ptr [rbp-0x200]
       mov      rcx, qword ptr [rbp-0x1F8]
       call     [System.Numerics.BigInteger:op_Subtraction(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x1F0], rax
       mov      qword ptr [rbp-0x1E8], rdx
       mov      rdi, gword ptr [rbp-0x1F0]
       mov      rsi, qword ptr [rbp-0x1E8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rsi, rax
 
G_M000_IG81:
       mov      rax, rsi
       jmp      G_M000_IG94
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG93
 
G_M000_IG83:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1B0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x208], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x208]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0x1B0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG94
 
G_M000_IG84:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      SHORT G_M000_IG90
 
G_M000_IG85:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x1A0], xmm0
       test     rax, rax
       je       SHORT G_M000_IG93
 
G_M000_IG86:
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG88
 
G_M000_IG87:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x1A8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1A0]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x1A8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG94
 
G_M000_IG88:
       cmp      qword ptr [rax], rsi
       jne      SHORT G_M000_IG93
 
G_M000_IG89:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x210], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x1A0]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x210]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG94
 
G_M000_IG90:
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
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
       mov      gword ptr [rbp-0x488], rax
       mov      edi, 88
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x478]
       test     rax, rax
       je       G_M000_IG106
 
G_M000_IG95:
       mov      rsi, 0xD1FFAB1E
       mov      rcx, rsi
       mov      qword ptr [rbp-0x458], rcx
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG103
 
G_M000_IG96:
       mov      r8, qword ptr [rax+0x08]
       mov      r9, r8
       mov      qword ptr [rbp-0x228], r9
       cmp      dword ptr [rax], eax
       mov      qword ptr [rbp-0x230], r8
       xor      edx, edx
       mov      qword ptr [rbp-0x238], rdx
       lea      rdx, [rbp-0x238]
       mov      rdi, r9
       mov      rsi, r8
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x238]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG99
       mov      rax, qword ptr [rbp-0x238]
       mov      qword ptr [rbp-0x280], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG97
       test     rdi, rdi
       jl       G_M000_IG218
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG217
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG98
 
G_M000_IG97:
       mov      rdi, qword ptr [rbp-0x458]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x280]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG98:
       jmp      G_M000_IG102
 
G_M000_IG99:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x248], xmm0
       lea      rdi, [rbp-0x248]
       mov      rsi, qword ptr [rbp-0x228]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG100:
       vmovdqu  xmm0, xmmword ptr [rbp-0x248]
       vmovdqu  xmmword ptr [rbp-0x258], xmm0
 
G_M000_IG101:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x278], xmm0
       lea      rdi, [rbp-0x278]
       mov      rsi, qword ptr [rbp-0x230]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x258]
       mov      rsi, qword ptr [rbp-0x250]
       mov      rdx, gword ptr [rbp-0x278]
       mov      rcx, qword ptr [rbp-0x270]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x268], rax
       mov      qword ptr [rbp-0x260], rdx
       mov      rdi, gword ptr [rbp-0x268]
       mov      rsi, qword ptr [rbp-0x260]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG102:
       jmp      SHORT G_M000_IG107
 
G_M000_IG103:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG105
 
G_M000_IG104:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x218], xmm0
       cmp      dword ptr [rax], eax
       vmovsd   qword ptr [rbp-0x220], xmm0
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x218]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x220]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG107
 
G_M000_IG105:
       mov      rdx, qword ptr [rax]
 
G_M000_IG106:
       mov      rdx, rax
       mov      rsi, rax
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG107:
       mov      gword ptr [rbp-0x4C0], rax
       mov      rcx, gword ptr [rbp-0x480]
       test     rcx, rcx
       je       G_M000_IG119
 
G_M000_IG108:
       mov      rdi, 0xD1FFAB1E
       mov      r8, rdi
       mov      qword ptr [rbp-0x458], r8
       cmp      qword ptr [rcx], r8
       jne      G_M000_IG116
 
G_M000_IG109:
       mov      r9, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x298], r9
       cmp      dword ptr [rcx], ecx
       mov      r10, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x2A0], r10
       xor      edx, edx
       mov      qword ptr [rbp-0x2A8], rdx
       lea      rdx, [rbp-0x2A8]
       mov      rdi, r9
       mov      rsi, r10
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x2A8]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG112
       mov      rax, qword ptr [rbp-0x2A8]
       mov      qword ptr [rbp-0x2F0], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG110
       test     rdi, rdi
       jl       G_M000_IG218
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG217
       test     rdi, rdi
       jl       G_M000_IG218
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG111
 
G_M000_IG110:
       mov      rdi, qword ptr [rbp-0x458]
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x2F0]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG111:
       jmp      G_M000_IG115
 
G_M000_IG112:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2B8], xmm0
       lea      rdi, [rbp-0x2B8]
       mov      rsi, qword ptr [rbp-0x298]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG113:
       vmovdqu  xmm0, xmmword ptr [rbp-0x2B8]
       vmovdqu  xmmword ptr [rbp-0x2C8], xmm0
 
G_M000_IG114:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x2E8], xmm0
       lea      rdi, [rbp-0x2E8]
       mov      rsi, qword ptr [rbp-0x2A0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x2C8]
       mov      rsi, qword ptr [rbp-0x2C0]
       mov      rdx, gword ptr [rbp-0x2E8]
       mov      rcx, qword ptr [rbp-0x2E0]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x2D8], rax
       mov      qword ptr [rbp-0x2D0], rdx
       mov      rdi, gword ptr [rbp-0x2D8]
       mov      rsi, qword ptr [rbp-0x2D0]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG115:
       jmp      SHORT G_M000_IG120
 
G_M000_IG116:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG118
 
G_M000_IG117:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x288], xmm0
       cmp      dword ptr [rcx], ecx
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x290], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x288]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x290]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG120
 
G_M000_IG118:
       mov      rdx, qword ptr [rcx]
 
G_M000_IG119:
       mov      rdx, rcx
       mov      rsi, rcx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      rdx, rax
 
G_M000_IG120:
       mov      rax, gword ptr [rbp-0x4C0]
       test     rax, rax
       je       G_M000_IG143
 
G_M000_IG121:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG133
 
G_M000_IG122:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG143
 
G_M000_IG123:
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG131
 
G_M000_IG124:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x310], rax
       lea      rdx, [rsi+rax]
       mov      qword ptr [rbp-0x318], rdx
       mov      rdi, rsi
       xor      rdi, rdx
       mov      r8, rax
       xor      r8, rdx
       and      rdi, r8
       jl       SHORT G_M000_IG127
       lea      rdi, [rdx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG125
       test     rdi, rdi
       jl       G_M000_IG218
       mov      rcx, rdi
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rcx, 262
       jae      G_M000_IG217
       test     rdi, rdi
       jl       G_M000_IG218
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG126
 
G_M000_IG125:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x318]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG126:
       jmp      G_M000_IG130
 
G_M000_IG127:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x328], xmm0
       lea      rdi, [rbp-0x328]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG128:
       vmovdqu  xmm0, xmmword ptr [rbp-0x328]
       vmovdqu  xmmword ptr [rbp-0x338], xmm0
 
G_M000_IG129:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x358], xmm0
       lea      rdi, [rbp-0x358]
       mov      rsi, qword ptr [rbp-0x310]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x338]
       mov      rsi, qword ptr [rbp-0x330]
       mov      rdx, gword ptr [rbp-0x358]
       mov      rcx, qword ptr [rbp-0x350]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x348], rax
       mov      qword ptr [rbp-0x340], rdx
       mov      rdi, gword ptr [rbp-0x348]
       mov      rsi, qword ptr [rbp-0x340]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG130:
       jmp      G_M000_IG144
 
G_M000_IG131:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG143
 
G_M000_IG132:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x308], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x360], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x360]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x308]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG144
 
G_M000_IG133:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG139
 
G_M000_IG134:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x2F8], xmm0
       test     rdx, rdx
       je       G_M000_IG143
 
G_M000_IG135:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG137
 
G_M000_IG136:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x300], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x2F8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x300]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG144
 
G_M000_IG137:
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG143
 
G_M000_IG138:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x368], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x368]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x2F8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG144
 
G_M000_IG139:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG143
 
G_M000_IG140:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG143
 
G_M000_IG141:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG143
 
G_M000_IG142:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG144
 
G_M000_IG143:
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG144:
       mov      gword ptr [rbp-0x4C8], rax
       mov      rcx, gword ptr [rbp-0x488]
       test     rcx, rcx
       je       G_M000_IG156
 
G_M000_IG145:
       mov      rdx, 0xD1FFAB1E
       mov      r8, rdx
       mov      qword ptr [rbp-0x458], r8
       cmp      qword ptr [rcx], r8
       jne      G_M000_IG153
 
G_M000_IG146:
       mov      r9, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x380], r9
       cmp      dword ptr [rcx], ecx
       mov      r10, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x388], r10
       xor      edx, edx
       mov      qword ptr [rbp-0x390], rdx
       lea      rdx, [rbp-0x390]
       mov      rdi, r9
       mov      rsi, r10
       call     [System.Math:BigMul(long,long,byref):long]
       mov      rdi, qword ptr [rbp-0x390]
       sar      rdi, 63
       cmp      rax, rdi
       jne      SHORT G_M000_IG149
       mov      rax, qword ptr [rbp-0x390]
       mov      qword ptr [rbp-0x3D8], rax
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG147
       test     rdi, rdi
       jl       G_M000_IG218
       mov      rax, rdi
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       cmp      rax, 262
       jae      G_M000_IG217
       test     rdi, rdi
       jl       G_M000_IG218
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG148
 
G_M000_IG147:
       mov      rdi, qword ptr [rbp-0x458]
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x3D8]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG148:
       jmp      G_M000_IG152
 
G_M000_IG149:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3A0], xmm0
       lea      rdi, [rbp-0x3A0]
       mov      rsi, qword ptr [rbp-0x380]
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG150:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x3A0]
       vmovdqu32 xmmword ptr [rbp-0x3B0], xmm0
 
G_M000_IG151:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x3D0], xmm0
       lea      rdi, [rbp-0x3D0]
       mov      rsi, qword ptr [rbp-0x388]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x3B0]
       mov      rsi, qword ptr [rbp-0x3A8]
       mov      rdx, gword ptr [rbp-0x3D0]
       mov      rcx, qword ptr [rbp-0x3C8]
       call     [System.Numerics.BigInteger:op_Multiply(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x3C0], rax
       mov      qword ptr [rbp-0x3B8], rdx
       mov      rdi, gword ptr [rbp-0x3C0]
       mov      rsi, qword ptr [rbp-0x3B8]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG152:
       jmp      SHORT G_M000_IG157
 
G_M000_IG153:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG155
 
G_M000_IG154:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x370], xmm0
       cmp      dword ptr [rcx], ecx
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x378], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x370]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x378]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG157
 
G_M000_IG155:
       mov      rdx, qword ptr [rcx]
 
G_M000_IG156:
       mov      rdx, rcx
       mov      rsi, rcx
       mov      edi, 3
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
 
G_M000_IG157:
       mov      rsi, rax
       mov      gword ptr [rbp-0x4D0], rsi
       xor      rdi, rdi
       mov      gword ptr [rbp-0x3F0], rdi
 
G_M000_IG158:
       mov      gword ptr [rbp-0x3E8], rdi
 
G_M000_IG159:
       mov      gword ptr [rbp-0x3E0], rdi
       mov      rax, gword ptr [rbp-0x4C8]
       test     rax, rax
       je       G_M000_IG174
 
G_M000_IG160:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, rdi
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG175
 
G_M000_IG161:
       mov      rdx, qword ptr [rax+0x08]
       test     rsi, rsi
       je       G_M000_IG173
 
G_M000_IG162:
       cmp      qword ptr [rsi], rcx
       jne      G_M000_IG170
 
G_M000_IG163:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [rbp-0x3F8], rax
       lea      rsi, [rdx+rax]
       mov      qword ptr [rbp-0x400], rsi
       mov      rdi, rdx
       xor      rdi, rsi
       mov      r8, rax
       xor      r8, rsi
       and      rdi, r8
       jl       SHORT G_M000_IG166
       lea      rdi, [rsi+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG164
       test     rdi, rdi
       jl       G_M000_IG218
       mov      rcx, rdi
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rcx, 262
       jae      G_M000_IG217
       test     rdi, rdi
       jl       G_M000_IG218
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG165
 
G_M000_IG164:
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x400]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG165:
       jmp      G_M000_IG169
 
G_M000_IG166:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x410], xmm0
       lea      rdi, [rbp-0x410]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
 
G_M000_IG167:
       vmovdqu32 xmm0, xmmword ptr [rbp-0x410]
       vmovdqu32 xmmword ptr [rbp-0x420], xmm0
 
G_M000_IG168:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x440], xmm0
       lea      rdi, [rbp-0x440]
       mov      rsi, qword ptr [rbp-0x3F8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x420]
       mov      rsi, qword ptr [rbp-0x418]
       mov      rdx, gword ptr [rbp-0x440]
       mov      rcx, qword ptr [rbp-0x438]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x430], rax
       mov      qword ptr [rbp-0x428], rdx
       mov      rdi, gword ptr [rbp-0x430]
       mov      rsi, qword ptr [rbp-0x428]
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG169:
       mov      gword ptr [rbp-0x4E8], rax
       jmp      G_M000_IG187
 
G_M000_IG170:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG172
 
G_M000_IG171:
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, rdx
       vmovsd   xmm1, qword ptr [rsi+0x08]
       lea      rsi, [rbp-0x3F0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x4C8]
       mov      rsi, gword ptr [rbp-0x4D0]
       je       SHORT G_M000_IG174
       mov      rax, gword ptr [rbp-0x3F0]
       mov      gword ptr [rbp-0x4E8], rax
       jmp      G_M000_IG187
 
G_M000_IG172:
       mov      rax, gword ptr [rbp-0x4C8]
       jmp      SHORT G_M000_IG174
 
G_M000_IG173:
       mov      rax, gword ptr [rbp-0x4C8]
 
G_M000_IG174:
       mov      rdx, rsi
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4E8], rax
       jmp      G_M000_IG187
 
G_M000_IG175:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG183
 
G_M000_IG176:
       vmovsd   xmm0, qword ptr [rax+0x08]
       test     rsi, rsi
       je       G_M000_IG182
 
G_M000_IG177:
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG179
 
G_M000_IG178:
       vmovsd   xmm1, qword ptr [rsi+0x08]
       lea      rsi, [rbp-0x3E8]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x4C8]
       mov      rsi, gword ptr [rbp-0x4D0]
       je       SHORT G_M000_IG174
       mov      rax, gword ptr [rbp-0x3E8]
       mov      gword ptr [rbp-0x4E8], rax
       jmp      G_M000_IG187
 
G_M000_IG179:
       cmp      qword ptr [rsi], rcx
       jne      SHORT G_M000_IG181
 
G_M000_IG180:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rsi+0x08]
       lea      rsi, [rbp-0x3E0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:TryDoubleBinary(int,double,double,byref):bool]
       test     eax, eax
       mov      rax, gword ptr [rbp-0x4C8]
       mov      rsi, gword ptr [rbp-0x4D0]
       je       G_M000_IG174
       mov      rax, gword ptr [rbp-0x3E0]
       mov      gword ptr [rbp-0x4E8], rax
       jmp      SHORT G_M000_IG187
 
G_M000_IG181:
       mov      rax, gword ptr [rbp-0x4C8]
       jmp      G_M000_IG174
 
G_M000_IG182:
       mov      rax, gword ptr [rbp-0x4C8]
       jmp      G_M000_IG174
 
G_M000_IG183:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG174
 
G_M000_IG184:
       mov      rdi, rax
       test     rsi, rsi
       je       G_M000_IG174
 
G_M000_IG185:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rsi], rcx
       jne      G_M000_IG174
 
G_M000_IG186:
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      gword ptr [rbp-0x4E8], rax
 
G_M000_IG187:
       xor      rdi, rdi
       mov      gword ptr [rbp-0x3F0], rdi
 
G_M000_IG188:
       mov      gword ptr [rbp-0x3E8], rdi
 
G_M000_IG189:
       mov      gword ptr [rbp-0x3E0], rdi
       mov      edi, 89
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, gword ptr [rbp-0x4E8]
       mov      edi, 55
       mov      esi, 7
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x4E8]
       mov      edi, 56
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, rbx
       mov      edi, 57
       mov      esi, 4
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x490], rax
       mov      edi, 90
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, gword ptr [rbp-0x490]
       mov      edi, 58
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x498], rax
       mov      edi, 91
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x470]
 
G_M000_IG190:
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rcx, gword ptr [rbp-0x490]
       mov      edi, 59
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x4A0], rax
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG192
 
G_M000_IG191:
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 92
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x520], rax
       mov      rdx, gword ptr [rbp-0x478]
       mov      rcx, gword ptr [rbp-0x4A0]
       mov      edi, 60
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x520]
       mov      edi, 61
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG192:
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG194
 
G_M000_IG193:
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 93
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x528], rax
       mov      rdx, gword ptr [rbp-0x480]
       mov      rcx, gword ptr [rbp-0x4A0]
       mov      edi, 62
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x528]
       mov      edi, 63
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG194:
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG196
 
G_M000_IG195:
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 94
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x530], rax
       mov      rdx, gword ptr [rbp-0x488]
       mov      rcx, gword ptr [rbp-0x4A0]
       mov      edi, 64
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x530]
       mov      edi, 65
       mov      esi, 258
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG196:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG198
 
G_M000_IG197:
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 95
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x470]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x538], rax
       mov      rdx, gword ptr [rbp-0x478]
       mov      rcx, gword ptr [rbp-0x498]
       mov      edi, 66
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x538]
       mov      edi, 67
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x470]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG198:
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG200
 
G_M000_IG199:
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 96
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x470]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x540], rax
       mov      rdx, gword ptr [rbp-0x480]
       mov      rcx, gword ptr [rbp-0x498]
       mov      edi, 68
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x540]
       mov      edi, 69
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x470]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG200:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG202
 
G_M000_IG201:
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 97
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x470]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x548], rax
       mov      rdx, gword ptr [rbp-0x488]
       mov      rcx, gword ptr [rbp-0x498]
       mov      edi, 70
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x548]
       mov      edi, 71
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x470]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG202:
       mov      rax, qword ptr [rbp-0x60]
       add      rax, 1
       jo       G_M000_IG218
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x68]
 
G_M000_IG203:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG06
 
G_M000_IG204:
       mov      qword ptr [rbp-0x60], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       G_M000_IG34
 
G_M000_IG205:
       test     rdi, rdi
       jl       G_M000_IG218
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG217
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       mov      gword ptr [rbp-0x4E0], rdx
       jmp      G_M000_IG35
 
G_M000_IG206:
       mov      edi, 98
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyIter:Create(System.Object):RyuPy.Runtime.IPyIterator]
       mov      gword ptr [rbp-0x4D8], rax
       jmp      G_M000_IG208
 
G_M000_IG207:
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x518], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, rbx
       mov      edi, 76
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x518]
       mov      edi, 77
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
       mov      rax, gword ptr [rbp-0x4D8]
 
G_M000_IG208:
       mov      rdi, rax
       call     [RyuPy.Runtime.PyOps:IterNextOk(System.Object):bool]
       test     eax, eax
       je       G_M000_IG03
 
G_M000_IG209:
       mov      rdi, gword ptr [rbp-0x4D8]
       call     [RyuPy.Runtime.PyOps:IterValue(System.Object):System.Object]
       mov      gword ptr [rbp-0x468], rax
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG211
 
G_M000_IG210:
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 99
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x508], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, rbx
       mov      edi, 72
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x508]
       mov      edi, 73
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG211:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG213
 
G_M000_IG212:
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      edi, 100
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x510], rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, rbx
       mov      edi, 74
       mov      esi, 3
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rcx, rax
       mov      rdx, gword ptr [rbp-0x510]
       mov      edi, 75
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      rdx, rax
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rdi, gword ptr [rbp-0x468]
       call     [RyuPy.Runtime.PyOps:SetItem(System.Object,System.Object,System.Object)]
 
G_M000_IG213:
       mov      edi, 101
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       jne      G_M000_IG207
 
G_M000_IG214:
       mov      rax, gword ptr [rbp-0x4D8]
       jmp      G_M000_IG208
 
G_M000_IG215:
       mov      rdi, 0xD1FFAB1E
       mov      rbx, gword ptr [rdi]
       mov      rdi, r13
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, rbx
 
G_M000_IG216:
       add      rsp, 0x528
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG217:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG218:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 8106

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

