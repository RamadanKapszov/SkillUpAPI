using System.ComponentModel.DataAnnotations;

namespace SkillUpAPI.DTOs.CourseDTOs
{
    public class CourseUpdateDto
    {
        [StringLength(200)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public int? CategoryId { get; set; }

        // Only admin is allowed to set/change TeacherId
        public int? TeacherId { get; set; }
    }
}
