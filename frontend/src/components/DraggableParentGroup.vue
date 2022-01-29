<template>
	<div>
		<b-button v-b-toggle="'collapse_' + _uid.toString()" variant="primary" class="rounded-0 w-100 text-left mt-3">
			{{ Label }}
		</b-button>
		<b-collapse visible :id="'collapse_' + _uid.toString()">
			<slot></slot>
		</b-collapse>
	</div>
</template>

<script lang="ts">
import { Component, Prop, Ref, Vue } from "vue-property-decorator";

@Component({
  components: {
  },
})
export default class DraggableParentGroup extends Vue {

	@Prop({required: true, type: String}) readonly Label! : string;
	
}
</script>

<style lang="scss" scoped>
	.btn {
		box-shadow: none !important;
	}

	.collapse
	{
		padding: 0.5rem;
		width: 100%;
		//background-color: yellow;
	}
</style>