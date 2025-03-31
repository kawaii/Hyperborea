using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Hyperborea;
public class YamlFactory : DefaultSerializationFactory
{
    public override string DefaultConfigFileName => "DefaultConfig.yaml";

    public override T Deserialize<T>(string inputData)
    {
        return new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build().Deserialize<T>(inputData);
    }

    public override string Serialize(object s, bool prettyPrint)
    {
        return new SerializerBuilder().Build().Serialize(s);
    }
}
