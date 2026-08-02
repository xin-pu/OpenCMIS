using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;

namespace OpenCMIS.App.Core.Services;

internal sealed class CmisMonitorReader(IRegisterAccess registers)
{
    public async Task<ModuleMonitors> ReadAsync(int laneCount)
    {
        var tempBytes = await registers.ReadBlockAsync(
            0x00,
            CmisConstants.RegTemperatureMSB,
            2);
        var vccBytes = await registers.ReadBlockAsync(
            0x00,
            CmisConstants.RegVccMSB,
            2);

        // Read temperature alarm/warning thresholds (upper threshold page)
        var tempHighAlarmBytes = await registers.ReadBlockAsync(
            CmisConstants.ThresholdPage, CmisConstants.RegTempHighAlarmMSB, 2);
        var tempLowAlarmBytes = await registers.ReadBlockAsync(
            CmisConstants.ThresholdPage, CmisConstants.RegTempLowAlarmMSB, 2);
        var tempHighWarnBytes = await registers.ReadBlockAsync(
            CmisConstants.ThresholdPage, CmisConstants.RegTempHighWarnMSB, 2);
        var tempLowWarnBytes = await registers.ReadBlockAsync(
            CmisConstants.ThresholdPage, CmisConstants.RegTempLowWarnMSB, 2);

        // Read VCC alarm/warning thresholds
        var vccHighAlarmBytes = await registers.ReadBlockAsync(
            CmisConstants.ThresholdPage, CmisConstants.RegVccHighAlarmMSB, 2);
        var vccLowAlarmBytes = await registers.ReadBlockAsync(
            CmisConstants.ThresholdPage, CmisConstants.RegVccLowAlarmMSB, 2);
        var vccHighWarnBytes = await registers.ReadBlockAsync(
            CmisConstants.ThresholdPage, CmisConstants.RegVccHighWarnMSB, 2);
        // VCC Low Warning threshold is not present in CMIS 5.2 lower page 0x00.
        // Passing lowWarnAvailable: false prevents zero-threshold misclassification.
        var vccLowWarnUnavailable = Array.Empty<byte>();

        var tempValue = ParseTemperature(tempBytes);
        var vccValue = ParseVcc(vccBytes);

        var result = new ModuleMonitors
        {
            Temperature = BuildMonitorValue(
                tempBytes, tempValue, "°C",
                tempHighAlarmBytes, ParseTemperature(tempHighAlarmBytes),
                tempLowAlarmBytes, ParseTemperature(tempLowAlarmBytes),
                tempHighWarnBytes, ParseTemperature(tempHighWarnBytes),
                tempLowWarnBytes, ParseTemperature(tempLowWarnBytes)),
            VCC = BuildMonitorValue(
                vccBytes, vccValue, "V",
                vccHighAlarmBytes, ParseVcc(vccHighAlarmBytes),
                vccLowAlarmBytes, ParseVcc(vccLowAlarmBytes),
                vccHighWarnBytes, ParseVcc(vccHighWarnBytes),
                vccLowWarnUnavailable, 0,
                lowWarnAvailable: false)
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
                new MonitorValue
                {
                    Value = ParseCurrent(bias),
                    Unit = "mA",
                    RawBytes = [bias[0], bias[1]]
                });
            result.TxPowerPerLane.Add(
                new MonitorValue
                {
                    Value = ParsePower(txPower),
                    Unit = "mW",
                    RawBytes = [txPower[0], txPower[1]]
                });
            result.RxPowerPerLane.Add(
                new MonitorValue
                {
                    Value = ParsePower(rxPower),
                    Unit = "mW",
                    RawBytes = [rxPower[0], rxPower[1]]
                });
        }

        result.ComputeTotals();
        return result;
    }

    private static MonitorValue BuildMonitorValue(
        byte[] rawBytes,
        double value,
        string unit,
        byte[] alarmHighRaw,
        double alarmHigh,
        byte[] alarmLowRaw,
        double alarmLow,
        byte[] warnHighRaw,
        double warnHigh,
        byte[] warnLowRaw,
        double warnLow,
        bool lowWarnAvailable = true)
    {
        var hasAlarm = value >= alarmHigh || value <= alarmLow;
        var hasWarning =
            !hasAlarm
            && (value >= warnHigh || (lowWarnAvailable && value <= warnLow));

        return new MonitorValue
        {
            Value = value,
            Unit = unit,
            RawBytes = [rawBytes[0], rawBytes[1]],
            HasAlarm = hasAlarm,
            HasWarning = hasWarning,
            AlarmHigh = alarmHigh,
            AlarmLow = alarmLow,
            WarnHigh = warnHigh,
            WarnLow = warnLow,
            LowWarnAvailable = lowWarnAvailable,
            RawAlarmHighBytes = [alarmHighRaw[0], alarmHighRaw[1]],
            RawAlarmLowBytes = [alarmLowRaw[0], alarmLowRaw[1]],
            RawWarnHighBytes = [warnHighRaw[0], warnHighRaw[1]],
            RawWarnLowBytes = lowWarnAvailable
                ? [warnLowRaw[0], warnLowRaw[1]]
                : []
        };
    }

    /// <summary>
    ///     Parses a CMIS 16-bit unsigned monitor value. CMIS registers are
    ///     big-endian: bytes[0] is the MSB (lower address), bytes[1] is the LSB.
    /// </summary>
    private static ushort ParseUInt16BigEndian(byte[] bytes)
    {
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }

    /// <summary>
    ///     Parses a CMIS 16-bit signed monitor value in big-endian byte order.
    /// </summary>
    private static short ParseInt16BigEndian(byte[] bytes)
    {
        return (short)((bytes[0] << 8) | bytes[1]);
    }

    internal static double ParseCurrent(byte[] bytes)
    {
        var raw = ParseUInt16BigEndian(bytes);
        return Math.Round(raw * 2e-3, 3);
    }

    internal static double ParsePower(byte[] bytes)
    {
        var raw = ParseUInt16BigEndian(bytes);
        return Math.Round(raw * 1e-4, 4);
    }

    internal static double ParseTemperature(byte[] bytes)
    {
        var raw = ParseInt16BigEndian(bytes);
        return Math.Round(raw / 256.0, 2);
    }

    internal static double ParseVcc(byte[] bytes)
    {
        var raw = ParseUInt16BigEndian(bytes);
        return Math.Round(raw * 100.0 / 1_000_000.0, 4);
    }
}
