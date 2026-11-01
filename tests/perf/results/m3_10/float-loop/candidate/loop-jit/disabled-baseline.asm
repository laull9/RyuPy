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
       sub      rsp, 312
       lea      rbp, [rsp+0x160]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x110], zmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       xor      r15d, r15d
       mov      qword ptr [rbp-0x50], r15
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
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       add      r12, 1
       jo       G_M000_IG70
 
G_M000_IG04:
       cmp      r12, r13
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG67
 
G_M000_IG05:
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r15, gword ptr [rdi]
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x130], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x140], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, 0xD1FFAB1E
       mov      rdx, gword ptr [rcx]
       mov      rcx, rdx
       lea      rdi, [rbp-0x60]
       mov      rsi, gword ptr [rbp-0x140]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x50]
       mov      rsi, gword ptr [rbp-0x60]
       mov      rdx, gword ptr [rbp-0x58]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x138], rax
       mov      rdi, gword ptr [rbp-0x130]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       mov      rdi, gword ptr [rbp-0x138]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x30]
       mov      qword ptr [rbp-0x40], rax
       jmp      G_M000_IG15
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rax, qword ptr [rbp-0x38]
       mov      qword ptr [rdx+0x08], rax
       mov      gword ptr [rbp-0x148], rdx
 
G_M000_IG07:
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x148]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       test     rax, rax
       je       G_M000_IG18
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG18
 
G_M000_IG09:
       test     rdx, rdx
       je       G_M000_IG19
 
G_M000_IG10:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG19
 
G_M000_IG11:
       test     r15, r15
       je       G_M000_IG19
 
G_M000_IG12:
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG19
 
G_M000_IG13:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm0
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
 
G_M000_IG14:
       mov      r15, rax
       mov      rax, qword ptr [rbp-0x38]
       add      rax, 1
       jo       G_M000_IG70
       mov      rcx, rax
       mov      rax, qword ptr [rbp-0x40]
 
G_M000_IG15:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG03
 
G_M000_IG16:
       mov      qword ptr [rbp-0x38], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       G_M000_IG06
 
G_M000_IG17:
       test     rdi, rdi
       jl       G_M000_IG70
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG69
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       mov      gword ptr [rbp-0x148], rdx
       jmp      G_M000_IG07
 
G_M000_IG18:
       test     rax, rax
       je       G_M000_IG43
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG37
 
G_M000_IG20:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG43
 
G_M000_IG21:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG35
 
G_M000_IG22:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x80], rax
       lea      rcx, [rbp-0xA0]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rcx], r8
       mov      rcx, qword ptr [rbp-0xA0]
       mov      qword ptr [rbp-0x98], rcx
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
       jne      SHORT G_M000_IG25
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG23
       test     rax, rax
       jl       G_M000_IG70
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG69
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG24
 
G_M000_IG23:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x98]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG24:
       jmp      G_M000_IG28
 
G_M000_IG25:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       lea      rdi, [rbp-0x90]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x90]
       mov      gword ptr [rbp-0x150], rax
       mov      ecx, dword ptr [rbp-0x88]
       mov      dword ptr [rbp-0x124], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xB0], xmm0
       lea      rdi, [rbp-0xB0]
       mov      rsi, qword ptr [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB0]
       mov      r9d, dword ptr [rbp-0xA8]
       mov      rcx, gword ptr [rbp-0x150]
       test     rcx, rcx
       jne      SHORT G_M000_IG30
 
G_M000_IG26:
       test     rax, rax
       jne      SHORT G_M000_IG29
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xC0], xmm0
       movsxd   rsi, dword ptr [rbp-0x124]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xC0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xC0]
       mov      esi, dword ptr [rbp-0xB8]
 
G_M000_IG27:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG28:
       jmp      G_M000_IG44
 
G_M000_IG29:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG31
 
G_M000_IG30:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG31:
       test     rax, rax
       je       SHORT G_M000_IG34
 
G_M000_IG32:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG33:
       mov      edx, dword ptr [rbp-0x124]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG27
 
G_M000_IG34:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG33
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG43
 
G_M000_IG36:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x78], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xC8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xC8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x78]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG44
 
G_M000_IG37:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      SHORT G_M000_IG43
 
G_M000_IG38:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x68], xmm0
       test     rdx, rdx
       je       SHORT G_M000_IG43
 
G_M000_IG39:
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG41
 
G_M000_IG40:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x70], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x68]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG44
 
