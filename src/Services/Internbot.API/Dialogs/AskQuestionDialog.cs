using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Internbot.API.Recognizers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Internbot.API.Dialogs
{
    public class AskMultipleQuestionsDialog : ComponentDialog
    {
        private readonly ILogger _logger;

        public AskMultipleQuestionsDialog(AskQuestionDialog askQuestionDialog, ILogger<AskMultipleQuestionsDialog> logger)
            : base(nameof(AskMultipleQuestionsDialog))
        {
            _logger = logger;

            AddDialog(askQuestionDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AskQuestionStepAsync,
                NextQuestionStepAsync,
                EndDialogStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> AskQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var multipleQuestionsDetails = stepContext.Options as MultipleQuestionsDetails ?? new MultipleQuestionsDetails();

            var question = multipleQuestionsDetails.Questions.Pop();

            return await stepContext.BeginDialogAsync(nameof(AskQuestionDialog), question, cancellationToken);
        }

        private async Task<DialogTurnResult> NextQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var multipleQuestionsDetails = stepContext.Options as MultipleQuestionsDetails ?? new MultipleQuestionsDetails();

            if (multipleQuestionsDetails.Questions.Count() == 0)
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(nameof(AskMultipleQuestionsDialog), multipleQuestionsDetails, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> EndDialogStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }

    public class AskQuestionDialog : ComponentDialog
    {
        private readonly QuestionRecognizer _luisRecognizer;
        private readonly ILogger _logger;

        public AskQuestionDialog(QuestionRecognizer luisRecognizer, ILogger<AskQuestionDialog> logger)
            : base(nameof(AskQuestionDialog))
        {
            _luisRecognizer = luisRecognizer;
            _logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                AskQuestionStepAsync,
                KeepAskingStepAsync,
                EndQuestionStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> AskQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var questionDetails = (QuestionDetails)stepContext.Options;

            var promptMessage = MessageFactory.Text(questionDetails.Question, questionDetails.Question, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> KeepAskingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var questionDetails = (QuestionDetails)stepContext.Options;

            if (!string.IsNullOrWhiteSpace(questionDetails.KeepAskingResponse))
            {
                var luisResult = await _luisRecognizer.RecognizeAsync(stepContext.Context, cancellationToken);

                if (luisResult.Intents.TryGetValue("KeepAsking", out var intentScore))
                {
                    var promptMessage = MessageFactory.Text(questionDetails.KeepAskingResponse, questionDetails.KeepAskingResponse, InputHints.ExpectingInput);
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
                }
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> EndQuestionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }

    public class QuestionDetails
    {
        public string Question { get; set; }
        public string KeepAskingResponse { get; set; }
    }

    public class MultipleQuestionsDetails
    {
        public Stack<QuestionDetails> Questions { get; set; } = new Stack<QuestionDetails>();
    }
}
