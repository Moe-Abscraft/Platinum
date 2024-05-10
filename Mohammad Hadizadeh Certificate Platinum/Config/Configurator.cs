using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.Media;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class Configurator
    {
        private readonly string _configFile;
        private string _manifestIpAddress;
        private string _manifestPort;
        private string _manifestStoresFile;
        private string _manifestRetailFile;
        private string _buildingIpAddress;
        private string _buildingPort;
        private string _orderIpAddress;
        private string _orderPort;
        private string _rate;

        private HttpClient _manifestClient;
        private HttpClientResponse _manifestResponse;

        public static Store[] Stores = new Store[] { };
        public Retail[] Retail = new Retail[] { };

        public static string BuildingIpAddress;
        public static int BuildingPort;
        
        public static string OrderIpAddress;
        public static int OrderPort;

        public Configurator()
        {
            _configFile = Path.Combine(Directory.GetApplicationRootDirectory(), "/user/profile.csv");
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_configFile)) return;
                using (var reader = new StreamReader(_configFile))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (line == null) continue;
                        var values = line.Split(',');
                        var key = values[0];
                        var value = new string[values.Length - 1];
                        Array.Copy(values, 1, value, 0, values.Length - 1);
                        switch (key)
                        {
                            case "#manifest":
                                _manifestIpAddress = value[0];
                                _manifestPort = value[1];
                                _manifestStoresFile = value[2];
                                _manifestRetailFile = value[3];
                                break;
                            case "#building":
                                _buildingIpAddress = value[0];
                                _buildingPort = value[1];
                                break;
                            case "#order":
                                _orderIpAddress = value[0];
                                _orderPort = value[1];
                                break;
                            case "#rate":
                                _rate = value[0];
                                break;
                        }
                    }
                }

                CrestronConsole.PrintLine($"Manifest IP: {_manifestIpAddress}");
                CrestronConsole.PrintLine($"Manifest Port: {_manifestPort}");
                CrestronConsole.PrintLine($"Manifest Stores File: {_manifestStoresFile}");
                CrestronConsole.PrintLine($"Manifest Retail File: {_manifestRetailFile}");
                CrestronConsole.PrintLine($"Building IP: {_buildingIpAddress}");
                CrestronConsole.PrintLine($"Building Port: {_buildingPort}");
                CrestronConsole.PrintLine($"Order IP: {_orderIpAddress}");
                CrestronConsole.PrintLine($"Order Port: {_orderPort}");
                CrestronConsole.PrintLine($"Rate: {_rate}");

                ControlSystem.Rate = float.Parse(_rate);

                BuildingIpAddress = _buildingIpAddress;
                BuildingPort = int.Parse(_buildingPort);
                
                OrderIpAddress = _orderIpAddress;
                OrderPort = int.Parse(_orderPort);

                GetManifest(_manifestStoresFile);
                GetManifest(_manifestRetailFile);
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error reading config file: {e.Message}");
            }
        }

        private void GetManifest(string file)
        {
            try
            {
                // var manifestUrl = $"http://{_manifestIpAddress}:{_manifestPort}/{_manifestStoresFile}";
                // var manifestUrl = $"http://gui.abscraft.ca:{_manifestPort}/{file}";
                var manifestUrl = $"http://{_manifestIpAddress}:{_manifestPort}/{file}";
                var manifestClientRequest = new HttpClientRequest
                {
                    RequestType = RequestType.Get,
                    KeepAlive = false
                };
                manifestClientRequest.Header.SetHeaderValue("Content-Type", "application/json");
                manifestClientRequest.Url = new UrlParser(manifestUrl);

                using (_manifestClient = new HttpClient())
                {
                    using (_manifestResponse = _manifestClient.Dispatch(manifestClientRequest))
                    {
                        if (_manifestResponse.Code != 200)
                        {
                            CrestronConsole.PrintLine($"Error getting manifest: {_manifestResponse.Code}");
                            return;
                        }

                        // CrestronConsole.PrintLine($"Manifest: {_manifestResponse.ContentString}");
                        if (file == _manifestStoresFile)
                        {
                            if (ValidateManifestStore(_manifestResponse.ContentString))
                            {
                                Stores = JArray.Parse(_manifestResponse.ContentString).ToObject<Store[]>();

                                ControlSystem.StoreFronts = new StoreFronts(Stores.Count(s => s.IS_STOREFRONT));
                                ControlSystem.WorkSpaces = new WorkSpaces(Stores.Count(s => !s.IS_STOREFRONT));

                                var i = 0;
                                var j = 0;
                                foreach (var store in Stores)
                                {
                                    CrestronConsole.PrintLine($"Store: {store.SPACE_ID}");
                                    
                                    if (MacAddressNormalize(store.MACADDR) == MacAddressNormalize(ControlSystem.MacAddress))
                                    {
                                        ControlSystem.MyStore = store;
                                        ControlSystem.SpaceId = store.SPACE_ID;
                                        ControlSystem.SpaceDecor = store.SPACE_DECOR;
                                        ControlSystem.NumOfStoresAvailable = Stores.Count(s => s.IS_STOREFRONT);
                                        ControlSystem.NumOfStoresOpen = Stores.Count(s => s.OPEN);
                                    }

                                    if (store.IS_STOREFRONT)
                                    {
                                        ControlSystem.StoreFronts[store.SPACE_ID] = new StoreFront()
                                        {
                                            SpaceId = store.SPACE_ID, 
                                            SpaceMode = SpaceMode.Closed,
                                            MemberName = "",
                                            MemberId = "",
                                            Area = float.Parse(store.AREA)
                                        };

                                        i++;
                                        switch (store.SPACE_ID)
                                        {
                                            case "A":
                                                store.Fans = new ushort[] { 1, 3 };
                                                store.Walls = new ushort[] { 1 };
                                                break;
                                            case "B":
                                                store.Fans = new ushort[] { 4 };
                                                store.Walls = new ushort[] { 3 };
                                                break;
                                            case "C":
                                                store.Fans = new ushort[] { 6, 8 };
                                                store.Walls = new ushort[] { 5 };
                                                break;
                                            case "D":
                                                store.Fans = new ushort[] { 9 };
                                                store.Walls = new ushort[] { 7 };
                                                break;
                                            case "E":
                                                store.Fans = new ushort[] { 11, 13 };
                                                store.Walls = new ushort[] { 9 };
                                                break;
                                            case "F":
                                                store.Fans = new ushort[] { 14, 16 };
                                                store.Walls = new ushort[] { 11 };
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        ControlSystem.WorkSpaces[store.SPACE_ID] = new WorkSpace()
                                        {
                                            SpaceId = store.SPACE_ID, 
                                            SpaceMode = SpaceMode.Available,
                                            MemberName = "",
                                            MemberId = "",
                                            AdjacentStorefrontId = Stores[int.Parse(store.SPACE_ID) - 1].SPACE_ID,
                                            StorefrontQueue = new CrestronQueue<string>(),
                                            Area = float.Parse(store.AREA)
                                        };
                                        
                                        switch (store.SPACE_ID)
                                        {
                                            case "1":
                                                store.Fans = new ushort[] { 2 };
                                                store.Walls = new ushort[] { 2 };
                                                ControlSystem.WorkSpaces[store.SPACE_ID].AdjacentWorkSpaces = new string[] { "2" };
                                                break;
                                            case "2":
                                                store.Fans = new ushort[] { 5 };
                                                store.Walls = new ushort[] { 2, 4 };
                                                ControlSystem.WorkSpaces[store.SPACE_ID].AdjacentWorkSpaces = new string[] { "1", "3" };
                                                break;
                                            case "3":
                                                store.Fans = new ushort[] { 7 };
                                                store.Walls = new ushort[] { 4, 6 };
                                                ControlSystem.WorkSpaces[store.SPACE_ID].AdjacentWorkSpaces = new string[] { "2", "4" };
                                                break;
                                            case "4":
                                                store.Fans = new ushort[] { 10 };
                                                store.Walls = new ushort[] { 6, 8 };
                                                ControlSystem.WorkSpaces[store.SPACE_ID].AdjacentWorkSpaces = new string[] { "3", "5" };
                                                break;
                                            case "5":
                                                store.Fans = new ushort[] { 12 };
                                                store.Walls = new ushort[] { 8, 10 };
                                                ControlSystem.WorkSpaces[store.SPACE_ID].AdjacentWorkSpaces = new string[] { "4", "6" };
                                                break;
                                            case "6":
                                                store.Fans = new ushort[] { 15 };
                                                store.Walls = new ushort[] { 10 };
                                                ControlSystem.WorkSpaces[store.SPACE_ID].AdjacentWorkSpaces = new string[] { "5" };
                                                break;
                                        }
                                    }
                                }
                                return;
                            }

                            CrestronConsole.PrintLine($"Error validating manifest: Stores");
                        }
                        else
                        {
                            if (ValidateManifestRetail(_manifestResponse.ContentString))
                            {
                                if (Shopping.ShoppingItems == null) Shopping.ShoppingItems = new List<Retail>();
                                Retail = JArray.Parse(_manifestResponse.ContentString).ToObject<Retail[]>();
                                foreach (var retail in Retail)
                                {
                                    CrestronConsole.PrintLine($"Retail: {retail.UPC}");
                                    Shopping.ShoppingItems.Add(new Retail()
                                    {
                                        VENDOR = retail.VENDOR,
                                        UPC = retail.UPC,
                                        PRODUCT = retail.PRODUCT,
                                        PRICE = retail.PRICE
                                    });
                                }
                                ControlSystem.NumOfMarketItemsAvailable = Retail.Count(r => r.UPC != null);
                                return;
                            }
                            
                            CrestronConsole.PrintLine($"Error validating manifest: Retail");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error getting manifest: {e.Message}");
            }
            finally
            {
                _manifestClient = null;
                _manifestResponse = null;
            }
        }

        // create a method to validate the manifest data and return a list of stores based on the Stores class space_id property type can be string or integer
        private bool ValidateManifestStore(string stores)
        {
            JsonSchema schema = JsonSchema.Parse(@"{
                        'type': 'array',
                        'items': {
                            'type': 'object',
                            'properties': {
                                'SPACE_ID': {'type': ['string', 'integer'], length: 1},
                                'SPACE_DECOR': {'type': 'string', maxLength: 50},
                                'IS_STOREFRONT': {'type': 'boolean'},
                                'MACADDR': {'type': 'string'},
                                'AREA': {'type': 'string', precision: 3},
                                'OPEN': {'type': 'boolean'}
                            }
                        }
                    }
                }
            }");

            try
            {
                var obj = JArray.Parse(stores);
                var isValid = obj.IsValid(schema);
                return isValid;
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error validating manifest: {e.Message}");
                return false;
            }
        }

        private bool ValidateManifestRetail(string retail)
        {
            JsonSchema schema = JsonSchema.Parse(@"{
                        'type': 'array',
                        'items': {
                            'type': 'object',  
                            'properties': {
                                'UPC': {'type': 'string', maxLength: 20},
                                'VENDOR': {'type': 'string', maxLength: 50},
                                'PRODUCT': {'type': 'string', maxLength: 50},
                                'PRICE': {'type': 'string', precision: 2}
                            }
                        }
                    }
                }
            }");

            try
            {
                var obj = JArray.Parse(retail);
                var isValid = obj.IsValid(schema);
                return isValid;
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error validating manifest: {e.Message}");
                return false;
            }
        }
        
        private string MacAddressNormalize(string macAddress)
        {
            try
            {
                var normalizedMacAddress = macAddress
                    .Replace(",", "")
                    .Replace(":", "")
                    .Replace(".", "")
                    .Replace("-", "")
                    .Replace(" ", "")
                    .ToUpper();

                if (normalizedMacAddress.Length != 12)
                {
                    return string.Empty;
                }

                return normalizedMacAddress;
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine($"Error normalizing mac address: {e.Message}");
                return string.Empty;
            }
        }
    }
}