using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProximityVoiceChat.Config;
using StardewValley;

namespace ProximityVoiceChat.UI;

/// <summary>Draws the voice status in the HUD. Purely visual: it never handles input.</summary>
public sealed class VoiceIndicator
{
    private static readonly Color TextColor = Color.White;
    private static readonly Color ShadowColor = Color.Black * 0.75f;
    private static readonly Color SpeakingColor = new(144, 238, 144);

    public void Draw(
        SpriteBatch batch,
        bool microphoneEnabled,
        bool transmitting,
        ActivationMode activationMode,
        int voiceReach,
        IReadOnlyList<string> speakers)
    {
        List<string> lines = new()
        {
            microphoneEnabled
                ? (transmitting ? "Mic: speaking" : activationMode == ActivationMode.PushToTalk ? "Mic: on (hold to talk)" : "Mic: on")
                : "Mic: muted",
            IndicatorRules.FormatVoiceReach(voiceReach)
        };

        if (speakers.Count > 0)
            lines.Add("Nearby: " + string.Join(", ", speakers));

        this.DrawPanel(batch, lines, 0f, null);
    }

    /// <summary>Draw the local microphone test overlay, including its input-level bar.</summary>
    public void DrawMicrophoneTest(SpriteBatch batch, float level, bool monitoring, int playbackVolume, string stopKey, bool multiplayer)
    {
        string decibels = level <= 0.000001f ? "-inf" : $"{20f * MathF.Log10(level):0}";
        List<string> lines = new()
        {
            $"Mic test: {decibels} dBFS"
        };

        if (multiplayer)
            lines.Add("Voice chat paused");

        lines.Add(monitoring ? $"Monitor: on (playback {playbackVolume}%)" : "Monitor: off");
        lines.Add($"{stopKey} to stop");

        this.DrawPanel(batch, lines, Math.Clamp(level * 4f, 0f, 1f), SpeakingColor);
    }

    private void DrawPanel(SpriteBatch batch, List<string> lines, float level, Color? levelColor)
    {
        float y = 16f;
        float width = 0f;
        foreach (string line in lines)
            width = System.Math.Max(width, Game1.smallFont.MeasureString(line).X);

        int barHeight = levelColor.HasValue ? 12 : 0;
        float contentWidth = levelColor.HasValue ? System.Math.Max(width, 240f) : width;
        Rectangle background = new(8, 8, (int)contentWidth + 16, (int)(lines.Count * Game1.smallFont.LineSpacing) + 12 + barHeight);
        batch.Draw(Game1.staminaRect, background, Color.Black * 0.55f);

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            Color color = i == 0 && levelColor.HasValue ? levelColor.Value : TextColor;
            Vector2 position = new(16f, y + (i * Game1.smallFont.LineSpacing));
            batch.DrawString(Game1.smallFont, line, position + new Vector2(1f, 1f), ShadowColor);
            batch.DrawString(Game1.smallFont, line, position, color);
        }

        if (levelColor.HasValue)
        {
            Rectangle barBackground = new(16, (int)(y + (lines.Count * Game1.smallFont.LineSpacing)), 240, 10);
            batch.Draw(Game1.staminaRect, barBackground, Color.Black * 0.6f);
            batch.Draw(
                Game1.staminaRect,
                new Rectangle(barBackground.X, barBackground.Y, (int)(barBackground.Width * level), barBackground.Height),
                levelColor.Value);
        }
    }
}
