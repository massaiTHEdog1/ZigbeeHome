<template>
	<div>
		<input ref="label" type="text" class="w-100 mb-0 label-input" :title="label" v-model="label" @blur="blurLabel" @keyup.enter="blurLabel" df-label/>

		<hr class="mb-2 mt-2"/>

		<p class="mb-1">Wait (ms) :</p>
		<input ref="delay" type="number" class="w-100 mb-0" min="0" max="65535" v-model="delay" df-delay/>

		<p class="node-type">Command : Wait</p>
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
export default class WaitCommand extends Vue {

	Manager = Manager;

	label = "Wait";
	labelBeforeEdition = "";
	delay = 1000;

	@Ref("label") labelElement! : HTMLInputElement;
	@Ref("delay") delayElement! : HTMLInputElement;

	mounted()
	{
		this.$nextTick(() => {
			this.label = this.labelElement.value;
			this.delay = +this.delayElement.value;
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