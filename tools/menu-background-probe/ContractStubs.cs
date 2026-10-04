using System.Runtime.CompilerServices;
namespace Godot
{
    public class GodotObject
    {
        internal bool Valid = true;
        public static bool IsInstanceValid(GodotObject? value) => value is { Valid: true };
    }
    public static class OS
    {
        public static ulong Caller = 1;
        public static ulong GetThreadCallerId() => Caller;
        public static ulong GetMainThreadId() => 1;
    }
    public sealed class Callable
    {
        private static readonly Queue<Action> Queue = new();
        private readonly Action _action;
        private Callable(Action action) => _action = action;
        public static Callable From(Action action) => new(action);
        public void CallDeferred() => Queue.Enqueue(_action);
        public static int Pending => Queue.Count;
        public static void Reset() => Queue.Clear();
        public static void Drain()
        {
            int count = 0;
            while (Queue.Count > 0)
            {
                if (++count > 1000) throw new Exception("deferred queue did not settle");
                Queue.Dequeue()();
            }
        }
    }
    public class Node : GodotObject
    {
        public enum ProcessModeEnum { Inherit, Pausable, WhenPaused, Always, Disabled }
        public event Action? Ready, TreeEntered, TreeExiting, ChildOrderChanged;
        public event Action<Node>? ChildEnteredTree;
        private readonly List<Node> _children = new();
        private ProcessModeEnum _mode;
        private bool _inside, _ready;
        public string Name = "";
        public Node? Parent { get; private set; }
        public Action<ProcessModeEnum>? ModeChanged;
        public ProcessModeEnum ProcessMode { get => _mode; set { _mode = value; ModeChanged?.Invoke(value); } }
        public bool IsInsideTree() => _inside;
        public bool IsNodeReady() => _ready;
        public int GetChildCount(bool includeInternal = false) => _children.Count;
        public Node GetChild(int index, bool includeInternal = false) => _children[index];
        public T? GetNodeOrNull<T>(string path) where T : Node => _children.FirstOrDefault(n => n.Name == path) as T;
        public bool IsAncestorOf(Node child)
        {
            for (Node? at = child.Parent; at != null; at = at.Parent) if (ReferenceEquals(at, this)) return true;
            return false;
        }
        public void AddChild(Node child)
        {
            child.Parent?.RemoveChild(child); _children.Add(child); child.Parent = this;
            if (_inside) child.Enter(); ChildOrderChanged?.Invoke();
        }
        public void RemoveChild(Node child)
        {
            if (!_children.Contains(child)) return;
            child.Exit(); _children.Remove(child); child.Parent = null; ChildOrderChanged?.Invoke();
        }
        public void Enter()
        {
            if (_inside) return;
            _inside = true; TreeEntered?.Invoke(); Parent?.ChildEnteredTree?.Invoke(this);
            foreach (var child in _children.ToArray()) child.Enter();
            if (!_ready) { _ready = true; _Ready(); Ready?.Invoke(); }
        }
        public void Exit()
        {
            if (!_inside) return;
            foreach (var child in _children.ToArray()) child.Exit();
            TreeExiting?.Invoke(); _inside = false;
        }
        public void Destroy() { Parent?.RemoveChild(this); Exit(); Valid = false; }
        public virtual void _Ready() { }
        internal void EmitVisibility()
        {
            if (this is CanvasItem canvas) canvas.NotifyVisibility();
            foreach (var child in _children.ToArray()) child.EmitVisibility();
        }
    }
    public class CanvasItem : Node
    {
        public event Action? VisibilityChanged;
        private bool _visible = true;
        public bool Visible { get => _visible; set { _visible = value; EmitVisibility(); } }
        public bool IsVisibleInTree()
        {
            if (!IsInsideTree() || !Visible) return false;
            for (Node? at = Parent; at != null; at = at.Parent) if (at is CanvasItem c && !c.Visible) return false;
            return true;
        }
        internal void NotifyVisibility() => VisibilityChanged?.Invoke();
    }
    public class Control : CanvasItem { }
}
namespace MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect
{
    public class NCharacterSelectScreen : Godot.Control
    {
        public int ReadyCalls;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void _Ready() => ReadyCalls++;
    }
}
namespace Spire1.Spire1Code
{
    public static class MainFile
    {
        public static readonly ProbeLogger Logger = new();
        public sealed class ProbeLogger
        {
            public readonly List<string> Errors = new();
            public void Error(string message) => Errors.Add(message);
        }
    }
}