G_M000_IG41:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG43
 
G_M000_IG42:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xD0], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xD0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG44
 
G_M000_IG43:
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG44:
       test     r15, r15
       je       G_M000_IG65
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG55
 
G_M000_IG46:
       mov      rsi, qword ptr [r15+0x08]
       test     rdx, rdx
       je       G_M000_IG65
 
G_M000_IG47:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG53
 
G_M000_IG48:
       mov      r15, qword ptr [rdx+0x08]
       lea      rax, [rsi+r15]
       mov      qword ptr [rbp-0xF0], rax
       mov      rcx, rsi
       xor      rcx, rax
       mov      rdx, r15
       xor      rdx, rax
       and      rcx, rdx
       jl       SHORT G_M000_IG51
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       ja       SHORT G_M000_IG49
       test     rcx, rcx
       jl       G_M000_IG70
       mov      rdi, rcx
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG69
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG50
 
G_M000_IG49:
       call     CORINFO_HELP_NEWSFAST
       mov      r15, qword ptr [rbp-0xF0]
       mov      qword ptr [rax+0x08], r15
 
G_M000_IG50:
       jmp      SHORT G_M000_IG52
 
G_M000_IG51:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x100], xmm0
       lea      rdi, [rbp-0x100]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x100]
       mov      gword ptr [rbp-0x158], rax
       mov      ecx, dword ptr [rbp-0xF8]
       mov      dword ptr [rbp-0x128], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x110], xmm0
       lea      rdi, [rbp-0x110]
       mov      rsi, r15
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x158]
       mov      esi, dword ptr [rbp-0x128]
       mov      rdx, gword ptr [rbp-0x110]
       mov      rcx, qword ptr [rbp-0x108]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG52:
       jmp      G_M000_IG66
 
G_M000_IG53:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG65
 
G_M000_IG54:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xE8], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x118], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x118]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xE8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG66
 
G_M000_IG55:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [r15], rcx
       jne      SHORT G_M000_IG61
 
G_M000_IG56:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm0
       test     rdx, rdx
       je       G_M000_IG65
 
G_M000_IG57:
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG59
 
G_M000_IG58:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xE0], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xD8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xE0]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG66
 
G_M000_IG59:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG65
 
G_M000_IG60:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x120], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x120]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG66
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      SHORT G_M000_IG65
 
G_M000_IG62:
       mov      rdi, r15
       test     rdx, rdx
       je       SHORT G_M000_IG65
 
G_M000_IG63:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG65
 
G_M000_IG64:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG66
 
G_M000_IG65:
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG66:
       jmp      G_M000_IG14
 
G_M000_IG67:
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r15
 
G_M000_IG68:
       add      rsp, 312
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG69:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG70:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1958

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
       sub      rsp, 392
       lea      rbp, [rsp+0x1B0]
       xor      eax, eax
       mov      qword ptr [rbp-0x138], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x130], zmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqu  ymmword ptr [rbp-0x70], ymm8
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
       mov      gword ptr [rbp-0x180], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, r12
       mov      rdx, r12
       lea      rdi, [rbp-0x68]
       mov      rsi, gword ptr [rbp-0x180]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x58]
       mov      rsi, gword ptr [rbp-0x68]
       mov      rdx, gword ptr [rbp-0x60]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x168], rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r13, rax
       mov      rdi, gword ptr [rbp-0x168]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x30], rax
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rcx, gword ptr [rbp-0x160]
       mov      rsi, r14
       mov      edi, 2
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:CallMethod1(int,System.Object,System.String,System.Object):System.Object]
       add      r13, 1
       jo       G_M000_IG76
       mov      rax, qword ptr [rbp-0x30]
 
G_M000_IG04:
       cmp      r13, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG73
 
