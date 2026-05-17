using Eatah.Client.Services.Contracts;

namespace Eatah.Client.Services;

/// <summary>
/// Singleton state for the chat popup UI.
/// Components subscribe to OnChanged to re-render when the popup opens/closes.
/// </summary>
public sealed class ChatState
{
    public bool IsOpen { get; private set; }
    public ChatThreadSummaryResponse? ActiveThread { get; private set; }

    public event Action? OnChanged;

    public void Open(ChatThreadSummaryResponse? thread = null)
    {
        IsOpen = true;
        if (thread is not null) ActiveThread = thread;
        OnChanged?.Invoke();
    }

    public void Close()
    {
        IsOpen = false;
        OnChanged?.Invoke();
    }

    public void SetActiveThread(ChatThreadSummaryResponse thread)
    {
        ActiveThread = thread;
        IsOpen = true;
        OnChanged?.Invoke();
    }
}
