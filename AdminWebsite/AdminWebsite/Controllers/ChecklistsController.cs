﻿using System.Net;
using System.Threading.Tasks;
using AdminWebsite.Models;
using AdminWebsite.Security;
using AdminWebsite.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AdminWebsite.Controllers
{
    /// <summary>
    /// Responsible for retrieving reference data when requesting a booking.
    /// </summary>
    [Produces("application/json")]
    [Route("api/checklists")]
    [ApiController]
    public class ChecklistsController : ControllerBase
    {
        private readonly IBookingsApiClient _bookingsApiClient;
        private readonly IUserIdentity _userIdentity;

        public ChecklistsController(IBookingsApiClient bookingsApiClient, IUserIdentity userIdentity)
        {
            _bookingsApiClient = bookingsApiClient;
            _userIdentity = userIdentity;
        }
        
        /// <summary>
        /// Gets list of all submitted participant checklists including participant and hearing details.
        /// Ordered by checklist submission date, most recent checklist first.
        /// </summary>
        /// <param name="pageSize">Maximum number of items to retrieve in the page, maximum allowed 1000.</param>
        /// <param name="page">One-based index of page to retrieve.</param>
        /// <returns>The list of the participants questionnaire answers.</returns>
        [HttpGet]
        [SwaggerOperation(OperationId = "GetAllParticipantsChecklists")]
        [ProducesResponseType(typeof(ChecklistsResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetAllParticipantsChecklists(int pageSize = 5, int page = 1)
        {
            if (!_userIdentity.IsVhOfficerAdministratorRole())
                return Unauthorized();

            var response = new ChecklistsResponse(); // await _bookingsApiClient.GetAllParticipantsChecklistsAsync(pageSize, page);
            return Ok(response);
        }
    }
}