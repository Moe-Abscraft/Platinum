using System;
using System.CodeDom;
using System.IO;
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
        
        private CardReader _cardReader = new CardReader();

        public UI(ControlSystem cs)
        {
            _tsw770 = new Tsw770(0x2A, cs);
            _tsw770.SigChange += _tsw770_SigChange;
            _tsw770.OnlineStatusChange += _tsw770_OnlineStatusChange;
            
            var sgdFile = Path.Combine(Directory.GetApplicationDirectory(), "VPUB-TSW770.sgd");
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
        }
        private void _tsw770_OnlineStatusChange(GenericBase currentdevice, OnlineOfflineEventArgs args)
        {
            _timer = new CTimer(UpdateTime, null, 0, 1000);
            
            if(!args.DeviceOnLine) return;
            
            foreach (var popup in UI_Actions.PopupsJoinGroup)
            {
                ((Tsw770)currentdevice).BooleanOutput[popup.Value].UserObject = new Action<bool>(b =>
                {
                    if(b) UI_Actions.TogglePopup((Tsw770)currentdevice, popup.Value);
                });
            }
            
            _tsw770.StringInput[3].StringValue = $"Storefront { ControlSystem.SpaceId }";
            _tsw770.StringInput[4].StringValue = $"{ ControlSystem.SpaceDecor }";
            _tsw770.StringInput[5].StringValue = $"{ ControlSystem.OsVersion }";
            _tsw770.StringInput[6].StringValue = $"{ ControlSystem.IpAddress }";
            _tsw770.StringInput[7].StringValue = $"{ ControlSystem.MacAddress.ToUpper() }";
            _tsw770.StringInput[8].StringValue = $"{ ControlSystem.NumOfStoresAvailable }";
            _tsw770.StringInput[9].StringValue = $"{ ControlSystem.NumOfStoresOpen }";
            _tsw770.StringInput[10].StringValue = $"{ ControlSystem.NumOfMarketItemsAvailable }";

            foreach (var smartObject1BooleanOutput in _tsw770.SmartObjects[1].BooleanOutput)
            {
                smartObject1BooleanOutput.UserObject = new Action<bool>(b =>
                {
                    if (b)
                    {
                        CardReader.CardNumber = UI_Actions.KeypadInput(CardReader.CardNumber.ToString(), smartObject1BooleanOutput.Name);
                        CrestronConsole.PrintLine(CardReader.CardNumber.ToString());
                    }
                });
            }
            
            _tsw770.BooleanOutput[10].UserObject = new Action<bool>(b =>
            {
                if (b)
                {
                    var memberInfo = _cardReader.GetMemberInfo(CardReader.CardNumber);
                    CrestronConsole.PrintLine($"Member Info: {memberInfo}");
                }
            });
        }

        private void UpdateTime(object userspecific)
        {
            _now = DateTime.Now;
            _tsw770.StringInput[1].StringValue = _now.ToString("dddd, dd MMMM, yyyy");
            _tsw770.StringInput[2].StringValue = _now.ToString("HH:mm:ss");
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