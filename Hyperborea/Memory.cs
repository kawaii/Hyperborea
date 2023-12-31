using ECommons.ExcelServices;
using ECommons.EzHookManager;

namespace Hyperborea;
public unsafe class Memory
{
    internal delegate nint LoadZone(nint a1, uint a2, int a3, byte a4, byte a5, byte a6);
    internal EzHook<LoadZone> LoadZoneHook;

    internal delegate nint PacketDispatcher_OnReceivePacket(nint a1, uint a2, nint a3);
    internal EzHook<PacketDispatcher_OnReceivePacket> PacketDispatcher_OnReceivePacketHook;

    internal delegate byte PacketDispatcher_OnSendPacket(nint a1, nint a2, nint a3, byte a4);
    internal EzHook<PacketDispatcher_OnSendPacket> PacketDispatcher_OnSendPacketHook;

    internal delegate nint TargetSystem_InteractWithObject(nint a1, nint a2, byte a3);
    internal EzHook<TargetSystem_InteractWithObject> TargetSystem_InteractWithObjectHook;

    byte* TrueWeather = (byte*) (*(IntPtr*) Svc.SigScanner.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 48 83 C1 10 48 89 74 24") + 0x26);

    internal delegate nint SetupTerritoryTypeDelegate(void* EventFramework, ushort territoryType);
    internal SetupTerritoryTypeDelegate SetupTerritoryType = EzDelegate.Get<SetupTerritoryTypeDelegate>("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 66 89 91");


    public Memory()
    {
        LoadZoneHook = new("40 55 56 57 41 56 41 57 48 83 EC 50 48 8B F9", LoadZoneDetour);
        PacketDispatcher_OnReceivePacketHook = new("40 53 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 8B F2", PacketDispatcher_OnReceivePacketDetour, false);
        PacketDispatcher_OnSendPacketHook = new("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 70 8B 81 ?? ?? ?? ??", PacketDispatcher_OnSendPacketDetour, false);
        TargetSystem_InteractWithObjectHook = new("48 89 5C 24 ?? 48 89 6C 24 ?? 56 48 83 EC 20 48 8B E9 41 0F B6 F0", TargetSystem_InteractWithObjectDetour, false);
    }

    private nint TargetSystem_InteractWithObjectDetour(nint a1, nint a2, byte a3)
    {
        return 0;
    }

    public void EnableFirewall()
    {
        PacketDispatcher_OnReceivePacketHook.Enable();
        PacketDispatcher_OnSendPacketHook.Enable();
    }

    public void DisableFirewall()
    {
        PacketDispatcher_OnReceivePacketHook.Disable();
        PacketDispatcher_OnSendPacketHook.Disable();
    }

    public bool IsFirewallEnabled => PacketDispatcher_OnSendPacketHook.IsEnabled;

    private byte PacketDispatcher_OnSendPacketDetour(nint a1, nint a2, nint a3, byte a4)
    {
        try
        {
            var opcode = *(ushort*)a2;
            if(opcode == 566)
            {
                PluginLog.Verbose($"[HyperFirewall] Passing outgoing packet with opcode {opcode} through");
                return PacketDispatcher_OnSendPacketHook.Original(a1, a2, a3, a4);
            }
            else
            {
                PluginLog.Verbose($"[HyperFirewall] Suppressing outgoing packet with opcode {opcode}");
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
        return 1;
    }

    private nint PacketDispatcher_OnReceivePacketDetour(nint a1, uint a2, nint a3)
    {
        try
        {
            var opcode = *(ushort*)(a3 + 2);
            if (opcode == 388)
            {
                PluginLog.Verbose($"[HyperFirewall] Passing incoming packet with opcode {opcode} through");
                return PacketDispatcher_OnReceivePacketHook.Original(a1, a2, a3);
            }
            else if (opcode == 226)
            {
                PluginLog.Verbose($"[HyperFirewall] Passing incoming packet with opcode {opcode} through");
                return PacketDispatcher_OnReceivePacketHook.Original(a1, a2, a3);
            }
            else
            {
                PluginLog.Verbose($"[HyperFirewall] Suppressing incoming packet with opcode {opcode}");
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
        return 0;
    }

    internal nint LoadZoneDetour(nint a1, uint a2, int a3, byte a4, byte a5, byte a6)
    {
        try
        {
            PluginLog.Debug($"Loading {ExcelTerritoryHelper.GetName(a2, true)}, {a3}, {a4}, {a5}, {a6}");
        }
        catch(Exception e)
        {
            e.Log();
        }
        return LoadZoneHook.Original(a1, a2, a3, a4, a5, a6);
    }
}
