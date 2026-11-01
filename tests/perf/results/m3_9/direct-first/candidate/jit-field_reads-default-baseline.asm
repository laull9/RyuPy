; Assembly listing for method RyuPy.Generated.Module:m0_field_reads(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 240
       lea      rbp, [rsp+0xF0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xE0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xA0], zmm8
       vmovdqu  ymmword ptr [rbp-0x60], ymm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0xE8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x58], rax
       mov      rax, gword ptr [rbp-0x58]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      qword ptr [rbp-0x88], 1
       mov      rdi, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyNumericAccumulator:Create(System.Object):System.Object]
       mov      gword ptr [rbp-0xE0], rax
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xE8]
       dec      eax
       mov      dword ptr [rbp-0xE8], eax
       cmp      dword ptr [rbp-0xE8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xE8]
       mov      esi, 94
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x88], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x8C], eax
       mov      rax, qword ptr [rbp-0x78]
       cmp      rax, qword ptr [rbp-0x80]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x90], eax
       mov      rax, qword ptr [rbp-0x78]
       cmp      rax, qword ptr [rbp-0x80]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      eax, dword ptr [rbp-0x8C]
       and      eax, dword ptr [rbp-0x90]
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       cmp      dword ptr [rbp-0x8C], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x9C]
       and      eax, dword ptr [rbp-0x94]
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       mov      eax, dword ptr [rbp-0x98]
       or       eax, dword ptr [rbp-0xA0]
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       cmp      dword ptr [rbp-0xA4], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rsi, gword ptr [rbp-0x38]
       mov      edi, 3
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:GetAttr(int,System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rsi, gword ptr [rbp-0x38]
       mov      edi, 4
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:GetAttr(int,System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xC0], rax
       mov      rdx, gword ptr [rbp-0xB8]
       mov      rcx, gword ptr [rbp-0xC0]
       mov      edi, 5
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xC8], rax
       mov      rdi, gword ptr [rbp-0xE0]
       mov      rsi, gword ptr [rbp-0xC8]
       call     [RyuPy.Runtime.PyNumericAccumulator:Add(System.Object,System.Object)]
       mov      rax, qword ptr [rbp-0x78]
       add      rax, qword ptr [rbp-0x88]
       jo       SHORT G_M000_IG09
       mov      qword ptr [rbp-0x78], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0xE0]
       call     [RyuPy.Runtime.PyNumericAccumulator:Value(System.Object):System.Object]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xD8]
 
G_M000_IG08:
       add      rsp, 240
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 687

; Assembly listing for method RyuPy.Generated.Module:m0_field_reads(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x5e
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
; 28 inlinees with PGO data; 35 single block inlinees; 36 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 272
       mov      qword ptr [rsp+0x208], r15
       mov      qword ptr [rsp+0x200], r14
       mov      qword ptr [rsp+0x1F8], r13
       mov      qword ptr [rsp+0x1F0], r12
       mov      qword ptr [rsp+0x1E8], rbx
       lea      rbp, [rsp+0x110]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x80], ymm8
       mov      rbx, gword ptr [rbp+0xC8]
       mov      r14, qword ptr [rbp+0x88]
       mov      r13, qword ptr [rbp+0x80]
       mov      r12, qword ptr [rbp+0x78]
       mov      r15, bword ptr [rbp+0x20]
 
G_M000_IG02:
       test     r12, r12
       setg     r8b
       movzx    r8, r8b
       mov      dword ptr [rbp-0xAC], r8d
       jmp      G_M000_IG43
 
G_M000_IG03:
       xor      edi, edi
       jmp      G_M000_IG79
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rcx, rax
       mov      gword ptr [rbp-0xE0], rcx
       lea      rdi, bword ptr [rcx+0x08]
       mov      rsi, gword ptr [rbp-0x110]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, gword ptr [rbp-0xE0]
       mov      edi, dword ptr [rbp-0xA8]
       mov      dword ptr [rcx+0x10], edi
       jmp      G_M000_IG81
 
G_M000_IG05:
       lea      rax, [rcx+0x05]
       cmp      rax, 261
       jbe      SHORT G_M000_IG08
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x60]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG07:
       jmp      G_M000_IG82
 
