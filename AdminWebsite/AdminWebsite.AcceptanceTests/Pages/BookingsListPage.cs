﻿using OpenQA.Selenium;

namespace AdminWebsite.AcceptanceTests.Pages
{
    public class BookingsListPage
    {
        public By Rows = By.XPath("//div[contains(@class,'vh-a')]");
        public By Row(string caseNumber) => By.XPath($"//div[contains(text(),'{caseNumber}')]/parent::div/parent::div");
        public By RowWithId(string id) => By.Id(id);
        public By ScheduledTime(string id) => By.XPath($"//div[@id='{id}']//div[contains(text(),':')]");
        public By ScheduledDuration(string id) => By.XPath($"//div[@id='{id}']//div[contains(text(),'listed for')]");
        public By CaseNumber(string id, string caseNumber) => CaseInfo(id, caseNumber);
        public By CaseName(string id, string caseName) => CaseInfo(id, caseName);
        public By HearingType(string id, string caseType) => CaseInfo(id, caseType);
        public By Judge(string id, string judge) => CaseInfo(id, judge);
        public By Venue(string id, string venue) => CaseInfo(id, venue);
        public By CreatedBy(string id, string createdBy) => By.XPath($"//div[@id='{id}']//div[contains(text(),'{createdBy}')]");

        private static By CaseInfo(string id, string info)
        {
            return By.XPath($"//div[@id='{id}']//div[contains(text(),'{info}')]");
        }
    }
}