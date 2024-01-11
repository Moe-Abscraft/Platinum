using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class UI_Actions
    {
        public static readonly Dictionary<string, uint> PopupsJoinGroup = new Dictionary<string, uint>()
        {
            {"ClosePopUps", 25},
            {"Message_Pop", 11},
            {"Message_KP", 12}
        };

        public enum DigitalJoins
        {
            VPubLoginEnter = 20,
            VPubLoginAck = 21,
        }
        
        public enum VisibilityJoins
        {
            VPubLoginOK = 7,
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
        }

        public enum SubpageJoin
        {
            VpubLogin = 12,
            VPubLoginKeypad = 12,
            VPubLoginMessage = 11,
            OperatingPage = 13,
        }
        
        public static void TogglePopup(BasicTriListWithSmartObject tp, uint join)
        {
            if(join == PopupsJoinGroup["ClosePopUps"])
            {            
                foreach (var popup in PopupsJoinGroup.Where(popup => popup.Value != @join))
                {
                    tp.BooleanInput[popup.Value].BoolValue = false;
                }
            }
            
            else
            {
                foreach (var popup in PopupsJoinGroup.Where(popup => popup.Value != @join))
                {
                    tp.BooleanInput[popup.Value].BoolValue = false;
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