using AdminWebsite.Models;
using AdminWebsite.Security;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AdminWebsite.Contracts.Responses;
using AdminWebsite.Mappers;
using AdminWebsite.Services;
using BookingsApi.Client;
using BookingsApi.Contract.Responses;
using HearingTypeResponse = AdminWebsite.Contracts.Responses.HearingTypeResponse;

namespace AdminWebsite.Controllers
{
    /// <summary>
    /// Responsible for retrieving reference data when requesting a booking.
    /// </summary>
    [Produces("application/json")]
    [Route("api/reference")]
    [ApiController]
    public class ReferenceDataController : ControllerBase
    {
        private readonly IBookingsApiClient _bookingsApiClient;
        private readonly IPublicHolidayRetriever _publicHolidayRetriever;
        private readonly IUserIdentity _identity;

        /// <summary>
        /// Instantiate the controller
        /// </summary>
        public ReferenceDataController(IBookingsApiClient bookingsApiClient, IUserIdentity identity, IPublicHolidayRetriever publicHolidayRetriever)
        {
            _bookingsApiClient = bookingsApiClient;
            _identity = identity;
            _publicHolidayRetriever = publicHolidayRetriever;
        }

        /// <summary>
        ///     Gets a list hearing types
        /// </summary>
        /// <returns>List of hearing types</returns>
        [HttpGet("types", Name = "GetHearingTypes")]
        [ProducesResponseType(typeof(IList<HearingTypeResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<IList<HearingTypeResponse>>> GetHearingTypes()
        {
            var caseTypes = await _bookingsApiClient.GetCaseTypesAsync();
            var reult = caseTypes.SelectMany(caseType => caseType.HearingTypes.Select(hearingType => new HearingTypeResponse
            {
                Group = caseType.Name,
                Id = hearingType.Id,
                Name = hearingType.Name
            })).ToList();

            return Ok(reult);
        }

        /// <summary>
        ///     Get available participant roles
        /// </summary>
        /// <returns>List of valid participant roles</returns>
        [HttpGet("participantroles", Name = "GetParticipantRoles")]
        [ProducesResponseType(typeof(IList<CaseAndHearingRolesResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<IList<CaseAndHearingRolesResponse>>> GetParticipantRoles(string caseTypeName)
        {
            var response = new List<CaseAndHearingRolesResponse>();

            var caseRoles = await _bookingsApiClient.GetCaseRolesForCaseTypeAsync(caseTypeName);
            if (caseRoles != null && caseRoles.Any())
            {
                foreach (var item in caseRoles)
                {
                    var caseRole = new CaseAndHearingRolesResponse { Name = item.Name };
                    var hearingRoles = await _bookingsApiClient.GetHearingRolesForCaseRoleAsync(caseTypeName, item.Name);
                    
                    caseRole.HearingRoles = hearingRoles.ToList().ConvertAll(x => new HearingRole(x.Name, x.UserRole));

                    response.Add(caseRole);
                }
            }

            return Ok(response);
        }

        /// <summary>
        ///     Get available courts
        /// </summary>
        /// <returns>List of courts</returns>
        [HttpGet("courts", Name = "GetCourts")]
        [ProducesResponseType(typeof(IList<HearingVenueResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<IList<HearingVenueResponse>>> GetCourts()
        {
            var response = await _bookingsApiClient.GetHearingVenuesAsync();
            return Ok(response);
        }

        /// <summary>
        ///     Get upcoming public holidays in England and Wales
        /// </summary>
        /// <returns>List upcoming public holidays</returns>
        [HttpGet("public-holidays")]
        [ProducesResponseType(typeof(IList<PublicHolidayResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<IList<PublicHolidayResponse>>> PublicHolidays()
        {
            var holidays = await _publicHolidayRetriever.RetrieveUpcomingHolidays();
            var response = holidays.Select(PublicHolidayResponseMapper.MapFrom).ToList();
            return Ok(response);
        }
    }
}