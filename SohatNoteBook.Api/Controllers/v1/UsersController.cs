using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SohatNotebook.Configuration.Messages;
using SohatNotebook.DataService.IConfiguration;
using SohatNotebook.Entities.DbSet;
using SohatNotebook.Entities.Dtos.Generic;
using SohatNotebook.Entities.Dtos.Incoming;

namespace SohatNoteBook.Api.Controllers.v1;
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsersController : BaseController
{
    public UsersController(
        IUnitOfWork unitOfWork,
        UserManager<IdentityUser> userManager)
        : base(unitOfWork, userManager)
    {
    }

    // Get
    [HttpGet]
    [Route("{id}", Name = "GetUser")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = new Result<User>();
        var user = await _unitOfWork.Users.GetById(id);
        if (user is not null)
        {
            result.Content = user;
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
        var result = new Result<User>();

        var _user = new User();
        _user.LastName = user.LastName;
        _user.FirstName = user.FirstName;
        _user.Email = user.Email;
        _user.DateOfBirth = Convert.ToDateTime(user.DateOfBirth);
        _user.Phone = user.Phone;
        _user.Country = user.Country;
        _user.Status = 1;

        await _unitOfWork.Users.Add(_user);
        await _unitOfWork.CompleteAsync();

        return CreatedAtRoute("GetUser", new { id = _user.Id }, user);
    }

    // Get all
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var result = new PagedResult<User>();

        var users = await _unitOfWork.Users.All();
        result.Content = users.ToList();
        result.ResultCount = users.Count();
        return Ok(result);
    }
}
