#if SPIRE1_MENU_PERFORMANCE_PROBE
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace Spire1.Spire1Code.Experimental.MenuPerformance;

// 实验门禁: 仅暂停 AnimatedBg 的隐藏背景, 不接管选人屏幕或联网大厅的处理.
internal static class CharacterSelectBackgroundPause
{
    private static readonly ConditionalWeakTable<Control, Controller> Controllers = new();

    internal static Controller? Attach(NCharacterSelectScreen screen)
    {
        RequireMainThread();
        if (!GodotObject.IsInstanceValid(screen))
            return null;

        Control? container = screen.GetNodeOrNull<Control>("AnimatedBg");
        return container is null ? null : Attach(container);
    }

    // 独立入口也供中央源码链接探针使用, 不依赖修改 MainFile 或场景资源.
    internal static Controller Attach(Control container)
    {
        RequireMainThread();
        if (!GodotObject.IsInstanceValid(container))
            throw new ArgumentException("背景容器已经失效.", nameof(container));
        if (Controllers.TryGetValue(container, out Controller? existing))
            return existing;

        var controller = new Controller(container);
        Controllers.Add(container, controller);
        try
        {
            controller.Start();
            return controller;
        }
        catch
        {
            controller.Dispose();
            throw;
        }
    }

    private static void RequireMainThread()
    {
        if (OS.GetThreadCallerId() != OS.GetMainThreadId())
            throw new InvalidOperationException("背景暂停控制器只能在 Godot 主线程操作.");
    }

    internal sealed class Controller : IDisposable
    {
        private readonly Control _container;
        private readonly Dictionary<Node, TrackedNode> _nodes = new(ReferenceEqualityComparer.Instance);
        private bool _lifetimeAttached;
        private bool _active;
        private bool _disposed;
        private bool _queued;
        private long _generation;
        private long _revision;

        internal Controller(Control container) => _container = container;

        internal void Start()
        {
            RequireMainThread();
            _container.TreeEntered += OnContainerEntered;
            _container.TreeExiting += OnContainerExiting;
            _lifetimeAttached = true;
            if (_container.IsInsideTree())
                OnContainerEntered();
        }

        private void OnContainerEntered()
        {
            RequireMainThread();
            if (_disposed || _active || !GodotObject.IsInstanceValid(_container))
                return;

            _active = true;
            Track(_container, watchVisibility: true);
            RequestReconcile();
        }

        private void OnContainerExiting()
        {
            RequireMainThread();
            StopWatching();
            // 仅容器的两条寿命订阅保留到 Dispose 或容器释放, 支持移出后重入.
        }

        private void StopWatching()
        {
            _active = false;
            _queued = false;
            _generation++;
            var previous = new List<TrackedNode>(_nodes.Values);
            _nodes.Clear();
            foreach (TrackedNode node in previous)
                node.DetachAndRestore();
        }

        public void Dispose()
        {
            RequireMainThread();
            if (_disposed)
                return;

            _disposed = true;
            if (_lifetimeAttached && GodotObject.IsInstanceValid(_container))
            {
                _container.TreeEntered -= OnContainerEntered;
                _container.TreeExiting -= OnContainerExiting;
            }
            _lifetimeAttached = false;
            StopWatching();
            if (Controllers.TryGetValue(_container, out Controller? current) && ReferenceEquals(current, this))
                Controllers.Remove(_container);
        }

        private bool IsCurrent(long generation) => !_disposed && _active && generation == _generation;

        private void RequestReconcile()
        {
            RequireMainThread();
            if (_disposed || !_active)
                return;

            _revision++;
            if (_queued)
                return;
            _queued = true;
            QueueBarrier(_generation);
        }

        private void QueueBarrier(long generation)
        {
            // 第一阶段只排第二阶段, 让 _Ready 中直接排入的 deferred 写入先完成.
            // Ready/结构/可见性事件发生在待执行期间时, 用 revision 重开屏障.
            Callable.From(() =>
            {
                if (!IsCurrent(generation))
                    return;
                long revision = _revision;
                Callable.From(() => ReconcileDeferred(generation, revision)).CallDeferred();
            }).CallDeferred();
        }

        private void ReconcileDeferred(long generation, long revision)
        {
            RequireMainThread();
            if (!IsCurrent(generation))
                return;
            if (revision != _revision)
            {
                QueueBarrier(generation);
                return;
            }

            _queued = false;
            try
            {
                Reconcile();
            }
            catch (Exception error)
            {
                // 失败时释放当前控制器, 不把半套暂停状态留给正式菜单.
                Dispose();
                MainFile.Logger.Error($"选人背景暂停实验已关闭: {error.Message}");
            }
        }

        private TrackedNode Track(Node node, bool watchVisibility)
        {
            if (!_nodes.TryGetValue(node, out TrackedNode? tracked))
            {
                tracked = new TrackedNode(this, node);
                _nodes.Add(node, tracked);
                tracked.Attach();
                RequestReconcile();
            }
            tracked.SetVisibilityBoundary(watchVisibility);
            return tracked;
        }

        private void Forget(TrackedNode tracked)
        {
            RequireMainThread();
            if (_nodes.TryGetValue(tracked.Node, out TrackedNode? current) && ReferenceEquals(current, tracked))
                _nodes.Remove(tracked.Node);
            tracked.DetachAndRestore();
        }

