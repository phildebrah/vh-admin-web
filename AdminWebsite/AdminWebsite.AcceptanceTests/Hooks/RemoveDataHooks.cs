﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AcceptanceTests.Common.Api.Hearings;
using AcceptanceTests.Common.Api.Helpers;
using AdminWebsite.AcceptanceTests.Helpers;
using BookingsApi.Contract.Responses;
using FluentAssertions;
using TechTalk.SpecFlow;
using VideoApi.Contract.Responses;

namespace AdminWebsite.AcceptanceTests.Hooks
{
    [Binding]
    public sealed class RemoveDataHooks
    {
        private string _username;

        [AfterScenario]
        public void RemovePreviousHearings(TestContext context)
        {
            if (context?.Users == null) return;
            if (context.Users?.Count == 0) return;
            _username = Users.GetJudgeUser(context.Users).Username;
            ClearHearingsForUser(context.Api);
            ClearClosedConferencesForUser(context.Api);
        }

        private void ClearHearingsForUser(TestApiManager api)
        {
            var response = api.GetHearingsByUsername(_username);
            var hearings = RequestHelper.Deserialise<List<HearingDetailsResponse>>(response.Content);
            if (hearings == null) return;

            var ids =  hearings.Select(x => x.GroupId).Distinct().ToList();
            foreach (var id in ids.Where(x=> x.HasValue))
            {
                DeleteTheHearing(api, id.Value);
            }
        }

        private static void DeleteTheHearing(TestApiManager api, Guid hearingId)
        {
            var response = api.DeleteHearing(hearingId);
            response.IsSuccessful.Should().BeTrue($"HearingDetails {hearingId} has been deleted. Status {response.StatusCode}. {response.Content}");
        }

        private void ClearClosedConferencesForUser(TestApiManager api)
        {
            var response = api.GetConferencesForTodayJudge(_username);
            var todaysConferences = RequestHelper.Deserialise<List<ConferenceForHostResponse>>(FormatSerializedString(response.Content));
            if (todaysConferences == null) return;

            foreach (var conference in todaysConferences)
            {
                var hearingId = GetTheHearingIdFromTheConference(api, conference.Id);

                if (HearingHasNotBeenDeletedAlready(api, hearingId) && !hearingId.Equals(Guid.Empty))
                    DeleteTheHearing(api, hearingId);

                if (ConferenceHasNotBeenDeletedAlready(api, conference.Id))
                    DeleteTheConference(api, hearingId, conference.Id);
            }
        }

        private static Guid GetTheHearingIdFromTheConference(TestApiManager api, Guid conferenceId)
        {
            var response = api.GetConferenceByConferenceId(conferenceId);
            if (!response.IsSuccessful) return Guid.Empty;
            var conference = RequestHelper.Deserialise<ConferenceDetailsResponse>(FormatSerializedString(response.Content));
            return conference.HearingId;
        }

        private static string FormatSerializedString(string content)
        {
            var formattedContent = content;
            formattedContent = formattedContent.Replace("\\", "");
            formattedContent = formattedContent.Replace("\"[", "[");
            formattedContent = formattedContent.Replace("]\"", "]");

            return formattedContent;
        }

        private static bool HearingHasNotBeenDeletedAlready(TestApiManager api, Guid hearingId)
        {
            var response = api.GetHearing(hearingId);
            return !response.StatusCode.Equals(HttpStatusCode.NotFound);
        }

        private static bool ConferenceHasNotBeenDeletedAlready(TestApiManager api, Guid conferenceId)
        {
            var response = api.GetConferenceByConferenceId(conferenceId);
            return !response.StatusCode.Equals(HttpStatusCode.NotFound);
        }

        private static void DeleteTheConference(TestApiManager api, Guid hearingId, Guid conferenceId)
        {
            api.DeleteConference(hearingId, conferenceId);
        }
    }
}
