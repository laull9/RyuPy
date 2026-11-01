; Assembly listing for method RyuPy.Generated.Module:m0_field_reads(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; fully interruptible
; No PGO data
; 3 inlinees with PGO data; 20 single block inlinees; 25 inlinees without PGO data

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
       vmovdqu  ymmword ptr [rbp-0x90], ymm8
       mov      r15, rdi
       mov      rbx, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       xor      eax, eax
       test     r13, r13
       je       G_M000_IG11
 
G_M000_IG03:
       mov      rdi, qword ptr [r13]
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jne      SHORT G_M000_IG10
 
G_M000_IG04:
       mov      edx, 1
       mov      rax, qword ptr [r13+0x08]
       mov      qword ptr [rbp-0x30], rax
       jmp      SHORT G_M000_IG07
 
G_M000_IG05:
       mov      eax, 1
       mov      rdi, qword ptr [r13+0x08]
       mov      rcx, rdi
       mov      rdx, rcx
       mov      ecx, eax
 
G_M000_IG06:
       add      r12, 1
       jo       G_M000_IG89
       mov      qword ptr [rbp-0x30], rdx
       mov      edx, ecx
 
G_M000_IG07:
       cmp      r12, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG83
 
G_M000_IG08:
       mov      dword ptr [rbp-0x34], edx
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     rbx, rbx
       jne      SHORT G_M000_IG12
 
G_M000_IG09:
       mov      rdi, rbx
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:GetAttr(System.Object,System.String):System.Object]
       jmp      G_M000_IG30
 
G_M000_IG10:
       xor      edx, edx
       mov      qword ptr [rbp-0x30], rax
       jmp      SHORT G_M000_IG07
       align    [0 bytes for IG14]
 
G_M000_IG11:
       xor      edx, edx
       mov      dword ptr [rbp-0x34], edx
       mov      qword ptr [rbp-0x30], rax
       mov      edx, dword ptr [rbp-0x34]
       jmp      SHORT G_M000_IG07
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG09
 
G_M000_IG13:
       mov      rdi, rbx
       mov      rsi, gword ptr [rbx+0x08]
       cmp      byte  ptr [rsi+0x74], 0
       jne      SHORT G_M000_IG09
       mov      rax, qword ptr [(reloc)]
       mov      qword ptr [rbp-0x40], rax
       xor      esi, esi
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
 
G_M000_IG14:
       lea      edx, [rsi+0x0C]
       imul     rdx, rdx, 48
       lea      rdx, bword ptr [rcx+rdx+0x10]
       cmp      byte  ptr [rdx+0x2C], 0
       je       SHORT G_M000_IG22
 
G_M000_IG15:
       mov      r8, gword ptr [rdx]
       cmp      r8, gword ptr [rbx+0x08]
       jne      SHORT G_M000_IG22
       cmp      qword ptr [rdx+0x20], rax
       je       SHORT G_M000_IG17
 
G_M000_IG16:
       mov      rax, qword ptr [rbp-0x40]
       jmp      SHORT G_M000_IG22
 
G_M000_IG17:
       cmp      gword ptr [rdx+0x08], 0
       jne      G_M000_IG28
       cmp      byte  ptr [rdx+0x2D], 0
       jne      G_M000_IG25
       mov      edx, dword ptr [rdx+0x28]
       mov      r8, gword ptr [rbx+0x10]
       cmp      dword ptr [r8+0x08], edx
       ja       SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rax, qword ptr [rbp-0x40]
       jmp      SHORT G_M000_IG22
 
G_M000_IG19:
       cmp      edx, dword ptr [r8+0x08]
       jae      G_M000_IG88
       mov      edx, edx
       mov      rdx, gword ptr [r8+8*rdx+0x10]
       test     rdx, rdx
       jne      SHORT G_M000_IG21
 
G_M000_IG20:
       mov      rax, qword ptr [rbp-0x40]
       jmp      SHORT G_M000_IG22
 
G_M000_IG21:
       mov      r8, 0xD1FFAB1E
       cmp      rdx, gword ptr [r8]
       mov      rax, qword ptr [rbp-0x40]
       jne      SHORT G_M000_IG24
 
G_M000_IG22:
       inc      esi
       cmp      esi, 4
       jl       G_M000_IG14
 
G_M000_IG23:
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:GetAttr(System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rcx, qword ptr [rbp-0x40]
       mov      rsi, rbx
       mov      edi, 3
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:FillGetAttr(int,RyuPy.Runtime.PyInstance,System.String,long)]
       mov      rax, gword ptr [rbp-0xB8]
       jmp      G_M000_IG30
       align    [0 bytes for IG33]
 
