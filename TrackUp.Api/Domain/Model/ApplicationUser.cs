using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop.Infrastructure;

namespace TrackUp.Api_.Domain.Model
{
    public class ApplicationUser : IdentityUser
    {
        //IdentityUser already gives you:   صح! هذا الكلاس بيمثّل المستخدم في تطبيقنا، وبحدد البيانات اللي بدنا نخزّنها لكل مستخدم، مثل
        //                                  FirstName وLastName،  IdentityUser with a  الإضافة للخصائص الموروثة  ex:  Id وEmail وUserName وPasswordHash.                             
        //Id
        //UserName
        //Email
        //PasswordHash
        //PhoneNumber
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? EmailConfirmationTokenHash { get; set; }
        public DateTime? EmailConfirmationTokenExpiresAt { get; set; }
        public DateTime? LastConfirmationEmailSentAt { get; set; }
    }
}