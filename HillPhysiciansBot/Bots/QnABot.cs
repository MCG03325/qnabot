// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Microsoft.BotBuilderSamples
{
    public class QnABot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QnABot> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public QnABot(IConfiguration configuration, ILogger<QnABot> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Name == "webchat/join")
            {
                await turnContext.SendActivityAsync("Welcome!");
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text("Hello, how can I help you today? Type in any question below, or choose from one of the following prompts:");

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction() { Title = "Purpose of this?", Type = ActionTypes.ImBack, Value = "Why do I have to do this?" },
                    new CardAction() { Title = "Steps for e-measures", Type = ActionTypes.ImBack, Value = "Steps for e-measures" },
                    new CardAction() { Title = "Troubleshoot", Type = ActionTypes.ImBack, Value = "Troubleshoot" },
                },
            };

            await turnContext.SendActivityAsync(reply, cancellationToken);

        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var qnaMaker = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = _configuration["QnAKnowledgebaseId"],
                EndpointKey = _configuration["QnAAuthKey"],
                Host = GetHostname()
            },
            null,
            httpClient);

            _logger.LogInformation("Calling QnA Maker");

            // The actual call to the QnA Maker service.
            var response = await qnaMaker.GetAnswersAsync(turnContext);
            if (response != null && response.Length > 0)
            {

                await turnContext.SendActivityAsync(MessageFactory.Text(response[0].Answer), cancellationToken);


                /* Needs work
                var reply = MessageFactory.Text("Did this answer your question?");

                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                    {
                        new CardAction() { Title = "Yes", Type = ActionTypes.ImBack, Value = "Yes, that answered my question." },
                        new CardAction() { Title = "No", Type = ActionTypes.ImBack, Value = "No, I need more help." },
                    },
                };
                
                if (turnContext.Activity.Text == "I need more help")
                {
                    var reply = MessageFactory.Text("Tell me your issue");
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
                await turnContext.SendActivityAsync(reply, cancellationToken);
                */
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("I'm not sure how to answer that question. Call your Practice Support Advisor (PSA) for access to Hill inSite or for any issues with the automated questionnaire."), cancellationToken);
            }
            
        }

        private string GetHostname()
        {
            var hostname = _configuration["QnAEndpointHostName"];
            if (!hostname.StartsWith("https://"))
            {
                hostname = string.Concat("https://", hostname);
            }

            if (!hostname.EndsWith("/qnamaker"))
            {
                hostname = string.Concat(hostname, "/qnamaker");
            }

            return hostname;
        }

    }
}
