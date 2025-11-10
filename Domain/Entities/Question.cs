namespace SkillUpAPI.Domain.Entities
{
    public class Question
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public Test Test { get; set; } = null!;
        public string Text { get; set; } = null!;
        public QuestionType Type { get; set; } = QuestionType.Single;
        public string? OptionsJson { get; set; }    // JSON string for options
        public string CorrectAnswer { get; set; } = null!; // JSON or plain
        public int Points { get; set; } = 1;
    }
}
