using System;
using System.Collections.Generic;
using System.Configuration;
using System.EnterpriseServices.Internal;
using System.Linq;
using System.Net;
using System.Net.Http;
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


                var message = predefinedQuestions(activity.Text);
                if (string.IsNullOrEmpty(message))
                {
                    // calculate something for us to return
                    int length = (activity.Text ?? string.Empty).Length;
                    message = $"You sent {activity.Text} which was {length} characters";
                }

                // return our reply to the user
                Activity reply = activity.CreateReply(message);
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

        public string predefinedQuestions(string message)
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

    }

    public class BatchResult
    {
        public List<DocumentResult> Documents { get; set; }
        public List<object> Errors { get; set; }
    }
    public class DocumentResult
    {
        public int Id { get; set; }
        public double Score { get; set; }
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