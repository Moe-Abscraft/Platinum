using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.WebScripting;
using Mohammad_Hadizadeh_Certificate_Platinum.HGVR;
using Newtonsoft.Json;

namespace Mohammad_Hadizadeh_Certificate_Platinum
{
    public class Inquiry_Server
    {
        private readonly HttpCwsServer _server;

        public Inquiry_Server()
        {
            _server = new HttpCwsServer("/api");
            _server.ReceivedRequestEvent += _server_ReceivedRequestEvent;
            _server.HttpRequestHandler = new DefaultHttpCwsRequestHandler();

            HttpCwsRoute route_1 = new HttpCwsRoute("member_inquiry")
            {
                Name = "member_inquiry",
                RouteHandler = new MemberInquiryHandler()
            };
            _server.Routes.Add(route_1);

            HttpCwsRoute route_2 = new HttpCwsRoute("store_status")
            {
                Name = "store_status",
                RouteHandler = new StoreStatusHandler()
            };
            _server.Routes.Add(route_2);

            HttpCwsRoute route_3 = new HttpCwsRoute("workspace_status")
            {
                Name = "workspace_status",
                RouteHandler = new WorkspaceStatusHandler()
            };
            _server.Routes.Add(route_3);

            HttpCwsRoute route_4 = new HttpCwsRoute("queue_status")
            {
                Name = "queue_status",
                RouteHandler = new QueueStatusHandler()
            };
            _server.Routes.Add(route_4);

            HttpCwsRoute route_5 = new HttpCwsRoute("wall_status")
            {
                Name = "wall_status",
                RouteHandler = new WallStatusHandler()
            };
            _server.Routes.Add(route_5);

            HttpCwsRoute route_6 = new HttpCwsRoute("quirkyTech_status")
            {
                Name = "quirkyTech_status",
                RouteHandler = new QuirkyTechHandler()
            };
            _server.Routes.Add(route_6);
            
            HttpCwsRoute route_7 = new HttpCwsRoute("storeId_inquiry")
            {
                Name = "storeId_inquiry",
                RouteHandler = new StoreIdInquiryHandler()
            };
            _server.Routes.Add(route_7);

            _server.Register();
        }

        private static void _server_ReceivedRequestEvent(object sender, HttpCwsRequestEventArgs args)
        {
            // CrestronConsole.PrintLine("Received Request");
            // CrestronConsole.PrintLine(args.Context.Request.HttpMethod);
            // CrestronConsole.PrintLine(args.Context.Request.RouteData.Route.Name);
        }
    }

    public class MemberInquiryHandler : IHttpCwsHandler
    {
        public void ProcessRequest(HttpCwsContext context)
        {
            if (context.Request.HttpMethod == "GET")
            {
                using (var sr = new StreamReader(context.Request.InputStream))
                {
                    var data = sr.ReadToEnd();
                    CrestronConsole.PrintLine(data);
                }

                if (context.Request.RouteData.Route.Name == "member_inquiry")
                {
                    try
                    {
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "OK";
                        context.Response.AppendHeader("Content-Type", "application/json");
                        context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                        context.Response.Write(
                            JsonConvert.SerializeObject(new InquiryResponseModel() { MemberId = CardReader.MemberId }),
                            true);
                        context.Response.End();
                    }
                    catch (Exception e)
                    {
                        CrestronConsole.PrintLine(e.Message);
                    }
                }
            }
        }
    }

    public class QueueStatusHandler : IHttpCwsHandler
    {
        public void ProcessRequest(HttpCwsContext context)
        {
            if (context.Request.HttpMethod == "GET")
            {
            }
            else if (context.Request.HttpMethod == "POST")
            {
                if (context.Request.RouteData.Route.Name == "queue_status")
                {
                    using (var sr = new StreamReader(context.Request.InputStream))
                    {
                        try
                        {
                            var data = sr.ReadToEnd();
                            CrestronConsole.PrintLine($"Request: Adding space {data} to the queue");
                            // data= "workspace","storefront"
                            var queuedata = data.Split(',');
                            var workSpace = ControlSystem.WorkSpaces[queuedata[0]];
                            var storeFront = ControlSystem.StoreFronts[queuedata[1]];
                            RentalService.WorkspaceStorefrontQueue(workSpace, storeFront, null);
                        }
                        catch (Exception e)
                        {
                            CrestronConsole.PrintLine(e.Message);
                        }
                    }

                    try
                    {
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "QueueUpdate:OK";
                        context.Response.AppendHeader("Content-Type", "application/json");
                        context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                        context.Response.Write("OK", true);
                        context.Response.End();
                    }
                    catch (Exception e)
                    {
                        CrestronConsole.PrintLine(e.Message);
                    }
                }
            }
        }
    }

