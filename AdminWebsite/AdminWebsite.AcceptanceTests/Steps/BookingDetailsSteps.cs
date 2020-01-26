﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AcceptanceTests.Common.Api.Hearings;
using AcceptanceTests.Common.Api.Requests;
using AcceptanceTests.Common.Configuration.Users;
using AcceptanceTests.Common.Driver.Browser;
using AcceptanceTests.Common.Driver.Helpers;
using AcceptanceTests.Common.Test.Steps;
using AdminWebsite.AcceptanceTests.Data;
using AdminWebsite.AcceptanceTests.Helpers;
using AdminWebsite.AcceptanceTests.Pages;
using AdminWebsite.BookingsAPI.Client;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace AdminWebsite.AcceptanceTests.Steps
{
    [Binding]
    public class BookingDetailsSteps : ISteps
    {
        private const int Timeout = 60;
        private readonly TestContext _c;
        private readonly Dictionary<string, UserBrowser> _browsers;
        private readonly BookingsApiManager _bookingsApiManager;
        private readonly VideoApiManager _videoApiManager;
        public BookingDetailsSteps(TestContext testContext, Dictionary<string, UserBrowser> browsers)
        {
            _c = testContext;
            _browsers = browsers;
            _bookingsApiManager = new BookingsApiManager(_c.AdminWebConfig.VhServices.BookingsApiUrl, _c.Tokens.BookingsApiBearerToken);
            _videoApiManager = new VideoApiManager(_c.AdminWebConfig.VhServices.VideoApiUrl, _c.Tokens.VideoApiBearerToken);
        }

        public void ProgressToNextPage()
        {
            WhenTheUserConfirmsTheBooking();
            ThenTheHearingIsAvailableInTheVideoWeb();
        }

        [When(@"the user views the booking details")]
        public void WhenTheUserViewsTheBookingDetails()
        {
            PollForHearingStatus(BookingStatus.Booked);
            VerifyTheBookingDetails();
            VerifyJudgeInParticipantsList();
            VerifyTheParticipantDetails();
        }

        private void VerifyTheBookingDetails()
        {
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.CaseNumberTitle).Text
                .Should().Be(_c.Test.HearingDetails.CaseNumber);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.CreatedBy).Text
                .Should().Be(_c.CurrentUser.Username);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.CreatedDate).Text
                .Should().NotBeNullOrWhiteSpace();
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.CaseNumber).Text
                .Should().Be(_c.Test.HearingDetails.CaseNumber);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.CaseName).Text
                .Should().Be(_c.Test.HearingDetails.CaseName);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.HearingType).Text
                .Should().Be(_c.Test.HearingDetails.HearingType.Name);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.HearingStartDate).Text.ToLower()
                .Should().Be(_c.Test.HearingSchedule.ScheduledDate.ToLocalTime().ToString(DateFormats.HearingSummaryDate).ToLower());
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.CourtroomAddress).Text
                .Should().Be($"{_c.Test.HearingSchedule.HearingVenue}, {_c.Test.HearingSchedule.Room}");
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.Duration).Text
                .Should().Contain($"listed for {_c.Test.HearingSchedule.DurationMinutes} minutes");
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.OtherInformation).Text
                .Should().Be(_c.Test.OtherInformation);
        }

        private void VerifyJudgeInParticipantsList()
        {
            var judge = UserManager.GetClerkUser(_c.Test.HearingParticipants);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.JudgeName).Text.Should().Contain(judge.DisplayName);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.JudgeEmail).Text.Should().Be(judge.AlternativeEmail);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.JudgeUsername).Text.Should().Be(judge.Username);
        }

        private void VerifyTheParticipantDetails()
        {
            for (var i = 0; i < _c.Test.HearingParticipants.Count - 1; i++)
            {
                var email = _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.ParticipantEmail(i)).Text;
                var participant = _c.Test.HearingParticipants.First(x => x.AlternativeEmail.ToLower().Equals(email.ToLower()));
                _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.ParticipantName(i)).Text
                    .Should().Contain($"{_c.Test.TestData.AddParticipant.Participant.Title} {participant.Firstname} {participant.Lastname}");
                _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.ParticipantRole(i)).Text
                    .Should().Contain(participant.HearingRoleName);
                _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.ParticipantUsername(i)).Text.ToLower()
                    .Should().Be(participant.Username.ToLower());
            }
        }

        [When(@"the user confirms the booking")]
        public void WhenTheUserConfirmsTheBooking()
        {
            _browsers[_c.CurrentUser.Key].ScrollTo(BookingDetailsPage.ConfirmButton);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilElementClickable(BookingDetailsPage.ConfirmButton).Click();
            _browsers[_c.CurrentUser.Key].ScrollTo(BookingDetailsPage.ConfirmedLabel);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(BookingDetailsPage.ConfirmedLabel).Displayed.Should().BeTrue();
        }

        [Then(@"the hearing is available in the video web")]
        public void ThenTheHearingIsAvailableInTheVideoWeb()
        {
            PollForHearingStatus(BookingStatus.Created).Should().BeTrue();
            _videoApiManager.PollForConferenceExists(GetHearing().Id).Should().BeTrue();
        }

        [When(@"the user cancels the hearing")]
        public void WhenTheUserAttemptsToCancelTheHearing()
        {
            _browsers[_c.CurrentUser.Key].ScrollTo(BookingDetailsPage.CancelButton);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilElementClickable(BookingDetailsPage.CancelButton).Click();
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilElementClickable(BookingDetailsPage.ConfirmCancelButton).Click();
        }

        [Then(@"the hearing is cancelled")]
        public void ThenTheHearingIsCancelled()
        {
            PollForHearingStatus(BookingStatus.Cancelled).Should().BeTrue();
        }

        [Then(@"the conference is deleted")]
        public void ThenTheConferenceIsDeleted()
        {
            var hearing = GetHearing();
            _videoApiManager.PollForConferenceDeleted(hearing.Id, Timeout).Should().BeTrue();
        }

        private HearingDetailsResponse GetHearing()
        {
            var clerkUsername = UserManager.GetClerkUser(_c.UserAccounts).Username;
            var hearingResponse = _bookingsApiManager.GetHearingsForUsername(clerkUsername);
            var hearings = RequestHelper.DeserialiseSnakeCaseJsonToResponse<List<HearingDetailsResponse>>(hearingResponse.Content);
            return hearings.First(x => x.Cases.First().Name.Equals(_c.Test.HearingDetails.CaseName));
        }

        private bool PollForHearingStatus(BookingStatus expectedStatus)
        {
            for (var i = 0; i < Timeout; i++)
            {
                var hearing = GetHearing();
                if (hearing.Status.Equals(expectedStatus))
                {
                    return true;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            return false;
        }

        public void ClickEdit()
        {
            _browsers[_c.CurrentUser.Key].ScrollTo(BookingDetailsPage.EditButton);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilElementClickable(BookingDetailsPage.EditButton).Click();
        }
    }
}
