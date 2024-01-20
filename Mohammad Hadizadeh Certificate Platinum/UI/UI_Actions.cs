using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class UI_Actions
    {
        public static readonly Dictionary<string, ushort[]> PopupsJoinGroup = new Dictionary<string, ushort[]>()
        {
            {"ClosePopUps", new ushort[] {25, 21}},
            {"Message_Pop", new ushort[] {11}},
            {"Message_KP", new ushort[] {12}}
        };

        public enum DigitalJoins
        {
            VPubLoginEnter = 20,
            VPubLoginAck = 21,
            VPubLogout = 10,
        }
        
        public enum VisibilityJoins
        {
            VPubLoginOk = 7,
        }
        
        public enum SerialJoins
        {
            Date = 1,
            Time = 2,
            SpaceId = 3,
            Decorator = 4,
            OsVersion = 5,
            IpAddress = 6,
            MacAddress = 7,
            StorefrontAvailable = 8,
            StorefrontTotal = 9,
            MarketItemAvailable = 10,
            KeypadInput = 11,
            MemberAccessMessage = 12,
            MemberPubId = 13,
            MemberName = 14,
            MemberExpireDate = 15,
        }

        public enum SubpageJoins
        {
            VPubLoginKeypad = 12,
            VPubLoginMessage = 11,
            OperatingPage = 13,
            MessagePage = 14,
        }
        
        public static void TogglePopup(BasicTriListWithSmartObject tp, ushort join)
        {
            if(PopupsJoinGroup["ClosePopUps"].Contains<ushort>(join))
            {
                foreach (var pop in PopupsJoinGroup.SelectMany(popup => popup.Value.Where(p => p != join)))
                {
                    tp.BooleanInput[pop].BoolValue = false;
                }
            }
            
            else
            {
                foreach (var pop in PopupsJoinGroup.SelectMany(popup => popup.Value.Where(p => p != join)))
                {
                    tp.BooleanInput[pop].BoolValue = false;
                }

                tp.BooleanInput[join].BoolValue = !tp.BooleanInput[join].BoolValue;
            }
        }
        
        public static ushort KeypadInput(string input, string key)
        {
            if (key == "Misc_2")
            {
                if(input.Length == 1) input = "0";
                else if (input.Length > 1)
                {
                    input = input.Remove(input.Length - 1);
                }
            }
            else if (key == "Misc_1")
            {
                input = "0";
            }
            else
            {
                input += key;
            }

            var output = int.Parse(input);
            if(output > 65535) output = 65535;
            return (ushort)output;
        }
    }
}