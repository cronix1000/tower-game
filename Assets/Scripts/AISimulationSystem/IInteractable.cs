public interface IInteractable
{
    /// <summary>
    /// Interact with the AI agent.
    /// </summary>
    /// <param name="agent">The AI agent interacting with this object.</param>
    void Interact(AIAgent agent);
}