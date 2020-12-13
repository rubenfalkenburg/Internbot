using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Internbot.API.Recognizers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Internbot.API.Bots
{
    public class DialogBot<T> : ActivityHandler
        where T : Dialog
    {
        protected readonly Dialog Dialog;
        protected readonly BotState ConversationState;
        protected readonly QuestionRecognizer QuestionRecognizer;
        protected readonly ILogger Logger;

        public DialogBot(QuestionRecognizer questionRecognizer, ConversationState conversationState, T dialog, ILogger<DialogBot<T>> logger)
        {
            QuestionRecognizer = questionRecognizer;
            ConversationState = conversationState;
            Dialog = dialog;
            Logger = logger;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if(QuestionRecognizer.IsConfigured)
            {
                await base.OnMembersAddedAsync(membersAdded, turnContext, cancellationToken);

                var response = MessageFactory.Text("Hello, my name is ...");
                await turnContext.SendActivityAsync(response, cancellationToken);
                await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
            }
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            await ConversationState.SaveChangesAsync(turnContext, false,  cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
        }
    }
}
