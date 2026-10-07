using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace LolKey;

public static class KeyboardEngine
{
    // ==========================================
    // WIN32 CONSTANTS & STRUCTS (64-BIT ALIGNED)
    // ==========================================
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    private const ushort VK_BACK = 0x08;
    private const ushort VK_TAB = 0x09;
    private const ushort VK_RETURN = 0x0D;
    private const ushort VK_SPACE = 0x20;
    private const ushort VK_Z = 0x5A;
    private const ushort VK_F11 = 0x7A;

    // Special magic token to identify our own simulated keystrokes
    private static readonly UIntPtr MAGIC_EXTRA_INFO = (UIntPtr)0x5A454E; // "ZEN"

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    // Explicit 40-byte layout for 64-bit Windows SendInput
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct INPUT
    {
        [FieldOffset(0)]
        public uint type;

        [FieldOffset(8)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    // ==========================================
    // ENGINE EVENTS & STATE
    // ==========================================
    public static event Action<string, string, int>? OnWordRescued;
    public static event Action? OnStatusChanged;

    private static IntPtr _hookID = IntPtr.Zero;
    private static LowLevelKeyboardProc? _hookDelegate;

    private static readonly StringBuilder _currentWord = new();
    private static string _previousWord = "";
    private static readonly object _syncLock = new();

    private static bool _ctrlShiftPressed = false;

    // Single unified dictionary for direct toxic replacement
    private static readonly Dictionary<string, string> _dictToxic = new(StringComparer.OrdinalIgnoreCase);

    public static void Start()
    {
        LoadDictionary();
        VietnameseEngine.UpdateConfig(ConfigManager.Current);

        _hookDelegate = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        IntPtr hMod = GetModuleHandle(curModule?.ModuleName);
        _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _hookDelegate, hMod, 0);
    }

    public static void Stop()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }

    public static void ResetTypingBuffers()
    {
        VietnameseEngine.ClearBuffer();
        lock (_syncLock)
        {
            _currentWord.Clear();
            _previousWord = "";
        }
    }

    public static void ToggleVietnamese(bool playBeep = true)
    {
        ConfigManager.Current.EnableVietnamese = !ConfigManager.Current.EnableVietnamese;
        ConfigManager.Save();
        ResetTypingBuffers();
        if (playBeep)
        {
            PlayBeep(ConfigManager.Current.EnableVietnamese ? 1100 : 700);
        }
        OnStatusChanged?.Invoke();
    }

