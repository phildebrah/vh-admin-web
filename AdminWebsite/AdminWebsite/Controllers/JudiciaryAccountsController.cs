﻿using AdminWebsite.Configuration;
using AdminWebsite.Contracts.Responses;
using AdminWebsite.Mappers;
using AdminWebsite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BookingsApi.Client;
using BookingsApi.Contract.Requests;
using BookingsApi.Contract.Responses;

namespace AdminWebsite.Controllers
{
    [Produces("application/json")]
    [Route("api/judiciary")]
    [ApiController]
    public class JudiciaryAccountsController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;
        private readonly JavaScriptEncoder _encoder;
        private readonly IBookingsApiClient _bookingsApiClient;
        private readonly TestUserSecrets _testSettings;

        public JudiciaryAccountsController(IUserAccountService userAccountService, JavaScriptEncoder encoder,
            IBookingsApiClient bookingsApiClient, IOptions<TestUserSecrets> testSettings) 
        {           
            _userAccountService = userAccountService;
            _encoder = encoder;
            _bookingsApiClient = bookingsApiClient;
            _testSettings = testSettings.Value;
        }

        /// <summary>
        /// Find judges and court rooms accounts list by email search term.
        /// </summary>
        /// <param name = "term" > The email address search term.</param>
        /// <returns> The list of judges</returns>
        [HttpPost("judges", Name = "PostJudgesBySearchTerm")]
        [SwaggerOperation(OperationId = "PostJudgesBySearchTerm")]
        [ProducesResponseType(typeof(List<JudgeResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IList<JudgeResponse>>> PostJudgesBySearchTermAsync([FromBody] string term)
        {
            try
            {
                term = _encoder.Encode(term);
                var searchTerm = new SearchTermRequest(term);

                var courtRoomJudgesTask = _userAccountService.SearchJudgesByEmail(searchTerm.Term);
                var eJudiciaryJudgesTask = GetEjudiciaryJudgesBySearchTermAsync(searchTerm);

                await Task.WhenAll(courtRoomJudgesTask, eJudiciaryJudgesTask);

                var courtRoomJudges = await courtRoomJudgesTask;
                var eJudiciaryJudges = (await eJudiciaryJudgesTask).Select(x => JudgeResponseMapper.MapTo(x));

                var allJudges = courtRoomJudges.Concat(eJudiciaryJudges)
                    .OrderBy(x => x.Email).Take(20).ToList();

                return Ok(allJudges);
            }
            catch (BookingsApiException e)
            {
                if (e.StatusCode == (int)HttpStatusCode.BadRequest)
                {
                    return BadRequest(e.Response);
                }

                throw;
            }
        }


        /// <summary>
        /// Find judiciary person list by email search term.
        /// </summary>
        /// <param name = "term" > The email address search term.</param>
        /// <returns> The list of judiciary person</returns>
        [HttpPost(Name = "PostJudiciaryPersonBySearchTerm")]
        [SwaggerOperation(OperationId = "PostJudiciaryPersonBySearchTerm")]
        [ProducesResponseType(typeof(List<PersonResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<IList<PersonResponse>>> PostJudiciaryPersonBySearchTermAsync([FromBody] string term)
        {
            try
            {
                term = _encoder.Encode(term);
                var searchTerm = new SearchTermRequest(term);

                var courtRoomJudgesTask = _userAccountService.SearchEjudiciaryJudgesByEmailUserResponse(searchTerm.Term);
                var eJudiciaryJudgesTask = GetEjudiciaryJudgesBySearchTermAsync(searchTerm);

                await Task.WhenAll(courtRoomJudgesTask, eJudiciaryJudgesTask);

                var eJudiciaryJudges = (await eJudiciaryJudgesTask).Where(p => !p.Username.Contains(_testSettings.TestUsernameStem)).ToList();
                var courtRoomJudges = (await courtRoomJudgesTask)
                    .Where(x => !eJudiciaryJudges.Select(e => e.Username).Contains(x.ContactEmail))
                    .Select(x => UserResponseMapper.MapFrom(x));
                
                var allJudges = courtRoomJudges.Concat(eJudiciaryJudges)
                    .OrderBy(x => x.ContactEmail).Take(20).ToList();

                return Ok(allJudges);
            }
            catch (BookingsApiException e)
            {
                if (e.StatusCode == (int)HttpStatusCode.BadRequest)
                {
                    return BadRequest(e.Response);
                }

                throw;
            }
        }

        private async Task<IList<PersonResponse>> GetEjudiciaryJudgesBySearchTermAsync(SearchTermRequest term)
        {
            var personsResponse = (await _bookingsApiClient.PostJudiciaryPersonBySearchTermAsync(term)).ToList();

            return personsResponse;
        }
    }
}
