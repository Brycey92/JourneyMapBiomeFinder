using Substrate;
using Substrate.Core;
using Substrate.Nbt;

namespace JourneyMapBiomeFinder
{
    class Program
    {
        private static string biomeNameStr = "biome_name";

        private static void Main(string[] args)
        {
            if (args.Length < 2 || args[0] == null || args[0].Trim().Equals("") || args[1] == null || args[1].Trim().Equals(""))
            {
                Console.WriteLine("Usage: <biome_name> <path>");
                Console.WriteLine("\tbiome_name:\te.g. \"minecraft:cold_beach\"");
                Console.WriteLine("\tpath:\tPath to your Minecraft instance's journeymap/data/mp/ServerName folder");
                return;
            }

            if (Directory.Exists(args[1]))
            {
                string[] files = Directory.GetFiles(args[1], "*", SearchOption.AllDirectories);
                foreach (string path in files)
                {
                    FindBiomesInFile(path, args[0]);
                }
            }

            Console.WriteLine("Finished searching for biomes");
        }

        private static int FindBiomesInFile(string path, string biomeToFind)
        {
            if(!Path.GetExtension(path).ToLower().Equals(".mca"))// || !Path.GetFileName(path).StartsWith('r'))
            {
                return 0;
            }
            Console.WriteLine(path);
            NbtTree tree = TryCreateFrom(path);

            foreach (var chunkNode in tree.Root)
            {
                if(!chunkNode.Value.IsCastableTo(TagType.TAG_LIST))
                {
                    continue;
                }

                foreach(var blockNode in chunkNode.Value.ToTagCompound())
                {
                    TagNodeCompound blockCompound = blockNode.Value.ToTagCompound();

                    if (!blockCompound.ContainsKey(biomeNameStr))// || !blockCompound[biomeNameStr].Equals(biomeToFind))
                    {
                        continue;
                    }

                    Console.WriteLine("Found " + biomeToFind + " at (X,Z) " + blockNode.Key);
                }
            }
            return 0;
        }

        public static NbtTree TryCreateFrom(string path)
        {
            return TryCreateFrom(path, CompressionType.GZip)
                ?? TryCreateFrom(path, CompressionType.None);
        }

        private static NbtTree TryCreateFrom(string path, CompressionType compressionType)
        {
            Console.WriteLine(compressionType.ToString());
            try
            {
                NBTFile file = new NBTFile(path);
                NbtTree tree = new NbtTree();
                tree.ReadFrom(file.GetDataInputStream(compressionType));

                Console.WriteLine(tree.ToString());
                Console.WriteLine(tree.Root);
                if (tree.Root == null)
                    return null;

                return tree;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
    }
}