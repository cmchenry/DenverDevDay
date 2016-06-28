using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using TripleDBot.Model;
using TripleDBot.Services;

namespace TripleDBot.Dialogs
{
    [Serializable]
    public class TripleDDialog : LuisDialog<object>
    {

        public TripleDDialog(ILuisService service=null) : base(service)
        {
            var modelId = ConfigurationManager.AppSettings.Get("LuisModelId");
            var subscriptionKey = ConfigurationManager.AppSettings.Get("LuisSubscriptionKey");
            var luisAttribute = new LuisModelAttribute(modelId, subscriptionKey);
            service = new LuisService(luisAttribute);
        }

        private string message;

        protected override async Task MessageReceived(IDialogContext context, IAwaitable<Message> item)
        {
            var msg = await item;
            this.message = msg.Text;
            await base.MessageReceived(context, item);
        }

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string respMessage = "";
            if (message.Equals("hello", StringComparison.InvariantCultureIgnoreCase))
                respMessage = "Hello, I'm TripleD, your personal concierge for Denver Dev Day";
            else
                respMessage = $"Sorry I did not understand: " + message;
            await context.PostAsync(respMessage);
            await context.PostAsync("Here are a few examples of the types of queries I can respond to: \n 1. Is Chris McHenry speaking this year?\n 2. What sessions are going at 10:15 AM?\n 3. What sessions are in Pikes Peak at 9:00 AM?\n 4. What sessions are on C#?");
            context.Wait(MessageReceived);
        }


        [LuisIntent("FindSessionTime")]
        public async Task FindSession(IDialogContext context, LuisResult result)
        {
            if (result.Entities.Count > 0)
            {
                var sessions = FindSessions(result);
                if (sessions.Count > 0)
                {

                    await context.PostAsync($"I found the following sessions:\n\n" + string.Join("\n\n", sessions.Select(s => s.ToString())));
                }
                else
                {
                    await context.PostAsync("Sorry I don't see any sessions for " + string.Join(", ", result.Entities.Select(i => i.Entity)));
                }
            }
            else
                await context.PostAsync("Sorry I'm just a simple bot, I can't understand your thoughtful and eloquent query.");
            
            
            context.Wait(MessageReceived);
        }

        [LuisIntent("FindSpeaker")]
        public async Task FindSpeaker(IDialogContext context, LuisResult result)
        {
            if (result.Entities.Count > 0)
            {
                var speakers = FindSpeakers(result);
                if(speakers.Count > 0)
                {
                    await context.PostAsync($"I found the following speakers:\n\n" + string.Join("\n\n", speakers.Select(s => s.ToString())));
                }
                else
                {
                    await context.PostAsync("Sorry I don't see any speakers for \n\n" + string.Join("\n\n", result.Entities.Select(i => i.Entity)));

                }


            }
            else
                await context.PostAsync("Sorry I'm just a simple bot, I can't understand your thoughtful and eloquent query.");

            context.Wait(MessageReceived);
        }

        public IList<Session> FindSessions(LuisResult result)
        {
            IList<Session> sessions = new List<Session>();
            SessionQuery query = new SessionQuery();
            IList<string> topics = new List<string>();

            foreach (EntityRecommendation entity in result.Entities)
            {
                if (entity.Type.Equals("Topic"))
                    topics.Add(entity.Entity);
                else if (entity.Type.Equals("Speaker"))
                    query.Speaker = entity.Entity;
                else if (entity.Type.Equals("Room"))
                    query.Room = entity.Entity;
                else if (entity.Type.Equals("Time"))
                {
                    var localTimeZone = TimeZoneInfo.Local.BaseUtcOffset;
                    var mountainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time").BaseUtcOffset;
                    var timeZoneDiff = localTimeZone.Subtract(mountainTimeZone);
                    DateTime eventDay = new DateTime(2016, 06, 24, 0, 0, 0, DateTimeKind.Local);
                    int tzDiff = timeZoneDiff.Hours;
                    eventDay.AddHours(tzDiff);


                    DateTime timeResult = new DateTime();
                    //specific time
                    var validTime = DateTime.TryParse(entity.Entity, out timeResult);
                    if (validTime)
                    {
                        //Convert to Mountain time
                        timeResult = timeResult.AddHours(tzDiff);

                        //Convert to the Event Date
                        if (timeResult.Date.CompareTo(eventDay) != 0)
                        {
                            TimeSpan ts = eventDay - timeResult.Date;
                            // Difference in days
                            int differenceInDays = ts.Days;
                            timeResult = timeResult.AddDays(differenceInDays);
                        }

                        DateTimeOffset offset = new DateTimeOffset(timeResult.ToUniversalTime());
                        query.StartTime = offset.AddHours(-1);
                        query.EndTime = offset.AddHours(1);
                    }
                }
            }
            query.Topics = topics;

            var svc = new AzureSearchService();
            sessions = svc.FindSessions(query);
            
            return sessions;
        }

        public IList<Speaker> FindSpeakers(LuisResult result)
        {
            IList<Speaker> speakers = new List<Speaker>();
            EntityRecommendation entity;
            var svc = new AzureSearchService();

            if (result.TryFindEntity("Speaker", out entity))
            {
                var speaker = entity.Entity;
                
                speakers = svc.FindSpeakers(speaker);

            }
            else if( result.TryFindEntity("Company", out entity))
            {
                var company = entity.Entity;
                speakers = svc.FindSpeakers(company);
            }

            return speakers;

        }
    }
}