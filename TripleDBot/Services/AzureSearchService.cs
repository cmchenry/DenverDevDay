using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using TripleDBot.Model;

namespace TripleDBot.Services
{
    public class AzureSearchService
    {
        private const string searchServiceName = "tripled";
        private const string sessionIdx = "tripled-sessions-idx";
        private const string speakerIdx = "tripled-speakers-idx";

        private string apiKey = ConfigurationManager.AppSettings.Get("AzureSearchApiKey");
        private enum Indexes { session, speaker }
        private SearchServiceClient svcClient;

        public AzureSearchService()
        {
            svcClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
        }
        private SearchIndexClient GetIndexClient(Indexes index)
        {
            if (index == Indexes.speaker)
                return svcClient.Indexes.GetClient(speakerIdx);

            return svcClient.Indexes.GetClient(sessionIdx);
        }

        public IList<Session> FindSessions(SessionQuery query)
        {
            IList<Session> sessionList = new List<Session>();

            var sessionClient = GetIndexClient(Indexes.session);
            var parameters = new SearchParameters();
            //Handle Session Time by a filter
            if (query.StartTime > DateTime.MinValue)
            {
                var startTimeJson = JsonConvert.SerializeObject(query.StartTime);
                var filterStartTime = "time ge " + startTimeJson.Replace("\"", "");
                parameters.Filter = filterStartTime;
            }

            
            if (query.EndTime > DateTime.MinValue)
            {
                var endTimeJson = JsonConvert.SerializeObject(query.EndTime);
                var filterEndTime = "time le " + endTimeJson.Replace("\"", "");
                if (!string.IsNullOrWhiteSpace(parameters.Filter))
                    filterEndTime = " and " + filterEndTime;
   
                parameters.Filter += filterEndTime;
            }

            //Search for sessions with ANY of the Topics
            //Multiple words will be treated as Phrase
            var searchText = "";

            if(query.Topics.Count > 0)
                searchText = string.Format("\"{0}\"", string.Join("\"|\"", query.Topics));

            //Search for the Speaker's name
            if (query.Speaker != null)
                searchText = searchText + "+" + query.Speaker;
            
            //Search for the Room name
            if (query.Room != null)
                searchText = searchText + "+" + query.Room;

            //If we're only search for time, need to put a wild card into the searchText
            if (string.IsNullOrWhiteSpace(searchText))
                searchText = "*";
            var response = sessionClient.Documents.Search<Session>(searchText, parameters);
            foreach (SearchResult<Session> result in response.Results)
            {
                sessionList.Add(result.Document);
            }

            return sessionList;
        }

        
        public IList<Speaker> FindSpeakers(string query)
        {
            IList<Speaker> speakerList = new List<Speaker>();
            var speakerClient = GetIndexClient(Indexes.speaker);
            var response = speakerClient.Documents.Search<Speaker>(query);
            foreach (SearchResult<Speaker> result in response.Results)
            {
                speakerList.Add(result.Document);
            }

            return speakerList;
        }
    }
}