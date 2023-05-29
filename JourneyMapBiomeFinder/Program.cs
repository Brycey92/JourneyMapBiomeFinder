using Substrate;
using Substrate.Core;
using Substrate.Nbt;
using System.Xml.Linq;

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
                Console.WriteLine("\tbiome_name:\te.g. \"minecraft:plains\"");
                Console.WriteLine("\tpath:\tPath to your Minecraft instance's journeymap/data/mp/YourServerName folder");
                Console.WriteLine("\t\tThe server name folder may have a long string of letters, numbers, and \'~\' after the name");
                return;
            }

            if (!Directory.Exists(args[1]))
            {
                Console.WriteLine(args[1] + " does not exist or is not a folder, aborting");
                return;
            }

            Console.WriteLine("Searching for biomes...");

            string[] files = Directory.GetFiles(args[1], "*", SearchOption.AllDirectories);
            foreach (string path in files)
            {
                if (FindBiomesInFile(path, args[0]) != 0)
                {
                    return;
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
            
            RegionFile regionFile = TryCreateFrom(path);

            if(regionFile == null)
            {
                Console.WriteLine(path + " is not a valid region file, aborting");
                return -1;
            }

            // Each region file has up to 32 * 32 = 1024 chunks
            for (int x = 0; x < 32; x++)
            {
                for (int z = 0; z < 32; z++)
                {
                    if (regionFile.HasChunk(x, z))
                    {
                        NbtTree chunkTree = new NbtTree();
                        chunkTree.ReadFrom(regionFile.GetChunkDataInputStream(x, z));

                        if (chunkTree.Root == null)
                        {
                            Console.WriteLine(path + " chunk " + x + "," + z + " has a null root, aborting");
                            return -1;
                        }

                        // Each chunk has blocks
                        foreach (string key in chunkTree.Root.Keys)
                        {
                            TagNode blockNode = chunkTree.Root[key];

                            if (!blockNode.IsCastableTo(TagType.TAG_COMPOUND))
                            {
                                Console.WriteLine(path + " chunk " + x + "," + z + " is not castable to TagNodeCompound, aborting");
                                return -1;
                            }

                            TagNodeCompound blockCompound = blockNode.ToTagCompound();

                            if(!blockCompound.ContainsKey(biomeNameStr))
                            {
                                continue;
                            }

                            TagNode biomeNameNode = blockCompound[biomeNameStr];

                            if (biomeNameNode == null)
                            {
                                Console.WriteLine(path + " chunk " + x + "," + z + " block (X,Z) " + key + " \"biome_name\" value is null, aborting");
                                return -1;
                            }

                            if (!biomeNameNode.IsCastableTo(TagType.TAG_STRING))
                            {
                                Console.WriteLine(path + " chunk " + x + "," + z + " block (X,Z) " + key + " \"biome_name\" value is not castable to string, aborting");
                                return -1;
                            }

                            if (!biomeNameNode.ToString().Equals(biomeToFind))
                            {
                                continue;
                            }

                            Console.WriteLine("Found " + biomeToFind + " at (X,Z) " + key);
                        }
                    }
                }
            }
            return 0;
        }

        private static RegionFile? TryCreateFrom(string path)
        {
            try
            {
                return new RegionFile(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
    }
}