using ZigBeeNet;
using ZigBeeNet.Database;

namespace ZigbeeHome.DTO
{
    public class DeviceDto
    {
        public string? Label { get; set; }
        public ushort NetworkAddress { get; set; }
        public ulong IeeeAddress { get; set; }
        public bool IsSynchronizing { get; set; }

        public DeviceDto(ZigBeeNode node, string label)
        {
            Label = label;
            NetworkAddress = node.NetworkAddress;
            IeeeAddress = node.IeeeAddress.Value;
        }

        public DeviceDto(ZigBeeNodeDao node, string label)
        {
            Label = label;
            NetworkAddress = node.NetworkAddress;
            IeeeAddress = node.IeeeAddress.Value;
        }
    }
}
