using System;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.WebScripting;
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
            
            HttpCwsRoute route = new HttpCwsRoute("inquiry")
            {
                Name = "member_inquiry",
                RouteHandler = new MemberInquiryHandler()
            };
            _server.Routes.Add(route);
            
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

    public class DefaultHttpCwsRequestHandler : IHttpCwsHandler
    {
        public void ProcessRequest(HttpCwsContext context)
        {
            
        }
    }
    
    public class MemberInquiryRequest
    {
        private HttpClient _client;
        HttpClientRequest _request = new HttpClientRequest()
        {
            RequestType = RequestType.Get,
            KeepAlive = false
        };
        
        public string GetMemberInquiryRequest(string host)
        {
            try
            {
                InquiryResponseModel responseModel;
                using (_client = new HttpClient())
                {
                    _request.Url = new UrlParser("http://" + host + "/cws/api/inquiry");
                    _request.Header.ContentType = "application/json";
                
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
    }
        
    public class InquiryResponseModel
    {
        public string MemberId { get; set; }
    }
}