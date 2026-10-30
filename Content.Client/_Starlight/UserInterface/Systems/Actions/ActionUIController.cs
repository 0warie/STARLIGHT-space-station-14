using System.Linq;
using System.Numerics;
using Content.Client.Actions;
using Content.Client._Starlight.UserInterface.Systems.Actions.Controls;
using Content.Client.ContextMenu.UI;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Shared._Starlight.CCVar;
using Content.Shared.Actions.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

// ReSharper disable CheckNamespace
namespace Content.Client.UserInterface.Systems.Actions;

public sealed partial class ActionUIController
{
    /// <summary>
    /// True from attaching to an entity until all of its actions have arrived. While set, the whole bar gets
    /// re-sorted whenever actions come in, instead of appending them in whatever order the client received them.
    /// </summary>
    private bool _settling;

    private bool _sortQueued;

    /// <summary>
    /// Set once the player rearranges the bar (or a saved layout is loaded); we stop re-sorting so we don't undo it.
    /// </summary>
    private bool _userArranged;

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ContextMenuUIController _contextMenu = default!;

    /// <summary>
    /// Right-click menu for hotbar actions. Created on first use.
    /// </summary>
    private ContextMenuPopup? _barMenu;

    /// <summary>
    /// The action the right-click menu was opened for.
    /// </summary>
    private EntityUid? _barMenuAction;

    /// <summary>
    /// The bar button currently showing the insert marker while dragging, if any.
    /// </summary>
    private ActionButton? _insertMarkerButton;

    /// <summary>
    /// The spare slot at the end of the bar, hidden while an action already on the bar is dragged.
    /// </summary>
    private ActionButton? _hiddenEndSlot;

    private void StartSettling()
    {
        _userArranged = false;
        _settling = true;
    }

    private void QueueSortIfSettling()
    {
        if (_settling && !_userArranged)
            _sortQueued = true;
    }

    private bool AllActionsArrived()
    {
        if (_playerManager.LocalEntity is not { } user
            || !EntityManager.TryGetComponent<ActionsComponent>(user, out var actions))
            return false;

        return actions.Actions.All(action => EntityManager.HasComponent<ActionComponent>(action));
    }

    /// <summary>
    /// Handles dropping a dragged action. Dropping on the bar inserts it at the marked position, dropping a bar
    /// action on the actions window removes it, and dropping anywhere else leaves the bar unchanged.
    /// </summary>
    private void DropDraggedAction(ActionButton dragged, EntityUid action)
    {
        var hovered = UIManager.MouseGetControl(_input.MouseScreenPosition);

        if (GetBarButton(hovered) is { } target)
        {
            // Skipped for drops that wouldn't move anything, so they don't count as the player rearranging the bar.
            if (!IsNoOpDrop(target))
                InsertAction(dragged, action, target);
            return;
        }

        if (dragged.Parent == _container && hovered != null && _window != null
            && hovered.GetSelfAndLogicalAncestors().Contains(_window))
            SetAction(dragged, null, false);
    }

    /// <summary>
    /// Returns <paramref name="control"/> as an action button if it is one of the hotbar's buttons, otherwise null.
    /// </summary>
    private ActionButton? GetBarButton(Control? control)
    {
        return control is ActionButton button && _container != null && button.Parent == _container ? button : null;
    }

