//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using Ionic.Zip;
using Ionic.Zlib;
using System;
using System.IO;
using System.Text;

namespace MaterialUI
{
	public class ZipUtil
	{
		public static byte[] CompressBuffer(byte[] bytes)
		{
			return ZlibStream.CompressBuffer(bytes);
		}

		public static byte[] UncompressBuffer(byte[] bytes)
		{
			return ZlibStream.UncompressBuffer(bytes);
		}

		public static void Compress(string location, string zipFilePath)
		{
			using (ZipFile zip = new ZipFile())
			{
				zip.AddDirectory(location, Path.GetFileName(Path.GetDirectoryName(location)));
				zip.Save(zipFilePath);
			}
		}

		public static void Compress(string location, byte[] zipByteStream)
		{
			using (var memoryStream = new MemoryStream(zipByteStream))
			{
				using (ZipFile zip = new ZipFile())
				{
					zip.AddDirectory(location, Path.GetFileName(Path.GetDirectoryName(location)));
					zip.Save(memoryStream);
				}
			}
		}

		public static void Uncompress(string zipFilePath, string location)
		{
			if(!Directory.Exists(location))
				Directory.CreateDirectory(location);

			using (ZipFile zip = ZipFile.Read(zipFilePath))
			{
				zip.ExtractAll(location, ExtractExistingFileAction.OverwriteSilently);
			}
		}

		public static void Uncompress(byte[] zipByteStream, string location)
		{
			if (!Directory.Exists(location))
				Directory.CreateDirectory(location);

			using (var memoryStream = new MemoryStream(zipByteStream))
			{
				using (ZipFile zip = ZipFile.Read(memoryStream))
				{
					zip.ExtractAll(location, ExtractExistingFileAction.OverwriteSilently);
				}
			}
		}
	}
}