
using System.Collections.Generic;
using UnityEngine;
using Utils.Vibility;
namespace AISimulationSystem
{
    /// <summary>
    /// Interface for interactable objects in the AI simulation system.
    /// </summary>

public abstract class Interactable : MonoBehaviour
{
    /// <summary>
    /// Interact with the AI agent.
    /// </summary>
    /// <param name="agent">The AI agent interacting with this object.</param>
    public abstract void Interact(Actor agent);
}
}