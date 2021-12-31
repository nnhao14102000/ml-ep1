using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SohatNotebook.Configuration.Messages;
using SohatNotebook.DataService.IConfiguration;
using SohatNotebook.Entities.DbSet;
using SohatNotebook.Entities.Dtos.Generic;
using SohatNotebook.Entities.Dtos.Incoming;
using SohatNotebook.Entities.Dtos.Outgoing.Profile;

namespace SohatNoteBook.Api.Controllers.v1;
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsersController : BaseController
{
    public UsersController(
        IUnitOfWork unitOfWork,
        UserManager<IdentityUser> userManager,
        IMapper mapper)
        : base(unitOfWork, userManager, mapper)
    {
    }

    // Get
    [HttpGet]
    [Route("{id}", Name = "GetUser")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = new Result<ProfileDto>();
        var user = await _unitOfWork.Users.GetById(id);
        if (user is not null)
        {
            var mapUser = _mapper.Map<ProfileDto>(user);
            result.Content = mapUser;
            return Ok(result);
        }

        result.Error = PopulateError(404,
                MessageErrors.Users.UserNotFound,
                MessageErrors.Generic.ObjectNotFound);
        return NotFound(result);
    }

    // Post
    [HttpPost]
    public async Task<IActionResult> AddUser(UserDto user)
    {
        var _mapUser = _mapper.Map<User>(user);

        await _unitOfWork.Users.Add(_mapUser);
        await _unitOfWork.CompleteAsync();

        // TODO: Add the correct return to this action
        var result = new Result<UserDto>();
        result.Content = user;
        return CreatedAtRoute("GetUser", new { id = _mapUser.Id }, result);
    }

    // Get all
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var result = new PagedResult<ProfileDto>();

        var users = await _unitOfWork.Users.All();
        var mapUsers = _mapper.Map<IEnumerable<ProfileDto>>(users);
        result.Content = mapUsers.ToList();
        result.ResultCount = mapUsers.Count();
        return Ok(result);
    }
}
