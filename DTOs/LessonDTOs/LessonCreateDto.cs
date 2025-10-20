using System.ComponentModel.DataAnnotations;

namespace SkillUpAPI.DTOs.LessonDTOs
{
    public class LessonCreateDto
    {
        [Required]
        public int CourseId { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = null!;

        [Required, Url]
        public string ContentUrl { get; set; } = null!;

        [Required]
        public int OrderIndex { get; set; }
    }
}
