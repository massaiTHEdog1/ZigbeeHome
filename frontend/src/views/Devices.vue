<template>
	<b-container class="flex-grow-1 pt-4">
		<p class="mb-1 d-inline-block mr-1">Permit join : <span :class="permitJoin ? 'text-success' : 'text-danger'">{{ permitJoin ? "enabled - scanning" : "disabled" }}</span></p><b-spinner v-if="permitJoin" small variant="success" type="grow"></b-spinner>
		<b-button class="d-block" size="sm" @click="togglePermitJoin()" :variant="permitJoin ? 'danger' : 'primary'">{{ permitJoin ? "Disable" : "Enable" }}</b-button>

		<h4 class="mt-3">List of devices</h4>

		<b-container>
			<b-row cols="3">
				<b-card v-for="(device, index) in Manager.devices" :key="device.ieeeAddress">
					<!-- <font-awesome-icon icon="edit" size="sm" /> -->
					<div v-if="device.isSynchronizing">
						<p class="mb-1 d-inline-block mr-1 text-info">Synchronizing </p><b-spinner small variant="info" type="grow"></b-spinner>
					</div>
					<p role="button" class="text-truncate mb-3" :title="device.label" v-show="!device.editing" @click="edit(device, index)">{{device.label}}</p>
					<input ref="input" type="text" class="w-100 mb-3" v-model="device.editText" v-show="device.editing" @blur="blur(device)" @keyup.enter="blur(device)"/>
					<p class="mb-0">Network address : {{device.networkAddress}}</p>
					<p class="mb-0">Ieee address : {{device.ieeeAddress}}</p>
				</b-card>
			</b-row>
		</b-container>
	</b-container>
</template>

<script lang="ts">
import { Component, Ref, Vue } from "vue-property-decorator";
import { HubConnection, HubConnectionBuilder, HubConnectionState } from "@microsoft/signalr";
import { ManagerStateEnum } from '@/classes/enums/ManagerStateEnum'
import { EventBus, Manager } from "@/main";
import { Device } from "@/classes/Device";

@Component({
	components: {
	},
})
export default class Devices extends Vue {

	Manager = Manager;

	permitJoin = false;

	@Ref("input") readonly input! : Array<HTMLInputElement>;

	async mounted()
	{
		Manager.connection!.on("permitJoinReceived", (permitJoin : boolean) => {
			this.permitJoin = permitJoin;
		});

		Manager.connection!.on("nodeLabelUpdated", () => {
			EventBus.$emit("addSuccessToast", 'Label updated');
		});

		Manager.connection!.on("stateReceived", async (state: ManagerStateEnum) => {
			await Manager.connection!.send("GetPermitJoin");
		});

		await Manager.connection?.send("GetPermitJoin");
	}

	public async togglePermitJoin()
	{
		try{
			if(this.permitJoin === true && Manager.devices.some(x => x.isSynchronizing))
			{
				EventBus.$emit("addAlertToast", `Can't stop while a device is synchronizing.`);
				return;
			}

			await Manager.connection?.send("SetPermitJoin", !this.permitJoin);
		}
		catch(e)
		{
		}
	}

	public async edit(device : Device, index : number)
	{
		device.editing = true;
		device.editText = device.label;

		this.$nextTick(() => {
			this.input[index].focus();
			this.input[index].select();
		});
	}

	public async blur(device : Device, index : number)
	{
		if(!this.isEmpty(device.editText) && device.editText !== device.label)
		{
			device.label = device.editText;
			await Manager.connection!.send("SetNodeLabel", device.ieeeAddress, device.label);
		}

		device.editing = false;
	}

	isEmpty(text: string): boolean {
		return text === null || text.match(/^ *$/) !== null;
	}
}
</script>

<style lang="scss">
	.card{
		background-color: #1C1C1C;
	}
</style>