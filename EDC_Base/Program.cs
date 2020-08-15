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
        string hugeLCDName = "[Base] Huge LCD";
        string gravelSorterName = "[Base] Gravel Sorter";
        double gravelMaximum = 5000;
        bool autobuildDisplay = false;

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script.
            //
            // The constructor is optional and can be removed if not
            // needed.
            //
            // It's recommended to set RuntimeInfo.UpdateFrequency
            // here, which will allow your script to run itself without a
            // timer block.
        }


        public void Main(string argument, UpdateType updateSource)
        {
            switch (argument)
            {
                case "UpdateDisplays": UpdateDisplays(); break;
                case "AutobuildNow": AutobuildComponents("[Base] Automation LCD 4", "Monospace", Me.CubeGrid.CustomName, true); break;
                case "ClearQueue": ClearQueue(); break;
            }

            string[] tokens = argument.Split(':');
            if (tokens[0] == "RunFunc")
            {
                switch (tokens[1])
                {
                    case "ToggleDisplaySwitch": ToggleDisplaySwitch(tokens[2], tokens[3], tokens[4]); break;
                    case "PistonControl": PistonControl(tokens[2], tokens[3], tokens[4], tokens[5]); break;
                    case "SwitchHugeLCD": SwitchHugeLCD(tokens[2]); break;
                    default: Echo($"Unrecognised call {tokens[1]}({tokens[2]})"); break;
                }
            }
        }

        private void ToggleDisplaySwitch(string switchName, string displayNum, string blockName)
        {
            IMyButtonPanel panel = (IMyButtonPanel)GridTerminalSystem.GetBlockWithName(switchName);
            IMyTextSurface surface = ((IMyTextSurfaceProvider)panel).GetSurface(Int32.Parse(displayNum));
            IMyFunctionalBlock block = (IMyFunctionalBlock)GridTerminalSystem.GetBlockWithName(blockName);

            bool isPowered = block.Enabled;
            if (isPowered) surface.BackgroundColor = Color.Red;
            else surface.BackgroundColor = Color.Green;

            block.Enabled = !isPowered;
        }

        private void PistonControl(string switchName, string displayNum, string pistonName, string action)
        {
            IMyButtonPanel panel = (IMyButtonPanel)GridTerminalSystem.GetBlockWithName(switchName);
            IMyTextSurface surface = ((IMyTextSurfaceProvider)panel).GetSurface(Int32.Parse(displayNum));
            IMyPistonBase piston = (IMyPistonBase)GridTerminalSystem.GetBlockWithName(pistonName);
            if (action == "On/Off")
            {
                bool pistonEnabled = piston.Enabled;
                if (pistonEnabled) surface.BackgroundColor = Color.Red;
                else surface.BackgroundColor = Color.Green;
                piston.Enabled = !pistonEnabled;
            }
            if (action == "Reverse")
            {
                piston.Reverse();
                if (piston.Velocity < 0) surface.BackgroundColor = Color.Red;
                else surface.BackgroundColor = Color.Green;
            }
        }

        private void SwitchHugeLCD(string subPanelNumber)
        {
            IMyTextPanel mainPanel = (IMyTextPanel)GridTerminalSystem.GetBlockWithName($"{hugeLCDName}");
            IMyTextPanel subPanel = (IMyTextPanel)GridTerminalSystem.GetBlockWithName($"{hugeLCDName} {subPanelNumber}");

            mainPanel.WriteText(subPanel.GetText());
        }

        private void UpdateDisplays()
        {
            ShowAllMaterials("[Base] Automation LCD 1", "Monospace", Me.CubeGrid.CustomName);
            DisplayIceAndGas("[Base] Automation LCD 3");
            UpdateCryoLCD(2);
            UpdateCryoLCD(3);

            AutobuildComponents("[Base] Automation LCD 4", "Monospace", Me.CubeGrid.CustomName);
            //UpdateExteriorDisplay("AutobuildComponents", "[Base] Automation LCD 4");
        }

        private void UpdateCryoLCD(int chamberNumber)
        {
            IMyTextPanel panel = (IMyTextPanel)GridTerminalSystem.GetBlockWithName($"[Base] Cryo LCD {chamberNumber}");
            IMyShipController chamber = (IMyShipController)GridTerminalSystem.GetBlockWithName($"[Base] Cryo Chamber {chamberNumber}");
            //IMyCharacter occupant = chamber.Pilot;
            IMySensorBlock sensor = (IMySensorBlock)GridTerminalSystem.GetBlockWithName($"Cryo Sensor");
            string entity = sensor.LastDetectedEntity.Name;

            Echo(chamber.CustomInfo);
            if (chamber.IsUnderControl || !chamber.IsWorking)
            {
                panel.WriteText("Occupied");
                panel.BackgroundColor = Color.Red;
            }
            else
            {
                panel.WriteText("Vacant");
                panel.BackgroundColor = Color.Green;
            }
        }


        private void DisplayIceAndGas(string panelName)
        {
            IMyTextPanel panel = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(panelName);
            string nowString = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            string outputText = "ICE STORAGE ___________________________\n\n";
            double totalIce = 0;

            List<IMyGasGenerator> gasGenList = new List<IMyGasGenerator>();
            //GridTerminalSystem.GetBlocksOfType<IMyEntity>(blockList, block => block.InventoryCount > 0);
            GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(gasGenList, block => block.InventoryCount > 0);

            gasGenList.Sort((x, y) => string.Compare(x.DisplayNameText, y.DisplayNameText));

            foreach (IMyGasGenerator block in gasGenList)
            {
                double subtotalIce = 0;

                outputText += $"{block.DisplayNameText}: ";

                List<MyInventoryItem> iceList = new List<MyInventoryItem>();
                block.GetInventory(0).GetItems(iceList, item => item.Type.SubtypeId == "Ice");
                foreach (MyInventoryItem item in iceList)
                {
                    subtotalIce += (double)item.Amount;
                }

                outputText += $"{ShortAmount(subtotalIce)}\n";
                totalIce += subtotalIce;
            }

            List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyEntity>(blockList, block => block.BlockDefinition.TypeId.ToString() != "MyObjectBuilder_OxygenGenerator"
                                                                                                                           && block.InventoryCount > 0);

            blockList.Sort((x, y) => string.Compare(x.DisplayNameText, y.DisplayNameText));

            double otherIce = 0;
            foreach (IMyTerminalBlock block in blockList)
            {

                List<MyInventoryItem> iceList = new List<MyInventoryItem>();
                block.GetInventory(0).GetItems(iceList, item => item.Type.SubtypeId == "Ice");

                foreach (MyInventoryItem item in iceList)
                {
                    otherIce += (double)item.Amount;
                }

                //Echo (block.BlockDefinition.TypeId.ToString());
            }

            outputText += $"Other Containers: {ShortAmount(otherIce)}\n";

            totalIce += otherIce;
            outputText += $"Total: {ShortAmount(totalIce)}\n\n";

            outputText += "GAS LEVELS ____________________________\n\n";

            List<IMyGasTank> tankList = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tankList, block => true);

            tankList.Sort((x, y) => string.Compare(x.DisplayNameText, y.DisplayNameText));

            foreach (IMyGasTank block in tankList)
            {
                outputText += $"{block.DisplayNameText}: {block.FilledRatio:##0.0%}\n";
            }

            outputText += $"\n{nowString}";
            panel.Font = "Debug";
            panel.WriteText(outputText);
            panel.ContentType = ContentType.TEXT_AND_IMAGE;
        }

        public class ItemClass
        {
            public string itemSubType = "";
            public double oreCount = 0, ingotCount = 0;
        }

        public SortedList<string, ItemClass> materialList = new SortedList<string, ItemClass>();

        private void AddItemCount(MyInventoryItem item)
        {
            //string itemSubType = PTName(item.Type.SubtypeId);
            string itemSubType = item.Type.SubtypeId;
            if (!materialList.ContainsKey(itemSubType))
            {
                materialList[itemSubType] = new ItemClass();
                materialList[itemSubType].itemSubType = itemSubType;
            }
            if (item.Type.TypeId == "MyObjectBuilder_Ore") materialList[itemSubType].oreCount += (double)item.Amount;
            else materialList[itemSubType].ingotCount += (double)item.Amount;
        }

        private void ShowAllMaterials(string panelName, string font, string gridName = "All")
        {
            IMyTextPanel panel = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(panelName);
            panel.ContentType = ContentType.TEXT_AND_IMAGE;
            panel.Font = font;

            string nowString = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
            string outputText = $"TOTAL MATERIALS\nGrid: {gridName}\n\n";

            for (int i = 0; i < materialList.Count; i++)
            {
                materialList.Values[i].oreCount = 0;
                materialList.Values[i].ingotCount = 0;
            }

            List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();
            if (gridName == "All") GridTerminalSystem.GetBlocksOfType<IMyEntity>(blockList, block => block.InventoryCount > 0);
            else
            {
                GridTerminalSystem.GetBlocksOfType<IMyEntity>(blockList, block => block.CubeGrid.CustomName == gridName && block.InventoryCount > 0);
            }

            foreach (IMyTerminalBlock block in blockList)
            {
                List<MyInventoryItem> oresIngotsList = new List<MyInventoryItem>();
                block.GetInventory(0).GetItems(oresIngotsList, item => item.Type.TypeId == "MyObjectBuilder_Ore" || item.Type.TypeId == "MyObjectBuilder_Ingot");
                foreach (MyInventoryItem item in oresIngotsList)
                {
                    AddItemCount(item);
                }
                if (block.InventoryCount > 1)
                {
                    oresIngotsList = new List<MyInventoryItem>();
                    block.GetInventory(1).GetItems(oresIngotsList, item => item.Type.TypeId == "MyObjectBuilder_Ore" || item.Type.TypeId == "MyObjectBuilder_Ingot");
                    foreach (MyInventoryItem item in oresIngotsList)
                    {
                        AddItemCount(item);
                    }
                }
            }

            outputText += "MATERIAL   ORE    INGOTS\n";
            for (int i = 0; i < materialList.Count; i++)
            {
                outputText += materialList.Values[i].itemSubType.PadRight(9) + "  " +
                                     ShortAmount(materialList.Values[i].oreCount).PadLeft(5) + "  " +
                                     ShortAmount(materialList.Values[i].ingotCount).PadLeft(5) + "\n";

                //if (materialList.Values[i].itemSubType == "Stone") CheckGravelLevels(materialList.Values[i].ingotCount);
            }

            outputText += $"\n{nowString}";
            panel.WriteText(outputText);
        }

        private void CheckGravelLevels(double gravelLevel)
        {
            IMyConveyorSorter gravelSorter = (IMyConveyorSorter)GridTerminalSystem.GetBlockWithName(gravelSorterName);
            IMyGravityGeneratorBase gravelGun = (IMyGravityGeneratorBase)GridTerminalSystem.GetBlockWithName(gravelSorterName + " Gun");

            if (gravelLevel > gravelMaximum)
            {
                gravelSorter.Enabled = true;
                gravelGun.Enabled = true;
            }
            else
            {
                gravelSorter.Enabled = false;
                gravelGun.Enabled = true;
            }

            //Echo($"Gravel Level = {gravelLevel}\nGravel Max = {gravelMaximum}\nGravel Sorter Enabled = {gravelSorter.Enabled}");
        }

        private string PTName(string ingotName)
        {
            switch (ingotName)
            {
                case "Cobalt": return "Co";
                case "Gold": return "Au";
                case "Stone": return "Gr";
                case "Iron": return "Fe";
                case "Magnesium": return "Mg";
                case "Nickel": return "Ni";
                case "Platinum": return "Pt";
                case "Silicon": return "Si";
                case "Silver": return "Ag";
                case "Uranium": return "U";
                default: return ingotName;
            }
        }

        private string ShortAmount(double amount)
        {
            if (amount < 10) return amount.ToString("0.000");
            if (amount < 100) return amount.ToString("00.00");
            if (amount < 1000) return amount.ToString("000.0");
            if (amount < 10000) return (amount / 1000).ToString("0.00") + "k";
            if (amount < 100000) return (amount / 1000).ToString("00.0") + "k";
            if (amount < 1000000) return (amount / 1000).ToString("000") + "k";
            if (amount < 10000000) return (amount / 1000000).ToString("0.00") + "m";
            if (amount < 100000000) return (amount / 1000000).ToString("00.0") + "m";
            if (amount < 1000000000) return (amount / 1000000).ToString("000") + "m";
            return amount.ToString();
        }



        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public class ComponentClass
        {
            public string itemSubType = "";
            public double itemCount = 0;
            public double buildCount = 0;
            public double minCount = 0;
        }

        public SortedList<string, ComponentClass> componentList = new SortedList<string, ComponentClass>();

        private void UpdateComponentMin(string componentName, double minAmount)
        {
            componentName = CorrectComponent(componentName);

            if (!componentList.ContainsKey(componentName))
            {
                componentList[componentName] = new ComponentClass();
                componentList[componentName].itemSubType = componentName;
            }
            componentList[componentName].minCount = minAmount;
        }

        private void AddComponentBuild(MyProductionItem item)
        {
            //Echo($"{item.ItemId}|{item.BlueprintId}|{item.Amount}\n");
            string componentName = item.BlueprintId.SubtypeName;
            //Echo(componentName);


            componentName = CorrectComponent(componentName);

            if (!componentList.ContainsKey(componentName))
            {
                componentList[componentName] = new ComponentClass();
                componentList[componentName].itemSubType = componentName;
            }
            componentList[componentName].buildCount += (double)item.Amount;
        }

        private void AddComponentCount(MyInventoryItem item)
        {
            string itemType = item.Type.TypeId.Substring(16);
            string componentName = $"{item.Type.SubtypeId}";

            componentName = CorrectComponent(componentName);

            if (!componentList.ContainsKey(componentName))
            {
                componentList[componentName] = new ComponentClass();
                componentList[componentName].itemSubType = componentName;
            }
            componentList[componentName].itemCount += (double)item.Amount;
        }

        private void AutobuildComponents(string panelName, string font, string gridName = "All", bool buildNow = false)
        {
            componentList = new SortedList<string, ComponentClass>();

            IMyTextPanel panel = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(panelName);
            panel.ContentType = ContentType.TEXT_AND_IMAGE;
            panel.Font = font;

            IMyTimerBlock timer = (IMyTimerBlock)GridTerminalSystem.GetBlockWithName("[Base] Autobuild Timer");

            double minDefault = 0;
            string nowString = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            string outputText;

            //Echo($"Build Now? {buildNow}\n");
            if (buildNow) timer.CustomData = nowString;
            DateTime lastUpdate = DateTime.Parse(timer.CustomData);
            DateTime nextUpdate = lastUpdate.AddSeconds(timer.TriggerDelay);
            TimeSpan timeRemaining = nextUpdate.Subtract(DateTime.Now);

            string[] minAmounts = panel.CustomData.Split('\n');
            foreach (string minAmount in minAmounts)
            {
                //Echo(minAmount);
                if (minAmount == "END") break;

                string[] thisMin = minAmount.Split('=');
                if (thisMin[0] == "default") minDefault = (double)Int32.Parse(thisMin[1]);
                else
                {
                    //Echo($"{thisMin[0]} is {thisMin[1]}");
                    if (thisMin[1] == "default") UpdateComponentMin(thisMin[0], minDefault);
                    else UpdateComponentMin(thisMin[0], (double)Int32.Parse(thisMin[1]));
                }
            }

            //Echo("Cycling Blocks");
            List<IMyTerminalBlock> blockList = new List<IMyTerminalBlock>();
            if (gridName == "All") GridTerminalSystem.GetBlocksOfType<IMyEntity>(blockList, block => block.InventoryCount > 0);
            else
            {
                GridTerminalSystem.GetBlocksOfType<IMyEntity>(blockList, block => block.CubeGrid.CustomName == gridName && block.InventoryCount > 0);
            }

            foreach (IMyTerminalBlock block in blockList)
            {
                List<MyInventoryItem> componentList = new List<MyInventoryItem>();
                block.GetInventory(0).GetItems(componentList, item => item.Type.TypeId == "MyObjectBuilder_Component");
                foreach (MyInventoryItem item in componentList)
                {
                    if (item.Type.SubtypeId == "AngleGrinder") Echo($"Type {item.Type.TypeId} | Subtype {item.Type.SubtypeId}");
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
            }

            //Echo("Cycling Assemblers");
            List<IMyAssembler> assemblerList = new List<IMyAssembler>();
            if (gridName == "All") GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblerList, assembler => !assembler.IsQueueEmpty);
            else
            {
                GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblerList, assembler => assembler.CubeGrid.CustomName == gridName && !assembler.IsQueueEmpty);
            }

            foreach (IMyAssembler assembler in assemblerList)
            {
                List<MyProductionItem> productionList = new List<MyProductionItem>();
                assembler.GetQueue(productionList);
                foreach (MyProductionItem item in productionList)
                {
                    AddComponentBuild(item);
                }
            }

            //Echo("Output");
            string timerDelay = timer.TriggerDelay.ToString("#0");
            //outputText = $"COMPONENTS  {nowString}  Default Min:{minDefault.ToString("#,##0")}  Autobuilds every {timerDelay}s\n\n";
            outputText = $"COMPONENTS  {nowString}  Default Min:{minDefault:#,##0}  Autobuilds in {timeRemaining:mm\\:ss}\n\n";
            string customText = $"default={minDefault:0}\n";

            int componentCount = componentList.Count;
            string componentText = "";

            componentText += "                               STORAGE    BUILDING   TOTAL     MINIMUM\n";

            for (int i = 0; i < componentList.Count; i++)
            {
                string minCount = componentList.Values[i].minCount.ToString("#0");
                string minDisplay = componentList.Values[i].minCount.ToString("#,##0");
                if (componentList.Values[i].minCount == minDefault)
                {
                    minCount = "default";
                    minDisplay = "default";
                }

                double totalCount = componentList.Values[i].itemCount + componentList.Values[i].buildCount;
                double toBuild = componentList.Values[i].minCount - totalCount;

                if (buildNow && toBuild > 0)
                {
                    BuildComponent(componentList.Values[i].itemSubType, toBuild, minDefault);
                }
                else toBuild = componentList.Values[i].buildCount;

                componentText += componentList.Values[i].itemSubType.PadRight(27) + "  " +
                                    componentList.Values[i].itemCount.ToString("#,##0").PadLeft(10) + " " +
                                    toBuild.ToString("#,##0").PadLeft(10) + " " +
                                    totalCount.ToString("#,##0").PadLeft(10) + " " +
                                    minDisplay.PadLeft(10) + "\n";


                customText += componentList.Values[i].itemSubType + "=" + minCount + "\n";
            }

            outputText += componentText;
            //Echo(nowString);
            if (autobuildDisplay) panel.CustomData = customText + "END";
            panel.WriteText(outputText);
        }

        private void BuildComponent(string componentName, double buildAmount, double batchAmount)
        {
            //Echo($"Build {buildAmount} {componentName}");

            componentName = CorrectComponent(componentName);

            MyDefinitionId blueprint = MyDefinitionId.Parse($"MyObjectBuilder_BlueprintDefinition/{componentName}");

            //Echo(blueprint.ToString());
            List<IMyAssembler> assemblerList = new List<IMyAssembler>();
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblerList, assembler => assembler.CubeGrid == Me.CubeGrid);

            double thisOrder;
            while (buildAmount > 0)
            {
                thisOrder = Math.Min(buildAmount, batchAmount);
                buildAmount -= thisOrder;

                string assemblerName = "";
                double minAmount = 1000000, thisAmount;
                foreach (IMyAssembler assembler in assemblerList)
                {
                    thisAmount = 0;
                    Echo($"{assembler.DisplayNameText}\n{assembler.GetType()}\n");
                    if (assembler.DisplayNameText == "[Base] Survival kit") Echo("Survival Kits are gay");
                    else
                    {
                        List<MyProductionItem> productionList = new List<MyProductionItem>();
                        assembler.GetQueue(productionList);
                        foreach (MyProductionItem item in productionList) thisAmount += (double)item.Amount;

                        //Echo($"ThisAmount={thisAmount}");

                        if (thisAmount < minAmount)
                        {
                            minAmount = thisAmount;
                            assemblerName = assembler.DisplayNameText;

                            //Echo($"Min {assemblerName}={minAmount}");
                        }
                    }
                }

                IMyAssembler minAssembler = (IMyAssembler)GridTerminalSystem.GetBlockWithName(assemblerName);
                bool canUseBlueprint = minAssembler.CanUseBlueprint(blueprint);
                //Echo($"Does it assemble? {canUseBlueprint}");
                if (canUseBlueprint) minAssembler.AddQueueItem(blueprint, thisOrder);
            }

        }

        private void ClearQueue()
        {
            List<IMyAssembler> assemblerList = new List<IMyAssembler>();
            GridTerminalSystem.GetBlocksOfType<IMyAssembler>(assemblerList, assembler => assembler.CubeGrid == Me.CubeGrid);
            foreach (IMyAssembler assembler in assemblerList)
            {
                assembler.ClearQueue();
            }
        }

        private string CorrectComponent(string componentName)
        {
            string newName;
            switch (componentName)
            {
                case "Computer": newName = "ComputerComponent"; break;
                case "Construction": newName = "ConstructionComponent"; break;
                case "Detector": newName = "DetectorComponent"; break;
                case "Girder": newName = "GirderComponent"; break;
                case "GravityGenerator": newName = "GravityGeneratorComponent"; break;
                case "Medical": newName = "MedicalComponent"; break;
                case "Motor": newName = "MotorComponent"; break;
                case "RadioCommunication": newName = "RadioCommunicationComponent"; break;
                case "Reactor": newName = "ReactorComponents"; break;
                case "Thrust": newName = "ThrustComponent"; break;
                default: newName = componentName; break;
            }
            return newName;
        }
    }
}
