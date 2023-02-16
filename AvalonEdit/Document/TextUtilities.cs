using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Documents;

namespace AvalonEdit.Document;

public enum CaretPositioningMode
{
    Normal,
    WordBorder,
    WordStart,
    WordStartOrSymbol,
    WordBorderOrSymbol,
    EveryCodepoint
}

public static partial class TextUtilities
{
    #region GetControlCharacterName

    private static readonly string[] c0Table = { "NUL", "SOH", "STX", "ETX", "EOT", "ENQ", "ACK", "BEL", "BS", "HT", "LF", "VT", "FF", "CR", "SO", "SI", "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB", "CAN", "EM", "SUB", "ESC", "FS", "GS", "RS", "US" };

    private static readonly string[] delAndC1Table = { "DEL", "PAD", "HOP", "BPH", "NBH", "IND", "NEL", "SSA", "ESA", "HTS", "HTJ", "VTS", "PLD", "PLU", "RI", "SS2", "SS3", "DCS", "PU1", "PU2", "STS", "CCH", "MW", "SPA", "EPA", "SOS", "SGCI", "SCI", "CSI", "ST", "OSC", "PM", "APC" };

    public static string GetControlCharacterName(char controlCharacter)
    {
        var num = (int)controlCharacter;
        if (num < c0Table.Length) return c0Table[num];
        return num is >= 127 and <= 159 ? delAndC1Table[num - 127] : num.ToString("x4", CultureInfo.InvariantCulture);
    }

    #endregion

    #region GetWhitespace

    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace", Justification = "WPF uses 'Whitespace'")]
    public static ISegment GetWhitespaceAfter(ITextSource textSource, int offset)
    {
        if (textSource == null) throw new ArgumentNullException(nameof(textSource));
        int pos;
        for (pos = offset; pos < textSource.TextLength; pos++)
        {
            var c = textSource.GetCharAt(pos);
            if (c != ' ' && c != '\t') break;
        }

        return new SimpleSegment(offset, pos - offset);
    }

    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace", Justification = "WPF uses 'Whitespace'")]
    public static ISegment GetWhitespaceBefore(ITextSource textSource, int offset)
    {
        if (textSource == null) throw new ArgumentNullException(nameof(textSource));
        int pos;
        for (pos = offset - 1; pos >= 0; pos--)
        {
            var c = textSource.GetCharAt(pos);
            if (c != ' ' && c != '\t') break;
        }

        pos++;
        return new SimpleSegment(pos, offset - pos);
    }

    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace", Justification = "WPF uses 'Whitespace'")]
    [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Parameter cannot be ITextSource because it must belong to the DocumentLine")]
    public static ISegment GetLeadingWhitespace(TextDocument document, DocumentLine documentLine)
    {
        if (documentLine == null) throw new ArgumentNullException(nameof(documentLine));
        return GetWhitespaceAfter(document, documentLine.Offset);
    }

    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace", Justification = "WPF uses 'Whitespace'")]
    [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Parameter cannot be ITextSource because it must belong to the DocumentLine")]
    public static ISegment GetTrailingWhitespace(TextDocument document, DocumentLine documentLine)
    {
        if (documentLine == null) throw new ArgumentNullException(nameof(documentLine));
        var segment = GetWhitespaceBefore(document, documentLine.EndOffset);
        return segment.Offset == documentLine.Offset ? new SimpleSegment(documentLine.EndOffset, 0) : segment;
    }

    #endregion

    #region GetSingleIndentationSegment

    #endregion

    #region GetCharacterClass

    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "c")]
    public static CharacterClass GetCharacterClass(char c)
    {
        if (c is '\r' or '\n') return CharacterClass.LineTerminator;
        return c == '_' ? CharacterClass.IdentifierPart : GetCharacterClass(char.GetUnicodeCategory(c));
    }

    private static CharacterClass GetCharacterClass(char highSurrogate, char lowSurrogate)
    {
        return char.IsSurrogatePair(highSurrogate, lowSurrogate) ? GetCharacterClass(char.GetUnicodeCategory(highSurrogate + lowSurrogate.ToString(), 0)) : CharacterClass.Other;
    }

