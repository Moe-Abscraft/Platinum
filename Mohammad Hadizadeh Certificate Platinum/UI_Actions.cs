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
            {"Keypad", 12}
        };
        
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