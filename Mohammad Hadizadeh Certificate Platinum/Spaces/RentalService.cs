using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class RentalService
    {
        public static void RentSpace(StoreFront storeFront, WorkSpace workSpace, InquiryRequest inquiryRequest)
        {
            if(storeFront.AssignedWorkSpaces == null) storeFront.AssignedWorkSpaces = new List<WorkSpace>();
            else
            {
                if(storeFront.AssignedWorkSpaces.Exists(ws => ws.SpaceId == workSpace.SpaceId))
                {
                    CrestronConsole.PrintLine("Workspace is being released from this storefront.");
                    var indexOfMainWorkspace = storeFront.AssignedWorkSpaces.FindIndex(ws => ws.SpaceId == workSpace.SpaceId);
                    
                    var workSpacesToRemove = storeFront.AssignedWorkSpaces.Skip(indexOfMainWorkspace);
                    var spacesToRemove = workSpacesToRemove as WorkSpace[] ?? workSpacesToRemove.ToArray();
                    foreach (var space in spacesToRemove)
                    {
                        space.SpaceMode = SpaceMode.Available;
                        space.MemberName = "";
                        space.MemberId = "";
                        
                        storeFront.Area -= space.Area;
                        CrestronConsole.PrintLine($"New Area: {storeFront.Area}");
                    
                        inquiryRequest.UpdateWorkspaceStatusRequest(ControlSystem.IpAddress, space);
                        foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
                        {
                            inquiryRequest.UpdateWorkspaceStatusRequest(storesIpAddress.ToString(), space);
                        }
                    }
                    
                    storeFront.AssignedWorkSpaces.RemoveRange(indexOfMainWorkspace, spacesToRemove.Count());

                    return;
                }
            }
                    
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
                WorkspaceStorefrontQueue(workSpace, storeFront, inquiryRequest);
                return;
            }
            
            CrestronConsole.PrintLine("Workspace is being assigned to this storefront.");
                    
            workSpace.SpaceMode = SpaceMode.Occupied;
            workSpace.MemberName = storeFront.MemberName;
            workSpace.MemberId = storeFront.MemberId;
                    
            storeFront.AssignedWorkSpaces.Add(workSpace);
            storeFront.Area += workSpace.Area;
            CrestronConsole.PrintLine($"New Area: {storeFront.Area}");

            inquiryRequest.UpdateWorkspaceStatusRequest(ControlSystem.IpAddress, workSpace);
            foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
            {
                inquiryRequest.UpdateWorkspaceStatusRequest(storesIpAddress.ToString(), workSpace);
            }
        }

        public static void WorkspaceStorefrontQueue(WorkSpace workSpace, StoreFront storeFront, InquiryRequest inquiryRequest)
        {
            if (workSpace.StorefrontQueue == null) workSpace.StorefrontQueue = new CrestronQueue<string>();
            if (workSpace.StorefrontQueue.Contains(storeFront.SpaceId))
            {
                CrestronConsole.PrintLine($"Space {storeFront.SpaceId} already is in queue");
                return;
            }
            workSpace.StorefrontQueue.Enqueue(storeFront.SpaceId);
            
            if(inquiryRequest == null) return;
            inquiryRequest.UpdateQueueStatusRequest(ControlSystem.IpAddress, workSpace, storeFront);
            foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
            {
                inquiryRequest.UpdateQueueStatusRequest(storesIpAddress.ToString(), workSpace, storeFront);
            }
        }
    }
}