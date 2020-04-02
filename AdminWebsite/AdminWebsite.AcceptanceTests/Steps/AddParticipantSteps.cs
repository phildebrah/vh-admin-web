﻿using System;
using System.Collections.Generic;
using System.Linq;
using AcceptanceTests.Common.Configuration.Users;
using AcceptanceTests.Common.Driver.Browser;
using AcceptanceTests.Common.Driver.Helpers;
using AcceptanceTests.Common.Model.Participant;
using AcceptanceTests.Common.Test.Steps;
using AdminWebsite.AcceptanceTests.Helpers;
using AdminWebsite.AcceptanceTests.Pages;
using FluentAssertions;
using OpenQA.Selenium;
using TechTalk.SpecFlow;

namespace AdminWebsite.AcceptanceTests.Steps
{
    [Binding]
    public class AddParticipantSteps : ISteps
    {
        private const int TimeoutToRetrieveUserFromAad = 60;
        private readonly TestContext _c;
        private readonly Dictionary<string, UserBrowser> _browsers;
        private string _individualDisplayName = "Representing";
        private readonly CommonSharedSteps _commonSharedSteps;
        public AddParticipantSteps(TestContext testContext, Dictionary<string, UserBrowser> browsers, CommonSharedSteps commonSharedSteps)
        {
            _c = testContext;
            _browsers = browsers;
            _commonSharedSteps = commonSharedSteps;
        }

        [When(@"the user completes the add participants form")]
        public void ProgressToNextPage()
        {
            AddExistingClaimantIndividual();
            AddExistingClaimantRep();
            AddNewDefendantIndividual();
            AddNewDefendantRep();
            VerifyUsersAreAddedToTheParticipantsList();
            ClickNext();
        }

        public void ClickNext()
        {
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.NextButton);
            _browsers[_c.CurrentUser.Key].Click(AddParticipantsPage.NextButton);
        }

        private void AddExistingClaimantIndividual()
        {
            var individual = UserManager.GetIndividualUsers(_c.UserAccounts)[0];
            individual.CaseRoleName = Party.Claimant.Name;
            individual.HearingRoleName = PartyRole.ClaimantLip.Name;
            _c.Test.HearingParticipants.Add(individual);
            SetParty(individual.CaseRoleName);
            SetRole(individual.HearingRoleName);
            SetExistingIndividualDetails(individual);
        }

        private void AddExistingClaimantRep()
        {
            var rep = UserManager.GetRepresentativeUsers(_c.UserAccounts)[0];
            rep.CaseRoleName = Party.Claimant.Name;
            rep.HearingRoleName = PartyRole.Representative.Name;
            _c.Test.HearingParticipants.Add(rep);
            SetParty(rep.CaseRoleName);
            SetRole(rep.HearingRoleName);
            SetExistingRepDetails(rep);
        }

        private void AddNewDefendantIndividual()
        {

            var individual = CreateNewUser("Individual");
            individual.CaseRoleName = Party.Defendant.Name;
            individual.HearingRoleName = PartyRole.DefendantLip.Name;
            _individualDisplayName = individual.DisplayName;
            _c.Test.HearingParticipants.Add(individual);
            SetParty(individual.CaseRoleName);
            SetRole(individual.HearingRoleName);
            SetNewIndividualDetails(individual);
        }

        private void AddNewDefendantRep()
        {
            var rep = CreateNewUser("Representative");
            rep.CaseRoleName = Party.Defendant.Name;
            rep.HearingRoleName = PartyRole.Representative.Name;
            rep.Representee = _individualDisplayName;
            _c.Test.HearingParticipants.Add(rep);
            SetParty(rep.CaseRoleName);
            SetRole(rep.HearingRoleName);
            SetNewRepDetails(rep);
        }

        private void SetParty(string party)
        {
            _commonSharedSteps.WhenTheUserSelectsTheOptionFromTheDropdown(_browsers[_c.CurrentUser.Key].Driver, AddParticipantsPage.PartyDropdown, Party.FromString(party).Name);
        }

