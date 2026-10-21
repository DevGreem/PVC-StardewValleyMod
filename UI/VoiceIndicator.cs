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

    private Texture2D? _mutedIcon;

    /// <summary>Use the packaged muted-microphone sprite when it is available.</summary>
    public void SetMutedIcon(Texture2D icon)
    {
        this._mutedIcon = icon;
    }

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
        int iconSize = GetMutedIconSize(lineHeight);
        int rowHeight = Math.Max(lineHeight, iconSize + 2);
        int width = 0;
        foreach (string line in header)
            width = System.Math.Max(width, (int)Game1.smallFont.MeasureString(line).X);

        if (speakers.Count > 0)
            width = System.Math.Max(width, (int)Game1.smallFont.MeasureString("Nearby:").X);

        foreach (VoiceIndicatorSpeaker speaker in speakers)
            width = System.Math.Max(width, (int)Game1.smallFont.MeasureString(speaker.Name).X + iconSize + 8);

        int headerHeight = header.Count * lineHeight;
        int speakerHeight = speakers.Count > 0 ? (1 + speakers.Count) * rowHeight : 0;
        Rectangle background = new(8, 8, width + 16, headerHeight + speakerHeight + 12);
        batch.Draw(Game1.staminaRect, background, Color.Black * 0.55f);

        int y = 16;
        foreach (string line in header)
        {
            DrawText(batch, line, 16, y, TextColor);
            y += lineHeight;
        }

        if (speakers.Count == 0)
            return;

        DrawText(batch, "Nearby:", 16, y + ((rowHeight - lineHeight) / 2), TextColor);
        y += rowHeight;

        foreach (VoiceIndicatorSpeaker speaker in speakers)
        {
            DrawText(batch, speaker.Name, 16, y + ((rowHeight - lineHeight) / 2), TextColor);
            if (speaker.IsMuted)
            {
                Rectangle iconBounds = new(
                    16 + width - iconSize,
                    y + ((rowHeight - iconSize) / 2),
                    iconSize,
                    iconSize);
                if (this._mutedIcon is not null)
                    batch.Draw(this._mutedIcon, iconBounds, Color.White);
                else
                    DrawMutedMic(batch, iconBounds);
            }

            y += rowHeight;
        }
    }

    private static int GetMutedIconSize(int lineHeight)
    {
        return Math.Clamp((int)(lineHeight * 1.35f), 24, 48);
    }

    private static void DrawText(SpriteBatch batch, string text, int x, int y, Color color)
    {
        Vector2 position = new(x, y);
        batch.DrawString(Game1.smallFont, text, position + new Vector2(1f, 1f), ShadowColor);
        batch.DrawString(Game1.smallFont, text, position, color);
    }

    private static void DrawMutedMic(SpriteBatch batch, Rectangle bounds)
    {
        Color mic = Color.White;
        Color slash = new(255, 120, 120);
        float scaleX = bounds.Width / 12f;
        float scaleY = bounds.Height / 12f;

        void Fill(int x, int y, int width, int height, Color color)
        {
            batch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    bounds.X + (int)(x * scaleX),
                    bounds.Y + (int)(y * scaleY),
                    Math.Max(1, (int)(width * scaleX)),
                    Math.Max(1, (int)(height * scaleY))),
                color);
        }

        Fill(4, 0, 4, 1, mic);
        Fill(3, 1, 6, 5, mic);
        Fill(4, 6, 4, 1, mic);
        Fill(1, 4, 1, 5, mic);
        Fill(10, 4, 1, 5, mic);
        Fill(2, 9, 9, 1, mic);
        Fill(5, 10, 2, 1, mic);
        Fill(4, 11, 4, 1, mic);

        for (int i = 0; i < 11; i++)
            Fill(i, 11 - i, 2, 2, slash);
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
