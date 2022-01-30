<template>
	<b-container fluid class="h-100 d-flex flex-column pl-0 pr-0">
		<b-navbar toggleable="sm" type="dark">
			<b-navbar-brand to="/">ZigBeeHome</b-navbar-brand>

			<b-navbar-toggle target="nav-collapse"></b-navbar-toggle>

			<b-collapse id="nav-collapse" is-nav>
				<b-navbar-nav v-if="Manager.connection !== null && Manager.connection.state === HubConnectionState.Connected">
					<b-nav-item to="/editor" exact-active-class="link-active">Editor</b-nav-item>
					<b-nav-item to="/devices" exact-active-class="link-active">Devices</b-nav-item>
				</b-navbar-nav>

				<!-- Right aligned nav items -->
				<b-navbar-nav class="ml-auto">
					<b-nav-text>
						<div class="state border-light" :style="{ 'background-color' : stateColor }"></div> {{ stateText }}
						<b-button size="sm" class="ml-1" v-show="Manager.connection === null || Manager.connection.state === HubConnectionState.Disconnected" @click="Manager.startConnectionToServer()">Reconnect</b-button>
						<b-spinner small v-show="Manager.connection !== null && (Manager.connection.state === HubConnectionState.Connecting || Manager.connection.state === HubConnectionState.Reconnecting)"></b-spinner>
						<b-button size="sm" variant="info" class="ml-1" v-show="Manager.state === ManagerStateEnum.STOPPED" @click="Manager.startManager()">Start</b-button>
						<b-button size="sm" variant="danger" class="ml-1" v-show="Manager.state === ManagerStateEnum.RUNNING" @click="Manager.reinitializeDrawflow()">Reinitialize</b-button>
						<b-spinner small v-show="Manager.state !== null && Manager.state === ManagerStateEnum.INITIALIZING"></b-spinner>
					</b-nav-text>
				</b-navbar-nav>
			</b-collapse>
		</b-navbar>

		<keep-alive>
			<router-view />
		</keep-alive>
	</b-container>
</template>

<style lang="scss">

</style>

<script lang="ts">
import { Component, Prop, Vue, Watch } from "vue-property-decorator";
import { EventBus, Manager } from "@/main";
import { ManagerStateEnum } from "./classes/enums/ManagerStateEnum";
import { HubConnectionState } from "@microsoft/signalr";

@Component({
	components: {
		
	},
})
export default class App extends Vue {

	Manager = Manager;
	ManagerStateEnum = ManagerStateEnum;
	HubConnectionState = HubConnectionState;

	async mounted()
	{
		EventBus.$on("addAlertToast", this.addAlertToast);
		EventBus.$on("addSuccessToast", this.addSuccessToast);
	}

	async addAlertToast(text : string)
	{
		this.$bvToast.toast(text, {
			title: 'Error',
			variant: 'danger',
			solid: true,
			appendToast: true,
			toaster: 'b-toaster-bottom-right'
		});
	}

	async addSuccessToast(text : string, autoHide = true)
	{
		this.$bvToast.toast(text, {
			title: 'Success',
			variant: 'success',
			solid: true,
			appendToast: true,
			toaster: 'b-toaster-bottom-right',
			noAutoHide: !autoHide
		});
	}

	public get stateColor() : string
	{
		return Manager.state === ManagerStateEnum.STOPPED ? "red" :
				Manager.state === ManagerStateEnum.INITIALIZING ? "orange" :
				Manager.state === ManagerStateEnum.RUNNING ? "green" :
				"transparent";
	}
	
	public get stateText() : string
	{
		return Manager.state === ManagerStateEnum.STOPPED ? "Stopped" :
				Manager.state === ManagerStateEnum.INITIALIZING ? "Initializing" :
				Manager.state === ManagerStateEnum.RUNNING ? "Running" :
				(Manager.connection?.state === HubConnectionState.Connecting || Manager.connection?.state === HubConnectionState.Reconnecting) ? "Connecting" :
				"Not connected";
	}
}
</script>


<style lang="scss">
	.navbar{
		background-color: #1C1C1C;
	}

	.state
	{
		width: 10px; 
		height: 10px;
		display: inline-block;
		border-radius: 100%;
		border: 1px solid;
	}

	.link-active
	{
		color: white !important;
	}
</style>