    public class WallStatusHandler : IHttpCwsHandler
    {
        public void ProcessRequest(HttpCwsContext context)
        {
            if (context.Request.HttpMethod == "GET")
            {
            }
            else if (context.Request.HttpMethod == "POST")
            {
                if (context.Request.RouteData.Route.Name == "wall_status")
                {
                    using (var sr = new StreamReader(context.Request.InputStream))
                    {
                        try
                        {
                            var data = sr.ReadToEnd();
                            var buildingStatus = JsonConvert.DeserializeObject<BuildingStatus>(data);
                            foreach (var wall in buildingStatus.Walls)
                            {
                                HGVRConfigurator.UpdateWallStatus(wall.Key, wall.Value);
                            }

                            foreach (var fan in buildingStatus.Fans)
                            {
                                HGVRConfigurator.UpdateFanStatus(fan.Key, fan.Value);
                            }
                        }
                        catch (Exception e)
                        {
                            CrestronConsole.PrintLine(e.Message);
                        }
                    }

                    try
                    {
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "BuildingStatusUpdate:OK";
                        context.Response.AppendHeader("Content-Type", "application/json");
                        context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                        context.Response.Write("OK", true);
                        context.Response.End();
                    }
                    catch (Exception e)
                    {
                        CrestronConsole.PrintLine(e.Message);
                    }
                }
            }
        }
    }

    public class QuirkyTechHandler : IHttpCwsHandler
    {
        public void ProcessRequest(HttpCwsContext context)
        {
            if (context.Request.HttpMethod == "GET")
            {
            }
            else if (context.Request.HttpMethod == "POST")
            {
                if (context.Request.RouteData.Route.Name == "quirkyTech_status")
                {
                    using (var sr = new StreamReader(context.Request.InputStream))
                    {
                        try
                        {
                            var data = sr.ReadToEnd();
                            var quirkyTechStatus = JsonConvert.DeserializeObject<QuirkyTechStatus>(data);
                            UI.QuirkyTechStatus = quirkyTechStatus.IsBusy;
                        }
                        catch (Exception e)
                        {
                            CrestronConsole.PrintLine(e.Message);
                        }
                    }

                    try
                    {
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "QuirkyTechStatusUpdate:OK";
                        context.Response.AppendHeader("Content-Type", "application/json");
                        context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                        context.Response.Write("OK", true);
                        context.Response.End();
                    }
                    catch (Exception e)
                    {
                        CrestronConsole.PrintLine(e.Message);
                    }
                }
            }
        }
    }

    public class StoreStatusHandler : IHttpCwsHandler
    {
        public static event EventHandler<Space> SpaceStatusChangedEvent = delegate { };

        protected virtual void OnSpaceStatusChangedEvent(Space e)
        {
            SpaceStatusChangedEvent?.Invoke(this, e);
        }

        public void ProcessRequest(HttpCwsContext context)
        {
            if (context.Request.HttpMethod == "POST")
            {
                if (context.Request.RouteData.Route.Name == "store_status")
                {
                    using (var sr = new StreamReader(context.Request.InputStream))
                    {
                        try
                        {
                            var data = sr.ReadToEnd();
                            CrestronConsole.PrintLine(data);
                            var storeStatus = JsonConvert.DeserializeObject<StoreFront>(data);
                            OnSpaceStatusChangedEvent(storeStatus);
                            //ControlSystem.StoreFronts[storeStatus.SpaceId] = storeStatus;
                        }
                        catch (Exception e)
                        {
                            CrestronConsole.PrintLine(e.Message);
                        }
                    }

                    try
                    {
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "OK";
                        context.Response.AppendHeader("Content-Type", "application/json");
                        context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                        context.Response.Write("OK", true);
                        context.Response.End();
                    }
                    catch (Exception e)
                    {
                        CrestronConsole.PrintLine(e.Message);
                    }
                }
            }
            else if (context.Request.HttpMethod == "GET")
            {
                if (context.Request.RouteData.Route.Name == "store_status")
                {
                    try
                    {
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "OK";
                        context.Response.AppendHeader("Content-Type", "application/json");
                        context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                        context.Response.Write(
                            JsonConvert.SerializeObject(ControlSystem.StoreFronts[ControlSystem.SpaceId]), true);
                        context.Response.End();
                    }
                    catch (Exception e)
                    {
                        CrestronConsole.PrintLine(e.Message);
                    }
                }
            }
        }
    }

