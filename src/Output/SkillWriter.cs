
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using UnityEngine;
using WikiHelper.Models;

namespace WikiHelper.Output;

public static class SkillWriter
{
    public static void WriteSkillFiles(
        Dictionary<string, ActionData> action,
        Dictionary<string, TraitData> traits,
        Dictionary<string, SigTraitData> sigTraits
    )
    {
        Directory.CreateDirectory(FileWriter.DirectoryPath);

        WriteActionFile(action);
        WriteTraitFile(traits);
        WriteSigTraitFile(sigTraits);
    }

    private static void WriteActionFile(Dictionary<string, ActionData> actions)
    {
        using StreamWriter outputFile = new StreamWriter(FileWriter.FilePath("actions"));

        // Split by type
        List<ActionData> starting = actions.Values.Where(action => action.Starting).OrderBy(p => p.Name).ToList();
        List<ActionData> basic = actions.Values.Where(action => !action.Starting).OrderBy(p => p.Name).ToList();

        // Header
        outputFile.WriteLine("actionData = {");

        // Starting Actions
        outputFile.WriteLine("\t-- STARTING ACTIONS --");
        foreach (var action in starting)
        {
            WriteAction(action, outputFile);
        }

        // List of Actions
        outputFile.WriteLine("\t-- LIST OF ACTIONS --");
        foreach (var action in basic)
        {
            WriteAction(action, outputFile);
        }

        // Footer
        outputFile.WriteLine("}");
        outputFile.WriteLine("");
        outputFile.WriteLine("return actionData");
    }

    private static void WriteAction(ActionData action, StreamWriter outputFile)
    {
        outputFile.WriteLine($"\t[\"{action.Name.ToUpperInvariant()}\"] = {{");
        outputFile.WriteLine($"\t\tname\t\t= \"{action.Name}\",");
        outputFile.WriteLine($"\t\tstarting\t= {(action.Starting ? "true" : "false")},");
        outputFile.WriteLine($"\t\tmaverick\t= {(action.Maverick ? "true" : "false")},");
        outputFile.WriteLine($"\t\tattack\t\t= {(action.Attack ? "true" : "false")},");
        outputFile.WriteLine($"\t\tfree\t\t= {(action.Free ? "true" : "false")},");
        if (!action.Types.Any())
        {
            outputFile.WriteLine("\t\ttypes\t\t= {},");
        }
        else
        {
            string types = string.Join(", ", action.Types.Select(e => $"\"{e}\""));
            types = "{" + types + "}";
            outputFile.WriteLine($"\t\ttypes\t\t= {types},");
        }
        string elements = string.Join(", ", action.Elements.Select(e => $"\"{e}\""));
        elements = "{" + elements + "}";
        outputFile.WriteLine($"\t\telements\t= {elements},");
        outputFile.WriteLine($"\t\trequires\t= \"{action.Requires}\",");
        outputFile.WriteLine($"\t\teffect\t\t= \"{action.Effect}\",");
        if (!action.Key.Any())
        {
            outputFile.WriteLine("\t\tkey\t\t\t= {},");
        }
        else
        {
            string keys = string.Join(", ", action.Key.Select(e => $"\"{e}\""));
            keys = "{" + keys + "}";
            outputFile.WriteLine($"\t\tkey\t\t\t= {keys},");
        }
        outputFile.WriteLine($"\t\tcategory\t= {{}},");
        outputFile.WriteLine($"\t}},");
    }

    private static void WriteTraitFile(Dictionary<string, TraitData> traits)
    {
        using StreamWriter outputFile = new StreamWriter(FileWriter.FilePath("traits"));

        // Header
        outputFile.WriteLine("local traits = {");

        foreach (var trait in traits.Values.OrderBy(p => p.Name))
        {
            WriteTrait(trait, outputFile);
        }

        // Footer
        outputFile.WriteLine("}");
        outputFile.WriteLine("");
        outputFile.WriteLine("return traits");
    }

    private static void WriteTrait(TraitData trait, StreamWriter outputFile)
    {
        outputFile.WriteLine($"\t[\"{trait.Name.ToUpperInvariant()}\"] = {{");
        outputFile.WriteLine($"\t\tname\t\t= \"{trait.Name}\",");
        outputFile.WriteLine($"\t\tmaverick\t= {(trait.Maverick ? "true" : "false")},");
        outputFile.WriteLine($"\t\taura\t\t= {(trait.Aura ? "true" : "false")},");
        if (!trait.Types.Any())
        {
            outputFile.WriteLine("\t\ttypes\t\t= {},");
        }
        else
        {
            string types = string.Join(", ", trait.Types.Select(e => $"\"{e}\""));
            types = "{" + types + "}";
            outputFile.WriteLine($"\t\ttypes\t\t= {types},");
        }
        outputFile.WriteLine($"\t\trequires\t= \"{trait.Requires}\",");
        outputFile.WriteLine($"\t\teffect\t\t= \"{trait.Effect}\",");
        if (!trait.Key.Any())
        {
            outputFile.WriteLine("\t\tkey\t\t\t= {},");
        }
        else
        {
            string keys = string.Join(", ", trait.Key.Select(e => $"\"{e}\""));
            keys = "{" + keys + "}";
            outputFile.WriteLine($"\t\tkey\t\t\t= {keys},");
        }
        outputFile.WriteLine($"\t\tcategory\t= {{}},");
        outputFile.WriteLine($"\t}},");
    }

    private static void WriteSigTraitFile(Dictionary<string, SigTraitData> traits)
    {
        using StreamWriter outputFile = new StreamWriter(FileWriter.FilePath("sig_traits"));

        // Header
        outputFile.WriteLine("local traits = {");

        foreach (var trait in traits.Values.OrderBy(p => p.Name))
        {
            WriteSigTrait(trait, outputFile);
        }

        // Footer
        outputFile.WriteLine("}");
        outputFile.WriteLine("");
        outputFile.WriteLine("return traits");
    }

    private static void WriteSigTrait(SigTraitData trait, StreamWriter outputFile)
    {
        string index = trait.Shifted ? $"{trait.Name.ToUpperInvariant()}-S" : trait.Name.ToUpperInvariant();
        outputFile.WriteLine($"\t[\"{index}\"] = {{");
        outputFile.WriteLine($"\t\tname\t\t= \"{trait.Name}\",");
        outputFile.WriteLine($"\t\tshifted\t\t= {(trait.Shifted ? "true" : "false")},");
        outputFile.WriteLine($"\t\tmonster\t\t= {(trait.Aura ? "true" : "false")},");
        outputFile.WriteLine($"\t\taura\t\t= {(trait.Aura ? "true" : "false")},");
        outputFile.WriteLine($"\t\teffect\t\t= \"{trait.Effect}\",");
        if (!trait.Key.Any())
        {
            outputFile.WriteLine("\t\tkey\t\t\t= {},");
        }
        else
        {
            string keys = string.Join(", ", trait.Key.Select(e => $"\"{e}\""));
            keys = "{" + keys + "}";
            outputFile.WriteLine($"\t\tkey\t\t\t= {keys},");
        }
        outputFile.WriteLine($"\t\tcategory\t= {{}},");
        outputFile.WriteLine($"\t}},");
    }
}