using System;
using Computer.Bus.Domain.Contracts;

namespace Computer.Bus.Integration;

public record MapRegistration(Type Domain, Type Dto, Type Mapper) : IMapRegistration;