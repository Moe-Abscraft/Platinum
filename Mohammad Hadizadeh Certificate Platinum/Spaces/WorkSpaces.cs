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
                    return GetOccupiedColor(SpaceId);
                case SpaceMode.Closed:
                    return 1;
                default:
                    return 0;
            }
        }
        
        public static ushort GetOccupiedColor(string spaceId)
        {
            switch (spaceId)
            {
                case "A":
                    return 2;
                case "B":
                    return 3;
                case "C":
                    return 5;
                case "D":
                    return 6;
                case "E":
                    return 7;
                case "F":
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