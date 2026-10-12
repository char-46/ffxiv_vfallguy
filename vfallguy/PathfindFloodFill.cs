using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace vfallguy;

public class PathfindFloodFill
{
    public const int MaxAirSteps = 4; // ~1s airtime ≈ 6y at run speed
    private static readonly (int dx, int dy)[] LandMoves = [(0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)];

    public int NextT { get; private set; }
    private PathfindMap _map;
    private BitArray _solutions; // true if voxel is reachable on the ground
    private BitArray[] _air = []; // [k] = cells occupied mid-jump at hop k (1..MaxAirSteps); ignores wall blocking (bounded by airtime)
    private int[] _airCount = new int[MaxAirSteps + 1];
    private List<(int dx, int dy)> _moves = [];
    private List<(int dx, int dy)> _airMoves = [];
    private List<(int yMin, int yMax, List<(int dx, int dy)> moves)> _zones = [];
    private int _goalDir; // -1 = goal at smaller y, +1 = goal at larger y

    public bool Solution(int x, int y, int t) => _map.InBounds(x, y, t) ? _solutions[t * _map.Width * _map.Height + y * _map.Width + x] : false;
    public bool this[int x, int y, int t] => Solution(x, y, t);

    public PathfindFloodFill(PathfindMap map, float speed, int startX, int startY, int startT, int goalY, List<(int yMin, int yMax, float mul, bool noStay)>? zones = null)
    {
        _map = map;
        _solutions = new(map.Voxels.Length);
        _air = new BitArray[MaxAirSteps + 1];
        for (int k = 1; k <= MaxAirSteps; ++k)
            _air[k] = new(map.Voxels.Length);
        _goalDir = goalY >= startY ? 1 : -1;

        _moves = BuildMoves(speed * map.TimeResolution * map.InvSpaceResolution, false);
        _airMoves = _moves.Where(m => Math.Abs(m.dx) + Math.Abs(m.dy) >= 3).ToList();
        if (zones != null)
        {
            foreach (var z in zones)
                _zones.Add((z.yMin, z.yMax, BuildMoves(speed * z.mul * map.TimeResolution * map.InvSpaceResolution, z.noStay)));
        }

        if (_map.InBounds(startX, startY, startT))
            _solutions[startT * _map.Width * _map.Height + startY * _map.Width + startX] = true;
        NextT = startT + 1;
    }

    private static List<(int dx, int dy)> BuildMoves(float r, bool noStay)
    {
        var moves = new List<(int dx, int dy)>();
        var ir = (int)r;
        var rsq = r * r;
        var l = (int)(r / Math.Sqrt(2));
        if (!noStay)
            moves.Add((0, 0));
        moves.Add((0, ir));
        moves.Add((0, -ir));
        moves.Add((ir, 0));
        moves.Add((-ir, 0));
        for (int x = 1; x <= l; ++x)
        {
            var y = (int)Math.Sqrt(rsq - x * x);
            moves.Add((x, y));
            moves.Add((x, -y));
            moves.Add((-x, y));
            moves.Add((-x, -y));
            if (x != y)
            {
                moves.Add((y, x));
                moves.Add((y, -x));
                moves.Add((-y, x));
                moves.Add((-y, -x));
            }
        }
        return moves;
    }

    private List<(int dx, int dy)> MovesFor(int y)
    {
        foreach (var z in _zones)
            if (y >= z.yMin && y <= z.yMax)
                return z.moves;
        return _moves;
    }

    private IEnumerable<List<(int dx, int dy)>> AllMoveLists
    {
        get
        {
            yield return _moves;
            foreach (var z in _zones)
                yield return z.moves;
        }
    }

