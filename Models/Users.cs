using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; } = default!;

        [Required, StringLength(100)]
        public string LastName { get; set; } = default!;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = default!;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public class CreateUserRequest
    {
        [Required, StringLength(100)]
        public string FirstName { get; set; } = default!;

        [Required, StringLength(100)]
        public string LastName { get; set; } = default!;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = default!;

        public bool IsActive { get; set; } = true;
    }

    public class UpdateUserRequest
    {
        [Required, StringLength(100)]
        public string FirstName { get; set; } = default!;

        [Required, StringLength(100)]
        public string LastName { get; set; } = default!;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = default!;

        public bool IsActive { get; set; } = true;
    }
}
