using Microsoft.AspNetCore.Identity;
using MyApp.Shared.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Auth.Domain.Entities
{
    /// <summary>
    /// Represents the relationship between a user and a role.
    /// </summary>
    public class ApplicationUserRole : IdentityUserRole<Guid>//, IEntity<Guid>
    {
        //[Key]
        //public Guid Id { get; set ; }

        /// <summary>
        /// Gets or sets the user associated with this user-role assignment.
        /// </summary>
        public ApplicationUser User { get; set; } = default!;

        /// <summary>
        /// Gets or sets the role assigned to this user.
        /// </summary>
        public ApplicationRole Role { get; set; } = default!;
    }
}
