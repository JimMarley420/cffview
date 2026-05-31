using System.Windows;
using System.Windows.Media;

namespace cffview.Services;

public static class ThemeManager
{
    public static bool IsDark { get; private set; }

    public static void Apply(bool dark)
    {
        IsDark = dark;
        if (Application.Current == null) return;
        var r = Application.Current.Resources;

        if (dark)
        {
            Set(r, "AppBackground",       0x12, 0x12, 0x12);
            Set(r, "CardBackground",      0x1E, 0x1E, 0x1E);
            Set(r, "CardBorderColor",     0x30, 0x30, 0x30);
            Set(r, "TextPrimary",         0xF0, 0xF0, 0xF0);
            Set(r, "TextSecondary",       0xA0, 0xA0, 0xA0);
            Set(r, "TextMuted",           0x60, 0x60, 0x60);
            Set(r, "BorderColor",         0x33, 0x33, 0x33);
            Set(r, "RowHover",            0x2A, 0x2A, 0x2A);
            Set(r, "SearchBackground",    0x25, 0x25, 0x25);
            Set(r, "FooterBackground",    0x1A, 0x1A, 0x1A);
            Set(r, "DropdownBackground",  0x28, 0x28, 0x28);
            Set(r, "DropdownBorder",      0x44, 0x44, 0x44);
            Set(r, "IconButtonHover",     0x35, 0x35, 0x35);
            Set(r, "ListItemHover",       0x2D, 0x1A, 0x1A);
            Set(r, "SeparatorColor",      0x2E, 0x2E, 0x2E);
            Set(r, "PreviewBackground",   0x22, 0x22, 0x22);
        }
        else
        {
            Set(r, "AppBackground",       0xF2, 0xF3, 0xF5);
            Set(r, "CardBackground",      0xFF, 0xFF, 0xFF);
            Set(r, "CardBorderColor",     0xEE, 0xEE, 0xEE);
            Set(r, "TextPrimary",         0x1A, 0x1A, 0x1A);
            Set(r, "TextSecondary",       0x55, 0x55, 0x55);
            Set(r, "TextMuted",           0xAA, 0xAA, 0xAA);
            Set(r, "BorderColor",         0xDD, 0xDD, 0xDD);
            Set(r, "RowHover",            0xF7, 0xF7, 0xF7);
            Set(r, "SearchBackground",    0xFF, 0xFF, 0xFF);
            Set(r, "FooterBackground",    0xFF, 0xFF, 0xFF);
            Set(r, "DropdownBackground",  0xFF, 0xFF, 0xFF);
            Set(r, "DropdownBorder",      0xE0, 0xE0, 0xE0);
            Set(r, "IconButtonHover",     0xF0, 0xF0, 0xF0);
            Set(r, "ListItemHover",       0xFF, 0xF0, 0xF0);
            Set(r, "SeparatorColor",      0xF0, 0xF0, 0xF0);
            Set(r, "PreviewBackground",   0xFF, 0xFF, 0xFF);
        }
    }

    private static void Set(ResourceDictionary r, string key, byte red, byte green, byte blue)
        => r[key] = new SolidColorBrush(Color.FromRgb(red, green, blue));
}
