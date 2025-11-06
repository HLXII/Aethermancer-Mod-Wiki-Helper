
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WikiHelper.Models;

namespace WikiHelper.Output;

public static class EquipWriter
{
    public static void WriteFiles(
        IEnumerable<EquipmentData> equips
    )
    {
        Directory.CreateDirectory(FileWriter.DirectoryPath);

        WriteEquipFile(equips);
    }

    private static void WriteEquipFile(IEnumerable<EquipmentData> equips)
    {
        using StreamWriter outputFile = new StreamWriter(FileWriter.FilePath("equips"));

        // Header
        outputFile.WriteLine("local equipment = {");

        foreach (var equip in equips)
        {
            WriteEquip(equip, outputFile);
        }

        // Footer
        outputFile.WriteLine("}");
        outputFile.WriteLine("");
        outputFile.WriteLine("return equipment");
    }

    private static void WriteEquip(EquipmentData equip, StreamWriter outputFile)
    {
        outputFile.WriteLine($"\t[\"{equip.Name.ToUpperInvariant()}\"] = {{");
        outputFile.WriteLine($"\t\tname\t\t= \"{equip.Name}\",");
        outputFile.WriteLine($"\t\tcommon\t\t= \"{equip.Common}\",");
        outputFile.WriteLine($"\t\trare\t\t= \"{equip.Rare}\",");
        outputFile.WriteLine($"\t\tepic\t\t= \"{equip.Epic}\",");

        if (!equip.Key.Any())
        {
            outputFile.WriteLine("\t\tkey\t\t\t= {},");
        }
        else
        {
            string keys = string.Join(", ", equip.Key.Select(e => $"\"{e}\""));
            keys = "{" + keys + "}";
            outputFile.WriteLine($"\t\tkey\t\t\t= {keys},");
        }
        outputFile.WriteLine($"\t\tcategory\t= {{}},");
        outputFile.WriteLine($"\t}},");
    }
}