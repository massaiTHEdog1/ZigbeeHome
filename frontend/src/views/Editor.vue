<template>
	<b-container fluid class="h-100 overflow-hidden">
		<b-row class="h-100">
			<b-col style="flex: 0 0 300px; overflow-y: auto;" class="h-100">
				<draggable-parent-group Label="Variables">
					<draggable-element Label="Set variable" :NodeType="NodeTypeEnum.SET_VARIABLE"></draggable-element>
					<draggable-element Label="Condition" :NodeType="NodeTypeEnum.CONDITION"></draggable-element>
				</draggable-parent-group>
				
				<draggable-parent-group Label="Events">
					<draggable-element Label="Manual trigger" :NodeType="NodeTypeEnum.MANUAL_TRIGGER"></draggable-element>
					<draggable-element Label="On startup" :NodeType="NodeTypeEnum.ON_STARTUP"></draggable-element>
					<draggable-element Label="On report attributes" :NodeType="NodeTypeEnum.ON_REPORT_ATTRIBUTES_COMMAND_RECEIVED"></draggable-element>
				</draggable-parent-group>

				<draggable-parent-group Label="Commands">
					<draggable-element Label="On" :NodeType="NodeTypeEnum.ON_COMMAND"></draggable-element>
					<draggable-element Label="Off" :NodeType="NodeTypeEnum.OFF_COMMAND"></draggable-element>
					<draggable-element Label="Move to level" :NodeType="NodeTypeEnum.MOVE_TO_LEVEL_COMMAND"></draggable-element>
					<draggable-element Label="Move to color" :NodeType="NodeTypeEnum.MOVE_TO_COLOR_COMMAND"></draggable-element>
					<draggable-element Label="Wait" :NodeType="NodeTypeEnum.WAIT_COMMAND"></draggable-element>
				</draggable-parent-group>
			</b-col>
			<b-col class="h-100 p-0">
				<div ref="editor" style="height: 100%;" @drop="onDrop" @dragover="dragover($event)" @dragenter.prevent @wheel="onWheel"></div>
			</b-col>
		</b-row>
	</b-container>
</template>

<script lang="ts">
import { Component, Ref, Vue } from "vue-property-decorator";
import { HubConnection, HubConnectionBuilder, HubConnectionState } from "@microsoft/signalr";
import { ManagerStateEnum } from '@/classes/enums/ManagerStateEnum'
import Drawflow from 'drawflow'
import { NodeTypeEnum } from '@/classes/enums/NodeTypeEnum'
import DraggableElement from "@/components/DraggableElement.vue";
import DraggableParentGroup from "@/components/DraggableParentGroup.vue";
import OnCommand from "@/components/nodes/OnCommand.vue";
import OffCommand from "@/components/nodes/OffCommand.vue";
import MoveToLevelCommand from "@/components/nodes/MoveToLevelCommand.vue";
import MoveToColorCommand from "@/components/nodes/MoveToColorCommand.vue";
import WaitCommand from "@/components/nodes/WaitCommand.vue";
import StartupEvent from "@/components/nodes/StartupEvent.vue";
import ReportAttributesCommandEvent from "@/components/nodes/ReportAttributesCommandEvent.vue";
import ManualTriggerEvent from "@/components/nodes/ManualTriggerEvent.vue";
import SetVariable from "@/components/nodes/SetVariable.vue";
import Condition from "@/components/nodes/Condition.vue";
import { Manager } from "@/main";

require('/node_modules/drawflow/dist/drawflow.min.css')

@Component({
  components: {
		DraggableElement,
		DraggableParentGroup
  },
})
export default class Editor extends Vue {

	NodeTypeEnum = NodeTypeEnum;
	@Ref("editor") editorElement! : HTMLElement;
	editor! : Drawflow;

	async mounted() {
		this.editor = new Drawflow(this.editorElement, Vue, this);
		this.editor.registerNode(NodeTypeEnum[NodeTypeEnum.ON_COMMAND], OnCommand, {}, {});
		this.editor.registerNode(NodeTypeEnum[NodeTypeEnum.OFF_COMMAND], OffCommand, {}, {});
		this.editor.registerNode(NodeTypeEnum[NodeTypeEnum.MOVE_TO_LEVEL_COMMAND], MoveToLevelCommand, {}, {});
		this.editor.registerNode(NodeTypeEnum[NodeTypeEnum.MOVE_TO_COLOR_COMMAND], MoveToColorCommand, {}, {});
		this.editor.registerNode(NodeTypeEnum[NodeTypeEnum.WAIT_COMMAND], WaitCommand, {}, {});
		this.editor.registerNode(NodeTypeEnum[NodeTypeEnum.ON_STARTUP], StartupEvent, {}, {});
		this.editor.registerNode(NodeTypeEnum[NodeTypeEnum.ON_REPORT_ATTRIBUTES_COMMAND_RECEIVED], ReportAttributesCommandEvent, {}, {});
		this.editor.registerNode(NodeTypeEnum[NodeTypeEnum.MANUAL_TRIGGER], ManualTriggerEvent, {}, {});
		this.editor.registerNode(NodeTypeEnum[NodeTypeEnum.SET_VARIABLE], SetVariable, {}, {});
		this.editor.registerNode(NodeTypeEnum[NodeTypeEnum.CONDITION], Condition, {}, {});
		this.editor.reroute = true;
		this.editor.start();

		this.editor.on("nodeCreated", this.saveEditor);
		this.editor.on("nodeRemoved", this.saveEditor);
		this.editor.on("nodeMoved", this.saveEditor);
		this.editor.on("connectionCreated", this.saveEditor);
		this.editor.on("connectionRemoved", this.saveEditor);
		this.editor.on("moduleCreated", this.saveEditor);
		this.editor.on("moduleChanged", this.saveEditor);
		this.editor.on("moduleRemoved", this.saveEditor);

		Manager.connection!.on("drawflowReceived", (json : string) => {

			if(!this.isEmpty(json))
			{
				this.editor.import(JSON.parse(json));
			}
		});

		Manager.connection!.send("GetDrawflow");
	}

