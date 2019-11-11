﻿// Copyright Sebastian Karasek, Matthias Koch 2018.
// Distributed under the MIT License.
// https://github.com/nuke-build/docker/blob/master/LICENSE

using System;
using System.IO;
using Nuke.CodeGeneration;
using Nuke.Common.IO;

namespace CodeGeneration.Docker
{
    public static class SpecificationGenerator
    {
        public static void GenerateSpecifications(SpecificationGeneratorSettings settings)
        {
            Console.WriteLine("Generating docker specifications...");
            var definitions =
                DefinitionFetcher.GetCommandDefinitionsFromFolder(settings.DefinitionFolder, settings.Reference, settings.CommandsToSkip);
            var tool = DefinitionParser.GenerateTool(definitions);

            Directory.CreateDirectory(settings.OutputFolder);
            ToolSerializer.Save(tool, PathConstruction.Combine(settings.OutputFolder, "Docker.json"));

            Console.WriteLine();
            Console.WriteLine("Generation finished.");
            Console.WriteLine($"Created Tasks: {tool.Tasks.Count}");
            Console.WriteLine($"Created Data Classes: {tool.DataClasses.Count}");
            Console.WriteLine($"Created Enumerations: {tool.Enumerations.Count}");
            Console.WriteLine($"Created Common Task Properties: {tool.CommonTaskProperties.Count}");
            Console.WriteLine($"Created Common Task Property Sets: {tool.CommonTaskPropertySets.Count}");
        }
    }
}
