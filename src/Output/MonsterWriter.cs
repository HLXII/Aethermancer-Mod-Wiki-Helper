
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WikiHelper.Models;

namespace WikiHelper.Output;

public static class MonsterWriter
{
    public static void WriteFiles(Dictionary<string, MonsterData> monsters)
    {
        Directory.CreateDirectory(FileWriter.DirectoryPath);

        WriteMonsterFile(monsters);
    }

    private static void WriteMonsterFile(Dictionary<string, MonsterData> monsters)
    {
        using StreamWriter outputFile = new StreamWriter(FileWriter.FilePath("monsters"));

        // Header
        outputFile.WriteLine("local monsters = {");

        foreach (var monster in monsters.Values.OrderBy(p => p.MonsterId * 2 + (p.Shifted ? 1 : 0)))
        {
            WriteMonster(monster, outputFile);
        }

        // Footer
        outputFile.WriteLine("}");
        outputFile.WriteLine("");
        outputFile.WriteLine("return monsters");
    }

    private static void WriteMonster(MonsterData monster, StreamWriter outputFile)
    {
        string index = monster.Name.ToUpperInvariant() + (monster.Shifted ? "-S" : "");
        outputFile.WriteLine($"\t[\"{index}\"] = {{");
        outputFile.WriteLine($"\t\tid\t\t\t\t= {monster.MonsterId + (monster.Shifted ? 0.1 : 0)},");
        outputFile.WriteLine($"\t\tname\t\t\t= \"{monster.Name}\",");
        outputFile.WriteLine($"\t\tshifted\t\t\t= {(monster.Shifted ? "true" : "false")},");
        outputFile.WriteLine($"\t\tstarter\t\t\t= {(monster.Starter ? "true" : "false")},");
        outputFile.WriteLine($"\t\tambush\t\t\t= {(monster.Ambush ? "true" : "false")},");
        outputFile.WriteLine($"\t\thidden\t\t\t= {(monster.Hidden ? "true" : "false")},");
        outputFile.WriteLine($"\t\tchampion\t\t= {(monster.Champion ? "true" : "false")},");
        string elements = string.Join(", ", monster.Elements.Select(e => $"\"{e.ToLower()}\""));
        elements = "{" + elements + "}";
        outputFile.WriteLine($"\t\telements\t\t= {elements},");
        string types = string.Join(", ", monster.Types.Select(e => $"\"{e.ToLower()}\""));
        types = "{" + types + "}";
        outputFile.WriteLine($"\t\ttypes\t\t\t= {types},");
        outputFile.WriteLine($"\t\thealth\t\t\t= {monster.Health},");
        string startActions = string.Join(", ", monster.StartingActions.Select(e => $"\"{e.ToLower()}\""));
        startActions = "{" + startActions + "}";
        outputFile.WriteLine($"\t\tstart_actions\t= {startActions},");
        outputFile.WriteLine($"\t\tsig\t\t\t\t= \"{monster.SignatureTrait.ToLower()}\",");
        outputFile.WriteLine($"\t\tperks\t\t\t= {{");
        foreach ((var perk, var val) in monster.Perks)
        {
            string perkStr = "{" + $"\"{perk.ToLower()}\", {val}" + "}";
            outputFile.WriteLine($"\t\t\t{perkStr},");
        }
        outputFile.WriteLine("\t\t},");
        outputFile.WriteLine($"\t\tarchetype\t\t= \"{monster.Archetype}\",");

        if (!monster.Shifted)
        {
            outputFile.WriteLine($"\t\tsoulbond\t\t= {monster.Soulbond},");
            outputFile.WriteLine($"\t\tavailable\t\t= {(monster.Available ? "true" : "false")},");
        }

        outputFile.WriteLine($"\t}},");
    }
}