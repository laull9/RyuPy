; Assembly listing for method RyuPy.Generated.Module:m0_choose_unknown(System.Object,System.Object):System.Object (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 80
       lea      rbp, [rsp+0x50]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x20], rax
       mov      edi, 29
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x24], eax
       cmp      dword ptr [rbp-0x24], 0
       jne      SHORT G_M000_IG03
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      edi, 30
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x10]
       mov      gword ptr [rbp-0x18], rax
       jmp      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      edi, 32
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x30], rax
       mov      rax, gword ptr [rbp-0x30]
       mov      gword ptr [rbp-0x18], rax
 
G_M000_IG05:
       mov      edi, 33
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x40], rax
       mov      rdx, gword ptr [rbp-0x18]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 3
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x48], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, qword ptr [rbp-0x20]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x38]
 
G_M000_IG06:
       add      rsp, 80
       pop      rbp
       ret      
 
; Total bytes of code 242

; Assembly listing for method RyuPy.Generated.Module:m0_choose_unknown(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 80
       lea      rbp, [rsp+0x50]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x20], rax
       mov      edi, 29
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x24], eax
       cmp      dword ptr [rbp-0x24], 0
       jne      SHORT G_M000_IG03
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 30
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x10]
       mov      gword ptr [rbp-0x18], rax
       jmp      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      edi, 32
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x30], rax
       mov      rax, gword ptr [rbp-0x30]
       mov      gword ptr [rbp-0x18], rax
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 33
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x40], rax
       mov      rdx, gword ptr [rbp-0x18]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 3
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x48], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, qword ptr [rbp-0x20]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x38]
 
G_M000_IG06:
       add      rsp, 80
       pop      rbp
       ret      
 
; Total bytes of code 272

; Assembly listing for method RyuPy.Generated.Module:m0_choose_unknown(System.Object,System.Object):System.Object (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 25964
; 19 inlinees with PGO data; 9 single block inlinees; 8 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 72
       lea      rbp, [rsp+0x70]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      r13, 0xD1FFAB1E
       mov      edi, dword ptr [r13]
       test     edi, edi
       jle      SHORT G_M000_IG05
 
G_M000_IG03:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG83
       cmp      dword ptr [rcx+4*rdx+0x10], 29
       je       SHORT G_M000_IG05
 
G_M000_IG04:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG83
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 29
 
G_M000_IG05:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG66
 
G_M000_IG06:
       mov      edi, dword ptr [r13]
       test     edi, edi
       jle      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG83
       cmp      dword ptr [rcx+4*rdx+0x10], 30
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG83
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 30
 
G_M000_IG09:
       mov      edi, dword ptr [r13]
       test     edi, edi
       jle      SHORT G_M000_IG12
 
G_M000_IG10:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rax, rsi
       dec      edi
       mov      ecx, edi
       cmp      ecx, dword ptr [rax+0x08]
       jae      G_M000_IG83
       cmp      dword ptr [rax+4*rcx+0x10], 33
       je       SHORT G_M000_IG12
 
G_M000_IG11:
       cmp      edi, dword ptr [rsi+0x08]
       jae      G_M000_IG83
       mov      edi, edi
       mov      dword ptr [rsi+4*rdi+0x10], 33
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       test     r15, r15
       je       G_M000_IG52
 
G_M000_IG13:
       mov      rbx, qword ptr [r15]
       mov      rdi, 0xD1FFAB1E
       cmp      rbx, rdi
       jne      G_M000_IG43
 
G_M000_IG14:
       mov      rbx, qword ptr [r15+0x08]
       test     rdx, rdx
       je       G_M000_IG38
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      G_M000_IG38
 
G_M000_IG16:
       mov      r15, qword ptr [rdx+0x08]
       lea      r12, [rbx+r15]
       mov      rdi, rbx
       xor      rdi, r12
       mov      rsi, r15
       xor      rsi, r12
       and      rdi, rsi
       jge      G_M000_IG80
       lea      rdi, [rbp-0x50]
       mov      rsi, rbx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x50]
       mov      ebx, dword ptr [rbp-0x48]
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       lea      rdi, [rbp-0x60]
       mov      rsi, r15
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r15, gword ptr [rbp-0x60]
       mov      r9d, dword ptr [rbp-0x58]
       mov      rdi, r12
       or       rdi, r15
       je       G_M000_IG67
       mov      edi, ebx
       shr      edi, 31
       mov      esi, r9d
       shr      esi, 31
       cmp      edi, esi
       jne      G_M000_IG68
       test     r12, r12
       je       G_M000_IG31
 
