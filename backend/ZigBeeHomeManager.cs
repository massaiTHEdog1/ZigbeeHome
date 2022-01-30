using Microsoft.AspNetCore.SignalR;
using ZigbeeHome.DTO;
using ZigbeeHome.Enums;
using ZigbeeHome.Hubs;
using ZigBeeNet;
using ZigBeeNet.DataStore.Json;
using ZigBeeNet.Hardware.TI.CC2531;
using ZigBeeNet.Tranport.SerialPort;
using ZigBeeNet.Transaction;
using ZigBeeNet.Transport;
using ZigBeeNet.ZCL.Clusters.General;
using ZigBeeNet.ZCL.Field;
using ZigBeeNet.ZCL.Protocol;
using System.Linq;
using ZigBeeNet.App.Discovery;
using ZigBeeNet.Database;
using Newtonsoft.Json;
using ZigbeeHome.Classes;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging.Console;
using ZigBeeNet.Util;
using ZigBeeNet.ZDO.Command;
using System.Collections.Concurrent;

namespace ZigbeeHome
{
    public class ZigBeeHomeManager
    {
		private bool _enableLog = true;
        public ZigBeeNetworkManager? NetworkManager;
		public IHubContext<MainHub> HubContext;
        private ZigBeeNode? _coordinator;
		public ZigBeeDiscoveryExtension DiscoveryExtension;
        private JsonNetworkDataStore _dataStore;
        public ConcurrentDictionary<ulong, DeviceDto> Devices = new ConcurrentDictionary<ulong, DeviceDto>();

		/// <summary>
		/// Dictionnary containing the variables of the drawflow.<br/>
		/// Key : Variable name<br/>
		/// Value : Variable value
		/// </summary>
		public Dictionary<string, string> DrawflowVariables { get; set; } = new Dictionary<string, string>();

		public ZigBeeHomeManager(IHubContext<MainHub> hubContext)
        {
			HubContext = hubContext;

			if(_enableLog)
            {
				ILoggerFactory _factory = LoggerFactory.Create(builder =>
				{
					builder
						.SetMinimumLevel(LogLevel.Debug)
						.AddDebug()
						.AddSimpleConsole(c =>
						{
							c.ColorBehavior = LoggerColorBehavior.Enabled;
							c.SingleLine = true;
							c.TimestampFormat = "[HH:mm:ss] ";
						});
				});
				LogManager.SetFactory(_factory);
            }

			Task.Run(async () => await InitAsync());
		}

        public ManagerStateEnum State { get; set; } = ManagerStateEnum.STOPPED;
		public bool PermitJoin { get; set; } = false;

		/// <summary>
		/// Delete a device.
		/// </summary>
        public async Task<bool> DeleteDeviceAsync(ulong ieeeAddress)
        {
			if (State == ManagerStateEnum.INITIALIZING)
				return false;

			if (State == ManagerStateEnum.RUNNING)
			{
				NetworkManager!.RemoveNode(NetworkManager.GetNode(new IeeeAddress(ieeeAddress)));
			}
			else if(State == ManagerStateEnum.STOPPED)
            {
				await RemoveNodeAsync(ieeeAddress);
			}

			return true;
        }

		public IEnumerable<DeviceDto> GetDevices()
		{
			return Devices.Select(x => x.Value);
		}

