using Microsoft.AspNetCore.Identity;
using wirtualny_dziekanat.Data.Entities;

namespace wirtualny_dziekanat.Data
{
    public class ApplicationUser : IdentityUser
    {
        public Student? Student { get; set; }
        public Teacher? Teacher { get; set; }
    }

}
