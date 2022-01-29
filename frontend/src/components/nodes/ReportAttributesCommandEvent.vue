<template>
	<div>
		<input ref="label" type="text" class="w-100 mb-0 label-input" :title="label" v-model="label" @blur="blurLabel" @keyup.enter="blurLabel" df-label/>

		<hr class="mb-2 mt-2"/>

		<p class="mb-1">Source :</p>
		<select ref="source" class="custom-select custom-select-sm" df-source>
			<option v-for="device in devices" :key="device.ieeeAddress" :value="device.ieeeAddress">{{device.label}}</option>
		</select>

		<p class="mb-1 mt-1">Store value in :</p>
		<input ref="variable" placeholder="MyVariable" type="text" class="form-control form-control-sm" :title="variable" v-model="variable" df-variable/>

		<p class="node-type">Event : Report attributes</p>
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
export default class ReportAttributesCommandEvent extends Vue {

	Manager = Manager;

	label = "On report attributes";
	labelBeforeEdition = "";
	source : number | null = null;
	variable : string | null = null;

	@Ref("label") labelElement! : HTMLInputElement;
	@Ref("source") sourceElement! : HTMLInputElement;
	@Ref("variable") variableElement! : HTMLInputElement;

	mounted()
	{
		this.$nextTick(() => {
			this.label = this.labelElement.value;
			this.source = parseInt(this.sourceElement.value) || null;
			this.variable = this.variableElement.value;
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