using System;
using System.Collections.Generic;
using System.Numerics;

namespace vfallguy;

// 2B 金碟攻城 (x -240..-160, z 190..300):
// 起始平台(y6, z~200) -> 坑阵(y2 坑底, z 220-255, 平台间隙 1-6y 需跳跃, 少量 8-10y 不可跳) -> 大平台(z~260) -> 坑阵 -> 末端平台(z~290) + y18 城楼
// terrain baked from BGCollision scans (terrain_1165_20261012_*.json), walkable = y in [3.5, 7.5]
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

    public override string DisplayName => "金碟攻城";

    private static PathfindMap BuildBaseMap()
    {
        var map = new PathfindMap(0.5f, 0.25f, new(-200, 6, 245), 40, 55, 90);
        foreach (var (x1, x2, z1, z2) in _walls)
            map.BlockPixelsInsideAlignedRect(x1, x2, z1, z2, 0, 90, 100, 0);
        return map;
    }

    private static readonly (float x1, float x2, float z1, float z2)[] _walls =
    [
new(-240f, -159f, 189.5f, 197.5f),
        new(-240f, -202f, 197.5f, 198.5f),
        new(-197f, -159f, 197.5f, 198.5f),
        new(-240f, -204f, 198.5f, 199.5f),
        new(-195f, -159f, 198.5f, 199.5f),
        new(-240f, -205f, 199.5f, 200.5f),
        new(-194f, -159f, 199.5f, 200.5f),
        new(-240f, -206f, 200.5f, 201.5f),
        new(-193f, -159f, 200.5f, 201.5f),
        new(-240f, -207f, 201.5f, 203.5f),
        new(-192f, -159f, 201.5f, 203.5f),
        new(-240f, -208f, 203.5f, 208.5f),
        new(-191f, -159f, 203.5f, 207.5f),
        new(-196f, -195f, 206.5f, 207.5f),
        new(-207f, -159f, 207.5f, 208.5f),
        new(-240f, -159f, 208.5f, 210.5f),
        new(-240f, -187f, 210.5f, 211.5f),
        new(-181f, -159f, 210.5f, 211.5f),
        new(-240f, -220f, 211.5f, 212.5f),
        new(-179f, -159f, 211.5f, 212.5f),
        new(-240f, -222f, 212.5f, 213.5f),
        new(-206f, -205f, 212.5f, 213.5f),
        new(-194f, -193f, 212.5f, 213.5f),
        new(-177f, -159f, 212.5f, 213.5f),
        new(-240f, -224f, 213.5f, 214.5f),
        new(-210f, -205f, 213.5f, 214.5f),
        new(-194f, -189f, 213.5f, 214.5f),
        new(-175f, -159f, 213.5f, 214.5f),
        new(-240f, -225f, 214.5f, 215.5f),
        new(-214f, -205f, 214.5f, 215.5f),
        new(-194f, -185f, 214.5f, 215.5f),
        new(-174f, -159f, 214.5f, 215.5f),
        new(-240f, -226f, 215.5f, 216.5f),
        new(-216f, -205f, 215.5f, 216.5f),
        new(-194f, -183f, 215.5f, 216.5f),
        new(-173f, -159f, 215.5f, 216.5f),
        new(-240f, -228f, 216.5f, 217.5f),
        new(-218f, -205f, 216.5f, 217.5f),
        new(-194f, -181f, 216.5f, 217.5f),
        new(-171f, -159f, 216.5f, 217.5f),
        new(-240f, -229f, 217.5f, 218.5f),
        new(-220f, -205f, 217.5f, 218.5f),
        new(-194f, -179f, 217.5f, 218.5f),
        new(-170f, -159f, 217.5f, 218.5f),
        new(-240f, -230f, 218.5f, 219.5f),
        new(-221f, -205f, 218.5f, 219.5f),
        new(-194f, -178f, 218.5f, 219.5f),
        new(-169f, -159f, 218.5f, 219.5f),
        new(-240f, -231f, 219.5f, 221.5f),
        new(-223f, -205f, 219.5f, 220.5f),
        new(-194f, -176f, 219.5f, 220.5f),
        new(-168f, -159f, 219.5f, 221.5f),
        new(-224f, -205f, 220.5f, 221.5f),
        new(-194f, -175f, 220.5f, 221.5f),
        new(-240f, -230f, 221.5f, 222.5f),
        new(-225f, -205f, 221.5f, 222.5f),
        new(-194f, -174f, 221.5f, 222.5f),
        new(-169f, -159f, 221.5f, 222.5f),
        new(-240f, -229f, 222.5f, 223.5f),
        new(-226f, -205f, 222.5f, 223.5f),
        new(-194f, -172f, 222.5f, 223.5f),
        new(-170f, -159f, 222.5f, 223.5f),
        new(-240f, -228f, 223.5f, 224.5f),
        new(-227f, -205f, 223.5f, 224.5f),
        new(-194f, -159f, 223.5f, 225.5f),
        new(-240f, -205f, 224.5f, 225.5f),
        new(-240f, -203f, 225.5f, 228.5f),
        new(-196f, -159f, 225.5f, 228.5f),
        new(-240f, -205f, 228.5f, 241.5f),
        new(-194f, -159f, 228.5f, 241.5f),
        new(-238f, -205f, 241.5f, 242.5f),
        new(-194f, -161f, 241.5f, 242.5f),
        new(-238f, -211f, 242.5f, 243.5f),
        new(-209f, -205f, 242.5f, 243.5f),
        new(-194f, -190f, 242.5f, 243.5f),
        new(-188f, -161f, 242.5f, 243.5f),
        new(-238f, -212f, 243.5f, 244.5f),
        new(-207f, -205f, 243.5f, 244.5f),
        new(-194f, -192f, 243.5f, 244.5f),
        new(-187f, -161f, 243.5f, 245.5f),
        new(-239f, -212f, 244.5f, 245.5f),
        new(-239f, -213f, 245.5f, 247.5f),
        new(-186f, -160f, 245.5f, 247.5f),
        new(-239f, -212f, 247.5f, 248.5f),
        new(-187f, -160f, 247.5f, 248.5f),
        new(-239f, -211f, 248.5f, 249.5f),
        new(-188f, -160f, 248.5f, 249.5f),
        new(-239f, -209f, 249.5f, 251.5f),
        new(-190f, -160f, 249.5f, 251.5f),
        new(-239f, -212f, 251.5f, 252.5f),
        new(-189f, -160f, 251.5f, 252.5f),
        new(-239f, -213f, 252.5f, 253.5f),
        new(-187f, -160f, 252.5f, 254.5f),
        new(-239f, -215f, 253.5f, 254.5f),
        new(-239f, -217f, 254.5f, 255.5f),
        new(-188f, -160f, 254.5f, 255.5f),
        new(-239f, -218f, 255.5f, 256.5f),
        new(-187f, -185f, 255.5f, 256.5f),
        new(-182f, -160f, 255.5f, 256.5f),
        new(-239f, -220f, 256.5f, 257.5f),
        new(-180f, -160f, 256.5f, 257.5f),
        new(-239f, -222f, 257.5f, 258.5f),
        new(-178f, -160f, 257.5f, 258.5f),
        new(-238f, -224f, 258.5f, 259.5f),
        new(-177f, -161f, 258.5f, 259.5f),
        new(-238f, -225f, 259.5f, 260.5f),
        new(-175f, -161f, 259.5f, 260.5f),
        new(-238f, -227f, 260.5f, 261.5f),
        new(-205f, -202f, 260.5f, 262.5f),
        new(-197f, -194f, 260.5f, 261.5f),
        new(-191f, -190f, 260.5f, 261.5f),
        new(-173f, -161f, 260.5f, 261.5f),
        new(-238f, -229f, 261.5f, 262.5f),
        new(-197f, -193f, 261.5f, 262.5f),
        new(-192f, -188f, 261.5f, 262.5f),
        new(-171f, -161f, 261.5f, 262.5f),
        new(-237f, -230f, 262.5f, 263.5f),
        new(-207f, -202f, 262.5f, 263.5f),
        new(-197f, -189f, 262.5f, 263.5f),
        new(-170f, -162f, 262.5f, 263.5f),
        new(-237f, -232f, 263.5f, 264.5f),
        new(-208f, -202f, 263.5f, 264.5f),
        new(-197f, -190f, 263.5f, 264.5f),
        new(-168f, -162f, 263.5f, 265.5f),
        new(-237f, -234f, 264.5f, 265.5f),
        new(-210f, -202f, 264.5f, 265.5f),
        new(-197f, -188f, 264.5f, 265.5f),
        new(-237f, -236f, 265.5f, 266.5f),
        new(-212f, -202f, 265.5f, 266.5f),
        new(-197f, -186f, 265.5f, 266.5f),
        new(-169f, -165f, 265.5f, 266.5f),
        new(-164f, -162f, 265.5f, 266.5f),
        new(-214f, -185f, 266.5f, 267.5f),
        new(-168f, -166f, 266.5f, 267.5f),
        new(-215f, -183f, 267.5f, 268.5f),
        new(-217f, -181f, 268.5f, 269.5f),
        new(-219f, -179f, 269.5f, 270.5f),
        new(-220f, -178f, 270.5f, 271.5f),
        new(-222f, -176f, 271.5f, 272.5f),
        new(-172f, -171f, 271.5f, 272.5f),
        new(-224f, -174f, 272.5f, 273.5f),
        new(-173f, -169f, 272.5f, 273.5f),
        new(-226f, -170f, 273.5f, 274.5f),
        new(-228f, -171f, 274.5f, 275.5f),
        new(-229f, -169f, 275.5f, 276.5f),
        new(-230f, -169f, 276.5f, 277.5f),
        new(-229f, -170f, 277.5f, 278.5f),
        new(-228f, -171f, 278.5f, 279.5f),
        new(-227f, -172f, 279.5f, 280.5f),
        new(-232f, -231f, 280.5f, 281.5f),
        new(-226f, -173f, 280.5f, 281.5f),
        new(-168f, -166f, 280.5f, 281.5f),
        new(-234f, -229f, 281.5f, 282.5f),
        new(-225f, -174f, 281.5f, 282.5f),
        new(-170f, -165f, 281.5f, 282.5f),
        new(-236f, -229f, 282.5f, 283.5f),
        new(-224f, -175f, 282.5f, 283.5f),
        new(-170f, -163f, 282.5f, 283.5f),
        new(-240f, -229f, 283.5f, 286.5f),
        new(-223f, -176f, 283.5f, 284.5f),
        new(-170f, -159f, 283.5f, 286.5f),
        new(-221f, -177f, 284.5f, 285.5f),
        new(-220f, -179f, 285.5f, 286.5f),
        new(-240f, -228f, 286.5f, 287.5f),
        new(-218f, -181f, 286.5f, 287.5f),
        new(-171f, -159f, 286.5f, 287.5f),
        new(-240f, -227f, 287.5f, 288.5f),
        new(-216f, -183f, 287.5f, 288.5f),
        new(-172f, -159f, 287.5f, 288.5f),
        new(-240f, -225f, 288.5f, 289.5f),
        new(-213f, -185f, 288.5f, 289.5f),
        new(-174f, -159f, 288.5f, 289.5f),
        new(-240f, -224f, 289.5f, 290.5f),
        new(-210f, -188f, 289.5f, 290.5f),
        new(-175f, -159f, 289.5f, 290.5f),
        new(-240f, -222f, 290.5f, 291.5f),
        new(-210f, -189f, 290.5f, 293.5f),
        new(-177f, -159f, 290.5f, 291.5f),
        new(-240f, -220f, 291.5f, 292.5f),
        new(-179f, -159f, 291.5f, 292.5f),
        new(-240f, -218f, 292.5f, 293.5f),
        new(-181f, -159f, 292.5f, 293.5f),
        new(-240f, -216f, 293.5f, 294.5f),
        new(-211f, -188f, 293.5f, 295.5f),
        new(-183f, -159f, 293.5f, 294.5f),
        new(-240f, -213f, 294.5f, 295.5f),
        new(-186f, -159f, 294.5f, 295.5f),
        new(-240f, -159f, 295.5f, 300.5f),
    ];

    private static readonly List<(float z, float y)> _heightProfile = [(189f, 6f), (301f, 6f)];

    public Map2B(GameEvents events) : base(events, BuildBaseMap(), _heightProfile, 289)
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
