namespace FamilyBudget.Mobile.Common;

public static class NumberInput
{
    public static bool TryParseLong(string? text, out long value, bool allowNegative = false)
    {
        var source = text?.Trim() ?? string.Empty;
        var digits = new string(source.Where(character => character is >= '0' and <= '9').ToArray());
        var normalized = allowNegative && source.StartsWith('-') ? $"-{digits}" : digits;
        return long.TryParse(normalized, out value);
    }
}
