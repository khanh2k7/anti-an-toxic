//------------------------------------------------------------------------------
// Anti Ăn Toxic (AUT) - Vietnamese Keyboard & Anti-Toxic Engine
// Core Vietnamese typing engine faithfully ported from UniKey 3.6.2
// Copyright (C) 1998-2002 Pham Kim Long (UniKey)
// C# port: faithfully aligned with vietkey.cpp from Uk362src
// Licensed under GNU General Public License (GPL v2)
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace LolKey;

/// <summary>
/// Core Vietnamese Input Engine faithfully ported from Pham Kim Long's UniKey 3.6.2 engine.
/// Provides 100% standard orthography, tone placement, circumflex/horn marks, delayed-d, and undo.
/// Source credit: Pham Kim Long - UniKey (GPL v2).
/// </summary>
public static class VietnameseEngine
{
    private const int KEY_BUFSIZE = 40;
    private const int KEYS_MAINTAIN = 20;

    private const int BACK_CHAR = 8;
    private const int ENTER_CHAR = 13;

    private const int BREVE_MARK = 1;
    private const int TONE_MARK = 2;
    private const int DOUBLE_KEY = 3;
    private const int SHORT_KEY = 4;
    private const int VOWEL_CHAR = 5;
    private const int SEPARATOR_KEY = 6;
    private const int VNI_DOUBLE_CHAR_MARK = 7;
    private const int ESCAPE_KEY = 8;
    private const int SOFT_SEPARATOR_KEY = 9;

    private const int VNI_CIRCUMFLEX_INDEX = 1;
    private const int VNI_HORN_INDEX = 2;
    private const int VNI_BREVE_INDEX = 3;
    private const int VNI_D_INDEX = 4;

    private const int MAX_AFTER_VOWEL = 2;
    private const int MAX_VOWEL_SQUENCE = 3;
    private const int MAX_MODIFY_LENGTH = 6;

    public const int TELEX_INPUT = 0;
    public const int VNI_INPUT = 1;
    public const int VIQR_INPUT = 2;

    public const int UNICODE_CHARSET = 0x101;
    public const int TCVN3_CHARSET = 0;
    public const int VNI_CHARSET = 0x102;

    // DT Bitfield Macros - faithfully from UniKey
    private static uint ATTR_VOWEL_INDEX(uint x)      => (x & 0x1Fu);
    private static uint ATTR_IS_BREVE(uint x)          => ((x >> 22) & 0x1u);
    private static uint ATTR_TONE_INDEX(uint x)        => ((x >> 14) & 0xFu);
    private static uint ATTR_IS_SOFT_SEPARATOR(uint x) => ((x >> 24) & 0x1u);
    private static uint ATTR_DBCHAR_INDEX(uint x)      => ((x >> 9) & 0x1Fu);
    private static uint ATTR_MACRO_INDEX(uint x)       => ((x >> 5) & 0xFu);
    private static uint ATTR_IS_SEPARATOR(uint x)      => (x & 0x2000000u);
    private static uint ATTR_VNI_DOUBLE_INDEX(uint x)  => ((x >> 26) & 0x7u);
    private static uint ATTR_CURRENT_TONE(uint x)      => ((x >> 18) & 0xFu);
    private static uint ATTR_IS_WORD_STOP(uint x)      => ((x >> 29) & 0x1u);

    // Engine runtime state
    private static int _keys = 0;
    private static readonly byte[] _buf = new byte[KEY_BUFSIZE];
    private static readonly bool[] _lowerCase = new bool[KEY_BUFSIZE];
    private static int _lastWConverted = 0;
    private static int _tempVietOff = 0;

    private static int _keysPushed = 0;
    private static int _backs = 0;
    private static readonly char[] _uniPush = new char[512];
    private static readonly byte[] _ansiPush = new byte[1024];

    private static byte _changedChar;
    private static byte _oldChar;

    // Configuration settings
    public static bool FreeMarking { get; set; } = true;
    public static bool ModernStyle { get; set; } = true;
    public static bool ToneNextToVowel { get; set; } = false;
    public static InputMethod CurrentMethod { get; set; } = InputMethod.Telex;
    public static Charset CurrentCharset { get; set; } = Charset.Unicode;
    public static bool SpellCheckEnabled { get; set; } = true;

    // Tables
    private static readonly uint[] DT = new uint[256];
    private static readonly byte[][] BD = new byte[12][];
    private static readonly byte[] BK = new byte[8];
    private static readonly byte[] BW = new byte[6];
    private static readonly byte[] BT = new byte[6];
    private static readonly char[] ToUniH = new char[256];
    private static readonly char[] ToUniL = new char[256];

    // Constants from encode.cpp - ORDER MUST NOT BE CHANGED (UniKey constraint)
    private static readonly byte[] DoubleChars   = new byte[] { (byte)'d', (byte)'D', (byte)'e', (byte)'E', (byte)'a', (byte)'A', (byte)'o', (byte)'O', (byte)'u', (byte)'U' };
    private static readonly byte[] DoubleReverse = new byte[] { (byte)'d', (byte)'D', (byte)'e', (byte)'E', (byte)'a', (byte)'A', (byte)'o', (byte)'O' };
    private static readonly byte[] WReverse      = new byte[] { (byte)'a', (byte)'A', (byte)'o', (byte)'O', (byte)'u', (byte)'U' };
    private static readonly byte[] TelexTones    = new byte[] { (byte)'s', (byte)'f', (byte)'r', (byte)'x', (byte)'j', (byte)'z' };
    private static readonly byte[] TelexBreves   = new byte[] { (byte)'w', (byte)'W' };
    private static readonly byte[] TelexShortcuts = new byte[] { (byte)'[', (byte)']', (byte)'w', (byte)'W', (byte)'{', (byte)'}' };