G_M000_IG08:
       test     rax, rax
       jl       G_M000_IG138
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG137
       mov      rax, gword ptr [rax+8*rdi+0x10]
       jmp      SHORT G_M000_IG07
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG27
 
G_M000_IG10:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x50], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0x88], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x88]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG28
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG17
 
G_M000_IG12:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       test     rax, rax
       je       G_M000_IG27
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG15
 
G_M000_IG14:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x48], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG28
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG27
 
G_M000_IG16:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x40]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG28
 
G_M000_IG17:
       mov      rsi, 0xD1FFAB1E
       cmp      rdx, rsi
       jne      G_M000_IG27
 
G_M000_IG18:
       mov      rsi, rcx
       mov      gword ptr [rbp-0xD0], rsi
       test     rax, rax
       je       G_M000_IG27
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG27
 
G_M000_IG20:
       mov      gword ptr [rbp-0xD8], rax
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG25
 
G_M000_IG21:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG24
 
G_M000_IG22:
       mov      ecx, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0x94], ecx
       mov      edi, ecx
       add      edi, dword ptr [rax+0x08]
       js       G_M000_IG136
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0xF0], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0xD0]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x94]
       mov      rax, gword ptr [rbp-0xF0]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      rsi, gword ptr [rbp-0xD8]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rax, gword ptr [rbp-0xF0]
 
G_M000_IG23:
       jmp      SHORT G_M000_IG28
 
G_M000_IG24:
       mov      rax, rsi
       jmp      SHORT G_M000_IG23
       align    [0 bytes for IG50]
 
G_M000_IG25:
       cmp      dword ptr [rax+0x08], 0
       je       G_M000_IG135
 
G_M000_IG26:
       jmp      SHORT G_M000_IG23
 
G_M000_IG27:
       mov      rdx, rax
       mov      rsi, rcx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG28:
       movzx    rdi, byte  ptr [r15+0x18]
       test     edi, edi
       je       G_M000_IG84
 
G_M000_IG29:
       test     rax, rax
       je       SHORT G_M000_IG33
 
G_M000_IG30:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG33
 
G_M000_IG31:
       mov      gword ptr [rbp-0xC8], rax
       mov      rdi, qword ptr [rax+0x08]
       mov      rcx, qword ptr [r15+0x10]
       lea      rdx, [rcx+rdi]
       xor      rcx, rdx
       xor      rdi, rdx
       and      rdi, rcx
       mov      rax, gword ptr [rbp-0xC8]
       jl       SHORT G_M000_IG33
 
G_M000_IG32:
       mov      qword ptr [r15+0x10], rdx
       xor      rdi, rdi
       mov      gword ptr [r15+0x08], rdi
       jmp      G_M000_IG42
 
G_M000_IG33:
       mov      rsi, gword ptr [r15+0x08]
       test     rsi, rsi
       jne      SHORT G_M000_IG37
 
G_M000_IG34:
       mov      rcx, qword ptr [r15+0x10]
       mov      qword ptr [rbp-0xA0], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      G_M000_IG83
 
G_M000_IG35:
       mov      gword ptr [rbp-0xC8], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0xA0]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG36:
       lea      rdi, bword ptr [r15+0x08]
       mov      gword ptr [rbp-0x100], rsi
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x100]
       mov      rax, gword ptr [rbp-0xC8]
 
G_M000_IG37:
       mov      rdx, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xF8], rax
       lea      rdi, bword ptr [r15+0x08]
       mov      rsi, rax
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0xF8]
       test     rsi, rsi
       jne      G_M000_IG85
 
G_M000_IG38:
       xor      eax, eax
 
G_M000_IG39:
       mov      byte  ptr [r15+0x18], al
       test     rsi, rsi
       je       SHORT G_M000_IG42
 
G_M000_IG40:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      SHORT G_M000_IG42
 
G_M000_IG41:
       mov      rax, qword ptr [rsi+0x08]
       mov      qword ptr [r15+0x10], rax
 
G_M000_IG42:
       add      r14, r12
       jo       G_M000_IG138
       mov      r8d, dword ptr [rbp-0xAC]
 
G_M000_IG43:
       mov      edx, r8d
       cmp      r14, r13
       setl     sil
       movzx    rsi, sil
       and      esi, edx
       cmp      r14, r13
       setg     cl
       movzx    rcx, cl
       test     edx, edx
       sete     dl
       movzx    rdx, dl
       and      edx, ecx
       or       edx, esi
       je       G_M000_IG87
 