		/// <summary>
		/// Init the manager
		/// </summary>
		public async Task InitAsync()
        {
			if (State != ManagerStateEnum.STOPPED)
				return;

			try
			{
				await SetManagerStateAsync(ManagerStateEnum.INITIALIZING);

				if (_dataStore == null)
				{
					_dataStore = new JsonNetworkDataStore("data\\devices");

					Devices = new ConcurrentDictionary<ulong, DeviceDto>();
					var labels = GetNodesLabels();
					foreach (var node in _dataStore.ReadNetworkNodes().Select(x => _dataStore.ReadNode(x)).Where(x => x.NetworkAddress != 0))
					{
						var deviceLabel = labels.GetValueOrDefault(node.IeeeAddress.Value.ToString()) ?? $"Device {node.IeeeAddress}";
						Devices.TryAdd(node.IeeeAddress.Value, new DeviceDto(node, deviceLabel));
					}
				}

				var zigbeePort = new ZigBeeSerialPort("COM3");
				var dongle = new ZigBeeDongleTiCc2531(zigbeePort);

				NetworkManager = new ZigBeeNetworkManager(dongle);

				NetworkManager.SetNetworkDataStore(_dataStore);				

				NetworkManager.AddNetworkNodeListener(new NodeListener(HubContext, this));

				NetworkManager.AddCommandListener(new ZigBeeTransaction(NetworkManager));
				NetworkManager.AddCommandListener(new CommandListener(this));

				DiscoveryExtension = new ZigBeeDiscoveryExtension();
				DiscoveryExtension.SetUpdatePeriod(0);
				DiscoveryExtension.ExtensionInitialize(NetworkManager);

				//var discoveryExtension = new ZigBeeDiscoveryExtension();
				//discoveryExtension.SetUpdatePeriod(60);
				//NetworkManager.AddExtension(discoveryExtension);

				// Initialise the network
				NetworkManager.Initialize();

				var startupSucceded = NetworkManager.Startup(false);

				if (startupSucceded == ZigBeeStatus.SUCCESS)
				{
					_coordinator = NetworkManager.GetNode(0);

					await SetManagerStateAsync(ManagerStateEnum.RUNNING);

					await ReinitializeDrawflowAsync();
				}
				else
                {
					await SetManagerStateAsync(ManagerStateEnum.STOPPED);
				}
			}
			catch
            {
				await SetManagerStateAsync(ManagerStateEnum.STOPPED);
            }
		}

		/// <summary>
		/// Erase all the variable and trigger startup nodes
		/// </summary>
		/// <returns></returns>
        public async Task ReinitializeDrawflowAsync()
        {
			DrawflowVariables = new Dictionary<string, string>();

			var drawflow = GetDrawflowAsObject(await GetDrawflowAsStringAsync());

			if (drawflow == null)
				return;

			var startupNodes = drawflow.GetNodes().Where(x => x.name == NodeTypeEnum.ON_STARTUP);

			foreach (var node in startupNodes)
			{
				var nodeProcessor = new NodeProcessor(this);
				nodeProcessor.Start(drawflow, node);
			}
		}

        public async Task SaveDrawflowAsync(string json)
        {
			File.WriteAllText("data\\drawflow.json", json);
		}

        public async Task<string> GetDrawflowAsStringAsync()
        {
			if (File.Exists("data\\drawflow.json"))
				return File.ReadAllText("data\\drawflow.json");

			return null;
		}

        public async Task SetNodeLabelAsync(ulong ieeeAddress, string label)
        {
			if (string.IsNullOrWhiteSpace(label))
				return;

			var labels = GetNodesLabels();
			labels[ieeeAddress.ToString()] = label;
			SetNodesLabels(labels);
		}

        /// <summary>
        /// Set permit join
        /// </summary>
        public async Task<bool> SetPermitJoinAsync(bool permitJoin)
        {
			if (State != ManagerStateEnum.RUNNING || PermitJoin == permitJoin || Devices.Any(x => x.Value.IsSynchronizing))
				return false;

			if (permitJoin)
			{
				DiscoveryExtension.ExtensionStartup();
				_coordinator?.PermitJoin(true);
			}
			else
			{
				_coordinator?.PermitJoin(false);
				DiscoveryExtension.ExtensionShutdown();
			}

			PermitJoin = permitJoin;
			return true;
		}

        /// <summary>
        /// Set the state and notifies clients
        /// </summary>
        private async Task SetManagerStateAsync(ManagerStateEnum state)
        {
			State = state;

			await HubContext.Clients.All.SendAsync("stateReceived", state);
		}

		/// <summary>
		/// Get the dictionnary containing labels of nodes
		/// </summary>
		public Dictionary<string, string> GetNodesLabels()
        {
			if (File.Exists("data\\labels.json"))
				return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("data\\labels.json"));