    /// <summary>
    /// Moves (or adds, if dragged from the window) an action to the drop position next to <paramref name="target"/>,
    /// shifting the rest of the bar along instead of overwriting whatever was there. Empty slots take the action in
    /// place, swapping with it so nothing else on the bar moves.
    /// </summary>
    private void InsertAction(ActionButton dragged, EntityUid action, ActionButton target)
    {
        if (_container == null || !_container.TryGetButtonIndex(target, out var targetIndex))
            return;

        var oldIndex = GetDraggedIndex(dragged, action);
        _userArranged = true;

        // Empty slot inside the list (nothing there, or an action that is currently hidden).
        if (target.Action == null && targetIndex < _actions.Count)
        {
            if (oldIndex >= 0)
                (_actions[oldIndex], _actions[targetIndex]) = (_actions[targetIndex], action);
            else
            {
                // A hidden action in the slot goes to the end rather than shifting everything after it along.
                if (_actions[targetIndex] is { } hidden)
                    _actions.Add(hidden);
                _actions[targetIndex] = action;
            }

            // A swap can leave the old slot as a trailing gap; drop it so the bar doesn't grow.
            while (_actions.Count > 0 && _actions[^1] == null)
                _actions.RemoveAt(_actions.Count - 1);
            return;
        }

        var insertIndex = GetDropIndex(target, targetIndex);
        if (oldIndex >= 0)
        {
            _actions.RemoveAt(oldIndex);
            if (oldIndex < insertIndex)
                insertIndex--;
        }

        _actions.Insert(Math.Clamp(insertIndex, 0, _actions.Count), action);
    }

    /// <summary>
    /// Gets the dragged action's current index in the bar's action list, or -1 if it is not on the bar.
    /// </summary>
    private int GetDraggedIndex(ActionButton dragged, EntityUid action)
    {
        var index = _container != null && _container.TryGetButtonIndex(dragged, out var buttonIndex)
            ? buttonIndex
            : _actions.IndexOf(action);
        return index < _actions.Count ? index : -1;
    }

    /// <summary>
    /// Gets the list index a drop on <paramref name="target"/> would insert at: before it when the mouse is on the
    /// half nearer the start of the bar, after it otherwise. Empty slots always take the action in place.
    /// </summary>
    private int GetDropIndex(ActionButton target, int targetIndex)
    {
        return target.Action != null && GetInsertSide(target) == ActionInsertSide.After ? targetIndex + 1 : targetIndex;
    }

    /// <summary>
    /// True if the bar fills top-to-bottom (default HUD) rather than left-to-right (separated chat HUD).
    /// </summary>
    private bool IsBarVertical()
    {
        return _container?.LimitedDimension == Dimension.Row;
    }

    /// <summary>
    /// Which side of <paramref name="control"/> the mouse is on, along the direction the bar fills in.
    /// </summary>
    private ActionInsertSide GetInsertSide(Control control)
    {
        var mouse = _input.MouseScreenPosition.Position;
        var center = control.GlobalPixelPosition + control.PixelSize / 2f;
        var after = IsBarVertical() ? mouse.Y > center.Y : mouse.X > center.X;
        return after ? ActionInsertSide.After : ActionInsertSide.Before;
    }

    /// <summary>
    /// Called every frame while dragging. Shows a marker on the side of the hovered bar action the dragged action
    /// will be inserted on. Drops that would leave the action where it already is get no marker.
    /// </summary>
    private void UpdateInsertMarker()
    {
        // A bar action frees its own slot when moved, so the spare slot at the end would only mislead.
        if (_hiddenEndSlot == null && _menuDragHelper.Dragged?.Parent == _container
            && _container?.ChildCount > 0 && _container.GetChild(_container.ChildCount - 1) is ActionButton { Action: null } endSlot)
        {
            _hiddenEndSlot = endSlot;
            endSlot.Visible = false;
        }

        var target = GetBarButton(UIManager.MouseGetControl(_input.MouseScreenPosition));
        if (target is { Action: null } || IsNoOpDrop(target))
            target = null;

        if (_insertMarkerButton != target)
            HideInsertMarker();

        _insertMarkerButton = target;
        target?.ShowInsertMarker(GetInsertSide(target), IsBarVertical());
    }