    private static CharacterClass GetCharacterClass(UnicodeCategory c)
    {
        switch (c)
        {
            case UnicodeCategory.SpaceSeparator:
            case UnicodeCategory.LineSeparator:
            case UnicodeCategory.ParagraphSeparator:
            case UnicodeCategory.Control:
                return CharacterClass.Whitespace;
            case UnicodeCategory.UppercaseLetter:
            case UnicodeCategory.LowercaseLetter:
            case UnicodeCategory.TitlecaseLetter:
            case UnicodeCategory.ModifierLetter:
            case UnicodeCategory.OtherLetter:
            case UnicodeCategory.DecimalDigitNumber:
                return CharacterClass.IdentifierPart;
            case UnicodeCategory.NonSpacingMark:
            case UnicodeCategory.SpacingCombiningMark:
            case UnicodeCategory.EnclosingMark:
                return CharacterClass.CombiningMark;
            default:
                return CharacterClass.Other;
        }
    }

    #endregion

    #region GetNextCaretPosition

    public static int GetNextCaretPosition(ITextSource textSource, int offset, LogicalDirection direction, CaretPositioningMode mode)
    {
        if (textSource == null) throw new ArgumentNullException(nameof(textSource));
        switch (mode)
        {
            case CaretPositioningMode.Normal:
            case CaretPositioningMode.EveryCodepoint:
            case CaretPositioningMode.WordBorder:
            case CaretPositioningMode.WordBorderOrSymbol:
            case CaretPositioningMode.WordStart:
            case CaretPositioningMode.WordStartOrSymbol:
                break;
            default:
                throw new ArgumentException("Unsupported CaretPositioningMode: " + mode, nameof(mode));
        }

        if (direction != LogicalDirection.Backward && direction != LogicalDirection.Forward) throw new ArgumentException("Invalid LogicalDirection: " + direction, nameof(direction));
        var textLength = textSource.TextLength;
        if (textLength <= 0)
        {
            if (!IsNormal(mode)) return -1;
            if (offset > 0 && direction == LogicalDirection.Backward) return 0;
            if (offset < 0 && direction == LogicalDirection.Forward) return 0;

            return -1;
        }

        while (true)
        {
            var nextPos = direction == LogicalDirection.Backward ? offset - 1 : offset + 1;

            if (nextPos < 0 || nextPos > textLength) return -1;

            if (nextPos == 0)
            {
                if (IsNormal(mode) || !char.IsWhiteSpace(textSource.GetCharAt(0))) return nextPos;
            }
            else if (nextPos == textLength)
            {
                if (mode != CaretPositioningMode.WordStart && mode != CaretPositioningMode.WordStartOrSymbol)
                    if (IsNormal(mode) || !char.IsWhiteSpace(textSource.GetCharAt(textLength - 1)))
                        return nextPos;
            }
            else
            {
                var charBefore = textSource.GetCharAt(nextPos - 1);
                var charAfter  = textSource.GetCharAt(nextPos);
                if (!char.IsSurrogatePair(charBefore, charAfter))
                {
                    var classBefore                                                             = GetCharacterClass(charBefore);
                    var classAfter                                                              = GetCharacterClass(charAfter);
                    if (char.IsLowSurrogate(charBefore) && nextPos >= 2) classBefore            = GetCharacterClass(textSource.GetCharAt(nextPos - 2), charBefore);
                    if (char.IsHighSurrogate(charAfter) && nextPos + 1 < textLength) classAfter = GetCharacterClass(charAfter, textSource.GetCharAt(nextPos + 1));
                    if (StopBetweenCharacters(mode, classBefore, classAfter)) return nextPos;
                }
            }

            offset = nextPos;
        }
    }

    private static bool IsNormal(CaretPositioningMode mode)
    {
        return mode == CaretPositioningMode.Normal || mode == CaretPositioningMode.EveryCodepoint;
    }

    private static bool StopBetweenCharacters(CaretPositioningMode mode, CharacterClass charBefore, CharacterClass charAfter)
    {
        if (mode == CaretPositioningMode.EveryCodepoint) return true;
        if (charAfter == CharacterClass.CombiningMark) return false;
        if (mode == CaretPositioningMode.Normal) return true;
        if (charBefore == charAfter)
        {
            if (charBefore == CharacterClass.Other && (mode == CaretPositioningMode.WordBorderOrSymbol || mode == CaretPositioningMode.WordStartOrSymbol)) return true;
        }
        else
        {
            if (!((mode == CaretPositioningMode.WordStart || mode == CaretPositioningMode.WordStartOrSymbol) && (charAfter == CharacterClass.Whitespace || charAfter == CharacterClass.LineTerminator))) return true;
        }

        return false;
    }

    #endregion
}

public enum CharacterClass
{
    Other,
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace", Justification = "WPF uses 'Whitespace'")]
    Whitespace,
    IdentifierPart,
    LineTerminator,
    CombiningMark
}