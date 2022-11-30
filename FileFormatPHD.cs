using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace TR1GlidosDump
{
    public struct TRTexPage8BPP
    {
        public byte[] data;
    }

    public struct TRColour24
    {
        public byte r;
        public byte g;
        public byte b;
    }

    public struct TRTextureCoordinate
    {
        //public byte U1;
        //public byte U2;
        //public byte V1;
        //public byte V2;

        public ushort U;
        public ushort V;
    }

    public struct TRObjectTexture
    {
        public ushort attributes;
        public ushort tpageAndFlag;
        public TRTextureCoordinate[] coordinates;

        //Additional non-standard data for extraction.
        public uint X;
        public uint Y;
        public uint W;
        public uint H;
    }

    public struct TRSpriteTexture
    {
        public ushort tpage;
        public byte x;
        public byte y;
        public ushort width;
        public ushort height;
        public short left;
        public short top;
        public short right;
        public short bottom;
    }

    public class FileFormatPHD
    {
        public TRTexPage8BPP[] texpages;
        public TRObjectTexture[] objectTextures;
        public TRSpriteTexture[] spriteTextures;
        public TRColour24[] palette;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public FileFormatPHD(string filepath)
        {
            if(!File.Exists(filepath))
            {
                Console.WriteLine($"File does not exist: {filepath}");
                return;
            }

            using(BinaryInputStream bis = new BinaryInputStream(filepath))
            {
                //
                //Read PHD file
                //
                uint version = bis.ReadUInt32();
                if(version != 0x20)
                {
                    Console.WriteLine($"Unexpected file version: {version}. Should be 0x20");
                }

                //Read PHD 8BPP Texture Pages
                uint numTpage = bis.ReadUInt32();

                texpages = new TRTexPage8BPP[numTpage];
                for (int i = 0; i < numTpage; ++i)
                {
                    texpages[i].data = bis.ReadBytes(65536);
                }
                Console.WriteLine($"Number of TPages: {numTpage}");

                //
                // We now skip a load of data until we reach the palettes
                //
                uint unused1 = bis.ReadUInt32();

                if(unused1 != 0)
                {
                    Console.WriteLine($"Unexpected value in unused slot: {unused1}. Should be 0");
                }

                //Skip rooms
                ushort numRoom = bis.ReadUInt16();
                for(int i = 0; i < numRoom; ++i)
                {
                    //Skip Room Info
                    bis.Seek(16, SeekOrigin.Current);

                    //Skip Room Data
                    uint numRawU16 = bis.ReadUInt32();
                    bis.Seek(2 * numRawU16, SeekOrigin.Current);

                    //Skip Room Portals
                    ushort roomNumPortal = bis.ReadUInt16();
                    bis.Seek(32 * roomNumPortal, SeekOrigin.Current);

                    //Skip Room Sectors
                    ushort roomSectorsX = bis.ReadUInt16();
                    ushort roomSectorsZ = bis.ReadUInt16();
                    bis.Seek(8 * roomSectorsZ * roomSectorsX, SeekOrigin.Current);

                    //Skip room ambient intensity
                    bis.Seek(2, SeekOrigin.Current);

                    //Skip room lights
                    ushort roomNumLight = bis.ReadUInt16();
                    bis.Seek(18 * roomNumLight, SeekOrigin.Current);

                    //skip static meshes
                    ushort roomNumStaticMeshes = bis.ReadUInt16();
                    bis.Seek(18 * roomNumStaticMeshes, SeekOrigin.Current);

                    //skip alternate room
                    bis.Seek(2, SeekOrigin.Current);

                    //Skip flags
                    bis.Seek(2, SeekOrigin.Current);
                }
                Console.WriteLine($"Number of rooms: {numRoom}");

                //Skip Floor Data
                uint numFloorData = bis.ReadUInt32();
                bis.Seek(2 * numFloorData, SeekOrigin.Current);
                Console.WriteLine($"Floor Data Count: {numFloorData}");

                //Skip Mesh Data
                uint numMeshData = bis.ReadUInt32();
                bis.Seek(2 * numMeshData, SeekOrigin.Current);

                //Skip Mesh Pointers
                uint numMeshPointer = bis.ReadUInt32();
                bis.Seek(4 * numMeshPointer, SeekOrigin.Current);

                //Skip Animations
                uint numAnimation = bis.ReadUInt32();
                bis.Seek(32 * numAnimation, SeekOrigin.Current);

                //Skip State Changes
                uint numStateChange = bis.ReadUInt32();
                bis.Seek(6 * numStateChange, SeekOrigin.Current);

                //Skip anim dispatchers
                uint numAnimDispatcher = bis.ReadUInt32();
                bis.Seek(8 * numAnimDispatcher, SeekOrigin.Current);

                //Skip anim commands
                uint numAnimCommands = bis.ReadUInt32();
                bis.Seek(2 * numAnimCommands, SeekOrigin.Current);

                //Skip mesh trees
                uint numMeshTree = bis.ReadUInt32();
                bis.Seek(4 * numMeshTree, SeekOrigin.Current);

                //Skip frames
                uint numFrames = bis.ReadUInt32();
                bis.Seek(2 * numFrames, SeekOrigin.Current);

                //Skip models
                uint numModels = bis.ReadUInt32();
                bis.Seek(18 * numModels, SeekOrigin.Current);

                //Skip static mesh
                uint numStaticMesh = bis.ReadUInt32();
                bis.Seek(32 * numStaticMesh, SeekOrigin.Current);

                //
                // Finally return to reading data we want.
                //

                //Read Object Textures
                uint numObjectTexture = bis.ReadUInt32();

                objectTextures = new TRObjectTexture[numObjectTexture];
                for (int i = 0; i < numObjectTexture; ++i)
                {
                    objectTextures[i] = new TRObjectTexture
                    {
                        attributes = bis.ReadUInt16(),
                        tpageAndFlag = bis.ReadUInt16(),
                        coordinates = new TRTextureCoordinate[4]
                    };

                    Console.WriteLine($"Object Texture [{1 + i} / {numObjectTexture}]:");
                    Console.WriteLine($"\tAttributes: {objectTextures[i].attributes}");
                    Console.WriteLine($"\tTPageAndFlag: {objectTextures[i].tpageAndFlag}");

                    for (int j = 0; j < 4; ++j)
                    {
                        objectTextures[i].coordinates[j].U = (ushort)((bis.ReadUInt16() & 0xFF00) >> 8);
                        objectTextures[i].coordinates[j].V = (ushort)((bis.ReadUInt16() & 0xFF00) >> 8);
                    }

                    objectTextures[i].X = Math.Min(Math.Min(objectTextures[i].coordinates[0].U, objectTextures[i].coordinates[1].U), Math.Min(objectTextures[i].coordinates[2].U, objectTextures[i].coordinates[3].U));
                    objectTextures[i].Y = Math.Min(Math.Min(objectTextures[i].coordinates[0].V, objectTextures[i].coordinates[1].V), Math.Min(objectTextures[i].coordinates[2].V, objectTextures[i].coordinates[3].V));
                    objectTextures[i].W = Math.Max(Math.Max(objectTextures[i].coordinates[0].U, objectTextures[i].coordinates[1].U), Math.Max(objectTextures[i].coordinates[2].U, objectTextures[i].coordinates[3].U));
                    objectTextures[i].H = Math.Max(Math.Max(objectTextures[i].coordinates[0].V, objectTextures[i].coordinates[1].V), Math.Max(objectTextures[i].coordinates[2].V, objectTextures[i].coordinates[3].V));
                    objectTextures[i].W -= (objectTextures[i].X - 1);
                    objectTextures[i].H -= (objectTextures[i].Y - 1);

                    Console.WriteLine($"\tX: {objectTextures[i].X}");
                    Console.WriteLine($"\tY: {objectTextures[i].Y}");
                    Console.WriteLine($"\tW: {objectTextures[i].W}");
                    Console.WriteLine($"\tH: {objectTextures[i].H}");

                }
                Console.WriteLine();

                //Read Sprite Textures
                uint numSpriteTexture = bis.ReadUInt32();

                spriteTextures = new TRSpriteTexture[numSpriteTexture];

                for (int i = 0; i < numSpriteTexture; ++i)
                {
                    Console.WriteLine($"Sprite Texture [{1 + i} / {numSpriteTexture}]:");

                    spriteTextures[i] = new TRSpriteTexture
                    {
                        tpage = bis.ReadUInt16(),
                        x = bis.ReadByte(),
                        y = bis.ReadByte(),
                        width = bis.ReadUInt16(),
                        height = bis.ReadUInt16(),
                        left = bis.ReadInt16(),
                        top = bis.ReadInt16(),
                        right = bis.ReadInt16(),
                        bottom = bis.ReadInt16()
                    };

                    Console.WriteLine($"\tTexture Page: {spriteTextures[i].tpage}");
                    Console.WriteLine($"\tX Coordinate: {spriteTextures[i].x}");
                    Console.WriteLine($"\tY Coordinate: {spriteTextures[i].y}");
                    Console.WriteLine($"\tWidth: {(spriteTextures[i].width / 256) + 1}");
                    Console.WriteLine($"\tHeight: {(spriteTextures[i].height / 256) + 1}");
                    Console.WriteLine($"\tLeft: {spriteTextures[i].left}");
                    Console.WriteLine($"\tTop: {spriteTextures[i].top}");
                    Console.WriteLine($"\tRight: {spriteTextures[i].right}");
                    Console.WriteLine($"\tBottom: {spriteTextures[i].bottom}");
                }
                Console.WriteLine();

                //
                // Skipping data again...
                //

                //Skip sprite sequences
                uint numSpriteSequence = bis.ReadUInt32();
                bis.Seek(8 * numSpriteSequence, SeekOrigin.Current);

                //Skip cameras
                uint numCamera = bis.ReadUInt32();
                bis.Seek(16 * numCamera, SeekOrigin.Current);

                //Skip Sound Sources
                uint numSoundSource = bis.ReadUInt32();
                bis.Seek(16 * numSoundSource, SeekOrigin.Current);

                //Skip boxes
                uint numBoxes = bis.ReadUInt32();
                bis.Seek(20 * numBoxes, SeekOrigin.Current);

                //Skip overlaps
                uint numOverlaps = bis.ReadUInt32();
                bis.Seek(2 * numOverlaps, SeekOrigin.Current);

                //Skip Zones
                bis.Seek(2 * numBoxes, SeekOrigin.Current); //Ground Zones #1
                bis.Seek(2 * numBoxes, SeekOrigin.Current); //Ground Zones #2
                bis.Seek(2 * numBoxes, SeekOrigin.Current); //Fly Zones
                bis.Seek(2 * numBoxes, SeekOrigin.Current); //Ground Zone Alts #1
                bis.Seek(2 * numBoxes, SeekOrigin.Current); //Ground Zone Alts #2
                bis.Seek(2 * numBoxes, SeekOrigin.Current); //Fly Zone Alts

                //Skip animated textures
                uint numAnimatedTextures = bis.ReadUInt32();
                bis.Seek(2 * numAnimatedTextures, SeekOrigin.Current);

                //Skip entities
                uint numEntities = bis.ReadUInt32();
                bis.Seek(22 * numEntities, SeekOrigin.Current);

                //Skip lightmap
                bis.Seek(8192, SeekOrigin.Current);

                //
                // FINALLY DONE AFTER READING PALETTE, WOOO! :3
                //
                palette = new TRColour24[256];
                for(int i = 0; i < 256; ++i)
                {
                    palette[i] = new TRColour24
                    {
                        r = (byte) (bis.ReadByte() * 4),
                        g = (byte)(bis.ReadByte() * 4),
                        b = (byte)(bis.ReadByte() * 4),
                    };
                }

                return;
            }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        }

        #pragma warning disable CA1416 // Validate platform compatibility
        public void WriteTexturesRaw(string filepath)
        {
            Console.WriteLine($"Export Path: {filepath}");
            Directory.CreateDirectory(filepath);

            int tid = 0;
            foreach(TRTexPage8BPP tpage in texpages)
            {
                using (Bitmap bm = new Bitmap(256, 256, PixelFormat.Format8bppIndexed))
                {
                    //Write Palette
                    ColorPalette pal = bm.Palette;
                    for (int i = 0; i < 256; ++i)
                    {
                        pal.Entries[i] = Color.FromArgb(255, palette[i].r, palette[i].g, palette[i].b);
                    }
                    bm.Palette = pal;

                    //Write Indices
                    BitmapData bmd = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                    Marshal.Copy(tpage.data, 0, bmd.Scan0, 65536);
                    bm.UnlockBits(bmd);

                    bm.Save(Path.Combine(filepath, $"texturepage_{tid}.png"), ImageFormat.Png);
                }

                tid++;
            }
        }

        public void WriteGlidosTexturePack(string filepath)
        {
            //Create TPage Directories
            string[] tpagePaths = new string[texpages.Length];

            byte[] md5Hash;

            for(int i = 0; i < texpages.Length; ++i)
            {
                TRTexPage8BPP tpage = texpages[i];

                md5Hash = MD5.HashData(tpage.data);
                tpagePaths[i] = Path.Combine(filepath, string.Concat(md5Hash.Select(hb => hb.ToString("X2"))));

                if (!Directory.Exists(tpagePaths[i]))
                {
                    Directory.CreateDirectory(tpagePaths[i]);
                }
            }
            
            //Export Object Textures
            foreach(TRObjectTexture objTex in objectTextures)
            {
                int texId = objTex.tpageAndFlag & 0x7FFF;

                string outputDir = tpagePaths[texId];
                string outputName = $"({objTex.X}--{objTex.X + objTex.W})({objTex.Y}--{objTex.Y + objTex.H})";

                //Copy texture into byte array, accounting for stride.
                byte[] texData = new byte[objTex.W * objTex.H];
                int strideX = 256;
                int srcPos = (int)((256 * objTex.Y) + objTex.X);
                int destPos = 0;

                Console.WriteLine($"Copy Object Tex Data [begin: {srcPos}, row width: {objTex.W}]");
                for (int i = 0; i < objTex.H; ++i)
                {
                    Array.Copy(texpages[texId].data, srcPos, texData, destPos, objTex.W);
                    srcPos += strideX;
                    destPos += (int)objTex.W;
                }

                //Write Texture
                using (Bitmap bm = new Bitmap((int)objTex.W, (int)objTex.H, PixelFormat.Format8bppIndexed))
                {
                    //Copy Palette
                    ColorPalette pal = bm.Palette;
                    pal.Entries[0] = Color.FromArgb(0, palette[0].r, palette[0].g, palette[0].b);
                    for (int i = 1; i < 256; ++i)
                    {
                        pal.Entries[i] = Color.FromArgb(255, palette[i].r, palette[i].g, palette[i].b);
                    }
                    bm.Palette = pal;

                    //Write Indices
                    BitmapData bmd = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                    Marshal.Copy(texData, 0, bmd.Scan0, texData.Length);
                    bm.UnlockBits(bmd);

                    bm.Save(Path.Combine(outputDir, outputName) + ".png", ImageFormat.Png);
                }
            }

            //Export Sprite Textures
            foreach(TRSpriteTexture sprTex in spriteTextures)
            {
                int sprWidth = (sprTex.width / 256) + 1;
                int sprHeight = (sprTex.height / 256) + 1;

                string outputDir = tpagePaths[sprTex.tpage];
                string outputName = $"({sprTex.x}--{sprTex.x + sprWidth})({sprTex.y}--{sprTex.y + sprHeight})";

                //Copy texture into byte array, accounting for stride.
                byte[] texData = new byte[sprWidth * sprHeight];
                int strideX = 256;
                int srcPos = (int)((256 * sprTex.y) + sprTex.x);
                int destPos = 0;

                Console.WriteLine($"Copy Sprite Tex Data [begin: {srcPos}, row width: {sprWidth}");
                for (int i = 0; i < sprHeight; ++i)
                {
                    Array.Copy(texpages[sprTex.tpage].data, srcPos, texData, destPos, sprWidth);
                    srcPos += strideX;
                    destPos += sprWidth;
                }

                //Write Texture
                using (Bitmap bm = new Bitmap((int)sprWidth, (int)sprHeight, PixelFormat.Format8bppIndexed))
                {
                    //Copy Palette
                    ColorPalette pal = bm.Palette;
                    pal.Entries[0] = Color.FromArgb(0, palette[0].r, palette[0].g, palette[0].b);

                    for (int i = 1; i < 256; ++i)
                    {
                        pal.Entries[i] = Color.FromArgb(255, palette[i].r, palette[i].g, palette[i].b);
                    }
                    bm.Palette = pal;

                    //Write Indices
                    BitmapData bmd = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                    Marshal.Copy(texData, 0, bmd.Scan0, texData.Length);
                    bm.UnlockBits(bmd);

                    bm.Save(Path.Combine(outputDir, outputName) + ".png", ImageFormat.Png);
                }
            }
        }
        #pragma warning restore CA1416 // Validate platform compatibility
    }
}
