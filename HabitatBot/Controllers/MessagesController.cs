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
using System.Web.UI.WebControls;
using HabitatBot.Models;
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
                var connector = new ConnectorClient(new Uri(activity.ServiceUrl));

             

                var reply= await MakeReply(activity);

             

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

        private async Task<HttpResponseMessage> PostResquestToAzureTextAnalytics(string message, string action)
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

        async Task<Activity> MakeReply(Activity message)
        {



            //            {
            //                "type": "message",
            //  "text": "Sample with a hero card",
            //  "attachments": [
            //    {
            //      "contentType": "application/vnd.microsoft.card.hero",
            //      "content": {
            //        "title": "I'm a hero card",
            //        "subtitle": "Please visit my site.",
            //        "images": [
            //          {
            //            "url": "https://mydeploy.azurewebsites.net/matsu.jpg"
            //          }
            //        ],
            //        "buttons": [
            //          {
            //            "type": "openUrl",
            //            "title": "Go to my site",
            //            "value": "https://blogs.msdn.microsoft.com/tsmatsuz"
            //          }
            //        ]
            //      }
            //    }
            //  ]
            //}


            Activity reply=null;
            string replyText = null;

            var luis = await DetectEntityFromLuis(message.Text);
            if (luis.TopScoringIntent != null)
            {
                switch (luis.TopScoringIntent.Intent)
                {
                    case "Hi":
                         replyText = $"Hi there :), how can I help you?";
                         reply = message.CreateReply(replyText);
                      
                        break;
                    case "Search":
                        if (Request.RequestUri.Host.Contains("localhost"))
                        {
                            //sitecore api hosted locally
                            //Call api
                        }
                        else
                        {
                            replyText =
                                "I know you searched for the content but our sitecore endpoint is hosted locally and you need to test this feature with bot emoulator.";
                            reply = message.CreateReply(replyText);
                        }
                        break;
                    case "Who":
                        var replyToConversation = message.CreateReply("This bot is built by:");
                        replyToConversation.Recipient = message.From;
                        replyToConversation.Type = "message";
                        replyToConversation.Attachments = new List<Attachment>();
                        var card1 = CreateCardMessageOneButtonOneImage("https://pbs.twimg.com/profile_images/764455063892791299/D3h9i0qI.jpg",@"See more", "https://azadehkhojandi.blogspot.com.au/", "Azadeh Khojandi","");
                        var card2 = CreateCardMessageOneButtonOneImage("https://pbs.twimg.com/profile_images/794145230110887937/K8avquW0.jpg", @"See more", "https://zhenyuan.azurewebsites.net/", "Zhen Yuan", "");
                        var card3 = CreateCardMessageOneButtonOneImage("https://abs.twimg.com/sticky/default_profile_images/default_profile_4_200x200.png", @"See more", "https://twitter.com/budi4w4n", "Budiawan Muliawan", "");
                        replyToConversation.Attachments.Add(card1);
                        replyToConversation.Attachments.Add(card2);
                        replyToConversation.Attachments.Add(card3);

                        reply = replyToConversation;
                        break;
                    case "Help":
                        replyText = $"You can search or ask me a question?" + Environment.NewLine + "Like what is habiat? or What are habitat features?" + Environment.NewLine + " you can ask me to analyse your text to see how smart I'm ;)?";
                        reply = message.CreateReply(replyText);
                        break;
                    case "Analyse":
                        replyText = "Here is what I can tell you:" +
                                    Environment.NewLine + $"{await GetMessageSentimentScoreText(message.Text)} and {await GetMessageKeyPhrasesText(message.Text)}";
                        reply = message.CreateReply(replyText);
                        break;
                    default:
                        replyText = "I didn't understand your command but here is what I can tell you:" +
                                    Environment.NewLine + $"{await GetMessageSentimentScoreText(message.Text)} and {await GetMessageKeyPhrasesText(message.Text)}";
                        reply = message.CreateReply(replyText);
                        break;
                }
            }
            if ( reply == null)
            {
                replyText = "I didn't understand your command may be start by typeing help";
                reply = message.CreateReply(replyText);
            }
            return reply;



        }

        private static Attachment CreateCardMessageOneButtonOneImage(string imageUrl, string buttonTitle, string buttonUrl,string heroTitle, string heroSubtitle)
        {
            var cardImages = new List<CardImage>
            {
                new CardImage(url: imageUrl)
            };
            var cardButtons = new List<CardAction>();
            var plButton = new CardAction()
            {
                Value = buttonUrl,
                Type = "openUrl",
                Title = buttonTitle
            };
            cardButtons.Add(plButton);
            var plCard = new HeroCard()
            {
                Title = heroTitle,
                Subtitle = heroSubtitle,
                Images = cardImages,
                Buttons = cardButtons
            };
            var plAttachment = plCard.ToAttachment();
            return plAttachment;
        }

        private async Task<List<string>> GetMessageKeyPhrases(string message)
        {
            //Check keywords of message
            var keyPhrasesPost = await DetectKeyPhrases(message);
            var keyPhrasesRawResponse = await keyPhrasesPost.Content.ReadAsStringAsync();
            var keyPhrasesJsonResponse = JsonConvert.DeserializeObject<BatchKeyPhrasesResul>(keyPhrasesRawResponse);
            var keyPhrases = keyPhrasesJsonResponse?.Documents?.FirstOrDefault()?.KeyPhrases;
            return keyPhrases;
        }

        private async Task<double> GetMessageSentimentScore(string message)
        {
            //Check sentiment of message
            var sentimentPost = await DetectSentiment(message);
            var sentimentRawResponse = await sentimentPost.Content.ReadAsStringAsync();
            var sentimentJsonResponse = JsonConvert.DeserializeObject<BatchSentimentResult>(sentimentRawResponse);
            var sentimentScore = sentimentJsonResponse?.Documents?.FirstOrDefault()?.Score ?? 0;

            return sentimentScore;
        }

        private async Task<string> GetMessageSentimentScoreText(string message)
        {
            var sentimentScore = await GetMessageSentimentScore(message);
            //TODO Check sentiment and respond accordingly
            string sentimentText;
            if (sentimentScore > 0.7)
            {
                sentimentText = $"It's a positive statement";
            }
            else if (sentimentScore < 0.3)
            {
                sentimentText = $"unfortunately,It's a negavtive statement";
            }
            else
            {
                sentimentText = $"It's a neutral statement";
            }
            return $"{sentimentText} and your sentimentScore is {sentimentScore}";
        }

        private async Task<string> GetMessageKeyPhrasesText(string message)
        {
            var keyPhrases = await GetMessageKeyPhrases(message);


            return $"key phrases I found: {(keyPhrases != null ? string.Join(",", keyPhrases.ToArray()) : "")} ";
            
        }

        private static async Task<HabitatLuis> DetectEntityFromLuis(string query)
        {
            query = Uri.EscapeDataString(query);
            var result = new HabitatLuis();
            using (var client = new HttpClient())
            {
                var requestUri = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/479df8af-3547-4c6b-a216-18dab9e5ac5e?subscription-key=54c25066ceac4f148a48c70f18787404&verbose=true&spellCheck=true&q=" + query;
                var msg = await client.GetAsync(requestUri);

                if (msg.IsSuccessStatusCode)
                {
                    var jsonDataResponse = await msg.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<HabitatLuis>(jsonDataResponse);
                }
            }
            return result;
        }

    }
}