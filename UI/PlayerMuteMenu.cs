using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace ProximityVoiceChat.UI;

/// <summary>A small menu for muting current players locally.</summary>
public sealed class PlayerMuteMenu : IClickableMenu
{
    private const int MenuWidth = 700;
    private const int MenuHeight = 460;
    private const int RowHeight = 36;

    private readonly Func<IReadOnlyList<PlayerMuteCandidate>> _snapshot;
    private readonly Action<long, string> _toggle;
    private readonly List<(Rectangle Bounds, PlayerMuteRow Row)> _rowBounds = new();

    private Rectangle _nearbyTab;
    private Rectangle _allTab;
    private Rectangle _closeButton;
    private bool _nearbyOnly = true;
    private IReadOnlyList<PlayerMuteRow> _rows = Array.Empty<PlayerMuteRow>();

    public PlayerMuteMenu(
        Func<IReadOnlyList<PlayerMuteCandidate>> snapshot,
        Action<long, string> toggle)
        : base(
            (Game1.viewport.Width - MenuWidth) / 2,
            (Game1.viewport.Height - MenuHeight) / 2,
            MenuWidth,
            MenuHeight,
            showUpperRightCloseButton: false)
    {
        this._snapshot = snapshot;
        this._toggle = toggle;
    }

    public override void draw(SpriteBatch b)
    {
        this.RebuildRows();

        Rectangle background = new(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);
        b.Draw(Game1.staminaRect, background, Color.Black * 0.88f);

        int left = this.xPositionOnScreen + 24;
        int top = this.yPositionOnScreen + 20;
        b.DrawString(Game1.smallFont, "Player mute (local)", new Vector2(left, top), Color.White);

        this._nearbyTab = new Rectangle(left, top + 36, 120, 34);
        this._allTab = new Rectangle(left + 128, top + 36, 100, 34);
        this.DrawTab(b, this._nearbyTab, "Nearby", this._nearbyOnly);
        this.DrawTab(b, this._allTab, "All", !this._nearbyOnly);

        b.DrawString(
            Game1.smallFont,
            "Mutes are local: other players still hear the muted farmer.",
            new Vector2(left, top + 82),
            Color.LightGray);

        int rowTop = top + 116;
        this._rowBounds.Clear();
        if (this._rows.Count == 0)
        {
            b.DrawString(Game1.smallFont, "No players in this view.", new Vector2(left, rowTop), Color.LightGray);
        }
        else
        {
            int shown = Math.Min(this._rows.Count, 8);
            for (int i = 0; i < shown; i++)
            {
                PlayerMuteRow row = this._rows[i];
                Rectangle bounds = new(left, rowTop + (i * RowHeight), this.width - 48, RowHeight - 4);
                this._rowBounds.Add((bounds, row));

                b.Draw(Game1.staminaRect, bounds, row.IsMuted ? Color.DarkRed * 0.65f : Color.DimGray * 0.45f);
                string label = $"{row.Name}  [{row.Location}, {row.VoiceReach} tiles]";
                b.DrawString(Game1.smallFont, label, new Vector2(bounds.X + 10, bounds.Y + 6), Color.White);
                string state = row.IsMuted ? "Muted" : "Unmuted";
                Vector2 stateSize = Game1.smallFont.MeasureString(state);
                b.DrawString(
                    Game1.smallFont,
                    state,
                    new Vector2(bounds.Right - stateSize.X - 10, bounds.Y + 6),
                    row.IsMuted ? Color.Salmon : Color.PaleGreen);
            }
        }

        this._closeButton = new Rectangle(this.xPositionOnScreen + this.width - 120, this.yPositionOnScreen + this.height - 48, 96, 32);
        b.Draw(Game1.staminaRect, this._closeButton, Color.DimGray * 0.7f);
        Vector2 closeSize = Game1.smallFont.MeasureString("Close");
        b.DrawString(
            Game1.smallFont,
            "Close",
            new Vector2(
                this._closeButton.Center.X - (closeSize.X / 2f),
                this._closeButton.Center.Y - (closeSize.Y / 2f)),
            Color.White);

        this.drawMouse(b);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this._closeButton.Contains(x, y))
        {
            this.exitThisMenu();
            return;
        }

        if (this._nearbyTab.Contains(x, y))
        {
            this._nearbyOnly = true;
            Game1.playSound("smallSelect");
            return;
        }

        if (this._allTab.Contains(x, y))
        {
            this._nearbyOnly = false;
            Game1.playSound("smallSelect");
            return;
        }

        foreach ((Rectangle bounds, PlayerMuteRow row) in this._rowBounds)
        {
            if (!bounds.Contains(x, y))
                continue;

            this._toggle(row.PlayerId, row.Name);
            Game1.playSound("smallSelect");
            return;
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape)
        {
            this.exitThisMenu();
            return;
        }

        base.receiveKeyPress(key);
    }

    private void RebuildRows()
    {
        Farmer? local = Game1.player;
        if (local?.currentLocation is null)
        {
            this._rows = Array.Empty<PlayerMuteRow>();
            return;
        }

        this._rows = PlayerMuteMenuModel.Build(
            this._snapshot(),
            local.currentLocation.NameOrUniqueName,
            (int)local.Tile.X,
            (int)local.Tile.Y,
            this._nearbyOnly);
    }

    private void DrawTab(SpriteBatch b, Rectangle bounds, string label, bool selected)
    {
        b.Draw(Game1.staminaRect, bounds, selected ? Color.SteelBlue * 0.8f : Color.DimGray * 0.55f);
        Vector2 size = Game1.smallFont.MeasureString(label);
        b.DrawString(
            Game1.smallFont,
            label,
            new Vector2(bounds.Center.X - (size.X / 2f), bounds.Center.Y - (size.Y / 2f)),
            Color.White);
    }
}