    public static void LoadDictionary()
    {
        lock (_syncLock)
        {
            _dictToxic.Clear();

            string dictPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "toxic_dict.json");
            if (File.Exists(dictPath))
            {
                try
                {
                    string json = File.ReadAllText(dictPath, Encoding.UTF8);
                    using JsonDocument doc = JsonDocument.Parse(json);

                    // Load Wholesome entries or root properties
                    if (doc.RootElement.TryGetProperty("Wholesome", out var wElem))
                    {
                        foreach (var prop in wElem.EnumerateObject())
                        {
                            _dictToxic[prop.Name] = prop.Value.GetString() ?? "";
                        }
                    }
                    else
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.String)
                                _dictToxic[prop.Name] = prop.Value.GetString() ?? "";
                        }
                    }
                }
                catch { }
            }

            // Fallback to internal AES-256 encrypted dictionary (100% standalone single-file support)
            if (_dictToxic.Count == 0)
            {
                LoadEmbeddedEncryptedDictionary();
            }

            // Ultimate fallback defaults
            if (_dictToxic.Count == 0)
            {
                _dictToxic["dm"] = "dmmm";
                _dictToxic["đm"] = "dmmm";
                _dictToxic["dmm"] = "dmmm";
                _dictToxic["đmm"] = "dmmm";
                _dictToxic["dcm"] = "dcmmm";
                _dictToxic["đcm"] = "dcmmm";
                _dictToxic["vcl"] = "vcll";
                _dictToxic["vl"] = "vll";
                _dictToxic["ngu"] = "nguu";
                _dictToxic["suc vat"] = "succ vatt";
                _dictToxic["súc vật"] = "súcc vậtt";
                _dictToxic["cut"] = "cutt";
                _dictToxic["cút"] = "cứtt";
                _dictToxic["me may"] = "mẹe màyy";
                _dictToxic["mẹ mày"] = "mẹe màyy";
                _dictToxic["an cut"] = "cứtt";
                _dictToxic["oc cho"] = "ócc chóo";
                _dictToxic["óc chó"] = "ócc chóo";
                _dictToxic["cc"] = "cặcc";
                _dictToxic["cặc"] = "cặcc";
                _dictToxic["clm"] = "clmm";
            }
        }
    }

    private static void LoadEmbeddedEncryptedDictionary()
    {
        try
        {
            const string payload = "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";
            byte[] cipher = Convert.FromBase64String(payload);
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] key = sha.ComputeHash(Encoding.UTF8.GetBytes("LOLKey_ZenSecret_2026_AntiToxic_Key"));
            byte[] iv = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC, 0xBA, 0x09, 0x87, 0x65, 0x43, 0x21 };

            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            string json = Encoding.UTF8.GetString(plain);

            using JsonDocument doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Wholesome", out var wElem))
            {
                foreach (var prop in wElem.EnumerateObject())
                {
                    _dictToxic[prop.Name] = prop.Value.GetString() ?? "";
                }
            }
        }
        catch { }
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            try
            {
                KBDLLHOOKSTRUCT kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                // Ignore simulated keystrokes from this engine
                if (kbd.dwExtraInfo == MAGIC_EXTRA_INFO)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                int msg = (int)wParam;
                bool isKeyDown = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                bool isKeyUp = (msg == WM_KEYUP || msg == WM_SYSKEYUP);

                bool isShiftCode = (kbd.vkCode == 0x10 || kbd.vkCode == 0xA0 || kbd.vkCode == 0xA1);
                bool isCtrlCode = (kbd.vkCode == 0x11 || kbd.vkCode == 0xA2 || kbd.vkCode == 0xA3);

                // On KeyUp: reset hotkey latch and exit immediately
                if (isKeyUp)
                {
                    if (isShiftCode || isCtrlCode)
                    {
                        _ctrlShiftPressed = false;
                    }
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                if (isKeyDown)
                {
                    // 1. HOTKEY: Ctrl + Shift (UniKey classic toggle)
                    if (ConfigManager.Current.SwitchKey == SwitchKeyMode.CtrlShift)
                    {
                        bool isTriggeringCtrlShift = (isShiftCode && (GetAsyncKeyState(0x11) & 0x8000) != 0) ||
                                                     (isCtrlCode && (GetAsyncKeyState(0x10) & 0x8000) != 0);
                        if (isTriggeringCtrlShift)
                        {
                            if (!_ctrlShiftPressed)
                            {
                                _ctrlShiftPressed = true;
                                ToggleVietnamese(playBeep: true);
                            }
                            return CallNextHookEx(_hookID, nCode, wParam, lParam);
                        }
                    }
                    // 2. HOTKEY: Alt + Z
                    else if (ConfigManager.Current.SwitchKey == SwitchKeyMode.AltZ)
                    {
                        if (kbd.vkCode == VK_Z && (GetAsyncKeyState(0x12) & 0x8000) != 0)
                        {
                            ToggleVietnamese(playBeep: true);
                            return (IntPtr)1; // Swallow Alt+Z
                        }
                    }

                    // Hotkey F11: Toggle Anti-Toxic protection
                    if (kbd.vkCode == VK_F11)
                    {
                        ConfigManager.Current.IsEnabled = !ConfigManager.Current.IsEnabled;
                        ConfigManager.Save();
                        PlayBeep(ConfigManager.Current.IsEnabled ? 1000 : 500);
                        OnStatusChanged?.Invoke();
                        return (IntPtr)1;
                    }

                    // If modifier key itself was pressed (Shift, Ctrl, Alt, Win), pass through immediately
                    if (isShiftCode || isCtrlCode || kbd.vkCode == 0x12 || kbd.vkCode == 0xA4 || kbd.vkCode == 0xA5 ||
                        kbd.vkCode == 0x5B || kbd.vkCode == 0x5C)
                    {
                        return CallNextHookEx(_hookID, nCode, wParam, lParam);
                    }

                    // Shortcut pass-through (UniKey standard):
                    // If Ctrl, Alt, or Win is held, this is a system shortcut (Ctrl+C, Ctrl+V, Alt+Tab, etc.)
                    // Reset buffer and pass through immediately without processing Vietnamese or text replacement.
                    bool ctrlHeld = (GetAsyncKeyState(0x11) & 0x8000) != 0;
                    bool altHeld = (GetAsyncKeyState(0x12) & 0x8000) != 0;
                    bool winHeld = (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
                    if (ctrlHeld || altHeld || winHeld)
                    {
                        VietnameseEngine.ClearBuffer();
                        lock (_syncLock)
                        {
                            _currentWord.Clear();
                            _previousWord = "";
                        }
                        return CallNextHookEx(_hookID, nCode, wParam, lParam);
                    }

                    // Cursor navigation & edit keys (Arrows, Home, End, PageUp, PageDown, Delete, Esc) -> Reset buffer
                    if (IsNavigationOrResetKey(kbd.vkCode))
                    {
                        VietnameseEngine.ClearBuffer();
                        lock (_syncLock)
                        {
                            _currentWord.Clear();
                            _previousWord = "";
                        }
                        return CallNextHookEx(_hookID, nCode, wParam, lParam);
                    }

                    lock (_syncLock)
                    {
                        // Backspace tracking
                        if (kbd.vkCode == VK_BACK)
                        {
                            VietnameseEngine.ProcessBackspace();
                            if (_currentWord.Length > 0)
                            {
                                _currentWord.Length--;
                            }
                            else if (!string.IsNullOrEmpty(_previousWord))
                            {
                                _previousWord = "";
                            }
                            return CallNextHookEx(_hookID, nCode, wParam, lParam);
                        }

                        // Delimiter triggers (Space, Enter, Tab, Punctuation)
                        bool isSpace = (kbd.vkCode == VK_SPACE);
                        bool isEnter = (kbd.vkCode == VK_RETURN);
                        bool isTab = (kbd.vkCode == VK_TAB);
                        bool isPunc = IsPunctuationVk(kbd.vkCode);
                        bool isDelimiter = isSpace || isEnter || isTab || isPunc;

                        if (isDelimiter)
                        {
                            VietnameseEngine.ClearBuffer();

                            string current = _currentWord.ToString();

                            if (!string.IsNullOrEmpty(current) && ConfigManager.Current.IsEnabled)
                            {
                                string normalizedCurrent = NormalizeWord(current);
                                string normalizedTwoWords = !string.IsNullOrEmpty(_previousWord)
                                    ? NormalizeWord(_previousWord + " " + current)
                                    : "";

                                string matchedToxic = "";
                                string replacement = "";
                                int charsToDelete = 0;

                                // 1. Two-word phrase check (e.g. "suc vat", "me may", "oc cho")
                                if (!string.IsNullOrEmpty(normalizedTwoWords) && TryGetReplacement(normalizedTwoWords, out string repTwo))
                                {
                                    matchedToxic = _previousWord + " " + current;
                                    replacement = repTwo;
                                    charsToDelete = current.Length + 1 + _previousWord.Length;
                                    _previousWord = "";
                                    _currentWord.Clear();
                                }
                                // 2. Single word check (e.g. "dm", "vcl", "cút", "ngu")
                                else if (TryGetReplacement(normalizedCurrent, out string repOne))
                                {
                                    matchedToxic = current;
                                    replacement = repOne;
                                    charsToDelete = current.Length;
                                    _previousWord = "";
                                    _currentWord.Clear();
                                }
                                else
                                {
                                    _previousWord = current;
                                    _currentWord.Clear();
                                }

                                if (!string.IsNullOrEmpty(matchedToxic))
                                {
                                    ConfigManager.Current.RescuedCount++;
                                    ConfigManager.Save();

                                    OnWordRescued?.Invoke(matchedToxic, replacement, ConfigManager.Current.RescuedCount);

                                    if (ConfigManager.Current.SoundEnabled)
                                    {
                                        ThreadPool.QueueUserWorkItem(_ => PlayBeep(900));
                                    }

                                    ushort triggerVk = (ushort)kbd.vkCode;
                                    PerformAtomicReplace(charsToDelete, replacement, triggerVk, true);
                                    return (IntPtr)1; // Swallow initial delimiter since replaced atomically
                                }
                            }
                            else
                            {
                                if (isEnter) _previousWord = "";
                                _currentWord.Clear();
                            }

                            return CallNextHookEx(_hookID, nCode, wParam, lParam);
                        }

                        // Extract char (Zero-latency fast path)
                        char typedChar = GetCharFromHook(kbd);
                        if (typedChar != '\0' && !char.IsControl(typedChar))
                        {
                            // 1. VIETNAMESE ENGINE TRANSFORMATION (UniKey 3.6.2 Engine)
                            if (ConfigManager.Current.EnableVietnamese)
                            {
                                if (VietnameseEngine.ProcessKey(typedChar, out int backs, out string replacement))
                                {
                                    // Immediate synchronous SendInput inside hook callback
                                    PerformAtomicReplace(backs, replacement, 0, false);

                                    if (backs > 0)
                                    {
                                        _currentWord.Length -= Math.Min(backs, _currentWord.Length);
                                    }
                                    _currentWord.Append(replacement);

                                    return (IntPtr)1; // Swallow transformed trigger key
                                }
                            }

                            // Normal char: append and pass through naturally with zero latency
                            _currentWord.Append(typedChar);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Safety net: never let any exception escape low-level hook
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    // Single atomic batch of SendInput (Backspaces + Unicode chars + optional delimiter)
    private static void PerformAtomicReplace(int charsToDelete, string replacement, ushort triggerVk, bool sendTrigger)
    {
        List<INPUT> inputs = new();

        // 1. Backspaces (Using real 0x0E scancode for full compatibility with all Windows apps & games)
        for (int i = 0; i < charsToDelete; i++)
        {
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                ki = new KEYBDINPUT
                {
                    wVk = VK_BACK,
                    wScan = 0x0E,
                    dwFlags = 0,
                    dwExtraInfo = MAGIC_EXTRA_INFO
                }
            });
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                ki = new KEYBDINPUT
                {
                    wVk = VK_BACK,
                    wScan = 0x0E,
                    dwFlags = KEYEVENTF_KEYUP,
                    dwExtraInfo = MAGIC_EXTRA_INFO
                }
            });
        }

        // 2. Replacement Unicode text
        foreach (char c in replacement)
        {
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = (ushort)c,
                    dwFlags = KEYEVENTF_UNICODE,
                    dwExtraInfo = MAGIC_EXTRA_INFO
                }
            });
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = (ushort)c,
                    dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                    dwExtraInfo = MAGIC_EXTRA_INFO
                }
            });
        }

        // 3. Optional trigger delimiter
        if (sendTrigger && triggerVk != 0)
        {
            ushort scan = (ushort)MapVirtualKey(triggerVk, 0);
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                ki = new KEYBDINPUT
                {
                    wVk = triggerVk,
                    wScan = scan,
                    dwFlags = 0,
                    dwExtraInfo = MAGIC_EXTRA_INFO
                }
            });
            inputs.Add(new INPUT
            {
                type = INPUT_KEYBOARD,
                ki = new KEYBDINPUT
                {
                    wVk = triggerVk,
                    wScan = scan,
                    dwFlags = KEYEVENTF_KEYUP,
                    dwExtraInfo = MAGIC_EXTRA_INFO
                }
            });
        }

        // Send all inputs atomically
        SendInput((uint)inputs.Count, inputs.ToArray(), 40);
    }

    private static bool TryGetReplacement(string word, out string replacement)
    {
        replacement = "";
        string key = word.Trim().ToLowerInvariant();

        if (_dictToxic.TryGetValue(key, out var val))
        {
            replacement = val;
            return true;
        }

        return false;
    }

    private static string NormalizeWord(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        string s = input.ToLowerInvariant();
        s = Regex.Replace(s, @"[.\-_/*]", "");
        s = Regex.Replace(s, @"(.)\1{2,}", "$1");

        return s.Trim();
    }

    private static bool IsNavigationOrResetKey(uint vk)
    {
        return vk switch
        {
            0x1B => true, // ESC
            0x21 => true, // PAGE UP
            0x22 => true, // PAGE DOWN
            0x23 => true, // END
            0x24 => true, // HOME
            0x25 => true, // LEFT
            0x26 => true, // UP
            0x27 => true, // RIGHT
            0x28 => true, // DOWN
            0x2E => true, // DELETE
            0x2D => true, // INSERT
            _ => false
        };
    }

    private static bool IsPunctuationVk(uint vk)
    {
        return vk switch
        {
            0xBC => true, // Comma ,
            0xBE => true, // Period .
            0xBA => true, // Semicolon ;
            0xBF => true, // Slash /
            0xDE => true, // Single quote '
            0xBD => true, // Minus -
            0xBB => true, // Equals =
            _ => false
        };
    }

    private static char GetCharFromHook(KBDLLHOOKSTRUCT kbd)
    {
        if ((kbd.flags & 4) != 0 || kbd.vkCode == 0 || kbd.vkCode == 0xE7)
        {
            return (char)kbd.scanCode;
        }

        bool shift = (GetAsyncKeyState(0x10) & 0x8000) != 0;

        // Letters A-Z: instant resolution (0 microseconds)
        if (kbd.vkCode >= 0x41 && kbd.vkCode <= 0x5A)
        {
            bool caps = (GetKeyState(0x14) & 0x0001) != 0;
            bool isUpper = shift ^ caps;
            char baseChar = (char)('a' + (kbd.vkCode - 0x41));
            return isUpper ? char.ToUpperInvariant(baseChar) : baseChar;
        }

        // Digits 0-9: instant resolution
        if (kbd.vkCode >= 0x30 && kbd.vkCode <= 0x39)
        {
            if (!shift)
            {
                return (char)('0' + (kbd.vkCode - 0x30));
            }
            return kbd.vkCode switch
            {
                0x30 => ')',
                0x31 => '!',
                0x32 => '@',
                0x33 => '#',
                0x34 => '$',
                0x35 => '%',
                0x36 => '^',
                0x37 => '&',
                0x38 => '*',
                0x39 => '(',
                _ => '\0'
            };
        }

        // Numpad 0-9: instant resolution
        if (kbd.vkCode >= 0x60 && kbd.vkCode <= 0x69)
        {
            return (char)('0' + (kbd.vkCode - 0x60));
        }

        // Common punctuation: instant resolution
        switch (kbd.vkCode)
        {
            case 0x20: return ' ';
            case 0xBA: return shift ? ':' : ';';
            case 0xBB: return shift ? '+' : '=';
            case 0xBC: return shift ? '<' : ',';
            case 0xBD: return shift ? '_' : '-';
            case 0xBE: return shift ? '>' : '.';
            case 0xBF: return shift ? '?' : '/';
            case 0xC0: return shift ? '~' : '`';
            case 0xDB: return shift ? '{' : '[';
            case 0xDC: return shift ? '|' : '\\';
            case 0xDD: return shift ? '}' : ']';
            case 0xDE: return shift ? '"' : '\'';
        }

        // Fallback for non-standard keyboard layouts
        try
        {
            IntPtr hkl = GetKeyboardLayout(0);

            byte[] keyState = new byte[256];
            GetKeyboardState(keyState);

            StringBuilder sb = new StringBuilder(4);
            int rc = ToUnicodeEx(kbd.vkCode, kbd.scanCode, keyState, sb, sb.Capacity, 0, hkl);
            if (rc > 0 && sb.Length > 0)
            {
                return sb[0];
            }
        }
        catch { }

        return '\0';
    }

    public static void PlayBeep(int freq)
    {
        try
        {
            Console.Beep(freq, 80);
        }
        catch { }
    }
}
