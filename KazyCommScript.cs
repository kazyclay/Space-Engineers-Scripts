/*Instructions:
   In your cockpit add tool bar items for the programming block 
   'previous'	in the first slot
   'next' 		in the second 
   'select' 	in third slot
   'cancel' 	in forth slot
   'scan' 		to any other slot 
   - The menu will show on an LCD named 'LCDMain' if not then the LCD first found.
   - IMPORTANT: If you plan to communicate you should have a UNIQUE antenna name for each ship with the script. 
   - IMPORTANT: You will need to change your antenna's Assigned Programming Block to the program block this program is in. 
   - You may set up a timer to run the default argument to allow your ship to 'ping' to be picked up by other ships with the same script
   - Copy the script to any ships you want to communicate with. 
   - The script finds what blocks it needs so you don't have to name anything.
   -- LCDs and such can be specified with the script's on screen menu.
*/
//These are the block names for the script
int TrageterMaxDistance          = 50000;  				//change this for how far out you want to target in meters
string LCD_Main_Name  			= "LCDMain";			//LCD	  default selected: first LCD found 
string LCD_Signals_Name			= "LCDSignals";			//LCD	  default selected: null
string LCD_Targets_Received_Name= "LCDTargets";			//LCD	  default selected: null
string LCD_Raycast_Targets_Name = "LCDRaycast"; 		//LCD  	  default selected: null
string Antenna_Name				= "Comm";				//Antenna default selected: first antenna turned on with broadcasting on
string Targeting_Camera_Name	= "Targeting Camera";	//Camera  default selected: first camera found
string Remote_Control_Name		= "Main Remote Control";//Remote  default selected: first Remote Block found

// Selection variables 
string selectedBlockName = null;
string modifingSelectedBlockName = null;
string selectedBlockType = null;
string selectedSignal = null;


//used to repopulate the remote ship menu
string lastReply = null;
string LCD_Main_Current_text = null;
string RemoteShip = null;
string insertbefore = "";
int cancelCount = 0;

bool setupMessage = true;

//Selection Blocks
IMyRemoteControl shipRemote = null;
IMyCameraBlock TargetingCamera = null;
IMyTextPanel LCD_Signals = null, LCD_Targets_Received = null, LCD_Raycast_Targets = null, LCD_Main = null; //LCD_Main2 = null;
IMyTextPanel LCD_Signals2 = null, LCD_Targets_Received2 = null, LCD_Raycast_Targets2 = null, LCD_Main2 = null; // second set of screens [optional]
IMyTextPanel SelectedLCD = null;
IMyRadioAntenna SelectedAntenna = null;
IMyCameraBlock SelectedCamera = null;
IMyCargoContainer SelectedCargo = null;
IMyRemoteControl SelectedRemote = null;
IMyThrust SelectedThruster = null;
IMyRefinery SelectedRefinery = null;
IMyAssembler SelectedAssembler = null;
IMyLargeTurretBase SelectedTurret = null;


string currentMenu = "Main", debug = "";
int currentMenuItem = 1;
bool MenuSetup = true;
bool blockActionUpdate = false;
bool RemoteAccess = true;

List<string> Signals = new List<string>();
List<string> Scans = new List<string>();
List<string> ScanGPS = new List<string>();

float distanceToTarget;

IMyRadioAntenna Comm = null;//

// Define/Populate Block Group Lists:
List<IMyTerminalBlock> antennas = new List<IMyTerminalBlock>(); 
List<IMyTerminalBlock> turrets = new List<IMyTerminalBlock>(); 
List<IMyTerminalBlock> thrusters = new List<IMyTerminalBlock>(); 
List<IMyTerminalBlock> lcdpanels = new List<IMyTerminalBlock>(); 
List<IMyTerminalBlock> cameras = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> laserantennas = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> remoteblocks = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> cargos = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> refineries = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> assemblers = new List<IMyTerminalBlock>();

List<List<string>> menu = new List<List<string>>();

Vector3D position = new Vector3D(0,0,0);
Vector3D Pos;
long NowTime;

double get_speed() 
{ 
	Vector3D current_position = Me.GetPosition();  
	double speed = Math.Round(((current_position-position)*60).Length()/100,2); 
	position = current_position; 
	return speed; 
}
private MyTransmitTarget getTarget( int dst ) { 
    MyTransmitTarget target = MyTransmitTarget.None;   
    if( (dst & 1) == 1 ) {   
        target = target | MyTransmitTarget.Owned;   
    }   
    if( (dst & 2) == 2 ) {   
        target = target | MyTransmitTarget.Ally;   
    }   
    if( (dst & 4) == 4 ) {   
        target = target | MyTransmitTarget.Neutral;   
    }   
    if( (dst & 8) == 8 ) {   
        target = target | MyTransmitTarget.Enemy;   
    }   
    return target;   
} 

void AddBlockActions(IMyTerminalBlock block, string blockTag, string previousMenuName, string additionalMenus = null, bool hasInventory = false)
{
	List<string> tempList = new List<string>();
	tempList.Add(blockTag+":Settings");
	tempList.Add(previousMenuName);
	tempList.Add("["+blockTag+" Actions]");
	if(additionalMenus!=null){
		if(additionalMenus.Contains("|")){
			string[] additionalMenus_arr=additionalMenus.Split('|');
			for(int i = 0; i < additionalMenus.Length; i++){
				tempList.Add(additionalMenus_arr[i]);
			}
		}else{
			tempList.Add(additionalMenus);
		}
		
	}
	List<ITerminalProperty> Property = new List<ITerminalProperty>();
	block.GetProperties(Property); 
	foreach (var action in Property) { 
		string x = "";
		if(action.TypeName.ToString() == "Int64"){
			x = block.GetValue<long>(action.Id.ToString()).ToString();
			tempList.Add("I:"+action.Id+": "+x);
		} else if (action.TypeName.ToString() == "StringBuilder"){
			x = block.CustomName.ToString();
			tempList.Add("S:"+action.Id+": "+x);
		} else if (action.TypeName.ToString() == "Single"){
			x = block.GetValue<Single>(action.Id.ToString()).ToString();
			tempList.Add("F:"+action.Id+": "+x);
		} else {
			x = "UnknownType: "+action.TypeName.ToString();
			//tempList.Add("Unknown:"+action.Id+": "+x);
		}
		//Echo(action.Id+" "+action.TypeName+" "+x); 
		//tempList.Add(action.Id+" "+x);
	}
	if(hasInventory){
		tempList.Add("["+blockTag+" Cargo]"); 
		if (block.HasInventory) {
			var containerItems = block.GetInventory(0).GetItems();
			List<string> tempList3 = new List<string>();
			tempList3.Add("["+blockTag+" Cargo]");
			tempList3.Add(blockTag+":Settings");
			List<string> itemAddedArr = new List<string>();
			List<double> itemValueArr = new List<double>();
			for(int j = containerItems.Count-1; j >= 0; j--)      
			{      
				double itemAmount = Math.Floor(Convert.ToDouble(containerItems[j].Amount.ToString()));
				string txt = itemAmount + "   " + containerItems[j].Content.SubtypeId.ToString();
				bool Added = false;
				for(int i = 0; i < itemAddedArr.Count; i++){
					if(containerItems[j].Content.SubtypeId.ToString()==itemAddedArr[i]){
						Added = true;
						itemValueArr[i] = itemValueArr[i]+itemAmount;
					}
				}
				if(!Added){
					itemValueArr.Add(itemAmount);
					itemAddedArr.Add(containerItems[j].Content.SubtypeId.ToString());
				}
			}    
			for(int i=0; i < itemAddedArr.Count;i++){
				tempList3.Add(itemAddedArr[i]+"  "+itemValueArr[i]);
			}
			menu.Add(tempList3);
		}
	}
	menu.Add(tempList);
	List<ITerminalAction> actions = new List<ITerminalAction>();
	List<string> tempList2 = new List<string>();
	tempList2.Add("["+blockTag+" Actions]");
	tempList2.Add(blockTag+":Settings");
	block.GetActions(actions);
	foreach (var action in actions)
	{
		foreach (var action2 in Property) { 
			if (action2.TypeName.ToString() == "Boolean"){
				if(action.Id==action2.Id){
					if(block.GetValue<bool>(action2.Id.ToString())){
						tempList2.Add($"A:\uE001:{action.Id}:");
					}else{
						tempList2.Add($"A:\uE003:{action.Id}:");
					}
					
					
				}	
			} 
		}
	}
	menu.Add(tempList2);
}