G_M000_IG44:
       mov      edx, dword ptr [(reloc)]
       test     edx, edx
       jle      SHORT G_M000_IG47
 
G_M000_IG45:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      rcx, rsi
       dec      edx
       mov      edi, edx
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG137
       cmp      dword ptr [rcx+4*rdi+0x10], 20
       je       SHORT G_M000_IG47
 
G_M000_IG46:
       cmp      edx, dword ptr [rsi+0x08]
       jae      G_M000_IG137
       mov      edx, edx
       mov      dword ptr [rsi+4*rdx+0x10], 20
 
G_M000_IG47:
       test     rbx, rbx
       je       G_M000_IG58
 
G_M000_IG48:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdx
       jne      G_M000_IG58
 
G_M000_IG49:
       mov      rdi, rbx
       mov      rdx, gword ptr [rbx+0x08]
       cmp      byte  ptr [rdx+0x74], 0
       jne      G_M000_IG58
       mov      r9, qword ptr [(reloc)]
       mov      qword ptr [rbp-0x30], r9
       xor      edx, edx
       mov      rsi, 0xD1FFAB1E
       mov      r10, gword ptr [rsi]
       jmp      SHORT G_M000_IG52
 
G_M000_IG50:
       mov      r9, qword ptr [rbp-0x30]
 
G_M000_IG51:
       inc      edx
       cmp      edx, 4
       jge      G_M000_IG109
 
G_M000_IG52:
       lea      esi, [rdx+0x0C]
       imul     rsi, rsi, 48
       lea      rsi, bword ptr [r10+rsi+0x10]
       cmp      byte  ptr [rsi+0x2C], 0
       je       SHORT G_M000_IG51
 
G_M000_IG53:
       mov      rcx, gword ptr [rsi]
       cmp      rcx, gword ptr [rbx+0x08]
       jne      SHORT G_M000_IG51
       cmp      qword ptr [rsi+0x20], r9
       jne      G_M000_IG102
       cmp      gword ptr [rsi+0x08], 0
       jne      G_M000_IG103
       cmp      byte  ptr [rsi+0x2D], 0
       jne      G_M000_IG104
       mov      esi, dword ptr [rsi+0x28]
       mov      rcx, gword ptr [rbx+0x10]
       cmp      dword ptr [rcx+0x08], esi
       jbe      SHORT G_M000_IG50
 
G_M000_IG54:
       cmp      esi, dword ptr [rcx+0x08]
       jae      G_M000_IG137
       mov      esi, esi
       mov      r11, gword ptr [rcx+8*rsi+0x10]
       test     r11, r11
       je       SHORT G_M000_IG57
 
G_M000_IG55:
       mov      rsi, 0xD1FFAB1E
       cmp      r11, gword ptr [rsi]
       mov      r9, qword ptr [rbp-0x30]
       je       SHORT G_M000_IG51
 
G_M000_IG56:
       mov      rax, r11
       jmp      SHORT G_M000_IG59
       align    [0 bytes for IG62]
 
G_M000_IG57:
       mov      r9, qword ptr [rbp-0x30]
       jmp      G_M000_IG51
 
G_M000_IG58:
       mov      rdi, rbx
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:GetAttr(System.Object,System.String):System.Object]
 
G_M000_IG59:
       mov      gword ptr [rbp+0x48], rax
       test     rbx, rbx
       je       G_M000_IG70
 
G_M000_IG60:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdx
       jne      G_M000_IG70
 
G_M000_IG61:
       mov      rdi, rbx
       mov      rdx, gword ptr [rbx+0x08]
       cmp      byte  ptr [rdx+0x74], 0
       jne      G_M000_IG70
       mov      r8, qword ptr [(reloc)]
       mov      qword ptr [rbp-0x38], r8
       xor      edx, edx
       mov      rsi, 0xD1FFAB1E
       mov      r10, gword ptr [rsi]
       jmp      SHORT G_M000_IG64
 
G_M000_IG62:
       mov      r8, qword ptr [rbp-0x38]
 
