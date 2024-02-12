using System;
using System.ComponentModel;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public abstract class Space : EventArgs
    {
        public string SpaceId { get; set; }
        public SpaceMode SpaceMode { get; set; }
        public string MemberName { get; set; }
        public string MemberId { get; set; }
        public abstract ushort GetModeColor();
    }

    public enum SpaceMode
    {
        MySpace,
        Available,
        Occupied,
        Closed
    }
}