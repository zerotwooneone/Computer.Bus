using System;
using System.Collections.Generic;
using Computer.Bus.Domain.Contracts;

namespace Computer.Bus.Integration;

internal class MapperFactory : IMapperFactory
{
    private readonly Dictionary<Type, IMapper> _maps;
    private readonly publishDtoNameSpace.ExampleClassMapper _publishMapper = new();
    private readonly subscribeDtoNameSpace.ExampleClassMapper _subscribeMapper = new();

    public MapperFactory()
    {
        _maps = new Dictionary<Type, IMapper>
        {
            {typeof(publishDtoNameSpace.ExampleClassMapper), _publishMapper},
            {typeof(subscribeDtoNameSpace.ExampleClassMapper), _subscribeMapper}
        };
    }

    public IMapper GetMapper(Type mapperType, Type dto, Type domain)
    {
        if (_maps.TryGetValue(mapperType, out var mapper))
        {
            return mapper;
        }

        throw new ArgumentException($"unknown type {dto} not mapped");
    }
}