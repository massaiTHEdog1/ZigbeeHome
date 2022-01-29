<template>
	<div class="element" draggable @dragstart="startDrag($event, NodeType)">{{Label}}</div>
</template>

<script lang="ts">
import { Component, Prop, Ref, Vue } from "vue-property-decorator";
import { NodeTypeEnum } from '@/classes/enums/NodeTypeEnum'

@Component({
  components: {
  },
})
export default class DraggableElement extends Vue {

	@Prop({required: true, type: String}) readonly Label! : string;
	@Prop({required: true}) readonly NodeType! : NodeTypeEnum;
	
	NodeTypeEnum = NodeTypeEnum;
	
	public startDrag (event : DragEvent, nodeType : NodeTypeEnum) {
        event.dataTransfer!.dropEffect = 'move';
        event.dataTransfer!.effectAllowed = 'move';
        event.dataTransfer!.setData('nodetypeenum', nodeType.toString());
	}
}
</script>

<style lang="scss" scoped>
	.element
	{
		cursor: grab;
		background-color: #1C1C1C;
		padding: 2px 5px;
	}

	.element:not(:first-child)
	{
		margin-top: 5px;
	}
</style>