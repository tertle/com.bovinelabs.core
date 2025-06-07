// <copyright file="AnalyzersProjectFileGeneration.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_ANALYZERS
namespace BovineLabs.Core.Editor.Analyzers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using UnityEditor;

    /// <summary>
    /// Customize the project file generation with Roslyn Analyzers and custom c# version.
    /// </summary>
    public class AnalyzersProjectFileGeneration : AssetPostprocessor
    {
        private const string Directory = "RoslynAnalyzers";

        private static string OnGeneratedCSProject(string path, string contents)
        {
            var fileName = Path.GetFileName(path);
            if (fileName.StartsWith("Unity."))
            {
                return contents;
            }

            var xml = XDocument.Parse(contents);

            UpgradeProjectFile(xml);

            // Write to the csproj file:
            using var str = new Utf8StringWriter();
            xml.Save(str);
            return str.ToString();
        }

        private static void UpgradeProjectFile(XDocument doc)
        {
            var projectContentElement = doc.Root;
            if (projectContentElement != null)
            {
                XNamespace xmlns = projectContentElement.Name.NamespaceName; // do not use var
                SetRoslynAnalyzers(projectContentElement, xmlns);
            }
        }

        /// <summary>
        /// Add everything from RoslynAnalyzers folder to csproj.
        /// </summary>
        private static void SetRoslynAnalyzers(XElement projectContentElement, XNamespace xmlns)
        {
            var currentDirectory = System.IO.Directory.GetCurrentDirectory();

            var roslynAnalyzerBaseDir = new DirectoryInfo(Path.Combine(currentDirectory, Directory));

            if (!roslynAnalyzerBaseDir.Exists)
            {
                return;
            }

            var relPaths = roslynAnalyzerBaseDir.GetFiles("*", SearchOption.AllDirectories).Select(x => x.FullName[(currentDirectory.Length + 1)..]);

            var itemGroup = new XElement(xmlns + "ItemGroup");

            foreach (var file in relPaths)
            {
                var extension = new FileInfo(file).Extension;

                switch (extension)
                {
                    case ".dll":
                    {
                        var reference = new XElement(xmlns + "Analyzer");
                        reference.Add(new XAttribute("Include", file));
                        itemGroup.Add(reference);
                        break;
                    }

                    case ".json":
                    {
                        var reference = new XElement(xmlns + "AdditionalFiles");
                        reference.Add(new XAttribute("Include", file));
                        itemGroup.Add(reference);
                        break;
                    }

                    case ".ruleset":
                    {
                        SetOrUpdateProperty(projectContentElement, xmlns, "CodeAnalysisRuleSet", _ => file);
                        break;
                    }
                }
            }

            projectContentElement.Add(itemGroup);
        }

        private static void SetOrUpdateProperty(XContainer root, XNamespace xmlns, string name, Func<string, string> updater)
        {
            var element = root.Elements(xmlns + "PropertyGroup").Elements(xmlns + name).FirstOrDefault();
            if (element != null)
            {
                var result = updater(element.Value);
                if (result != element.Value)
                {
                    element.SetValue(result);
                }
            }
            else
            {
                AddProperty(root, xmlns, name, updater(string.Empty));
            }
        }

        // Adds a property to the first property group without a condition
        private static void AddProperty(XContainer root, XNamespace xmlns, string name, string content)
        {
            var propertyGroup = root.Elements(xmlns + "PropertyGroup").FirstOrDefault(e => !e.Attributes(xmlns + "Condition").Any());
            if (propertyGroup == null)
            {
                propertyGroup = new XElement(xmlns + "PropertyGroup");
                root.AddFirst(propertyGroup);
            }

            propertyGroup.Add(new XElement(xmlns + name, content));
        }

        private class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }
    }
}
#endif
