using System.ComponentModel.DataAnnotations;

namespace WealthTracker.Models;

    public class User : CommonData
    {
        [Required]
        [StringLength(50)]
        public required string FirstName { get; set; } 
        [Required]
        [StringLength(50)]
        public required string LastName { get; set; }

        [StringLength(50)] 
        public string? Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public required string Email { get; set; }

        [Required]
        [StringLength(200)]
        public required string Password { get; set; }

        public ICollection<Transaction> Transactions { get; set; } = [];
        public ICollection<Category> Categories { get; set; } = []; 
    }