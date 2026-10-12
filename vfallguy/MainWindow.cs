using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace vfallguy;

public class MainWindow : Window, IDisposable
{
    private GameEvents _gameEvents = new();
    private DebugDrawer _drawer = new();
    private AutoJoinLeave _automation = new();
    private Map? _map;
    private DateTime _now;
    private Vector3 _prevPos;
    private Vector3 _movementDirection;
    private float _movementSpeed;
    private bool _autoJoin;
    private bool _autoLeaveIfNotSolo;
    private bool _showAOEs;
    private bool _showAOEText;
    private bool _showPathfind;
    private bool _dynamicPathfind;
    private float _repathInterval = 0.5f;
    private bool _autoMove;
    private bool _captureEvents;
    private bool _terrainScan;
    private string? _lastTerrainExport;
    private readonly OverrideMovement _mover = new();
    private Vector3 _lastCapturePos;
    private bool _haveCapturePos;
    private uint _lastCaptureTerritory;
    private readonly HashSet<uint> _lastStatuses = new();
    private string? _statusLock;
    private string? _noAutoHint;
    // 无法移动类(1284) 死亡(1548) 止息(3782); add more as identified from capture logs
    private static readonly HashSet<uint> MoveBlockStatuses = [1284, 1548, 3782];
    private DateTime _autoJoinAt = DateTime.MaxValue;
    private DateTime _autoLeaveAt = DateTime.MaxValue;
    private int _numPlayersInDuty;
    private float _autoJoinDelay = 0.5f;
    private float _autoLeaveDelay = 3;
    private int _autoLeaveLimit = 1;

    public MainWindow() : base("vfailguy")
    {
        ShowCloseButton = false;
        RespectCloseHotkey = false;
    }

    public void Dispose()
    {
        _map?.Dispose();
        _gameEvents.Dispose();
        _automation.Dispose();
        _mover.Dispose();
    }

    public unsafe override void PreOpenCheck()
    {
        _automation.Update();
        _drawer.Update();

        _now = DateTime.Now;
        var playerPos = Service.ObjectTable.LocalPlayer?.Position ?? new();
        _movementDirection = playerPos - _prevPos;
        _prevPos = playerPos;
        _movementSpeed = _movementDirection.Length() / Framework.Instance()->FrameDeltaTime;
        _movementDirection = _movementDirection.NormalizedXZ();

        IsOpen = Service.ClientState.TerritoryType is 1165 or 1197;

        UpdateMap();
        UpdateAutoMove();
        UpdateCapture();
        if (_terrainScan && IsOpen && !Service.Condition[ConditionFlag.BetweenAreas])
        {
            try
            {
                TerrainScanner.Update(Service.ObjectTable.LocalPlayer?.Position ?? new());
            }
            catch (Exception e)
            {
                Service.Log.Error($"Terrain scanner failed (signature mismatch?): {e.Message}");
                _terrainScan = false;
                TerrainScanner.Reset();
            }
        }
        UpdateAutoJoin();
        UpdateAutoLeave();
        DrawOverlays();

        _drawer.DrawWorldPrimitives();
    }

