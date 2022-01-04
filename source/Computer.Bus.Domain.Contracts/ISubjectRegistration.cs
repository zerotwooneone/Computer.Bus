namespace Computer.Bus.Domain.Contracts;

public interface ISubjectRegistration
{
    string SubjectName { get; }
    Type? Type { get; }
}