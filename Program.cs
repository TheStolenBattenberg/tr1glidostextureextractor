using System;

namespace TR1GlidosDump
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 1)
            {
                Console.WriteLine("No file provided, or too many arguments. Usage is: ");
                Console.WriteLine("TR1GlidosDump.exe [phd file]");
                return;
            }

            //Open PHD file
            FileFormatPHD phd = new FileFormatPHD(args[0]);

            //Export Textures
            //phd.WriteTexturesRaw(Path.GetFullPath(args[0]).Replace(Path.GetFileName(args[0]), ""));
            phd.WriteGlidosTexturePack(Path.GetFullPath(args[0]).Replace(Path.GetFileName(args[0]), ""));
        }
    }
}