    public unsafe override void Draw()
    {
        if (ImGui.Button("排队"))
            _automation.RegisterForDuty();
        ImGui.SameLine();
        if (ImGui.Button("离开"))
            _automation.LeaveDuty();
        ImGui.SameLine();
        ImGui.TextUnformatted($"副本内人数: {_numPlayersInDuty} (自动离开: {(_autoLeaveAt == DateTime.MaxValue ? "关闭" : $"{(_autoLeaveAt - _now).TotalSeconds:f1}秒后")})");

        ImGui.Checkbox("自动排队", ref _autoJoin);
        if (_autoJoin)
        {
            using (ImRaii.PushIndent())
            {
                ImGui.SliderFloat("延迟###j", ref _autoJoinDelay, 0, 10);
            }
        }
        ImGui.Checkbox("非单人自动离开", ref _autoLeaveIfNotSolo);
        if (_autoLeaveIfNotSolo)
        {
            using (ImRaii.PushIndent())
            {
                ImGui.SliderFloat("延迟###l", ref _autoLeaveDelay, 0, 10);
                ImGui.SliderInt("人数上限", ref _autoLeaveLimit, 1, 23);
            }
        }
        ImGui.Checkbox("显示 AOE 范围", ref _showAOEs);
        ImGui.Checkbox("显示 AOE 调试文本", ref _showAOEText);
        ImGui.Checkbox("显示规划路线", ref _showPathfind);
        ImGui.Checkbox("动态算路 (实验)", ref _dynamicPathfind);
        if (_dynamicPathfind)
        {
            using (ImRaii.PushIndent())
            {
                ImGui.SliderFloat("重算间隔###r", ref _repathInterval, 0.1f, 2.0f, "%.2f 秒");
            }
        }
        ImGui.Checkbox("自动移动 (实验)", ref _autoMove);
        ImGui.Checkbox("记录事件 (数据采集)", ref _captureEvents);
        ImGui.Checkbox("地形扫描 (实验)", ref _terrainScan);
        ImGui.SameLine();
        if (ImGui.Button("导出地形"))
            _lastTerrainExport = TerrainScanner.Export(Service.ClientState.TerritoryType);
        ImGui.SameLine();
        if (ImGui.Button("清空"))
            TerrainScanner.Reset();
        ImGui.TextUnformatted($"已扫描: {TerrainScanner.ScannedCount} 格{(_lastTerrainExport != null ? $"\n{_lastTerrainExport}" : "")}");

        if (_map != null)
        {
            var strats = _map.Strats();
            if (strats.Length > 0)
                ImGui.TextUnformatted(strats);
            ImGui.TextUnformatted($"地图: {_map.DisplayName}");
            ImGui.TextUnformatted($"坐标: {_map.PlayerPos}");
            ImGui.TextUnformatted($"路径: {_map.PathSkip}-{_map.Path.Count}");
            ImGui.TextUnformatted($"速度: {_movementSpeed}");
            if (_statusLock != null)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, 0xff00a5ff))
                    ImGui.TextUnformatted($"移动锁定: {_statusLock}");
            }
            if (_noAutoHint != null)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, 0xff00a5ff))
                    ImGui.TextUnformatted($"手动段: {_noAutoHint}");
            }
            var statuses = Service.ObjectTable.LocalPlayer?.StatusList.Where(s => s.StatusId != 0).Select(s => s.StatusId.ToString()).ToArray();
            if (statuses is { Length: > 0 })
                ImGui.TextUnformatted($"状态: {string.Join(",", statuses)}");

            //foreach (var aoe in _map.AOEs.Where(aoe => aoe.NextActivation != default))
            //{
            //    var nextActivation = (aoe.NextActivation - _now).TotalSeconds;
            //    using (ImRaii.PushColor(ImGuiCol.Text, nextActivation < 0 ? 0xff0000ff : 0xffffffff))
            //        ImGui.TextUnformatted($"{aoe.Type} R{aoe.R1} @ {aoe.Origin}: activate in {nextActivation:f3}, repeat={aoe.Repeat}, seqd={aoe.SeqDelay}");
            //}
        }
    }

    private void UpdateMap()
    {
        if (Service.Condition[ConditionFlag.BetweenAreas])
            return;

        Type? mapType = null;
        if (IsOpen)
        {
            if (Service.ClientState.TerritoryType == 1197)
            {
                //mapType = typeof(MapTest);
            }
            else
            {
                var pos = Service.ObjectTable.LocalPlayer!.Position;
                mapType = pos switch
                {
                    { X: >= -40 and <= 40, Z: <= -180 } => typeof(Map1),            // 1A: central corridor
                    { X: <= -150, Z: <= -100 and >= -300 } => typeof(Map1B),        // 1B: west climbing maze
                    { X: >= 150, Z: <= -90 and >= -155 } => typeof(Map2A),          // 2A: east ring bridge
                    { X: <= -150, Z: >= 100 and <= 350 } => typeof(Map2B),          // 2B: west platforms
                    { X: >= -40 and <= 40, Z: >= 100 and <= 350 } => typeof(Map3),  // 3: center arena
                    _ => null
                };
            }
        }

        if (_map?.GetType() != mapType)
        {
            _map?.Dispose();
            _map = null;
            if (mapType != null)
                _map = (Map?)Activator.CreateInstance(mapType, _gameEvents);
        }

        if (_map != null)
        {
            _map.DynamicPathfinding = _dynamicPathfind;
            _map.RepathInterval = _repathInterval;
        }
        _map?.Update();
    }

    private void UpdateAutoMove()
    {
        GameEvents.CaptureEvents = _captureEvents;
        _statusLock = null;
        _noAutoHint = null;
        if (!_autoMove || !IsOpen || _map == null || Service.Condition[ConditionFlag.BetweenAreas])
        {
            _mover.Enabled = false;
            return;
        }

        // control-impairing statuses (无法移动/死亡/止息...): release control entirely
        var player = Service.ObjectTable.LocalPlayer;
        if (player != null)
        {
            foreach (var s in player.StatusList)
            {
                if (MoveBlockStatuses.Contains(s.StatusId))
                {
                    _statusLock = $"状态 {s.StatusId}";
                    _mover.Enabled = false;
                    return;
                }
            }
        }

        var pos = _map.PlayerPos;
        // manual-play sections (ice/finger floors etc.)
        foreach (var (zMin, zMax, reason) in _map.NoAutoZones)
        {
            if (pos.Z >= zMin && pos.Z <= zMax)
            {
                _noAutoHint = reason;
                _mover.Enabled = false;
                return;
            }
        }

        while (_map.Path.Count > _map.PathSkip && (_map.Path[_map.PathSkip].Dest - pos).LengthXZ() < 1.0f)
            ++_map.PathSkip;
        if (_map.Path.Count <= _map.PathSkip)
        {
            _mover.Enabled = false;
            return;
        }

        var wp = _map.Path[_map.PathSkip];
        if (wp.Jump && (wp.Dest - pos).LengthXZ() < 0.8f)
            _mover.ExecuteJump();
        if (wp.StartMoveAt != default && _now < wp.StartMoveAt)
        {
            // full stop while waiting: hold position instead of drifting
            _mover.DesiredPosition = pos;
            _mover.Enabled = true;
            return;
        }
        _mover.DesiredPosition = wp.Dest;
        _mover.Enabled = true;
    }

    private void UpdateCapture()
    {
        if (!_captureEvents)
        {
            _haveCapturePos = false;
            return;
        }

        var territory = Service.ClientState.TerritoryType;
        if (territory != _lastCaptureTerritory)
        {
            _lastCaptureTerritory = territory;
            _haveCapturePos = false;
            Service.Log.Information($"[capture] TERRITORY {territory}");
        }

        if (territory != 1165)
            return;
        var p = Service.ObjectTable.LocalPlayer?.Position ?? new();
        if (!_haveCapturePos || (p - _lastCapturePos).LengthXZ() > 1.0f)
        {
            _haveCapturePos = true;
            _lastCapturePos = p;
            Service.Log.Information($"[capture] POS <{p.X:f2}, {p.Y:f2}, {p.Z:f2}>");
        }

        var player = Service.ObjectTable.LocalPlayer;
        var cur = player != null ? player.StatusList.Select(s => s.StatusId).Where(id => id != 0).ToHashSet() : [];
        foreach (var id in cur)
            if (_lastStatuses.Add(id))
            {
                var name = Service.LuminaRow<Lumina.Excel.Sheets.Status>(id)?.Name.ToString() ?? "?";
                Service.Log.Information($"[capture] BUFF +{id} {name}");
            }
        foreach (var id in _lastStatuses.Where(id => !cur.Contains(id)).ToList())
        {
            Service.Log.Information($"[capture] BUFF -{id}");
            _lastStatuses.Remove(id);
        }
    }

    private void UpdateAutoJoin()
    {
        bool wantAutoJoin = _autoJoin && _automation.Idle && IsOpen && Service.ClientState.TerritoryType == 1197 && !Service.Condition[ConditionFlag.WaitingForDutyFinder] && !Service.Condition[ConditionFlag.BetweenAreas];
        if (!wantAutoJoin)
        {
            _autoJoinAt = DateTime.MaxValue;
        }
        else if (_autoJoinAt == DateTime.MaxValue)
        {
            Service.Log.Debug($"Auto-joining in {_autoJoinDelay:f2}s...");
            _autoJoinAt = _now.AddSeconds(_autoJoinDelay);
        }
        else if (_now >= _autoJoinAt)
        {
            Service.Log.Debug($"Auto-joining");
            _automation.RegisterForDuty();
            _autoJoinAt = DateTime.MaxValue;
        }
    }

    private void UpdateAutoLeave()
    {
        _numPlayersInDuty = Service.ClientState.TerritoryType == 1165 && Service.Condition[ConditionFlag.BoundByDuty] && !Service.Condition[ConditionFlag.BetweenAreas]
            ? Service.ObjectTable.Count(o => o.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Pc)
            : 0;
        bool wantAutoLeave = _autoLeaveIfNotSolo && _numPlayersInDuty > _autoLeaveLimit && _automation.Idle;
        if (!wantAutoLeave)
        {
            _autoLeaveAt = DateTime.MaxValue;
        }
        else if (_autoLeaveAt == DateTime.MaxValue)
        {
            Service.Log.Debug($"Auto-leaving in {_autoLeaveDelay:f2}s...");
            _autoLeaveAt = _now.AddSeconds(_autoLeaveDelay);
        }
        else if (_now >= _autoLeaveAt)
        {
            Service.Log.Debug($"Auto-leaving: {_numPlayersInDuty} players");
            _automation.LeaveDuty();
            _autoLeaveAt = DateTime.MaxValue;
        }
    }

    private void DrawOverlays()
    {
        if (_map == null || Service.Condition[ConditionFlag.BetweenAreas])
            return;

        if (_showPathfind)
        {
            var from = _map.PlayerPos;
            for (int i = _map.PathSkip; i < _map.Path.Count; ++i)
            {
                var wp = _map.Path[i];
                var delay = (wp.StartMoveAt - _now).TotalSeconds;
                _drawer.DrawWorldLine(from, wp.Dest, i > 0 ? 0xff00ffff : delay <= 0 ? 0xff00ff00 : 0xff0000ff);
                if (delay > 0)
                    _drawer.DrawWorldText(from, 0xff0000ff, $"{delay:f3}");
                from = wp.Dest;
            }
        }

        foreach (var aoe in _map.AOEs.Where(aoe => aoe.NextActivation != default))
        {
            var nextActivation = (aoe.NextActivation - _now).TotalSeconds;
            if (nextActivation < 2.5f)
            {
                var (aoeEnter, aoeExit) = _movementSpeed > 0 ? aoe.Intersect(_map.PlayerPos, _movementDirection) : aoe.Contains(_map.PlayerPos) ? (0, float.PositiveInfinity) : (float.NaN, float.NaN);
                var delay = !float.IsNaN(aoeEnter) ? aoe.ActivatesBetween(_now, aoeEnter * Map.InvSpeed - 0.1f, aoeExit * Map.InvSpeed + 0.1f) : 0;
                var color = delay > 0 ? 0xff0000ff : 0xff00ffff;
                if (_showAOEs)
                {
                    aoe.Draw(_drawer, color);
                }
                if (_showAOEText)
                {
                    var text = $"{nextActivation:f3} [{aoeEnter * Map.InvSpeed:f2}-{aoeExit * Map.InvSpeed:f2}, {delay:f2}]";
                    var dir = (aoe.Origin - _map.PlayerPos).NormalizedXZ();
                    var (enter, exit) = aoe.Intersect(_map.PlayerPos, dir);
                    var textPos = _map.PlayerPos + dir * MathF.Max(enter, 0);
                    _drawer.DrawWorldText(textPos, color, text);
                }
            }
        }
    }
}
