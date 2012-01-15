//  
//  Main.cs
//  
//  Author:
//       seeseekey <>
// 
//  Copyright (c) 2012 seeseekey
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;

namespace mcmt
{
	class Program
	{
		static void DisplayHelp()
		{
			Console.WriteLine("mcmt v1.00");
			Console.WriteLine("(c) 2012 by the seeseekey (http://seeseekey.net)");
			Console.WriteLine("");
			Console.WriteLine("Nutzung: ivktool -action -parameters");
			Console.WriteLine("  z.B. mcmt -repairBedrockLayer world");
			Console.WriteLine("");
			Console.WriteLine("  -repairBedrockLayer <mapfile>");
			Console.WriteLine("  -replace (not implemented)");
		}
		
				static List<string> GetFilesFromParameters(Parameters param)
		{
			List<string> ret=new List<string>();

			foreach(string i in param.GetNames())
			{
				if(i.StartsWith("file"))
				{
					ret.Add(param.GetString(i));
				}
			}

			return ret;
		}
		
		#region Functions
		private void RepairBedrockLayer(string world)
		{
		if (args.Length != 3) {
                Console.WriteLine("Usage: BlockReplace <world> <before-id> <after-id>");
                return;
            }

            string dest = args[0];
            int before = Convert.ToInt32(args[1]);
            int after = Convert.ToInt32(args[2]);

            // Open our world
            BetaWorld world = BetaWorld.Open(dest);

            // The chunk manager is more efficient than the block manager for
            // this purpose, since we'll inspect every block
            BetaChunkManager cm = world.GetChunkManager();

            foreach (ChunkRef chunk in cm) {
                // You could hardcode your dimensions, but maybe some day they
                // won't always be 16.  Also the CLR is a bit stupid and has
                // trouble optimizing repeated calls to Chunk.Blocks.xx, so we
                // cache them in locals
                int xdim = chunk.Blocks.XDim;
                int ydim = chunk.Blocks.YDim;
                int zdim = chunk.Blocks.ZDim;

                // x, z, y is the most efficient order to scan blocks (not that
                // you should care about internal detail)
                for (int x = 0; x < xdim; x++) {
                    for (int z = 0; z < zdim; z++) {
                        for (int y = 0; y < ydim; y++) {

                            // Replace the block with after if it matches before
                            if (chunk.Blocks.GetID(x, y, z) == before) {
                                chunk.Blocks.SetData(x, y, z, 0);
                                chunk.Blocks.SetID(x, y, z, after);
                            }
                        }
                    }
                }

                // Save the chunk
                cm.Save();
            }
		}
		#endregion
		
		public static void Main(string[] args)
		{		
			//Parameter auswerten
			Parameters parameters=null;

			try
			{
				parameters=Parameters.InterpretCommandLine(args);
			}
			catch
			{
				Console.WriteLine("Parameters could't regonized!");
				Console.WriteLine("");
				DisplayHelp();
				return;
			}
			
			//Aktion starten
			if(parameters.GetBool("repairBedrockLayer"))
			{				
				List<string> files=GetFilesFromParameters(parameters);

				if(files.Count==0) Console.WriteLine("No filename detected!");
				else
				{
					RepairBedrockLayer(files[0]);
				}
			}
			else if(parameters.GetBool("replace"))
			{
				throw new NotImplementedException();
			}
			else
			{
				DisplayHelp();
			}
		}
	}
}
