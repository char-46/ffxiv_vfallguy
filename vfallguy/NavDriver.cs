using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Plugin.Ipc;

namespace vfallguy;

public class NavDriver : IDisposable
{
    private ICallGateSubscriber<bool>? _isReady;
    private ICallGateSubscriber<Vector3, Vector3, bool, List<Vector3>>? _pathfind;
    private ICallGateSubscriber<List<Vector3>, bool>? _moveTo;
    private ICallGateSubscriber<List<Vector3>, bool, bool>? _moveTo2;
    private ICallGateSubscriber<bool>? _stop;
    private bool _resolved;
    private bool _moving;
    private Vector3 _target;
    private DateTime _lastCalc = DateTime.MinValue;
    private string? _lastWarn;

    public void Dispose() => Stop();

    private bool Resolve()
    {
        if (_resolved)
            return _isReady != null;
        _resolved = true;
        var pi = Service.PluginInterface;
        if (pi == null)
            return false;
        try
        {
            _isReady = pi.GetIpcSubscriber<bool>("Nav.IsReady");
            _pathfind = pi.GetIpcSubscriber<Vector3, Vector3, bool, List<Vector3>>("Nav.Pathfind");
            _moveTo = pi.GetIpcSubscriber<List<Vector3>, bool>("Nav.MoveTo");
            _stop = pi.GetIpcSubscriber<bool>("Nav.Stop");
        }
        catch (Exception e)
        {
            WarnOnce($"IPC init failed: {e.Message}");
            _isReady = null;
        }
        return _isReady != null;
    }

    public bool IsReady()
    {
        if (!Resolve())
            return false;
        try
        {
            return _isReady!.InvokeFunc();
        }
        catch (Exception e)
        {
            WarnOnce($"Nav.IsReady: {e.Message}");
            return false;
        }
    }

    public void Navigate(Vector3 from, Vector3 to)
    {
        if (!Resolve())
            return;
        if (_moving && (_target - to).LengthSquared() < 0.01f && (DateTime.Now - _lastCalc).TotalSeconds < 2.5f)
            return;
        if (!IsReady())
        {
            WarnOnce("vnavmesh not ready");
            _moving = false;
            return;
        }

        List<Vector3>? path;
        try
        {
            path = _pathfind!.InvokeFunc(from, to, false);
        }
        catch (Exception e)
        {
            WarnOnce($"Nav.Pathfind failed: {e.Message}");
            _moving = false;
            return;
        }
        if (path == null || path.Count == 0)
        {
            WarnOnce($"no path {from}->{to}");
            _moving = false;
            return;
        }

        bool ok;
        try
        {
            ok = _moveTo!.InvokeFunc(path);
        }
        catch (Exception)
        {
            ok = false;
            try
            {
                _moveTo2 ??= Service.PluginInterface!.GetIpcSubscriber<List<Vector3>, bool, bool>("Nav.MoveTo");
                ok = _moveTo2.InvokeFunc(path, false);
            }
            catch (Exception e)
            {
                WarnOnce($"Nav.MoveTo failed: {e.Message}");
                _moving = false;
                return;
            }
        }
        if (!ok)
        {
            WarnOnce("Nav.MoveTo returned false");
            _moving = false;
            return;
        }
        _target = to;
        _lastCalc = DateTime.Now;
        _moving = true;
    }

    public void Stop()
    {
        if (!_moving || !Resolve())
            return;
        try
        {
            _stop!.InvokeFunc();
        }
        catch (Exception e)
        {
            WarnOnce($"Nav.Stop failed: {e.Message}");
        }
        _moving = false;
    }

    private void WarnOnce(string msg)
    {
        if (msg == _lastWarn)
            return;
        _lastWarn = msg;
        Service.Log.Warning($"[vnav] {msg}");
    }
}
