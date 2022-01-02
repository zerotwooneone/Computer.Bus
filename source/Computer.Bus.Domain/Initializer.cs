using Computer.Bus.Domain.Contracts;

namespace Computer.Bus.Domain;

public class Initializer
{
    public IReadOnlyDictionary<string, SubjectRegistration>? RegistrationsBySubject => _registrationsBySubject;
    private Dictionary<string, SubjectRegistration>? _registrationsBySubject = null;

    public void Register(IEnumerable<ISubjectRegistration> registrations,
        IEnumerable<IMapRegistration> maps)
    {
        var dtoToDomain = ToDictionary(maps);
        if (_registrationsBySubject == null)
        {
            _registrationsBySubject = new Dictionary<string, SubjectRegistration>(registrations.Select(r =>ToKvp(r, dtoToDomain)));
            return;
        }

        foreach (var kvp in registrations.Select(r =>ToKvp(r, dtoToDomain)))
        {
            _registrationsBySubject.Add(kvp.Key, kvp.Value);
        }
    }

    private static Dictionary<Type, IMapRegistration> ToDictionary(IEnumerable<IMapRegistration> x)
    {
        return x.Aggregate(new Dictionary<Type, IMapRegistration>(), (d, r) =>
        {
            d[r.Dto] = r;
            return d;
        });
    }

    private static KeyValuePair<string, SubjectRegistration> ToKvp(ISubjectRegistration arg, 
        IReadOnlyDictionary<Type, IMapRegistration> mapsByDto)
    {
        if (arg.Type == null)
        {
            return new KeyValuePair<string, SubjectRegistration>(arg.SubjectName, new SubjectRegistration(null, null, null));    
        }

        var dtoToDomainKvp = mapsByDto.TryGetValue(arg.Type, out var map)
            ? (map.Domain, map.Mapper)
            : (null, null);
        return new KeyValuePair<string, SubjectRegistration>(arg.SubjectName, new SubjectRegistration(arg.Type, dtoToDomainKvp.Domain, dtoToDomainKvp.Mapper));
    }
}

public record SubjectRegistration(Type? Dto, Type? Domain, Type? Mapper);