	isEmpty(text: string): boolean {
		return text === null || text.match(/^ *$/) !== null;
	}

	public async saveEditor()
	{
		Manager.connection!.send("SaveDrawflow", JSON.stringify(this.editor.export()));
	}

	public async dragover(event : DragEvent)
	{
		if(event.dataTransfer!.types.includes("nodetypeenum"))
		{
			event.preventDefault();
		}
	}

	public onDrop (event : DragEvent) {

		const enumValue = +event.dataTransfer!.getData('nodetypeenum') as NodeTypeEnum;

		if((event.target as any).classList.contains("drawflow"))//if we drop on the drawflow
		{
			this.addNodeToDrawFlow(enumValue, event.offsetX, event.offsetY);
		}
		else//If we drop on the parent
		{
			const parentPos = this.editorElement.getBoundingClientRect();
			const childPos = this.editorElement.querySelector(".drawflow")!.getBoundingClientRect();
			let childPosRelativeToParent = {left : 0, top : 0};

			childPosRelativeToParent.top = childPos.top - parentPos.top,
			childPosRelativeToParent.left = childPos.left - parentPos.left;

			this.addNodeToDrawFlow(enumValue, (event.offsetX - childPosRelativeToParent.left) / this.editor.zoom, (event.offsetY - childPosRelativeToParent.top) / this.editor.zoom);
		}
	}

	public addNodeToDrawFlow(nodeType : NodeTypeEnum, posX : number, posY : number)
	{
		switch(nodeType)
		{
			case NodeTypeEnum.ON_COMMAND:
				this.editor.addNode(NodeTypeEnum[nodeType], 1, 1, posX, posY, "", {}, NodeTypeEnum[nodeType], 'vue');
				break;
			case NodeTypeEnum.OFF_COMMAND:
				this.editor.addNode(NodeTypeEnum[nodeType], 1, 1, posX, posY, "", {}, NodeTypeEnum[nodeType], 'vue');
				break;
			case NodeTypeEnum.MOVE_TO_LEVEL_COMMAND:
				this.editor.addNode(NodeTypeEnum[nodeType], 1, 1, posX, posY, "", {}, NodeTypeEnum[nodeType], 'vue');
				break;
			case NodeTypeEnum.MOVE_TO_COLOR_COMMAND:
				this.editor.addNode(NodeTypeEnum[nodeType], 1, 1, posX, posY, "", {}, NodeTypeEnum[nodeType], 'vue');
				break;
			case NodeTypeEnum.WAIT_COMMAND:
				this.editor.addNode(NodeTypeEnum[nodeType], 1, 1, posX, posY, "", {}, NodeTypeEnum[nodeType], 'vue');
				break;
			case NodeTypeEnum.ON_STARTUP:
				this.editor.addNode(NodeTypeEnum[nodeType], 0, 1, posX, posY, "", {}, NodeTypeEnum[nodeType], 'vue');
				break;
			case NodeTypeEnum.ON_REPORT_ATTRIBUTES_COMMAND_RECEIVED:
				this.editor.addNode(NodeTypeEnum[nodeType], 0, 1, posX, posY, "", {}, NodeTypeEnum[nodeType], 'vue');
				break;
			case NodeTypeEnum.MANUAL_TRIGGER:
				this.editor.addNode(NodeTypeEnum[nodeType], 0, 1, posX, posY, "", {}, NodeTypeEnum[nodeType], 'vue');
				break;
			case NodeTypeEnum.SET_VARIABLE:
				this.editor.addNode(NodeTypeEnum[nodeType], 1, 1, posX, posY, "", {}, NodeTypeEnum[nodeType], 'vue');
				break;
			case NodeTypeEnum.CONDITION:
				this.editor.addNode(NodeTypeEnum[nodeType], 1, 2, posX, posY, "", {}, NodeTypeEnum[nodeType], 'vue');
				break;
			default:
				throw new Error(`Node '${NodeTypeEnum[nodeType]}' not implemented !`);
		}
		
	}

	public onWheel(event : WheelEvent)
	{
		if(event.deltaY > 0)
		{
			this.editor.zoom_out();	
		}
		else if(event.deltaY < 0)
		{
			this.editor.zoom_in();
		}
	}
}
</script>

<style lang="scss">
	.drawflow-node
	{
		background-color: #3d3d3d !important;
		color: white !important;
		padding: 5px 15px !important;
		font-size: 0.8rem !important;
	}

	.drawflow-node.selected
	{
		background-color: #595959 !important;
	}

	.drawflow-node hr
	{
		background-color: white;
	}
</style>