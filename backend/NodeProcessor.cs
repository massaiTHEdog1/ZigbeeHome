using Microsoft.AspNetCore.SignalR;
using System.Drawing;
using ZigbeeHome.Classes;
using ZigbeeHome.DTO;
using ZigbeeHome.Enums;
using ZigbeeHome.Hubs;
using ZigBeeNet;
using ZigBeeNet.ZCL.Clusters.ColorControl;
using ZigBeeNet.ZCL.Clusters.LevelControl;
using ZigBeeNet.ZCL.Clusters.OnOff;

namespace ZigbeeHome
{
    public class NodeProcessor
    {
		private Thread? _thread;
		private readonly ZigBeeHomeManager _zigBeeHomeManager;

		public NodeProcessor(ZigBeeHomeManager zigBeeHomeManager)
        {
			_zigBeeHomeManager = zigBeeHomeManager;
		}

		public void Start(Drawflow drawflow, Node startingNode)
        {
			_thread = new Thread(async () =>
			{
				try
				{
					await ProcessNodeAsync(drawflow, startingNode);
				}
				catch (Exception ex)
                {

                }
			});
			_thread.Start();
		}

		private async Task ProcessNodeAsync(Drawflow drawflow, Node node)
		{
			if(node.name == NodeTypeEnum.ON_REPORT_ATTRIBUTES_COMMAND_RECEIVED)
            {
				if (node.outputs.Any())//This node only has 1 output
				{
					foreach (var connection in node.outputs.First().connections)
					{
						await ProcessNodeAsync(drawflow, connection.target);
					}
				}
			}
			else if(node.name == NodeTypeEnum.CONDITION)
            {
				var conditionVariable = node.data["variable"];
				var conditionValue = node.data["condition"];

				if (_zigBeeHomeManager.DrawflowVariables[conditionVariable] == conditionValue)
                {
					if (node.outputs.Count >= 1)
					{
						foreach (var connection in node.outputs[0].connections)
						{
							await ProcessNodeAsync(drawflow, connection.target);
						}
					}
				}
				else
                {
					if (node.outputs.Count >= 2)
					{
						foreach (var connection in node.outputs[1].connections)
						{
							await ProcessNodeAsync(drawflow, connection.target);
						}
					}
				}				
			}
			else if(node.name == NodeTypeEnum.ON_COMMAND)
            {
				var target = ulong.Parse(node.data["target"]);

				var targetNode = _zigBeeHomeManager.NetworkManager!.GetNode(new IeeeAddress(target));
				var endpoint = targetNode.GetEndpoints().First();
				ZigBeeEndpointAddress endpointAddress = endpoint.GetEndpointAddress();

				await _zigBeeHomeManager.NetworkManager.Send(endpointAddress, new OnCommand());

				if (node.outputs.Any())//This node only has 1 output
				{
					foreach (var connection in node.outputs.First().connections)
					{
						await ProcessNodeAsync(drawflow, connection.target);
					}
				}
			}
			else if(node.name == NodeTypeEnum.OFF_COMMAND)
            {
				var target = ulong.Parse(node.data["target"]);

				var targetNode = _zigBeeHomeManager.NetworkManager!.GetNode(new IeeeAddress(target));
				var endpoint = targetNode.GetEndpoints().First();
				ZigBeeEndpointAddress endpointAddress = endpoint.GetEndpointAddress();

				await _zigBeeHomeManager.NetworkManager.Send(endpointAddress, new OffCommand());

				if (node.outputs.Any())//This node only has 1 output
				{
					foreach (var connection in node.outputs.First().connections)
					{
						await ProcessNodeAsync(drawflow, connection.target);
					}
				}
			}
			else if (node.name == NodeTypeEnum.MOVE_TO_COLOR_COMMAND)
			{
				var target = ulong.Parse(node.data["target"]);
				var color = ColorTranslator.FromHtml(node.data["color"]);
				var xy = ZigBeeNet.Util.ColorConverter.RgbToCie(color.R, color.G, color.B);
				ushort transitionTime = 0;
				if (node.data.ContainsKey("transitiontime"))
					transitionTime = ushort.Parse(node.data["transitiontime"]);

				var targetNode = _zigBeeHomeManager.NetworkManager!.GetNode(new IeeeAddress(target));
				var endpoint = targetNode.GetEndpoints().First();
				ZigBeeEndpointAddress endpointAddress = endpoint.GetEndpointAddress();

				await _zigBeeHomeManager.NetworkManager.Send(endpointAddress, new MoveToColorCommand()
				{
					ColorX = xy.X,
					ColorY = xy.Y,
					TransitionTime = transitionTime
				});

				if (node.outputs.Any())//This node only has 1 output
				{
					foreach (var connection in node.outputs.First().connections)
					{
						await ProcessNodeAsync(drawflow, connection.target);
					}
				}
			}
			else if (node.name == NodeTypeEnum.MOVE_TO_LEVEL_COMMAND)
			{
				var target = ulong.Parse(node.data["target"]);
				byte level = 0;
				if (node.data.ContainsKey("level"))
					level = byte.Parse(node.data["level"]);
				ushort transitionTime = 0;
				if (node.data.ContainsKey("transitiontime"))
					transitionTime = ushort.Parse(node.data["transitiontime"]);

				var targetNode = _zigBeeHomeManager.NetworkManager!.GetNode(new IeeeAddress(target));
				var endpoint = targetNode.GetEndpoints().First();
				ZigBeeEndpointAddress endpointAddress = endpoint.GetEndpointAddress();

				await _zigBeeHomeManager.NetworkManager.Send(endpointAddress, new MoveToLevelCommand()
				{
					Level = level,
					TransitionTime = transitionTime
				});

				if (node.outputs.Any())//This node only has 1 output
				{
					foreach (var connection in node.outputs.First().connections)
					{
						await ProcessNodeAsync(drawflow, connection.target);
					}
				}
			}
			else if (node.name == NodeTypeEnum.SET_VARIABLE)
			{
				var conditionVariable = node.data["variable"];
				var conditionValue = node.data["value"];

				if(_zigBeeHomeManager.DrawflowVariables.ContainsKey(conditionVariable))
                {
					_zigBeeHomeManager.DrawflowVariables[conditionVariable] = conditionValue;
				}
				else
                {
					_zigBeeHomeManager.DrawflowVariables.Add(conditionVariable, conditionValue);
				}

				if (node.outputs.Any())//This node only has 1 output
				{
					foreach (var connection in node.outputs.First().connections)
					{
						await ProcessNodeAsync(drawflow, connection.target);
					}
				}
			}
			else if (node.name == NodeTypeEnum.WAIT_COMMAND)
			{
				var delay = 1000;
				if (node.data.ContainsKey("delay"))
					delay = int.Parse(node.data["delay"]);

				await Task.Delay(delay);

				if (node.outputs.Any())//This node only has 1 output
				{
					foreach (var connection in node.outputs.First().connections)
					{
						await ProcessNodeAsync(drawflow, connection.target);
					}
				}
			}
			else if (node.name == NodeTypeEnum.ON_STARTUP)
			{
				if (node.outputs.Any())//This node only has 1 output
				{
					foreach (var connection in node.outputs.First().connections)
					{
						await ProcessNodeAsync(drawflow, connection.target);
					}
				}
			}
			else
            {
				throw new Exception("This node is not implemented");
			}
		}
	}
}
