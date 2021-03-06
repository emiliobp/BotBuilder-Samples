﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// For each interaction from the user, an instance of this class is created and
    /// the OnTurnAsync method is called.
    /// This is a transient lifetime service. Transient lifetime services are created
    /// each time they're requested. For each <see cref="Activity"/> received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    /// <seealso cref="https://docs.microsoft.com/en-us/dotnet/api/microsoft.bot.ibot?view=botbuilder-dotnet-preview"/>
    public class LuisBot : IBot
    {
        /// <summary>
        /// Key in the bot config (.bot file) for the LUIS instance.
        /// In the .bot file, multiple instances of LUIS can be configured.
        /// </summary>
        public static readonly string LuisKey = "LuisBot";

        private const string WelcomeText = "This bot will introduce you to natural language processing with LUIS. Type an utterance to get started";

        /// <summary>
        /// Services configured from the ".bot" file.
        /// </summary>
        private readonly BotServices _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuisBot"/> class.
        /// </summary>
        /// <param name="services">Services configured from the ".bot" file.</param>
        public LuisBot(BotServices services)
        {
            _services = services ?? throw new System.ArgumentNullException(nameof(services));
            if (!_services.LuisServices.ContainsKey(LuisKey))
            {
                throw new System.ArgumentException($"Invalid configuration. Please check your '.bot' file for a LUIS service named '{LuisKey}'.");
            }
        }

        /// <summary>
        /// Every conversation turn for our LUIS Bot will call this method.
        /// There are no dialogs used, the sample only uses "single turn" processing,
        /// meaning a single request and response, with no stateful conversation.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Check LUIS model
                var recognizerResult = await _services.LuisServices[LuisKey].RecognizeAsync(turnContext, cancellationToken);
                var topIntent = recognizerResult?.GetTopScoringIntent();
                if (topIntent != null && topIntent.HasValue && topIntent.Value.intent != "None")
                {
                    var NumBoletos = "";
                    var Origen = "";
                    var Destino = "";

                    await turnContext.SendActivityAsync($"==>LUIS Top Scoring Intent: {topIntent.Value.intent}, Score: {topIntent.Value.score}\n");
                    if (topIntent.Value.intent == "Reservacion")
                    {
                        dynamic dynObj = JsonConvert.DeserializeObject(recognizerResult.Entities?.ToString());

                        if (dynObj.NumBoletos != null)
                        {
                            foreach (var data in dynObj.NumBoletos)
                            {
                             NumBoletos = data;
                            }
                        }
                        if (dynObj.Origen != null)
                        {
                            foreach (var data1 in dynObj.Origen)
                            {
                                Origen = data1;
                            }
                        }
                        if (dynObj.Destino != null)
                        {
                            foreach (var data2 in dynObj.Destino)
                            {
                                Destino = data2;
                            }
                        }
                    }
                    await turnContext.SendActivityAsync($"==>LUIS Info for {NumBoletos} tickets\tfrom {Origen} to {Destino}\n");
                }
                else
                {
                    var msg = @"No LUIS intents were found.
                            This sample is about identifying two user intents:
                            'Calendar.Add'
                            'Calendar.Find'
                            Try typing 'Add Event' or 'Show me tomorrow'.";
                    await turnContext.SendActivityAsync(msg);
                }
                /**
                // See if LUIS found and used an entity to determine user intent.
                var entityFound = ParseLuisForEntities(recognizerResult);

                // Inform the user if LUIS used an entity.
                if (entityFound.ToString() != string.Empty)
                {
                    await turnContext.SendActivityAsync($"==>LUIS Entity Found: {entityFound}\n");
                }
                else
                {
                    await turnContext.SendActivityAsync($"==>No LUIS Entities Found.\n");
                } */
            } 
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                // Send a welcome message to the user and tell them what actions they may perform to use this bot
                await SendWelcomeMessageAsync(turnContext, cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected", cancellationToken: cancellationToken);
            }

        }

        public string ParseLuisForEntities(RecognizerResult recognizerResult)
        {
            var result = string.Empty;

            // recognizerResult.Entities returns type JObject.
            foreach (var entity in recognizerResult.Entities)
            {
                // Parse JObject for a known entity types: Origen, Destino, and NumBoletos.
                var origenFound = JObject.Parse(entity.Value.ToString())["Origen"];
                var destinoFound = JObject.Parse(entity.Value.ToString())["Destino"];
                var numBolFound = JObject.Parse(entity.Value.ToString())["NumBoletos"];

                // We will return info on the first entity found.
                if (origenFound != null)
                {
                    // use JsonConvert to convert entity.Value to a dynamic object.
                    dynamic o = JsonConvert.DeserializeObject<dynamic>(entity.Value.ToString());
                    if (o.Origen[0] != null)
                    {
                        // Find and return the entity type and score.
                        var entType = o.Origen[0].type;
                        var entScore = o.Origen[0].score;
                        result = "Entity: " + entType + ", Score: " + entScore + ".";

                        return result;
                    }
                }

                if (destinoFound != null)
                {
                    // use JsonConvert to convert entity.Value to a dynamic object.
                    dynamic o = JsonConvert.DeserializeObject<dynamic>(entity.Value.ToString());
                    if (o.Destino[0] != null)
                    {
                        // Find and return the entity type and score.
                        var entType = o.Destino[0].type;
                        var entScore = o.Destino[0].score;
                        result = "Entity: " + entType + ", Score: " + entScore + ".";

                        return result;
                    }
                }

                if (numBolFound != null)
                {
                    // use JsonConvert to convert entity.Value to a dynamic object.
                    dynamic o = JsonConvert.DeserializeObject<dynamic>(entity.Value.ToString());
                    if (o.NumBoletos[0] != null)
                    {
                        // Find and return the entity type and score.
                        var entType = o.NumBoletos[0].type;
                        var entScore = o.NumBoletos[0].score;
                        result = "Entity: " + entType + ", Score: " + entScore + ".";

                        return result;
                    }
                }
            }

            // No entities were found, empty string returned.
            return result;
        }
        /// <summary>
        /// On a conversation update activity sent to the bot, the bot will
        /// send a message to the any new user(s) that were added.
        /// </summary>
        /// <param name="turnContext">Provides the <see cref="ITurnContext"/> for the turn of the bot.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>>A <see cref="Task"/> representing the operation result of the Turn operation.</returns>
        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(
                        $"Welcome to LuisBot {member.Name}. {WelcomeText}",
                        cancellationToken: cancellationToken);
                }
            }
        }
    }
}
