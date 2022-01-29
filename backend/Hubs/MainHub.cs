using Microsoft.AspNetCore.SignalR;

namespace ZigbeeHome.Hubs
{
    public class MainHub : Hub
    {
        private readonly ZigBeeHomeManager _zigBeeHomeManager;

        public MainHub(ZigBeeHomeManager zigBeeHomeManager)
        {
            _zigBeeHomeManager = zigBeeHomeManager;
        }

        public async Task GetState()
        {
            await Clients.Caller.SendAsync("stateReceived", _zigBeeHomeManager.State);
        }

        public async Task Restart()
        {
            await _zigBeeHomeManager.InitAsync();
        }

        public async Task ReinitializeDrawflow()
        {
            await _zigBeeHomeManager.ReinitializeDrawflowAsync();
        }

        public async Task GetDevices()
        {
            await Clients.Caller.SendAsync("devicesReceived", _zigBeeHomeManager.Devices);
        }

        public async Task GetPermitJoin()
        {
            await Clients.Caller.SendAsync("permitJoinReceived", _zigBeeHomeManager.PermitJoin);
        }

        public async Task SetPermitJoin(bool permitJoin)
        {
            if(await _zigBeeHomeManager.SetPermitJoinAsync(permitJoin))
            {
                await Clients.All.SendAsync("permitJoinReceived", permitJoin);
            }
        }

        public async Task SetNodeLabel(ulong ieeeAddress, string label)
        {
            await _zigBeeHomeManager.SetNodeLabelAsync(ieeeAddress, label);
            await Clients.Caller.SendAsync("nodeLabelUpdated");
        }

        public async Task GetDrawflow()
        {
            await Clients.Caller.SendAsync("drawflowReceived", await _zigBeeHomeManager.GetDrawflowAsStringAsync());
        }

        public async Task SaveDrawflow(string json)
        {
            await _zigBeeHomeManager.SaveDrawflowAsync(json);
            await Clients.Others.SendAsync("drawflowReceived", json);
        }
    }
}
