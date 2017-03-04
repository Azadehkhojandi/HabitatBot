using System;
using System.Collections.Generic;
using System.Configuration;
using System.EnterpriseServices.Internal;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

namespace HabitatBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                var replyText = await MakeReplyText(activity.Text);

                // return our reply to the user
                Activity reply = activity.CreateReply(replyText);
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

        public string PredefinedQuestions(string message)
        {
            var sitecoreSearchTextEndPoint = ConfigurationManager.AppSettings["SitecoreSearchTextEndPoint"];
            var qa = new Dictionary<string, string>
            {
                {"who built this bot", "team red staffy: Az,Zhen,Budi"},
                {"bot info", $" SitecoreSearchTextEndPoint: {sitecoreSearchTextEndPoint}"}
            };

            var answer = qa.FirstOrDefault(x => message.Contains(x.Key));
            if (answer.Value != null)
            {
                return answer.Value;
            }
            return null;

        }
        async Task<HttpResponseMessage> DetectSentiment(string message)
        {
            return await PostResquestToAzureTextAnalytics(message, "sentiment");
        }

        async Task<HttpResponseMessage> DetectKeyPhrases(string message)
        {
            return await PostResquestToAzureTextAnalytics(message, "keyPhrases");
        }

        private static async Task<HttpResponseMessage> PostResquestToAzureTextAnalytics(string message, string action)
        {
            var uri = $"https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/{action}";
            var client = new HttpClient();

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ConfigurationManager.AppSettings["TextAnalyticsKey"]);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var input = new BatchInput
            {
                Documents = new List<DocumentInput>
                {
                    new DocumentInput
                    {
                        Id = 1,
                        Text = message,
                    }
                }
            };
            var json = JsonConvert.SerializeObject(input);
            var postResult = await client.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));

            return postResult;
        }



        async Task<string> MakeReplyText(string message)
        {

            var replyText = PredefinedQuestions(message);
            if (!string.IsNullOrEmpty(replyText))
            {
                return replyText;
            }

            //Check sentiment of message
            var sentimentPost = await DetectSentiment(message);
            var sentimentRawResponse = await sentimentPost.Content.ReadAsStringAsync();
            var sentimentJsonResponse = JsonConvert.DeserializeObject<BatchSentimentResult>(sentimentRawResponse);
            var sentimentScore = sentimentJsonResponse?.Documents?.FirstOrDefault()?.Score ?? 0;

            //TODO Check sentiment and respond accordingly
            string sentimentText;
            if (sentimentScore > 0.7)
            {
                sentimentText = $"";
            }
            else if (sentimentScore < 0.3)
            {
                sentimentText = $"";
            }
            else
            {
                sentimentText = $"";
            }

            //
            //Check keywords of message
            var keyPhrasesPost = await DetectKeyPhrases(message);
            var keyPhrasesRawResponse = await keyPhrasesPost.Content.ReadAsStringAsync();
            var keyPhrasesJsonResponse = JsonConvert.DeserializeObject<BatchKeyPhrasesResul>(keyPhrasesRawResponse);
            var keyPhrases = keyPhrasesJsonResponse?.Documents?.FirstOrDefault()?.KeyPhrases;


            replyText = $"sentimentScore: {sentimentScore} , {(keyPhrases != null ? string.Join(",", keyPhrases.ToArray()) : "")} ";
            return replyText;
        }

    }
    public class BatchKeyPhrasesResul
    {
        public List<DocumentKeyPhrasesResult> Documents { get; set; }
        public List<object> Errors { get; set; }
    }
    public class BatchSentimentResult
    {
        public List<DocumentSentimentResult> Documents { get; set; }
        public List<object> Errors { get; set; }
    }
    public class DocumentSentimentResult
    {
        public int Id { get; set; }
        public double Score { get; set; }
    }
    public class DocumentKeyPhrasesResult
    {
        public int Id { get; set; }
        public List<string> KeyPhrases { get; set; }
    }

    public class BatchInput
    {
        public List<DocumentInput> Documents { get; set; }
    }

    public class DocumentInput
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }
}