G_M000_IG24:
       mov      rax, rdx
       jmp      G_M000_IG30
 
G_M000_IG25:
       cmp      dword ptr [rdx+0x28], 0
       jl       SHORT G_M000_IG26
       mov      eax, dword ptr [rdx+0x28]
       mov      r8, gword ptr [rbx+0x10]
       cmp      dword ptr [r8+0x08], eax
       jbe      SHORT G_M000_IG26
       mov      rdi, r8
       cmp      eax, dword ptr [rdi+0x08]
       jae      G_M000_IG88
       mov      esi, eax
       mov      rax, gword ptr [rdi+8*rsi+0x10]
       test     rax, rax
       je       SHORT G_M000_IG26
       mov      rdi, 0xD1FFAB1E
       cmp      rax, gword ptr [rdi]
       jne      SHORT G_M000_IG27
 
G_M000_IG26:
       mov      rax, gword ptr [rdx+0x18]
       jmp      G_M000_IG30
 
G_M000_IG27:
       jmp      G_M000_IG30
 
G_M000_IG28:
       mov      rax, gword ptr [rdx+0x08]
       mov      gword ptr [rbp-0xD0], rax
       mov      rcx, gword ptr [rdx+0x10]
       mov      gword ptr [rbp-0xD8], rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      gword ptr [rbp-0xC0], rdx
       mov      r8, gword ptr [rdx+0x28]
       test     r8, r8
       jne      SHORT G_M000_IG29
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r8, rax
       mov      gword ptr [rbp-0xE0], r8
       mov      rdi, gword ptr [rbp-0xC0]
       lea      rdi, bword ptr [rdi+0x28]
       mov      rsi, r8
       call     CORINFO_HELP_ASSIGN_REF
       mov      r8, gword ptr [rbp-0xE0]
       mov      rax, r8
       mov      r8, rax
 
G_M000_IG29:
       mov      gword ptr [rbp-0xC8], r8
       mov      rdi, r8
       mov      rdx, rbx
       xor      esi, esi
       call     CORINFO_HELP_ARRADDR_ST
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, 0xD1FFAB1E
       mov      r9, gword ptr [r9]
       mov      rdi, gword ptr [rbp-0xD0]
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdx, rbx
       mov      rcx, gword ptr [rbp-0xC8]
       call     [RyuPy.Runtime.PyObject:InvokePrepared(RyuPy.Runtime.PyFunction,RyuPy.Runtime.PyClass,System.Object,System.Object[],System.String[],System.Object[]):System.Object]
 
G_M000_IG30:
       mov      gword ptr [rbp-0xB0], rax
       test     rbx, rbx
       je       G_M000_IG49
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      G_M000_IG49
 
G_M000_IG32:
       mov      rdi, rbx
       mov      rsi, gword ptr [rbx+0x08]
       cmp      byte  ptr [rsi+0x74], 0
       jne      G_M000_IG49
       mov      rcx, qword ptr [(reloc)]
       mov      qword ptr [rbp-0x48], rcx
       xor      esi, esi
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
 
G_M000_IG33:
       lea      r8d, [rsi+0x10]
       imul     r8, r8, 48
       lea      r8, bword ptr [rdx+r8+0x10]
       cmp      byte  ptr [r8+0x2C], 0
       je       SHORT G_M000_IG41
 
G_M000_IG34:
       mov      r9, gword ptr [r8]
       cmp      r9, gword ptr [rbx+0x08]
       jne      SHORT G_M000_IG41
       cmp      qword ptr [r8+0x20], rcx
       je       SHORT G_M000_IG36
 
G_M000_IG35:
       mov      rcx, qword ptr [rbp-0x48]
       jmp      SHORT G_M000_IG41
 
G_M000_IG36:
       cmp      gword ptr [r8+0x08], 0
       jne      G_M000_IG47
       cmp      byte  ptr [r8+0x2D], 0
       jne      G_M000_IG44
       mov      r8d, dword ptr [r8+0x28]
       mov      r9, gword ptr [rbx+0x10]
       cmp      dword ptr [r9+0x08], r8d
       ja       SHORT G_M000_IG38
 
G_M000_IG37:
       mov      rcx, qword ptr [rbp-0x48]
       jmp      SHORT G_M000_IG41
 
G_M000_IG38:
       cmp      r8d, dword ptr [r9+0x08]
       jae      G_M000_IG88
       mov      r8d, r8d
       mov      r8, gword ptr [r9+8*r8+0x10]
       test     r8, r8
       jne      SHORT G_M000_IG40
 