string outputMenuString(string menuName, int itemNumber, string insertBetween = "")
{
	//menuName+" "+itemNumber+
	string text = insertbefore+"1:previous 2:next 3:select 4:cancel Comms["+Signals.Count+"]"+insertBetween;
	if(TargetingCamera!=null){
		text = text+ "\nAvailable Raycast Range:  "+(TargetingCamera.AvailableScanRange/1000).ToString()+"km";
	}
	if(Comm!=null){
		if(Comm.Radius>1000){
			text = text+ "\nCurrent Comm Range:  "+(Math.Round(Comm.Radius/1000,1)).ToString()+"km";
		}else{
			text = text+ "\nCurrent Comm Range:  "+(Math.Round(Comm.Radius)).ToString()+"m";
		}
	}
	foreach (var sublist in menu)
	{ 
		
		if(menuName==sublist[0]){
			text = text+"\n----------"+menuName+" Menu----------\n";
			foreach (var value in sublist)
			{
				if (sublist.IndexOf(value)>1){
					if(sublist.IndexOf(value) > itemNumber-4){
						if(sublist.IndexOf(value)==itemNumber+1){
							text = text +"\uE001"+ value+"\n";
						}else{
							text = text + "    " + value+"\n";
						}
					}
				}
			}
		}
	}
	return text;
}

