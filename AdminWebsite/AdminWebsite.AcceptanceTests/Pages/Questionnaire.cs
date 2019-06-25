﻿using AdminWebsite.AcceptanceTests.Helpers;
using OpenQA.Selenium;
using System.Collections.Generic;
using System.Linq;

namespace AdminWebsite.AcceptanceTests.Pages
{
    public class Questionnaire : Common
    {
        private readonly BrowserContext _browserContext;
        private readonly By _questionnaireList = By.Id("vh-table");

        public Questionnaire(BrowserContext browserContext) : base(browserContext)
        {
            _browserContext = browserContext;
        }

        public List<IWebElement> Particpants()
        {
            return GetListOfElements(By.CssSelector("span.vh-ml15")).ToList();
        }

        public IList<string> Answers()
        {
            var answerElements = GetListOfElements(By.XPath("//*[@class='govuk-body vh-date vh-wrap vh-mr15']"));
            return answerElements.Select(x => x.Text).ToList();
        }
    }
}