using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.AudioDistribution;
using Crestron.SimplSharpPro.DeviceSupport;
using Mohammad_Hadizadeh_Certificate_Platinum.HGVR;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class RentalService
    {
        public static float TotalCharge;
        public static float TotalShoppingCharge;
        public static void RentSpace(StoreFront storeFront, WorkSpace workSpace, InquiryRequest inquiryRequest)
        {
            if(storeFront.MemberId == "")
            {
                CrestronConsole.PrintLine("Member is not logged in.");
                return;
            }
            if(storeFront.AssignedWorkSpaces == null) storeFront.AssignedWorkSpaces = new List<WorkSpace>();
            else
            {
                if(storeFront.AssignedWorkSpaces.Exists(ws => ws.SpaceId == workSpace.SpaceId))
                {
                    // Release a workspace
                    
                    CrestronConsole.PrintLine("Workspace is being released from this storefront.");
                    var indexOfMainWorkspace = storeFront.AssignedWorkSpaces.FindIndex(ws => ws.SpaceId == workSpace.SpaceId);
                    
                    var workSpacesToRemove = storeFront.AssignedWorkSpaces.Skip(indexOfMainWorkspace);
                    var spacesToRemove = workSpacesToRemove as WorkSpace[] ?? workSpacesToRemove.ToArray();
                    foreach (var space in spacesToRemove)
                    {
                        space.SpaceMode = SpaceMode.Available;
                        space.MemberName = "";
                        space.MemberId = "";
                        space.AssignedStoreFrontId = "";
                        
                        storeFront.Area -= space.Area;
                        CrestronConsole.PrintLine($"New Area: {storeFront.Area}");
                        
                        QuirkyTech.EndRentalService(space.SpaceId);

                        inquiryRequest.UpdateWorkspaceStatusRequest(ControlSystem.IpAddress, space);
                        foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
                        {
                            inquiryRequest.UpdateWorkspaceStatusRequest(storesIpAddress.ToString(), space);
                        }
                    }
                    
                    var hgvrWorkspaces = Configurator.Stores.Where(s => !s.IS_STOREFRONT);
                    var hgvrAssignedWorkspaces =
                        hgvrWorkspaces.Where(ws => storeFront.AssignedWorkSpaces.Any(s => s.SpaceId == ws.SPACE_ID));
                    List<ushort> wallsToClose = new List<ushort>();
                    List<ushort> fansToTurnOff = new List<ushort>();
                    foreach (var workspace in hgvrAssignedWorkspaces)
                    {
                        foreach (var wall in workspace.Walls)
                        {
                            wallsToClose.Add(wall);
                        }
                        foreach (var fan in workspace.Fans)
                        {
                            fansToTurnOff.Add(fan);
                        }
                    }

                    wallsToClose = wallsToClose.GroupBy(x => x)
                        .Where(g => g.Count() > 1)
                        .Select(y => y.Key)
                        .ToList();
                    HGVRConfigurator.CloseWalls(wallsToClose.ToArray());
                    HGVRConfigurator.TurnOffFans(fansToTurnOff.ToArray());
                    
                    storeFront.AssignedWorkSpaces.RemoveRange(indexOfMainWorkspace, spacesToRemove.Count());

                    return;
                }
            }
                    
            // Reserve a workspace
            
            var isAdjacent = workSpace.AdjacentStorefrontId == ControlSystem.SpaceId;
            if (!isAdjacent)
            {
                if(storeFront.AssignedWorkSpaces.Count > 0)
                {
                    foreach (var assignedWorkSpace in 
                             storeFront.AssignedWorkSpaces
                                 .Where(assignedWorkSpace => assignedWorkSpace.AdjacentWorkSpaces
                                     .Any(adjacentWorkSpace => workSpace.SpaceId == adjacentWorkSpace)))
                    {
                        isAdjacent = true;
                    }
                }
            }
            CrestronConsole.PrintLine(isAdjacent ? "Adjacent Storefront" : "Not Adjacent Storefront");
            if (!isAdjacent) return;
            
            var isOpen = workSpace.SpaceMode != SpaceMode.Closed;
            CrestronConsole.PrintLine(isOpen ? "Workspace is Open" : "Workspace is not Open");
            if (!isOpen) return;
            
            var isAvailable = workSpace.SpaceMode != SpaceMode.Occupied;
            CrestronConsole.PrintLine(isAvailable ? "Workspace is Available" : "Workspace is not Available -- Adding to Queue");
            if (!isAvailable)
            {
                if(workSpace.StorefrontQueue != null)
                {
                    if (workSpace.StorefrontQueue.Contains(storeFront.SpaceId))
                    {
                        CrestronConsole.PrintLine($"Space {storeFront.SpaceId} already is in queue, removing from queue.");
                        WorkspaceStorefrontQueue(workSpace, storeFront, inquiryRequest, "remove");
                        return;
                    }
                }
                WorkspaceStorefrontQueue(workSpace, storeFront, inquiryRequest, "add");
                return;
            }
            
            CrestronConsole.PrintLine("Workspace is being assigned to this storefront.");
                    
            workSpace.SpaceMode = SpaceMode.Occupied;
            workSpace.MemberName = storeFront.MemberName;
            workSpace.MemberId = storeFront.MemberId;
            workSpace.AssignedStoreFrontId = storeFront.SpaceId;
                    
            storeFront.AssignedWorkSpaces.Add(workSpace);
            storeFront.Area += workSpace.Area;
            CrestronConsole.PrintLine($"New Area: {storeFront.Area}");
            
            QuirkyTech.StartRentalService(workSpace.SpaceId);

            var workspaces = Configurator.Stores.Where(s => !s.IS_STOREFRONT);
            var assignedWorkspaces =
                workspaces.Where(ws => storeFront.AssignedWorkSpaces.Any(s => s.SpaceId == ws.SPACE_ID));
            List<ushort> wallsToOpen = new List<ushort>();
            List<ushort> fansToTurnOn = new List<ushort>();
            foreach (var workspace in assignedWorkspaces)
            {
                foreach (var wall in workspace.Walls)
                {
                    wallsToOpen.Add(wall);
                }
                foreach (var fan in workspace.Fans)
                {
                    fansToTurnOn.Add(fan);
                }
            }

            wallsToOpen = wallsToOpen.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key)
                .ToList();
            HGVRConfigurator.OpenWalls(wallsToOpen.ToArray());
            HGVRConfigurator.TurnOnFans(fansToTurnOn.ToArray());
            
            foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
            {
                inquiryRequest.UpdateWorkspaceStatusRequest(storesIpAddress.ToString(), workSpace);
            }
            inquiryRequest.UpdateWorkspaceStatusRequest(ControlSystem.IpAddress, workSpace);
        }

        public static void WorkspaceStorefrontQueue(WorkSpace workSpace, StoreFront storeFront, InquiryRequest inquiryRequest, string action)
        {
            if(action == "remove")
            {
                if (workSpace.StorefrontQueue == null) return;
                if (!workSpace.StorefrontQueue.Contains(storeFront.SpaceId)) return;
                
                var newQueue = new CrestronQueue<string>();
                foreach (var spaceId in workSpace.StorefrontQueue)
                {
                    if (spaceId == storeFront.SpaceId) continue;
                    newQueue.Enqueue(spaceId);
                }
                workSpace.StorefrontQueue = newQueue;
                
                if(inquiryRequest == null) return;
                inquiryRequest.UpdateQueueStatusRequest(ControlSystem.IpAddress, workSpace, storeFront, "remove");
                foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
                {
                    inquiryRequest.UpdateQueueStatusRequest(storesIpAddress.ToString(), workSpace, storeFront, "remove");
                }
            }
            else if (action == "add")
            {
                if (workSpace.StorefrontQueue == null) workSpace.StorefrontQueue = new CrestronQueue<string>();
                if (workSpace.StorefrontQueue.Contains(storeFront.SpaceId))
                {
                    CrestronConsole.PrintLine($"Space {storeFront.SpaceId} already is in queue");
                    return;
                }
                workSpace.StorefrontQueue.Enqueue(storeFront.SpaceId);
            
                if(inquiryRequest == null) return;
                inquiryRequest.UpdateQueueStatusRequest(ControlSystem.IpAddress, workSpace, storeFront, "add");
                foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
                {
                    inquiryRequest.UpdateQueueStatusRequest(storesIpAddress.ToString(), workSpace, storeFront, "add");
                }
            }
            else if (action == "remove_all")
            {
                workSpace.StorefrontQueue.Dispose();
                workSpace.StorefrontQueue = null;
                workSpace.StorefrontQueue = new CrestronQueue<string>();
                
                inquiryRequest.UpdateQueueStatusRequest(ControlSystem.IpAddress, workSpace, storeFront, "remove_all");
                foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
                {
                    inquiryRequest.UpdateQueueStatusRequest(storesIpAddress.ToString(), workSpace, storeFront, "remove_all");
                }
            }
        }

        public static float GetTotalCharge(Stopwatch stopwatch)
        {
            var storeFront = ControlSystem.StoreFronts[ControlSystem.MyStore.SPACE_ID];
            var area = storeFront.Area;
            var totalTime = stopwatch.Elapsed.Hours * 60 + stopwatch.Elapsed.Minutes;
            // if (totalTime == 0) return 0;
            // totalTime = 1;
            var totalAreaCharge = area * ControlSystem.Rate / 60;
            TotalCharge = totalAreaCharge * totalTime + TotalShoppingCharge;
            CrestronConsole.PrintLine($"Area: {area} Rate: {ControlSystem.Rate} Time: {totalTime} Charge: {totalAreaCharge} Total Charge: {TotalCharge}");
            TotalCharge = (float)System.Math.Round(TotalCharge, 2);
            return TotalCharge;
        }
    }
}