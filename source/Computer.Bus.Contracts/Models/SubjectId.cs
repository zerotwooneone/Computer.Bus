namespace Computer.Bus.Contracts.Models
{
    public record SubjectId : ISubjectId
    {
        public string SubjectName { get; init; } = "Uninitialized Subject Name";
    }
}