			return new Dictionary<string, string>();
		}

		/// <summary>
		/// Set the dictionnary containing labels of nodes
		/// </summary>
		public void SetNodesLabels(Dictionary<string, string> dictionnary)
        {
			string json = JsonConvert.SerializeObject(dictionnary, Formatting.Indented);

			File.WriteAllText("data\\labels.json", json);

		}

		/// <summary>
		/// Get a node by address
		/// </summary>
		public ZigBeeNode? GetNode(ushort address)
        {
			return NetworkManager?.GetNode(address);
        }

		/// <summary>
		/// Parse the json and return a drawflow object
		/// </summary>
		public Drawflow GetDrawflowAsObject(string json)
        {
			if (string.IsNullOrWhiteSpace(json))
				return null;

			var root = JObject.Parse(json);

			var drawflow = new Drawflow()
			{
				modules = new List<Module>(),
			};

			Dictionary<int, Node> nodes = new Dictionary<int, Node>();

			foreach (JProperty moduleProperty in root["drawflow"])//modules
			{
				var module = new Module()
				{
					name = moduleProperty.Name,
					nodes = new List<Node>()
				};

				foreach (JProperty nodeProperty in moduleProperty.First["data"])
				{
					var node = nodeProperty.First.ToObject<Node>();
					node.inputs = new List<Input>();
					node.outputs = new List<Output>();

					nodes.Add(node.id, node);//adding this node to the list

					foreach (JProperty inputProperty in nodeProperty.First["inputs"])
					{
						var input = inputProperty.First.ToObject<Input>();
						input.name = inputProperty.Name;
						node.inputs.Add(input);
					}

					foreach (JProperty outputProperty in nodeProperty.First["outputs"])
					{
						var output = outputProperty.First.ToObject<Output>();
						output.name = outputProperty.Name;
						node.outputs.Add(output);
					}

					module.nodes.Add(node);
				}

				drawflow.modules.Add(module);
			}

			

			foreach(var module in drawflow.modules)
            {
				foreach(var node in module.nodes)
                {
					foreach (var input in node.inputs)
					{
						foreach(var connection in input.connections)
                        {
							connection.target = nodes[connection.node];
                        }
					}

					foreach (var output in node.outputs)
                    {
						foreach (var connection in output.connections)
						{
							connection.target = nodes[connection.node];
						}
					}
                }
            }
			//TODO : Repasser sur chaque input/output de node pour lier à la node correspondante

			return drawflow;
		}

        public void SetDrawflowVariable(string key, string value)
        {
			if (DrawflowVariables.ContainsKey(key))
				DrawflowVariables[key] = value;
			else
				DrawflowVariables.Add(key, value);
		}