        private void Reconcile()
        {
            if (!GodotObject.IsInstanceValid(_container) || !_container.IsInsideTree())
            {
                StopWatching();
                return;
            }

            // 本轮才发现的节点至少再经过一轮屏障, 避免捕获尚在队列中的 _Ready 初始化.
            foreach (TrackedNode tracked in _nodes.Values)
                tracked.InitializationSettled = true;

            var seen = new HashSet<Node>(ReferenceEqualityComparer.Instance);
            var plan = new List<(TrackedNode Tracked, bool Hidden)>();
            var pending = new Stack<(Node Node, bool Hidden, bool WithinBackground)>();
            pending.Push((_container, false, false));

            // 先读树并生成计划, 再改模式. 模式通知可能同步改变结构, 不在枚举时写树.
            while (pending.Count != 0)
            {
                var item = pending.Pop();
                Node node = item.Node;
                if (!GodotObject.IsInstanceValid(node) || !node.IsInsideTree() || !seen.Add(node))
                    continue;

                bool isContainer = ReferenceEquals(node, _container);
                bool boundary = node is CanvasItem && (isContainer || !item.WithinBackground);
                bool hidden = item.Hidden || (boundary && !((CanvasItem)node).IsVisibleInTree());
                bool withinBackground = item.WithinBackground || (!isContainer && node is CanvasItem);
                plan.Add((Track(node, boundary), hidden));
                int count = node.GetChildCount(includeInternal: true);
                for (int i = count - 1; i >= 0; i--)
                    pending.Push((node.GetChild(i, includeInternal: true), hidden, withinBackground));
            }

            // 常规移出由 TreeExiting 同步恢复. 此处还回收不再属于容器的遗留记录.
            foreach (TrackedNode old in new List<TrackedNode>(_nodes.Values))
            {
                if (!seen.Contains(old.Node))
                    Forget(old);
            }

            foreach (var item in plan)
            {
                if (_disposed || !_active)
                    break;
                TrackedNode tracked = item.Tracked;
                Node node = tracked.Node;
                if (!_nodes.TryGetValue(node, out TrackedNode? current) || !ReferenceEquals(current, tracked))
                    continue;
                if (!GodotObject.IsInstanceValid(node) || !node.IsInsideTree() ||
                    !GodotObject.IsInstanceValid(_container) ||
                    (!ReferenceEquals(node, _container) && !_container.IsAncestorOf(node)))
                {
                    Forget(tracked);
                    continue;
                }

                if (item.Hidden)
                {
                    // 不在 _EnterTree/尚未结束的 _Ready 中捕获初始化前的模式.
                    if (tracked.InitializationSettled && node.IsNodeReady())
                        tracked.Pause();
                }
                else
                {
                    tracked.Restore();
                }
            }
        }

        private sealed class TrackedNode
        {
            private readonly Controller _owner;
            private readonly CanvasItem? _canvasItem;
            private bool _attached;
            private bool _watchVisibility;
            private bool _hasOriginal;
            private Godot.Node.ProcessModeEnum _original;

            internal Node Node { get; }
            internal bool InitializationSettled { get; set; }

            internal TrackedNode(Controller owner, Node node)
            {
                _owner = owner;
                Node = node;
                _canvasItem = node as CanvasItem;
            }

            internal void Attach()
            {
                _attached = true;
                Node.Ready += Changed;
                Node.ChildEnteredTree += ChildEntered;
                Node.ChildOrderChanged += Changed;
                if (!ReferenceEquals(Node, _owner._container))
                    Node.TreeExiting += Exiting;
            }

            internal void SetVisibilityBoundary(bool enabled)
            {
                if (_canvasItem is null || _watchVisibility == enabled)
                    return;
                if (enabled)
                    _canvasItem.VisibilityChanged += Changed;
                else
                    _canvasItem.VisibilityChanged -= Changed;
                _watchVisibility = enabled;
            }

            private void Changed() => _owner.RequestReconcile();
            private void ChildEntered(Node child) => _owner.RequestReconcile();
            private void Exiting() => _owner.Forget(this);

            internal void Pause()
            {
                if (!_hasOriginal)
                {
                    _original = Node.ProcessMode;
                    _hasOriginal = true;
                }
                // 重复隐藏不得把原值覆盖为 Disabled. 原本 Disabled 也单独记忆.
                if (Node.ProcessMode != Godot.Node.ProcessModeEnum.Disabled)
                    Node.ProcessMode = Godot.Node.ProcessModeEnum.Disabled;
            }

            internal void Restore()
            {
                if (!_hasOriginal)
                    return;
                Godot.Node.ProcessModeEnum original = _original;
                _hasOriginal = false;
                if (GodotObject.IsInstanceValid(Node) && Node.ProcessMode != original)
                    Node.ProcessMode = original;
            }

            internal void DetachAndRestore()
            {
                if (_attached && GodotObject.IsInstanceValid(Node))
                {
                    Node.Ready -= Changed;
                    Node.ChildEnteredTree -= ChildEntered;
                    Node.ChildOrderChanged -= Changed;
                    if (!ReferenceEquals(Node, _owner._container))
                        Node.TreeExiting -= Exiting;
                    if (_watchVisibility && _canvasItem is not null)
                        _canvasItem.VisibilityChanged -= Changed;
                }
                _attached = false;
                _watchVisibility = false;
                Restore();
            }
        }
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen._Ready))]
internal static class CharacterSelectBackgroundPauseReadyPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCharacterSelectScreen __instance)
    {
        try
        {
            CharacterSelectBackgroundPause.Attach(__instance);
        }
        catch (Exception error)
        {
            MainFile.Logger.Error($"选人背景暂停实验未装配: {error.Message}");
        }
    }
}
#endif