    public (int t, int minY, int maxY) ExecuteStep()
    {
        if (NextT >= _map.Duration)
            return (-1, 0, 0);

        var W = _map.Width;
        var H = _map.Height;
        var tPrev = (NextT - 1) * W * H;
        var tNext = NextT * W * H;
        var minY = int.MaxValue;
        var maxY = int.MinValue;
        for (int y = 0; y < H; ++y)
        {
            var moves = MovesFor(y);
            var rowOff = tPrev + y * W;
            for (int x = 0; x < W; ++x)
            {
                if (!_solutions[rowOff + x])
                    continue;
                foreach (var (dx, dy) in moves)
                {
                    var x1 = x + dx;
                    var y1 = y + dy;
                    if (x1 >= 0 && x1 < W && y1 >= 0 && y1 < H && !_map.Voxels[tNext + y1 * W + x1])
                    {
                        _solutions[tNext + y1 * W + x1] = true;
                        minY = Math.Min(minY, y1);
                        maxY = Math.Max(maxY, y1);
                    }
                }
                // jump takeoff: trajectory ignores ground blocking (bounded by MaxAirSteps)
                foreach (var (dx, dy) in _airMoves)
                {
                    var x1 = x + dx;
                    var y1 = y + dy;
                    if (x1 >= 0 && x1 < W && y1 >= 0 && y1 < H && !_air[1][tNext + y1 * W + x1])
                    {
                        _air[1][tNext + y1 * W + x1] = true;
                        ++_airCount[1];
                    }
                }
            }
        }

        for (int k = 1; k <= MaxAirSteps; ++k)
        {
            if (_airCount[k] == 0)
                continue;
            var airPrev = _air[k];
            for (int y = 0; y < H; ++y)
            {
                var rowOff = tPrev + y * W;
                for (int x = 0; x < W; ++x)
                {
                    if (!airPrev[rowOff + x])
                        continue;
                    if (k < MaxAirSteps)
                    {
                        foreach (var (dx, dy) in _airMoves)
                        {
                            var x1 = x + dx;
                            var y1 = y + dy;
                            if (x1 >= 0 && x1 < W && y1 >= 0 && y1 < H && !_air[k + 1][tNext + y1 * W + x1])
                            {
                                _air[k + 1][tNext + y1 * W + x1] = true;
                                ++_airCount[k + 1];
                            }
                        }
                    }
                    // landing: must hit walkable & unblocked ground
                    foreach (var (dx, dy) in LandMoves)
                    {
                        var x1 = x + dx;
                        var y1 = y + dy;
                        if (x1 >= 0 && x1 < W && y1 >= 0 && y1 < H && !_map.Voxels[tNext + y1 * W + x1])
                        {
                            _solutions[tNext + y1 * W + x1] = true;
                            minY = Math.Min(minY, y1);
                            maxY = Math.Max(maxY, y1);
                        }
                    }
                }
            }
        }
        Array.Clear(_airCount);
        for (int k = 1; k <= MaxAirSteps; ++k)
            _airCount[k] = CountBits(_air[k], tNext, tNext + W * H);

        return (NextT++, minY == int.MaxValue ? 0 : minY, maxY == int.MinValue ? 0 : maxY);
    }

    private static int CountBits(BitArray arr, int from, int to)
    {
        var count = 0;
        for (int i = from; i < to; ++i)
            if (arr[i])
                ++count;
        return count;
    }

    private int AirAt(int x, int y, int t)
    {
        if (!_map.InBounds(x, y, t))
            return 0;
        var off = t * _map.Width * _map.Height + y * _map.Width + x;
        for (int k = 1; k <= MaxAirSteps; ++k)
            if (_air[k][off])
                return k;
        return 0;
    }

    // find predecessor state (ground or air) of ground cell (x,y,t)
    private (int x, int y, int t, bool air)? StepGround(int x, int y, int time)
    {
        var t = time - 1;
        if (!_map.InBounds(x, y, t))
            return null;

        var tPrev = t * _map.Width * _map.Height;
        // landing from jump?
        foreach (var (dx, dy) in LandMoves)
        {
            var x1 = x - dx;
            var y1 = y - dy;
            if (x1 >= 0 && x1 < _map.Width && y1 >= 0 && y1 < _map.Height && AirAt(x1, y1, t) > 0)
                return (x1, y1, t, true);
        }
        // normal ground move
        foreach (var moves in AllMoveLists)
        {
            foreach (var (dx, dy) in moves)
            {
                var x1 = x + dx;
                var y1 = y + dy;
                if (x1 >= 0 && x1 < _map.Width && y1 >= 0 && y1 < _map.Height && _solutions[tPrev + y1 * _map.Width + x1])
                    return (x1, y1, t, false);
            }
        }
        return null;
    }

    // find predecessor state of air cell (x,y,t) at hop k
    private (int x, int y, int t, bool air)? StepAir(int x, int y, int time, int k)
    {
        var t = time - 1;
        if (!_map.InBounds(x, y, t))
            return null;

        var tPrev = t * _map.Width * _map.Height;
        foreach (var (dx, dy) in _airMoves)
        {
            var x1 = x - dx;
            var y1 = y - dy;
            if (x1 >= 0 && x1 < _map.Width && y1 >= 0 && y1 < _map.Height)
            {
                if (k > 1 && _air[k - 1][tPrev + y1 * _map.Width + x1])
                    return (x1, y1, t, true);
                if (k == 1 && _solutions[tPrev + y1 * _map.Width + x1])
                    return (x1, y1, t, false); // takeoff
            }
        }
        return null;
    }

    // backwards
    public IEnumerable<(int x, int y, int t, bool air)> FindSolution(int x, int y, int t)
    {
        yield return (x, y, t, false);
        while (true)
        {
            var k = AirAt(x, y, t);
            var step = k > 0 ? StepAir(x, y, t, k) : StepGround(x, y, t);
            if (step == null)
                yield break;
            (x, y, t, var air) = step.Value;
            yield return (x, y, t, air);
        }
    }

    public IEnumerable<(int x, int y, int t, bool air)> SolveUntilY(int goalY)
    {
        var t = DateTime.Now;
        while (ExecuteStep() is var res && res.t >= 0)
        {
            var row = _goalDir < 0 ? res.minY : res.maxY;
            if (_goalDir < 0 ? res.minY <= goalY : res.maxY >= goalY)
            {
                var firstX = Enumerable.Range(0, _map.Width).First(x => this[x, row, res.t]);
                Service.Log.Info($"Path rebuilt: took {(DateTime.Now - t).TotalMilliseconds:f3}ms");
                return FindSolution(firstX, row, res.t);
            }
        }
        return Enumerable.Empty<(int, int, int, bool)>();
    }
}
