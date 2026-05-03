using System.ComponentModel.DataAnnotations;

namespace MotorInsurance.API.DTOs.User
{
    public class UpdateUserRoleDto
    {
        [Required]
        [RegularExpression("^(Admin|Employee|Client)$", ErrorMessage = "Role must be Admin, Employee, or Client")]
        public string Role { get; set; } = null!;
    }
}