G_M000_IG17:
       lea      rdi, bword ptr [r12+0x10]
       mov      esi, dword ptr [r12+0x08]
 
G_M000_IG18:
       test     r15, r15
       je       G_M000_IG32
 
G_M000_IG19:
       lea      rcx, bword ptr [r15+0x10]
       mov      r8d, dword ptr [r15+0x08]
 
G_M000_IG20:
       mov      edx, ebx
       call     [System.Numerics.BigInteger:Add(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rbx, rax
       mov      r15d, edx
 
G_M000_IG21:
       test     rbx, rbx
       je       G_M000_IG73
       movsxd   rdi, r15d
       mov      rax, 0xD1FFAB1E
       xor      rax, rdi
       jl       SHORT G_M000_IG23
 
G_M000_IG22:
       mov      eax, dword ptr [rbx+0x08]
       mov      ecx, eax
       cmp      ecx, 2
       jle      SHORT G_M000_IG33
 
G_M000_IG23:
       mov      eax, r15d
 
G_M000_IG24:
       test     eax, eax
       jl       SHORT G_M000_IG29
 
G_M000_IG25:
       test     rbx, rbx
       je       G_M000_IG77
       mov      rax, 0xD1FFAB1E
       xor      rdi, rax
       jl       SHORT G_M000_IG27
 
G_M000_IG26:
       mov      eax, dword ptr [rbx+0x08]
       mov      edi, eax
       cmp      edi, 2
       jle      G_M000_IG36
 
G_M000_IG27:
       mov      ecx, r15d
 
G_M000_IG28:
       test     ecx, ecx
       jle      G_M000_IG79
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       lea      rdi, bword ptr [r12+0x08]
       mov      rsi, rbx
       call     CORINFO_HELP_ASSIGN_REF
       mov      dword ptr [r12+0x10], r15d
 
G_M000_IG30:
       jmp      G_M000_IG61
 
G_M000_IG31:
       xor      rdi, rdi
       xor      esi, esi
       jmp      G_M000_IG18
 
G_M000_IG32:
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      G_M000_IG20
 
G_M000_IG33:
       cmp      ecx, 2
       jne      G_M000_IG74
       mov      ecx, dword ptr [rbx+0x14]
       shl      rcx, 32
       mov      edx, dword ptr [rbx+0x10]
       or       rcx, rdx
 
G_M000_IG34:
       mov      rax, 0xD1FFAB1E
       cmp      rcx, rax
       jae      G_M000_IG76
       jmp      G_M000_IG75
 
G_M000_IG35:
       imul     eax, r15d
       jmp      G_M000_IG24
 
G_M000_IG36:
       cmp      edi, 2
       jne      G_M000_IG78
       mov      edi, dword ptr [rbx+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [rbx+0x10]
       or       rdi, rcx
 
G_M000_IG37:
       mov      rax, 0xD1FFAB1E
       mov      rcx, rdi
       shr      rcx, 63
       mov      edx, -1
       cmp      rdi, rax
       cmovb    ecx, edx
       imul     ecx, r15d
       jmp      G_M000_IG28
 
G_M000_IG38:
       mov      rsi, rdx
       test     rsi, rsi
       je       SHORT G_M000_IG41
 
G_M000_IG39:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG41
 
G_M000_IG40:
       xor      rsi, rsi
 
G_M000_IG41:
       test     rsi, rsi
       je       G_M000_IG65
 
G_M000_IG42:
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rbx
       vmovsd   qword ptr [rbp-0x68], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x68]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG61
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       cmp      rbx, rdi
       jne      G_M000_IG52
 
G_M000_IG44:
       vmovsd   xmm0, qword ptr [r15+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rdx, rdx
       je       SHORT G_M000_IG47
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rdx], rdi
       jne      SHORT G_M000_IG47
 
G_M000_IG46:
       vmovsd   xmm1, qword ptr [rdx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      G_M000_IG61
 
G_M000_IG47:
       mov      rsi, rdx
       test     rsi, rsi
       je       SHORT G_M000_IG50
 
G_M000_IG48:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG50
 
G_M000_IG49:
       xor      rsi, rsi
 
G_M000_IG50:
       test     rsi, rsi
       je       G_M000_IG65
 
G_M000_IG51:
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x70], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x70]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x30]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r12, rax
       jmp      SHORT G_M000_IG61
 
