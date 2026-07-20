using System.ComponentModel.DataAnnotations;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.DTOs
{
    public class UserAddressDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Label { get; set; } = null!;
        public string AddressText { get; set; } = null!;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public PropertyType PropertyType { get; set; }
        public bool IsDefault { get; set; }
    }

    public class CreateUserAddressDto
    {
        [Required]
        public string AddressText { get; set; } = null!;

        public string Label { get; set; } = "Home";

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        [Required]
        public PropertyType PropertyType { get; set; }

        public bool IsDefault { get; set; }
    }

    public class UpdateUserAddressDto : CreateUserAddressDto
    {
    }
}