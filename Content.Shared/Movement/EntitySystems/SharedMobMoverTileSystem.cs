using System;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Shared.Movement.EntitySystems;

public class SharedMobMoverTileSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (mover, transform) in EntityManager.EntityQuery<SharedPlayerInputTileMoverComponent, TransformComponent>())
        {
            if (!mover.Lerping)
                continue;

            var moved = mover.TilesPerSecond * mover.Accumulator;

            var progress = Math.Clamp(moved / mover.LerpLength, 0f, 1f);

            transform.LocalPosition = Vector2.Lerp(mover.OriginLerp, mover.EndLerp, progress);

            mover.Accumulator += frameTime;

            if (progress >= 1f)
            {
                mover.Lerping = false;
                mover.Accumulator = 0f;
                mover.OriginLerp = default;
                mover.LerpMovement = default!;
            }
        }
    }
}
