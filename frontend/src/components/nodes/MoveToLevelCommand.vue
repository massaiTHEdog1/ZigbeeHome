<template>
	<div>
		<input ref="label" type="text" class="w-100 mb-0 label-input" :title="label" v-model="label" @blur="blurLabel" @keyup.enter="blurLabel" df-label/>

		<hr class="mb-2 mt-2"/>

		<p class="mb-1">Target :</p>
		<select ref="target" class="custom-select custom-select-sm" df-target>
			<option v-for="device in devices" :key="device.ieeeAddress" :value="device.ieeeAddress">{{device.label}}</option>
		</select>

		<p class="mb-0 mt-1">Level : {{level}}</p>
		<input ref="range" type="range" class="w-100 mb-0" min="0" max="255" v-model="level" df-level/>

		<p class="mb-0 mt-1">Transition time :</p>
		<input ref="transitionTime" type="number" class="w-100 mb-0" min="0" max="65535" v-model="transitionTime" df-transitionTime/>

		<p class="node-type">Command : Move to level</p>
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
export default class MoveToLevelCommand extends Vue {

	Manager = Manager;

	label = "Move to level";
	target : number | null = null;
	labelBeforeEdition = "";
	level = 0;
	transitionTime = 0;

	@Ref("label") labelElement! : HTMLInputElement;
	@Ref("target") targetElement! : HTMLInputElement;
	@Ref("range") rangeElement! : HTMLInputElement;
	@Ref("transitionTime") transitionTimeElement! : HTMLInputElement;

	mounted()
	{
		this.$nextTick(() => {
			this.label = this.labelElement.value;
			this.target = parseInt(this.targetElement.value) || null;
			this.level = +this.rangeElement.value;
			this.transitionTime = +this.transitionTimeElement.value;
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