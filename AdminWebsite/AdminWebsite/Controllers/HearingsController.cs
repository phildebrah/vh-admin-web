using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AdminWebsite.Attributes;
using AdminWebsite.Contracts.Enums;
using AdminWebsite.Contracts.Requests;
using AdminWebsite.Extensions;
using AdminWebsite.Helper;
using AdminWebsite.Mappers;
using AdminWebsite.Models;
using AdminWebsite.Security;
using AdminWebsite.Services;
using BookingsApi.Client;
using BookingsApi.Contract.Enums;
using BookingsApi.Contract.Requests;
using BookingsApi.Contract.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using VideoApi.Client;
using VideoApi.Contract.Consts;

namespace AdminWebsite.Controllers
{
    /// <summary>
    ///     Responsible for retrieving and storing hearing information
    /// </summary>
    [Produces("application/json")]
    [Route("api/hearings")]
    [ApiController]
    public class HearingsController : ControllerBase
    {
        private readonly IBookingsApiClient _bookingsApiClient;
        private readonly IValidator<EditHearingRequest> _editHearingRequestValidator;
        private readonly IHearingsService _hearingsService;
        private readonly IConferenceDetailsService _conferenceDetailsService;
        private readonly ILogger<HearingsController> _logger;
        private readonly IUserIdentity _userIdentity;
        private const int StartingSoonMinutesThreshold = 30;

