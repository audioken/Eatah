using Microsoft.AspNetCore.Components;

namespace Eatah.Client.Services;

/// <summary>
/// Application-wide modal stack. UI mounts <see cref="Components.Shared.ModalHost"/> once
/// in <c>MainLayout</c>; pages call <see cref="Show{TComponent}"/> to open a modal.
/// </summary>
public sealed class ModalService
{
    private readonly List<ModalInstance> _stack = new();

    public IReadOnlyList<ModalInstance> Stack => _stack;

    public event Action? OnChange;

    public ModalInstance Show<TComponent>(IDictionary<string, object?>? parameters = null)
        where TComponent : ComponentBase
    {
        var instance = new ModalInstance(typeof(TComponent), parameters);
        _stack.Add(instance);
        OnChange?.Invoke();
        return instance;
    }

    public void Close(ModalInstance instance, object? result = null)
    {
        if (!_stack.Remove(instance)) return;
        instance.SetResult(result);
        OnChange?.Invoke();
    }

    public void CloseTop(object? result = null)
    {
        if (_stack.Count == 0) return;
        Close(_stack[^1], result);
    }
}

public sealed class ModalInstance
{
    private readonly TaskCompletionSource<object?> _tcs = new();

    public ModalInstance(Type componentType, IDictionary<string, object?>? parameters)
    {
        ComponentType = componentType;
        Parameters = parameters ?? new Dictionary<string, object?>();
    }

    public Type ComponentType { get; }
    public IDictionary<string, object?> Parameters { get; }
    public Task<object?> Result => _tcs.Task;

    internal void SetResult(object? result) => _tcs.TrySetResult(result);
}
