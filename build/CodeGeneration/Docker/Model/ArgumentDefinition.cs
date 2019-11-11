﻿// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/docker/blob/master/LICENSE

using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace CodeGeneration.Docker.Model
{
    [UsedImplicitly]
    class ArgumentDefinition : DefinitionBase
    {
        [YamlMember(Alias = "option")] public string Name { get; set; }
        [YamlMember(Alias = "shorthand")] public string Shorthand { get; set; }
        [YamlMember(Alias = "value_type")] public string ValueType { get; set; }
        [YamlMember(Alias = "default_value")] public string DefaultValue { get; set; }
        [YamlMember(Alias = "description")] public string Description { get; set; }
        [YamlMember(Alias = "os_type")] public string OsType { get; set; }
    }
}
