﻿using AdminWebsite.Contracts.Responses;
using AdminWebsite.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.JsonPatch;

namespace AdminWebsite.Controllers
{
    [Produces("application/json")]
    [Route("api/accounts")]
    public class UserDataController : ControllerBase
    {
        private readonly IUserAccountService _userAccountService;

        public UserDataController(IUserAccountService userAccountService)
        {
            _userAccountService = userAccountService;
        }

        /// <summary>
        ///     Get Judges
        /// </summary>
        [HttpGet("judges", Name = "GetJudges")]
        [ProducesResponseType(typeof(IList<JudgeResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public ActionResult<IList<JudgeResponse>> GetJudges()
        {
            var response = _userAccountService.GetJudgeUsers();
            return Ok(response);
        }

        [HttpPatch("updateUser")]
        [SwaggerOperation(OperationId = "UpdateUser")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> UpdateUser([FromBody]string userName)
        {
            try
            {
                await _userAccountService.UpdateParticipantPassword(userName);
                return NoContent();
            }
            catch (UserAPI.Client.UserServiceException e)
            {
                if (e.StatusCode == (int)HttpStatusCode.BadRequest)
                {
                    return BadRequest(e.Response);
                }
                if (e.StatusCode == (int)HttpStatusCode.NotFound)
                {
                    return NotFound(e.Response);
                }
                throw;
            }
        }
    }
}