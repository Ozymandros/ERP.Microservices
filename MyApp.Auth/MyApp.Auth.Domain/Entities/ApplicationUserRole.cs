using Microsoft.AspNetCore.Identity;
using MyApp.Shared.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Auth.Domain.Entities
{
    public class ApplicationUserRole: IdentityUserRole<Guid>//, IEntity<Guid>
    {
        //[Key]
        //public Guid Id { get; set ; }

        public ApplicationUser User { get; set; } = default!;
        public ApplicationRole Role { get; set; } = default!;
    }
}
