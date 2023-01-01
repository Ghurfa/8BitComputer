# Calling Convention/Stack stuff
- No base pointer register (like gcc -fomit-frame-pointer)
  - All local vars are referenced relative to the stack pointer
- Stack grows upward