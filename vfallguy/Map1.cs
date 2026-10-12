using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace vfallguy;

public class Map1 : Map
{
    private readonly List<AOESequence> _rotating = [];
    private readonly List<AOESequence> _pendulums = [];
    private AOESequence _slopePillars1 = null!;
    private AOESequence _slopePillars2 = null!;
    private AOESequence _slopePillars3 = null!;
    private AOESequence _gatePillarsA = null!;
    private AOESequence _gatePillarsB = null!;
    private readonly List<AOESequence> _lanes = [];

    private static PathfindMap BuildBaseMap()
    {
        // walls derived from BGCollision terrain scan (terrain_1165_20261012_180442.json)
        var map = new PathfindMap(0.5f, 0.25f, new(0, 10, -280), 20, 100, 60);

        // start pad
        map.BlockPixelsInsideAlignedRect(-30, -11.5f, -189, -180, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(11.5f, 30, -189, -180, 0, 60, 100, 0);
        // circles corridor
        map.BlockPixelsInsideAlignedRect(-30, -18.5f, -254, -189, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(18.5f, 30, -254, -189, 0, 60, 100, 0);
        // funnel + gate
        map.BlockPixelsInsideAlignedRect(-30, -7.5f, -263, -254, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(7.5f, 30, -263, -254, 0, 60, 100, 0);
        // pendulum section: only the central plateau progresses; side ledges (y6) are penalty zones
        // (being hit knocks you down there; they dead-end with a 4y climb back at the start)
        map.BlockPixelsInsideAlignedRect(-30, -9.5f, -336, -263, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(9.5f, 30, -336, -263, 0, 60, 100, 0);
        // slope
        map.BlockPixelsInsideAlignedRect(-30, -8.5f, -366, -336, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(8.5f, 30, -366, -336, 0, 60, 100, 0);
        // finish
        map.BlockPixelsInsideAlignedRect(-30, -14.5f, -380, -366, 0, 60, 100, 0);
        map.BlockPixelsInsideAlignedRect(14.5f, 30, -380, -366, 0, 60, 100, 0);

        return map;
    }

    private static readonly List<(float z, float y)> _heightProfile =
    [
        (-370f, 18.9f),
        (-362f, 18.4f),
        (-350f, 15.7f),
        (-342f, 12.8f),
        (-336f, 10f),
        (-280f, 10f),
        (-180f, 10f),
    ];

    public Map1(GameEvents events) : base(events, BuildBaseMap(), _heightProfile, -362)
    {
        MovementZones =
        [
            (-335f, -278f, 0.6f, false), // pendulum section: slippery, measured 3.6 yalm/s
            (-366f, -344f, 0.6f, true),  // slope: slippery + never stop (slide back)
        ];

        for (int i = 0; i < 5; ++i)
        {
            var z = -202 - 10 * i;
            var xs = (i & 1) == 0 ? new float[] { -10, 0, 10 } : new float[] { -10, 10 };
            foreach (var x in xs)
            {
                var p = new Vector3(x, 10, z);
                var inst = Enumerable.Repeat((p, 0f, 0.41f), 8).ToList();
                inst[0] = (p, 0f, 3.72f);
                _rotating.Add(CreateSequence(AOEShape.Circle, 5, 0, inst));
            }
        }

        AddPendulumRow(-321f, +0.5f);
        AddPendulumRow(-308f, -6.5f);
        AddPendulumRow(-301f, +0.5f);
        AddPendulumRow(-288f, -6.5f);
        AddPendulumRow(-281f, +0.5f);
        AddPendulumRow(-328f, -6.5f);

        _slopePillars1 = CreatePillarRow(1.6f, 13.7f, -347.8f, 3.5f, [-6.9f, -3.5f, 0f, 3.5f, 6.9f]);
        _slopePillars2 = CreatePillarRow(1.6f, 16.0f, -355.8f, 3.5f, [-6.9f, -3.5f, 0f, 3.5f, 6.9f]);
        _slopePillars3 = CreatePillarRow(1.6f, 18.2f, -363.8f, 3.5f, [-6.9f, -3.5f, 0f, 3.5f, 6.9f]);
        _gatePillarsA = CreatePillarRow(2.1f, 10f, -248.5f, 20.58f, [-17.6f, 3f]);
        _gatePillarsB = CreatePillarRow(2.1f, 10f, -251.5f, 19.71f, [-3f, 16.7f]);

        _lanes.Add(CreateLane(+5.5f, 17.8f, -363.7f));
        _lanes.Add(CreateLane(-5.5f, 17.8f, -363.7f));
        _lanes.Add(CreateLane(+1.8f, 17.2f, -364.7f));
        _lanes.Add(CreateLane(-1.8f, 17.2f, -364.7f));
    }

    protected override void OnActionEffect(uint actionId, Vector3 casterPos)
    {
        switch (actionId)
        {
            case 34773:
                UpdateSequence(casterPos, 0, _rotating);
                break;
            case 34774:
                if (casterPos.X > -100)
                    UpdateSequence(casterPos, 0, [_slopePillars1, _slopePillars2, _slopePillars3, _gatePillarsA, _gatePillarsB]);
                break;
            case 34775:
                UpdateSequence(casterPos, 0, _pendulums);
                break;
            case 34776:
                UpdateSequence(casterPos, 0, _lanes);
                break;
        }
    }

    private void AddPendulumRow(float z, float x0)
    {
        float y = 10;
        List<(Vector3, float, float)> l =
        [
            (new(x0, y, z), 0f, 1.79f),
            (new(x0 + 3, y, z), 0f, 0.20f),
            (new(x0 + 6, y, z), 0f, 0.12f),
            (new(x0 + 6, y, z), 0f, 1.81f),
            (new(x0 + 3, y, z), 0f, 0.25f),
            (new(x0, y, z), 0f, 0.10f),
        ];
        _pendulums.Add(CreateSequence(AOEShape.Square, 1.5f, 0, l));
    }

    private AOESequence CreatePillarRow(float delay, float y, float z, float len, float[] xs)
    {
        List<(Vector3, float, float)> l = [];
        for (int i = 0; i < xs.Length - 1; ++i)
            l.Add((new(xs[i], y, z), MathF.PI * 0.5f, delay));
        for (int i = 0; i < xs.Length - 1; ++i)
            l.Add((new(xs[xs.Length - 1 - i], y, z), -MathF.PI * 0.5f, delay));
        return CreateSequence(AOEShape.Rect, len, 1.5f, l);
    }

    private AOESequence CreateLane(float x, float y0, float z0)
    {
        List<(Vector3, float, float)> l = [];
        for (int k = 0; k < 13; ++k)
            l.Add((new(x, y0 - 0.41f * k, z0 + 1.583f * k), 0f, k == 12 ? 0.52f : 0.39f));
        return CreateSequence(AOEShape.Circle, 1.5f, 0, l);
    }
}