G_M000_IG63:
       inc      edx
       cmp      edx, 4
       jge      G_M000_IG117
 
G_M000_IG64:
       lea      esi, [rdx+0x10]
       imul     rsi, rsi, 48
       lea      rsi, bword ptr [r10+rsi+0x10]
       cmp      byte  ptr [rsi+0x2C], 0
       je       SHORT G_M000_IG63
 
G_M000_IG65:
       mov      rcx, gword ptr [rsi]
       cmp      rcx, gword ptr [rbx+0x08]
       jne      SHORT G_M000_IG63
       cmp      qword ptr [rsi+0x20], r8
       jne      G_M000_IG110
       cmp      gword ptr [rsi+0x08], 0
       jne      G_M000_IG111
       cmp      byte  ptr [rsi+0x2D], 0
       jne      G_M000_IG112
       mov      esi, dword ptr [rsi+0x28]
       mov      rcx, gword ptr [rbx+0x10]
       cmp      dword ptr [rcx+0x08], esi
       jbe      SHORT G_M000_IG62
 
G_M000_IG66:
       cmp      esi, dword ptr [rcx+0x08]
       jae      G_M000_IG137
       mov      esi, esi
       mov      rsi, gword ptr [rcx+8*rsi+0x10]
       test     rsi, rsi
       je       SHORT G_M000_IG69
 
G_M000_IG67:
       mov      rcx, 0xD1FFAB1E
       cmp      rsi, gword ptr [rcx]
       mov      r8, qword ptr [rbp-0x38]
       je       SHORT G_M000_IG63
 
G_M000_IG68:
       mov      rdx, rsi
       mov      r9, rdx
       mov      rax, r9
       jmp      SHORT G_M000_IG71
 
G_M000_IG69:
       mov      r8, qword ptr [rbp-0x38]
       jmp      G_M000_IG63
 
G_M000_IG70:
       mov      rdi, rbx
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:GetAttr(System.Object,System.String):System.Object]
 
G_M000_IG71:
       mov      rcx, gword ptr [rbp+0x48]
       test     rcx, rcx
       je       G_M000_IG27
 
G_M000_IG72:
       mov      rdx, qword ptr [rcx]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG11
 
G_M000_IG73:
       mov      rsi, qword ptr [rcx+0x08]
       test     rax, rax
       je       G_M000_IG27
 
G_M000_IG74:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG09
 
G_M000_IG75:
       mov      rax, qword ptr [rax+0x08]
       mov      qword ptr [rbp-0x58], rax
       lea      rcx, [rsi+rax]
       mov      qword ptr [rbp-0x60], rcx
       mov      rdi, rsi
       xor      rdi, rcx
       mov      rdx, rax
       xor      rdx, rcx
       and      rdi, rdx
       jge      G_M000_IG05
 
G_M000_IG76:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x70]
       mov      gword ptr [rbp-0x108], rax
       mov      ecx, dword ptr [rbp-0x68]
       mov      dword ptr [rbp-0xA4], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x80], xmm0
       lea      rdi, [rbp-0x80]
       mov      rsi, qword ptr [rbp-0x58]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x108]
       mov      esi, dword ptr [rbp-0xA4]
       mov      rdx, gword ptr [rbp-0x80]
       mov      rcx, qword ptr [rbp-0x78]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x110], rax
       mov      ecx, edx
       mov      dword ptr [rbp-0xA8], edx
       test     rax, rax
       jne      G_M000_IG118
 
G_M000_IG77:
       test     rax, rax
       jne      G_M000_IG127
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      G_M000_IG03
 
G_M000_IG78:
       mov      edi, -1
 
G_M000_IG79:
       test     edi, edi
       jg       G_M000_IG04
 
G_M000_IG80:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0xE8], rax
       mov      rdi, gword ptr [rbp-0x110]
       mov      esi, dword ptr [rbp-0xA8]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rcx, gword ptr [rbp-0xE8]
       mov      qword ptr [rcx+0x08], rax
 
G_M000_IG81:
       mov      rax, rcx
 
G_M000_IG82:
       jmp      G_M000_IG28
 
G_M000_IG83:
       test     rdx, rdx
       jl       G_M000_IG138
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG137
       mov      rsi, gword ptr [rdi+8*rsi+0x10]
       mov      gword ptr [rbp-0xC8], rax
       jmp      G_M000_IG36
 