    public class WorkspaceStatusHandler : IHttpCwsHandler
    {
        public static event EventHandler<Space> WorkspaceStatusChangedEvent = delegate { };

        protected virtual void OnWorkspaceStatusChangedEvent(Space e)
        {
            WorkspaceStatusChangedEvent?.Invoke(this, e);
        }

        public void ProcessRequest(HttpCwsContext context)
        {
            if (context.Request.HttpMethod == "POST")
            {
                if (context.Request.RouteData.Route.Name == "workspace_status")
                {
                    using (var sr = new StreamReader(context.Request.InputStream))
                    {
                        try
                        {
                            var data = sr.ReadToEnd();
                            CrestronConsole.PrintLine(data);
                            var storeStatus = JsonConvert.DeserializeObject<WorkSpace>(data);
                            OnWorkspaceStatusChangedEvent(storeStatus);
                            //ControlSystem.StoreFronts[storeStatus.SpaceId] = storeStatus;
                        }
                        catch (Exception e)
                        {
                            CrestronConsole.PrintLine(e.Message);
                        }
                    }

                    try
                    {
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "OK";
                        context.Response.AppendHeader("Content-Type", "application/json");
                        context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                        context.Response.Write("OK", true);
                        context.Response.End();
                    }
                    catch (Exception e)
                    {
                        CrestronConsole.PrintLine(e.Message);
                    }
                }
            }
            else if (context.Request.HttpMethod == "GET")
            {
                if (context.Request.RouteData.Route.Name == "workspace_status")
                {
                    try
                    {
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "OK";
                        context.Response.AppendHeader("Content-Type", "application/json");
                        context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                        var myWorkSpaces =
                            (from store in Configurator.Stores
                                where !store.IS_STOREFRONT
                                where ControlSystem.WorkSpaces[store.SPACE_ID] != null
                                where ControlSystem.WorkSpaces[store.SPACE_ID].SpaceMode == SpaceMode.MySpace
                                select ControlSystem.WorkSpaces[store.SPACE_ID]).ToList();
                        context.Response.Write(JsonConvert.SerializeObject(myWorkSpaces), true);
                        context.Response.End();
                    }
                    catch (Exception e)
                    {
                        CrestronConsole.PrintLine(e.Message);
                    }
                }
            }
        }
    }
    
    public class StoreIdInquiryHandler : IHttpCwsHandler
    {
        public void ProcessRequest(HttpCwsContext context)
        {
            if (context.Request.HttpMethod == "GET")
            {
                using (var sr = new StreamReader(context.Request.InputStream))
                {
                    var data = sr.ReadToEnd();
                    CrestronConsole.PrintLine(data);
                }

                if (context.Request.RouteData.Route.Name == "storeId_inquiry")
                {
                    try
                    {
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "OK";
                        context.Response.AppendHeader("Content-Type", "application/json");
                        context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                        context.Response.Write(
                            JsonConvert.SerializeObject(new StoreIdInquiryResponseModel() { StoreId = ControlSystem.SpaceId }),
                            true);
                        context.Response.End();
                    }
                    catch (Exception e)
                    {
                        CrestronConsole.PrintLine(e.Message);
                    }
                }
            }
        }
    }

    public class DefaultHttpCwsRequestHandler : IHttpCwsHandler
    {
        public void ProcessRequest(HttpCwsContext context)
        {
        }
    }

    public class InquiryRequest
    {
        private HttpClient _client;

        HttpClientRequest _request = new HttpClientRequest()
        {
            KeepAlive = false
        };

        public string GetMemberInquiryRequest(string host)
        {
            try
            {
                InquiryResponseModel responseModel;
                using (_client = new HttpClient())
                {
                    _request.Url = new UrlParser("http://" + host + "/cws/api/member_inquiry");
                    _request.Header.ContentType = "application/json";
                    _request.RequestType = RequestType.Get;

                    using (var response = _client.Dispatch(_request))
                    {
                        responseModel = JsonConvert.DeserializeObject<InquiryResponseModel>(response.ContentString);
                        CrestronConsole.PrintLine(responseModel.MemberId);
                    }
                }

                return responseModel.MemberId;
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine(e.Message);
                return "";
            }
        }

