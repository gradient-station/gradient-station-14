using System.Diagnostics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Shared.Movement.Components;

[RegisterComponent, ComponentReference(typeof(IMoverComponent))]
public class SharedPlayerInputTileMoverComponent : Component, IMoverComponent
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public override string Name => "PlayerInputTileMover";

    public float CurrentWalkSpeed { get; }
    public float CurrentSprintSpeed { get; }
    public bool Sprinting { get; }
    public Angle LastGridAngle { get; set; }
    public (Vector2 walking, Vector2 sprinting) VelocityDir { get; }

    public bool Lerping = false;
    public Vector2 OriginLerp = default;
    public Vector2 EndLerp => OriginLerp + LerpMovement;
    public float Accumulator = 0f;

    public Vector2 LerpMovement;
    public float LerpLength => LerpMovement.Length;

    public float TilesPerSecond = 2f;

    public void SetVelocityDirection(Direction direction, ushort subTick, bool enabled)
    {
        if (Lerping || !enabled)
            return;

        Lerping = true;
        var transform = _entMan.GetComponent<TransformComponent>(Owner);

        OriginLerp = transform.LocalPosition;
        LerpMovement = direction.ToVec();
        Accumulator = 0f;
    }

    public void SetSprinting(ushort subTick, bool walking)
    {

    }
}
