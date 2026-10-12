using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace vfallguy;

public class Map1B : Map
{
    // terrain baked from BGCollision scan terrain_1165_20261012_182536.json (x -220..-180, z -292..-98)
    // walkable = ray hit (multi-level climbing section projected to 2D; voids/pits blocked)
    private static readonly (float x1, float x2, float z1, float z2)[] _walls =
    [
            (-220.5f, -207.5f, -246.5f, -292.5f),
            (-206.5f, -203.5f, -289.5f, -292.5f),
            (-197.5f, -193.5f, -289.5f, -292.5f),
            (-192.5f, -179.5f, -246.5f, -292.5f),
            (-201.5f, -198.5f, -279.5f, -283.5f),
            (-201.5f, -198.5f, -267.5f, -271.5f),
            (-206.5f, -203.5f, -248.5f, -252.5f),
            (-197.5f, -193.5f, -248.5f, -252.5f),
            (-220.5f, -210.5f, -203.5f, -246.5f),
            (-189.5f, -179.5f, -186.5f, -246.5f),
            (-220.5f, -209.5f, -186.5f, -203.5f),
            (-201.5f, -197.5f, -186.5f, -203.5f),
            (-220.5f, -215.5f, -179.5f, -186.5f),
            (-184.5f, -179.5f, -172.5f, -186.5f),
            (-220.5f, -214.5f, -173.5f, -179.5f),
            (-206.5f, -203.5f, -174.5f, -179.5f),
            (-195.5f, -193.5f, -174.5f, -179.5f),
            (-202.5f, -201.5f, -174.5f, -175.5f),
            (-213.5f, -190.5f, -173.5f, -174.5f),
            (-220.5f, -185.5f, -172.5f, -173.5f),
            (-220.5f, -179.5f, -171.5f, -172.5f),
            (-220.5f, -193.5f, -170.5f, -171.5f),
            (-184.5f, -179.5f, -161.5f, -171.5f),
            (-220.5f, -214.5f, -162.5f, -170.5f),
            (-206.5f, -203.5f, -162.5f, -170.5f),
            (-195.5f, -193.5f, -162.5f, -170.5f),
            (-220.5f, -215.5f, -161.5f, -162.5f),
            (-220.5f, -214.5f, -160.5f, -161.5f),
            (-185.5f, -179.5f, -160.5f, -161.5f),
            (-220.5f, -213.5f, -159.5f, -160.5f),
            (-186.5f, -179.5f, -159.5f, -160.5f),
            (-220.5f, -212.5f, -158.5f, -159.5f),
            (-187.5f, -179.5f, -158.5f, -159.5f),
            (-220.5f, -211.5f, -154.5f, -158.5f),
            (-188.5f, -179.5f, -154.5f, -158.5f),
            (-220.5f, -212.5f, -153.5f, -154.5f),
            (-187.5f, -179.5f, -153.5f, -154.5f),
            (-220.5f, -213.5f, -151.5f, -153.5f),
            (-186.5f, -179.5f, -151.5f, -153.5f),
            (-220.5f, -214.5f, -150.5f, -151.5f),
            (-185.5f, -179.5f, -150.5f, -151.5f),
            (-220.5f, -215.5f, -147.5f, -150.5f),
            (-184.5f, -179.5f, -147.5f, -150.5f),
            (-202.5f, -197.5f, -147.5f, -149.5f),
            (-220.5f, -213.5f, -144.5f, -147.5f),
            (-204.5f, -195.5f, -144.5f, -147.5f),
            (-186.5f, -179.5f, -144.5f, -147.5f),
            (-210.5f, -209.5f, -144.5f, -146.5f),
            (-192.5f, -191.5f, -144.5f, -146.5f),
            (-220.5f, -215.5f, -139.5f, -144.5f),
            (-202.5f, -197.5f, -141.5f, -144.5f),
            (-184.5f, -179.5f, -139.5f, -144.5f),
            (-203.5f, -197.5f, -140.5f, -141.5f),
            (-214.5f, -213.5f, -139.5f, -140.5f),
            (-209.5f, -207.5f, -138.5f, -140.5f),
            (-205.5f, -197.5f, -139.5f, -140.5f),
            (-196.5f, -195.5f, -139.5f, -140.5f),
            (-190.5f, -189.5f, -138.5f, -140.5f),
            (-186.5f, -185.5f, -139.5f, -140.5f),
            (-220.5f, -213.5f, -136.5f, -139.5f),
            (-205.5f, -195.5f, -136.5f, -139.5f),
            (-186.5f, -179.5f, -138.5f, -139.5f),
            (-187.5f, -179.5f, -136.5f, -138.5f),
            (-220.5f, -215.5f, -132.5f, -136.5f),
            (-203.5f, -197.5f, -132.5f, -136.5f),
            (-184.5f, -179.5f, -132.5f, -136.5f),
            (-210.5f, -209.5f, -133.5f, -134.5f),
            (-192.5f, -191.5f, -133.5f, -134.5f),
            (-220.5f, -213.5f, -128.5f, -132.5f),
            (-204.5f, -195.5f, -128.5f, -132.5f),
            (-187.5f, -179.5f, -129.5f, -132.5f),
            (-186.5f, -179.5f, -128.5f, -129.5f),
            (-220.5f, -215.5f, -126.5f, -128.5f),
            (-202.5f, -197.5f, -126.5f, -128.5f),
            (-184.5f, -179.5f, -126.5f, -128.5f),
            (-220.5f, -211.5f, -124.5f, -126.5f),
            (-188.5f, -179.5f, -124.5f, -126.5f),
            (-220.5f, -212.5f, -121.5f, -124.5f),
            (-187.5f, -179.5f, -121.5f, -124.5f),
            (-220.5f, -213.5f, -118.5f, -121.5f),
            (-186.5f, -179.5f, -118.5f, -121.5f),
            (-220.5f, -214.5f, -116.5f, -118.5f),
            (-185.5f, -179.5f, -116.5f, -118.5f),
            (-220.5f, -215.5f, -99.5f, -116.5f),
            (-184.5f, -179.5f, -99.5f, -116.5f),
            (-220.5f, -179.5f, -97.5f, -99.5f),

    ];

