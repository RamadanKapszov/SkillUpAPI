using System.Collections.Generic;

namespace SkillUpAPI.Domain.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public List<Course> Courses { get; set; } = new();
    }
}