		/// <summary>
		/// Remove a node from the Devices list and from the datastore.
		/// </summary>
        public async Task RemoveNodeAsync(ulong ieeeAddress)
        {
			DeviceDto deletedDto = null;
			Devices.TryRemove(ieeeAddress, out deletedDto);
			_dataStore.RemoveNode(new IeeeAddress(ieeeAddress));
			await HubContext.Clients.All.SendAsync("devicesReceived", GetDevices());
		}
    }

	public class NodeListener : IZigBeeNetworkNodeListener
	{
		private readonly IHubContext<MainHub> _hubContext;
		private readonly ZigBeeHomeManager _zigBeeHomeManager;
		

		public NodeListener(IHubContext<MainHub> hubContext, ZigBeeHomeManager zigBeeHomeManager)
        {
			_hubContext = hubContext;
			_zigBeeHomeManager = zigBeeHomeManager;
		}

		public async void NodeAdded(ZigBeeNode node)
		{
			if(node.NetworkAddress != 0 && !_zigBeeHomeManager.GetDevices().Any(x => x.IeeeAddress == node.IeeeAddress.Value))
            {
				var nodeLabel = "Device " + node.IeeeAddress.Value.ToString();
				var labels = _zigBeeHomeManager.GetNodesLabels();

				if (!labels.ContainsKey(node.IeeeAddress.Value.ToString()))
				{
					labels.Add(node.IeeeAddress.Value.ToString(), nodeLabel);
					_zigBeeHomeManager.SetNodesLabels(labels);
				}
				else
				{
					nodeLabel = labels[node.IeeeAddress.Value.ToString()];
				}

				var deviceDto = new DeviceDto(node, nodeLabel);
				_zigBeeHomeManager.Devices.TryAdd(deviceDto.IeeeAddress, deviceDto);
				deviceDto.IsSynchronizing = true;
				await _hubContext.Clients.All.SendAsync("newDeviceReceived", deviceDto);

				//var discoverer = new ZigBeeNodeServiceDiscoverer(_zigBeeHomeManager.NetworkManager, _zigBeeHomeManager.NetworkManager!.GetNode(node.NetworkAddress));
				//await discoverer.StartDiscovery();

				await Task.Run(async () =>
				{
					while(true)
                    {
						var discoverers = _zigBeeHomeManager.DiscoveryExtension.GetNodeDiscoverers();
						var discoverer = discoverers.FirstOrDefault(x => x.Node.IeeeAddress == node.IeeeAddress);

						if (discoverer?.IsFinished == true)
							break;

						await Task.Delay(500);
					}
				});

				deviceDto.IsSynchronizing = false;
				await _hubContext.Clients.All.SendAsync("deviceSynchronized", node.IeeeAddress.Value);
			}
		}

		public async void NodeRemoved(ZigBeeNode node)
		{
			await _zigBeeHomeManager.RemoveNodeAsync(node.IeeeAddress.Value);
		}

		public async void NodeUpdated(ZigBeeNode node)
		{
			await _hubContext.Clients.All.SendAsync("devicesReceived", _zigBeeHomeManager.GetDevices());
		}
	}

	public class CommandListener : IZigBeeCommandListener
	{
        private ZigBeeHomeManager _zigBeeManager;

        public CommandListener(ZigBeeHomeManager zigBeeManager)
        {
			_zigBeeManager = zigBeeManager;
        }

		public async void CommandReceived(ZigBeeCommand command)
		{
			var clusterType = ZclClusterType.GetValueById(command.ClusterId);

			if (clusterType != null)
			{
				if (command is ReportAttributesCommand)
				{
					//Console.WriteLine("ReportAttributesCommand received");
					//Console.WriteLine("Cluster : " + clusterType.Label);

					var reportCommand = (ReportAttributesCommand)command;

					var node = _zigBeeManager.GetNode(command.SourceAddress.Address);
					//var endpoint = node?.GetEndpoints()?.FirstOrDefault();

					//var cluster = clusterType.ClusterFactory(endpoint);

					//foreach (var element in reportCommand.Reports ?? new List<AttributeReport>())
					//{
					//	Console.WriteLine("Attribute : " + cluster.GetAttribute(element.AttributeIdentifier)?.Name);
					//	Console.WriteLine("Value : " + element.AttributeValue);
					//}

					var drawflow = _zigBeeManager.GetDrawflowAsObject(await _zigBeeManager.GetDrawflowAsStringAsync());

					var nodes = drawflow.GetNodes();
					var reportAttributesNodes = nodes.Where(x => x.name == NodeTypeEnum.ON_REPORT_ATTRIBUTES_COMMAND_RECEIVED && x.data["source"] == node.IeeeAddress.Value.ToString());
					var report = reportCommand.Reports?.FirstOrDefault();
					var reportValue = report.AttributeValue;

					if (reportAttributesNodes.Any())
					{ 
						foreach (var reportAttributesNode in reportAttributesNodes)
                        {
							_zigBeeManager.SetDrawflowVariable(reportAttributesNode.data["variable"], reportValue.ToString());

							var nodeProcessor = new NodeProcessor(_zigBeeManager);
							nodeProcessor.Start(drawflow, reportAttributesNode);							
						}
                    }
				}
				else
				{

				}
			}

			//Console.WriteLine("Command received " + command);
		}
	}
}
