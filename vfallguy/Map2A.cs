using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace vfallguy;

public class Map2A : Map
{
    // terrain baked from BGCollision scan terrain_1165_20261012_182536.json (x 165..235, z -150..-95)
    // ring bridge around a central chasm; two arms at y10
    private static readonly (float x1, float x2, float z1, float z2)[] _walls =
    [
            (164.5f, 183.5f, -147.5f, -150.5f),
            (185.5f, 188.5f, -148.5f, -150.5f),
            (189.5f, 190.5f, -148.5f, -150.5f),
            (191.5f, 202.5f, -149.5f, -150.5f),
            (203.5f, 208.5f, -149.5f, -150.5f),
            (210.5f, 235.5f, -145.5f, -150.5f),
            (191.5f, 203.5f, -148.5f, -149.5f),
            (204.5f, 208.5f, -148.5f, -149.5f),
            (185.5f, 187.5f, -147.5f, -148.5f),
            (188.5f, 190.5f, -146.5f, -148.5f),
            (191.5f, 204.5f, -147.5f, -148.5f),
            (205.5f, 208.5f, -147.5f, -148.5f),
            (164.5f, 184.5f, -144.5f, -147.5f),
            (185.5f, 186.5f, -145.5f, -147.5f),
            (191.5f, 205.5f, -146.5f, -147.5f),
            (206.5f, 209.5f, -146.5f, -147.5f),
            (187.5f, 190.5f, -144.5f, -146.5f),
            (191.5f, 206.5f, -145.5f, -146.5f),
            (207.5f, 209.5f, -145.5f, -146.5f),
            (191.5f, 207.5f, -144.5f, -145.5f),
            (208.5f, 235.5f, -144.5f, -145.5f),
            (164.5f, 183.5f, -141.5f, -144.5f),
            (187.5f, 208.5f, -143.5f, -144.5f),
            (212.5f, 215.5f, -143.5f, -144.5f),
            (216.5f, 235.5f, -143.5f, -144.5f),
            (187.5f, 189.5f, -142.5f, -143.5f),
            (190.5f, 208.5f, -141.5f, -143.5f),
            (212.5f, 214.5f, -142.5f, -143.5f),
            (217.5f, 235.5f, -142.5f, -143.5f),
            (186.5f, 189.5f, -141.5f, -142.5f),
            (212.5f, 213.5f, -141.5f, -142.5f),
            (218.5f, 235.5f, -141.5f, -142.5f),
            (164.5f, 180.5f, -140.5f, -141.5f),
            (183.5f, 189.5f, -140.5f, -141.5f),
            (190.5f, 209.5f, -140.5f, -141.5f),
            (211.5f, 213.5f, -140.5f, -141.5f),
            (219.5f, 235.5f, -140.5f, -141.5f),
            (164.5f, 179.5f, -139.5f, -140.5f),
            (184.5f, 188.5f, -139.5f, -140.5f),
            (191.5f, 212.5f, -139.5f, -140.5f),
            (220.5f, 235.5f, -139.5f, -140.5f),
            (164.5f, 178.5f, -137.5f, -139.5f),
            (185.5f, 187.5f, -138.5f, -139.5f),
            (191.5f, 211.5f, -138.5f, -139.5f),
            (221.5f, 235.5f, -137.5f, -139.5f),
            (186.5f, 188.5f, -137.5f, -138.5f),
            (191.5f, 210.5f, -137.5f, -138.5f),
            (164.5f, 177.5f, -135.5f, -137.5f),
            (187.5f, 209.5f, -136.5f, -137.5f),
            (222.5f, 235.5f, -135.5f, -137.5f),
            (188.5f, 210.5f, -135.5f, -136.5f),
            (164.5f, 176.5f, -132.5f, -135.5f),
            (188.5f, 211.5f, -134.5f, -135.5f),
            (223.5f, 235.5f, -133.5f, -135.5f),
            (188.5f, 212.5f, -133.5f, -134.5f),
            (187.5f, 212.5f, -132.5f, -133.5f),
            (224.5f, 235.5f, -129.5f, -133.5f),
            (164.5f, 175.5f, -128.5f, -132.5f),
            (186.5f, 213.5f, -130.5f, -132.5f),
            (185.5f, 214.5f, -125.5f, -130.5f),
            (225.5f, 235.5f, -120.5f, -129.5f),
            (164.5f, 174.5f, -121.5f, -128.5f),
            (185.5f, 215.5f, -124.5f, -125.5f),
            (185.5f, 214.5f, -119.5f, -124.5f),
            (164.5f, 175.5f, -117.5f, -121.5f),
            (224.5f, 235.5f, -116.5f, -120.5f),
            (186.5f, 213.5f, -117.5f, -119.5f),
            (164.5f, 176.5f, -114.5f, -117.5f),
            (187.5f, 212.5f, -116.5f, -117.5f),
            (188.5f, 211.5f, -114.5f, -116.5f),
            (223.5f, 235.5f, -114.5f, -116.5f),
            (164.5f, 177.5f, -112.5f, -114.5f),
            (189.5f, 210.5f, -113.5f, -114.5f),
            (222.5f, 235.5f, -112.5f, -114.5f),
            (191.5f, 208.5f, -112.5f, -113.5f),
            (164.5f, 178.5f, -111.5f, -112.5f),
            (192.5f, 207.5f, -111.5f, -112.5f),
            (221.5f, 235.5f, -110.5f, -112.5f),
            (164.5f, 179.5f, -109.5f, -111.5f),
            (194.5f, 205.5f, -110.5f, -111.5f),
            (220.5f, 235.5f, -109.5f, -110.5f),
            (164.5f, 180.5f, -108.5f, -109.5f),
            (219.5f, 235.5f, -108.5f, -109.5f),
            (164.5f, 181.5f, -107.5f, -108.5f),
            (218.5f, 235.5f, -107.5f, -108.5f),
            (164.5f, 182.5f, -106.5f, -107.5f),
            (217.5f, 235.5f, -106.5f, -107.5f),
            (164.5f, 183.5f, -105.5f, -106.5f),
            (216.5f, 235.5f, -105.5f, -106.5f),
            (164.5f, 184.5f, -104.5f, -105.5f),
            (215.5f, 235.5f, -104.5f, -105.5f),
            (164.5f, 186.5f, -103.5f, -104.5f),
            (214.5f, 235.5f, -103.5f, -104.5f),
            (164.5f, 187.5f, -102.5f, -103.5f),
            (212.5f, 235.5f, -102.5f, -103.5f),
            (164.5f, 189.5f, -101.5f, -102.5f),
            (210.5f, 235.5f, -101.5f, -102.5f),
            (164.5f, 192.5f, -100.5f, -101.5f),
            (207.5f, 235.5f, -100.5f, -101.5f),
            (164.5f, 196.5f, -99.5f, -100.5f),
            (203.5f, 235.5f, -99.5f, -100.5f),
            (164.5f, 235.5f, -94.5f, -99.5f),

    ];

