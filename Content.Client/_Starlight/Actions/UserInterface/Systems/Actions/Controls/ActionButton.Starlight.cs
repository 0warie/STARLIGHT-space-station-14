using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

// ReSharper disable CheckNamespace
// The namespace must match ActionButton for this partial class.
namespace Content.Client.UserInterface.Systems.Actions.Controls;

public sealed partial class ActionButton
{
    public bool RemoteSelected { get; private set; }

    /// <summary>
    /// Thin bar on one edge showing where a dragged action will be inserted. Created on first use.
    /// </summary>
    private PanelContainer? _insertMarker;

    public void SetRemoteSelected(bool selected)
    {
        RemoteSelected = selected;
        DrawModeChanged();
    }

    public void ShowInsertMarker(ActionInsertSide side, bool vertical)
    {
        var after = side == ActionInsertSide.After;
        var hAlign = vertical ? HAlignment.Stretch : after ? HAlignment.Right : HAlignment.Left;
        var vAlign = !vertical ? VAlignment.Stretch : after ? VAlignment.Bottom : VAlignment.Top;

        if (_insertMarker == null)
        {
            _insertMarker = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat(Color.White.WithAlpha(0.8f)),
                MouseFilter = MouseFilterMode.Ignore,
                MinSize = new Vector2(4, 4),
            };
            AddChild(_insertMarker);
        }

        // Pinned to the edge on the bar's axis, stretched across the other.
        _insertMarker.HorizontalAlignment = hAlign;
        _insertMarker.VerticalAlignment = vAlign;

        _insertMarker.Visible = true;
    }

    public void HideInsertMarker()
    {
        if (_insertMarker != null)
            _insertMarker.Visible = false;
    }
}

public enum ActionInsertSide : byte
{
    Before,
    After,
}
