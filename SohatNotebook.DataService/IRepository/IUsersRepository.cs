using System;
using System.Threading.Tasks;
using SohatNotebook.Entities.DbSet;

namespace SohatNotebook.DataService.IRepository
{
    public interface IUsersRepository: IGenericRepository<User>
    {
         Task<bool> UpdateUserProfile(User user);
         Task<User> GetUserByIdentityId(Guid identityId);
    }
}