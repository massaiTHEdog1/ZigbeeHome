<template>
	<div>
		<input ref="label" type="text" class="w-100 mb-0 label-input" :title="label" v-model="label" @blur="blurLabel" @keyup.enter="blurLabel" df-label/>

		<hr class="mb-2 mt-2"/>

		<p class="mb-1 mt-1">Variable :</p>
		<input ref="variable" placeholder="MyVariable" type="text" class="form-control form-control-sm" :title="variable" v-model="variable" df-variable/>

		<p class="mb-1 mt-1">Value :</p>
		<input ref="value" type="text" class="form-control form-control-sm" :title="value" v-model="value" df-value/>

		<p class="node-type">Set variable</p>
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
export default class SetVariable extends Vue {

	Manager = Manager;

	label = "Set variable";
	labelBeforeEdition = "";
	variable : string | null = null;
	value : string | null = null;

	@Ref("label") labelElement! : HTMLInputElement;
	@Ref("variable") variableElement! : HTMLInputElement;
	@Ref("value") valueElement! : HTMLInputElement;

	mounted()
	{
		this.$nextTick(() => {
			this.label = this.labelElement.value;
			this.variable = this.variableElement.value;
			this.value = this.valueElement.value;
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