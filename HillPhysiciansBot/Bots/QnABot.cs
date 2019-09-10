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
using Microsoft.Bot.Connector;
using System;


namespace Microsoft.BotBuilderSamples
{
    public class QnABot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<QnABot> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly List<String> ehrNames = new List<String>{ "Cerner" , "Elation Health", "Kareo", "Meditab Software, Inc.",
                            "Office Ally", "System Physician’s Computer Company (PCC)", "Practice Fusion",
                            "Quest – Quanum", "AdvancedMD", "AllScripts", "Amazing Charts", "AthenaHealth", "Chart Logic",
                            "eClinicalWorks", "e-MDs", "GE Centricity", "MediTouch", "HealthFusion","IMS","MEDITECH",
                            "Office Practicum", "Origins"}; //hardcoded, grab info from KB

        private List<CardAction> ehrStepPrompts = new List<CardAction>();
        private List<CardAction> ehrContactPrompts = new List<CardAction>();

        public QnABot(IConfiguration configuration, ILogger<QnABot> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            //LoadEhrStepPrompts();
            //LoadEhrContactPrompts();
        }

        
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await OpenMenu(turnContext, cancellationToken);
        }
        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Activity.Name == "webchat/join")
            {
                await turnContext.SendActivityAsync("Welcome Message!");
            }
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

                if (isEHR(turnContext.Activity.Text))
                {
                    
                    await turnContext.SendActivityAsync(MessageFactory.Text(response[0].Answer), cancellationToken);

                    var card = new HeroCard();

                    card.Text = @"Were you able to obtain your e-measures?";
                    card.Buttons = new List<CardAction>()
                    {
                        new CardAction() { Title = "Yes", Type = ActionTypes.ImBack, Value = "Yes, I completed the survey." },
                        new CardAction() { Title = "No, I need more help\n from my EHR", Type = ActionTypes.ImBack, Value = String.Format("I need more help from {0}.", turnContext.Activity.Text) },
                        new CardAction() { Title = "No, I need steps for\n a different EHR", Type = ActionTypes.ImBack, Value = "I need steps for a different EHR." },
                        new CardAction() { Title = "Go back", Type = ActionTypes.ImBack, Value = "Go back" },
                    };

                    var reply = MessageFactory.Attachment(card.ToAttachment());
                    await turnContext.SendActivityAsync(reply, cancellationToken);

                }
                else if (turnContext.Activity.Text == "MENU" || turnContext.Activity.Text == "Go back")
                {
                    await OpenMenu(turnContext, cancellationToken);
                }
                else if (turnContext.Activity.Text == "FAQ")
                {
                    var card = new HeroCard();

                    card.Text = @"Call your Practice Support Advisor(PSA) for access to Hill inSite or for any issues with the automated questionnaire. Or go back to the Main Menu.";
                    card.Buttons = new List<CardAction>()
                    {
                        new CardAction() { Title = "How do I access the\n questionnaire?", Type = ActionTypes.ImBack, Value = "How do I access the questionnaire?" },
                        new CardAction() { Title = "What is the deadline\n to complete the\n questionnaire? ", Type = ActionTypes.ImBack, Value = "What is the deadline to complete the questionnaire? " },
                        new CardAction() { Title = "Why is this\n information requested?", Type = ActionTypes.ImBack, Value = "Why is this information requested?" },
                        new CardAction() { Title = "What are the \n two e-Measures?", Type = ActionTypes.ImBack, Value = "What are the two e-Measures?" },
                        new CardAction() { Title = "For which\n measurement year\n is the e-Measure \n data being requested?", Type = ActionTypes.ImBack, Value = "For which measurement year is the e-Measure data being requested?" },
                        new CardAction() { Title = "Who may complete\n the form? ", Type = ActionTypes.ImBack, Value = "Who may complete the form? " },
                        new CardAction() { Title = "Does the form have to\n be completed\n once per provider?", Type = ActionTypes.ImBack, Value = "Does the form have to be completed once per provider?" },
                        new CardAction() { Title = "Is this information\n collected only\n for Hill\n Physicians’ members?", Type = ActionTypes.ImBack, Value = "Is this information collected only for Hill Physicians’ members?" },
                        new CardAction() { Title = "Is performance based\n on the number\n of compliant\n patients?", Type = ActionTypes.ImBack, Value = "Is performance based on the number of compliant patients?" },
                        new CardAction() { Title = "Why do I have to\n complete this\n form every year?", Type = ActionTypes.ImBack, Value = "Why do I have to complete this form every year?" },
                        new CardAction() { Title = "Go back", Type = ActionTypes.ImBack, Value = "Go back" }
                    };

                    var reply = MessageFactory.Attachment(card.ToAttachment());
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
                else if (turnContext.Activity.Text.Contains("more help")) //FIX
                {
                    var card = new HeroCard();

                    card.Text = @"Call your Practice Support Advisor(PSA) for access to Hill inSite or for any issues with the automated questionnaire. Or go back to the Main Menu.";
                    card.Buttons = new List<CardAction>()
                    {
                        new CardAction() { Title = "Go back", Type = ActionTypes.ImBack, Value = "Go back" },
                    };

                    var reply = MessageFactory.Attachment(card.ToAttachment());
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
                else
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(response[0].Answer), cancellationToken);
                }                
            }
            else
            {
                var card = new HeroCard();

                card.Text = @"I'm not sure how to answer that question. Call your Practice Support Advisor(PSA) for access to Hill inSite or for any issues with the automated questionnaire. Or go back to the Main Menu.";
                card.Buttons = new List<CardAction>()
                {
                        new CardAction() { Title = "Go back", Type = ActionTypes.ImBack, Value = "Go back" },
                };

                var reply = MessageFactory.Attachment(card.ToAttachment());
                await turnContext.SendActivityAsync(reply, cancellationToken);
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

        private void LoadEhrStepPrompts()
        {
            foreach (String name in ehrNames)
            {
                ehrStepPrompts.Add(new CardAction() { Title = String.Format("{0}", name), Type = ActionTypes.ImBack, Value = String.Format("Steps for {0}", name) });
            }
        }
        private void LoadEhrContactPrompts()
        {
            foreach (String name in ehrNames)
            {
                ehrContactPrompts.Add(new CardAction() { Title = String.Format("{0}", name), Type = ActionTypes.ImBack, Value = String.Format("Contact {0}", name) });
            }
        }

        private bool isEHR(String reply)
        {
            reply = reply.ToLower();
            foreach (String ehr in ehrNames)
            {
                String ehrLow = ehr.ToLower();
                if (ehrLow.Contains(reply) || reply.Contains(ehrLow))
                    return true;
            }
            return false;
        }

        private static async Task OpenMenu(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard();
            card.Title = "Welcome to the e-Measure\n Survey Chatbot!";
            card.Text = @"How can I help you today? Type in any question below, or choose from one of the following prompts:";
            card.Buttons = new List<CardAction>()
            {
                new CardAction() { Title = "Steps for e-measures", Type = ActionTypes.ImBack, Value = "Steps for e-measures" },
                //new CardAction() { Title = "Contact my EHR", Type = ActionTypes.ImBack, Value = "Contact my EHR" },
                new CardAction() { Title = "FAQ", Type = ActionTypes.ImBack, Value = "FAQ" },
                new CardAction() { Title = "Troubleshoot", Type = ActionTypes.ImBack, Value = "Troubleshoot" },
            };

            var response = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(response, cancellationToken);
        }


    }
}
