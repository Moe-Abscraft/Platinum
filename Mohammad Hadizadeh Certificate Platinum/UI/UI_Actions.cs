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
            { "ClosePopUps", new ushort[] { 25, 21 } },
            { "Message_Pop", new ushort[] { 11 } },
            { "Message_KP", new ushort[] { 12 } },
            { "Operation_Storefronts", new ushort[] { 15 } },
            { "Operation_Retail", new ushort[] { 16 } }
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
            TimerHours = 16,
            TimerMinutes = 17,
            TimerSeconds = 18,
            StorefrontStatusA = 21,
            StorefrontStatusB = 22,
            StorefrontStatusC = 23,
            StorefrontStatusD = 24,
            StorefrontStatusE = 25,
            StorefrontStatusF = 26,
            StorefrontMemberA = 27,
            StorefrontMemberB = 28,
            StorefrontMemberC = 29,
            StorefrontMemberD = 30,
            StorefrontMemberE = 31,
            StorefrontMemberF = 32,
            WorkSpaceStatus1 = 33,
            WorkSpaceStatus2 = 34,
            WorkSpaceStatus3 = 35,
            WorkSpaceStatus4 = 36,
            WorkSpaceStatus5 = 37,
            WorkSpaceStatus6 = 38,
            WorkSpaceButtonAction1 = 39,
            WorkSpaceButtonAction2 = 40,
            WorkSpaceButtonAction3 = 41,
            WorkSpaceButtonAction4 = 42,
            WorkSpaceButtonAction5 = 43,
            WorkSpaceButtonAction6 = 44,
        }

        public enum AnalogJoins
        {
            StoreFrontMode1 = 1,
            StoreFrontMode2 = 2,
            StoreFrontMode3 = 3,
            StoreFrontMode4 = 4,
            StoreFrontMode5 = 5,
            StoreFrontMode6 = 6,
            WorkSpaceMode1 = 7,
            WorkSpaceMode2 = 8,
            WorkSpaceMode3 = 9,
            WorkSpaceMode4 = 10,
            WorkSpaceMode5 = 11,
            WorkSpaceMode6 = 12,
        }

        public enum SubpageJoins
        {
            VPubLoginKeypad = 12,
            VPubLoginMessage = 11,
            OperatingPage = 13,
            MessagePage = 14,
            StorefrontsPage = 15,
            RetailPage = 16,
        }

        public static void TogglePopup(BasicTriListWithSmartObject tp, ushort join)
        {
            if (PopupsJoinGroup["ClosePopUps"].Contains<ushort>(join))
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
                if (input.Length == 1) input = "0";
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
            if (output > 65535) output = 65535;
            return (ushort)output;
        }

        public static void SetStoreMode(BasicTriListWithSmartObject tp, string spaceId)
        {
            if(ControlSystem.StoreFronts[spaceId] == null && ControlSystem.WorkSpaces[spaceId] == null) return;

            CrestronConsole.PrintLine(
                $"Setting Store Mode for Space {spaceId}");

            switch (spaceId)
            {
                case "A":
                    tp.UShortInput[(ushort)UI_Actions.AnalogJoins.StoreFrontMode1].UShortValue =
                        ControlSystem.StoreFronts[spaceId].GetModeColor();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontStatusA].StringValue =
                        ControlSystem.StoreFronts[spaceId].SpaceMode == SpaceMode.MySpace
                            ? "Your Space"
                            : ControlSystem.StoreFronts[spaceId].SpaceMode.ToString();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontMemberA].StringValue =
                        ControlSystem.StoreFronts[spaceId].MemberName;
                    break;
                case "B":
                    tp.UShortInput[(ushort)UI_Actions.AnalogJoins.StoreFrontMode2].UShortValue =
                        ControlSystem.StoreFronts[spaceId].GetModeColor();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontStatusB].StringValue =
                        ControlSystem.StoreFronts[spaceId].SpaceMode == SpaceMode.MySpace
                            ? "Your Space"
                            : ControlSystem.StoreFronts[spaceId].SpaceMode.ToString();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontMemberB].StringValue =
                        ControlSystem.StoreFronts[spaceId].MemberName;
                    break;
                case "C":
                    tp.UShortInput[(ushort)UI_Actions.AnalogJoins.StoreFrontMode3].UShortValue =
                        ControlSystem.StoreFronts[spaceId].GetModeColor();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontStatusC].StringValue =
                        ControlSystem.StoreFronts[spaceId].SpaceMode == SpaceMode.MySpace
                            ? "Your Space"
                            : ControlSystem.StoreFronts[spaceId].SpaceMode.ToString();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontMemberC].StringValue =
                        ControlSystem.StoreFronts[spaceId].MemberName;
                    break;
                case "D":
                    tp.UShortInput[(ushort)UI_Actions.AnalogJoins.StoreFrontMode4].UShortValue =
                        ControlSystem.StoreFronts[spaceId].GetModeColor();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontStatusD].StringValue =
                        ControlSystem.StoreFronts[spaceId].SpaceMode == SpaceMode.MySpace
                            ? "Your Space"
                            : ControlSystem.StoreFronts[spaceId].SpaceMode.ToString();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontMemberD].StringValue =
                        ControlSystem.StoreFronts[spaceId].MemberName;
                    break;
                case "E":
                    tp.UShortInput[(ushort)UI_Actions.AnalogJoins.StoreFrontMode5].UShortValue =
                        ControlSystem.StoreFronts[spaceId].GetModeColor();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontStatusE].StringValue =
                        ControlSystem.StoreFronts[spaceId].SpaceMode == SpaceMode.MySpace
                            ? "Your Space"
                            : ControlSystem.StoreFronts[spaceId].SpaceMode.ToString();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontMemberE].StringValue =
                        ControlSystem.StoreFronts[spaceId].MemberName;
                    break;
                case "F":
                    tp.UShortInput[(ushort)UI_Actions.AnalogJoins.StoreFrontMode6].UShortValue =
                        ControlSystem.StoreFronts[spaceId].GetModeColor();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontStatusF].StringValue =
                        ControlSystem.StoreFronts[spaceId].SpaceMode == SpaceMode.MySpace
                            ? "Your Space"
                            : ControlSystem.StoreFronts[spaceId].SpaceMode.ToString();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontMemberF].StringValue =
                        ControlSystem.StoreFronts[spaceId].MemberName;
                    break;
                case "1":
                    tp.UShortInput[(ushort)UI_Actions.AnalogJoins.WorkSpaceMode1].UShortValue =
                        ControlSystem.WorkSpaces[spaceId].GetModeColor();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.WorkSpaceButtonAction1].StringValue =
                        ControlSystem.WorkSpaces[spaceId].GetActionText();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.WorkSpaceStatus1].StringValue =
                        ControlSystem.WorkSpaces[spaceId].GetStatusText();
                    break;
                case "2":
                    tp.UShortInput[(ushort)UI_Actions.AnalogJoins.WorkSpaceMode2].UShortValue =
                        ControlSystem.WorkSpaces[spaceId].GetModeColor();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.WorkSpaceButtonAction2].StringValue =
                        ControlSystem.WorkSpaces[spaceId].GetActionText();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.WorkSpaceStatus2].StringValue =
                        ControlSystem.WorkSpaces[spaceId].GetStatusText();
                    break;
                case "3":
                    tp.UShortInput[(ushort)UI_Actions.AnalogJoins.WorkSpaceMode3].UShortValue =
                        ControlSystem.WorkSpaces[spaceId].GetModeColor();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.WorkSpaceButtonAction3].StringValue =
                        ControlSystem.WorkSpaces[spaceId].GetActionText();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.WorkSpaceStatus3].StringValue =
                        ControlSystem.WorkSpaces[spaceId].GetStatusText();
                    break;
                case "4":
                    tp.UShortInput[(ushort)UI_Actions.AnalogJoins.WorkSpaceMode4].UShortValue =
                        ControlSystem.WorkSpaces[spaceId].GetModeColor();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.WorkSpaceButtonAction4].StringValue =
                        ControlSystem.WorkSpaces[spaceId].GetActionText();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.WorkSpaceStatus4].StringValue =
                        ControlSystem.WorkSpaces[spaceId].GetStatusText();
                    break;
                case "5":
                    tp.UShortInput[(ushort)UI_Actions.AnalogJoins.WorkSpaceMode5].UShortValue =
                        ControlSystem.WorkSpaces[spaceId].GetModeColor();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.WorkSpaceButtonAction5].StringValue =
                        ControlSystem.WorkSpaces[spaceId].GetActionText();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.WorkSpaceStatus5].StringValue =
                        ControlSystem.WorkSpaces[spaceId].GetStatusText();
                    break;
                case "6":
                    tp.UShortInput[(ushort)UI_Actions.AnalogJoins.WorkSpaceMode6].UShortValue =
                        ControlSystem.WorkSpaces[spaceId].GetModeColor();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.WorkSpaceButtonAction6].StringValue =
                        ControlSystem.WorkSpaces[spaceId].GetActionText();
                    tp.StringInput[(ushort)UI_Actions.SerialJoins.WorkSpaceStatus6].StringValue =
                        ControlSystem.WorkSpaces[spaceId].GetStatusText();
                    break;
            }
        }
    }
}