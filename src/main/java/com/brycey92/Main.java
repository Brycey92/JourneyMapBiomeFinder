package com.brycey92;

import net.querz.nbt.io.NBTUtil;
import net.querz.nbt.io.NamedTag;
import net.querz.nbt.tag.CompoundTag;
import net.querz.nbt.tag.Tag;

//import dev.dewy.nbt.Nbt;
//import dev.dewy.nbt.tags.collection.CompoundTag;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.Map.Entry;
import java.util.stream.Stream;

public class Main {
    static String biomeName;
    public static void main(String[] args) {
        // Check whether we have an argument
        if (args.length < 2 || args[0] == null || args[0].trim().isEmpty() || args[1] == null || args[1].trim().isEmpty()) {
            String jarPath = Main.class
                    .getProtectionDomain()
                    .getCodeSource()
                    .getLocation()
                    .getPath();
            System.out.println("Usage: java -jar " + jarPath + " <biome_name> <path>");
            System.out.println("\tbiome_name:\te.g. \"minecraft:cold_beach\"");
            System.out.println("\tpath:\tPath to your Minecraft instance's journeymap/data/mp/ServerName folder");
            System.exit(-1);
            return;
        }

        biomeName = args[0];

        // Read the file
        File file = new File(args[1]);
        if(!file.isDirectory()) {
            System.err.println("Path is not a folder");
            System.exit(-1);
            return;
        }

        try (Stream<Path> stream = Files.walk(Paths.get(args[1]), Integer.MAX_VALUE)) {
            stream.filter(Files::isRegularFile)
                    .forEach(Main::findBiomeInFile);
        } catch (IOException e) {
            e.printStackTrace();
            System.exit(-1);
            return;
        }

        System.out.println("Finished searching folder");
    }

    private static void findBiomeInFile(Path path) {
        File file = path.toFile();

        if (file.isDirectory() || !file.getName().endsWith(".mca")) {
            return;
        }

        NamedTag rootNamedTag;
//        CompoundTag rootCompoundTag;
        try {
//            rootCompoundTag = new Nbt().fromFile(file);
//            System.out.println(rootCompoundTag.getName());
            rootNamedTag = NBTUtil.read(file);
        } catch (IOException e) {
            e.printStackTrace();
            System.exit(-1);
            return;
        }

        /*
        if (!rootNamedTag.getName().endsWith(".mca")) {
            System.err.println("Found root tag not ending in \".mca\": " + rootNamedTag.getName() + " in file " + path);
            System.exit(-1);
            return;
        }
        */

        Tag<?> rootTag = rootNamedTag.getTag();
        System.out.println(rootTag.getClass().getName());
        if (!(rootTag instanceof CompoundTag)) {
            System.err.println("Found non-CompoundTag root tag " + rootNamedTag.getName() + " in file " + path);
            System.exit(-1);
            return;
        }
        CompoundTag rootCompoundTag = (CompoundTag) rootTag;
        for (Entry<String, Tag<?>> chunkEntry : rootCompoundTag.entrySet()) {
            if (!chunkEntry.getKey().startsWith("Chunk [")) {
                System.err.println("Found Tag not starting with \"Chunk[\": " + chunkEntry.getKey() + " in file " + path);
                System.exit(-1);
                return;
            }

            Tag<?> chunkTag = chunkEntry.getValue();
            if (!(chunkTag instanceof CompoundTag)) {
                System.err.println("Found non-CompoundTag " + chunkEntry.getKey() + " in file " + path);
                System.exit(-1);
                return;
            }

            CompoundTag chunkCompoundTag = (CompoundTag) chunkTag;

            for (Entry<String, Tag<?>> blockEntry : chunkCompoundTag.entrySet()) {
                Tag<?> blockTag = blockEntry.getValue();
                if (!(blockTag instanceof CompoundTag)) {
                    System.err.println("Found non-CompoundTag " + blockEntry.getKey() + " in file " + path);
                    System.exit(-1);
                    return;
                }

                CompoundTag blockCompoundTag = (CompoundTag) blockTag;
                System.out.println(blockCompoundTag.getString("biome_name"));

                if (biomeName.equals(blockCompoundTag.getString("biome_name"))) {
                    System.out.println("Found " + biomeName + " at (X,Z) " + blockEntry.getKey());
                    break;
                }
            }
        }
    }
}