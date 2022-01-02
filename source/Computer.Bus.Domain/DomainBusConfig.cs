using Computer.Bus.Domain.Contracts;

namespace Computer.Bus.Domain;

public class DomainBusConfig
{
    private readonly Initializer _initializer;

    public DomainBusConfig(Initializer initializer)
    {
        _initializer = initializer;
    }
    /// <summary>
    /// Replaces all existing registrations with those provided
    /// </summary>
    /// <param name="registrations">subject to dto relationships</param>
    /// <param name="maps">dto to domain type relationships</param>
    public void Register(IEnumerable<ISubjectRegistration> registrations,
        IEnumerable<IMapRegistration> maps)
    {
        _initializer.Register(registrations, maps);
    }
}