    private static PathfindMap BuildBaseMap()
    {
        var map = new PathfindMap(0.5f, 0.25f, new(200, 10, -122), 37, 30, 60);
        foreach (var w in _walls)
            map.BlockPixelsInsideAlignedRect(w.x1, w.x2, w.z1, w.z2, 0, 60, 100, 0);
        return map;
    }

    private static readonly List<(float z, float y)> _heightProfile =
    [
        (-95f, 10f),
        (-150f, 10f),
    ];

    private AOESequence _pivot = null!;
    private readonly List<AOESequence> _bladeTips = [];
    private readonly List<AOESequence> _cornersA = [];
    private readonly List<AOESequence> _cornersB = [];

    // 2A 不要掉下去 (环形桥): 中央深渊 + 双刃旋转梁(34808 枢轴 0.62s, 34797/34798 刃尖 9.91s/圈) + 四角(34805)
    public override string DisplayName => "不要掉下去";

    public Map2A(GameEvents events) : base(events, BuildBaseMap(), _heightProfile, -146)
    {
        // 34808: central pivot pulsing every 0.62s (two-blade spinner, ~9.91s/rev, blades 180 deg apart)
        var c = new Vector3(200, 10, -125);
        _pivot = CreateSequence(AOEShape.Circle, 1.5f, 0, [(c, 0f, 0.62f)]);

        // 34797/34798: blade tip markers at the arm ends (1.44s + 8.47s gaps per revolution)
        foreach (var x in new float[] { 180, 220 })
        {
            var p = new Vector3(x, 10, -125);
            _bladeTips.Add(CreateSequence(AOEShape.Circle, 2f, 0, [(p, 0f, 1.44f), (p, 0f, 8.47f)]));
        }

        // 34805: trapezoid corner pairs, anti-phased (2.96s + 2.15s)
        foreach (var (x, z) in new[] { (183f, -108f), (211f, -114f) })
        {
            var p = new Vector3(x, 10, z);
            _cornersA.Add(CreateSequence(AOEShape.Circle, 2f, 0, [(p, 0f, 2.96f), (p, 0f, 2.15f)]));
        }
        foreach (var (x, z) in new[] { (217f, -108f), (189f, -114f) })
        {
            var p = new Vector3(x, 10, z);
            _cornersB.Add(CreateSequence(AOEShape.Circle, 2f, 0, [(p, 0f, 2.15f), (p, 0f, 2.96f)]));
        }

        // 35409: arm edge markers (sparse data; approximate cycle)
        foreach (var (x, z) in new[] { (176f, -121f), (185f, -122f), (215f, -128f), (224f, -129f) })
        {
            var p = new Vector3(x, 10, z);
            _bladeTips.Add(CreateSequence(AOEShape.Circle, 1.5f, 0, [(p, 0f, 1.44f), (p, 0f, 8.47f)]));
        }
    }

    protected override void OnActionEffect(uint actionId, Vector3 casterPos)
    {
        switch (actionId)
        {
            case 34808:
                UpdateSequence(casterPos, 0, [_pivot]);
                break;
            case 34797:
            case 34798:
            case 35409:
                UpdateSequence(casterPos, 0, _bladeTips);
                break;
            case 34805:
                UpdateSequence(casterPos, 0, [_cornersA[0], _cornersA[1], _cornersB[0], _cornersB[1]]);
                break;
        }
    }
}