G_M000_IG39:
       mov      rcx, qword ptr [rbp-0x48]
       jmp      SHORT G_M000_IG41
 
G_M000_IG40:
       mov      r9, 0xD1FFAB1E
       cmp      r8, gword ptr [r9]
       mov      rcx, qword ptr [rbp-0x48]
       jne      SHORT G_M000_IG43
 
G_M000_IG41:
       inc      esi
       cmp      esi, 4
       jl       G_M000_IG33
 
G_M000_IG42:
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:GetAttr(System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rcx, qword ptr [rbp-0x48]
       mov      rsi, rbx
       mov      edi, 4
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:FillGetAttr(int,RyuPy.Runtime.PyInstance,System.String,long)]
       mov      rdx, gword ptr [rbp-0xE8]
       jmp      G_M000_IG50
 
G_M000_IG43:
       mov      rdx, r8
       jmp      G_M000_IG50
 
G_M000_IG44:
       cmp      dword ptr [r8+0x28], 0
       jl       SHORT G_M000_IG45
       mov      edx, dword ptr [r8+0x28]
       mov      r9, gword ptr [rbx+0x10]
       cmp      dword ptr [r9+0x08], edx
       jbe      SHORT G_M000_IG45
       mov      rdi, r9
       cmp      edx, dword ptr [rdi+0x08]
       jae      G_M000_IG88
       mov      esi, edx
       mov      rdx, gword ptr [rdi+8*rsi+0x10]
       test     rdx, rdx
       je       SHORT G_M000_IG45
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, gword ptr [rdi]
       jne      SHORT G_M000_IG46
 
G_M000_IG45:
       mov      rdx, gword ptr [r8+0x18]
       jmp      G_M000_IG50
 
G_M000_IG46:
       jmp      G_M000_IG50
 
G_M000_IG47:
       mov      rcx, gword ptr [r8+0x08]
       mov      gword ptr [rbp-0x100], rcx
       mov      rdx, gword ptr [r8+0x10]
       mov      gword ptr [rbp-0x108], rdx
       mov      rdi, 0xD1FFAB1E
       mov      r8, gword ptr [rdi]
       mov      gword ptr [rbp-0xF0], r8
       mov      r9, gword ptr [r8+0x30]
       test     r9, r9
       jne      SHORT G_M000_IG48
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      r9, rax
       mov      gword ptr [rbp-0x110], r9
       mov      rdi, gword ptr [rbp-0xF0]
       lea      rdi, bword ptr [rdi+0x30]
       mov      rsi, r9
       call     CORINFO_HELP_ASSIGN_REF
       mov      r9, gword ptr [rbp-0x110]
       mov      rax, r9
       mov      r9, rax
 
G_M000_IG48:
       mov      gword ptr [rbp-0xF8], r9
       mov      rdi, r9
       mov      rdx, rbx
       xor      esi, esi
       call     CORINFO_HELP_ARRADDR_ST
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, 0xD1FFAB1E
       mov      r9, gword ptr [r9]
       mov      rdi, gword ptr [rbp-0x100]
       mov      rsi, gword ptr [rbp-0x108]
       mov      rdx, rbx
       mov      rcx, gword ptr [rbp-0xF8]
       call     [RyuPy.Runtime.PyObject:InvokePrepared(RyuPy.Runtime.PyFunction,RyuPy.Runtime.PyClass,System.Object,System.Object[],System.String[],System.Object[]):System.Object]
       mov      rdx, rax
       jmp      SHORT G_M000_IG50
 
G_M000_IG49:
       mov      rdi, rbx
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:GetAttr(System.Object,System.String):System.Object]
       mov      rsi, rax
       mov      rdx, rsi
 
G_M000_IG50:
       mov      rax, gword ptr [rbp-0xB0]
       test     rax, rax
       je       G_M000_IG71
 
G_M000_IG51:
       mov      rcx, qword ptr [rax]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
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
       mov      qword ptr [rbp-0x68], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x70], rcx
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
       jl       G_M000_IG89
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG88
       mov      rdx, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG56
 
G_M000_IG55:
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      rdi, qword ptr [rbp-0x70]
       mov      qword ptr [rdx+0x08], rdi
 
G_M000_IG56:
       mov      rax, rdx
       jmp      SHORT G_M000_IG58
 
