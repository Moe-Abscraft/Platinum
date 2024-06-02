using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.Fusion;
using Crestron.SimplSharpPro.UI;
using Mohammad_Hadizadeh_Certificate_Platinum.HGVR;
using Directory = Crestron.SimplSharp.CrestronIO.Directory;
using Thread = Crestron.SimplSharpPro.CrestronThread.Thread;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class UI
    {
        public readonly Tsw770 Tsw770;
        private DateTime _now = DateTime.Now;
        private CTimer _timer;
        private DateTime _startLoginTime;
        private Stopwatch _loginTimer;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        private readonly CardReader _cardReader;

        private int _page = 1;
        private bool _viewCart = false;

        private InquiryRequest _inquiryRequest;
        private string _vendor;
        private readonly List<string> _vendorList = new List<string>();
        private HGVRConfigurator _roomConfigurator;
        private QuirkyTech _quirkyTech;
        public static bool QuirkyTechStatus = false;
        private TransportTcpIpServer _cardReaderServer;

        public UI(ControlSystem cs, InquiryRequest inquiryRequest, TransportTcpIpServer cardReaderServer)
        {
            _inquiryRequest = inquiryRequest;
            _roomConfigurator = new HGVRConfigurator(_inquiryRequest);
            _quirkyTech = new QuirkyTech(cs);
            _cardReaderServer = cardReaderServer;
            _cardReaderServer.DataReceived += _cardReaderServer_DataReceived;

            Tsw770 = new Tsw770(0x2A, cs);
            Tsw770.SigChange += _tsw770_SigChange;
            Tsw770.OnlineStatusChange += _tsw770_OnlineStatusChange;

            var sgdFile = Path.Combine(Directory.GetApplicationDirectory(), "UI\\VPUB-TSW770.sgd");
            if (File.Exists(sgdFile))
            {
                Tsw770.LoadSmartObjects(sgdFile);
                foreach (var smartObject in Tsw770.SmartObjects)
                {
                    // CrestronConsole.PrintLine("Smart Object {0} loaded", smartObject.Value.ID);
                    smartObject.Value.SigChange += _tsw770_SmartGraphicsSigChange;
                }
            }

            if (Tsw770.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                ErrorLog.Error("Unable to register TSW770");

            _cardReader = new CardReader();

            // Card Reader Events Member Is Not Expired
            _cardReader.MemberIsNotExpired += (sender, args) =>
            {
                UI_Actions.TogglePopup(Tsw770, UI_Actions.PopupsJoinGroup["Message_Pop"][0]);
                Tsw770.BooleanInput[(ushort)UI_Actions.VisibilityJoins.VPubLoginOk].BoolValue = true;
                UI_Actions.KeypadInput("", "Misc_1");
            };

            _cardReader.MemberIsExpired += (sender, args) =>
            {
                UI_Actions.TogglePopup(Tsw770, UI_Actions.PopupsJoinGroup["Message_Pop"][0]);
                Tsw770.BooleanInput[(ushort)UI_Actions.VisibilityJoins.VPubLoginOk].BoolValue = true;
                UI_Actions.KeypadInput("", "Misc_1");
            };

            _loginTimer = new Stopwatch();
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            StoreStatusHandler.SpaceStatusChangedEvent += StoreStatusHandlerOnSpaceStatusChangedEvent;
            WorkspaceStatusHandler.WorkspaceStatusChangedEvent += WorkspaceStatusHandlerOnWorkspaceStatusChangedEvent;

            _roomConfigurator.TemperatureChanged += (sender, args) =>
            {
                Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.Temperature].StringValue =
                    args.Temperature.ToString(CultureInfo.InvariantCulture);
            };
            
            Initialize();
        }

        private void _cardReaderServer_DataReceived(object sender, MessageEventArgs e)
        {
            CrestronConsole.PrintLine($"Member Card Number: {e.Message}");
            ControlSystem.ReservationStatus = false;
            var memberInfo = _cardReader.GetMemberInfo(ushort.Parse(e.Message));
            UI_Actions.KeypadInput("", "Misc_1");
            CrestronConsole.PrintLine($"Member Info: {memberInfo}");

            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberAccessMessage].StringValue =
                CardReader.MembershipIsValid ? "Access Granted" : "Access Denied";
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberName].StringValue = CardReader.MemberName;
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberPubId].StringValue = CardReader.MemberId;
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberExpireDate].StringValue =
                "Exp. " + CardReader.MemberExpiryDateTime.ToString("dd MMMM yyyy");
        }

        private void WorkspaceStatusHandlerOnWorkspaceStatusChangedEvent(object sender, Space args)
        {
            try
            {
                var workSpace = ControlSystem.WorkSpaces[args.SpaceId];
                workSpace.SpaceMode = args.MemberId == CardReader.MemberId ? SpaceMode.MySpace : args.SpaceMode;
                workSpace.MemberId = args.MemberId;
                workSpace.MemberName = args.MemberName;
                workSpace.AssignedStoreFrontId = ((WorkSpace)args).AssignedStoreFrontId;
                
                if(args.MemberId.Length == 0 || CardReader.MemberId.Length == 0)
                {
                    workSpace.SpaceMode = args.SpaceMode;
                }
                // check the queue for the new assignment

                Task.Run(() => AssignWorkspaceFromQueue(workSpace));

                CrestronConsole.PrintLine($"Workspace Mode: {ControlSystem.WorkSpaces[args.SpaceId].SpaceMode}");
                UI_Actions.SetStoreMode(Tsw770, args.SpaceId);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error in WorkspaceStatusHandlerOnWorkspaceStatusChangedEvent: {e.Message}");
            }
        }

        private void AssignWorkspaceFromQueue(WorkSpace workSpace)
        {
            if (workSpace.SpaceMode == SpaceMode.Available)
            {
                if (workSpace.StorefrontQueue.Any())
                {
                    var nextStorefrontId = workSpace.StorefrontQueue.Dequeue();
                    var nextStorefront = ControlSystem.StoreFronts[nextStorefrontId];
                    if(ControlSystem.MyStore.SPACE_ID == nextStorefrontId)
                        RentalService.RentSpace(nextStorefront, workSpace, _inquiryRequest);
                }
            }
        }

        private void StoreStatusHandlerOnSpaceStatusChangedEvent(object sender, Space args)
        {
            CrestronConsole.PrintLine(
                $"Received Status Change Event for Space {args.SpaceId} with Mode {args.SpaceMode}");
            CrestronConsole.PrintLine(
                $"Member ID: {args.MemberId} Member Name: {args.MemberName} My Member ID: {CardReader.MemberId}");

            var area = ControlSystem.StoreFronts[args.SpaceId].Area;
            ControlSystem.StoreFronts[args.SpaceId] = null;
            ControlSystem.StoreFronts[args.SpaceId] = new StoreFront()
            {
                SpaceId = args.SpaceId,
                SpaceMode = args.MemberId == CardReader.MemberId ? SpaceMode.MySpace : args.SpaceMode,
                MemberId = args.MemberId,
                MemberName = args.MemberName,
                Area = area
            };
            
            if(args.MemberId.Length == 0 || CardReader.MemberId.Length == 0)
            {
                ControlSystem.StoreFronts[args.SpaceId].SpaceMode = args.SpaceMode;
            }

            CrestronConsole.PrintLine($"Space Mode: {ControlSystem.StoreFronts[args.SpaceId].SpaceMode}");

            UI_Actions.SetStoreMode(Tsw770, args.SpaceId);
        }


        private void _tsw770_OnlineStatusChange(GenericBase currentdevice, OnlineOfflineEventArgs args)
        {
            
        }

        private void Initialize()
        {
            _timer = new CTimer(UpdateTime, null, 0, 1000);
            
            Tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.VPubLoginAck].UserObject = new List<Action<bool>>();

            // Serial Joins
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.SpaceId].StringValue =
                $"Storefront {ControlSystem.SpaceId}";
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.Decorator].StringValue = $"{ControlSystem.SpaceDecor}";
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.OsVersion].StringValue = $"{ControlSystem.OsVersion}";
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.IpAddress].StringValue = $"{ControlSystem.IpAddress}";
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MacAddress].StringValue =
                $"{ControlSystem.MacAddress.ToUpper()}";
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontAvailable].StringValue =
                $"{ControlSystem.NumOfStoresAvailable}";
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.StorefrontTotal].StringValue =
                $"{ControlSystem.NumOfStoresOpen}";
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MarketItemAvailable].StringValue =
                $"{ControlSystem.NumOfMarketItemsAvailable}";

            // Smart Object Joins
            foreach (var smartObjectBooleanOutput in Tsw770.SmartObjects[1].BooleanOutput)
            {
                smartObjectBooleanOutput.UserObject = new Action<bool>(b =>
                {
                    if (!b) return;
                    CardReader.CardNumber =
                        UI_Actions.KeypadInput(CardReader.CardNumber.ToString(), smartObjectBooleanOutput.Name);
                    Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.KeypadInput].StringValue =
                        CardReader.CardNumber > 0 ? CardReader.CardNumber.ToString() : "";
                    CrestronConsole.PrintLine(CardReader.CardNumber.ToString());
                });
            }

            // Filter Items
            foreach (var item in Shopping.ShoppingItems)
            {
                if (_vendorList.Contains(item.VENDOR)) continue;
                _vendorList.Add(item.VENDOR);
            }

            for (int i = 0; i < _vendorList.Count; i++)
            {
                Tsw770.SmartObjects[2].StringInput[$"Set Item {i + 1} Text"].StringValue = _vendorList[i];
            }

            Tsw770.SmartObjects[2].UShortInput["Set Number of Items"].UShortValue = (ushort)_vendorList.Count;

            foreach (var smartObjectBooleanOutput in Tsw770.SmartObjects[2].BooleanOutput)
            {
                smartObjectBooleanOutput.UserObject = new Action<bool>(b =>
                {
                    if (!b) return;
                    _vendor = _vendorList[(int)(smartObjectBooleanOutput.Number - 11)];
                    CrestronConsole.PrintLine(_vendor);
                    UpdateShoppingListView(_viewCart, _vendor);
                });
            }

            // Boolean Joins
            Tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.VPubLoginEnter].UserObject = new Action<bool>(b =>
            {
                if (!b) return;
                ControlSystem.ReservationStatus = false;
                var memberInfo = _cardReader.GetMemberInfo(CardReader.CardNumber);
                UI_Actions.KeypadInput("", "Misc_1");
                CrestronConsole.PrintLine($"Member Info: {memberInfo}");

                Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberAccessMessage].StringValue =
                    CardReader.MembershipIsValid ? "Access Granted" : "Access Denied";
                Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberName].StringValue = CardReader.MemberName;
                Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberPubId].StringValue = CardReader.MemberId;
                Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberExpireDate].StringValue =
                    "Exp. " + CardReader.MemberExpiryDateTime.ToString("dd MMMM yyyy");
            });

            foreach (var workSpaceSelectJoin in UI_Actions.WorkSpaceSelectJoins)
            {
                Tsw770.BooleanOutput[workSpaceSelectJoin.Value].UserObject = new Action<bool>(b =>
                {
                    if (!b) return;
                    var workspace = ControlSystem.WorkSpaces[workSpaceSelectJoin.Key];
                    var storeFront = ControlSystem.StoreFronts[ControlSystem.SpaceId];

                    RentalService.RentSpace(storeFront, workspace, _inquiryRequest);
                    UI_Actions.SetStoreMode(Tsw770, workspace.SpaceId);
                });
            }

            Tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.ShoppingListCheckout].UserObject = new Action<bool>(
                b =>
                {
                    if (!b) return;
                    
                    foreach (var shoppingItem in Shopping.ShoppingCart)
                    {
                        RentalService.TotalShoppingCharge += float.Parse(shoppingItem.PRICE);
                    }
                    var newCharge = RentalService.GetTotalCharge(_loginTimer);
                    Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.TotalCharge].StringValue = newCharge.ToString(CultureInfo.InvariantCulture);
                    CrestronConsole.PrintLine($"Charge: {newCharge}");
                    
                    // RentalService.TotalShoppingCharge = 0;
                    
                    QuirkyTech.SendOrder(Shopping.ShoppingCart);
                    Shopping.ShoppingCart.Clear();
                    _viewCart = false;
                    Tsw770.BooleanInput[(ushort)UI_Actions.DigitalJoins.ViewShoppingCart].BoolValue = _viewCart;
                    _page = 1;
                    Tsw770.UShortInput[(ushort)UI_Actions.AnalogJoins.ShoppingItemsMode].UShortValue =
                        _viewCart ? (ushort)2 : (ushort)1;
                    UpdateShoppingListView(_viewCart, string.Empty);
                    Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.TotalShoppingItemsInCart].StringValue =
                        string.Empty;
                });

            // Login Success - Reservation Start
            ((List<Action<bool>>)Tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.VPubLoginAck].UserObject).Add(b =>
            {
                try
                {
                    if (!b) return;
                    if (ControlSystem.ReservationStatus)
                    {
                        //UI_Actions.TogglePopup(Tsw770, UI_Actions.PopupsJoinGroup["ClosePopUps"][0]);
                        Tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.OperatingPage].BoolValue = false;
                        Tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.MessagePage].BoolValue = true;
                        ControlSystem.ReservationStatus = false;
                        CardReader.ResetCard();
                        return;
                    }
                    CrestronConsole.PrintLine("VPubLoginAck");
                    CrestronConsole.PrintLine($"MembershipIsValid: {CardReader.MembershipIsValid}");

                    if (CardReader.MembershipIsValid)
                    {
                        Tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.MessagePage].BoolValue = false;
                        Tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.OperatingPage].BoolValue = true;
                        UI_Actions.TogglePopup(Tsw770, UI_Actions.PopupsJoinGroup["ClosePopUps"][0]);
                        UI_Actions.TogglePopup(Tsw770, UI_Actions.PopupsJoinGroup["Operation_Storefronts"][0]);
                        Tsw770.BooleanInput[UI_Actions.PopupsJoinGroup["Operation_Storefronts"][0]].BoolValue = true;

                        _startLoginTime = DateTime.Now;
                        if (_loginTimer != null)
                        {
                            _loginTimer.Reset();
                            _loginTimer = null;
                        }
                        _loginTimer = new Stopwatch();
                        
                        if(_cancellationTokenSource != null)
                        {
                            _cancellationTokenSource.Dispose();
                            _cancellationTokenSource = null;
                        }
                        _cancellationTokenSource = new CancellationTokenSource();
                        _cancellationToken = _cancellationTokenSource.Token;
                        
                         _loginTimer.Start();
                         Task.Run(LoginTimer_Elapsed, _cancellationToken);
                         
                        ControlSystem.StoreFronts[ControlSystem.SpaceId].SpaceMode = SpaceMode.Occupied;
                        ControlSystem.StoreFronts[ControlSystem.SpaceId].MemberId = CardReader.MemberId;
                        ControlSystem.StoreFronts[ControlSystem.SpaceId].MemberName = CardReader.MemberName;

                        foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
                        {
                            _inquiryRequest.UpdateStoreStatusRequest(storesIpAddress.ToString(),
                                ControlSystem.StoreFronts[ControlSystem.SpaceId]);
                        }

                        _inquiryRequest.UpdateStoreStatusRequest(ControlSystem.IpAddress,
                             ControlSystem.StoreFronts[ControlSystem.SpaceId]);
                        // Start Fans in Store
                        HGVRConfigurator.TurnOnFans(ControlSystem.MyStore.Fans);
                        HGVRConfigurator.OpenWalls(ControlSystem.MyStore.Walls);
                        foreach (var fan in ControlSystem.MyStore.Fans)
                        {
                            CrestronConsole.PrintLine($"Start Fan: {fan}");
                        }

                        // Order system
                        QuirkyTech.StartRentalService(ControlSystem.SpaceId);
                        QuirkyTech.SetDigitalSignageMessage(ControlSystem.SpaceId, CardReader.MemberName);
                    }

                    Tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.StorefrontsPage].BoolValue = true;
                    _page = 1;
                    Tsw770.UShortInput[(ushort)UI_Actions.AnalogJoins.ShoppingItemsMode].UShortValue = 1;
                    Tsw770.BooleanInput[(ushort)UI_Actions.DigitalJoins.ShoppingListFilterOn].BoolValue = true;
                    Tsw770.BooleanInput[(ushort)UI_Actions.DigitalJoins.ShoppingListFilterOff].BoolValue = false;
                    
                    ControlSystem.ReservationStatus = true;
                }
                catch (Exception e)
                {
                    CrestronConsole.PrintLine($"Error in VPubLoginAck: {e.Message}");
                }
            });

            // Reservation End
            Tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.VPubLogout].UserObject = new Action<bool>(b =>
            {
                try
                {
                    if (!b) return;
                    Tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.OperatingPage].BoolValue = false;
                    Tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.MessagePage].BoolValue = true;

                    //var storeStatusUpdate = new InquiryRequest();

                    ControlSystem.StoreFronts[ControlSystem.SpaceId].SpaceMode = SpaceMode.Available;
                    ControlSystem.StoreFronts[ControlSystem.SpaceId].MemberId = "";
                    ControlSystem.StoreFronts[ControlSystem.SpaceId].MemberName = "";

                    // Order system
                    QuirkyTech.EndRentalService(ControlSystem.SpaceId);
                    QuirkyTech.DeleteDigitalSignageMessage(ControlSystem.SpaceId, "Welcome to your space");

                    // Update status of the assigned workspaces to available
                    if (ControlSystem.StoreFronts[ControlSystem.SpaceId].AssignedWorkSpaces != null)
                    {
                        foreach (var space in ControlSystem.StoreFronts[ControlSystem.SpaceId].AssignedWorkSpaces)
                        {
                            space.SpaceMode = SpaceMode.Available;
                            space.MemberName = "";
                            space.MemberId = "";

                            _inquiryRequest.UpdateWorkspaceStatusRequest(ControlSystem.IpAddress, space);
                            foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
                            {
                                _inquiryRequest.UpdateWorkspaceStatusRequest(storesIpAddress.ToString(), space);
                            }
                        }
                    }

                    for (int i = 1; i <= 6; i++)
                    {
                        var workSpace = ControlSystem.WorkSpaces[i.ToString()];
                        var storeFront = ControlSystem.StoreFronts[ControlSystem.SpaceId];
                        if(workSpace != null)
                        {
                            if (workSpace.StorefrontQueue != null)
                            {
                                if (workSpace.StorefrontQueue.Contains(ControlSystem.SpaceId))
                                {
                                    RentalService.WorkspaceStorefrontQueue(workSpace, storeFront, _inquiryRequest, "remove");
                                }
                            }
                        }
                    }
                    
                    // Stop Fans in Store
                    HGVRConfigurator.TurnOffFans(ControlSystem.MyStore.Fans);
                    HGVRConfigurator.CloseWalls(ControlSystem.MyStore.Walls);

                    foreach (var storesIpAddress in ControlSystem.StoresIpAddresses)
                    {
                        _inquiryRequest.UpdateStoreStatusRequest(storesIpAddress.ToString(),
                            ControlSystem.StoreFronts[ControlSystem.SpaceId]);
                    }
                    _inquiryRequest.UpdateStoreStatusRequest(ControlSystem.IpAddress,
                        ControlSystem.StoreFronts[ControlSystem.SpaceId]);

                    ControlSystem.StoreFronts[ControlSystem.SpaceId].AssignedWorkSpaces?.Clear();

                    _loginTimer.Stop();
                    _loginTimer.Reset();
                    _cancellationTokenSource.Cancel();

                    Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberExpireDate].StringValue =
                        "Total Charges " + RentalService.TotalCharge;
                    Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.MemberAccessMessage].StringValue =
                        "Reservation Ended";
                    Tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.VPubLoginMessage].BoolValue = true;
                    RentalService.TotalCharge = 0;
                    // ControlSystem.ReservationStatus = false;
                    // CardReader.ResetCard();
                    CrestronConsole.PrintLine("Logged Out");
                }
                catch (Exception e)
                {
                    CrestronConsole.PrintLine($"Error in VPubLogout: {e.Message}");
                }
            });

            // Subpage Joins
            foreach (var popup in UI_Actions.PopupsJoinGroup.Values.SelectMany(joins => joins.Where(j => j > 0)))
            {
                if (Tsw770.BooleanOutput[popup].UserObject == null)
                    Tsw770.BooleanOutput[popup].UserObject = new List<Action<bool>>();
                ((List<Action<bool>>)Tsw770.BooleanOutput[popup].UserObject)?.Add(b =>
                {
                    if (!b) return;
                    UI_Actions.TogglePopup(Tsw770, popup);

                    if (popup == UI_Actions.PopupsJoinGroup["Message_KP"][0])
                    {
                        if (CardReader.CardNumber > 0)
                        {
                            // CardReader.CardNumber = 0;
                            CardReader.ResetCard();
                        }
                        Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.KeypadInput].StringValue =
                            CardReader.CardNumber > 0 ? CardReader.CardNumber.ToString() : "";
                    }
                    
                    _vendor = string.Empty;
                    Tsw770.BooleanInput[(ushort)UI_Actions.DigitalJoins.ShoppingListFilterOn].BoolValue = true;
                    Tsw770.BooleanInput[(ushort)UI_Actions.DigitalJoins.ShoppingListFilterOff].BoolValue = false;
                    UpdateShoppingListView(_viewCart, string.Empty);
                });
            }

            // Shopping Items Control
            Tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.ShoppingListUp].UserObject = new Action<bool>(b =>
            {
                if (!b) return;
                _page += 1;
                UpdateShoppingListView(_viewCart, string.Empty);
            });

            Tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.ShoppingListDn].UserObject = new Action<bool>(b =>
            {
                if (!b) return;
                _page -= 1;
                UpdateShoppingListView(_viewCart, string.Empty);
            });

            for (uint i = 81; i <= 85; i++)
            {
                var i1 = i;
                Tsw770.BooleanOutput[i].UserObject = new Action<bool>(b =>
                {
                    if (!b) return;
                    UpdateShoppingCart(i1 - 80, _vendor);
                });
            }

            Tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.ViewShoppingCart].UserObject = new Action<bool>(b =>
            {
                if (!b) return;
                _viewCart = !_viewCart;
                Tsw770.BooleanInput[(ushort)UI_Actions.DigitalJoins.ViewShoppingCart].BoolValue = _viewCart;
                _page = 1;
                Tsw770.UShortInput[(ushort)UI_Actions.AnalogJoins.ShoppingItemsMode].UShortValue =
                    _viewCart ? (ushort)2 : (ushort)1;
                UpdateShoppingListView(_viewCart, string.Empty);
            });

            // Vendor List Filter
            Tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.ShoppingListFilterOn].UserObject =
                new Action<bool>(b =>
                    {
                        if (!b) return;
                        _page = 1;
                        _vendor = string.Empty;
                        Tsw770.BooleanInput[(ushort)UI_Actions.DigitalJoins.ShoppingListFilterOn].BoolValue = true;
                        Tsw770.BooleanInput[(ushort)UI_Actions.DigitalJoins.ShoppingListFilterOff].BoolValue = false;
                        UpdateShoppingListView(_viewCart, string.Empty);
                    }
                );

            Tsw770.BooleanOutput[(ushort)UI_Actions.DigitalJoins.ShoppingListFilterOff].UserObject =
                new Action<bool>(b =>
                    {
                        if (!b) return;
                        _page = 1;
                        _vendor = string.Empty;
                        Tsw770.BooleanInput[(ushort)UI_Actions.DigitalJoins.ShoppingListFilterOn].BoolValue = false;
                        Tsw770.BooleanInput[(ushort)UI_Actions.DigitalJoins.ShoppingListFilterOff].BoolValue = true;
                        // UpdateShoppingListView(_viewCart, string.Empty);
                    }
                );
            
            Tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.OperatingPage].BoolValue = false;
            Tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.MessagePage].BoolValue = true;
            Tsw770.BooleanInput[(ushort)UI_Actions.SubpageJoins.VPubLoginMessage].BoolValue = false;
        }
        private void LoginTimer_Elapsed()
        {
            while (_loginTimer.IsRunning)
            {
                Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.TimerHours].StringValue =
                    _loginTimer.Elapsed.Hours.ToString("00");
                Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.TimerMinutes].StringValue =
                    _loginTimer.Elapsed.Minutes.ToString("00");
                Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.TimerSeconds].StringValue =
                    _loginTimer.Elapsed.Seconds.ToString("00");
                if (_loginTimer.Elapsed.Seconds == 0)
                {
                    CrestronConsole.PrintLine($"Login Timer: {_loginTimer.Elapsed.Minutes} minutes - Calculating Charges...");
                    var newCharge = RentalService.GetTotalCharge(_loginTimer);
                    Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.TotalCharge].StringValue = newCharge.ToString(CultureInfo.InvariantCulture);
                    CrestronConsole.PrintLine($"Charge: {newCharge}");
                }

                Thread.Sleep(1000);
            }
        }

        private void UpdateTime(object userspecific)
        {
            _now = DateTime.Now;
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.Date].StringValue = _now.ToString("dddd, dd MMMM, yyyy");
            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.Time].StringValue = _now.ToString("HH:mm:ss");
        }

        private void UpdateStoreFrontInfo(StoreFront storeFront)
        {
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

        public void UpdateShoppingListView(bool viewCart, string vendor)
        {
            CrestronConsole.PrintLine(_page.ToString());
            if (_page < 1) _page = 1;
            if (_page > Shopping.ShoppingItems.Count / 5) _page = Shopping.ShoppingItems.Count / 5;

            List<Retail> items;
            if (viewCart)
                items = Shopping.GetCartList(_page);
            else if (vendor.Length < 1)
                items = Shopping.GetShoppingList(_page);
            else
            {
                items = Shopping.GetShoppingList(_page, vendor);
            }

            foreach (var item in items)
            {
                CrestronConsole.PrintLine(item.PRODUCT);
            }

            for (var i = 0; i < 5; i++)
            {
                Tsw770.StringInput[(ushort)(61 + i)].StringValue = string.Empty;
                Tsw770.StringInput[(ushort)(81 + i)].StringValue = string.Empty;
                Tsw770.StringInput[(ushort)(91 + i)].StringValue = string.Empty;

                Tsw770.BooleanInput[(ushort)(101 + i)].BoolValue = false;
            }

            var count = items.Count > 5 ? 5 : items.Count;
            for (var i = 0; i < count; i++)
            {
                Tsw770.StringInput[(ushort)(61 + i)].StringValue = items[i] != null ? items[i].VENDOR : string.Empty;
            }

            for (var i = 0; i < count; i++)
            {
                Tsw770.StringInput[(ushort)(81 + i)].StringValue = items[i] != null ? items[i].PRODUCT : string.Empty;
            }

            for (var i = 0; i < count; i++)
            {
                Tsw770.StringInput[(ushort)(91 + i)].StringValue = items[i] != null ? items[i].PRICE : string.Empty;
            }

            for (int i = 0; i < count; i++)
            {
                Tsw770.BooleanInput[(ushort)(101 + i)].BoolValue = true;
            }
        }

        public void UpdateShoppingCart(uint item, string vendor)
        {
            var itemIndex = (_page * 5 - 5) + item - 1;
            if (!_viewCart)
                if (string.IsNullOrEmpty(vendor))
                    Shopping.AddToCart((int)itemIndex);
                else
                {
                    Shopping.AddToCart((int)itemIndex, vendor);
                }
            else
            {
                Shopping.RemoveFromCart((int)itemIndex);
                UpdateShoppingListView(_viewCart, string.Empty);
            }

            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.TotalShoppingItemsInCart].StringValue =
                Shopping.ShoppingCart.Count.ToString();

            Tsw770.StringInput[(ushort)UI_Actions.SerialJoins.TotalCharge].StringValue =
                RentalService.GetTotalCharge(_loginTimer).ToString(CultureInfo.InvariantCulture);
        }

        public void Dispose()
        {
            _cardReaderServer.DataReceived -= _cardReaderServer_DataReceived;
            Tsw770.SigChange -= _tsw770_SigChange;
            Tsw770.OnlineStatusChange -= _tsw770_OnlineStatusChange;
            Tsw770.Dispose();
            _timer?.Dispose();
            _quirkyTech?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}