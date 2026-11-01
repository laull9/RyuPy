; Assembly listing for method RyuPy.Generated.Module:m0_tuple_read(System.Object,System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x4e
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 2
; 0 inlinees with PGO data; 0 single block inlinees; 5 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 32
       mov      qword ptr [rsp+0x118], r15
       mov      qword ptr [rsp+0x110], r14
       mov      qword ptr [rsp+0x108], r13
       mov      qword ptr [rsp+0x100], rbx
       lea      rbp, [rsp+0x20]
       mov      rax, gword ptr [rbp+0xC8]
       mov      rdi, gword ptr [rbp+0xB8]
       mov      r8, qword ptr [rbp+0xA8]
       mov      rcx, qword ptr [rbp+0x88]
       mov      rdx, qword ptr [rbp+0x80]
       mov      rsi, qword ptr [rbp+0x78]
 
G_M000_IG02:
       test     rsi, rsi
       setg     r9b
       movzx    r9, r9b
       mov      r10d, r9d
       cmp      rcx, rdx
       setl     r11b
       movzx    r11, r11b
       and      r11d, r10d
       cmp      rcx, rdx
       setg     bl
       movzx    rbx, bl
       test     r10d, r10d
       sete     r15b
       movzx    r15, r15b
       and      ebx, r15d
       or       r11d, ebx
       je       SHORT G_M000_IG08
 
G_M000_IG03:
       mov      r11d, dword ptr [(reloc)]
       jmp      SHORT G_M000_IG06
       align    [0 bytes for IG04]
 
G_M000_IG04:
       mov      r10d, ebx
       mov      dword ptr [rdi+4*r10+0x10], 16
 
G_M000_IG05:
       mov      rdi, rax
       add      rcx, rsi
       jo       G_M000_IG17
       mov      r10d, r9d
       cmp      rcx, rdx
       setl     bl
       movzx    rbx, bl
       and      r10d, ebx
       cmp      rcx, rdx
       setg     bl
       movzx    rbx, bl
       and      ebx, r15d
       or       r10d, ebx
       je       SHORT G_M000_IG08
 
G_M000_IG06:
       test     r11d, r11d
       jle      SHORT G_M000_IG05
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      r10, rdi
       lea      ebx, [r11-0x01]
       mov      r14d, ebx
       mov      r13d, dword ptr [r10+0x08]
       cmp      r14d, r13d
       jae      SHORT G_M000_IG16
       mov      r13d, r14d
       cmp      dword ptr [r10+4*r13+0x10], 16
       je       SHORT G_M000_IG05
       jmp      SHORT G_M000_IG04
 
G_M000_IG08:
       mov      r11d, dword ptr [(reloc)]
       test     r11d, r11d
       jle      SHORT G_M000_IG11
 
G_M000_IG09:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       lea      ebx, [r11-0x01]
       mov      edx, ebx
       mov      r13d, dword ptr [rcx+0x08]
       cmp      edx, r13d
       jae      SHORT G_M000_IG16
       cmp      dword ptr [rcx+4*rdx+0x10], 17
       je       SHORT G_M000_IG11
 
G_M000_IG10:
       mov      ecx, ebx
       mov      dword ptr [rax+4*rcx+0x10], 17
 
G_M000_IG11:
       test     r8, r8
       jle      SHORT G_M000_IG14
 
G_M000_IG12:
       movsxd   rax, r11d
       cmp      r8, rax
       jg       SHORT G_M000_IG14
 
G_M000_IG13:
       dec      r8d
       mov      dword ptr [(reloc)], r8d
 
G_M000_IG14:
       mov      rax, rdi
 
G_M000_IG15:
       add      rsp, 256
       pop      rbx
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG16:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG17:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 370