void Main(string argument)
{
	debug="";
	string[] XYZ;
	Echo("run["+argument+"]");
	string[] recieveData = argument.Split(';');
	DateTime dateTimeNow = DateTime.Now;						  //Get Date/time and store in variable.
    NowTime= (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond); //Convert date/time to Milliseconds.
	
	Me.CustomData= Me.CustomData+"\n"+dateTimeNow+"  "+argument;  //add argument to custom data
	
	
	// Get all Text Panels
	GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(lcdpanels);
	// Get all turrets
	GridTerminalSystem.GetBlocksOfType<IMyLargeTurretBase>(turrets);
	// Get all thrusters
	GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);
	// Get all antennas
	GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(antennas);
	// Get all cargo blocks
	GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(cargos);
	// Get all remote blocks
	GridTerminalSystem.GetBlocksOfType<IMyRemoteControl>(remoteblocks);
	// Get all cameras
	GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(cameras);
	// Get all refineries
	GridTerminalSystem.GetBlocksOfType<IMyRefinery>(refineries);
	// Get all assemblers
	GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblers);
	
	if(MenuSetup){
		if(blockActionUpdate!=true){
			menu.Clear();
		} else {
			blockActionUpdate = false;
		}
		MenuSetup = false;	
		menu.Add( new List<string>(new string[] {"Main", "Main","Ship Status","Communications","Raycasts"}));
		if(shipRemote!=null){
			if(shipRemote.GetValue<bool>("AutoPilot")){
				menu[0].Add("AutoPilot: [\uE001]");
			}else{
				menu[0].Add("AutoPilot: [\uE003]");
			}
			
		}
		menu.Add( new List<string>(new string[] {"Ship Status", "Main","LCD Configuration","Turret Control","Thruster Control","Antenna Control","Cargos","Cameras","Remote Control","Clear Logs/Raycasts"}));
		
		List<string> SignalsList = new List<string>(new string[] {"Communications", "Main"});//Communications
		for(int i = 0; i < Signals.Count; i++){  
			string[] nameSplit = Signals[i].Split('@');
			string[] dataSplit = Signals[i].Split('|');
			string[] statSplit = dataSplit[1].Split(',');
			string[] PosSplit = dataSplit[0].Split(':');
			SignalsList.Add("COMM:"+nameSplit[0]+":\n"+statSplit[0]+statSplit[1]+"\n"+statSplit[2]);
			List<string> CommandsList = new List<string>(new string[] {"COMM:"+nameSplit[0]+":\n"+statSplit[0]+statSplit[1]+"\n"+statSplit[2], "Communications","[Connect]"});//Communications Commands
			if(shipRemote!=null){
				CommandsList.Add("[AutoPilot to :,"+nameSplit[0]+",]"+statSplit[1]+"\n     ----->,"+PosSplit[1]+","+PosSplit[3]+","+PosSplit[5]);
			}
			menu.Add(CommandsList);
		}
		menu.Add(SignalsList);
		List<string> RaycastList = new List<string>(new string[] {"Raycasts", "Main"});//Raycasts
		for(int i = 0; i < Scans.Count; i++){  
			string[] dataSplit = Scans[i].Split(':');
			//Scans.Add(Comm.CustomName+":SCAN:"+colorDot2+":"+targetInfo.Name.ToString()+":"+distanceToTarget+":X:"+Math.Round(targetPos.X)+":Y:"+Math.Round(targetPos.Y)+":Z:"+Math.Round(targetPos.Z));
			RaycastList.Add("SCAN:"+dataSplit[2]+":"+dataSplit[3]+": Distance: "+dataSplit[3]+"\n---->: X:"+dataSplit[6]+": Y:"+dataSplit[8]+": Z:"+dataSplit[10]);
		}
		menu.Add(RaycastList);
		
		if(lcdpanels.Count>0){ //LCD Configuration Menu
			List<string> tempList = new List<string>();
			tempList.Add("LCD Configuration");
			tempList.Add("Ship Status");
			for(int i = 0; i < lcdpanels.Count; i++){  
				tempList.Add("LCD:"+lcdpanels[i].CustomName);
			}
			menu.Add(tempList);
			//LCD Configuration Block Settings
			menu.Add( new List<string>(new string[] {"LCD:Settings", "LCD Configuration","Power","Change Name To:"+LCD_Main_Name,"Change Name To:"+LCD_Signals_Name,"Change Name To:"+LCD_Targets_Received_Name,"Change Name To:"+LCD_Raycast_Targets_Name,"Add 2 to end of name"}));
		}else{
			Echo("\nNo LCDs found");
		}

		if(turrets.Count>0){//Turret Control
			List<string> tempList = new List<string>();
			tempList.Add("Turret Control");
			tempList.Add("Ship Status");
			tempList.Add("[Turn Off All Turrets]");
			tempList.Add("[Turn On  All Turrets]");
			tempList.Add("[Set All Turrets To 800m]");
			for(int i = 0; i < turrets.Count; i++){  
				IMyLargeTurretBase tempTurret = turrets[i] as IMyLargeTurretBase;
				if(tempTurret.IsWorking){
					tempList.Add("TUR:[\uE001]:"+Math.Floor(tempTurret.Range)+":"+tempTurret.CustomName);
				}else{	
					tempList.Add("TUR:[\uE003]:"+Math.Floor(tempTurret.Range)+":"+tempTurret.CustomName);
				}	
			}
			
			menu.Add(tempList);
		}else{
			Echo("\nNo turrets found");
		}
		if(thrusters.Count>0){//Thruster Control
			List<string> tempList = new List<string>();
			tempList.Add("Thruster Control");
			tempList.Add("Ship Status");
			tempList.Add("[Turn Off All Thrusters]");
			tempList.Add("[Turn On All Thrusters]");
			tempList.Add("[Toggle Hydrogen Thrusters]");
			tempList.Add("[Remove All Thrust Overrides]");
			for(int i = 0; i < thrusters.Count; i++){  
				if(thrusters[i].IsWorking){
					tempList.Add("THR:[\uE001]:"+thrusters[i].CustomName);
				}else{
					tempList.Add("THR:[\uE003]:"+thrusters[i].CustomName);
				}	
			}
			menu.Add(tempList);
		}else{
			Echo("\nNo thrusters found");
		}
		if(antennas.Count>0){//Antenna Control
			List<string> tempList = new List<string>();
			tempList.Add("Antenna Control");
			tempList.Add("Ship Status");
			tempList.Add("[Engage Radio Silence]");
			tempList.Add("[Set All Antennas to 800m]");
			for(int i = 0; i < antennas.Count; i++){  
				IMyRadioAntenna tComm = antennas[i] as IMyRadioAntenna;
				if(antennas[i].CustomName == Antenna_Name){
					Comm = antennas[i] as IMyRadioAntenna;
				}
				if(tComm.IsWorking){
					if(tComm.IsBroadcasting){
						tempList.Add("ANT:[\uE001]:"+Math.Floor(tComm.Radius)+":"+tComm.CustomName);
						if(Comm==null){Comm = antennas[i] as IMyRadioAntenna;}
					}else{
						tempList.Add("ANT:[\uE004]:"+Math.Floor(tComm.Radius)+":"+tComm.CustomName);
					}
				}else{	
					tempList.Add("ANT:[\uE003]:"+Math.Floor(tComm.Radius)+":"+tComm.CustomName);
				}	
			}
			menu.Add(tempList);
			//Antenna Control Block Settings
			menu.Add( new List<string>(new string[] {"ANT:Settings","Antenna Control","Power","ANT Range","Broadcast","Change Name To :"+Antenna_Name}));
			menu.Add( new List<string>(new string[] {"ANT Range", "ANT:Settings","[Increase ANT Range]","[Decrease ANT Range]"}));
		
		}else{
			Echo("\nNo antennas found");
		}
		List<string> tempList2 = new List<string>();
		tempList2.Add("Cargos");
		tempList2.Add("Ship Status");
		if(cargos.Count>0){					//cargos
			for(int i = 0; i < cargos.Count; i++){  
				tempList2.Add("CGO:"+cargos[i].CustomName);
			}
		}else{
			Echo("\nNo Cargos found");
		}
		if(refineries.Count>0){					//refineries
			for(int i = 0; i < refineries.Count; i++){  
				if(refineries[i].IsWorking){
					tempList2.Add("REF:[\uE001]:"+refineries[i].CustomName);
				}else{
					tempList2.Add("REF:[\uE003]:"+refineries[i].CustomName);
				}
			}
		}else{
			Echo("\nNo Refineries found");
		}
		if(assemblers.Count>0){					//assemblers
			for(int i = 0; i < assemblers.Count; i++){  
				if(assemblers[i].IsWorking){
					tempList2.Add("ASM:[\uE001]:"+assemblers[i].CustomName);
				}else{
					tempList2.Add("ASM:[\uE003]:"+assemblers[i].CustomName);
				}
			}
		}else{
			Echo("\nNo Assemblers found");
		}
		menu.Add(tempList2);
		if(remoteblocks.Count>0){//Remote Control 
			List<string> tempList = new List<string>();
			tempList.Add("Remote Control");
			tempList.Add("Ship Status");
			for(int i = 0; i < remoteblocks.Count; i++){ 
				IMyRemoteControl remote = remoteblocks[i] as IMyRemoteControl;
				if(remote.CustomName == Remote_Control_Name){
					shipRemote = remote as IMyRemoteControl;
				}
				tempList.Add("RMC:"+remoteblocks[i].CustomName);
				if(shipRemote==null){shipRemote = remote as IMyRemoteControl;}
			}
			menu.Add(tempList);
		}else{
			Echo("\nNo Remote Controls found");
		}
		if(cameras.Count>0){//Cameras
			List<string> tempList = new List<string>();
			tempList.Add("Cameras");
			tempList.Add("Ship Status");
			for(int i = 0; i < cameras.Count; i++){  
				if(cameras[i].CustomName == Targeting_Camera_Name){
					TargetingCamera = cameras[i] as IMyCameraBlock;
				}
				if(cameras[i].IsWorking){
					tempList.Add("CAM:[\uE001]:"+cameras[i].CustomName);
					if(TargetingCamera==null){TargetingCamera = cameras[i] as IMyCameraBlock;}
				}else{
					tempList.Add("CAM:[\uE003]:"+cameras[i].CustomName);
				}
			}
			menu.Add(tempList);
			TargetingCamera.EnableRaycast = true; 
		}else{
			Echo("\nNo Cameras found");
		}
		if(lastReply!=null){
			string[] lastReplyarr =  lastReply.Split(';');
			menu.Add( new List<string>(new string[] {"RemoteSetting","Communications","Antenna ("+lastReplyarr[1]+")","Laser Antenna ("+lastReplyarr[2]+")","Camera ("+lastReplyarr[3]+")","Turrets("+lastReplyarr[4]+")","Thrusters ("+lastReplyarr[5]+")","LCD Panels("+lastReplyarr[6]+")","Remote Blocks("+lastReplyarr[7]+")","GPS:["+lastReplyarr[8]+"]"}));				
		}
	}//end menu setup
	for(int i = 0; i < lcdpanels.Count; i++){  
		if(lcdpanels[i].CustomName == LCD_Main_Name){LCD_Main = lcdpanels[i] as IMyTextPanel;}
		if(lcdpanels[i].CustomName == LCD_Signals_Name){LCD_Signals = lcdpanels[i] as IMyTextPanel;}
		if(lcdpanels[i].CustomName == LCD_Targets_Received_Name){LCD_Targets_Received = lcdpanels[i] as IMyTextPanel;}
		if(lcdpanels[i].CustomName == LCD_Raycast_Targets_Name){LCD_Raycast_Targets = lcdpanels[i] as IMyTextPanel;}
		if(lcdpanels[i].CustomName == LCD_Signals_Name+"2"){LCD_Signals2 = lcdpanels[i] as IMyTextPanel;}
		if(lcdpanels[i].CustomName == LCD_Targets_Received_Name+"2"){LCD_Targets_Received2 = lcdpanels[i] as IMyTextPanel;}
		if(lcdpanels[i].CustomName == LCD_Raycast_Targets_Name+"2"){LCD_Raycast_Targets2 = lcdpanels[i] as IMyTextPanel;}
		if(lcdpanels[i].CustomName == LCD_Main_Name+"2"){LCD_Main2 = lcdpanels[i] as IMyTextPanel;}
		if(i == lcdpanels.Count-1){// if on last count 
			if( LCD_Main == null && lcdpanels.Count>0){ 
				LCD_Main = lcdpanels[0] as IMyTextPanel;
			}
		}
	}
	
	switch (recieveData[0])
    {
		case "ping":
			Echo("ping "+recieveData[1]);
			XYZ = recieveData[2].Split(',');
			Pos = new Vector3D(Convert.ToDouble(XYZ[0]),Convert.ToDouble(XYZ[1]),Convert.ToDouble(XYZ[2]));
			distanceToTarget = Convert.ToSingle(Math.Round(Math.Sqrt(Vector3D.DistanceSquared(Me.GetPosition(), Pos))));
			bool KnownSignal = false;
			for(int i = 0;Signals.Count>i;i++){
				string[] nameSplit = Signals[i].Split('@');
				if(nameSplit[0]==recieveData[1]){
					KnownSignal = true;
					Signals[i] = recieveData[1]+"@ Position X:"+Math.Round(Convert.ToDouble(XYZ[0]))+": Y:"+Math.Round(Convert.ToDouble(XYZ[1]))+": Z:"+Math.Round(Convert.ToDouble(XYZ[2]))+":\n | Speed: "+recieveData[3]+"m/s,  Distance: "+distanceToTarget+"m, Time: "+dateTimeNow;
				}
			}
			if(KnownSignal == false){
				Signals.Add(recieveData[1]+"@ Position X:"+Math.Round(Convert.ToDouble(XYZ[0]))+": Y:"+Math.Round(Convert.ToDouble(XYZ[1]))+": Z:"+Math.Round(Convert.ToDouble(XYZ[2]))+":\n | Speed: "+recieveData[3]+"m/s,  Distance: "+distanceToTarget+"m, Time: "+dateTimeNow);
			}
			if(LCD_Signals!=null){
				string text = "";
				for(int i = 0;Signals.Count>i;i++){
					text = text + Signals[i]+"\n";
				}
				LCD_Signals.WritePublicText(text, false);    
				LCD_Signals.ShowPublicTextOnScreen();
				if(LCD_Signals2!=null){
					LCD_Signals2.WritePublicText(LCD_Signals.GetPublicText(), false);  
				}
			}
		break;
		case "control":
			if(recieveData[1]==Comm.CustomName){
				if(recieveData[3]=="Antenna"){
					switch (recieveData[4]){
						case "BroadcastON":
							Comm.ApplyAction("EnableBroadCast");
						break;
							
						case "IncreaseRadius":
							Comm.ApplyAction("IncreaseRadius");
						break;
							
						case "DecreaseRadius":
							Comm.ApplyAction("DecreaseRadius");
						break;

						default:
							
						break;
					}
				}else if(recieveData[3]=="Remote" && remoteblocks.Count>0){
					switch (recieveData[4]){
						case "Send":
							IMyRemoteControl remote;
							remote = remoteblocks[0] as IMyRemoteControl;
							Vector3D coords = new Vector3D(Convert.ToDouble(recieveData[5])+100,Convert.ToDouble(recieveData[6]),Convert.ToDouble(recieveData[7])); 
							remote.ClearWaypoints(); 
							remote.AddWaypoint(coords, "waypoint"); 
							remote.SetAutoPilotEnabled(true);
						break;
					
						default:
							
						break;
					}
				}else if (recieveData[3]=="WhatDoYouHave"){
					//string message = antennas.Count + ";"+laserantennas.Count + ";"+cameras.Count+";"+turrets.Count+";"+thrusters.Count+";"+lcdpanels.Count+";"+remoteblocks.Count+";"+Me.GetPosition().ToString();;
					//Comm.TransmitMessage("reply;"+recieveData[2]+";"+Comm.CustomName+";"+message, getTarget(3));
					Comm.TransmitMessage("reply;"+recieveData[2]+";"+Comm.CustomName+";"+LCD_Main_Current_text, getTarget(3));
				}
			}
		break;
		case "reply":
			if(recieveData[1]==Comm.CustomName){
				//lastReply = recieveData[2]+";"+recieveData[3]+";"+recieveData[4]+";"+recieveData[5]+";"+recieveData[6]+";"+recieveData[7]+";"+recieveData[8]+";"+recieveData[9]+";"+recieveData[10];
				//menu.Add( new List<string>(new string[] {"RemoteSetting","Communications","Name :"+recieveData[2]+":","Antenna :"+recieveData[3]+":","Laser Antenna :"+recieveData[4]+":","Camera :"+recieveData[5]+":","Turrets :"+recieveData[6]+":","Thrusters :"+recieveData[7]+":","LCD Panels :"+recieveData[8]+":","Remote Blocks:"+recieveData[9]+":","GPS:["+recieveData[10]+"]"}));
				currentMenu = "RemoteSetting";
				RemoteShip = recieveData[2];
				//modifingSelectedBlockName = "2-Way Comm Link Established"+recieveData[2]+"/n";
				if(LCD_Main != null){
					LCD_Main.WritePublicText("2-Way Comm Link Established\n"+recieveData[2]+"\n"+recieveData[3], false);    
					LCD_Main.ShowPublicTextOnScreen();
					if(LCD_Main2 != null){
						LCD_Main2.WritePublicText(LCD_Main.GetPublicText(), false);    
						LCD_Main2.ShowPublicTextOnScreen();
					}
				}else{
					Echo("Error: LCD_Main is null.\nShip Needs an LCD");
				}
			}
		break;
		case "raycast":			
			if(TargetingCamera != null){
				TargetingCamera.EnableRaycast = true;
				MyDetectedEntityInfo targetInfo; 
				targetInfo = TargetingCamera.Raycast(TrageterMaxDistance);
				Vector3D targetPos;
				string text = "";
				string colorDot2 = "\uE002";
				if (targetInfo.Type != 0){
					float distanceToTarget;
					targetPos = new Vector3D(Math.Round(targetInfo.HitPosition.Value.X,1),Math.Round(targetInfo.HitPosition.Value.Y,1),Math.Round(targetInfo.HitPosition.Value.Z,1));
					distanceToTarget = Convert.ToSingle(Math.Round(Math.Sqrt(Vector3D.DistanceSquared(Me.GetPosition(), targetInfo.Position))));
					//Comm.TransmitMessage("scan;"+targetInfo.Name.ToString()+";"+targetPos.X+","+targetPos.Y+","+targetPos.Z+";"+get_speed()+";"+targetInfo.Relationship.ToString()+";"+Comm.CustomName, getTarget(3));
					Echo("scan;"+targetInfo.Name.ToString()+";"+targetPos.X+","+targetPos.Y+","+targetPos.Z+";"+get_speed()+";"+targetInfo.Relationship.ToString());
					distanceToTarget = Convert.ToSingle(Math.Round(Math.Sqrt(Vector3D.DistanceSquared(Me.GetPosition(), targetPos))));
					
					switch (targetInfo.Relationship.ToString()){
						case "Ally":
							colorDot2 = "\uE001";
							break;
							
						case "Neutral":
							colorDot2 = "\uE004";
							break;
							
						case "Owner":
							colorDot2 = "\uE001";
							break;
					
						case "Enemies":
							colorDot2 = "\uE003";
							break;
						
						case "FactionShare":
							colorDot2 = "\uE001";
							break;
					
						default:
							colorDot2 = targetInfo.Relationship.ToString();
							break;
					}
					
					string targetName = targetInfo.Name.ToString();
					for(int i = 0;Scans.Count>i;i++){
						string[] Scan_arr = Scans[i].Split(':');
						for(int j = 1;Scan_arr[3]==targetName||Scan_arr[3]==targetName+Convert.ToString(j);j++){
							targetName = targetName+Convert.ToString(j);
						}
						text = text + Scans[i]+"\n";
					}
					ScanGPS.Add("GPS:"+targetName+":"+targetPos.X.ToString()+":"+targetPos.Y.ToString()+":"+targetPos.Z.ToString()+":\n");
					Scans.Add(Comm.CustomName+":SCAN:"+colorDot2+":"+targetName+":"+distanceToTarget+":X:"+Math.Round(targetPos.X)+":Y:"+Math.Round(targetPos.Y)+":Z:"+Math.Round(targetPos.Z));
					
					if(LCD_Raycast_Targets != null){
						for(int i = 0;ScanGPS.Count>i;i++){
							LCD_Raycast_Targets.CustomData = LCD_Raycast_Targets.CustomData + ScanGPS[i]+"\n";
						}
						LCD_Raycast_Targets.WritePublicText(text, false);    
						LCD_Raycast_Targets.ShowPublicTextOnScreen();	
					}
					if(LCD_Raycast_Targets2 != null){
						for(int i = 0;ScanGPS.Count>i;i++){
							LCD_Raycast_Targets2.CustomData = LCD_Raycast_Targets2.CustomData + ScanGPS[i];	
						}
						LCD_Raycast_Targets2.WritePublicText(text, false);    
						LCD_Raycast_Targets2.ShowPublicTextOnScreen();
					}
				}
			} else {
				if(LCD_Raycast_Targets != null){
					LCD_Raycast_Targets.WritePublicText("Targeting Camera Missing.", false);    
					LCD_Raycast_Targets.ShowPublicTextOnScreen();
				}
			} 
		break;
		case "previous":	
			setupMessage = false;
			RemoteAccess = true;
			if(recieveData.Length > 2 && Comm != null){
				if(recieveData[1]==Comm.CustomName){
					RemoteAccess = true;
				}else {
					RemoteAccess = false;
				}
			}
			if(currentMenu=="RemoteSetting"){
				cancelCount = 0;
				Comm.TransmitMessage("previous;"+RemoteShip+";"+Comm.CustomName, getTarget(3));
			}else{
				if(RemoteAccess){
					RemoteShip = null;
					if(currentMenuItem>1){
						currentMenuItem = currentMenuItem-1;
					}
					if(LCD_Main != null){
						LCD_Main.WritePublicText(outputMenuString(currentMenu,currentMenuItem,modifingSelectedBlockName), false);    
						LCD_Main.ShowPublicTextOnScreen();
						if(LCD_Main2 != null){
							LCD_Main2.WritePublicText(LCD_Main.GetPublicText(), false);    
							LCD_Main2.ShowPublicTextOnScreen();
						}
					}else{
						Echo("Error: LCD_Main is null.\nShip Needs an LCD");
					}
					LCD_Main_Current_text = outputMenuString(currentMenu,currentMenuItem,modifingSelectedBlockName);
					if(Comm != null && recieveData.Length > 1){
						if(recieveData[1]==Comm.CustomName){
							Comm.TransmitMessage("reply;"+recieveData[2]+";"+Comm.CustomName+";"+LCD_Main_Current_text, getTarget(3));
						}
					}
				}
			}
		break;
		
		case "next":
			setupMessage = false;
			RemoteAccess = true;
			if(recieveData.Length > 2 && Comm != null){
				if(recieveData[1]==Comm.CustomName){
					RemoteAccess = true;
					insertbefore = "Controlled by "+recieveData[1]+"\n";
				}else {
					RemoteAccess = false;
				}
			}
			if(currentMenu=="RemoteSetting"){
				cancelCount = 0;
				Comm.TransmitMessage("next;"+RemoteShip+";"+Comm.CustomName, getTarget(3));
			}else{
				if(RemoteAccess){
					RemoteShip = null;
					for (int x = 0; x < menu.Count; x++)
					{	
						List<string> innerList = menu[x];
						if(innerList[0]==currentMenu){
							if(currentMenuItem<innerList.Count-2){
								currentMenuItem = currentMenuItem+1;
							}
						}
					}
					if(LCD_Main != null){
						LCD_Main.WritePublicText(outputMenuString(currentMenu,currentMenuItem,modifingSelectedBlockName), false);    
						LCD_Main.ShowPublicTextOnScreen();
						if(LCD_Main2 != null){
							LCD_Main2.WritePublicText(LCD_Main.GetPublicText(), false);    
							LCD_Main2.ShowPublicTextOnScreen();
						}
					}else{
						Echo("Error: LCD_Main is null.\nShip Needs an LCD");
					}
					LCD_Main_Current_text = outputMenuString(currentMenu,currentMenuItem,modifingSelectedBlockName);
					if(Comm != null && recieveData.Length > 1){
						if(recieveData[1]==Comm.CustomName){
							Comm.TransmitMessage("reply;"+recieveData[2]+";"+Comm.CustomName+";"+LCD_Main_Current_text, getTarget(3));
						}
					}
				}
			}
			
		break;
		
		case "select":
			setupMessage = false;
			RemoteAccess = true;
			if(recieveData.Length > 2 && Comm != null){
				if(recieveData[1]==Comm.CustomName){
					RemoteAccess = true;
					insertbefore = "Controlled by "+recieveData[1]+"\n";
				}else {
					RemoteAccess = false;
				}
			}
			if(currentMenu=="RemoteSetting"){
				cancelCount = 0;
				Comm.TransmitMessage("select;"+RemoteShip+";"+Comm.CustomName, getTarget(3));
			}else{
				if(RemoteAccess){
					RemoteShip = null;
					string tempcurrentMenu="Main";
					foreach (var sublist in menu){ 
						if(sublist[0]==currentMenu){
							//debug = debug + sublist[0]+"=="+currentMenu+"\n";
							foreach (string value in sublist){
								if (sublist.IndexOf(value)==currentMenuItem+1){
									//debug = "\uE001\nsublist.IndexOf(value)= " + sublist.IndexOf(value);
									tempcurrentMenu = value;
								}
							}
						}	
					}
					string[] tempcurrentMenu_arr = tempcurrentMenu.Split(':');
					string info = "";
					if(tempcurrentMenu_arr.Length>1){	
						if( tempcurrentMenu_arr[0]=="LCD"){
							currentMenu = "LCD:Settings";
							selectedBlockType = tempcurrentMenu_arr[0];
							selectedBlockName = tempcurrentMenu_arr[1];
							SelectedLCD = GridTerminalSystem.GetBlockWithName(selectedBlockName) as IMyTextPanel;
							currentMenuItem = 1;
						}
						if( tempcurrentMenu_arr[0]=="THR"){
							selectedBlockType = tempcurrentMenu_arr[0];
							selectedBlockName = tempcurrentMenu_arr[2];
							SelectedThruster = GridTerminalSystem.GetBlockWithName(selectedBlockName) as IMyThrust;
							currentMenuItem = 1;
						}
						if( tempcurrentMenu_arr[0]=="ANT"){
							currentMenu = "ANT:Settings";
							selectedBlockType = tempcurrentMenu_arr[0];
							selectedBlockName = tempcurrentMenu_arr[3];
							SelectedAntenna = GridTerminalSystem.GetBlockWithName(selectedBlockName) as IMyRadioAntenna;
							info = "\nP["+SelectedAntenna.IsWorking+"] B["+SelectedAntenna.IsBroadcasting+"] R["+Math.Floor(SelectedAntenna.Radius)+"]";
							currentMenuItem = 1;
						}
						if( tempcurrentMenu_arr[0]=="TUR"){
							currentMenu = "TUR:Settings";
							selectedBlockType = tempcurrentMenu_arr[0];
							selectedBlockName = tempcurrentMenu_arr[3];
							SelectedTurret = GridTerminalSystem.GetBlockWithName(selectedBlockName) as IMyLargeTurretBase;
							info = "\nP["+SelectedTurret.IsWorking+"] R["+Math.Floor(SelectedTurret.Range)+"]";
							currentMenuItem = 1;
						}
						if( tempcurrentMenu_arr[0]=="CGO"){
							currentMenu = "CGO:Settings";
							selectedBlockType = tempcurrentMenu_arr[0];
							selectedBlockName = tempcurrentMenu_arr[1];
							SelectedCargo = GridTerminalSystem.GetBlockWithName(selectedBlockName) as IMyCargoContainer;
							currentMenuItem = 1;
						}
						if( tempcurrentMenu_arr[0]=="REF"){
							currentMenu = "REF:Settings";
							selectedBlockType = tempcurrentMenu_arr[0];
							selectedBlockName = tempcurrentMenu_arr[2];
							SelectedRefinery = GridTerminalSystem.GetBlockWithName(selectedBlockName) as IMyRefinery;
							currentMenuItem = 1;
						}
						if( tempcurrentMenu_arr[0]=="ASM"){
							currentMenu = "ASM:Settings";
							selectedBlockType = tempcurrentMenu_arr[0];
							selectedBlockName = tempcurrentMenu_arr[2];
							SelectedAssembler = GridTerminalSystem.GetBlockWithName(selectedBlockName) as IMyAssembler;
							currentMenuItem = 1;
						}
						if( tempcurrentMenu_arr[0]=="CAM"){
							currentMenu = "CAM:Settings";
							selectedBlockType = tempcurrentMenu_arr[0];
							selectedBlockName = tempcurrentMenu_arr[2];
							SelectedCamera = GridTerminalSystem.GetBlockWithName(selectedBlockName) as IMyCameraBlock;
							currentMenuItem = 1;
						}
						if( tempcurrentMenu_arr[0]=="RMC"){
							currentMenu = "RMC:Settings";
							selectedBlockType = tempcurrentMenu_arr[0];
							selectedBlockName = tempcurrentMenu_arr[1];
							SelectedRemote = GridTerminalSystem.GetBlockWithName(selectedBlockName) as IMyRemoteControl;
							currentMenuItem = 1;
						}
						if( tempcurrentMenu_arr[0]=="COMM"){
							selectedSignal = tempcurrentMenu_arr[1];
							currentMenuItem = 1;
						}
						if( tempcurrentMenu_arr[0]=="SCAN"){
							//RaycastList.Add("SCAN:"+dataSplit[2]+":"+dataSplit[3]+": Distance: "+dataSplit[3]+"\n---->: X:"+dataSplit[5]+": Y:"+dataSplit[7]+": Z:"+dataSplit[9]);
							Vector3D coords = new Vector3D(Convert.ToDouble(tempcurrentMenu_arr[6]),Convert.ToDouble(tempcurrentMenu_arr[8]),Convert.ToDouble(tempcurrentMenu_arr[10])); 
							shipRemote.ClearWaypoints(); 
							shipRemote.AddWaypoint(coords, tempcurrentMenu_arr[1]); 
							shipRemote.SetValue<bool>("CollisionAvoidance", true);
							shipRemote.SetValue<long>("FlightMode", 2);
							shipRemote.SetAutoPilotEnabled(true);	
						}
						if( tempcurrentMenu_arr[0]=="[AutoPilot to "){
							if(shipRemote!=null){
								selectedBlockType = "SGNL";
								string[] statSplit = tempcurrentMenu.Split(',');
								Vector3D coords = new Vector3D(Convert.ToDouble(statSplit[3]),Convert.ToDouble(statSplit[4]),Convert.ToDouble(statSplit[5])); 
								shipRemote.ClearWaypoints(); 
								shipRemote.AddWaypoint(coords, statSplit[1]); 
								shipRemote.SetValue<bool>("CollisionAvoidance", true);
								shipRemote.SetValue<long>("FlightMode", 2);
								shipRemote.SetAutoPilotEnabled(true);
							}
							MenuSetup = true;
						}
						if( tempcurrentMenu_arr[0]=="AutoPilot"){
							if(shipRemote!=null){
								selectedBlockType = "SGNL";
								if(shipRemote.GetValue<bool>("AutoPilot")){
									shipRemote.SetAutoPilotEnabled(false);
								}else{
									shipRemote.SetAutoPilotEnabled(true);
								}
							}
							MenuSetup = true;
						}
					}
					if(tempcurrentMenu=="Clear Logs/Raycasts"){
						MenuSetup = true;
						if(LCD_Raycast_Targets != null){
							LCD_Raycast_Targets.CustomData = "";
							LCD_Raycast_Targets.WritePublicText("", false);    
							LCD_Raycast_Targets.ShowPublicTextOnScreen();
						}
						if(LCD_Signals != null){
							LCD_Signals.CustomData = "";
							LCD_Signals.WritePublicText("", false);    
							LCD_Signals.ShowPublicTextOnScreen();
						}
						if(LCD_Targets_Received != null){
							LCD_Targets_Received.CustomData = "";
							LCD_Targets_Received.WritePublicText("", false);    
							LCD_Targets_Received.ShowPublicTextOnScreen();
						}
						if(LCD_Main != null){
							LCD_Main.CustomData = "";
						}
						Signals.Clear();
						Scans.Clear();
						ScanGPS.Clear();
						Me.CustomData="-log cleared "+dateTimeNow+"-\n";
						
					}else if(tempcurrentMenu=="ANT Range"){
						currentMenu = "ANT Range";
						info = "\nP["+SelectedAntenna.IsWorking+"] B["+SelectedAntenna.IsBroadcasting+"] R["+Math.Floor(SelectedAntenna.Radius)+"]";
					}else if(tempcurrentMenu=="[Increase ANT Range]"){
						SelectedAntenna.ApplyAction("IncreaseRadius");
						currentMenu = "ANT Range";
						MenuSetup = true;
						info = "\nP["+SelectedAntenna.IsWorking+"] B["+SelectedAntenna.IsBroadcasting+"] R["+Math.Floor(SelectedAntenna.Radius)+"]";
					}else if(tempcurrentMenu=="[Decrease ANT Range]"){
						SelectedAntenna.ApplyAction("DecreaseRadius");
						currentMenu = "ANT Range";
						MenuSetup = true;
						info = "\nP["+SelectedAntenna.IsWorking+"] B["+SelectedAntenna.IsBroadcasting+"] R["+Math.Floor(SelectedAntenna.Radius)+"]";
					}else if(tempcurrentMenu=="[Turn Off All Turrets]"){
						for(int i = 0; i < turrets.Count; i++){  
							turrets[i].ApplyAction("OnOff_Off");
						}
						MenuSetup = true;
					}else if(tempcurrentMenu=="[Turn On  All Turrets]"){
						for(int i = 0; i < turrets.Count; i++){  
							turrets[i].ApplyAction("OnOff_On");
						}
						MenuSetup = true;
					}else if(tempcurrentMenu=="[Set All Turrets To 800m]"){
						for(int i = 0; i < turrets.Count; i++){  
							IMyLargeTurretBase tempTurret = turrets[i] as IMyLargeTurretBase;
							tempTurret.SetValue<Single>("Range",800f);
						}
						MenuSetup = true;
					}else if(tempcurrentMenu=="[Turn Off All Thrusters]"){
						for(int i = 0; i < thrusters.Count; i++){  
							thrusters[i].ApplyAction("OnOff_Off");
						}
						MenuSetup = true;
					}else if (tempcurrentMenu=="[Turn On All Thrusters]"){
						for(int i = 0; i < thrusters.Count; i++){  
							thrusters[i].ApplyAction("OnOff_On");
						}
						MenuSetup = true;
					}else if (tempcurrentMenu=="[Toggle Hydrogen Thrusters]"){
						for(int i = 0; i < thrusters.Count; i++){  
							if(thrusters[i].CustomName.Contains("Hydrogen")){
								if(thrusters[i].IsWorking){
									thrusters[i].ApplyAction("OnOff_Off");
								}else{
									thrusters[i].ApplyAction("OnOff_On");
								}
							}
						}
						MenuSetup = true;
					}else if (tempcurrentMenu=="[Remove All Thrust Overrides]"){
						for(int i = 0; i < thrusters.Count; i++){  
							// not working, a bug perhaps?
							thrusters[i].SetValueFloat("Override", 0f);
						}
					}else if (tempcurrentMenu=="[Engage Radio Silence]"){
						MenuSetup = true;
						for(int i = 0; i < antennas.Count; i++){  
							antennas[i].ApplyAction("OnOff_Off");
						}
					}else if(tempcurrentMenu=="[Set All Antennas to 800m]"){
						for(int i = 0; i < antennas.Count; i++){  
							IMyRadioAntenna tempant = antennas[i] as IMyRadioAntenna;
							tempant.SetValue<Single>("Radius",800f);
						}
						MenuSetup = true;
					}else if (tempcurrentMenu=="[Connect]"){
						if(selectedSignal!= null){
							Comm.TransmitMessage("control;"+selectedSignal+";"+Comm.CustomName+";WhatDoYouHave", getTarget(3));
							if(LCD_Main != null){
							LCD_Main.WritePublicText(LCD_Main.GetPublicText()+"   \nWaiting For Reply...", false);    
							LCD_Main.ShowPublicTextOnScreen();
							}
							if(LCD_Main2 != null){
								LCD_Main2.WritePublicText(LCD_Main.GetPublicText(), false);    
								LCD_Main2.ShowPublicTextOnScreen();
							}
						}else{
							Echo("selectedSignal = null");
						}	
					}else if(selectedBlockType != null){
						switch (selectedBlockType)// block controls
						{
							case "LCD":
								if(tempcurrentMenu=="Power"){
									if(SelectedLCD.IsWorking){
										SelectedLCD.ApplyAction("OnOff_Off");
									}else{
										SelectedLCD.ApplyAction("OnOff_On");
									}
								}
								if(tempcurrentMenu=="Add 2 to end of name"){
									SelectedLCD.CustomName = SelectedLCD.CustomName+"2";
									MenuSetup = true;
								}
								if(tempcurrentMenu_arr.Length>1){
									if(tempcurrentMenu_arr[0]=="Change Name To"){
										for(int i = 0; i < lcdpanels.Count; i++){  
											if(lcdpanels[i].CustomName == tempcurrentMenu_arr[1]){
												lcdpanels[i].CustomName = lcdpanels[i].CustomName+"OLD";
											}
										}
										SelectedLCD.CustomName = tempcurrentMenu_arr[1];
										MenuSetup = true;
									}
								}
							break;
							case "TUR":
								if(SelectedTurret!=null){
									if(tempcurrentMenu=="["+selectedBlockType+" Actions]"){
										currentMenu = tempcurrentMenu;
										currentMenuItem = 1;
									}else if(tempcurrentMenu_arr[0]=="S"){
										
									}else if(tempcurrentMenu_arr[0]=="I"){
										
									}else if(tempcurrentMenu_arr[0]=="F"){
										
									}else if(tempcurrentMenu_arr[0]=="["+selectedBlockType+" Cargo]"){
										currentMenu = tempcurrentMenu;
										currentMenuItem = 1;
									}else if(tempcurrentMenu_arr[0]=="A"){
										SelectedTurret.ApplyAction(tempcurrentMenu_arr[2]);
										blockActionUpdate = true;
										menu.Clear();
										MenuSetup = true;
										AddBlockActions(SelectedTurret, selectedBlockType,"Turret Control",null,true);
									}else{
										AddBlockActions(SelectedTurret, selectedBlockType,"Turret Control",null,true);
									}
								}	
							break;
							case "THR":
								if(SelectedThruster != null){
									if(SelectedThruster.IsWorking){
										SelectedThruster.ApplyAction("OnOff_Off");
									}else{
										SelectedThruster.ApplyAction("OnOff_On");
									}
									selectedBlockType = null;
									SelectedThruster = null;
									modifingSelectedBlockName = null;
									selectedBlockName = null;
								}
								MenuSetup = true;
							break;
							case "ANT":
								if(tempcurrentMenu=="Power"){
									if(SelectedAntenna.IsWorking){
										SelectedAntenna.ApplyAction("OnOff_Off");
									}else{
										SelectedAntenna.ApplyAction("OnOff_On");
									}
								}
								if(tempcurrentMenu=="Broadcast"){
									if(SelectedAntenna.IsBroadcasting){
										SelectedAntenna.EnableBroadcasting = false;//DisableBroadcasting 
									}else{
										SelectedAntenna.EnableBroadcasting = true;//EnableBroadcasting 
									}
								}
								if(tempcurrentMenu_arr.Length>1){
									if(tempcurrentMenu_arr[0]=="Change Name To "){
										SelectedAntenna.CustomName = tempcurrentMenu_arr[1];
										MenuSetup = true;
									}
								}
								info = "\nP["+SelectedAntenna.IsWorking+"] B["+SelectedAntenna.IsBroadcasting+"] R["+Math.Floor(SelectedAntenna.Radius)+"]";
								MenuSetup = true;
								
							break;
							case "CGO":
								if(SelectedCargo!=null){
									if(tempcurrentMenu=="["+selectedBlockType+" Actions]"){
										currentMenu = tempcurrentMenu;
										currentMenuItem = 1;
									}else if(tempcurrentMenu_arr[0]=="S"){
										
									}else if(tempcurrentMenu_arr[0]=="I"){
										
									}else if(tempcurrentMenu_arr[0]=="F"){
										
									}else if(tempcurrentMenu_arr[0]=="["+selectedBlockType+" Cargo]"){
										currentMenu = tempcurrentMenu;
										currentMenuItem = 1;
									}else if(tempcurrentMenu_arr[0]=="A"){
										SelectedCargo.ApplyAction(tempcurrentMenu_arr[2]);
										blockActionUpdate = true;
										menu.Clear();
										MenuSetup = true;
										AddBlockActions(SelectedCargo, selectedBlockType,"Cargos",null,true);
									}else{
										AddBlockActions(SelectedCargo, selectedBlockType,"Cargos",null,true);
									}
								}

							break;
							case "REF":
								if(SelectedRefinery!=null){
									if(tempcurrentMenu=="["+selectedBlockType+" Actions]"){
										currentMenu = tempcurrentMenu;
										currentMenuItem = 1;
									}else if(tempcurrentMenu_arr[0]=="S"){
										
									}else if(tempcurrentMenu_arr[0]=="I"){
										
									}else if(tempcurrentMenu_arr[0]=="F"){
										
									}else if(tempcurrentMenu_arr[0]=="["+selectedBlockType+" Cargo]"){
										currentMenu = tempcurrentMenu;
										currentMenuItem = 1;
									}else if(tempcurrentMenu_arr[0]=="A"){
										SelectedRefinery.ApplyAction(tempcurrentMenu_arr[2]);
										blockActionUpdate = true;
										menu.Clear();
										MenuSetup = true;
										AddBlockActions(SelectedRefinery, selectedBlockType,"Cargos",null,true);
									}else{
										AddBlockActions(SelectedRefinery, selectedBlockType,"Cargos",null,true);
									}
								}
								
							break;
							case "ASM":
								if(SelectedAssembler!=null){
									if(tempcurrentMenu=="["+selectedBlockType+" Actions]"){
										currentMenu = tempcurrentMenu;
										currentMenuItem = 1;
									}else if(tempcurrentMenu_arr[0]=="S"){
										
									}else if(tempcurrentMenu_arr[0]=="I"){
										
									}else if(tempcurrentMenu_arr[0]=="F"){
										
									}else if(tempcurrentMenu_arr[0]=="["+selectedBlockType+" Cargo]"){
										currentMenu = tempcurrentMenu;
										currentMenuItem = 1;
									}else if(tempcurrentMenu_arr[0]=="A"){
										SelectedAssembler.ApplyAction(tempcurrentMenu_arr[2]);
										blockActionUpdate = true;
										menu.Clear();
										MenuSetup = true;
										AddBlockActions(SelectedAssembler, selectedBlockType,"Cargos",null,true);
									}else{
										AddBlockActions(SelectedAssembler, selectedBlockType,"Cargos",null,true);
									}
								}
							break;
							case "CAM":
								if(SelectedCamera!=null){
									string additionalMenus = "Change Name To :"+Targeting_Camera_Name;
									if(tempcurrentMenu=="["+selectedBlockType+" Actions]"){
										currentMenu = tempcurrentMenu;
										currentMenuItem = 1;
									}else if(tempcurrentMenu_arr[0]=="S"){
										
									}else if(tempcurrentMenu_arr[0]=="I"){
										
									}else if(tempcurrentMenu_arr[0]=="F"){
										
									}else if(tempcurrentMenu_arr[0]=="A"){
										SelectedCamera.ApplyAction(tempcurrentMenu_arr[2]);
										blockActionUpdate = true;
										menu.Clear();
										MenuSetup = true;
										AddBlockActions(SelectedCamera, selectedBlockType,"Cameras",additionalMenus);
									}else{
										AddBlockActions(SelectedCamera, selectedBlockType,"Cameras",additionalMenus);
									}
									if(tempcurrentMenu_arr[0]=="Change Name To"){
										for(int i = 0; i < cameras.Count; i++){  
											if(cameras[i].CustomName == tempcurrentMenu_arr[1]){
												cameras[i].CustomName = cameras[i].CustomName+"OLD";
											}
										}
										SelectedCamera.CustomName = tempcurrentMenu_arr[1];
										MenuSetup = true;
									}
								
								}	
								
							break;
							case "RMC":
								if(SelectedRemote!=null){
									if(tempcurrentMenu=="[RMC Actions]"){
										currentMenu = tempcurrentMenu;
									}else if(tempcurrentMenu_arr[0]=="S"){
										
									}else if(tempcurrentMenu_arr[0]=="I"){
										
									}else if(tempcurrentMenu_arr[0]=="F"){
										
									}else if(tempcurrentMenu_arr[0]=="A"){
										SelectedRemote.ApplyAction(tempcurrentMenu_arr[2]);
										blockActionUpdate = true;
										menu.Clear();
										MenuSetup = true;
										AddBlockActions(SelectedRemote, "RMC","Remote Control");
									} else {
										AddBlockActions(SelectedRemote, "RMC","Remote Control");
									}
									
								}	
							break;
						}
					}else{
						currentMenu = tempcurrentMenu;
						currentMenuItem = 1;
					}
					
					
					if(selectedBlockName != null){
						modifingSelectedBlockName = "\nModifiying["+selectedBlockName+"]"+info;
					}
					if(LCD_Main != null){
						LCD_Main.WritePublicText(outputMenuString(currentMenu,currentMenuItem,modifingSelectedBlockName), false);    
						LCD_Main.ShowPublicTextOnScreen();
						if(LCD_Main2 != null){
							LCD_Main2.WritePublicText(LCD_Main.GetPublicText(), false);    
							LCD_Main2.ShowPublicTextOnScreen();
						}
					}else{
						Echo("Error: LCD_Main is null.\nShip Needs an LCD");
					}
					LCD_Main_Current_text = outputMenuString(currentMenu,currentMenuItem,modifingSelectedBlockName);
					if(Comm != null && recieveData.Length > 1){
						if(recieveData[1]==Comm.CustomName){
							Comm.TransmitMessage("reply;"+recieveData[2]+";"+Comm.CustomName+";"+LCD_Main_Current_text, getTarget(3));
						}
					}
				}
			}	
		break;
			
		case "cancel":	
			RemoteAccess = true;
			setupMessage = false;
			if(recieveData.Length > 2 && Comm != null){
				if(recieveData[1]==Comm.CustomName){
					RemoteAccess = true;
					insertbefore = "Controlled by "+recieveData[1]+"\n";
				}else {
					RemoteAccess = false;
				}
			}
			if(currentMenu=="RemoteSetting"){
				cancelCount++;
				if(cancelCount>3){
					currentMenu="Communications";
					cancelCount = 0;
					if(LCD_Main != null){
						
						LCD_Main.WritePublicText(debug+outputMenuString(currentMenu,currentMenuItem,modifingSelectedBlockName), false);    
						LCD_Main.ShowPublicTextOnScreen();
						if(LCD_Main2 != null){
							LCD_Main2.WritePublicText(LCD_Main.GetPublicText(), false);    
							LCD_Main2.ShowPublicTextOnScreen();
						}
					}
				}else{
					Comm.TransmitMessage("cancel;"+RemoteShip+";"+Comm.CustomName, getTarget(3));
				}
			}else{
				if(RemoteAccess){
					currentMenuItem = 1;
					bool previousMenu = false;
					foreach (var sublist in menu){ 
						//debug = debug+"\nsublist[0]==currentMenu "+ sublist[0] + "   " + currentMenu;
						if(sublist[0]==currentMenu){
							currentMenu=sublist[1];
							//debug = debug+"\uE001";
							previousMenu = true;
						}
						
					}
					if(!previousMenu){
						currentMenu="Main";
					}
					if(currentMenu == "Main" || currentMenu == "Ship Status" || currentMenu=="[Connect]" || currentMenu=="Cargos" || currentMenu=="Turret Control" ){
						selectedBlockName = null;
						selectedBlockType = null;
						modifingSelectedBlockName = null;
						lastReply = null;
						MenuSetup = true;
					}
					if(LCD_Main != null){
						LCD_Main.WritePublicText(debug+outputMenuString(currentMenu,currentMenuItem,modifingSelectedBlockName), false);    
						LCD_Main.ShowPublicTextOnScreen();
						if(LCD_Main2 != null){
							LCD_Main2.WritePublicText(LCD_Main.GetPublicText(), false);    
							LCD_Main2.ShowPublicTextOnScreen();
						}
					}else{
						Echo("Error: LCD_Main is null.\nShip Needs an LCD");
					}
					LCD_Main_Current_text = outputMenuString(currentMenu,currentMenuItem,modifingSelectedBlockName);
					if(Comm != null && recieveData.Length > 1){
						if(recieveData[1]==Comm.CustomName){
							Comm.TransmitMessage("reply;"+recieveData[2]+";"+Comm.CustomName+";"+LCD_Main_Current_text, getTarget(3));
						}
					}
				}
			}
		break;
		
		default:
			Vector3D current_position = Me.GetPosition();
			if(Comm != null){
				Comm.TransmitMessage("ping;"+Comm.CustomName+";"+current_position.X+","+current_position.Y+","+current_position.Z+";"+get_speed(), getTarget(3));
			}else{
				Echo("Error: Comm is null.\nShip Needs an Antenna");
			}
			if(LCD_Main != null){
				if(setupMessage){
					debug = "----First Run Detected----\n"+
							"You Must now setup your cockpit\n"+
							"toolbar. Enter the cockpit,\n"+
							"press G, Then drag PB to toolbar,\n"+
							"enter the following arguments:\n"+
							"slot 1: previous\n"+
							"slot 2: next\n"+
							"slot 3: select\n"+
							"slot 4: cancel\n"+
							"slot 5: raycast\n";
				}
				LCD_Main.WritePublicText(debug+outputMenuString(currentMenu,currentMenuItem,modifingSelectedBlockName), false);
				LCD_Main.ShowPublicTextOnScreen();
				if(LCD_Main2 != null){
					LCD_Main2.WritePublicText(LCD_Main.GetPublicText(), false);    
					LCD_Main2.ShowPublicTextOnScreen();
				}
			}else{
				Echo("Error: LCD_Main is null.\nShip Needs an LCD");
			}
		break;
	}
	LCD_Main_Current_text=outputMenuString(currentMenu,currentMenuItem,modifingSelectedBlockName);
}
