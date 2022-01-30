import { Device } from "@/classes/Device";
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { ManagerStateEnum } from "../classes/enums/ManagerStateEnum";
import { EventBus } from "@/main";

export default class ManagerService {
	state : ManagerStateEnum | null = null;
	connection : HubConnection | null = null;
	devices : Array<Device> = [];
	//devices : Array<Device> = [ new Device({ieeeAddress: 12, networkAddress: 1, label: "Button"}), new Device({ieeeAddress: 87, networkAddress: 2, label: "Light"})];

	constructor()
	{
		this.connection = new HubConnectionBuilder()
			.withUrl(process.env.VUE_APP_BASE_URL + "/hub/main")
			.build();

		this.connection.onclose(() => this.onConnectionClosed(this));

		this.connection.on("stateReceived", async (state: ManagerStateEnum) => {
			this.state = state;
		});

		this.connection.on("devicesReceived", (devices: Array<Device>) => {
			this.devices = devices.map(x => new Device({...x}));
			//this.devices = [ new Device({ieeeAddress: 12, networkAddress: 1, label: "Button"}), new Device({ieeeAddress: 87, networkAddress: 2, label: "Light"})];
		});

		this.connection.on("newDeviceReceived", (device: Device) => {
			this.devices.push(new Device({...device}));
			EventBus.$emit("addSuccessToast", `Device added : '${device.label}'`, false);
		});

		this.connection.on("deviceSynchronized", (ieeeAddress: number) => {
			const device = this.devices.filter(x => x.ieeeAddress === ieeeAddress)[0];
			device.isSynchronizing = false;
			EventBus.$emit("addSuccessToast", `Device synchronized : '${device.label}'`, false);
		});

		this.connection.on("deviceDeleted", () => {
			EventBus.$emit("addSuccessToast", `Device deleted.`);
		});
	}

	private async onConnectionClosed(self : ManagerService) : Promise<void>{
		self.state = null;
	}

	/** Connect to the server */
	public async connect() : Promise<void> {
		// new Promise(async (resolve, reject) => {
		// 	setTimeout(() => {
		// 		resolve('foo');
		// 	}, 300);
		// });

		try{
			await this.connection?.start();
			await this.connection?.send("GetState");
			await this.connection?.send("GetDevices");
		}
		catch(e)
		{
			EventBus.$emit("addAlertToast", `Can't connect to the server.`);
		}
	}

	public async startManager() : Promise<void> {
		this.connection?.send("Restart");
	}

	public async reinitializeDrawflow() : Promise<void> {
		this.connection?.send("ReinitializeDrawflow");
	}
}