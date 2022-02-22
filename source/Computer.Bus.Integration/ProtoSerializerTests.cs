using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Computer.Bus.ProtobuffNet;
using NUnit.Framework;
using ProtoBuf;

namespace Computer.Bus.Integration;

public class ProtoSerializerTests
{
    private ProtoSerializer _protoSerializer;
    [SetUp]
    public void Setup()
    {
        _protoSerializer = new ProtoSerializer();
    }

    /// <summary>
    /// The lesson here is to use Concrete types of lists on the receiving end
    /// </summary>
    /// <exception cref="NullReferenceException"></exception>
    [Test]
    public async Task Test1()
    {
        var response = new DefaultListResponse
        {
            List = new ListModel
            {
                Id = "some app id",
                Items = new List<ItemModel>
                {
                    new ItemModel
                    {
                        Checked = true,
                        Text = "Success!!!"
                    }
                }
            },
            Success = true
        };

        string eventId = Guid.NewGuid().ToString();
        string correlationId = Guid.NewGuid().ToString();
        var serializeResult = await _protoSerializer.Serialize(response, typeof(DefaultListResponse), eventId, correlationId).ConfigureAwait(false);
        
        Assert.IsTrue(serializeResult.Success);
        Assert.IsNotNull(serializeResult.Param);
        if (serializeResult.Param == null)
        {
            throw new NullReferenceException("something");
        }
        var deserializationResult = await _protoSerializer.Deserialize(serializeResult.Param, typeof(DefaultListResponse)).ConfigureAwait(false);

        Assert.IsTrue(deserializationResult.Success);
        Assert.IsNotNull(deserializationResult.Param);
        
        var list = (DefaultListResponse?)deserializationResult.Param?.Payload;
        Assert.AreEqual("Success!!!", list?.List?.Items?.FirstOrDefault()?.Text);
    }
    
    [ProtoContract]
    public class ItemModel
    {
        [ProtoMember(1)]
        public string? Text { get; set; } = null;
        [ProtoMember(2)]
        public string? Url { get; set; } = null;
        [ProtoMember(3)]
        public string? ImageUrl { get; set; } = null;
        [ProtoMember(4)]
        public bool? Checked { get; set; } = false;
    }
    [ProtoContract]
    public class ListModel
    {
        [ProtoMember(1)]
        public string? Id { get; set; } = "not set";
        [ProtoMember(2)]
        public List<ItemModel>? Items { get; set; } = new List<ItemModel>();
    }
    [ProtoContract]
    public class DefaultListResponse
    {
        [ProtoMember(1)]
        public bool? Success { get; init; }

        [ProtoMember(2)]
        public ListModel? List { get; init; }
    }
}