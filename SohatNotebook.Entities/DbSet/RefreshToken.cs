using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SohatNotebook.Entities.DbSet
{
    public class RefreshToken : BaseEntity
    {
        public string UserId { get; set; } // User Id when logged in
        public string Token { get; set; }
        public string JwtId { get; set; } // the Id generated when a jwt Id has been requested
        public bool IsUsed { get; set; } // make sure that the token is only used once
        public bool IsRevoked { get; set; } // make sure they are valid
        public DateTime ExpiryDate { get; set; }

        [ForeignKey(nameof(UserId))]
        public IdentityUser User {get;set;}
    }
}