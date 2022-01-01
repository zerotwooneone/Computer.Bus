using System;
using System.Collections.Generic;

namespace Computer.Bus.RabbitMq;

public static class Initializer
{
    internal static IReadOnlyDictionary<string, SubjectRegistration>? RegistrationsBySubject = null;

    public static void Register(IEnumerable<KeyValuePair<string, SubjectRegistration>> registrations)
    {
        if (RegistrationsBySubject != null)
        {
            throw new InvalidOperationException("Registrations may only be completed once");
        }
        RegistrationsBySubject = new Dictionary<string, SubjectRegistration>(registrations);
    }
}

public record SubjectRegistration();