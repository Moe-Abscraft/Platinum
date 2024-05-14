using Crestron.SimplSharp;
using Newtonsoft.Json;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class WorkSpaces
    {
        private readonly WorkSpace[] _workSpaces;

        public WorkSpaces(int count)
        {
            _workSpaces = new WorkSpace[count];
        }
        public WorkSpace this[string spaceId]
        {
            get
            {
                foreach (var workSpace in _workSpaces)
                {
                    if (workSpace.SpaceId == spaceId)
                    {
                        return workSpace;
                    }
                }

                return null;
            }
            set
            {
                for (var i = 0; i < _workSpaces.Length; i++)
                {
                    if(_workSpaces[i] != null)
                    {
                        if (_workSpaces[i].SpaceId != spaceId) continue;
                        _workSpaces[i] = value;
                        break;
                    }

                    _workSpaces[i] = value;
                    break;
                }
            }
        }
    }
    public class WorkSpace : Space
    {
        public string AdjacentStorefrontId { get; set; }
        public string[] AdjacentWorkSpaces { get; set; }

        [JsonIgnore]
        public CrestronQueue<string> StorefrontQueue;
        public string AssignedStoreFrontId { get; set; }
        public float Area { get; set; }
        public override ushort GetModeColor()
        {
            CrestronConsole.PrintLine($"Getting workspace color for {SpaceMode}");
            switch (SpaceMode)
            {
                case SpaceMode.MySpace:
                    return 4;
                case SpaceMode.Available:
                    return 0;
                case SpaceMode.Occupied:
                    // return GetOccupiedColor(SpaceId);
                    return StoreFront.GetOccupiedColor(AssignedStoreFrontId);
                case SpaceMode.Closed:
                    return 1;
                default:
                    return 0;
            }
        }

        private static ushort GetOccupiedColor(string spaceId)
        {
            switch (spaceId)
            {
                case "1":
                    return 2;
                case "2":
                    return 3;
                case "3":
                    return 5;
                case "4":
                    return 6;
                case "5":
                    return 7;
                case "6":
                    return 8;
                default:
                    return 0;
            }
        }
        
        public string GetActionText()
        {
            switch (SpaceMode)
            {
                case SpaceMode.MySpace:
                    return "Press to REMOVE WorkSpace";
                case SpaceMode.Available:
                    return "Press to ADD WorkSpace";
                case SpaceMode.Occupied:
                    return "Press to QUEUE WorkSpace";
                case SpaceMode.Closed:
                    return "Closed";
                default:
                    return "Unknown";
            }
        }
        
        public string GetStatusText()
        {
            switch (SpaceMode)
            {
                case SpaceMode.MySpace:
                    return MemberName;
                case SpaceMode.Available:
                    return "Available";
                case SpaceMode.Occupied:
                    return MemberName;
                case SpaceMode.Closed:
                    return "";
                default:
                    return "Unknown";
            }
        }
    }
}