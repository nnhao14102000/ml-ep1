using AutoMapper;
using SohatNotebook.Entities.DbSet;
using SohatNotebook.Entities.Dtos.Incoming;
using SohatNotebook.Entities.Dtos.Outgoing.Profile;
using System;

namespace SohatNoteBook.Api.Profiles
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            // Source --> Destination
            CreateMap<UserDto, User>();
            CreateMap<User, ProfileDto>();
        }
    }
}
