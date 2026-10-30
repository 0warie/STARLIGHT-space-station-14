using System.Linq;
using Content.Client.Actions;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Shared.Actions.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

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

    /// <summary>
    /// The bar button currently showing the insert marker while dragging, if any.
    /// </summary>
    private ActionButton? _insertMarkerButton;

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
            else if (_actions[targetIndex] == null)
                _actions[targetIndex] = action;
            else
                _actions.Insert(targetIndex, action);

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
}
