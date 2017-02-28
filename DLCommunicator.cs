using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace SpeechInterface
{
    internal class DirectLineCommunicator
    {
        private const string BaseUrl = "https://directline.botframework.com";
        private const string Key = "<Insert Your DirectLine Key Here>";
        private static string _startConversation = "/v3/directline/conversations/";
        private const string DeviceName = "<Name Your Device Here>";

        public HttpResponseMessage StartConversation()
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Key);
            HttpResponseMessage response = httpClient.PostAsync(_startConversation, null).Result;
            return response;
        }

        public HttpResponseMessage SendActivity(string message, string convoId)
        {
            string getSendActivity = _startConversation + convoId + "/activities";
            var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Key);
            var content = new SendRequest
            {
                type = "message",
                from = new From() { id = DeviceName },
                text = message
            };
            string requestcontent = JsonConvert.SerializeObject(content);
            HttpResponseMessage response =
                httpClient.PostAsync(getSendActivity, new StringContent(requestcontent, Encoding.UTF8, "application/json")).Result;
            return response;
        }

        public HttpResponseMessage GetActivities(string convoId)
        {
            string getSendActivity = _startConversation + convoId + "/activities";
            var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Key);
            HttpResponseMessage response =
                httpClient.GetAsync(getSendActivity, HttpCompletionOption.ResponseContentRead).Result;
            return response;
        }
    }

    internal class StartConvo
    {
        public string conversationId;
        public string token;
        public string expires_in;
        public string streamUrl;
    }

    internal class SendActivity
    {
        public string id;
    }

    internal class GetActivity
    {
        public Activity[] activities;
        public string watermark;
    }

    internal class Activity
    {
        public string type;
        public string channelId;
        public Conversation conversation;
        public string id;
        public From from;
        public string text;
    }

    internal class Conversation
    {
        public string id;
    }

    internal class From
    {
        public string id;
    }

    internal class SendRequest
    {
        public string type;
        public From from;
        public string text;
    }

}
