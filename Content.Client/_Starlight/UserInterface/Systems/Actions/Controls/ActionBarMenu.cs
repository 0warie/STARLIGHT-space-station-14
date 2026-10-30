using System.Numerics;
using Content.Client.ContextMenu.UI;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Starlight.UserInterface.Systems.Actions.Controls;

public enum ActionBarMenuChoice : byte
{
    MoveStart,
    MoveEnd,
    Remove,
}

/// <summary>
/// Right-click menu for an action on the hotbar. Only reports the choice; the caller does the rearranging.
/// </summary>
public sealed partial class ActionBarMenu
{
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private const string IconDir = "/Textures/_Starlight/Interface/VerbIcons/";

    public event Action<EntityUid, ActionBarMenuChoice>? OnChoice;

    private readonly ContextMenuPopup _popup;
    private readonly Entry _moveStart;
    private readonly Entry _moveEnd;
    private EntityUid _action;

    public bool Disposed => _popup.Disposed;

    public ActionBarMenu(ContextMenuUIController uiController)
    {
        IoCManager.InjectDependencies(this);
        _popup = new ContextMenuPopup(uiController, null);

        _moveStart = AddEntry("ui-action-bar-menu-move-start", ActionBarMenuChoice.MoveStart);
        _moveEnd = AddEntry("ui-action-bar-menu-move-end", ActionBarMenuChoice.MoveEnd);
        AddEntry("ui-action-bar-menu-remove", ActionBarMenuChoice.Remove)
            .SetIcon(GetIcon("/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png"));
    }

    /// <summary>
    /// Opens the menu at the mouse for <paramref name="action"/>, with arrows matching the bar's direction.
    /// </summary>
    public void Open(EntityUid action, bool vertical)
    {
        _action = action;
        _moveStart.SetIcon(GetIcon(IconDir + (vertical ? "move_top.png" : "move_left.png")));
        _moveEnd.SetIcon(GetIcon(IconDir + (vertical ? "move_bottom.png" : "move_right.png")));
        _popup.Open(UIBox2.FromDimensions(_ui.MousePositionScaled.Position, new Vector2(1, 1)));
    }

    public void Close()
    {
        _popup.Close();
    }

    private Entry AddEntry(string locId, ActionBarMenuChoice choice)
    {
        var entry = new Entry(Loc.GetString(locId));
        entry.OnPressed += _ =>
        {
            Close();
            OnChoice?.Invoke(_action, choice);
        };
        _popup.MenuBody.AddChild(entry);
        return entry;
    }

    private Texture GetIcon(string path)
    {
        return _cache.GetTexture(path);
    }

    /// <summary>
    /// A menu entry whose icon can be set; <see cref="ContextMenuElement"/> keeps its icon slot protected.
    /// </summary>
    private sealed class Entry : ContextMenuElement
    {
        private readonly TextureRect _icon = new() { Stretch = TextureRect.StretchMode.KeepAspectCentered };

        public Entry(string text) : base(text)
        {
            Icon.AddChild(_icon);
        }

        public void SetIcon(Texture texture)
        {
            _icon.Texture = texture;
        }
    }
}
