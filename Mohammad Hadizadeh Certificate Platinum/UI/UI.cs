using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using Mohammad_Hadizadeh_Certificate_Platinum.StoreFronts;
using Directory = Crestron.SimplSharp.CrestronIO.Directory;
using Thread = Crestron.SimplSharpPro.CrestronThread.Thread;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class UI
    {
        private readonly Tsw770 _tsw770;
        private DateTime _now = DateTime.Now;
        private CTimer _timer;
        private DateTime _startLoginTime;
        private readonly Stopwatch _loginTimer;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        private readonly CardReader _cardReader;
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
                _tsw770.BooleanInput[(ushort)UI_Actions.VisibilityJoins.VPubLoginOk].BoolValue = true;
                UI_Actions.KeypadInput("", "Misc_1");
            };
            
            _cardReader.MemberIsExpired += (sender, args) =>
            {
                UI_Actions.TogglePopup(_tsw770, UI_Actions.PopupsJoinGroup["Message_Pop"][0]);
                _tsw770.BooleanInput[(ushort)UI_Actions.VisibilityJoins.VPubLoginOk].BoolValue = true;
                UI_Actions.KeypadInput("", "Misc_1");
            };

            _loginTimer = new Stopwatch();
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            StoreStatusHandler.SpaceStatusChangedEvent += StoreStatusHandlerOnSpaceStatusChangedEvent;
        }

        private void StoreStatusHandlerOnSpaceStatusChangedEvent(object sender, Space args)
        {
            CrestronConsole.PrintLine($"Received Status Change Event for Space {args.SpaceId} with Mode {args.SpaceMode}");
                
            ControlSystem.StoreFronts[args.SpaceId] = new StoreFront()
            {
                SpaceId = args.SpaceId, 
                SpaceMode = args.MemberId == CardReader.MemberId ? SpaceMode.MySpace : args.SpaceMode, 
                MemberId = args.MemberId, 
                MemberName = args.MemberName
            };

            switch (args.SpaceId)
            {
                case "A":
                    _tsw770.UShortInput[(ushort)UI_Actions.AnalogJoins.StoreFrontMode1].UShortValue = ControlSystem.StoreFronts[args.SpaceId].GetModeColor();
                    break;
                case "B":
                    _tsw770.UShortInput[(ushort)UI_Actions.AnalogJoins.StoreFrontMode2].UShortValue = ControlSystem.StoreFronts[args.SpaceId].GetModeColor();
                    break;
                case "C":
                    _tsw770.UShortInput[(ushort)UI_Actions.AnalogJoins.StoreFrontMode3].UShortValue = ControlSystem.StoreFronts[args.SpaceId].GetModeColor();
                    break;
                case "D":
                    _tsw770.UShortInput[(ushort)UI_Actions.AnalogJoins.StoreFrontMode4].UShortValue = ControlSystem.StoreFronts[args.SpaceId].GetModeColor();
                    break;
                case "E":
                    _tsw770.UShortInput[(ushort)UI_Actions.AnalogJoins.StoreFrontMode5].UShortValue = ControlSystem.StoreFronts[args.SpaceId].GetModeColor();
                    break;
                case "F":
                    _tsw770.UShortInput[(ushort)UI_Actions.AnalogJoins.StoreFrontMode6].UShortValue = ControlSystem.StoreFronts[args.SpaceId].GetModeColor();
                    break;
            }
        }

        private void _tsw770_OnlineStatusChange(GenericBase currentdevice, OnlineOfflineEventArgs args)
        {
            _timer = new CTimer(UpdateTime, null, 0, 1000);
            
            if(!args.DeviceOnLine) return;

            _tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.VPubLoginAck].UserObject = new List<Action<bool>>();

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
                
                _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberAccessMessage].StringValue = CardReader.MembershipIsValid ? "Access Granted" : "Access Denied";
                _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberName].StringValue = CardReader.MemberName;
                _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberPubId].StringValue = CardReader.MemberId;
                _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberExpireDate].StringValue = CardReader.MemberExpiryDateTime.ToString("dd MMMM yyyy");
            });
            
            // Login Success - Reservation Start
            ((List<Action<bool>>)_tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.VPubLoginAck].UserObject).Add(b =>
            {
                if(!b) return;
                CrestronConsole.PrintLine("VPubLoginAck");
                CrestronConsole.PrintLine($"MembershipIsValid: {CardReader.MembershipIsValid}");
                if (CardReader.MembershipIsValid)
                {
                    _tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.MessagePage].BoolValue = false;
                    _tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.OperatingPage].BoolValue = true;
                    UI_Actions.TogglePopup(_tsw770, UI_Actions.PopupsJoinGroup["ClosePopUps"][0]);
                    UI_Actions.TogglePopup(_tsw770, UI_Actions.PopupsJoinGroup["Operation_Storefronts"][0]);
                    
                    _startLoginTime = DateTime.Now;
                    _loginTimer.Start();
                    
                    Task.Run(LoginTimer_Elapsed, _cancellationToken);

                    // Start Fans in Store
                    foreach (var fan in ControlSystem.MyStore.Fans)
                    {
                        CrestronConsole.PrintLine($"Start Fan: {fan}");
                    }
                    
                    var storeStatusUpdate = new InquiryRequest();

                    ControlSystem.StoreFronts[ControlSystem.SpaceId].SpaceMode = SpaceMode.MySpace;
                    ControlSystem.StoreFronts[ControlSystem.SpaceId].MemberId = CardReader.MemberId;
                    ControlSystem.StoreFronts[ControlSystem.SpaceId].MemberName = CardReader.MemberName;
                    storeStatusUpdate.UpdateStoreStatusRequest(ControlSystem.IpAddress, ControlSystem.StoreFronts[ControlSystem.SpaceId]);
                    foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
                    {
                        storeStatusUpdate.UpdateStoreStatusRequest(storesIpAddress.ToString(), ControlSystem.StoreFronts[ControlSystem.SpaceId]);
                    }
                }
            });

            // Reservation End
            _tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.VPubLogout].UserObject = new Action<bool>(b =>
            {
                _tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.OperatingPage].BoolValue = false;
                _tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.MessagePage].BoolValue = true;
                
                _loginTimer.Stop();
                _loginTimer.Reset();
                _cancellationTokenSource.Cancel();
            });
            
            // Subpage Joins
            foreach (var popup in UI_Actions.PopupsJoinGroup.Values.SelectMany(joins => joins.Where(j => j > 0)))
            {
                if(_tsw770.BooleanOutput[popup].UserObject == null)
                    _tsw770.BooleanOutput[popup].UserObject = new List<Action<bool>>();
                ((List<Action<bool>>)_tsw770.BooleanOutput[popup].UserObject)?.Add(b =>
                {
                    if (!b) return;
                    UI_Actions.TogglePopup(_tsw770, popup);
                });
            }
        }
        
        private void LoginTimer_Elapsed()
        {
            while (_loginTimer.IsRunning)
            {
                _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.TimerHours].StringValue = _loginTimer.Elapsed.Hours.ToString("00");
                _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.TimerMinutes].StringValue = _loginTimer.Elapsed.Minutes.ToString("00");
                _tsw770.StringInput[(ushort)UI_Actions.SerialJoins.TimerSeconds].StringValue = _loginTimer.Elapsed.Seconds.ToString("00");
                Thread.Sleep(1000);
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
                    
                    var actionList = args.Sig.UserObject as List<Action<bool>>;
                    actionList?.ForEach(a => a.Invoke(args.Sig.BoolValue));
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