G_M000_IG52:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG55
 
G_M000_IG53:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG55
 
G_M000_IG54:
       xor      rdi, rdi
 
G_M000_IG55:
       test     rdi, rdi
       je       SHORT G_M000_IG65
 
G_M000_IG56:
       mov      rsi, rdx
       test     rsi, rsi
       je       SHORT G_M000_IG59
 
G_M000_IG57:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       je       SHORT G_M000_IG59
 
G_M000_IG58:
       xor      rsi, rsi
 
G_M000_IG59:
       test     rsi, rsi
       je       SHORT G_M000_IG65
 
G_M000_IG60:
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r12, rax
 
G_M000_IG61:
       test     r14, r14
       jle      SHORT G_M000_IG63
 
G_M000_IG62:
       movsxd   rax, dword ptr [r13]
       cmp      r14, rax
       jg       SHORT G_M000_IG63
       dec      r14d
       mov      dword ptr [r13], r14d
 
G_M000_IG63:
       mov      rax, r12
 
G_M000_IG64:
       add      rsp, 72
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG65:
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      r12, rax
       jmp      SHORT G_M000_IG61
 
G_M000_IG66:
       mov      edi, 32
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r15, gword ptr [rdi]
       jmp      G_M000_IG09
 
G_M000_IG67:
       movsxd   rdi, ebx
       movsxd   rax, r9d
       add      rdi, rax
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      rbx, rax
       mov      r15d, edx
       jmp      G_M000_IG21
 
G_M000_IG68:
       test     r12, r12
       jne      SHORT G_M000_IG69
       xor      rdi, rdi
       xor      esi, esi
       jmp      SHORT G_M000_IG70
 
G_M000_IG69:
       lea      rdi, bword ptr [r12+0x10]
       mov      esi, dword ptr [r12+0x08]
 
G_M000_IG70:
       test     r15, r15
       jne      SHORT G_M000_IG71
       xor      rcx, rcx
       xor      r8d, r8d
       jmp      SHORT G_M000_IG72
 
G_M000_IG71:
       lea      rcx, bword ptr [r15+0x10]
       mov      r8d, dword ptr [r15+0x08]
 
G_M000_IG72:
       neg      r9d
       mov      edx, ebx
       call     [System.Numerics.BigInteger:Subtract(System.ReadOnlySpan`1[uint],int,System.ReadOnlySpan`1[uint],int):System.Numerics.BigInteger]
       mov      rbx, rax
       mov      r15d, edx
       jmp      G_M000_IG21
 
G_M000_IG73:
       movsxd   rdi, r15d
       jmp      G_M000_IG25
 
G_M000_IG74:
       test     eax, eax
       je       G_M000_IG83
       mov      ecx, dword ptr [rbx+0x10]
       jmp      G_M000_IG34
 
G_M000_IG75:
       mov      eax, -1
       jmp      G_M000_IG35
 
G_M000_IG76:
       mov      rax, 0xD1FFAB1E
       cmp      rcx, rax
       seta     al
       movzx    rax, al
       jmp      G_M000_IG35
 
G_M000_IG77:
       mov      rcx, rdi
       mov      rdi, 0xD1FFAB1E
       xor      eax, eax
       mov      edx, -1
       cmp      rcx, rdi
       cmovl    eax, edx
       mov      ecx, eax
       jmp      G_M000_IG28
 
G_M000_IG78:
       test     eax, eax
       je       G_M000_IG83
       mov      edi, dword ptr [rbx+0x10]
       jmp      G_M000_IG37
 
G_M000_IG79:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r12, rax
       mov      rdi, rbx
       mov      esi, r15d
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      qword ptr [r12+0x08], rax
       jmp      G_M000_IG30
 
G_M000_IG80:
       lea      rbx, [r12+0x05]
       cmp      rbx, 261
       jbe      SHORT G_M000_IG81
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r12
       mov      r12, rax
       jmp      SHORT G_M000_IG82
 
G_M000_IG81:
       test     rbx, rbx
       jl       SHORT G_M000_IG84
       mov      rsi, rbx
       cmp      rsi, 262
       jae      SHORT G_M000_IG83
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       test     rbx, rbx
       jl       SHORT G_M000_IG84
       mov      rdi, rbx
       mov      r12, gword ptr [rsi+8*rdi+0x10]
 
G_M000_IG82:
       jmp      G_M000_IG30
 
G_M000_IG83:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG84:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1585
