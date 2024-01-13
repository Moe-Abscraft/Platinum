using System;
using System.CodeDom;
using System.IO;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using Directory = Crestron.SimplSharp.CrestronIO.Directory;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class UI
    {
        private readonly Tsw770 _tsw770;
        private DateTime _now = DateTime.Now;
        private CTimer _timer;
        
        private CardReader _cardReader;

        public UI(ControlSystem cs)
        {
            _tsw770 = new Tsw770(0x2A, cs);
            _tsw770.SigChange += _tsw770_SigChange;
            _tsw770.OnlineStatusChange += _tsw770_OnlineStatusChange;
            
            var sgdFile = Path.Combine(Directory.GetApplicationDirectory(), "UI\\VPUB-TSW770.sgd");
            if (File.Exists(sgdFile))
            {
                _tsw770.LoadSmartObjects(sgdFile);
                foreach (var smartObject in _tsw770.SmartObjects)
                {
                    CrestronConsole.PrintLine("Smart Object {0} loaded", smartObject.Value.ID);
                    smartObject.Value.SigChange += _tsw770_SmartGraphicsSigChange;
                }
            }
            
            if (_tsw770.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                ErrorLog.Error("Unable to register TSW770");
            
            _cardReader = new CardReader();
            
            // Card Reader Events Member Is Not Expired
            _cardReader.MemberIsNotExpired += (sender, args) =>
            {
                UI_Actions.TogglePopup(_tsw770, UI_Actions.PopupsJoinGroup["Message_Pop"][0]);
                _tsw770.BooleanInput[(ushort)UI_Actions.VisibilityJoins.VPubLoginOK].BoolValue = true;
                UI_Actions.KeypadInput("", "Misc_1");

                var memberInquiry = new MemberInquiryRequest().GetMemberInquiryRequest("192.168.1.15");
                CrestronConsole.PrintLine($"Member Inquiry: {memberInquiry}");
            };
            
            _cardReader.MemberIsExpired += (sender, args) =>
            {
                UI_Actions.TogglePopup(_tsw770, UI_Actions.PopupsJoinGroup["Message_Pop"][0]);
                _tsw770.BooleanInput[(ushort)UI_Actions.VisibilityJoins.VPubLoginOK].BoolValue = true;
                UI_Actions.KeypadInput("", "Misc_1");
            };

        }
        private void _tsw770_OnlineStatusChange(GenericBase currentdevice, OnlineOfflineEventArgs args)
        {
            _timer = new CTimer(UpdateTime, null, 0, 1000);
            
            if(!args.DeviceOnLine) return;

            // Serial Joins
            _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.SpaceId].StringValue = $"Storefront { ControlSystem.SpaceId }";
            _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.Decorator].StringValue = $"{ ControlSystem.SpaceDecor }";
            _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.OsVersion].StringValue = $"{ ControlSystem.OsVersion }";
            _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.IpAddress].StringValue = $"{ ControlSystem.IpAddress }";
            _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MacAddress].StringValue = $"{ ControlSystem.MacAddress.ToUpper() }";
            _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontAvailable].StringValue = $"{ ControlSystem.NumOfStoresAvailable }";
            _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontTotal].StringValue = $"{ ControlSystem.NumOfStoresOpen }";
            _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MarketItemAvailable].StringValue = $"{ ControlSystem.NumOfMarketItemsAvailable }";

            // Smart Object Joins
            foreach (var smartObject1BooleanOutput in _tsw770.SmartObjects[1].BooleanOutput)
            {
                smartObject1BooleanOutput.UserObject = new Action<bool>(b =>
                {
                    if (!b) return;
                    CardReader.CardNumber = UI_Actions.KeypadInput(CardReader.CardNumber.ToString(), smartObject1BooleanOutput.Name);
                    _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.KeypadInput].StringValue = CardReader.CardNumber > 0 ? CardReader.CardNumber.ToString() : "";
                    CrestronConsole.PrintLine(CardReader.CardNumber.ToString());
                });
            }
            
            // Boolean Joins
            _tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.VPubLoginEnter].UserObject = new Action<bool>(b =>
            {
                if (!b) return;
                var memberInfo = _cardReader.GetMemberInfo(CardReader.CardNumber);
                UI_Actions.KeypadInput("", "Misc_1");
                CrestronConsole.PrintLine($"Member Info: {memberInfo}");
                
                _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberAccessMessage].StringValue = CardReader.MembershipIsValie ? "Access Granted" : "Access Denied";
                _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberName].StringValue = CardReader.MemberName;
                _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberPubId].StringValue = CardReader.MemberId;
                _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberExpireDate].StringValue = CardReader.MemberExpiryDateTime.ToString("dd MMMM yyyy");
            });
            
            _tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.VPubLoginAck].UserObject = new Action<bool>(b =>
            {
                if(CardReader.MembershipIsValie)
                    _tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.OperatingPage].BoolValue = true;
            });
            
            // Subpage Joins
            foreach (var popup in UI_Actions.PopupsJoinGroup.Values.SelectMany(joins => joins.Where(j => j > 0)))
            {
                ((Tsw770)currentdevice).BooleanOutput[popup].UserObject = new Action<bool>(b =>
                {
                    if(b) UI_Actions.TogglePopup((Tsw770)currentdevice, popup);
                });
            }
        }

        private void UpdateTime(object userspecific)
        {
            _now = DateTime.Now;
            _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.Date].StringValue = _now.ToString("dddd, dd MMMM, yyyy");
            _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.Time].StringValue = _now.ToString("HH:mm:ss");
        }

        private void _tsw770_SigChange(BasicTriList currentdevice, SigEventArgs args)
        {
            switch (args.Sig.Type)
            {
                case eSigType.Bool:
                    var action = args.Sig.UserObject as Action<bool>;
                    action?.Invoke(args.Sig.BoolValue);
                    break;
                case eSigType.UShort:
                    var action1 = args.Sig.UserObject as Action<ushort>;
                    action1?.Invoke(args.Sig.UShortValue);
                    break;
                case eSigType.String:
                    var action2 = args.Sig.UserObject as Action<string>;
                    action2?.Invoke(args.Sig.StringValue);
                    break;
                case eSigType.NA:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void _tsw770_SmartGraphicsSigChange(GenericBase currentdevice, SmartObjectEventArgs args)
        {
            switch (args.Sig.Type)
            {
                case eSigType.Bool:
                    var action = args.Sig.UserObject as Action<bool>;
                    action?.Invoke(args.Sig.BoolValue);
                    break;
                case eSigType.UShort:
                    var action1 = args.Sig.UserObject as Action<ushort>;
                    action1?.Invoke(args.Sig.UShortValue);
                    break;
                case eSigType.String:
                    var action2 = args.Sig.UserObject as Action<string>;
                    action2?.Invoke(args.Sig.StringValue);
                    break;
                case eSigType.NA:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}