    private static readonly byte[] VniToneKeys   = new byte[] { (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'0' };
    private static readonly byte[] VniDoubleKeys  = new byte[] { (byte)'6', (byte)'7', (byte)'8', (byte)'9' };

    private static readonly byte[] VIQRToneKeys   = new byte[] { (byte)'\'', (byte)'`', (byte)'?', (byte)'~', (byte)'.', (byte)'0' };
    private static readonly byte[] VIQRDoubleKeys  = new byte[] { (byte)'^', (byte)'+', (byte)'(', (byte)'d' };

    private static readonly byte[] ToneLimits = new byte[]
    {
        (byte)'b', (byte)'d', (byte)'f', (byte)'j', (byte)'k', (byte)'l', (byte)'q', (byte)'r', (byte)'s', (byte)'v', (byte)'w', (byte)'x', (byte)'z',
        (byte)'B', (byte)'D', (byte)'F', (byte)'J', (byte)'K', (byte)'L', (byte)'Q', (byte)'R', (byte)'S', (byte)'V', (byte)'W', (byte)'X', (byte)'Z',
        (byte)',', (byte)';', (byte)':', (byte)'.', (byte)'"', (byte)'\'', (byte)'!', (byte)'?',
        (byte)' ', (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9',
        (byte)'<', (byte)'>', (byte)'=', (byte)'+', (byte)'-', (byte)'*', (byte)'/', (byte)'\\',
        (byte)'_', (byte)'~', (byte)'`', (byte)'@', (byte)'#', (byte)'$', (byte)'%', (byte)'^', (byte)'&', (byte)'(', (byte)')', (byte)'{', (byte)'}', (byte)'[', (byte)']'
    };

    private static readonly byte[] WordStops = new byte[]
    {
        (byte)',', (byte)';', (byte)':', (byte)'.', (byte)'"', (byte)'\'', (byte)'!', (byte)'?', (byte)' ',
        (byte)'<', (byte)'>', (byte)'=', (byte)'+', (byte)'-', (byte)'*', (byte)'/', (byte)'\\',
        (byte)'_', (byte)'~', (byte)'`', (byte)'@', (byte)'#', (byte)'$', (byte)'%', (byte)'^', (byte)'&', (byte)'(', (byte)')', (byte)'{', (byte)'}', (byte)'[', (byte)']'
    };

    // TCVN3 Table Data (charset index 0)
    private static readonly byte[][] MapBD_TCVN3 = new byte[][]
    {
        new byte[] { 0xB8, 0xB5, 0xB6, 0xB7, 0xB9, (byte)'a' },
        new byte[] { 0xCA, 0xC7, 0xC8, 0xC9, 0xCB, 0xA9 },
        new byte[] { 0xBE, 0xBB, 0xBC, 0xBD, 0xC6, 0xA8 },
        new byte[] { 0xD0, 0xCC, 0xCE, 0xCF, 0xD1, (byte)'e' },
        new byte[] { 0xD5, 0xD2, 0xD3, 0xD4, 0xD6, 0xAA },
        new byte[] { 0xDD, 0xD7, 0xD8, 0xDC, 0xDE, (byte)'i' },
        new byte[] { 0xE3, 0xDF, 0xE1, 0xE2, 0xE4, (byte)'o' },
        new byte[] { 0xE8, 0xE5, 0xE6, 0xE7, 0xE9, 0xAB },
        new byte[] { 0xED, 0xEA, 0xEB, 0xEC, 0xEE, 0xAC },
        new byte[] { 0xF3, 0xEF, 0xF1, 0xF2, 0xF4, (byte)'u' },
        new byte[] { 0xF8, 0xF5, 0xF6, 0xF7, 0xF9, 0xAD },
        new byte[] { 0xFD, 0xFA, 0xFB, 0xFC, 0xFE, (byte)'y' }
    };

    private static readonly byte[] MapBK_TCVN3 = new byte[] { 0xAE, 0xA7, 0xAA, 0xA3, 0xA9, 0xA2, 0xAB, 0xA4 };
    private static readonly byte[] MapBW_TCVN3 = new byte[] { 0xA8, 0xA1, 0xAC, 0xA5, 0xAD, 0xA6 };
    private static readonly byte[] MapBT_TCVN3 = new byte[] { 0xAC, 0xAD, 0xAD, 0xA6, 0xA5, 0xA6 };

    // Unicode Mappings
    private static readonly ushort[] UniVnL_Unicode = new ushort[]
    {
        0x00e1, 0x00e0, 0x1ea3, 0x00e3, 0x1ea1, 0x0061,
        0x1ea5, 0x1ea7, 0x1ea9, 0x1eab, 0x1ead, 0x00e2,
        0x1eaf, 0x1eb1, 0x1eb3, 0x1eb5, 0x1eb7, 0x0103,
        0x00e9, 0x00e8, 0x1ebb, 0x1ebd, 0x1eb9, 0x0065,
        0x1ebf, 0x1ec1, 0x1ec3, 0x1ec5, 0x1ec7, 0x00ea,
        0x00ed, 0x00ec, 0x1ec9, 0x0129, 0x1ecb, 0x0069,
        0x00f3, 0x00f2, 0x1ecf, 0x00f5, 0x1ecd, 0x006f,
        0x1ed1, 0x1ed3, 0x1ed5, 0x1ed7, 0x1ed9, 0x00f4,
        0x1edb, 0x1edd, 0x1edf, 0x1ee1, 0x1ee3, 0x01a1,
        0x00fa, 0x00f9, 0x1ee7, 0x0169, 0x1ee5, 0x0075,
        0x1ee9, 0x1eeb, 0x1eed, 0x1eef, 0x1ef1, 0x01b0,
        0x00fd, 0x1ef3, 0x1ef7, 0x1ef9, 0x1ef5, 0x0079
    };

    private static readonly ushort[] UniVnH_Unicode = new ushort[]
    {
        0x00c1, 0x00c0, 0x1ea2, 0x00c3, 0x1ea0, 0x0041,
        0x1ea4, 0x1ea6, 0x1ea8, 0x1eaa, 0x1eac, 0x00c2,
        0x1eae, 0x1eb0, 0x1eb2, 0x1eb4, 0x1eb6, 0x0102,
        0x00c9, 0x00c8, 0x1eba, 0x1ebc, 0x1eb8, 0x0045,
        0x1ebe, 0x1ec0, 0x1ec2, 0x1ec4, 0x1ec6, 0x00ca,
        0x00cd, 0x00cc, 0x1ec8, 0x0128, 0x1eca, 0x0049,
        0x00d3, 0x00d2, 0x1ece, 0x00d5, 0x1ecc, 0x004f,
        0x1ed0, 0x1ed2, 0x1ed4, 0x1ed6, 0x1ed8, 0x00d4,
        0x1eda, 0x1edc, 0x1ede, 0x1ee0, 0x1ee2, 0x01a0,
        0x00da, 0x00d9, 0x1ee6, 0x0168, 0x1ee4, 0x0055,
        0x1ee8, 0x1eea, 0x1eec, 0x1eee, 0x1ef0, 0x01af,
        0x00dd, 0x1ef2, 0x1ef6, 0x1ef8, 0x1ef4, 0x0059
    };

    // Invalid Vietnamese consonant endings (English words)
    private static readonly HashSet<string> InvalidFinalConsonants = new(StringComparer.OrdinalIgnoreCase)
    {
        "st", "sh", "sk", "ss", "sp", "ct", "nt", "rt", "lt", "ft", "pt", "ld", "nd", "rd", "ff", "ll", "ck", "mp"
    };

    static VietnameseEngine()
    {
        InitTables();
    }

    public static void UpdateConfig(AppConfig config)
    {
        FreeMarking = config.FreeMarking;
        ModernStyle = config.ModernTonePlacement;
        CurrentMethod = config.Method;
        CurrentCharset = config.EncodingCharset;
        SpellCheckEnabled = config.SpellCheck;

        InitTables();
    }

    public static void ClearBuffer()
    {
        _keys = 0;
        _lastWConverted = 0;
        _tempVietOff = 0;
    }

    public static void ProcessBackspace()
    {
        if (_keys <= 0) return;
        _keys--;
    }

    private static void PutChar(byte ch, bool isLower = true)
    {
        if (_keys == KEY_BUFSIZE)
            ThrowBuf();
        _lowerCase[_keys] = isLower;
        _buf[_keys] = ch;
        _keys++;
    }

    private static void ThrowBuf()
    {
        Array.Copy(_buf, _keys - KEYS_MAINTAIN, _buf, 0, KEYS_MAINTAIN);
        Array.Copy(_lowerCase, _keys - KEYS_MAINTAIN, _lowerCase, 0, KEYS_MAINTAIN);
        _keys = KEYS_MAINTAIN;
    }

    public static void InitTables()
    {
        for (int i = 0; i < 12; i++)
        {
            BD[i] = new byte[6];
            Array.Copy(MapBD_TCVN3[i], BD[i], 6);
        }
        Array.Copy(MapBK_TCVN3, BK, 8);
        Array.Copy(MapBW_TCVN3, BW, 6);
        Array.Copy(MapBT_TCVN3, BT, 6);

        BuildInputMethod(CurrentMethod);

        // Build conversion table for Unicode
        for (int i = 0; i < 256; i++)
        {
            ToUniH[i] = (char)i;
            ToUniL[i] = (char)i;
        }

        for (char ch = 'a', up = 'A'; ch <= 'z'; ch++, up++)
        {
            ToUniH[ch] = up;
        }

        int p = 0;
        for (int i = 0; i < 12; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                byte tcvnChar = BD[i][j];
                ToUniH[tcvnChar] = (char)UniVnH_Unicode[p];
                ToUniL[tcvnChar] = (char)UniVnL_Unicode[p];
                p++;
            }
        }

        ToUniL[BK[0]] = (char)0x0111; // đ
        ToUniH[BK[0]] = (char)0x0110; // Đ

        ToUniL[BT[4]] = (char)UniVnL_Unicode[8 * 6 + 5]; // ơ
        ToUniH[BT[4]] = (char)UniVnH_Unicode[8 * 6 + 5]; // Ơ

        ToUniL[BT[5]] = (char)UniVnL_Unicode[10 * 6 + 5]; // ư
        ToUniH[BT[5]] = (char)UniVnH_Unicode[10 * 6 + 5]; // Ư
    }

    private static void BuildInputMethod(InputMethod method)
    {
        for (int i = 0; i < 256; i++)
            DT[i] = 0x2000000u; // bit 25: hard separator

        for (char ch = 'a'; ch <= 'z'; ch++)
            DT[ch] = 0;

        for (char ch = 'A'; ch <= 'Z'; ch++)
            DT[ch] = 0;

        foreach (byte b in ToneLimits)
            DT[b] = 0;

        foreach (byte b in BK)
            DT[b] = 0;

        foreach (byte b in BW)
            DT[b] = 0;

        for (uint i = 0; i < 12; i++)
        {
            for (uint j = 0; j < 6; j++)
            {
                DT[BD[i][j]] = i + 1;
                DT[BD[i][j]] |= (j + 1) << 18; // tone index from bit 18
            }
        }

        uint offset = (uint)(DoubleChars.Length - BW.Length);
        for (uint i = 0; i < DoubleChars.Length; i++)
        {
            DT[DoubleChars[i]] |= ((i + 1) << 9);
            if (i < BK.Length)
                DT[BK[i]] |= ((i + 1) << 9);
            if (i + 1 > offset)
                DT[BW[i - offset]] |= ((i + 1) << 9);
        }

        foreach (byte b in ToneLimits)
            DT[b] |= 0x1000000u; // Set bit 24 for soft separator

        foreach (byte b in WordStops)
            DT[b] |= 0x20000000u; // Set bit 29 for word stops

        if (method == InputMethod.Telex || method == InputMethod.SimpleTelex)
        {
            for (uint i = 0; i < TelexTones.Length; i++)
            {
                DT[TelexTones[i]] |= (i + 1) << 14;
                DT[char.ToUpperInvariant((char)TelexTones[i])] |= DT[TelexTones[i]];
            }

            foreach (byte b in TelexBreves)
                DT[b] |= 0x400000u; // Bit 22

            for (uint i = 0; i < TelexShortcuts.Length; i++)
                DT[TelexShortcuts[i]] |= (i + 1) << 5; // Macro keys from bit 5
        }
        else if (method == InputMethod.VNI)
        {
            for (uint j = 0; j < VniDoubleKeys.Length; j++)
                DT[VniDoubleKeys[j]] |= (j + 1) << 26; // Bit 26-28
            for (uint i = 0; i < VniToneKeys.Length; i++)
                DT[VniToneKeys[i]] |= (i + 1) << 14;
        }
        else if (method == InputMethod.VIQR)
        {
            for (uint j = 0; j < VIQRDoubleKeys.Length; j++)
                DT[VIQRDoubleKeys[j]] |= (j + 1) << 26;
            for (uint i = 0; i < VIQRToneKeys.Length; i++)
                DT[VIQRToneKeys[i]] |= (i + 1) << 14;
        }
    }

    private static int KeyCategory(byte c)
    {
        uint attr = DT[c];

        if (ATTR_IS_BREVE(attr) > 0)
            return BREVE_MARK;

        if (ATTR_TONE_INDEX(attr) > 0)
            return TONE_MARK;

        uint index = ATTR_DBCHAR_INDEX(attr);
        if ((CurrentMethod == InputMethod.Telex || CurrentMethod == InputMethod.SimpleTelex) && index > 0 && index < 9)
            return DOUBLE_KEY;
        if ((CurrentMethod != InputMethod.Telex && CurrentMethod != InputMethod.SimpleTelex) && ATTR_VNI_DOUBLE_INDEX(attr) > 0)
            return VNI_DOUBLE_CHAR_MARK;

        if (ATTR_MACRO_INDEX(attr) > 0)
            return SHORT_KEY;

        if (ATTR_IS_SEPARATOR(attr) > 0)
            return SEPARATOR_KEY;

        if (ATTR_IS_SOFT_SEPARATOR(attr) > 0)
            return SOFT_SEPARATOR_KEY;

        return 0;
    }

    /// <summary>
    /// Processes a single keystroke.
    /// Returns true if keystroke caused a transformation requiring SendInput (backs + replacement).
    /// Faithfully ported from VietKey::processKey() in vietkey.cpp.
    /// </summary>
    public static bool ProcessKey(char ch, out int backs, out string replacement)
    {
        byte c = (byte)ch;
        _keysPushed = 0;
        _backs = 0;
        _oldChar = c;
        int thisWConverted = 0;

        c = (byte)char.ToLowerInvariant(ch);

        int kieu = KeyCategory(c);

        if (_tempVietOff != 0)
        {
            if (!char.IsLetter((char)c))
                _tempVietOff = 0;
            if (kieu == SEPARATOR_KEY)
            {
                if (c == BACK_CHAR)
                    ProcessBackspace();
                else
                    ClearBuffer();
            }
            else
            {
                PutChar(c, char.IsLower(ch));
            }
            _lastWConverted = 0;
            backs = 0;
            replacement = "";
            return false;
        }

        switch (kieu)
        {
            case BREVE_MARK:
                // Faithfully ported from processKey() in vietkey.cpp
                bool isTelex = (CurrentMethod == InputMethod.Telex || CurrentMethod == InputMethod.SimpleTelex);
                if (isTelex && _lastWConverted != 0 && (c == 'w' || c == 'W'))
                {
                    ShortKey(c, char.IsLower(ch));
                }
                else
                {
                    PutBreveMark(c, char.IsLower(ch));
                    // Special case for W key in TELEX mode (vietkey.cpp line 205)
                    if (isTelex && _keysPushed == 0 && _backs == 0 && (c == 'w' || c == 'W'))
                    {
                        ShortKey(c, char.IsLower(ch));
                        thisWConverted = 1;
                    }
                }
                break;

            case DOUBLE_KEY:
                DoubleChar(c, char.IsLower(ch));
                break;

            case TONE_MARK:
                PutToneMark(c, char.IsLower(ch));
                break;

            case SHORT_KEY:
                ShortKey(c, char.IsLower(ch));
                break;

            case VNI_DOUBLE_CHAR_MARK:
                VniDoubleCharMark(c, char.IsLower(ch));
                break;

            case SEPARATOR_KEY:
                if (c == BACK_CHAR)
                    ProcessBackspace();
                else
                    ClearBuffer();
                _lastWConverted = 0;
                backs = 0;
                replacement = "";
                return false;
        }

        _lastWConverted = thisWConverted;

        if (_keysPushed == 0 && _backs == 0)
        {
            PutChar(c, char.IsLower(ch));
            backs = 0;
            replacement = "";
            return false;
        }

        if (_keysPushed == 0)
        {
            backs = 0;
            replacement = "";
            return false;
        }

        PostProcess();

        backs = _backs;
        replacement = new string(_uniPush, 0, _keysPushed);
        return true;
    }

    private static void PostProcess()
    {
        for (int i = 0, j = Math.Max(0, _keys - _keysPushed); i < _keysPushed; i++, j++)
        {
            if (j >= _lowerCase.Length) break;
            _uniPush[i] = _lowerCase[j] ? ToUniL[_ansiPush[i]] : ToUniH[_ansiPush[i]];
        }
    }

    /// <summary>
    /// Faithfully ported from VietKey::putBreveMark() in vietkey.cpp.
    /// Handles w/W to apply breve/horn mark. Critical fix: correct freeMarking
    /// <summary>
    /// Handles the "uo" / "ươ" diphthong for Telex mode.
    /// When w is typed on a word containing "uo" (e.g. duoc, nuoc, thuong, muot, ruou):
    /// transforms BOTH u -> ư and o -> ơ. If both already have horns, reverts both to u and o.
    /// Preceded by 'q' (e.g. quơ) is excluded so only o gets horn.
    /// </summary>
    private static bool TryHandleUoSequence(byte c, bool isLower, int leftMost)
    {
        int uPos = -1;
        for (int j = _keys - 2; j >= leftMost; j--)
        {
            if (j < 0 || j + 1 >= _keys) continue;
            uint vU = ATTR_VOWEL_INDEX(DT[_buf[j]]);
            uint vO = ATTR_VOWEL_INDEX(DT[_buf[j + 1]]);

            // In TCVN3: Row 9 is 'u' (vowel 10), Row 10 is 'u+' / ư (vowel 11)
            //           Row 6 is 'o' (vowel 7),  Row 8 is 'o+' / ơ (vowel 9)
            bool isU = (vU == 10 || vU == 11);
            bool isO = (vO == 7 || vO == 9);

            if (isU && isO)
            {
                // Must not be preceded by 'q'/'Q' (e.g. "quơ" -> only 'o' gets horn)
                if (j == 0 || (_buf[j - 1] != 'q' && _buf[j - 1] != 'Q'))
                {
                    uPos = j;
                    break;
                }
            }
        }

        if (uPos < 0) return false;

        int oPos = uPos + 1;
        uint toneU = ATTR_CURRENT_TONE(DT[_buf[uPos]]);
        uint toneO = ATTR_CURRENT_TONE(DT[_buf[oPos]]);
        uint vBaseU = ATTR_VOWEL_INDEX(DT[_buf[uPos]]);
        uint vBaseO = ATTR_VOWEL_INDEX(DT[_buf[oPos]]);

        bool uHasHorn = (vBaseU == 11);
        bool oHasHorn = (vBaseO == 9);

        if (uHasHorn && oHasHorn)
        {
            // DUPLICATE (both already horned): revert back to unhorned u and o, then append w
            _backs = _keys - uPos;
            _changedChar = _buf[uPos];

            // Revert u
            _buf[uPos] = (toneU > 0 && toneU <= 5) ? BD[9][toneU - 1] : BD[9][5];
            _ansiPush[_keysPushed++] = _buf[uPos];

            // Revert o
            _buf[oPos] = (toneO > 0 && toneO <= 5) ? BD[6][toneO - 1] : BD[6][5];
            _ansiPush[_keysPushed++] = _buf[oPos];

            // Copy remaining characters
            for (int k = oPos + 1; k < _keys; k++)
                _ansiPush[_keysPushed++] = _buf[k];

            PutChar(c, isLower);
            _ansiPush[_keysPushed++] = c;
            _tempVietOff = 1;
            return true;
        }
        else
        {
            // Apply horn to BOTH u and o
            _backs = _keys - uPos;
            _changedChar = _buf[uPos];

            // u -> ư (preserve tone)
            _buf[uPos] = (toneU > 0 && toneU <= 5) ? BD[10][toneU - 1] : BD[10][5];
            _ansiPush[_keysPushed++] = _buf[uPos];

            // o -> ơ (preserve tone)
            _buf[oPos] = (toneO > 0 && toneO <= 5) ? BD[8][toneO - 1] : BD[8][5];
            _ansiPush[_keysPushed++] = _buf[oPos];

            // Copy remaining characters
            for (int k = oPos + 1; k < _keys; k++)
                _ansiPush[_keysPushed++] = _buf[k];

            return true;
        }
    }

    /// <summary>
    /// Faithfully ported from VietKey::putBreveMark() in vietkey.cpp.
    /// Handles w/W to apply breve/horn mark.
    /// </summary>
    private static void PutBreveMark(byte c, bool isLower)
    {
        if (_keys <= 0) return;

        int i, k;
        uint attr;
        byte newChar;
        int leftMost;
        uint index = 0;
        int index_c = 0;
        uint toneIndex = 0;

        i = _keys - 1;
        bool isTelex = (CurrentMethod == InputMethod.Telex || CurrentMethod == InputMethod.SimpleTelex);
        if (!isTelex)
            index_c = (int)ATTR_VNI_DOUBLE_INDEX(DT[c]);

        leftMost = FreeMarking ? 0 : Math.Max(0, _keys - 1);
        leftMost = Math.Max(0, Math.Max(leftMost, _keys - MAX_MODIFY_LENGTH));

        // For Telex mode: check if there is a 'uo' sequence that should become 'ươ'
        int uoLeftMost = FreeMarking ? 0 : Math.Max(0, _keys - 2);
        if (isTelex && TryHandleUoSequence(c, isLower, uoLeftMost))
        {
            return;
        }

        // Scan backward to find vowel eligible for breve/horn mark
        while (i >= leftMost)
        {
            attr = DT[_buf[i]];
            toneIndex = ATTR_CURRENT_TONE(DT[_buf[i]]);
            if (toneIndex == 0 || toneIndex == 6)
                index = ATTR_DBCHAR_INDEX(attr);
            else
            {
                uint vBase = ATTR_VOWEL_INDEX(attr);
                index = (vBase > 0 && vBase <= 12)
                    ? ATTR_DBCHAR_INDEX(DT[BD[vBase - 1][5]])
                    : ATTR_DBCHAR_INDEX(attr);
            }

            if (index > 4)
            {
                if (!isTelex)
                {
                    if ((index_c == VNI_HORN_INDEX && index > 6) ||
                        (index_c == VNI_BREVE_INDEX && index <= 6))
                        break;
                }
                else
                    break;
            }
            else if (ATTR_IS_SEPARATOR(attr) > 0 || ATTR_IS_SOFT_SEPARATOR(attr) > 0)
                break;
            i--;
        }

        if (i < leftMost || index <= 4)
            return;

        // ---------------------------------------------------------------
        // FreeMarking: backward scan for single vowel / ua sequences
        // ---------------------------------------------------------------
        if (FreeMarking && i > 0)
        {
            byte prevChar = _buf[i - 1];
            uint tmpIdx;

            // Resolve prevChar to its base vowel
            uint prevVowelIdx = ATTR_VOWEL_INDEX(DT[prevChar]);
            if (prevVowelIdx > 0 && prevVowelIdx <= 12)
                prevChar = BD[prevVowelIdx - 1][5];

            // If prevChar is a BW vowel, resolve to WReverse
            tmpIdx = ATTR_DBCHAR_INDEX(DT[prevChar]);
            if (tmpIdx > 4 && tmpIdx - 4 <= (uint)WReverse.Length)
                prevChar = WReverse[tmpIdx - 4 - 1];

            // Case: (prev is o/O/u/U) AND (buf[i] is u/U) → step back to u
            if ((prevChar == 'o' || prevChar == 'O' || prevChar == 'u' || prevChar == 'U') &&
                (_buf[i] == 'u' || _buf[i] == 'U'))
            {
                i--;
                toneIndex = ATTR_CURRENT_TONE(DT[_buf[i]]);
                if (toneIndex == 0 || toneIndex == 6)
                    index = ATTR_DBCHAR_INDEX(DT[_buf[i]]);
                else
                {
                    uint vBase = ATTR_VOWEL_INDEX(DT[_buf[i]]);
                    index = (vBase > 0 && vBase <= 12)
                        ? ATTR_DBCHAR_INDEX(DT[BD[vBase - 1][5]])
                        : ATTR_DBCHAR_INDEX(DT[_buf[i]]);
                }
            }

            // Second check: if prev-prev is U/u (and not after 'q'), step back further
            if (i > 0)
            {
                prevChar = _buf[i - 1];
                prevVowelIdx = ATTR_VOWEL_INDEX(DT[prevChar]);
                if (prevVowelIdx > 0 && prevVowelIdx <= 12)
                    prevChar = BD[prevVowelIdx - 1][5];

                if ((prevChar == 'U' || prevChar == 'u') &&
                    (i == 1 || (i > 1 && _buf[i - 2] != 'q' && _buf[i - 2] != 'Q')))
                {
                    byte t = _buf[i];
                    uint tVowelIdx = ATTR_VOWEL_INDEX(DT[t]);
                    if (tVowelIdx > 0 && tVowelIdx <= 12)
                        t = BD[tVowelIdx - 1][5];

                    int tmpIdxInt = (int)ATTR_DBCHAR_INDEX(DT[t]) - 4;
                    byte tUpper = (byte)char.ToUpperInvariant((char)t);

                    bool shouldStepBack = false;
                    if (tUpper == 'A' && isTelex)
                        shouldStepBack = true;
                    else if (tUpper == 'O' ||
                             (tmpIdxInt > 0 && tmpIdxInt <= WReverse.Length &&
                              (WReverse[tmpIdxInt - 1] == 'o' || WReverse[tmpIdxInt - 1] == 'O')))
                    {
                        if (i != _keys - 1) // has trailing consonant
                            shouldStepBack = true;
                    }

                    if (shouldStepBack)
                    {
                        i--;
                        toneIndex = ATTR_CURRENT_TONE(DT[_buf[i]]);
                        if (toneIndex == 0 || toneIndex == 6)
                            index = ATTR_DBCHAR_INDEX(DT[_buf[i]]);
                        else
                        {
                            uint vBase = ATTR_VOWEL_INDEX(DT[_buf[i]]);
                            index = (vBase > 0 && vBase <= 12)
                                ? ATTR_DBCHAR_INDEX(DT[BD[vBase - 1][5]])
                                : ATTR_DBCHAR_INDEX(DT[_buf[i]]);
                        }
                    }
                }
            }
        }

        // Compute newChar for breve/horn transformation
        if (toneIndex == 0 || toneIndex == 6)
        {
            uint bwIdx = index - 4;
            newChar = (bwIdx > 0 && bwIdx <= (uint)BW.Length) ? BW[bwIdx - 1] : _buf[i];
        }
        else
        {
            uint vBase = ATTR_VOWEL_INDEX(DT[_buf[i]]);
            if (vBase == 0 || vBase > 12) return;
            uint baseIdx = ATTR_DBCHAR_INDEX(DT[BD[vBase - 1][5]]) - 4;
            if (baseIdx == 0 || baseIdx > (uint)BW.Length) return;

            uint wVowelIdx = ATTR_VOWEL_INDEX(DT[BW[baseIdx - 1]]);
            if (wVowelIdx > 0 && wVowelIdx <= 12 && toneIndex > 0 && toneIndex <= 5)
                newChar = BD[wVowelIdx - 1][toneIndex - 1];
            else
                newChar = BW[baseIdx - 1];
        }

        if (newChar != _buf[i])
        {
            // Apply the mark
            _backs = _keys - i;
            _changedChar = _buf[i];
            _ansiPush[_keysPushed++] = _buf[i] = newChar;
            for (k = i + 1; k < _keys; k++)
                _ansiPush[_keysPushed++] = _buf[k];
        }
        else
        {
            // Duplicate → undo back to English
            _backs = _keys - i;
            _changedChar = _buf[i];

            uint bwIdx = index - 4;
            if (toneIndex == 0 || toneIndex == 6)
            {
                _ansiPush[_keysPushed++] = _buf[i] =
                    (bwIdx > 0 && bwIdx <= (uint)WReverse.Length) ? WReverse[bwIdx - 1] : _buf[i];
            }
            else
            {
                if (bwIdx > 0 && bwIdx <= (uint)WReverse.Length)
                {
                    uint vIdx = ATTR_VOWEL_INDEX(DT[WReverse[bwIdx - 1]]);
                    if (vIdx > 0 && vIdx <= 12 && toneIndex > 0 && toneIndex <= 5)
                        _ansiPush[_keysPushed++] = _buf[i] = BD[vIdx - 1][toneIndex - 1];
                    else
                        _ansiPush[_keysPushed++] = _buf[i] = WReverse[bwIdx - 1];
                }
                else
                {
                    _ansiPush[_keysPushed++] = _buf[i];
                }
            }

            for (k = i + 1; k < _keys; k++)
                _ansiPush[_keysPushed++] = _buf[k];
            PutChar(c, isLower);
            _ansiPush[_keysPushed++] = c;
            _tempVietOff = 1;
        }
    }

    /// <summary>
    /// Faithfully ported from VietKey::doubleChar() in vietkey.cpp.
    /// Handles dd->d, ee->e^, aa->a^, oo->o^, uu->u' type transformations.
    /// </summary>
    private static void DoubleChar(byte c, bool isLower)
    {
        if (_keys <= 0) return;

        int i, k, leftMost;
        uint attr;
        byte newChar;
        uint index = 0, index_c, toneIndex = 0;

        i = _keys - 1;
        bool isTelex = (CurrentMethod == InputMethod.Telex || CurrentMethod == InputMethod.SimpleTelex);
        if (!isTelex)
            index_c = ATTR_VNI_DOUBLE_INDEX(DT[c]);
        else
            index_c = ATTR_DBCHAR_INDEX(DT[c]);

        leftMost = FreeMarking ? 0 : Math.Max(0, _keys - 1);
        leftMost = Math.Max(0, Math.Max(leftMost, _keys - MAX_MODIFY_LENGTH));

        while (i >= leftMost)
        {
            attr = DT[_buf[i]];
            toneIndex = ATTR_CURRENT_TONE(DT[_buf[i]]);
            if (toneIndex == 0 || toneIndex == 6)
                index = ATTR_DBCHAR_INDEX(attr);
            else
            {
                uint vBase = ATTR_VOWEL_INDEX(attr);
                index = (vBase > 0 && vBase <= 12) ? ATTR_DBCHAR_INDEX(DT[BD[vBase - 1][5]]) : ATTR_DBCHAR_INDEX(attr);
            }

            if (index > 0 && index < 9)
            {
                if (!isTelex)
                {
                    if ((index_c == VNI_CIRCUMFLEX_INDEX && index > 2) ||
                        (index_c == VNI_D_INDEX && index <= 2))
                        break;
                }
                else if (index == index_c)
                    break;
            }
            else if (ATTR_IS_SEPARATOR(attr) > 0 || ATTR_IS_SOFT_SEPARATOR(attr) > 0)
                break;
            i--;
        }

        if (i < leftMost || index == 0 || index >= 9)
            return;

        // Telex: 'o' after 'e' vowel → do not treat as double char (oe, oea etc.)
        if (isTelex && char.ToUpperInvariant((char)c) == 'O' && i < _keys - 1)
        {
            byte ch = _buf[i + 1];
            uint vowelIndex = ATTR_VOWEL_INDEX(DT[ch]);
            if (vowelIndex > 0 && vowelIndex <= 12 && BD[vowelIndex - 1][5] == (byte)'e')
                return;
        }

        // For 'd'/'D' (index <= 2): consonant, never has tone marks
        if (index <= 2 || toneIndex == 0 || toneIndex == 6)
        {
            newChar = (index > 0 && index <= (uint)BK.Length) ? BK[index - 1] : _buf[i];
        }
        else
        {
            uint vBase = (index > 0 && index <= (uint)BK.Length) ? ATTR_VOWEL_INDEX(DT[BK[index - 1]]) : 0;
            if (vBase > 0 && vBase <= 12 && toneIndex > 0 && toneIndex <= 5)
                newChar = BD[vBase - 1][toneIndex - 1];
            else
                newChar = (index > 0 && index <= (uint)BK.Length) ? BK[index - 1] : _buf[i];
        }

        if (newChar != _buf[i])
        {
            _backs = _keys - i;
            _changedChar = _buf[i];
            _ansiPush[_keysPushed++] = _buf[i] = newChar;
            for (k = i + 1; k < _keys; k++)
                _ansiPush[_keysPushed++] = _buf[k];
        }
        else
        {
            // Back to English (undo)
            _backs = _keys - i;
            _changedChar = _buf[i];
            if (index <= 2 || toneIndex == 0 || toneIndex == 6)
            {
                _ansiPush[_keysPushed++] = (index > 0 && index <= (uint)DoubleReverse.Length)
                    ? DoubleReverse[index - 1] : c;
            }
            else
            {
                uint vBase = (index > 0 && index <= (uint)DoubleReverse.Length)
                    ? ATTR_VOWEL_INDEX(DT[DoubleReverse[index - 1]]) : 0;
                if (vBase > 0 && vBase <= 12 && toneIndex > 0 && toneIndex <= 5)
                    _ansiPush[_keysPushed++] = BD[vBase - 1][toneIndex - 1];
                else
                    _ansiPush[_keysPushed++] = (index > 0 && index <= (uint)DoubleReverse.Length)
                        ? DoubleReverse[index - 1] : c;
            }
            for (k = i + 1; k < _keys; k++)
                _ansiPush[_keysPushed++] = _buf[k];
            PutChar(c, isLower);
            _ansiPush[_keysPushed++] = c;
            _tempVietOff = 1;
        }
    }

    /// <summary>
    /// Faithfully ported from VietKey::shortKey() in vietkey.cpp.
    /// Handles bracket shortcuts: [ -> a(, ] -> a^, w -> u+, { -> o+, } -> u+.
    /// </summary>
    private static void ShortKey(byte c, bool isLower)
    {
        uint index = ATTR_MACRO_INDEX(DT[c]);
        if (index == 0 || index > (uint)BT.Length) return;
        byte newChar = BT[index - 1];

        _keysPushed = 0;
        bool duplicate = (_keys > 0) && (_buf[_keys - 1] == newChar);
        if (duplicate)
        {
            // Convert back to English
            _changedChar = _buf[_keys - 1];
            _buf[_keys - 1] = c;
            _ansiPush[_keysPushed++] = c;
            _backs = 1;
            _tempVietOff = 1;
            return;
        }

        _backs = 0;
        _ansiPush[_keysPushed++] = newChar;
        if (_keys == KEY_BUFSIZE)
            ThrowBuf();

        _lowerCase[_keys] = isLower;
        _buf[_keys] = newChar;
        _keys++;
    }

    /// <summary>
    /// Faithfully ported from VietKey::putToneMark() in vietkey.cpp.
    /// Handles tone marks: s/f/r/x/j/z (Telex) or 1-5/0 (VNI).
    /// FIX: Added critical "duplicate && index==5" early return to prevent ghost
    /// characters when pressing tone key on already-base vowel (fixes di, gi, etc.)
    /// </summary>
    private static void PutToneMark(byte c, bool isLower)
    {
        if (_keys <= 0) return;

        int i, k, l, cuoi, vowel, leftMost;
        uint index;
        byte newChar, t;
        uint attr = 0;

        // Find rightmost vowel from current position
        i = _keys - 1;
        leftMost = ToneNextToVowel ? i : 0;
        leftMost = Math.Max(0, Math.Max(_keys - 1 - MAX_AFTER_VOWEL, leftMost));
        while (i >= leftMost)
        {
            attr = DT[_buf[i]];
            if (ATTR_IS_SEPARATOR(attr) > 0 || ATTR_IS_SOFT_SEPARATOR(attr) > 0 || ATTR_VOWEL_INDEX(attr) > 0)
                break;
            i--;
        }
        if (i < leftMost || ATTR_VOWEL_INDEX(attr) == 0)
            return;

        // Find continuous vowel sequence (stop at already-toned vowels)
        cuoi = i;
        leftMost = ToneNextToVowel ? i : 0;
        leftMost = Math.Max(0, Math.Max(cuoi - MAX_VOWEL_SQUENCE + 1, leftMost));
        while (i >= leftMost && ATTR_VOWEL_INDEX(DT[_buf[i]]) > 0
               && ((_buf[i] <= 'z' && _buf[i] >= 'a') || (_buf[i] <= 'Z' && _buf[i] >= 'A')))
            i--;

        if (i < leftMost || ATTR_VOWEL_INDEX(DT[_buf[i]]) == 0)
        {
            l = cuoi - i; // length of vowel sequence
            switch (l)
            {
                case 2:
                    if (ModernStyle &&
                        ((_buf[cuoi - 1] == 'o' && _buf[cuoi] == 'a') ||
                         (_buf[cuoi - 1] == 'o' && _buf[cuoi] == 'e') ||
                         (_buf[cuoi - 1] == 'u' && _buf[cuoi] == 'y')))
                        i = cuoi;
                    else
                    {
                        t = (i >= 0 && i < _keys) ? (byte)char.ToUpperInvariant((char)_buf[i]) : (byte)0;
                        if (i >= 0 && (t == 'Q' || (t == 'G' && i + 1 < _keys && char.ToUpperInvariant((char)_buf[i + 1]) == 'I')))
                            i = cuoi;
                        else if (_keys > cuoi + 1)
                            i = cuoi; // has trailing consonant
                        else
                            i = cuoi - 1;
                    }
                    break;
                case 3:
                    i = cuoi - 1;
                    break;
                default:
                    i = cuoi;
                    break;
            }
        }

        if (i < 0 || i >= _keys) return;

        uint vIdx = ATTR_VOWEL_INDEX(DT[_buf[i]]);
        if (vIdx == 0 || vIdx > 12) return;
        vowel = (int)vIdx - 1;

        if (c >= 5)
        {
            uint tIdx = ATTR_TONE_INDEX(DT[c]);
            if (tIdx == 0) return;
            index = tIdx - 1;
        }
        else
            index = c;

        if (index >= 6) return;

        newChar = BD[vowel][index];
        bool duplicate = (newChar == _buf[i]);
        if (duplicate)
            newChar = BD[vowel][5]; // strip tone -> base vowel

        // CRITICAL FIX from vietkey.cpp:
        // If pressing tone key that is ALREADY the base vowel (duplicate of base), return.
        // This prevents ghost characters when typing on a neutral/base vowel.
        if (duplicate && index == 5)
            return;

        _backs = _keys - i;
        _changedChar = _buf[i];
        _buf[i] = _ansiPush[_keysPushed++] = newChar;
        for (k = 1; k < _keys - i; k++)
            _ansiPush[_keysPushed++] = _buf[i + k];

        if (duplicate)
        {
            _ansiPush[_keysPushed++] = c;
            PutChar(c, isLower);
            _tempVietOff = 1;
        }
    }

    private static void VniDoubleCharMark(byte c, bool isLower)
    {
        if (_keys == 0) return;
        uint index = ATTR_VNI_DOUBLE_INDEX(DT[c]);
        switch (index)
        {
            case VNI_CIRCUMFLEX_INDEX:
            case VNI_D_INDEX:
                DoubleChar(c, isLower);
                break;
            case VNI_HORN_INDEX:
            case VNI_BREVE_INDEX:
                PutBreveMark(c, isLower);
                break;
        }
    }

    // ==========================================
    // SPELLCHECK & ENGLISH AUTO-REVERT
    // ==========================================
    public static bool HasInvalidVietnameseEnding(string word)
    {
        if (word.Length < 2) return false;
        string ending = word[^2..];
        return InvalidFinalConsonants.Contains(ending);
    }

    public static string RemoveVietnameseAccents(string text)
    {
        string[] vietnamese = new string[]
        {
            "aáàảãạăắằẳẵặâấầẩẫậ",
            "AÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬ",
            "dđ", "DĐ",
            "eéèẻẽẹêếềểễệ",
            "EÉÈẺẼẸÊẾỀỂỄỆ",
            "iíìỉĩị", "IÍÌỈĨỊ",
            "oóòỏõọôốồổỗộơớờởỡợ",
            "OÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢ",
            "uúùủũụưứừửữự",
            "UÚÙỦŨỤƯỨỪỬỮỰ",
            "yýỳỷỹỵ", "YÝỲỶỸỴ"
        };
        char[] english = new char[] { 'a', 'A', 'd', 'D', 'e', 'E', 'i', 'I', 'o', 'O', 'u', 'U', 'y', 'Y' };

        StringBuilder sb = new StringBuilder(text);
        for (int i = 0; i < vietnamese.Length; i++)
        {
            foreach (char ch in vietnamese[i])
            {
                sb.Replace(ch, english[i]);
            }
        }
        return sb.ToString();
    }
}