G_M000_IG05:
       lea      rdi, [r13+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG07
 
G_M000_IG06:
       test     rdi, rdi
       jl       G_M000_IG76
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG75
       mov      rcx, gword ptr [rcx+8*rdi+0x10]
       mov      gword ptr [rbp-0x190], rcx
       jmp      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      qword ptr [rcx+0x08], r13
       mov      gword ptr [rbp-0x190], rcx
 
G_M000_IG08:
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, rbx
       mov      rsi, gword ptr [rbp-0x190]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x158], rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x160], rax
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x170], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0x188], rax
       lea      rdi, bword ptr [rax+0x10]
       mov      rsi, gword ptr [rbp-0x158]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, r12
       mov      rdx, r12
       lea      rdi, [rbp-0x80]
       mov      rsi, gword ptr [rbp-0x188]
       call     [RyuPy.Runtime.PyOps:FlattenArgs(System.Object[],System.Object[],System.Object[]):System.ValueTuple`3[System.Object[],System.String[],System.Object[]]]
       mov      rcx, gword ptr [rbp-0x70]
       mov      rsi, gword ptr [rbp-0x80]
       mov      rdx, gword ptr [rbp-0x78]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:BuiltinDispatch(System.String,System.Object[],System.String[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x178], rax
       mov      rdi, gword ptr [rbp-0x170]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x38], rax
       mov      rdi, gword ptr [rbp-0x178]
 
G_M000_IG09:
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      rcx, qword ptr [rbp-0x38]
       mov      qword ptr [rbp-0x48], rax
       jmp      G_M000_IG20
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rax, qword ptr [rbp-0x40]
       mov      qword ptr [rdx+0x08], rax
       mov      gword ptr [rbp-0x198], rdx
 
G_M000_IG11:
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x158]
       mov      rsi, gword ptr [rbp-0x198]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       test     rax, rax
       je       G_M000_IG24
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG24
 
G_M000_IG13:
       test     rdx, rdx
       je       G_M000_IG25
 
G_M000_IG14:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG25
 
G_M000_IG15:
       mov      rcx, gword ptr [rbp-0x160]
       test     rcx, rcx
       je       G_M000_IG23
 
G_M000_IG16:
       cmp      qword ptr [rcx], rdi
       je       SHORT G_M000_IG18
 
G_M000_IG17:
       mov      gword ptr [rbp-0x160], rcx
       jmp      G_M000_IG25
 
G_M000_IG18:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmulsd   xmm0, xmm0, xmm1
       vaddsd   xmm0, xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm0
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
 
G_M000_IG19:
       mov      gword ptr [rbp-0x160], rax
       mov      rdi, qword ptr [rbp-0x40]
       add      rdi, 1
       jo       G_M000_IG76
       mov      rcx, rdi
       mov      rax, qword ptr [rbp-0x48]
 
G_M000_IG20:
       cmp      rcx, rax
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG03
 
G_M000_IG21:
       mov      qword ptr [rbp-0x40], rcx
       lea      rdi, [rcx+0x05]
       cmp      rdi, 261
       ja       G_M000_IG10
 
G_M000_IG22:
       test     rdi, rdi
       jl       G_M000_IG76
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       cmp      rdi, 262
       jae      G_M000_IG75
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       mov      gword ptr [rbp-0x198], rdx
       jmp      G_M000_IG11
 
G_M000_IG23:
       mov      gword ptr [rbp-0x160], rcx
       jmp      SHORT G_M000_IG25
 
G_M000_IG24:
       test     rax, rax
       je       G_M000_IG49
 
G_M000_IG25:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG43
 
G_M000_IG26:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG49
 
G_M000_IG27:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG41
 
G_M000_IG28:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0xA0], rax
       lea      rcx, [rbp-0xC0]
       mov      rdx, rsi
       mulx     rdx, r8, rax
       mov      qword ptr [rcx], r8
       mov      rcx, qword ptr [rbp-0xC0]
       mov      qword ptr [rbp-0xB8], rcx
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
       jne      SHORT G_M000_IG31
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG29
       test     rax, rax
       jl       G_M000_IG76
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG75
       mov      rdx, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG30
 
G_M000_IG29:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0xB8]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG30:
       jmp      G_M000_IG34
 
G_M000_IG31:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xB0], xmm0
       lea      rdi, [rbp-0xB0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB0]
       mov      gword ptr [rbp-0x1A0], rax
       mov      ecx, dword ptr [rbp-0xA8]
       mov      dword ptr [rbp-0x14C], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xD0], xmm0
       lea      rdi, [rbp-0xD0]
       mov      rsi, qword ptr [rbp-0xA0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xD0]
       mov      r9d, dword ptr [rbp-0xC8]
       mov      rcx, gword ptr [rbp-0x1A0]
       test     rcx, rcx
       jne      SHORT G_M000_IG36
 
G_M000_IG32:
       test     rax, rax
       jne      SHORT G_M000_IG35
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xE0], xmm0
       movsxd   rsi, dword ptr [rbp-0x14C]
       movsxd   rdi, r9d
       imul     rsi, rdi
       lea      rdi, [rbp-0xE0]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0xE0]
       mov      esi, dword ptr [rbp-0xD8]
 
G_M000_IG33:
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      rdx, rax
 
G_M000_IG34:
       jmp      G_M000_IG50
 
G_M000_IG35:
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG37
 
G_M000_IG36:
       lea      rdi, bword ptr [rcx+0x10]
       mov      esi, dword ptr [rcx+0x08]
 
G_M000_IG37:
       test     rax, rax
       je       SHORT G_M000_IG40
 
G_M000_IG38:
       lea      rcx, bword ptr [rax+0x10]
       mov      r8d, dword ptr [rax+0x08]
 
G_M000_IG39:
       mov      edx, dword ptr [rbp-0x14C]
       call     [System.Numerics.BigInteger:Multiply(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       jmp      SHORT G_M000_IG33
 
G_M000_IG40:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG39
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG49
 
G_M000_IG42:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xE8], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xE8]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      G_M000_IG50
 
G_M000_IG43:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      SHORT G_M000_IG49
 
G_M000_IG44:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x88], xmm0
       test     rdx, rdx
       je       SHORT G_M000_IG49
 
G_M000_IG45:
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG47
 
G_M000_IG46:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG50
 
G_M000_IG47:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG49
 
G_M000_IG48:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xF0], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xF0]
       vmulsd   xmm0, xmm1, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       jmp      SHORT G_M000_IG50
 
G_M000_IG49:
       mov      rsi, rax
       mov      edi, 3
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
       mov      rdx, rax
 
G_M000_IG50:
       mov      rax, gword ptr [rbp-0x160]
       test     rax, rax
       je       G_M000_IG71
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG61
 
G_M000_IG52:
       mov      rsi, qword ptr [rax+0x08]
       test     rdx, rdx
       je       G_M000_IG71
 
G_M000_IG53:
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG59
 
G_M000_IG54:
       mov      rax, qword ptr [rdx+0x08]
       mov      qword ptr [rbp-0x110], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x118], rcx
       mov      rdx, rsi
       xor      rdx, rcx
       mov      r8, rax
       xor      r8, rcx
       and      rdx, r8
       jl       SHORT G_M000_IG57
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       ja       SHORT G_M000_IG55
       test     rax, rax
       jl       G_M000_IG76
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG75
       mov      rax, gword ptr [rdx+8*rdi+0x10]
       jmp      SHORT G_M000_IG56
 
G_M000_IG55:
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x118]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG56:
       jmp      G_M000_IG58
 
G_M000_IG57:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x128], xmm0
       lea      rdi, [rbp-0x128]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x128]
       mov      gword ptr [rbp-0x1A8], rax
       mov      ecx, dword ptr [rbp-0x120]
       mov      dword ptr [rbp-0x150], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x138], xmm0
       lea      rdi, [rbp-0x138]
       mov      rsi, qword ptr [rbp-0x110]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x1A8]
       mov      esi, dword ptr [rbp-0x150]
       mov      rdx, gword ptr [rbp-0x138]
       mov      rcx, qword ptr [rbp-0x130]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG58:
       jmp      G_M000_IG72
 
G_M000_IG59:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rdx], rcx
       jne      G_M000_IG71
 
G_M000_IG60:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x108], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x140], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x140]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x108]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG72
 
G_M000_IG61:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      SHORT G_M000_IG67
 
G_M000_IG62:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xF8], xmm0
       test     rdx, rdx
       je       G_M000_IG71
 
G_M000_IG63:
       cmp      qword ptr [rdx], rcx
       jne      SHORT G_M000_IG65
 
G_M000_IG64:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x100], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0xF8]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x100]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG72
 
G_M000_IG65:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG71
 
G_M000_IG66:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x148], xmm1
       mov      rdi, rcx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x148]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xF8]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG72
 
G_M000_IG67:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG71
 
G_M000_IG68:
       mov      rdi, rax
       test     rdx, rdx
       je       SHORT G_M000_IG71
 
G_M000_IG69:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG71
 
G_M000_IG70:
       mov      rsi, rdx
       call     [System.String:Concat(System.String,System.String):System.String]
       jmp      SHORT G_M000_IG72
 
G_M000_IG71:
       mov      rsi, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
 
G_M000_IG72:
       jmp      G_M000_IG19
 
G_M000_IG73:
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r14
 
G_M000_IG74:
       add      rsp, 392
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG75:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG76:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2385