G_M000_IG84:
       mov      rsi, gword ptr [r15+0x08]
       jmp      G_M000_IG37
 
G_M000_IG85:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       jne      G_M000_IG38
 
G_M000_IG86:
       mov      eax, 1
       jmp      G_M000_IG39
 
G_M000_IG87:
       cmp      byte  ptr [r15+0x18], 0
       je       G_M000_IG101
 
G_M000_IG88:
       mov      rcx, gword ptr [r15+0x08]
       test     rcx, rcx
       jne      SHORT G_M000_IG92
 
G_M000_IG89:
       mov      rbx, qword ptr [r15+0x10]
       lea      r14, [rbx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG100
 
G_M000_IG90:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      qword ptr [r13+0x08], rbx
 
G_M000_IG91:
       lea      rdi, bword ptr [r15+0x08]
       mov      rsi, r13
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rax, r13
       mov      rcx, rax
 
G_M000_IG92:
       mov      edx, dword ptr [(reloc)]
       test     edx, edx
       jle      SHORT G_M000_IG95
 
G_M000_IG93:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rdi
       lea      r8d, [rdx-0x01]
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG137
       cmp      dword ptr [rsi+4*r8+0x10], 21
       je       SHORT G_M000_IG95
 
G_M000_IG94:
       lea      esi, [rdx-0x01]
       cmp      esi, dword ptr [rdi+0x08]
       jae      G_M000_IG137
       mov      dword ptr [rdi+4*rsi+0x10], 21
 
G_M000_IG95:
       mov      rax, qword ptr [rbp+0xB0]
       test     rax, rax
       jle      SHORT G_M000_IG98
 
G_M000_IG96:
       movsxd   rdx, edx
       cmp      rax, rdx
       jg       SHORT G_M000_IG98
 
G_M000_IG97:
       dec      eax
       mov      dword ptr [(reloc)], eax
 
G_M000_IG98:
       mov      rax, rcx
 
G_M000_IG99:
       add      rsp, 488
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG100:
       test     r14, r14
       jl       G_M000_IG138
       mov      rdi, r14
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG137
       mov      r13, gword ptr [rsi+8*rdi+0x10]
       jmp      G_M000_IG91
 
G_M000_IG101:
       mov      rcx, gword ptr [r15+0x08]
       jmp      G_M000_IG92
 
G_M000_IG102:
       mov      r9, qword ptr [rbp-0x30]
       jmp      G_M000_IG51
 
G_M000_IG103:
       mov      rdx, gword ptr [rsi+0x10]
       mov      rsi, gword ptr [rsi+0x08]
       mov      rcx, rbx
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:CallGetter(int,RyuPy.Runtime.PyFunction,RyuPy.Runtime.PyClass,RyuPy.Runtime.PyInstance):System.Object]
       jmp      G_M000_IG59
 
G_M000_IG104:
       cmp      dword ptr [rsi+0x28], 0
       jl       SHORT G_M000_IG107
       mov      r11d, dword ptr [rsi+0x28]
       mov      rcx, gword ptr [rbx+0x10]
       cmp      dword ptr [rcx+0x08], r11d
       jbe      SHORT G_M000_IG105
       mov      rdx, rcx
       cmp      r11d, dword ptr [rdx+0x08]
       jae      G_M000_IG137
       mov      ecx, r11d
       mov      r11, gword ptr [rdx+8*rcx+0x10]
       test     r11, r11
       je       SHORT G_M000_IG105
       mov      rdx, 0xD1FFAB1E
       cmp      r11, gword ptr [rdx]
       je       SHORT G_M000_IG105
       mov      edx, 1
       jmp      SHORT G_M000_IG106
 
G_M000_IG105:
       xor      r11, r11
       xor      edx, edx
 
G_M000_IG106:
       test     edx, edx
       jne      SHORT G_M000_IG108
 
G_M000_IG107:
       mov      r11, gword ptr [rsi+0x18]
       mov      rax, r11
       jmp      G_M000_IG59
 
G_M000_IG108:
       mov      rax, r11
       jmp      G_M000_IG59
 
