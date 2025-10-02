using System.ComponentModel.DataAnnotations;

namespace SkillUpAPI.DTOs.CourseDTOs
{
    public class CourseCreateDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public int? CategoryId { get; set; }

        // Admin may set TeacherId; teacher requests will have TeacherId forced to the caller
        public int? TeacherId { get; set; }
    }
}
