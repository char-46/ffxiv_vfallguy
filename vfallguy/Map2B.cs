using System;
using System.Collections.Generic;
using System.Numerics;

namespace vfallguy;

public class Map2B : Map
{
    private AOESequence _chains1 = null!;
    private AOESequence _chains2 = null!;
    private AOESequence _chains3 = null!;
    private AOESequence _rockersA = null!;
    private AOESequence _rockersB1 = null!;
    private AOESequence _rockersB2 = null!;
    private AOESequence _stand = null!;
    private AOESequence _obstacles = null!;

    private static PathfindMap BuildBaseMap() => new(0.5f, 0.25f, new(-200, 6, 242), 35, 35, 8);

    public Map2B(GameEvents events) : base(events, BuildBaseMap(), [], -200)
    {
        _chains1 = CreateSequence(AOEShape.Circle, 1.5f, 0,
        [
            (new(-203f, 6, 226.9f), 0f, 1.0f),
            (new(-200f, 6, 226.9f), 0f, 1.0f),
            (new(-197f, 6, 226.9f), 0f, 1.0f),
        ]);
        _chains2 = CreateSequence(AOEShape.Circle, 1.5f, 0,
        [
            (new(-187.1f, 6, 256f), 0f, 1.0f),
            (new(-188.6f, 6, 258.6f), 0f, 1.0f),
            (new(-190.1f, 6, 261.2f), 0f, 1.0f),
        ]);
        _chains3 = CreateSequence(AOEShape.Circle, 1.5f, 0,
        [
            (new(-168f, 6, 267f), 0f, 1.0f),
            (new(-169.5f, 6, 269.6f), 0f, 1.0f),
            (new(-171f, 6, 272.2f), 0f, 1.0f),
        ]);

        _rockersA = CreateSequence(AOEShape.Square, 2f, 0,
        [
            (new(-204f, 6, 216.5f), 0f, 2.1f),
            (new(-196f, 6, 216.5f), 0f, 2.1f),
        ]);
        _rockersB1 = CreateSequence(AOEShape.Square, 2f, 0,
        [
            (new(-214.7f, 6, 255.9f), 0f, 1.0f),
            (new(-212.7f, 6, 259.4f), 0f, 1.0f),
            (new(-210.7f, 6, 262.9f), 0f, 3.0f),
        ]);
        _rockersB2 = CreateSequence(AOEShape.Square, 2f, 0,
        [
            (new(-228.6f, 6, 263.9f), 0f, 1.0f),
            (new(-226.6f, 6, 267.4f), 0f, 1.0f),
            (new(-224.6f, 6, 270.8f), 0f, 3.0f),
        ]);

        _stand = CreateSequence(AOEShape.Circle, 3f, 0, [(new(-200f, 6, 212f), 0f, 2.5f)]);

        List<(Vector3, float, float)> obs = [];
        foreach (var (x, z) in ObstaclePoints)
            obs.Add((new(x, 6, z), 0f, 1.4f));
        _obstacles = CreateSequence(AOEShape.Square, 1.5f, 0, obs);
    }

    private static readonly (float x, float z)[] ObstaclePoints =
    [
        (-212.2f, 253.2f), (-184.5f, 255.7f), (-187f, 257.8f), (-223.5f, 259.7f), (-176.6f, 259.7f),
        (-188.5f, 260.4f), (-226.1f, 261.1f), (-207.2f, 261.9f), (-189f, 263.6f), (-169.3f, 264.5f),
        (-169.7f, 267.8f), (-218.4f, 268.5f), (-181.7f, 268.5f), (-221.1f, 269.9f), (-171.2f, 270.3f),
        (-173.8f, 272.4f),
    ];

    protected override void OnActionEffect(uint actionId, Vector3 casterPos)
    {
        if (casterPos.X > -100)
            return;
        switch (actionId)
        {
            case 34716:
                UpdateSequence(casterPos, 0, [_chains1, _chains2, _chains3]);
                break;
            case 34774:
                UpdateSequence(casterPos, 0, _obstacles);
                break;
            case 34799:
                UpdateSequence(casterPos, 0, [_rockersA, _rockersB1, _rockersB2]);
                break;
            case 34800:
                UpdateSequence(casterPos, 0, _stand);
                break;
        }
    }
}
