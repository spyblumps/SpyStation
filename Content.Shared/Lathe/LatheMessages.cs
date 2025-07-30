using Content.Shared.Research.Prototypes;
using NetSerializer;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Lathe;

[Serializable, NetSerializable]
public sealed class LatheUpdateState : BoundUserInterfaceState
{
    public List<ProtoId<LatheRecipePrototype>> Recipes;

    public ProtoId<LatheRecipePrototype>[] Queue;

    public ProtoId<LatheRecipePrototype>? CurrentlyProducing;

    public bool HasAnyBlueprints; // Corvax-Next-BlueprintEject

    public LatheUpdateState(List<ProtoId<LatheRecipePrototype>> recipes, ProtoId<LatheRecipePrototype>[] queue, ProtoId<LatheRecipePrototype>? currentlyProducing = null, bool hasAnyBlueprints = false) // Corvax-Next-BlueprintEject
    {
        Recipes = recipes;
        Queue = queue;
        CurrentlyProducing = currentlyProducing;
        HasAnyBlueprints = hasAnyBlueprints; // Corvax-Next-BlueprintEject
    }
}

// Corvax-Next-BlueprintEject-start

/// <summary>
///     Sent to the server to eject blueprints from the lathe.
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheBlueprintEjectMessage : BoundUserInterfaceMessage;

// Corvax-Next-BlueprintEject-end

/// <summary>
///     Sent to the server to sync material storage and the recipe queue.
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheSyncRequestMessage : BoundUserInterfaceMessage
{

}

/// <summary>
///     Sent to the server when a client queues a new recipe.
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheQueueRecipeMessage : BoundUserInterfaceMessage
{
    public readonly string ID;
    public readonly int Quantity;
    public LatheQueueRecipeMessage(string id, int quantity)
    {
        ID = id;
        Quantity = quantity;
    }
}

[NetSerializable, Serializable]
public enum LatheUiKey
{
    Key,
}
