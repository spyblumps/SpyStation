using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Udder;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Animals;
/// <summary>
///     Gives the ability to produce milkable reagents;
///     produces endlessly if the owner does not have a HungerComponent.
/// </summary>
public sealed class UdderSystem : EntitySystem
{
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UdderComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<UdderComponent, GetVerbsEvent<AlternativeVerb>>(AddMilkVerb);
        SubscribeLocalEvent<UdderComponent, MilkingDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<UdderComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnMapInit(EntityUid uid, UdderComponent component, MapInitEvent args)
    {
        component.NextGrowth = _timing.CurTime + component.GrowthDelay;
    }

    private void OnEntRemoved(Entity<UdderComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity was our contained solution
        if (entity.Comp.Solution == null || args.Entity != entity.Comp.Solution.Value.Owner)
            return;

        // Cleared our cached reference to the solution entity
        entity.Comp.Solution = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<UdderComponent>();
        while (query.MoveNext(out var uid, out var udder))
        {
            if (_timing.CurTime < udder.NextGrowth)
                continue;

            udder.NextGrowth += udder.GrowthDelay;

            if (_mobState.IsDead(uid))
                continue;

            // Ensure the solution exists
            if (!_solutionContainerSystem.TryGetSolution(uid, udder.SolutionName, out var soln, out var solution))
                continue;

            if (solution.AvailableVolume == 0)
                continue;

            // Actually there is food digestion so no problem with instant reagent generation "OnFeed"
            if (TryComp(uid, out HungerComponent? hunger))
            {
                // Is there enough nutrition to produce reagent?
                if (_hunger.GetHungerThreshold(hunger) < HungerThreshold.Okay)
                    continue;

                _hunger.ModifyHunger(uid, -udder.HungerUsage, hunger);
            }

            //TODO: toxins from bloodstream !?
            _solutionContainerSystem.TryAddReagent(soln.Value, udder.ReagentId, udder.QuantityPerUpdate, out _);
        }
    }

    private void AttemptMilk(Entity<UdderComponent?> udder, EntityUid userUid, EntityUid containerUid)
    {
        if (!Resolve(udder, ref udder.Comp))
            return;

        var doargs = new DoAfterArgs(EntityManager, userUid, 5, new MilkingDoAfterEvent(), udder, udder, used: containerUid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
        };

        _doAfterSystem.TryStartDoAfter(doargs);
    }

    private void OnDoAfter(Entity<UdderComponent> entity, ref MilkingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Used == null)
            return;

        if (!_solutionContainerSystem.ResolveSolution(entity.Owner, entity.Comp.SolutionName, ref entity.Comp.Solution, out var solution))
            return;

        if (!_solutionContainerSystem.TryGetRefillableSolution(args.Args.Used.Value, out var targetSoln, out var targetSolution))
            return;

        args.Handled = true;
        var quantity = solution.Volume;
        if (quantity == 0)
        {
            _popupSystem.PopupClient(Loc.GetString("udder-system-dry"), entity.Owner, args.Args.User);
            return;
        }

        if (quantity > targetSolution.AvailableVolume)
            quantity = targetSolution.AvailableVolume;

        var split = _solutionContainerSystem.SplitSolution(entity.Comp.Solution.Value, quantity);
        _solutionContainerSystem.TryAddSolution(targetSoln.Value, split);

        _popupSystem.PopupClient(Loc.GetString("udder-system-success", ("amount", quantity), ("target", Identity.Entity(args.Args.Used.Value, EntityManager))), entity.Owner,
            args.Args.User, PopupType.Medium);
    }

    private void AddMilkVerb(Entity<UdderComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Using == null ||
             !args.CanInteract ||
             !HasComp<RefillableSolutionComponent>(args.Using.Value))
            return;

        var uid = entity.Owner;
        var user = args.User;
        var used = args.Using.Value;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                AttemptMilk(uid, user, used);
            },
            Text = Loc.GetString("udder-system-verb-milk"),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }
}
