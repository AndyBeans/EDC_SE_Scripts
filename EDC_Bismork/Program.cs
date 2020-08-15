using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        MyCommandLine _commandLine = new MyCommandLine();
        Dictionary<string, Action> _commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
        private readonly float repairWidth = 25.0f, repairHeight = 85.0f, repairDepth = 35.0f, repairMax = 150.0f;
        private readonly string repairGroup = "[Bismork] Repair";

        public Program()
        {
            _commands["ToggleRepairZone"] = ToggleRepairZone;
            _commands["TestActions"] = TestActions;
            _commands["ListInventory"] = ListInventory;
        }

        public void Save()
        {
            // To Do
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // MDK command parsing
            if (_commandLine.TryParse(argument))
            {
                Action commandAction;

                // Retrieve the first argument. Switches are ignored.
                string command = _commandLine.Argument(0);

                // Now we must validate that the first argument is actually specified, 
                // then attempt to find the matching command delegate.
                if (command == null)
                {
                    Echo("No command specified");
                }
                else if (_commands.TryGetValue(_commandLine.Argument(0), out commandAction))
                {
                    // We have found a command. Invoke it.
                    commandAction();
                }
                else
                {
                    Echo($"Unknown command {command}");
                }
            }
        }

        private void ListInventory()
        {
            List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blockList, block => block.CubeGrid == Me.CubeGrid && block.InventoryCount > 0);

            foreach (IMyTerminalBlock block in blockList)
            {
                //Echo($"{block.DefinitionDisplayNameText}:{block.DisplayNameText}\n");
                /*
                List<MyInventoryItem> componentList = new List<MyInventoryItem>();
                block.GetInventory(0).GetItems(componentList, item => item.Type.TypeId == "MyObjectBuilder_Component");
                foreach (MyInventoryItem item in componentList)
                {
                    AddComponentCount(item);
                }
                if (block.InventoryCount > 1)
                {
                    componentList = new List<MyInventoryItem>();
                    block.GetInventory(1).GetItems(componentList, item => item.Type.TypeId == "MyObjectBuilder_Component");
                    foreach (MyInventoryItem item in componentList)
                    {
                        AddComponentCount(item);
                    }
                }
                */
            }

            List<IMyUserControllableGun> gunList = new List<IMyUserControllableGun>();
            GridTerminalSystem.GetBlocksOfType<IMyUserControllableGun>(gunList, gun => gun.CubeGrid == Me.CubeGrid);

            gunList.Sort((x, y) => string.Compare(x.DisplayNameText, y.DisplayNameText));

            foreach (IMyUserControllableGun gun in gunList)
            {
                string gunType = gun.DefinitionDisplayNameText.Replace(" Turret", "").Replace(" Launcher", "");
                string gunPosition = gun.DisplayNameText.Replace($"[{Me.CubeGrid.CustomName}] {gun.DefinitionDisplayNameText} ", "");
                Echo($"{gunType}: {gunPosition}");
            }

        }

        private void TestActions()
        {
            IMyProgrammableBlock progBlock = Me;
            
            List<ITerminalAction> actions = new List<ITerminalAction>();
            progBlock.GetActions(actions, action => true);

            foreach (ITerminalAction action in actions)
            {
                Echo($"{action.Id} {action.Icon} {action.Name}");                
            }
        }

        private void ToggleRepairZone()
        {
            List<IMyShipWelder> repairList = new List<IMyShipWelder>();
            float maxArea = repairMax * repairMax * repairMax;
            
            GridTerminalSystem.GetBlockGroupWithName(repairGroup).GetBlocksOfType<IMyShipWelder>(repairList, repairUnit => true);

            float repairArea = repairList[0].GetValueFloat("BuildAndRepair.AreaWidth")
                                * repairList[0].GetValueFloat("BuildAndRepair.AreaHeight")
                                * repairList[0].GetValueFloat("BuildAndRepair.AreaDepth");

            foreach (IMyShipWelder repairUnit in repairList)
            {
                if (repairArea >= maxArea)
                {
                    repairUnit.SetValueFloat("BuildAndRepair.AreaWidth", repairWidth);
                    repairUnit.SetValueFloat("BuildAndRepair.AreaHeight", repairHeight);
                    repairUnit.SetValueFloat("BuildAndRepair.AreaDepth", repairDepth);
                }
                else
                {
                    repairUnit.SetValueFloat("BuildAndRepair.AreaWidth", repairMax);
                    repairUnit.SetValueFloat("BuildAndRepair.AreaHeight", repairMax);
                    repairUnit.SetValueFloat("BuildAndRepair.AreaDepth", repairMax);
                }
            }
        }
    }
}
