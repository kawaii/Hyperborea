using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Hyperborea;
public class YamlFactory : ISerializationFactory
{
    public string DefaultConfigFileName => "DefaultConfig.yaml";

    public T Deserialize<T>(string inputData)
    {
        return new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build().Deserialize<T>(inputData);
    }

    public string Serialize(object s, bool prettyPrint)
    {
        return new SerializerBuilder().Build().Serialize(s);
    }
}
