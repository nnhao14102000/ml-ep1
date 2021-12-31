using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SohatNotebook.Configuration.Messages;
using SohatNotebook.DataService.IConfiguration;
using SohatNotebook.Entities.DbSet;
using SohatNotebook.Entities.Dtos.Errors;
using SohatNotebook.Entities.Dtos.Generic;
using SohatNotebook.Entities.Dtos.Incoming.Profile;
using SohatNotebook.Entities.Dtos.Outgoing.Profile;
using System;
using System.Threading.Tasks;

namespace SohatNoteBook.Api.Controllers.v1;
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

public class ProfileController : BaseController
{
    public ProfileController(
        IUnitOfWork unitOfWork,
        UserManager<IdentityUser> userManager,
        IMapper mapper)
        : base(unitOfWork, userManager, mapper)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);
        var result = new Result<ProfileDto>();

        if (loggedInUser is null)
        {
            result.Error = PopulateError(400,
                MessageErrors.Profile.UserNotFound,
                MessageErrors.Generic.BadRequest);
            return BadRequest(result);
        }

        var identityId = new Guid(loggedInUser.Id);

        var profile = await _unitOfWork.Users.GetUserByIdentityId(identityId);

        if (profile is null)
        {
            result.Error = PopulateError(404,
                MessageErrors.Profile.UserNotFound,
                MessageErrors.Generic.NotFound);
            return NotFound(result);
        }
        var mapProfile = _mapper.Map<ProfileDto>(profile);
        result.Content = mapProfile;
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto profile)
    {
        var result = new Result<ProfileDto>();

        // If the model is valid
        if (!ModelState.IsValid)
        {
            result.Error = PopulateError(400,
                MessageErrors.Generic.InvalidPayload,
                MessageErrors.Generic.BadRequest);
            return BadRequest(result);
        }

        var loggedInUser = await _userManager.GetUserAsync(HttpContext.User);

        if (loggedInUser is null)
        {
            result.Error = PopulateError(400,
                MessageErrors.Profile.UserNotFound,
                MessageErrors.Generic.BadRequest);
            return BadRequest(result);
        }

        var identityId = new Guid(loggedInUser.Id);

        var userProfile = await _unitOfWork.Users.GetUserByIdentityId(identityId);

        if (userProfile is null)
        {
            result.Error = PopulateError(404,
                MessageErrors.Profile.UserNotFound,
                MessageErrors.Generic.NotFound);
            return NotFound(result);
        }

        userProfile.Address = profile.Address;
        userProfile.Sex = profile.Sex;
        userProfile.MobileNumber = profile.MobileNumber;
        userProfile.Country = profile.Country;

        var isUpdated = await _unitOfWork.Users.UpdateUserProfile(userProfile);

        if (isUpdated)
        {
            await _unitOfWork.CompleteAsync();

            var mapProfile = _mapper.Map<ProfileDto>(profile);
            result.Content = mapProfile;
            return Ok(result);
        }

        result.Error = PopulateError(500,
            MessageErrors.Generic.SomethinsWentWrong,
            MessageErrors.Generic.UnableToProcess);
        return BadRequest(result);
    }
}
