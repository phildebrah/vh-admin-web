using System;
using AdminWebsite.BookingsAPI.Client;
using AdminWebsite.Extensions;
using AdminWebsite.Models;
using FluentAssertions;
using NUnit.Framework;

namespace AdminWebsite.UnitTests.Extensions
{
    public class HearingDetailsResponseExtensionsTests
    {
        private HearingDetailsResponse _hearing;

        [SetUp]
        public void Setup()
        {
            _hearing = new HearingDetailsResponse
            {
                Id = Guid.NewGuid()
            };
        }
        
        [Test]
        public void Should_Return_True_If_Judge_Phone_Exists()
        {
            var otherInfo = new OtherInformationDetails {JudgePhone = "1234564978"};
            _hearing.Other_information = otherInfo.ToOtherInformationString();

            _hearing.DoesJudgePhoneExist().Should().BeTrue();
        }
        
        [Test]
        public void Should_Return_False_If_Judge_Phone_Does_Not_Exist()
        {
            _hearing.DoesJudgePhoneExist().Should().BeFalse();
        }
        
        [Test]
        public void Should_Return_True_If_Judge_Email_Exists()
        {
            var otherInfo = new OtherInformationDetails {JudgeEmail = "judge@hmcts.net"};
            _hearing.Other_information = otherInfo.ToOtherInformationString();

            _hearing.DoesJudgeEmailExist().Should().BeTrue();
        }
        
        [Test]
        public void Should_Return_False_If_Judge_Email_Does_Not_Exist()
        {
            _hearing.DoesJudgeEmailExist().Should().BeFalse();
        }

        [Test]
        public void Should_Return_False_If_Judge_Email_is_empty()
        {
            var otherInfo = new OtherInformationDetails { JudgeEmail = "" };
            _hearing.Other_information = otherInfo.ToOtherInformationString();
            _hearing.DoesJudgeEmailExist().Should().BeFalse();
        }

        [Test]
        public void Should_Return_False_If_Judge_Email_is_null()
        {
            var otherInfo = new OtherInformationDetails { JudgeEmail = null };
            _hearing.Other_information = otherInfo.ToOtherInformationString();
            _hearing.DoesJudgeEmailExist().Should().BeFalse();
        }
    }
}