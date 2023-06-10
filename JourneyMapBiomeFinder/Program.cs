using Substrate;
using Substrate.Core;
using Substrate.Nbt;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Linq;

namespace JourneyMapBiomeFinder
{
    class Program
    {
        private static string blockTagNameStr = "block";
        private static string blockNameStr = "Name";
        private static string biomeNameStr = "biome_name";

        private enum SearchType
        {
            BIOME_SEARCH,
            BLOCK_SEARCH
        }

        private enum ReturnAction
        {
            NO_ACTION,
            CONTINUE_ACTION,
            ERROR_ACTION
        }

        private static void Main(string[] args)
        {
            if (args.Length < 3 ||
                args[0] == null || args[0].Trim().Equals("") ||
                args[1] == null || args[1].Trim().Equals("") ||
                args[2] == null || args[2].Trim().Equals(""))
            {
                PrintUsage();
                return;
            }

            SearchType searchType;
            if (args[0].Equals("--biome"))
            {
                searchType = SearchType.BIOME_SEARCH;
            }
            else if (args[0].Equals("--block"))
            {
                searchType = SearchType.BLOCK_SEARCH;
            }
            else
            {
                Console.WriteLine("Unrecognized argument \"" + args[0] + "\", aborting\n");
                PrintUsage();
                return;
            }

            if (!Directory.Exists(args[2]))
            {
                Console.WriteLine(args[2] + " does not exist or is not a folder, aborting");
                return;
            }

            Console.WriteLine("Searching for " + args[0].Trim('-') + "s...");

            string[] files = Directory.GetFiles(args[2], "*", SearchOption.AllDirectories);
            foreach (string path in files)
            {
                if (FindInFile(path, args[1], searchType) != 0)
                {
                    return;
                }
            }

            Console.WriteLine("Finished searching for " + args[0].Trim('-') + "s");
        }

        private static int FindInFile(string path, string stringToFind, SearchType searchType)
        {
            if (!Path.GetExtension(path).ToLower().Equals(".mca"))// || !Path.GetFileName(path).StartsWith('r'))
            {
                return 0;
            }

            RegionFile regionFile = TryCreateFrom(path);

            if (regionFile == null)
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
                        try
                        {
                            chunkTree.ReadFrom(regionFile.GetChunkDataInputStream(x, z));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\nIn " + path + " chunk " + x + "," + z + ":");
                            Console.WriteLine(e.StackTrace + '\n');
                            continue;
                        }

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

                            ReturnAction returnAction;
                            switch (searchType)
                            {
                                case SearchType.BIOME_SEARCH:
                                    returnAction = FindBiomeInBlockNode(blockCompound, stringToFind, path, x, z, key);
                                    break;
                                case SearchType.BLOCK_SEARCH:
                                    returnAction = FindBlockInBlockNode(blockCompound, stringToFind, path, x, z, key);
                                    break;
                                default:
                                    returnAction = ReturnAction.ERROR_ACTION;
                                    break;
                            }

                            switch (returnAction)
                            {
                                case ReturnAction.NO_ACTION:
                                    break;
                                case ReturnAction.CONTINUE_ACTION:
                                    continue;
                                case ReturnAction.ERROR_ACTION:
                                default:
                                    return -1;
                            }
                        }
                    }
                }
            }
            return 0;
        }

        private static ReturnAction FindBiomeInBlockNode([NotNull] TagNodeCompound blockCompound, string biomeToFind, string path, int x, int z, string key)
        {
            // Each block has a string called "biome_name"
            if (!blockCompound.ContainsKey(biomeNameStr))
            {
                return ReturnAction.CONTINUE_ACTION;
            }

            TagNode biomeNameNode = blockCompound[biomeNameStr];

            if (biomeNameNode == null)
            {
                Console.WriteLine(path + " chunk " + x + "," + z + " block (X,Z) " + key + " \"" + biomeNameStr + "\" value is null, aborting");
                return ReturnAction.ERROR_ACTION;
            }

            if (!biomeNameNode.IsCastableTo(TagType.TAG_STRING))
            {
                Console.WriteLine(path + " chunk " + x + "," + z + " block (X,Z) " + key + " \"" + biomeNameStr + "\" value is not castable to string, aborting");
                return ReturnAction.ERROR_ACTION;
            }

            if (!biomeToFind.Equals(biomeNameNode.ToString()))
            {
                return ReturnAction.CONTINUE_ACTION;
            }

            Console.WriteLine("Found " + biomeToFind + " at (X,Z) " + key);
            return ReturnAction.NO_ACTION;
        }

        private static ReturnAction FindBlockInBlockNode([NotNull] TagNodeCompound blockCompound, string blockToFind, string path, int x, int z, string key)
        {
            // Each block has a compound tag called "block"
            if (!blockCompound.ContainsKey(blockTagNameStr))
            {
                return ReturnAction.CONTINUE_ACTION;
            }

            TagNode blockBlockNode = blockCompound[blockTagNameStr];

            if (blockBlockNode == null)
            {
                Console.WriteLine(path + " chunk " + x + "," + z + " block (X,Z) " + key + " \"" + blockTagNameStr + "\" value is null, aborting");
                return ReturnAction.ERROR_ACTION;
            }

            if (!blockBlockNode.IsCastableTo(TagType.TAG_COMPOUND))
            {
                Console.WriteLine(path + " chunk " + x + "," + z + " block (X,Z) " + key + " \"" + blockTagNameStr + "\" value is not castable to TagNodeCompound, aborting");
                return ReturnAction.ERROR_ACTION;
            }

            TagNodeCompound blockBlockCompound = blockBlockNode.ToTagCompound();

            // Each "block" compound tag has a string called "Name"
            if (!blockBlockCompound.ContainsKey(blockNameStr))
            {
                return ReturnAction.CONTINUE_ACTION;
            }

            TagNode blockNameNode = blockBlockCompound[blockNameStr];

            if (blockNameNode == null)
            {
                Console.WriteLine(path + " chunk " + x + "," + z + " block (X,Z) " + key + " \"" + blockNameStr + "\" value is null, aborting");
                return ReturnAction.ERROR_ACTION;
            }

            if (!blockNameNode.IsCastableTo(TagType.TAG_STRING))
            {
                Console.WriteLine(path + " chunk " + x + "," + z + " block (X,Z) " + key + " \"" + blockNameStr + "\" value is not castable to string, aborting");
                return ReturnAction.ERROR_ACTION;
            }

            if (!blockToFind.Equals(blockNameNode.ToString()))
            {
                return ReturnAction.CONTINUE_ACTION;
            }

            Console.WriteLine("Found " + blockToFind + " at (X,Z) " + key);
            return ReturnAction.NO_ACTION;
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

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: [--biome <biome_name>]");
            Console.WriteLine("     | [--block <block_name>]");
            Console.WriteLine("       <path>");
            Console.WriteLine("\tbiome_name:\te.g. \"minecraft:plains\"");
            Console.WriteLine("\tblock_name:\re.g. \"minecraft:iron_block\"");
            Console.WriteLine("\tpath:\tPath to your Minecraft instance's journeymap/data/mp/YourServerName folder");
            Console.WriteLine("\t\tThe server name folder may have a long string of letters, numbers, and \'~\' characters after the name");
        }
    }
}