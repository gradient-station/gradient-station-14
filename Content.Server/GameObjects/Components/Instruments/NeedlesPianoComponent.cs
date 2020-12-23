#nullable enable
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Instruments
{
    [RegisterComponent]
    public class NeedlesPianoComponent : Component, IEquippedHand
    {
        public override string Name => "NeedlesPiano";

        [ViewVariables(VVAccess.ReadWrite)]
        private IPlayerSession? _lastUser;

        public void EquippedHand(EquippedHandEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor) || actor.playerSession == _lastUser) return;

            _lastUser = actor.playerSession;

            Owner.PopupMessage(eventArgs.User, Loc.GetString("Play Needles Piano Now"));
            EntitySystem.Get<AudioSystem>().PlayGlobal("/Audio/Misc/needles_piano_pickup.ogg", null,
                session => session == _lastUser);
        }
    }
}
