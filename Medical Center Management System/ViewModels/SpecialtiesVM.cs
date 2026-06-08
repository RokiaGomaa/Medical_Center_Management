using System.ComponentModel.DataAnnotations;

namespace Medical_Center_Management_System.ViewModels
{
    public class SpecialtiesVM
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Max 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

    }
}