    /// <summary>
    /// True if dropping the dragged action on the filled slot <paramref name="target"/> right now would not move it,
    /// e.g. onto itself or onto the touching edge of a neighbour. Empty slots always take the action, so never count.
    /// </summary>
    private bool IsNoOpDrop(ActionButton? target)
    {
        if (target is not { Action: not null } || _container == null || _menuDragHelper.Dragged is not { Action: { } action } dragged
            || !_container.TryGetButtonIndex(target, out var targetIndex))
            return false;

        var oldIndex = GetDraggedIndex(dragged, action);
        if (oldIndex < 0)
            return false;

        var insertIndex = GetDropIndex(target, targetIndex);
        return insertIndex == oldIndex || insertIndex == oldIndex + 1;
    }

    /// <summary>
    /// Hides the insert marker when the drag moves off a button or ends.
    /// </summary>
    private void HideInsertMarker()
    {
        _insertMarkerButton?.HideInsertMarker();
        _insertMarkerButton = null;
    }

    private void OnBarDragEnded()
    {
        HideInsertMarker();

        if (_hiddenEndSlot != null)
            _hiddenEndSlot.Visible = true;
        _hiddenEndSlot = null;
    }

    /// <summary>
    /// Right-clicking a hotbar action removes it straight away with quick remove on, otherwise opens a menu for it.
    /// </summary>
    private void OnBarRightClick(ActionButton button)
    {
        // Checked first so empty slots still get cleared like before, collapsing gaps in the bar.
        if (_cfg.GetCVar(StarlightCCVars.ActionBarQuickRemove))
        {
            SetAction(button, null);
            return;
        }

        if (button.Action is not { } action)
            return;

        if (_barMenu is not { Disposed: false })
            _barMenu = new ContextMenuPopup(_contextMenu, null);

        // Rebuilt each time so the arrows match the bar's current direction.
        var vertical = IsBarVertical();
        _barMenu.MenuBody.RemoveAllChildren();
        AddBarMenuItem("ui-action-bar-menu-move-start", a => _actions.Insert(0, a),
            $"/Textures/_Starlight/Interface/VerbIcons/{(vertical ? "move_top" : "move_left")}.png");
        AddBarMenuItem("ui-action-bar-menu-move-end", a => _actions.Add(a),
            $"/Textures/_Starlight/Interface/VerbIcons/{(vertical ? "move_bottom" : "move_right")}.png");
        AddBarMenuItem("ui-action-bar-menu-remove", _ => { }, "/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png");

        _barMenuAction = action;
        _barMenu.Open(UIBox2.FromDimensions(UIManager.MousePositionScaled.Position, new Vector2(1, 1)));
    }

    /// <summary>
    /// Closes the right-click menu, e.g. when the bar's actions are unlinked so it doesn't linger over the lobby
    /// or act on a previous body's actions.
    /// </summary>
    private void CloseBarMenu()
    {
        _barMenu?.Close();
        _barMenuAction = null;
    }

    /// <summary>
    /// Adds an entry to the right-click menu. <paramref name="place"/> puts the action back into the bar after it
    /// has been taken out, so doing nothing removes it.
    /// </summary>
    private void AddBarMenuItem(string locId, Action<EntityUid> place, string? iconPath = null)
    {
        var icon = iconPath != null ? _spriteSystem.Frame0(new SpriteSpecifier.Texture(new ResPath(iconPath))) : null;
        var element = new ActionBarMenuElement(Loc.GetString(locId), icon);
        element.OnPressed += _ => ApplyBarMenuItem(place);
        _barMenu!.MenuBody.AddChild(element);
    }

    /// <summary>
    /// Takes the action the right-click menu was opened for out of the bar, wherever it has moved to since,
    /// then lets <paramref name="place"/> put it back.
    /// </summary>
    private void ApplyBarMenuItem(Action<EntityUid> place)
    {
        _barMenu?.Close();

        if (_barMenuAction is not { } action || _actionsSystem == null || !_actions.Remove(action))
            return;

        place(action);
        _userArranged = true;
        _container?.SetActionData(_actionsSystem, _actions.ToArray());
    }
}
