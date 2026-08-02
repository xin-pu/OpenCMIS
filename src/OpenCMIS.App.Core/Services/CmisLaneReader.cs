using OpenCMIS.Protocol.Abstractions;
using OpenCMIS.Protocol.Abstractions.Models;
using OpenCMIS.Shared;

namespace OpenCMIS.App.Core.Services;

internal sealed class CmisLaneReader(IRegisterAccess registers)
{
    public async Task<List<LaneStatus>> ReadAsync(int laneCount)
    {
        var lanes = new List<LaneStatus>();
        for (var lane = 0;
             lane < laneCount && lane < CmisConstants.MaxLanes;
             lane++)
        {
            var page = (byte)(CmisConstants.FirstLanePage + lane);
            var flags = await registers.ReadByteAsync(
                page,
                CmisConstants.RegLaneStatusFlags);
            var txPower = await registers.ReadBlockAsync(
                page,
                CmisConstants.RegLaneTxPowerMSB,
                2);
            var rxPower = await registers.ReadBlockAsync(
                page,
                CmisConstants.RegLaneRxPowerMSB,
                2);
            var bias = await registers.ReadBlockAsync(
                page,
                CmisConstants.RegLaneTxBiasMSB,
                2);
            var status = new LaneStatus
            {
                LaneNumber = lane + 1,
                TxPower = CmisMonitorReader.ParsePower(txPower),
                RxPower = CmisMonitorReader.ParsePower(rxPower),
                TxBias = CmisMonitorReader.ParseCurrent(bias),
                RawFlags = flags,
                IsEnabled = (flags & 0x01) != 0,      // bit 0: DataPathEnabled
                HasFault = (flags & 0x02) != 0,        // bit 1: TX Fault
                TxLos = (flags & 0x04) != 0,           // bit 2: TX LOS
                TxLol = (flags & 0x08) != 0,           // bit 3: TX LOL
                RxLos = (flags & 0x10) != 0,           // bit 4: RX LOS
                RxLol = (flags & 0x20) != 0,           // bit 5: RX LOL
                ReservedBits = (byte)(flags & 0xC0)    // bits 6-7: reserved
            };
            status.StatusText = status.GetStateText();
            lanes.Add(status);
        }

        return lanes;
    }
}
