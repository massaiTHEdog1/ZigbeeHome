export class Device
{
	ieeeAddress! : number;
	networkAddress! : number;
	isSynchronizing! : boolean;
	label! : string;
	editing = false;
	editText! : string;

	public constructor(init?:Partial<Device>) {
        Object.assign(this, init);
    }
}