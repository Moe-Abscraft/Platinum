using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.WebScripting;
using Mohammad_Hadizadeh_Certificate_Platinum.StoreFronts;
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
            if(context.Request.HttpMethod == "GET")
            {
                using (var sr = new StreamReader(context.Request.InputStream))
                {
                    var data = sr.ReadToEnd();
                    CrestronConsole.PrintLine(data);
                }
                
                if(context.Request.RouteData.Route.Name == "member_inquiry")
                {
                    try
                    {
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "OK";
                        context.Response.AppendHeader("Content-Type", "application/json");
                        context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                        context.Response.Write(JsonConvert.SerializeObject(new InquiryResponseModel() {MemberId = CardReader.MemberId}) , true);
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
        public static event EventHandler<Space> SpaceStatusChangedEvent = delegate {  };
        protected virtual void OnSpaceStatusChangedEvent(Space e)
        {
            SpaceStatusChangedEvent?.Invoke(this, e);
        }
        public void ProcessRequest(HttpCwsContext context)
        {
            if(context.Request.HttpMethod == "POST")
            {
                if(context.Request.RouteData.Route.Name == "store_status")
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
                        context.Response.Write("OK" , true);
                        context.Response.End();
                    }
                    catch (Exception e)
                    {
                        CrestronConsole.PrintLine(e.Message);
                    }
                }
            }
            else if(context.Request.HttpMethod == "GET")
            {
                if(context.Request.RouteData.Route.Name == "store_status")
                {
                    try
                    {
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "OK";
                        context.Response.AppendHeader("Content-Type", "application/json");
                        context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                        context.Response.Write(JsonConvert.SerializeObject(ControlSystem.StoreFronts[ControlSystem.SpaceId]) , true);
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
    }
        
    public class InquiryResponseModel
    {
        public string MemberId { get; set; }
    }
}