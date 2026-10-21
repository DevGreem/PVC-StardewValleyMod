using System;
using StardewModdingAPI;

namespace ProximityVoiceChat.Config;

/// <summary>Converts the persisted key names to and from SMAPI buttons.</summary>
public static class KeybindUtil
{
    public static bool TryParse(string? value, out SButton button)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && Enum.TryParse(value, ignoreCase: true, out button)
            && button != SButton.None)
        {
            return true;
        }

        button = SButton.None;
        return false;
    }

    public static SButton Parse(string? value, SButton fallback)
    {
        return TryParse(value, out SButton button) ? button : fallback;
    }

    public static string Format(SButton button)
    {
        return button.ToString();
    }
}
