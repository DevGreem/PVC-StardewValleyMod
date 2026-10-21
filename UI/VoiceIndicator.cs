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
        IReadOnlyList<VoiceIndicatorSpeaker> speakers)
    {
        List<string> header = new()
        {
            microphoneEnabled
                ? (transmitting ? "Mic: speaking" : activationMode == ActivationMode.PushToTalk ? "Mic: on (hold to talk)" : "Mic: on")
                : "Mic: muted",
            IndicatorRules.FormatVoiceReach(voiceReach)
        };

        int lineHeight = Game1.smallFont.LineSpacing;
        int width = 0;
        foreach (string line in header)
            width = System.Math.Max(width, (int)Game1.smallFont.MeasureString(line).X);

        if (speakers.Count > 0)
            width = System.Math.Max(width, (int)Game1.smallFont.MeasureString("Nearby:").X);

        foreach (VoiceIndicatorSpeaker speaker in speakers)
            width = System.Math.Max(width, (int)Game1.smallFont.MeasureString(speaker.Name).X + 18);

        int rowCount = header.Count + (speakers.Count > 0 ? 1 + speakers.Count : 0);
        Rectangle background = new(8, 8, width + 16, (rowCount * lineHeight) + 12);
        batch.Draw(Game1.staminaRect, background, Color.Black * 0.55f);

        int y = 16;
        foreach (string line in header)
        {
            DrawText(batch, line, 16, y, TextColor);
            y += lineHeight;
        }

        if (speakers.Count == 0)
            return;

        DrawText(batch, "Nearby:", 16, y, TextColor);
        y += lineHeight;

        foreach (VoiceIndicatorSpeaker speaker in speakers)
        {
            DrawText(batch, speaker.Name, 16, y, TextColor);
            if (speaker.IsMuted)
                DrawMutedMic(batch, 16 + width - 12, y + 2);

            y += lineHeight;
        }
    }

    private static void DrawText(SpriteBatch batch, string text, int x, int y, Color color)
    {
        Vector2 position = new(x, y);
        batch.DrawString(Game1.smallFont, text, position + new Vector2(1f, 1f), ShadowColor);
        batch.DrawString(Game1.smallFont, text, position, color);
    }

    private static void DrawMutedMic(SpriteBatch batch, int x, int y)
    {
        Color mic = Color.White;
        Color slash = new(255, 120, 120);

        batch.Draw(Game1.staminaRect, new Rectangle(x + 3, y, 4, 7), mic);
        batch.Draw(Game1.staminaRect, new Rectangle(x + 2, y + 1, 1, 5), mic);
        batch.Draw(Game1.staminaRect, new Rectangle(x + 7, y + 1, 1, 5), mic);
        batch.Draw(Game1.staminaRect, new Rectangle(x + 4, y + 7, 2, 3), mic);
        batch.Draw(Game1.staminaRect, new Rectangle(x + 2, y + 10, 6, 1), mic);

        for (int i = 0; i < 10; i++)
            batch.Draw(Game1.staminaRect, new Rectangle(x + i, y + 10 - i, 1, 1), slash);
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
