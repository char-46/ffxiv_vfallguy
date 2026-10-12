using System;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace vfallguy;

public unsafe class OverrideMovement : IDisposable
{
    public bool HookValid { get; private set; }

    private bool _enabled;
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (!HookValid || value == _enabled)
                return;
            if (value)
                _hook!.Enable();
            else
                _hook!.Disable();
            _enabled = value;
        }
    }

    public bool IgnoreUserInput;
    public Vector3 DesiredPosition;
    public float Precision = 0.4f;

    private delegate void RMIWalkDelegate(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk);
    private Hook<RMIWalkDelegate>? _hook;

    public OverrideMovement()
    {
        try
        {
            _hook = Service.Hook.HookFromSignature<RMIWalkDelegate>("E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D", RMIWalkDetour);
            HookValid = true;
            Service.Log.Information($"RMIWalk hook: 0x{_hook.Address:X}");
        }
        catch (Exception e)
        {
            Service.Log.Error($"RMIWalk hook 失败, 自动移动不可用: {e.Message}");
        }
    }

    public void Dispose()
    {
        if (_hook == null)
            return;
        _hook.Disable();
        _hook.Dispose();
    }

    private void RMIWalkDetour(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte bAdditiveUnk)
    {
        _hook!.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, bAdditiveUnk);
        if (bAdditiveUnk == 0 && (IgnoreUserInput || *sumLeft == 0 && *sumForward == 0) && DirectionToDestination() is var relDir && relDir != null)
        {
            *sumLeft = MathF.Sin(relDir.Value);
            *sumForward = MathF.Cos(relDir.Value);
        }
    }

    private float? DirectionToDestination()
    {
        var player = Service.ObjectTable.LocalPlayer;
        if (player == null)
            return null;

        var dist = DesiredPosition - player.Position;
        if (dist.LengthSquared() <= Precision * Precision)
            return null;

        var camera = CameraManager.Instance()->GetActiveCamera();
        if (camera == null)
            return null;

        var dir = MathF.Atan2(dist.X, dist.Z);
        var cameraDir = camera->DirH + MathF.PI;
        return dir - cameraDir;
    }
}
