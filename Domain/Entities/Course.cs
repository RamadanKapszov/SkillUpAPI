using SkillUpAPI.Domain.Entities;

public class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public string? ShortDescription { get; set; }
    public string? Level { get; set; }
    public string? Duration { get; set; }
    public string? Language { get; set; }
    public string? Prerequisites { get; set; }
    public string? WhatYouWillLearn { get; set; }
    public string? WhoIsFor { get; set; }
    public string? Tags { get; set; }
    public double? Rating { get; set; }
    public int? StudentsCount { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public int TeacherId { get; set; }
    public User Teacher { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public string? ImageUrl { get; set; }

    public List<Lesson> Lessons { get; set; } = new();
    public List<Test> Tests { get; set; } = new();
    public List<Enrollment> Enrollments { get; set; } = new();
}
