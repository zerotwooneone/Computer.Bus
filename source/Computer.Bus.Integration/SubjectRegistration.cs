using System;
using Computer.Bus.Domain.Contracts;

namespace Computer.Bus.Integration;

public record SubjectRegistration(string SubjectName, Type? Type) : ISubjectRegistration;