using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;

namespace vfallguy;

public static class TerrainScanner
{
    public const float Radius = 70f;
    public const int BudgetPerFrame = 128;

    private static readonly Dictionary<(int, int), float?> _heights = new();
    private static readonly HashSet<(int, int)> _enqueued = new();
    private static readonly Queue<(int, int)> _pending = new();
    private static (int, int) _lastCenter = (int.MaxValue, int.MaxValue);

    public static int ScannedCount => _heights.Count;

    public static void Reset()
    {
        _heights.Clear();
        _enqueued.Clear();
        _pending.Clear();
        _lastCenter = (int.MaxValue, int.MaxValue);
    }

    // call every frame while active; scans a grid of ground heights around the player
    public static unsafe void Update(Vector3 pos)
    {
        var cx = (int)MathF.Round(pos.X);
        var cz = (int)MathF.Round(pos.Z);
        if ((cx, cz) != _lastCenter)
        {
            _lastCenter = (cx, cz);
            var r = (int)Radius;
            for (var dz = -r; dz <= r; ++dz)
            {
                for (var dx = -r; dx <= r; ++dx)
                {
                    if (dx * dx + dz * dz > r * r)
                        continue;
                    var p = (cx + dx, cz + dz);
                    if (!_heights.ContainsKey(p) && _enqueued.Add(p))
                        _pending.Enqueue(p);
                }
            }
        }

        var budget = BudgetPerFrame;
        while (budget-- > 0 && _pending.TryDequeue(out var p))
        {
            _enqueued.Remove(p);
            if (_heights.ContainsKey(p))
                continue;
            var origin = new Vector3(p.Item1, pos.Y + 40, p.Item2);
            // note: this can hit overhead geometry (arches etc.); raw hits are exported and filtered offline
            if (BGCollisionModule.RaycastMaterialFilter(origin, new Vector3(0, -1, 0), out var hit, 90f))
                _heights[p] = hit.Point.Y;
            else
                _heights[p] = null; // void
        }
    }

    public static string? Export(uint territoryType)
    {
        if (_heights.Count == 0)
            return null;
        var dir = Service.PluginInterface?.GetPluginConfigDirectory();
        if (dir == null)
            return null;
        var points = _heights.Select(kv => new[] { kv.Key.Item1, kv.Key.Item2, kv.Value }).ToArray();
        var file = Path.Combine(dir, $"terrain_{territoryType}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        File.WriteAllText(file, JsonSerializer.Serialize(new { territoryType, count = _heights.Count, points }));
        Service.Log.Information($"[terrain] exported {_heights.Count} points to {file}");
        return file;
    }
}