        public void UpdateStoreStatusRequest(string host, StoreFront storeFront)
        {
            try
            {
                using (_client = new HttpClient())
                {
                    _request.Url = new UrlParser("http://" + host + "/cws/api/store_status");
                    _request.Header.ContentType = "application/json";
                    _request.RequestType = RequestType.Post;

                    _request.ContentString = JsonConvert.SerializeObject(storeFront);

                    using (var response = _client.Dispatch(_request))
                    {
                        CrestronConsole.PrintLine(response.ContentString);
                    }
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine(e.Message);
            }
        }

        public StoreFront GetStoreStatusRequest(string host, StoreFront storeFront)
        {
            try
            {
                using (_client = new HttpClient())
                {
                    _request.Url = new UrlParser("http://" + host + "/cws/api/store_status");
                    _request.Header.ContentType = "application/json";
                    _request.RequestType = RequestType.Get;
                    _request.ContentString = storeFront.SpaceId;

                    using (var response = _client.Dispatch(_request))
                    {
                        var status = JsonConvert.DeserializeObject<StoreFront>(response.ContentString);
                        return status;
                    }
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine(e.Message);
                return null;
            }
        }

        public List<WorkSpace> GetWorkSpaces(string host)
        {
            try
            {
                using (_client = new HttpClient())
                {
                    _request.Url = new UrlParser("http://" + host + "/cws/api/workspace_status");
                    _request.Header.ContentType = "application/json";
                    _request.RequestType = RequestType.Get;

                    using (var response = _client.Dispatch(_request))
                    {
                        var workspaces = JsonConvert.DeserializeObject<List<WorkSpace>>(response.ContentString);
                        return workspaces;
                    }
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine(e.Message);
                return null;
            }
        }

        public void UpdateWorkspaceStatusRequest(string host, WorkSpace workSpace)
        {
            try
            {
                using (_client = new HttpClient())
                {
                    _request.Url = new UrlParser("http://" + host + "/cws/api/workspace_status");
                    _request.Header.ContentType = "application/json";
                    _request.RequestType = RequestType.Post;

                    _request.ContentString = JsonConvert.SerializeObject(workSpace);

                    using (var response = _client.Dispatch(_request))
                    {
                        CrestronConsole.PrintLine(response.ContentString);
                    }
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine(e.Message);
            }
        }

        public void UpdateQueueStatusRequest(string host, WorkSpace workSpace, StoreFront storeFront)
        {
            try
            {
                using (_client = new HttpClient())
                {
                    _request.Url = new UrlParser("http://" + host + "/cws/api/queue_status");
                    _request.Header.ContentType = "application/json";
                    _request.RequestType = RequestType.Post;

                    _request.ContentString = workSpace.SpaceId + "," + storeFront.SpaceId;

                    using (var response = _client.Dispatch(_request))
                    {
                        CrestronConsole.PrintLine(response.ContentString);
                    }
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine(e.Message);
            }
        }

        public void UpdateBuildingStatusRequest(string host, Dictionary<ushort, bool> walls,
            Dictionary<ushort, bool> fans)
        {
            try
            {
                using (_client = new HttpClient())
                {
                    _request.Url = new UrlParser("http://" + host + "/cws/api/wall_status");
                    _request.Header.ContentType = "application/json";
                    _request.RequestType = RequestType.Post;

                    var buildingStatus = new BuildingStatus()
                    {
                        Walls = walls,
                        Fans = fans
                    };

                    _request.ContentString = JsonConvert.SerializeObject(buildingStatus);

                    using (var response = _client.Dispatch(_request))
                    {
                        CrestronConsole.PrintLine(response.ContentString);
                    }
                }
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine(e.Message);
            }
        }
        
        public string GetStoreIdInquiryRequest(string host)
        {
            try
            {
                StoreIdInquiryResponseModel responseModel;
                using (_client = new HttpClient())
                {
                    _request.Url = new UrlParser("http://" + host + "/cws/api/storeId_inquiry");
                    _request.Header.ContentType = "application/json";
                    _request.RequestType = RequestType.Get;

                    using (var response = _client.Dispatch(_request))
                    {
                        responseModel = JsonConvert.DeserializeObject<StoreIdInquiryResponseModel>(response.ContentString);
                        CrestronConsole.PrintLine(responseModel.StoreId);
                    }
                }

                return responseModel.StoreId;
            }
            catch (Exception e)
            {
                CrestronConsole.PrintLine(e.Message);
                return "";
            }
        }
    }

    public class InquiryResponseModel
    {
        public string MemberId { get; set; }
    }

    public class BuildingStatus
    {
        public Dictionary<ushort, bool> Walls { get; set; }
        public Dictionary<ushort, bool> Fans { get; set; }
    }

    public class QuirkyTechStatus
    {
        public bool IsBusy { get; set; }
    }
    
    public class StoreIdInquiryResponseModel
    {
        public string StoreId { get; set; }
    }
}