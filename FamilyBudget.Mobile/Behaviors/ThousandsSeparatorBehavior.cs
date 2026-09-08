using System.Globalization;

namespace FamilyBudget.Mobile.Behaviors;

public class ThousandsSeparatorBehavior : Behavior<Entry>
{
    public static readonly BindableProperty AllowNegativeProperty = BindableProperty.Create(
        nameof(AllowNegative), typeof(bool), typeof(ThousandsSeparatorBehavior), false);

    private bool isFormatting;
    private string lastValidText = string.Empty;

    public bool AllowNegative
    {
        get => (bool)GetValue(AllowNegativeProperty);
        set => SetValue(AllowNegativeProperty, value);
    }

    protected override void OnAttachedTo(Entry entry)
    {
        base.OnAttachedTo(entry);
        entry.TextChanged += OnTextChanged;
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnTextChanged;
        base.OnDetachingFrom(entry);
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (isFormatting || sender is not Entry entry)
        {
            return;
        }

        // Android's EmojiTextWatcher is still processing the native Editable during
        // TextChanged. Defer the mutation until that IME/text-watcher cycle completes.
        entry.Dispatcher.Dispatch(() => FormatCurrentText(entry));
    }

    private void FormatCurrentText(Entry entry)
    {
        if (isFormatting)
        {
            return;
        }

        var current = entry.Text ?? string.Empty;
        var isNegative = AllowNegative && current.TrimStart().StartsWith('-');
        var digits = new string(current.Where(character => character is >= '0' and <= '9').ToArray());

        string formatted;
        if (digits.Length == 0)
        {
            formatted = isNegative ? "-" : string.Empty;
        }
        else
        {
            var normalized = isNegative ? $"-{digits}" : digits;
            formatted = long.TryParse(normalized, out var amount)
                ? amount.ToString("N0", CultureInfo.GetCultureInfo("id-ID"))
                : lastValidText;
        }

        if (current == formatted)
        {
            lastValidText = formatted;
            return;
        }

        try
        {
            isFormatting = true;
            entry.Text = formatted;
            entry.CursorPosition = formatted.Length;
            lastValidText = formatted;
        }
        finally
        {
            isFormatting = false;
        }
    }
}