    private static PathfindMap BuildBaseMap()
    {
        var map = new PathfindMap(0.5f, 0.25f, new(-200, 13, -195), 22, 100, 90);
        foreach (var w in _walls)
            map.BlockPixelsInsideAlignedRect(w.x1, w.x2, w.z1, w.z2, 0, 60, 100, 0);
        return map;
    }

    private static readonly List<(float z, float y)> _heightProfile =
    [
        (-98f, 11.5f),
        (-128f, 11.5f),
        (-130f, 14f),
        (-147f, 17f),
        (-232f, 16.5f),
        (-240f, 12f),
        (-292f, 10.5f),
    ];

    private readonly List<AOESequence> _chains = [];
    private AOESequence _rowBand1 = null!;
    private AOESequence _rowBand2 = null!;
    private AOESequence _gate1 = null!;
    private AOESequence _gate2 = null!;

    // 1B 金碟狂热 (攀爬迷宫): 链阵(34716, 1.4s/4.2s) + 带(34717) + 门(34718, 8.15s 反相)
    // 特殊机制: 冰地板(滑)、加速度炸弹(状态期间移动会爆炸, 需停手)、手指指引(按住W固定手指方向并按其移动/松开W手指旋转)、交替出现/消失的地板
    // -> 手指/消失地板段落应加入 NoAutoZones 手动通过 (z 范围待采)
    public override string DisplayName => "金碟狂热";

    public Map1B(GameEvents events) : base(events, BuildBaseMap(), _heightProfile, -286)
    {
        // 34716: pulsing chain nodes (1.4s active / 4.2s cycle) on the climbing terraces
        var chainXs = new float[] { -188, -191, -194, -197, -200, -203, -206, -209, -212 };
        foreach (var x in chainXs)
        {
            foreach (var (z, y) in new[] { (-130f, 13f), (-139f, 15f), (-147f, 17f) })
            {
                var p = new Vector3(x, y, z);
                _chains.Add(CreateSequence(AOEShape.Circle, 1.5f, 0, [(p, 0f, 1.4f), (p, 0f, 2.8f)]));
            }
        }
        foreach (var (x, z, y) in new[] { (-191f, -239f, 12f), (-194f, -239f, 12f), (-197f, -239f, 12f), (-203f, -239f, 12f), (-206f, -239f, 12f), (-194f, -225f, 15f), (-197f, -225f, 15f), (-206f, -225f, 15f), (-200f, -220f, 17f), (-203f, -220f, 17f), (-197f, -232f, 14f), (-200f, -232f, 14f) })
        {
            var p = new Vector3(x, y, z);
            _chains.Add(CreateSequence(AOEShape.Circle, 1.5f, 0, [(p, 0f, 1.4f), (p, 0f, 2.8f)]));
        }

        // 34717: dense line bands (1s active; zone1 cycle ~9.6s, zone2 cycle ~11.6s)
        _rowBand1 = CreateRowBand(-200, 17.5f, -172, 26, 9.6f);
        _rowBand2 = CreateRowBand(-200, 17.5f, -196, 16, 11.6f);

        // 34718: gate rows at z -261/-281 (x -206..-194), anti-phased, cycle 8.15s
        _gate1 = CreateGate(-200, 10.5f, -261, 12, 0f);
        _gate2 = CreateGate(-200, 10.5f, -281, 12, 4.1f);
    }

    protected override void OnActionEffect(uint actionId, Vector3 casterPos)
    {
        switch (actionId)
        {
            case 34716:
                UpdateSequence(casterPos, 0, _chains);
                break;
            case 34717:
                UpdateSequence(casterPos, 0, [_rowBand1, _rowBand2]);
                break;
            case 34718:
                UpdateSequence(casterPos, 0, [_gate1, _gate2]);
                break;
        }
    }

    private AOESequence CreateRowBand(float x, float y, float z, float len, float cycle)
    {
        var p = new Vector3(x, y, z);
        return CreateSequence(AOEShape.Rect, len, 6, [(p, MathF.PI * 0.5f, 1f), (p, MathF.PI * 0.5f, cycle - 1f)]);
    }

    private AOESequence CreateGate(float x, float y, float z, float len, float delay)
    {
        var p = new Vector3(x, y, z);
        return CreateSequence(AOEShape.Rect, len, 1.5f, [(p, MathF.PI * 0.5f, delay), (p, MathF.PI * 0.5f, 8.15f - delay)]);
    }
}