using System.Text;

namespace ProximityVoiceChat.Config;

/// <summary>Keeps remote player names safe for display, logs and persistence.</summary>
public static class PlayerNameSanitizer
{
    public const int MaxVisibleLength = 32;

    public static string Sanitize(string? name, long playerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return $"Player {playerId}";

        string trimmed = name.Trim();
        StringBuilder builder = new(MaxVisibleLength);
        foreach (char character in trimmed)
        {
            if (builder.Length >= MaxVisibleLength)
                break;

            if (!char.IsControl(character))
                builder.Append(character);
        }

        string safe = builder.ToString().Trim();
        return safe.Length > 0 ? safe : $"Player {playerId}";
    }
}
