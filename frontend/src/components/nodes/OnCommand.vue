<template>
	<div>
		<input ref="label" type="text" class="w-100 mb-0 label-input" :title="label" v-model="label" @blur="blurLabel" @keyup.enter="blurLabel" df-label/>

		<hr class="mb-2 mt-2"/>

		<p class="mb-1">Target :</p>
		<select ref="target" class="custom-select custom-select-sm" df-target>
			<option v-for="device in devices" :key="device.ieeeAddress" :value="device.ieeeAddress">{{device.label}}</option>
		</select>

		<p class="node-type">Command : On</p>
	</div>
</template>

<script lang="ts">
import { Component, Prop, Ref, Vue } from "vue-property-decorator";
import { Manager } from "@/main";
import { Device } from "@/classes/Device";

@Component({
  components: {
  },
})
export default class OnCommand extends Vue {

	Manager = Manager;

	label = "Turn On";
	labelBeforeEdition = "";
	target : number | null = null;

	@Ref("label") labelElement! : HTMLInputElement;
	@Ref("target") targetElement! : HTMLInputElement;

	mounted()
	{
		this.$nextTick(() => {
			this.label = this.labelElement.value;
			this.target = parseInt(this.targetElement.value) || null;
		});
	}

	public async editLabel()
	{
		this.labelBeforeEdition = this.label;
	}

	public async blurLabel()
	{
		if(this.isEmpty(this.label) || this.label === this.labelBeforeEdition)
		{
			this.label = this.labelBeforeEdition;
		}
	}

	isEmpty(text: string): boolean {
		return text === null || text.match(/^ *$/) !== null;
	}

	public get devices() : Array<Device>
	{
		return [new Device({}), ...Manager.devices]
	}
}
</script>

<style lang="scss" scoped>
	.label-input:not(:focus)
	{
		background-color: transparent;
		border: 0;
		color: white;
		cursor: pointer;
	}

	.node-type
	{
		margin: 5px 0 0 0;
		font-style: italic;
		font-size: 0.7rem;
		text-align: right;
	}
</style>