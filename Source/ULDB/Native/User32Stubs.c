/* ULDB: stand-ins for the few Win32 user32 functions upstream calls directly through
 * P/Invoke ("user32.dll"). Mono's WinForms emulated them on Linux; ULDB has no native
 * windows, so they do nothing: window messages are dropped (upstream only uses them to
 * pause redrawing or tweak native controls), MessageBeep is silent. ULDB.Platform's
 * DllImportResolver maps "user32.dll" to this libuser32.so.
 *
 * On x86-64 Linux all the declared signatures (int or pointer arguments and results)
 * share one calling convention, so one definition per name serves every overload. */

#include <stdint.h>

#define EXPORT __attribute__((visibility("default")))

EXPORT intptr_t SendMessage(void *hwnd, intptr_t msg, intptr_t wparam, intptr_t lparam) { return 0; }
EXPORT intptr_t PostMessage(void *hwnd, intptr_t msg, intptr_t wparam, intptr_t lparam) { return 0; }
EXPORT int LockWindowUpdate(void *hwnd) { return 0; }
EXPORT int GetWindowLong(void *hwnd, int index) { return 0; }
EXPORT int SetWindowLong(void *hwnd, int index, int value) { return 0; }
EXPORT int MessageBeep(unsigned int type) { return 1; }