        /// <summary>
        ///     Instantiates the controller
        /// </summary>
#pragma warning disable S107
        public HearingsController(IBookingsApiClient bookingsApiClient, 
            IUserIdentity userIdentity,
            IValidator<EditHearingRequest> editHearingRequestValidator,
            ILogger<HearingsController> logger, 
            IHearingsService hearingsService,
            IConferenceDetailsService conferenceDetailsService)
        {
            _bookingsApiClient = bookingsApiClient;
            _userIdentity = userIdentity;
            _editHearingRequestValidator = editHearingRequestValidator;
            _logger = logger;
            _hearingsService = hearingsService;
            _conferenceDetailsService = conferenceDetailsService;
        }
#pragma warning restore S107
        /// <summary>
        ///     Create a hearing
        /// </summary>
        /// <param name="request">Hearing Request object</param>
        /// <returns>VideoHearingId</returns>
        [HttpPost]
        [SwaggerOperation(OperationId = "BookNewHearing")]
        [ProducesResponseType(typeof(HearingDetailsResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [HearingInputSanitizer]
        public async Task<ActionResult<HearingDetailsResponse>> Post([FromBody] BookHearingRequest request)
        {
            var newBookingRequest = request.BookingDetails;
            newBookingRequest.IsMultiDayHearing = request.IsMultiDay;
            try
            {
                if (newBookingRequest.Endpoints != null && newBookingRequest.Endpoints.Any())
                {
                    var endpointsWithDa = newBookingRequest.Endpoints
                        .Where(x => !string.IsNullOrWhiteSpace(x.DefenceAdvocateContactEmail))
                        .ToList();
                    _hearingsService.AssignEndpointDefenceAdvocates(endpointsWithDa, newBookingRequest.Participants.AsReadOnly());
                }

                newBookingRequest.CreatedBy = _userIdentity.GetUserIdentityName();
                _logger.LogInformation("BookNewHearing - Attempting to send booking request to Booking API");
                var hearingDetailsResponse = await _bookingsApiClient.BookNewHearingAsync(newBookingRequest);
                _logger.LogInformation("BookNewHearing - Successfully booked hearing {Hearing}", hearingDetailsResponse.Id);

                return Created("",hearingDetailsResponse);
            }
            catch (BookingsApiException e)
            {
                _logger.LogError(e, "BookNewHearing - There was a problem saving the booking. Status Code {StatusCode} - Message {Message}",
                    e.StatusCode, e.Response);
                if (e.StatusCode == (int)HttpStatusCode.BadRequest) 
                    return BadRequest(e.Response);
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "BookNewHearing - Failed to save hearing - {Message} -  for request: {RequestBody}",
                    e.Message, JsonConvert.SerializeObject(newBookingRequest));
                throw;
            }
        }

        /// <summary>
        ///     Clone hearings with the details of a given hearing on given dates
        /// </summary>
        /// <param name="hearingId">Original hearing to clone</param>
        /// <param name="hearingRequest">The dates range to create the new hearings on</param>
        /// <returns></returns>
        [HttpPost("{hearingId}/clone")]
        [SwaggerOperation(OperationId = "CloneHearing")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CloneHearing(Guid hearingId, MultiHearingRequest hearingRequest)
        {
            _logger.LogDebug("Attempting to clone hearing {Hearing}", hearingId);

            var hearingDates = hearingRequest.HearingDates != null && hearingRequest.HearingDates.Any() ? hearingRequest.HearingDates.Skip(1).ToList() : DateListMapper.GetListOfWorkingDates(hearingRequest.StartDate, hearingRequest.EndDate);

            if (!hearingDates.Any())
            {
                _logger.LogWarning("No working dates provided to clone to");
                return BadRequest();
            }

            var cloneHearingRequest = new CloneHearingRequest { Dates = hearingDates };

            try
            {
                _logger.LogDebug("Sending request to clone hearing to Bookings API");
                await _bookingsApiClient.CloneHearingAsync(hearingId, cloneHearingRequest);
                _logger.LogDebug("Successfully cloned hearing {Hearing}", hearingId);

                var groupedHearings = await _bookingsApiClient.GetHearingsByGroupIdAsync(hearingId);

                var conferenceStatusToGet = groupedHearings.Where(x => x.Participants?.Any(x => x.HearingRoleName == RoleNames.Judge) ?? false);
                var tasks = conferenceStatusToGet.Select(x => GetConferenceStatus(x.Id, 
                    $"Failed to get the conference from video api, possibly the conference was not created or the kinly meeting room is null - hearingId: {x.Id}")).ToList();
                await Task.WhenAll(tasks);
                
                return NoContent();
            }
            catch (BookingsApiException e)
            {
                _logger.LogError(e,
                    "There was a problem cloning the booking. Status Code {StatusCode} - Message {Message}",
                    e.StatusCode, e.Response);
                if (e.StatusCode == (int)HttpStatusCode.BadRequest) return BadRequest(e.Response);
                throw;
            }
        }

        /// <summary>
        ///     Edit a hearing
        /// </summary>
        /// <param name="hearingId">The id of the hearing to update</param>
        /// <param name="request">Hearing Request object for edit operation</param>
        /// <returns>VideoHearingId</returns>
        [HttpPut("{hearingId}")]
        [SwaggerOperation(OperationId = "EditHearing")]
        [ProducesResponseType(typeof(HearingDetailsResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [HearingInputSanitizer]
        public async Task<ActionResult<HearingDetailsResponse>> EditHearing(Guid hearingId, [FromBody] EditHearingRequest request)
        {
            if (hearingId == Guid.Empty)
            {
                _logger.LogWarning("No hearing id found to edit");
                ModelState.AddModelError(nameof(hearingId), $"Please provide a valid {nameof(hearingId)}");
                return BadRequest(ModelState);
            }

            _logger.LogDebug("Attempting to edit hearing {Hearing}", hearingId);
            var result = _editHearingRequestValidator.Validate(request);
            if (!result.IsValid)
            {
                _logger.LogWarning("Failed edit hearing validation");
                ModelState.AddFluentValidationErrors(result.Errors);
                return BadRequest(ModelState);
            }

            HearingDetailsResponse originalHearing;
            try
            {
                originalHearing = await _bookingsApiClient.GetHearingDetailsByIdAsync(hearingId);
            }
            catch (BookingsApiException e)
            {
                _logger.LogError(e, "Failed to get hearing {Hearing}. Status Code {StatusCode} - Message {Message}",
                    hearingId, e.StatusCode, e.Response);
                if (e.StatusCode != (int)HttpStatusCode.NotFound) throw;
                return NotFound($"No hearing with id found [{hearingId}]");
            }
            try
            {
                if (IsHearingStartingSoon(originalHearing) && originalHearing.Status == BookingStatus.Created &&
                    !_hearingsService.IsAddingParticipantOnly(request, originalHearing) && 
                    !_hearingsService.IsUpdatingJudge(request, originalHearing))
                {
                    var errorMessage =
                        $"You can't edit a confirmed hearing [{hearingId}] within {StartingSoonMinutesThreshold} minutes of it starting";
                    _logger.LogWarning(errorMessage);
                    ModelState.AddModelError(nameof(hearingId), errorMessage);
                    return BadRequest(ModelState);
                }

                var judgeExistsInRequest = request?.Participants?.Any(p => p.HearingRoleName == RoleNames.Judge) ?? false;
                if (originalHearing.Status == BookingStatus.Created && !judgeExistsInRequest)
                {
                    const string errorMessage = "You can't edit a confirmed hearing if the update removes the judge";
                    _logger.LogWarning(errorMessage);
                    ModelState.AddModelError(nameof(hearingId), errorMessage);
                    return BadRequest(ModelState);
                }
                var updatedHearing = await _bookingsApiClient.GetHearingDetailsByIdAsync(hearingId);
                //Save hearing details
                var updateHearingRequest = HearingUpdateRequestMapper.MapTo(request, _userIdentity.GetUserIdentityName());
                await _bookingsApiClient.UpdateHearingDetailsAsync(hearingId, updateHearingRequest);
                await UpdateParticipants(hearingId, request, originalHearing);
                
                if (updatedHearing.Status == BookingStatus.Failed) return Ok(updatedHearing);
                if (!updatedHearing.HasScheduleAmended(originalHearing)) return Ok(updatedHearing);
                return Ok(updatedHearing);
            }
            catch (BookingsApiException e)
            {
                _logger.LogError(e, "Failed to edit hearing {Hearing}. Status Code {StatusCode} - Message {Message}",
                    hearingId, e.StatusCode, e.Response);
                if (e.StatusCode == (int)HttpStatusCode.BadRequest) return BadRequest(e.Response);
                throw;
            }
        }

        private async Task UpdateParticipants(Guid hearingId, EditHearingRequest request, HearingDetailsResponse originalHearing)
        {
            var existingParticipants = new List<UpdateParticipantRequest>();
            var newParticipants = new List<ParticipantRequest>();
            var judgeContact =_hearingsService.GetJudgeInformationForUpdate(request.OtherInformation);
            
            var removedParticipantIds = originalHearing.Participants.Where(p => request.Participants.All(rp => rp.Id != p.Id))
                .Select(x => x.Id).ToList();

            foreach (var participant in request.Participants)
            {
                if (!participant.Id.HasValue)
                {
                    if (await _hearingsService.ProcessNewParticipant(hearingId, participant, removedParticipantIds, originalHearing) is { } newParticipant)
                    {
                        if (newParticipant.HearingRoleName == HearingRoleName.Judge)
                        {
                            newParticipant.ContactEmail = judgeContact.email ?? newParticipant.ContactEmail;
                            newParticipant.TelephoneNumber =  judgeContact.phone ?? newParticipant.TelephoneNumber;
                        }
                        newParticipants.Add(newParticipant);
                    }
                }
                else
                {
                    var existingParticipant = originalHearing.Participants.FirstOrDefault(p => p.Id.Equals(participant.Id));
                    if (existingParticipant == null || string.IsNullOrEmpty(existingParticipant.UserRoleName))
                        continue;
                    if (existingParticipant.HearingRoleName == HearingRoleName.Judge)
                    {
                        participant.ContactEmail = judgeContact.email ?? existingParticipant.ContactEmail;
                        participant.TelephoneNumber = judgeContact.phone ?? existingParticipant.TelephoneNumber;
                    }
                    var updateParticipantRequest = UpdateParticipantRequestMapper.MapTo(participant);
                    existingParticipants.Add(updateParticipantRequest);
                }
            }

            var linkedParticipants = new List<LinkedParticipantRequest>();
            var participantsWithLinks = request.Participants
                .Where(x => x.LinkedParticipants.Any() && 
                            !removedParticipantIds.Contains(x.LinkedParticipants[0].LinkedId) &&
                            !removedParticipantIds.Contains(x.LinkedParticipants[0].ParticipantId))
                .ToList();

            for (int i = 0; i < participantsWithLinks.Count; i++)
            {
                var participantWithLinks = participantsWithLinks[i];
                var linkedParticipantRequest = new LinkedParticipantRequest
                {
                    LinkedParticipantContactEmail = participantWithLinks.LinkedParticipants[0].LinkedParticipantContactEmail,
                    ParticipantContactEmail = participantWithLinks.LinkedParticipants[0].ParticipantContactEmail ??
                                              participantWithLinks.ContactEmail,
                    Type = participantWithLinks.LinkedParticipants[0].Type
                };

                // If the participant link is not new and already existed, then the ParticipantContactEmail will be null. We find it here and populate it.
                // We also remove the participant this one is linked to, to avoid duplicating entries.
                if (participantWithLinks.Id.HasValue &&
                    existingParticipants.SingleOrDefault(x => x.ParticipantId == participantWithLinks.Id) != null)
                {
                    // Is the linked participant an existing participant?
                    var secondaryParticipantInLinkContactEmail = originalHearing.Participants
                        .SingleOrDefault(x => x.Id == participantWithLinks.LinkedParticipants[0].LinkedId)?
                        .ContactEmail ?? newParticipants
                        .SingleOrDefault(x => x.ContactEmail == participantWithLinks.LinkedParticipants[0].LinkedParticipantContactEmail)?
                        .ContactEmail;

                    // If the linked participant isn't an existing participant it will be a newly added participant                        
                    linkedParticipantRequest.LinkedParticipantContactEmail = secondaryParticipantInLinkContactEmail;

                    // If the linked participant is an already existing user they will be mapped twice, so we remove them here.
                    var secondaryParticipantInLinkIndex = participantsWithLinks
                        .FindIndex(x => x.ContactEmail == secondaryParticipantInLinkContactEmail);
                    if (secondaryParticipantInLinkIndex >= 0)
                        participantsWithLinks.RemoveAt(secondaryParticipantInLinkIndex);
                }
                linkedParticipants.Add(linkedParticipantRequest);
            }
            // participants
            await _hearingsService.ProcessParticipants(hearingId, existingParticipants, newParticipants,
                removedParticipantIds.ToList(), linkedParticipants.ToList());
            // endpoints
            await _hearingsService.ProcessEndpoints(hearingId, request, originalHearing, newParticipants);
        }

        private static bool IsHearingStartingSoon(HearingDetailsResponse originalHearing)
        {
            var timeToCheckHearingAgainst = DateTime.UtcNow.AddMinutes(StartingSoonMinutesThreshold);
            return originalHearing.ScheduledDateTime < timeToCheckHearingAgainst;
        }

        /// <summary>
        ///     Gets bookings hearing by Id.
        /// </summary>
        /// <param name="hearingId">The unique sequential value of hearing ID.</param>
        /// <returns> The hearing</returns>
        [HttpGet("{hearingId}")]
        [SwaggerOperation(OperationId = "GetHearingById")]
        [ProducesResponseType(typeof(HearingDetailsResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> GetHearingById(Guid hearingId)
        {
            try
            {
                var hearingResponse = await _bookingsApiClient.GetHearingDetailsByIdAsync(hearingId);
                return Ok(hearingResponse);
            }
            catch (BookingsApiException e)
            {
                if (e.StatusCode == (int)HttpStatusCode.BadRequest) return BadRequest(e.Response);
                throw;
            }
        }

        /// <summary>
        ///     Get hearings by case number.
        /// </summary>
        /// <param name="caseNumber">The case number.</param>
        /// <param name="date">The date to filter by</param>
        /// <returns> The hearing</returns>
        [HttpGet("audiorecording/search")]
        [SwaggerOperation(OperationId = "SearchForAudioRecordedHearings")]
        [ProducesResponseType(typeof(List<HearingsForAudioFileSearchResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> SearchForAudioRecordedHearingsAsync([FromQuery] string caseNumber,
            [FromQuery] DateTime? date = null)
        {
            try
            {
                var decodedCaseNumber = string.IsNullOrWhiteSpace(caseNumber) ? null : WebUtility.UrlDecode(caseNumber);
                var hearingResponse = await _bookingsApiClient.SearchForHearingsAsync(decodedCaseNumber, date);
                return Ok(hearingResponse.Select(HearingsForAudioFileSearchMapper.MapFrom));
            }
            catch (BookingsApiException ex)
            {
                if (ex.StatusCode == (int)HttpStatusCode.BadRequest) return BadRequest(ex.Response);
                throw;
            }
        }

        /// <summary>
        ///     Update the hearing status.
        /// </summary>
        /// <param name="hearingId">The hearing id</param>
        /// <param name="updateBookingStatusRequest"></param>
        /// <returns>Success status</returns>
        [HttpPatch("{hearingId}")]
        [SwaggerOperation(OperationId = "UpdateBookingStatus")]
        [ProducesResponseType(typeof(UpdateBookingStatusResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateBookingStatus(Guid hearingId,
            UpdateBookingStatusRequest updateBookingStatusRequest)
        {
            var errorMessage =
                $"Failed to get the conference from video api, possibly the conference was not created or the kinly meeting room is null - hearingId: {hearingId}";
            try
            {
                var hearing = await _bookingsApiClient.GetHearingDetailsByIdAsync(hearingId);
                var judgeExists = hearing?.Participants?.Any(p => p.HearingRoleName == RoleNames.Judge) ?? false;
                if (!judgeExists && updateBookingStatusRequest.Status == BookingsApi.Contract.Requests.Enums.UpdateBookingStatus.Created)
                    return BadRequest("This hearing has no judge");

                _logger.LogDebug("Attempting to update hearing {Hearing} to booking status {BookingStatus}", hearingId, updateBookingStatusRequest.Status);

                updateBookingStatusRequest.UpdatedBy = _userIdentity.GetUserIdentityName();
                await _bookingsApiClient.UpdateBookingStatusAsync(hearingId, updateBookingStatusRequest);

                _logger.LogDebug("Updated hearing {Hearing} to booking status {BookingStatus}", hearingId, updateBookingStatusRequest.Status);

                if (updateBookingStatusRequest.Status != BookingsApi.Contract.Requests.Enums.UpdateBookingStatus.Created)
                    return Ok(new UpdateBookingStatusResponse { Success = true });

                return await GetConferenceStatus(hearingId, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "There was an unknown error updating status for hearing {Hearing}", hearingId);
                if (updateBookingStatusRequest.Status == BookingsApi.Contract.Requests.Enums.UpdateBookingStatus.Created)
                {
                    // Set the booking status to failed as the video api failed
                    await _hearingsService.UpdateFailedBookingStatus(hearingId);

                    return Ok(new UpdateBookingStatusResponse { Success = false, Message = errorMessage });
                }
                if (ex is BookingsApiException)
                {
                    var e = ex as BookingsApiException;
                    if (e.StatusCode == (int)HttpStatusCode.BadRequest) return BadRequest(e.Response);
                    if (e.StatusCode == (int)HttpStatusCode.NotFound) return NotFound(e.Response);
                    return BadRequest(e);
                }
                return BadRequest(ex.Message);
            }
        }
        
        /// <summary>
        ///     Get the conference status.
        /// </summary>
        /// <param name="hearingId">The hearing id</param>
        /// <returns>Success status</returns>
        [HttpGet("{hearingId}/conference-status")]
        [SwaggerOperation(OperationId = "GetHearingConferenceStatus")]
        [ProducesResponseType(typeof(UpdateBookingStatusResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetHearingConferenceStatus(Guid hearingId)
        {
            var errorMessage = $"Failed to get the conference from video api, possibly the conference was not created or the kinly meeting room is null - hearingId: {hearingId}";
            try
            {
                _logger.LogDebug($"Hearing {hearingId} is confirmed. Polling for Conference in VideoApi");

                var conferenceDetailsResponse = await _conferenceDetailsService.GetConferenceDetailsByHearingId(hearingId);

                _logger.LogInformation($"Found conference for hearing {hearingId}");
                
                    if (conferenceDetailsResponse.HasValidMeetingRoom())
                    {
                        return Ok(new UpdateBookingStatusResponse
                        {
                            Success = true,
                            TelephoneConferenceId = conferenceDetailsResponse.MeetingRoom.TelephoneConferenceId
                        });
                    }
                    
                _logger.LogError("Could not find hearing {Hearing}. Updating status to failed", hearingId);
                return Ok(new UpdateBookingStatusResponse {Success = false, Message = errorMessage});
            }
            catch (VideoApiException e)
            {                                
                _logger.LogError(e, "Failed to confirm a hearing. {ErrorMessage}", errorMessage);
                if (e.StatusCode == (int) HttpStatusCode.NotFound)
                    return Ok(new UpdateBookingStatusResponse {Success = false, Message = errorMessage});
                
                _logger.LogError("There was an unknown error for hearing {Hearing}. Updating status to failed", hearingId);  
                return BadRequest(e.Response);
            }
        }

        /// <summary>
        ///     Update the failed hearing status.
        /// </summary>
        /// <param name="hearingId">The hearing id</param>
        /// <returns>Success status</returns>
        [HttpPut("{hearingId}/failed-status")]
        [SwaggerOperation(OperationId = "UpdateFailedBookingStatus")]
        [ProducesResponseType(typeof(UpdateBookingStatusResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.NotFound)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateHearingStatus(Guid hearingId)
        {
            var errorMessage = $"Failed to update the failed status for a hearing - hearingId: {hearingId}";
            try
            {
                await _hearingsService.UpdateFailedBookingStatus(hearingId);
                return Ok(new UpdateBookingStatusResponse {Success = false, Message = $"Status updated for hearing: {hearingId}"});
            }
            catch (VideoApiException e)
            {
                _logger.LogError(e, errorMessage);
                if (e.StatusCode == (int) HttpStatusCode.NotFound) return NotFound();
                if (e.StatusCode == (int) HttpStatusCode.BadRequest) return BadRequest(e.Response);
                throw;
            }
        }


        private async Task<IActionResult> GetConferenceStatus(Guid hearingId, string errorMessage)
        {
            try
            {
                _logger.LogDebug("Hearing {Hearing} is confirmed. Polling for Conference in VideoApi", hearingId);
                var conferenceDetailsResponse = await _conferenceDetailsService.GetConferenceDetailsByHearingIdWithRetry(hearingId, errorMessage);
                _logger.LogInformation("Found conference for hearing {Hearing}", hearingId);

                if (conferenceDetailsResponse.HasValidMeetingRoom())
                {
                    return Ok(new UpdateBookingStatusResponse
                    {
                        Success = true,
                        TelephoneConferenceId = conferenceDetailsResponse.MeetingRoom.TelephoneConferenceId
                    });
                }
                //If not a success
                await _hearingsService.UpdateFailedBookingStatus(hearingId);
                return Ok(new UpdateBookingStatusResponse { Success = false, Message = errorMessage });
                
            }
            catch (VideoApiException ex)
            {
                _logger.LogError(ex, "Failed to confirm a hearing. {ErrorMessage}", errorMessage);
                _logger.LogError("There was an unknown error for hearing {Hearing}. Updating status to failed", hearingId);

                // Set the booking status to failed as the video api failed
                await _hearingsService.UpdateFailedBookingStatus(hearingId);

                return Ok(new UpdateBookingStatusResponse { Success = false, Message = errorMessage });
            }
        }



        /// <summary>
        ///     Gets for confirmed booking the telephone conference Id by hearing Id.
        /// </summary>
        /// <param name="hearingId">The unique sequential value of hearing ID.</param>
        /// <returns> The telephone conference Id</returns>
        [HttpGet("{hearingId}/telephoneConferenceId")]
        [SwaggerOperation(OperationId = "GetTelephoneConferenceIdById")]
        [ProducesResponseType(typeof(PhoneConferenceResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> GetTelephoneConferenceIdById(Guid hearingId)
        {
            try
            {
                var conferenceDetailsResponse = await _conferenceDetailsService.GetConferenceDetailsByHearingId(hearingId);

                if (conferenceDetailsResponse.HasValidMeetingRoom())
                    return Ok(new PhoneConferenceResponse
                    {
                        TelephoneConferenceId = conferenceDetailsResponse.MeetingRoom.TelephoneConferenceId
                    });
                return NotFound();
            }
            catch (VideoApiException e)
            {
                if (e.StatusCode == (int)HttpStatusCode.NotFound) return NotFound();
                if (e.StatusCode == (int)HttpStatusCode.BadRequest) return BadRequest(e.Response);
                throw;
            }
        }
    }
}