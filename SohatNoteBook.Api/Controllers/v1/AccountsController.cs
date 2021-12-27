using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SohatNotebook.Authentication.Configuration;
using SohatNotebook.Authentication.Models.DTO.Generic;
using SohatNotebook.Authentication.Models.DTO.Incoming;
using SohatNotebook.Authentication.Models.DTO.Outgoing;
using SohatNotebook.DataService.IConfiguration;
using SohatNotebook.Entities.DbSet;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SohatNoteBook.Api.Controllers.v1
{
    public class AccountsController : BaseController
    {
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly JwtConfig _jwtConfig;

        public AccountsController(
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            TokenValidationParameters tokenValidationParameters,
            IOptionsMonitor<JwtConfig> optionMonitor)
            : base(unitOfWork, userManager)
        {
            _jwtConfig = optionMonitor.CurrentValue;
            _tokenValidationParameters = tokenValidationParameters;
        }

        // Register Action
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationRequestDto registrationDto)
        {
            // Check the model of obj we are recieving is valid
            if (ModelState.IsValid)
            {
                // Check if email already exist
                var userExist = await _userManager.FindByEmailAsync(registrationDto.Email);

                if (userExist is not null) // email is already in the table
                {
                    return BadRequest(new UserRegistrationResponseDto()
                    {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Email already in use"
                        }
                    });
                }
                // Add the user
                var newUser = new IdentityUser()
                {
                    Email = registrationDto.Email,
                    UserName = registrationDto.Email,
                    EmailConfirmed = true // ToDo build email functionality to send to the user to confirm email
                };

                // Addin the user to the table
                var isCreated = await _userManager.CreateAsync(newUser, registrationDto.Password);

                if (!isCreated.Succeeded) // when the registration has fail
                {
                    return BadRequest(new UserRegistrationResponseDto()
                    {
                        Success = false,
                        Errors = isCreated.Errors.Select(x => x.Description).ToList()
                    });
                }

                // Adding user to the database
                var _user = new User();
                _user.IdentityId = new Guid(newUser.Id);
                _user.LastName = registrationDto.LastName;
                _user.FirstName = registrationDto.FirstName;
                _user.Email = registrationDto.Email;
                _user.DateOfBirth = DateTime.UtcNow;
                _user.Phone = "";
                _user.Country = "";
                _user.Status = 1;

                await _unitOfWork.Users.Add(_user);
                await _unitOfWork.CompleteAsync();

                // Create a jwt token
                var token = await GenerateJwtToken(newUser);

                // Return back to the user
                return Ok(new UserRegistrationResponseDto()
                {
                    Success = true,
                    Token = token.JwtToken,
                    RefreshToken = token.RefreshToken
                });
            }
            else
            {
                return BadRequest(new UserRegistrationResponseDto
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Invalid payload"
                    }
                });
            }
        }

        // Login Action
        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequestDto loginDto)
        {
            if (ModelState.IsValid)
            {
                // 1 - Check if email exist
                var userExist = await _userManager.FindByEmailAsync(loginDto.Email);

                if (userExist is null)
                {
                    return BadRequest(new UserLoginResponseDto()
                    {
                        Success = false,
                        Errors = new List<string>
                        {
                            "Invalid authentication request"
                        }
                    });
                }

                // 2 -  Check if user have valid password
                var isCorrect = await _userManager.CheckPasswordAsync(userExist, loginDto.Password);

                if (isCorrect)
                {
                    // We need to generate jwt token
                    var jwtToken = await GenerateJwtToken(userExist);
                    return Ok(new UserLoginResponseDto()
                    {
                        Success = true,
                        Token = jwtToken.JwtToken,
                        RefreshToken = jwtToken.RefreshToken
                    });
                }
                else
                {
                    // Password doesn't match
                    return BadRequest(new UserLoginResponseDto()
                    {
                        Success = false,
                        Errors = new List<string>
                        {
                            "Invalid authentication request"
                        }
                    });
                }

            }
            else
            {
                return BadRequest(new UserLoginResponseDto()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Invalid payload"
                    }
                });
            }
        }

        [HttpPost]
        [Route("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequestDto tokenRequestDto)
        {
            if (ModelState.IsValid)
            {
                // Check if the token is valid
                var result = await VerifyToken(tokenRequestDto);

                if (result is null)
                {
                    return BadRequest(new UserRegistrationResponseDto()
                    {
                        Success = false,
                        Errors = new List<string>()
                        {
                            "Token validation fail"
                        }
                    });
                }

                return Ok(result);
            }
            else
            {
                return BadRequest(new UserRegistrationResponseDto()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Invalid payload"
                    }
                });
            }
        }

        private async Task<AuthResult> VerifyToken(TokenRequestDto tokenRequestDto)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                // We need check the validity of the token
                var principal = tokenHandler.ValidateToken(tokenRequestDto.Token,
                                    _tokenValidationParameters, out var validatedToken);

                // We need to validate the results that has been generated for us
                // Validate if the string is an actual JWT token not a random string
                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    // check if the jwt token is created with the same algorithms as our jwt token
                    var result = jwtSecurityToken.Header.Alg
                        .Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

                    if (result is false)
                    {
                        return null;
                    }
                }

                // We need to check expiry date of the token
                var utcExpiryDate = long.Parse(
                    principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value
                );

                // Convert to date to check
                var expDate = UnixTimeStampToDateTime(utcExpiryDate);

                // Check if the jwt token has expired
                if (expDate > DateTime.UtcNow)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Errors = new List<string>(){
                            "Jwt token has not expired"
                        }
                    };
                }

                // check if the refresh token exist
                var refreshTokenExist = await _unitOfWork.RefreshTokens.GetByRefreshToken(tokenRequestDto.RefreshToken);

                if (refreshTokenExist is null)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Errors = new List<string>(){
                            "Invalid refresh token"
                        }
                    };
                }

                // check the expiry date of a refresh token
                if (refreshTokenExist.ExpiryDate < DateTime.UtcNow)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Errors = new List<string>(){
                            "Refresh token has expired, please login again"
                        }
                    };
                }

                // check if refresh token has been used or not
                if (refreshTokenExist.IsUsed)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Errors = new List<string>(){
                            "Refresh token has been used, it cannot be reused"
                        }
                    };
                }

                // check refresh token if it has been revoked
                if (refreshTokenExist.IsRevoked)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Errors = new List<string>(){
                            "Refresh token has been revoked, it cannot be used"
                        }
                    };
                }

                var jti = principal.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

                if (refreshTokenExist.JwtId != jti)
                {
                    return new AuthResult
                    {
                        Success = false,
                        Errors = new List<string>(){
                            "Refresh token reference does not match the jwt token"
                        }
                    };
                }

                //  Start processing and get new token
                refreshTokenExist.IsUsed = true;

                var updateResult = await _unitOfWork.RefreshTokens.MarkRefreshTokenAsUsed(refreshTokenExist);

                if (updateResult)
                {
                    await _unitOfWork.CompleteAsync();

                    // Get the user to generate a new jwt token
                    var dbUser = await _userManager.FindByIdAsync(refreshTokenExist.UserId);

                    if (dbUser is null)
                    {
                        return new AuthResult
                        {
                            Success = false,
                            Errors = new List<string>(){
                                "Error processing request"
                            }
                        };
                    }

                    // Generate a jwt token
                    var tokens = await GenerateJwtToken(dbUser);

                    return new AuthResult
                    {
                        Token = tokens.JwtToken,
                        Success = true,
                        RefreshToken = tokens.RefreshToken
                    };
                }

                return new AuthResult
                {
                    Success = false,
                    Errors = new List<string>(){
                        "Error processing request"
                    }
                };

            }
            catch (Exception)
            {
                // TODO: Add better error handling, and add a logger
                return null;
            }
        }

        private DateTime UnixTimeStampToDateTime(long unixDate)
        {
            // Set the time to 1 Jan 1970
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            // Add the number of seconds from 1 Jan 1970
            dateTime = dateTime.AddSeconds(unixDate).ToUniversalTime();
            return dateTime;
        }

        private async Task<TokenData> GenerateJwtToken(IdentityUser user)
        {
            // the handler is going to be responsible for creating the token
            var jwtHandler = new JwtSecurityTokenHandler();

            // get the security key
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // used by the refresh token
                }),
                Expires = DateTime.UtcNow.Add(_jwtConfig.ExpiryTimeFrame), // TODO update the expiration time to minutes
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature // TODO review the algorithm
                )
            };

            // generate the security obj token
            var token = jwtHandler.CreateToken(tokenDescriptor);

            // convert the security obj token into a string
            var jwtToken = jwtHandler.WriteToken(token);

            // Generate a refresh token
            var refreshToken = new RefreshToken
            {
                AddedDate = DateTime.UtcNow,
                Token = $"{RandomStringGenerator(25)}_{Guid.NewGuid()}",
                UserId = user.Id,
                IsRevoked = false,
                IsUsed = false,
                Status = 1,
                JwtId = token.Id,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
            };

            await _unitOfWork.RefreshTokens.Add(refreshToken);
            await _unitOfWork.CompleteAsync();

            var tokenData = new TokenData
            {
                JwtToken = jwtToken,
                RefreshToken = refreshToken.Token
            };

            return tokenData;
        }

        private string RandomStringGenerator(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSZTUVWXYZ0123456789";

            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
