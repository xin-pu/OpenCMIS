using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;

namespace OpenCMIS.App.Core.Services;

internal sealed class CmisMonitorReader(IRegisterAccess registers)
{
    public async Task<ModuleMonitors> ReadAsync(int laneCount)
    {
        var temperature = await registers.ReadBlockAsync(
            0x00,
            CmisConstants.RegTemperatureMSB,
            2);
        var vcc = await registers.ReadBlockAsync(
            0x00,
            CmisConstants.RegVccMSB,
            2);
        var result = new ModuleMonitors
        {
            Temperature = new MonitorValue
            {
                Value = ParseTemperature(temperature),
                Unit = "°C"
            },
            VCC = new MonitorValue
            {
                Value = ParseVcc(vcc),
                Unit = "V"
            }
        };

        for (var lane = 0;
             lane < laneCount && lane < CmisConstants.MaxLanes;
             lane++)
        {
            var page = (byte)(CmisConstants.FirstLanePage + lane);
            var bias = await registers.ReadBlockAsync(
                page,
                CmisConstants.RegLaneTxBiasMSB,
                2);
            var txPower = await registers.ReadBlockAsync(
                page,
                CmisConstants.RegLaneTxPowerMSB,
                2);
            var rxPower = await registers.ReadBlockAsync(
                page,
                CmisConstants.RegLaneRxPowerMSB,
                2);
            result.TxBiasPerLane.Add(
                new MonitorValue { Value = ParseCurrent(bias), Unit = "mA" });
            result.TxPowerPerLane.Add(
                new MonitorValue { Value = ParsePower(txPower), Unit = "mW" });
            result.RxPowerPerLane.Add(
                new MonitorValue { Value = ParsePower(rxPower), Unit = "mW" });
        }

        result.ComputeTotals();
        return result;
    }

    internal static double ParseCurrent(byte[] bytes)
    {
        var raw = (ushort)(bytes[0] | bytes[1] << 8);
        return Math.Round(raw * 2e-3, 3);
    }

    internal static double ParsePower(byte[] bytes)
    {
        var raw = (ushort)(bytes[0] | bytes[1] << 8);
        return Math.Round(raw * 1e-4, 4);
    }

    private static double ParseTemperature(byte[] bytes)
    {
        var raw = (short)(bytes[0] | bytes[1] << 8);
        return Math.Round(raw / 256.0, 2);
    }

    private static double ParseVcc(byte[] bytes)
    {
        var raw = (ushort)(bytes[0] | bytes[1] << 8);
        return Math.Round(raw * 100.0 / 1_000_000.0, 4);
    }
}
