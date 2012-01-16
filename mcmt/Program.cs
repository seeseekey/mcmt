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
using System.Collections.Generic;
using CSCL;
using Substrate;
using Substrate.Core;

namespace mcmt
{
	class Program
	{
		static void DisplayHelp()
		{
			Console.WriteLine("mcmt v1.05");
			Console.WriteLine("(c) 2012 by the seeseekey (http://seeseekey.net)");
			Console.WriteLine("");
			Console.WriteLine("Nutzung: mcmt -action -parameters");
			Console.WriteLine("  z.B. mcmt -repairBedrockLayer D:\\world");
			Console.WriteLine("");
			Console.WriteLine("  -createFlatWorld <worldPath> <xmin> <xmax> <zMin> <zMax>");
			Console.WriteLine("  -relightWorld <worldPath>");
			Console.WriteLine("  -removeEntity <worldPath> <entityID>");
			Console.WriteLine("  -repairBedrockLayer <worldPath>");
			Console.WriteLine("  -replaceBlocks <worldPath> <blockIDBefore> <blockIDAfter>");
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
		private static void CreateFlatWorld(string dest, int xmin, int xmax, int zmin, int zmax)
		{
			// This will instantly create any necessary directory structure
			BetaWorld world=BetaWorld.Create(dest);
			BetaChunkManager cm=world.GetChunkManager();

			// We can set different world parameters
			world.Level.LevelName="FlatWorld";
			world.Level.Spawn=new SpawnPoint(0, 70, 0);

			// world.Level.SetDefaultPlayer();
			// We'll let MC create the player for us, but you could use the above
			// line to create the SSP player entry in level.dat.

			// We'll create chunks at chunk coordinates xmin,zmin to xmax,zmax
			for(int xi=xmin; xi<xmax; xi++)
			{
				for(int zi=zmin; zi<zmax; zi++)
				{
					// This line will create a default empty chunk, and create a
					// backing region file if necessary (which will immediately be
					// written to disk)
					ChunkRef chunk=cm.CreateChunk(xi, zi);

					// This will suppress generating caves, ores, and all those
					// other goodies.
					chunk.IsTerrainPopulated=true;

					// Auto light recalculation is horrifically bad for creating
					// chunks from scratch, because we're placing thousands
					// of blocks.  Turn it off.
					chunk.Blocks.AutoLight=false;

					// Set the blocks
					FlatChunk(chunk, 64);

					// Reset and rebuild the lighting for the entire chunk at once
					chunk.Blocks.RebuildBlockLight();
					chunk.Blocks.RebuildSkyLight();

					Console.WriteLine("Built Chunk {0},{1}", chunk.X, chunk.Z);

					// Save the chunk to disk so it doesn't hang around in RAM
					cm.Save();
				}
			}

			// Save all remaining data (including a default level.dat)
			// If we didn't save chunks earlier, they would be saved here
			world.Save();
		}

		private static void FlatChunk(ChunkRef chunk, int height)
		{
			// Create bedrock
			for(int y=0; y<2; y++)
			{
				for(int x=0; x<16; x++)
				{
					for(int z=0; z<16; z++)
					{
						chunk.Blocks.SetID(x, y, z, (int)BlockType.BEDROCK);
					}
				}
			}

			// Create stone
			for(int y=2; y<height-5; y++)
			{
				for(int x=0; x<16; x++)
				{
					for(int z=0; z<16; z++)
					{
						chunk.Blocks.SetID(x, y, z, (int)BlockType.STONE);
					}
				}
			}

			// Create dirt
			for(int y=height-5; y<height-1; y++)
			{
				for(int x=0; x<16; x++)
				{
					for(int z=0; z<16; z++)
					{
						chunk.Blocks.SetID(x, y, z, (int)BlockType.DIRT);
					}
				}
			}

			// Create grass
			for(int y=height-1; y<height; y++)
			{
				for(int x=0; x<16; x++)
				{
					for(int z=0; z<16; z++)
					{
						chunk.Blocks.SetID(x, y, z, (int)BlockType.GRASS);
					}
				}
			}
		}

		private static void RelightWorld(string worldPath)
		{
			// Opening an NbtWorld will try to autodetect if a world is Alpha-style or Beta-style
			NbtWorld world=NbtWorld.Open(worldPath);

			// Grab a generic chunk manager reference
			IChunkManager cm=world.GetChunkManager();

			// First blank out all of the lighting in all of the chunks
			foreach(ChunkRef chunk in cm)
			{
				chunk.Blocks.RebuildHeightMap();
				chunk.Blocks.ResetBlockLight();
				chunk.Blocks.ResetSkyLight();

				cm.Save();

				Console.WriteLine("Reset Chunk {0},{1}", chunk.X, chunk.Z);
			}

			// In a separate pass, reconstruct the light
			foreach(ChunkRef chunk in cm)
			{
				chunk.Blocks.RebuildBlockLight();
				chunk.Blocks.RebuildSkyLight();

				// Save the chunk to disk so it doesn't hang around in RAM
				cm.Save();

				Console.WriteLine("Relight Chunk {0},{1}", chunk.X, chunk.Z);
			}
		}

		private static void RemoveEntity(string worldPath, string entityId)
		{
			// Our initial bounding box is "infinite"
			int x1=BlockManager.MIN_X;
			int x2=BlockManager.MAX_X;
			int z1=BlockManager.MIN_Z;
			int z2=BlockManager.MAX_Z;

			// Load world
			BetaWorld world=BetaWorld.Open(worldPath);
			BetaChunkManager cm=world.GetChunkManager();

			// Remove entities
			foreach(ChunkRef chunk in cm)
			{
				// Skip chunks that don't cover our selected area
				if(((chunk.X+1)*chunk.Blocks.XDim<x1)||
					(chunk.X*chunk.Blocks.XDim>=x2)||
					((chunk.Z+1)*chunk.Blocks.ZDim<z1)||
					(chunk.Z*chunk.Blocks.ZDim>=z2))
				{
					continue;
				}

				// Delete the specified entities
				int removeCount=chunk.Entities.RemoveAll(entityId);

				if(removeCount>0)
				{
					Console.WriteLine("{0} entities from type {1} removed in chunk {2}/{3}", removeCount, entityId, chunk.X, chunk.Z);
				}

				cm.Save();
			}
		}

		private static void RepairBedrockLayer(string worldPath)
		{
			// Open our world
			BetaWorld world=BetaWorld.Open(worldPath);

			// The chunk manager is more efficient than the block manager for
			// this purpose, since we'll inspect every block
			BetaChunkManager cm=world.GetChunkManager();

			foreach(ChunkRef chunk in cm)
			{
				Console.WriteLine("Process chunk: {0}/{1}", chunk.X, chunk.Z);

				// You could hardcode your dimensions, but maybe some day they
				// won't always be 16.  Also the CLR is a bit stupid and has
				// trouble optimizing repeated calls to Chunk.Blocks.xx, so we
				// cache them in locals
				int xdim=chunk.Blocks.XDim;
				int ydim=chunk.Blocks.YDim;
				int zdim=chunk.Blocks.ZDim;

				// x, z, y is the most efficient order to scan blocks (not that
				// you should care about internal detail)
				for(int x=0; x<xdim; x++)
				{
					for(int z=0; z<zdim; z++)
					{
						// Replace the block with after if it matches before
						if(chunk.Blocks.GetID(x, 0, z)!=BlockType.BEDROCK)
						{
							chunk.Blocks.SetData(x, 0, z, 0);
							chunk.Blocks.SetID(x, 0, z, BlockType.BEDROCK);

							Console.WriteLine("Repair Bedrock layer on {0}/{1} in chunk {2}/{3}", x, z, chunk.X, chunk.Z);
						}
					}
				}

				// Save the chunk
				cm.Save();
			}
		}

		private static void ReplaceBlocks(string worldPath, int before, int after)
		{
			// Open our world
			BetaWorld world=BetaWorld.Open(worldPath);

			// The chunk manager is more efficient than the block manager for
			// this purpose, since we'll inspect every block
			BetaChunkManager cm=world.GetChunkManager();

			foreach(ChunkRef chunk in cm)
			{
				Console.WriteLine("Process chunk: {0}/{1}", chunk.X, chunk.Z);

				// You could hardcode your dimensions, but maybe some day they
				// won't always be 16.  Also the CLR is a bit stupid and has
				// trouble optimizing repeated calls to Chunk.Blocks.xx, so we
				// cache them in locals
				int xdim=chunk.Blocks.XDim;
				int ydim=chunk.Blocks.YDim;
				int zdim=chunk.Blocks.ZDim;

				// x, z, y is the most efficient order to scan blocks (not that
				// you should care about internal detail)
				for(int x=0; x<xdim; x++)
				{
					for(int z=0; z<zdim; z++)
					{
						for(int y=0; y<ydim; y++)
						{

							// Replace the block with after if it matches before
							if(chunk.Blocks.GetID(x, y, z)==before)
							{
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
			if(parameters.GetBool("createFlatWorld"))
			{
				List<string> files=GetFilesFromParameters(parameters);

				if(files.Count<5) Console.WriteLine("Need more parameters!");
				else
				{
					string worldPath=files[0];
					int xmin=Convert.ToInt32(files[1]);
					int xmax=Convert.ToInt32(files[2]);
					int zMin=Convert.ToInt32(files[3]);
					int zMax=Convert.ToInt32(files[4]);

					CreateFlatWorld(worldPath, xmin, xmax, zMin, zMax);
				}
			}
			else if(parameters.GetBool("removeEntity"))
			{
				List<string> files=GetFilesFromParameters(parameters);

				if(files.Count<2) Console.WriteLine("Need more parameters!");
				else
				{
					string worldPath=files[0];
					string entityId=files[1];

					RemoveEntity(worldPath, entityId);
				}
			}
			else if(parameters.GetBool("relightWorld"))
			{
				List<string> files=GetFilesFromParameters(parameters);

				if(files.Count==0) Console.WriteLine("No filename detected!");
				else
				{
					foreach(string file in files)
					{
						RelightWorld(file);
					}
				}
			}
			else if(parameters.GetBool("repairBedrockLayer"))
			{				
				List<string> files=GetFilesFromParameters(parameters);

				if(files.Count==0) Console.WriteLine("No filename detected!");
				else
				{
					foreach(string file in files)
					{
						RepairBedrockLayer(file);
					}
				}
			}
			else if(parameters.GetBool("replaceBlocks"))
			{
				List<string> files=GetFilesFromParameters(parameters);

				if(files.Count<3) Console.WriteLine("Need more parameters!");
				else
				{
					string worldPath=files[0];
					int before=Convert.ToInt32(files[1]);
					int after=Convert.ToInt32(files[2]);

					ReplaceBlocks(worldPath, before, after);
				}
			}
			else
			{
				DisplayHelp();
			}
		}
	}
}