G_M000_IG57:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       lea      rdi, [rbp-0x80]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x80]
       mov      gword ptr [rbp-0x120], rax
       mov      ecx, dword ptr [rbp-0x78]
       mov      dword ptr [rbp-0xA4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0x90], xmm0
       lea      rdi, [rbp-0x90]
       mov      rsi, qword ptr [rbp-0x68]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x120]
       mov      esi, dword ptr [rbp-0xA4]
       mov      rdx, gword ptr [rbp-0x90]
       mov      rcx, qword ptr [rbp-0x88]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
 
G_M000_IG58:
       jmp      G_M000_IG72
 
G_M000_IG59:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG71
 
G_M000_IG60:
       vmovsd   xmm0, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x60], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x98], xmm1
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x98]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x60]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG72
 
G_M000_IG61:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      SHORT G_M000_IG67
 
G_M000_IG62:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm0
       test     rdx, rdx
       je       G_M000_IG71
 
G_M000_IG63:
       cmp      qword ptr [rdx], rsi
       jne      SHORT G_M000_IG65
 
G_M000_IG64:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x58], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x50]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x58]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG72
 
G_M000_IG65:
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG71
 
G_M000_IG66:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm1
       mov      rdi, rsi
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG72
 
G_M000_IG67:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
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
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG72:
       mov      ecx, dword ptr [rbp-0x34]
       test     ecx, ecx
       je       SHORT G_M000_IG76
 
G_M000_IG73:
       test     rax, rax
       je       SHORT G_M000_IG76
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG76
 
G_M000_IG75:
       mov      gword ptr [rbp-0x118], rax
       mov      rdi, qword ptr [rax+0x08]
       mov      rdx, qword ptr [rbp-0x30]
       lea      rsi, [rdx+rdi]
       mov      r8, rdx
       xor      r8, rsi
       xor      rdi, rsi
       and      rdi, r8
       mov      qword ptr [rbp-0x30], rdx
       mov      rax, gword ptr [rbp-0x118]
       jl       SHORT G_M000_IG76
       mov      rdx, rsi
       xor      r13, r13
       jmp      G_M000_IG06
 
G_M000_IG76:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     ecx, edi
       je       G_M000_IG80
       mov      rdx, qword ptr [rbp-0x30]
       lea      rdi, [rdx+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG77
       test     rdi, rdi
       jl       G_M000_IG89
       mov      rsi, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG88
       mov      r13, gword ptr [rdi+8*rsi+0x10]
       mov      gword ptr [rbp-0x118], rax
       jmp      SHORT G_M000_IG78
 
G_M000_IG77:
       mov      gword ptr [rbp-0x118], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      rax, qword ptr [rbp-0x30]
       mov      qword ptr [r13+0x08], rax
 
G_M000_IG78:
       mov      rdx, gword ptr [rbp-0x118]
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r13, rax
       test     r13, r13
       jne      SHORT G_M000_IG81
 
G_M000_IG79:
       xor      edi, edi
       mov      ecx, edi
       mov      rdx, qword ptr [rbp-0x30]
       jmp      G_M000_IG06
 
G_M000_IG80:
       mov      gword ptr [rbp-0x118], rax
       jmp      SHORT G_M000_IG78
 
G_M000_IG81:
       mov      rdi, qword ptr [r13]
       mov      rax, 0xD1FFAB1E
       cmp      rdi, rax
       je       G_M000_IG05
 
G_M000_IG82:
       xor      eax, eax
       xor      edi, edi
       mov      ecx, edi
       mov      rdx, qword ptr [rbp-0x30]
       jmp      G_M000_IG06
 
G_M000_IG83:
       test     r13, r13
       sete     dil
       movzx    rdi, dil
       test     edx, edi
       je       SHORT G_M000_IG86
 
G_M000_IG84:
       mov      rax, qword ptr [rbp-0x30]
       lea      rdi, [rax+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG85
       test     rdi, rdi
       jl       SHORT G_M000_IG89
       mov      rsi, rdi
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      SHORT G_M000_IG88
       mov      r13, gword ptr [rdi+8*rsi+0x10]
       jmp      SHORT G_M000_IG86
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      rbx, qword ptr [rbp-0x30]
       mov      qword ptr [r13+0x08], rbx
 
G_M000_IG86:
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG87:
       add      rsp, 248
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG88:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG89:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2398

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_field_reads(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rsp based frame
; partially interruptible
; No PGO data

G_M000_IG01:
       push     rax
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rsi]
       cmp      edx, 1
       jbe      SHORT G_M000_IG04
       mov      rsi, gword ptr [rsi+0x08]
       call     [RyuPy.Generated.Module:m0_field_reads(System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 35

