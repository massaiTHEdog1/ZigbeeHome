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
				<b-container fluid class="h-100">
					<b-row>
						<div class="ml-auto mr-auto">
							<div class="module" :class="{ 'active' : mod.active }" v-for="mod in modules" :key="mod.label" @click="changeModule(mod.label)">
								{{mod.label}}
								<span style="margin-left: 12px;" v-show="mod.label !== 'Home'">
									<b-icon-pencil-fill class="align-baseline" font-scale="0.8" title="Rename" @click="renameModule(mod)"></b-icon-pencil-fill>
								</span>
								<span style="margin-left: 6px;" v-show="mod.label !== 'Home'">
									<b-icon-trash-fill class="align-baseline text-danger" font-scale="0.8" title="Delete" @click="deleteModule(mod)"></b-icon-trash-fill>
								</span>
							</div>
							<div class="module" title="Add new module" @click="addModule">+</div>
						</div>

						<b-modal centered 
							id="modal-module-name"
							title="Module name"
							@ok="handleOk"
							header-bg-variant="dark"
							body-bg-variant="dark"
							footer-bg-variant="dark"
							header-text-variant="light"
						>
							<form ref="form" @submit.stop.prevent="handleSubmit">
								<b-form-group
								label="Name"
								label-for="name-input"
								invalid-feedback="Name is invalid."
								:state="moduleNameState"
								>
								<b-form-input
									id="name-input"
									v-model="moduleName"
									:state="moduleNameState"
								></b-form-input>
								</b-form-group>
							</form>
						</b-modal>

						<b-modal centered 
							id="modal-module-name-edit"
							title="Module name"
							@ok="handleOkEdit"
							header-bg-variant="dark"
							body-bg-variant="dark"
							footer-bg-variant="dark"
							header-text-variant="light"
						>
							<form ref="form" @submit.stop.prevent="handleSubmitEdit">
								<b-form-group
								label="Name"
								label-for="name-input"
								invalid-feedback="Name is invalid or already exists."
								:state="moduleNameEditState"
								>
								<b-form-input
									id="name-input"
									v-model="moduleNameEdit"
									:state="moduleNameEditState"
								></b-form-input>
								</b-form-group>
							</form>
						</b-modal>
					</b-row>
					<b-row class="h-100">
						<div ref="editor" style="height: 100%; width: 100%;" @drop="onDrop" @dragover="dragover($event)" @dragenter.prevent @wheel="onWheel"></div>
					</b-row>
				</b-container>
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
import { BForm, BvModalEvent } from "bootstrap-vue";

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
	modules : Array<Module> = [{ label : "Home", active : true }];

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
		this.editor.on("moduleRemoved", this.saveEditor);

		Manager.connection!.on("drawflowReceived", (json : string) => {

			if(!this.isEmpty(json))
			{
				this.editor.import(JSON.parse(json));
				this.ReloadModules();
				this.changeModule("Home");
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

	public ReloadModules()
	{
		this.modules = Object.keys(this.editor?.drawflow?.drawflow).map(x => { return { label : x, active : false}});
	}

	public changeModule(moduleName : string)
	{
		for(let element of this.modules)
		{
			element.active = element.label === moduleName;
		}

		this.editor.changeModule(moduleName);
	}

	//#region Add module

	moduleName = "";
	moduleNameState : boolean | null = null;

	public addModule()
	{
		this.moduleName = ''
        this.moduleNameState = null
		this.$bvModal.show('modal-module-name')
	}

	public handleOk(event : BvModalEvent) {
        // Prevent modal from closing
        event.preventDefault()
        // Trigger submit handler
        this.handleSubmit()
	}

	public handleSubmit() {
		// Exit when the form isn't valid
		const valid = !this.isEmpty(this.moduleName) && !this.modules.some(x => x.label === this.moduleName)
		this.moduleNameState = valid

		if (!valid) {
			return
		}
		
		this.editor.addModule(this.moduleName);

		this.ReloadModules();
		this.changeModule(this.moduleName);
		this.saveEditor();

		// Hide the modal manually
		this.$nextTick(() => {
			this.$bvModal.hide('modal-module-name')
		})
	}

	//#endregion Add module

	//#region Rename module

	moduleInEdition : Module | null = null;
	moduleNameEdit = "";
	moduleNameEditState : boolean | null = null;

	public renameModule(module : any)
	{
		this.moduleInEdition = module;
		this.moduleNameEdit = this.moduleInEdition!.label;
		this.moduleNameEditState = null;
		this.$bvModal.show('modal-module-name-edit')
	}

	public handleOkEdit(event : BvModalEvent) {
        // Prevent modal from closing
        event.preventDefault();
        // Trigger submit handler
        this.handleSubmitEdit();
	}

	public handleSubmitEdit() {

		if(this.moduleNameEdit === this.moduleInEdition?.label)//If name is unchanged
		{
			this.$nextTick(() => {
				this.$bvModal.hide('modal-module-name-edit')
			});

			return;
		}

		// Exit when the form isn't valid
		const valid = !this.isEmpty(this.moduleNameEdit) && !this.modules.some(x => x.label === this.moduleNameEdit)
		this.moduleNameEditState = valid

		if (!valid) {
			return
		}
		
		const old_key = this.moduleInEdition!.label;
		const new_key = this.moduleNameEdit;

		//delete Object.assign(this.editor.drawflow.drawflow, {[new_key]: this.editor.drawflow.drawflow[old_key] })[old_key];

		this.editor.drawflow.drawflow = this.RenameKeyInObject(this.editor.drawflow.drawflow, old_key, new_key);

		this.ReloadModules();
		this.changeModule(new_key);
		this.saveEditor();

		// Hide the modal manually
		this.$nextTick(() => {
			this.$bvModal.hide('modal-module-name-edit');
		})
	}

	/** Return the object with a key renamed */
	RenameKeyInObject(object : any, oldKey : string, newKey : string){
		const keys = Object.keys(object);
		const newObj = keys.reduce((acc : any, val)=>{
			if(val === oldKey){
				acc[newKey] = object[oldKey];
			}
			else {
				acc[val] = object[val];
			}
			return acc;
		}, {});

		return newObj;
	}

	//#endregion Rename module

	public async deleteModule(module : Module)
	{
		const confirmation = await this.$bvModal.msgBoxConfirm(`Please confirm that you want to delete module '${module.label}'.`, {
			title: 'Please Confirm',
			size: 'sm',
			buttonSize: 'sm',
			okVariant: 'danger',
			okTitle: 'YES',
			cancelTitle: 'NO',
			footerClass: 'p-2',
			hideHeaderClose: false,
			centered: true,
			headerBgVariant: "dark",
			bodyBgVariant: "dark",
			footerBgVariant: "dark",
			headerTextVariant: "light"
        });

		if(confirmation === true)
		{
			this.editor.removeModule(module.label);
			this.ReloadModules();
			this.changeModule("Home");
		}
	}
}

class Module
{
	label = "";
	active = false;
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

	.module
	{
		background-color: #303030;
		color: white;
		display: inline-block;
		cursor: pointer;
		padding: 0.5rem 0.5rem 0.5rem 1rem;
	}

	.module:not(:first-child)
	{
		border-left: 1px solid rgb(61, 61, 61);
	}

	.module:first-child
	{
		padding: 0.5rem 1rem;
	}

	.module:last-child
	{
		padding: 0.5rem 1rem;
		font-weight: bold;
	}

	.module:hover
	{
		background-color: #2e2e2e;
	}

	.module.active
	{
		background-color: #3e3e3e;
		font-weight: bold;
	}
</style>