        private void SetRole(string role)
        {
            _commonSharedSteps.WhenTheUserSelectsTheOptionFromTheDropdown(_browsers[_c.CurrentUser.Key].Driver, AddParticipantsPage.RoleDropdown, PartyRole.FromString(role).Name);
        }

        private UserAccount CreateNewUser(string role)
        {
            var user = new UserAccount();
            var prefix = _c.Test.TestData.AddParticipant.Participant.NewUserPrefix;
            user.AlternativeEmail = $"{prefix}{Faker.Internet.Email()}";
            var firstname = Faker.Name.First();
            var lastname = Faker.Name.Last();
            var displayName = $"{firstname} {lastname}";
            user.Firstname = $"{prefix}{firstname}";
            user.Lastname = $"{lastname}";
            user.DisplayName = $"{prefix}{displayName}";
            user.Role = role;
            user.Username = $"{user.Firstname.ToLower()}.{user.Lastname.ToLower()}{_c.AdminWebConfig.TestConfig.TestUsernameStem.ToLower()}";
            return user;
        }

        private void SetNewIndividualDetails(UserAccount user)
        {
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.ParticipantEmailTextfield).SendKeys(user.AlternativeEmail);
            var title = _c.Test.TestData.AddParticipant.Participant.Title;
            _commonSharedSteps.WhenTheUserSelectsTheOptionFromTheDropdown(_browsers[_c.CurrentUser.Key].Driver, AddParticipantsPage.TitleDropdown, title);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.FirstNameTextfield).SendKeys(user.Firstname);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.LastNameTextfield).SendKeys(user.Lastname);
            var organisation = _c.Test.TestData.AddParticipant.Participant.Organisation;
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.IndividualOrganisationTextfield).SendKeys(organisation);
            var telephone = _c.Test.TestData.AddParticipant.Participant.Phone;
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.PhoneTextfield).SendKeys(telephone);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.DisplayNameTextfield).SendKeys(user.DisplayName);
            var houseNumber = _c.Test.TestData.AddParticipant.Address.HouseNumber;
            EnterTextIfFieldIsNotPrePopulated(AddParticipantsPage.HouseNumberTextfield, houseNumber);
            var street = _c.Test.TestData.AddParticipant.Address.Street;
            EnterTextIfFieldIsNotPrePopulated(AddParticipantsPage.StreetTextfield, street);
            var city = _c.Test.TestData.AddParticipant.Address.Street;
            EnterTextIfFieldIsNotPrePopulated(AddParticipantsPage.CityTextfield, city);
            var county = _c.Test.TestData.AddParticipant.Address.County;
            EnterTextIfFieldIsNotPrePopulated(AddParticipantsPage.CountyTextfield, county);
            var postcode = _c.Test.TestData.AddParticipant.Address.Postcode;
            EnterTextIfFieldIsNotPrePopulated(AddParticipantsPage.PostcodeTextfield, postcode);
            _browsers[_c.CurrentUser.Key].ScrollTo(AddParticipantsPage.AddParticipantLink);
            _browsers[_c.CurrentUser.Key].ClickLink(AddParticipantsPage.AddParticipantLink);
        }

        private void EnterTextIfFieldIsNotPrePopulated(By element, string value)
        {
            if (_browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(element).GetAttribute("value").Length.Equals(0))
                _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(element).SendKeys(value);
        }

        private void SetNewRepDetails(UserAccount user)
        {
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.ParticipantEmailTextfield).SendKeys(user.AlternativeEmail);
            var title = _c.Test.TestData.AddParticipant.Participant.Title;
            _commonSharedSteps.WhenTheUserSelectsTheOptionFromTheDropdown(_browsers[_c.CurrentUser.Key].Driver, AddParticipantsPage.TitleDropdown, title);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.FirstNameTextfield).SendKeys(user.Firstname);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.LastNameTextfield).SendKeys(user.Lastname);
            var telephone = _c.Test.TestData.AddParticipant.Participant.Phone;
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.PhoneTextfield).SendKeys(telephone);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.DisplayNameTextfield).SendKeys(user.DisplayName);
            var organisation = _c.Test.TestData.AddParticipant.Participant.Organisation;
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.RepOrganisationTextfield).SendKeys(organisation);
            var reference = _c.Test.TestData.AddParticipant.Participant.Reference;
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.ReferenceTextfield).SendKeys(reference);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.RepresentingTextfield).SendKeys(user.Representee);
            _browsers[_c.CurrentUser.Key].ScrollTo(AddParticipantsPage.AddParticipantLink);
            _browsers[_c.CurrentUser.Key].ClickLink(AddParticipantsPage.AddParticipantLink);
        }

        private void SetExistingIndividualDetails(UserAccount user)
        {
            ExistingUserEmailIsSelected(user.AlternativeEmail).Should().BeTrue("Existing user email appeared in the dropdown list retrieved from AAD");
            IndividualFieldsAreSet(user);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.DisplayNameTextfield).SendKeys(user.DisplayName);
            _browsers[_c.CurrentUser.Key].ScrollTo(AddParticipantsPage.AddParticipantLink);
            _browsers[_c.CurrentUser.Key].ClickLink(AddParticipantsPage.AddParticipantLink);
        }

        private void SetExistingRepDetails(UserAccount user)
        {
            ExistingUserEmailIsSelected(user.AlternativeEmail).Should().BeTrue("Existing user email appeared in the dropdown list retrieved from AAD");
            RepFieldsAreSet(user);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.DisplayNameTextfield).SendKeys(user.DisplayName);
            var organisation = _c.Test.TestData.AddParticipant.Participant.Organisation;
            EnterTextIfFieldIsNotPrePopulated(AddParticipantsPage.RepOrganisationTextfield, organisation);
            var reference = _c.Test.TestData.AddParticipant.Participant.Reference;
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.ReferenceTextfield).SendKeys(reference);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.RepresentingTextfield).SendKeys(user.Representee);
            _browsers[_c.CurrentUser.Key].ScrollTo(AddParticipantsPage.AddParticipantLink);
            _browsers[_c.CurrentUser.Key].ClickLink(AddParticipantsPage.AddParticipantLink);
        }

        private bool ExistingUserEmailIsSelected(string alternativeEmail)
        {
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.ParticipantEmailTextfield).SendKeys(alternativeEmail);
            var retrievedListOfEmails = _browsers[_c.CurrentUser.Key].Driver.WaitUntilElementsVisible(AddParticipantsPage.ExistingEmailLinks, TimeoutToRetrieveUserFromAad);
            retrievedListOfEmails.Count.Should().BeGreaterThan(0);
            foreach (var email in retrievedListOfEmails)
            {
                if (!email.Text.ToLower().Contains(alternativeEmail.ToLower())) continue;
                email.Click();
                return true;
            }

            return false;
        }

        private void IndividualFieldsAreSet(UserAccount user)
        {
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.ParticipantEmailTextfield).Enabled.Should().BeFalse();
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.FirstNameTextfield).Enabled.Should().BeFalse();
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.LastNameTextfield).Enabled.Should().BeFalse();
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.ParticipantEmailTextfield).GetAttribute("value").Should().Be(user.AlternativeEmail);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.FirstNameTextfield).GetAttribute("value").Should().Be(user.Firstname);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.LastNameTextfield).GetAttribute("value").Should().Be(user.Lastname);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.PhoneTextfield).GetAttribute("value").Should().NotBeNullOrWhiteSpace();
            var organisation = _c.Test.TestData.AddParticipant.Participant.Organisation;
            EnterTextIfFieldIsNotPrePopulated(AddParticipantsPage.IndividualOrganisationTextfield, organisation);
            var houseNumber = _c.Test.TestData.AddParticipant.Address.HouseNumber;
            EnterTextIfFieldIsNotPrePopulated(AddParticipantsPage.HouseNumberTextfield, houseNumber);
            var street = _c.Test.TestData.AddParticipant.Address.Street;
            EnterTextIfFieldIsNotPrePopulated(AddParticipantsPage.StreetTextfield, street);
            var city = _c.Test.TestData.AddParticipant.Address.Street;
            EnterTextIfFieldIsNotPrePopulated(AddParticipantsPage.CityTextfield, city);
            var county = _c.Test.TestData.AddParticipant.Address.County;
            EnterTextIfFieldIsNotPrePopulated(AddParticipantsPage.CountyTextfield, county);
            var postcode = _c.Test.TestData.AddParticipant.Address.Postcode;
            EnterTextIfFieldIsNotPrePopulated(AddParticipantsPage.PostcodeTextfield, postcode);
        }

        private void RepFieldsAreSet(UserAccount user)
        {
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.ParticipantEmailTextfield).Enabled.Should().BeFalse();
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.FirstNameTextfield).Enabled.Should().BeFalse();
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.LastNameTextfield).Enabled.Should().BeFalse();
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.ParticipantEmailTextfield).GetAttribute("value").Should().Be(user.AlternativeEmail);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.FirstNameTextfield).GetAttribute("value").Should().Be(user.Firstname);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.LastNameTextfield).GetAttribute("value").Should().Be(user.Lastname);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.PhoneTextfield).GetAttribute("value").Should().NotBeNullOrWhiteSpace();
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.RepOrganisationTextfield).GetAttribute("value").Should().NotBeNullOrWhiteSpace();
        }

        private void VerifyUsersAreAddedToTheParticipantsList()
        {
            var clerk = UserManager.GetClerkUser(_c.UserAccounts);
            _browsers[_c.CurrentUser.Key].Driver
                .WaitUntilVisible(AddParticipantsPage.ClerkUserParticipantsList(clerk.Username))
                .Displayed.Should().BeTrue();

            var actualResult = GetAllParticipantsDetails();
            var title = _c.Test.TestData.AddParticipant.Participant.Title;

            foreach (var participant in _c.Test.HearingParticipants)
            {
                if (participant.Role.ToLower().Equals("clerk") || participant.Role.ToLower().Equals("judge")) continue;
                var expectedParticipant =
                    $"{title} {participant.Firstname} {participant.Lastname} {participant.HearingRoleName}";

                if (participant.HearingRoleName == PartyRole.Representative.Name)
                    expectedParticipant = $"{expectedParticipant}, representing {participant.Representee}";

                actualResult.Any(x => x.Replace(Environment.NewLine, " ").Equals(expectedParticipant)).Should()
                    .BeTrue($"expected participant matches {expectedParticipant}");
            }
        }

        public List<string> GetAllParticipantsDetails()
        {
            var elements = _browsers[_c.CurrentUser.Key].Driver.WaitUntilElementsVisible(AddParticipantsPage.ParticipantsList);
            return elements.Select(element => element.Text.Trim().Replace("\r\n", " ")).ToList();
        }

        public void EditANewParticipant(string alternativeEmail)
        {
            _c.Test.HearingParticipants.First(x => x.AlternativeEmail.ToLower().Equals(alternativeEmail.ToLower())).DisplayName = $"{_c.Test.AddParticipant.Participant.NewUserPrefix}Updated display name";
            _browsers[_c.CurrentUser.Key].Clear(AddParticipantsPage.DisplayNameTextfield);
            _browsers[_c.CurrentUser.Key].Driver.WaitUntilVisible(AddParticipantsPage.DisplayNameTextfield).SendKeys(_c.Test.HearingParticipants.First(x => x.AlternativeEmail.ToLower().Equals(alternativeEmail.ToLower())).DisplayName);
            _browsers[_c.CurrentUser.Key].ScrollTo(AddParticipantsPage.NextButton);
            _browsers[_c.CurrentUser.Key].Click(AddParticipantsPage.NextButton);
        }
    }
}