G_M000_IG109:
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:GetAttr(System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rcx, qword ptr [rbp-0x30]
       mov      rsi, rbx
       mov      edi, 3
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:FillGetAttr(int,RyuPy.Runtime.PyInstance,System.String,long)]
       mov      rax, gword ptr [rbp-0xB8]
       jmp      G_M000_IG59
 
G_M000_IG110:
       mov      r8, qword ptr [rbp-0x38]
       jmp      G_M000_IG63
 
G_M000_IG111:
       mov      rdx, gword ptr [rsi+0x10]
       mov      rsi, gword ptr [rsi+0x08]
       mov      rcx, rbx
       mov      edi, 4
       call     [RyuPy.Runtime.PyPic:CallGetter(int,RyuPy.Runtime.PyFunction,RyuPy.Runtime.PyClass,RyuPy.Runtime.PyInstance):System.Object]
       jmp      G_M000_IG71
 
G_M000_IG112:
       cmp      dword ptr [rsi+0x28], 0
       jl       SHORT G_M000_IG115
       mov      r9d, dword ptr [rsi+0x28]
       mov      rcx, gword ptr [rbx+0x10]
       cmp      dword ptr [rcx+0x08], r9d
       jbe      SHORT G_M000_IG113
       mov      rdx, rcx
       cmp      r9d, dword ptr [rdx+0x08]
       jae      G_M000_IG137
       mov      ecx, r9d
       mov      r9, gword ptr [rdx+8*rcx+0x10]
       test     r9, r9
       je       SHORT G_M000_IG113
       mov      rdx, 0xD1FFAB1E
       cmp      r9, gword ptr [rdx]
       je       SHORT G_M000_IG113
       mov      edx, 1
       jmp      SHORT G_M000_IG114
 
G_M000_IG113:
       xor      r9, r9
       xor      edx, edx
 
G_M000_IG114:
       test     edx, edx
       jne      SHORT G_M000_IG116
 
G_M000_IG115:
       mov      r9, gword ptr [rsi+0x18]
       mov      rax, r9
       jmp      G_M000_IG71
 
G_M000_IG116:
       mov      rax, r9
       jmp      G_M000_IG71
 
G_M000_IG117:
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyObject:GetAttr(System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xC0], rax
       mov      rcx, qword ptr [rbp-0x38]
       mov      rsi, rbx
       mov      edi, 4
       mov      rdx, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyPic:FillGetAttr(int,RyuPy.Runtime.PyInstance,System.String,long)]
       mov      rax, gword ptr [rbp-0xC0]
       jmp      G_M000_IG71
 
G_M000_IG118:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG119
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG120
 
G_M000_IG119:
       mov      edx, ecx
       jmp      SHORT G_M000_IG126
 
G_M000_IG120:
       cmp      edi, 2
       je       SHORT G_M000_IG121
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG137
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG122
 
G_M000_IG121:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
 
G_M000_IG122:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG123
       mov      edx, -1
       jmp      SHORT G_M000_IG125
 
G_M000_IG123:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jbe      SHORT G_M000_IG124
       mov      edx, 1
       jmp      SHORT G_M000_IG125
 
G_M000_IG124:
       xor      edx, edx
 
G_M000_IG125:
       imul     edx, ecx
 
G_M000_IG126:
       test     edx, edx
       jge      G_M000_IG77
       jmp      G_M000_IG04
 
G_M000_IG127:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG128
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG129
 
G_M000_IG128:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0xA8]
       jmp      G_M000_IG79
 
G_M000_IG129:
       cmp      edi, 2
       je       SHORT G_M000_IG130
       cmp      dword ptr [rax+0x08], 0
       jbe      SHORT G_M000_IG137
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG131
 
G_M000_IG130:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x110]
 
G_M000_IG131:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG132
       mov      edx, -1
       jmp      SHORT G_M000_IG134
 
G_M000_IG132:
       test     rdi, rdi
       jge      SHORT G_M000_IG133
       mov      edx, 1
       jmp      SHORT G_M000_IG134
 
G_M000_IG133:
       xor      edx, edx
 
G_M000_IG134:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG79
 
G_M000_IG135:
       mov      rax, 0xD1FFAB1E
       jmp      G_M000_IG23
 
G_M000_IG136:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG137:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG138:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2981

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_field_reads(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; optimized using Synthesized PGO
; rsp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
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

