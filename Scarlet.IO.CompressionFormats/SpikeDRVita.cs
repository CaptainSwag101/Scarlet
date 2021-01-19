﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.CompressionFormats
{
    /* Decompression code for Spike Chunsoft/Danganronpa PS Vita; adapted from dr_dec.py by @BlackDragonHunt & @FireyFly
     * https://github.com/BlackDragonHunt/Danganronpa-Tools
     * 
     * ################################################################################
     * # Copyright © 2016 BlackDragonHunt
     * # This work is free. You can redistribute it and/or modify it under the
     * # terms of the Do What The Fuck You Want To Public License, Version 2,
     * # as published by Sam Hocevar. See the COPYING file for more details.
     * ################################################################################
     */

    [MagicNumber(new byte[] { 0xFC, 0xAA, 0x55, 0xA7 }, 0x00)]
    [MagicNumber(new byte[] { 0x47, 0x58, 0x33, 0x00 }, 0x00)]  /* GX3 */
    public class SpikeDRVita : CompressionFormat
    {
        public uint MagicNumber { get; private set; }
        public uint UncompressedSize { get; private set; }
        public uint CompressedSize { get; private set; }

        string NewFileName;
        byte[] decompressed;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            string name = Path.GetFileName((reader.BaseStream as FileStream).Name).Substring(0, Path.GetFileName((reader.BaseStream as FileStream).Name).IndexOf(".")),
                ext = Path.GetFileName((reader.BaseStream as FileStream).Name).Substring(Path.GetFileName((reader.BaseStream as FileStream).Name).IndexOf("."), Path.GetFileName((reader.BaseStream as FileStream).Name).Length - Path.GetFileName((reader.BaseStream as FileStream).Name).IndexOf(".")),
                compressiontype = ".dec";

            MagicNumber = reader.ReadUInt32();
            if (MagicNumber == 0x00335847) MagicNumber = reader.ReadUInt32(); /* "double-compressed" GX3; correct FCAA55A7 magic follows afterwards, so just read magic twice */
            UncompressedSize = reader.ReadUInt32();
            CompressedSize = reader.ReadUInt32();

            decompressed = Decompress(reader.ReadBytes((int)CompressedSize - 0x0C));

            //"double-compressed" GX3
            if (decompressed[0] == 0x47 && decompressed[1] == 0x58 && decompressed[2] == 0x33 && decompressed[3] == 0x0)
            {
                UncompressedSize = BitConverter.ToUInt32(new byte[] { decompressed[8], decompressed[9], decompressed[10], decompressed[11] }, 0);
                CompressedSize = BitConverter.ToUInt32(new byte[] { decompressed[12], decompressed[13], decompressed[14], decompressed[15] }, 0);

                Array.Copy(decompressed, 0xF, decompressed, 0, decompressed.Length - 0xF);
                Array.Resize(ref decompressed, decompressed.Length - 0xF);
                decompressed = Decompress(decompressed);
                compressiontype = ".gx3dec";
            }

            /* Hacky file naming thingy; could be better, but eh... */
            if (reader.BaseStream is FileStream)
                if (ext.Contains(".cmp") || ext.Contains(".gx3"))
                    NewFileName = Path.GetFileName((reader.BaseStream as FileStream).Name).Replace(".gx3", ".gx3dec").Replace(".cmp", ".dec");
                else
                    NewFileName = name + compressiontype + ext;
            else
                NewFileName = "bin";
        }

        public override Stream GetDecompressedStream()
        {
            return new MemoryStream(decompressed);
        }

        public override string GetNameOrExtension()
        {
            return NewFileName;
        }

        private byte[] Decompress(byte[] compressed)
        {
            byte[] decompressed = new byte[UncompressedSize];

            int inOffset = 0, outOffset = 0;
            int windowOffset = 0, count = 0, prevOffset = 0;

            while (outOffset < UncompressedSize)
            {
                byte flags = compressed[inOffset++];

                if ((flags & 0x80) == 0x80)
                {
                    /* Copy data from the output.
                     * 1xxyyyyy yyyyyyyy
                     * Count -> x + 4
                     * Offset -> y
                     */
                    count = (((flags >> 5) & 0x3) + 4);
                    windowOffset = (((flags & 0x1F) << 8) + compressed[inOffset++]);
                    prevOffset = windowOffset;

                    for (int i = 0; i < count; i++)
                        decompressed[outOffset + i] = decompressed[(outOffset - windowOffset) + i];

                    outOffset += count;
                }
                else if ((flags & 0x60) == 0x60)
                {
                    /* Continue copying data from the output.
                     * 011xxxxx
                     * Count -> x
                     * Offset -> reused from above
                     */
                    count = (flags & 0x1F);
                    windowOffset = prevOffset;

                    for (int i = 0; i < count; i++)
                        decompressed[outOffset + i] = decompressed[(outOffset - windowOffset) + i];

                    outOffset += count;
                }
                else if ((flags & 0x40) == 0x40)
                {
                    /* Insert multiple copies of the next byte.
                     * 0100xxxx yyyyyyyy
                     * 0101xxxx xxxxxxxx yyyyyyyy
                     * Count -> x + 4
                     * Data -> y
                     */
                    if ((flags & 0x10) == 0x00)
                        count = ((flags & 0x0F) + 4);
                    else
                        count = ((((flags & 0x0F) << 8) + compressed[inOffset++]) + 4);

                    byte data = compressed[inOffset++];
                    for (int i = 0; i < count; i++)
                        decompressed[outOffset++] = data;
                }
                else if ((flags & 0xC0) == 0x00)
                {
                    /* Insert raw bytes from the input.
                     * 000xxxxx
                     * 001xxxxx xxxxxxxx
                     * Count -> x
                     */
                    if ((flags & 0x20) == 0x00)
                        count = (flags & 0x1F);
                    else
                        count = (((flags & 0x1F) << 8) + compressed[inOffset++]);

                    for (int i = 0; i < count; i++)
                        decompressed[outOffset++] = compressed[inOffset++];
                }
                else
                {
                    throw new Exception();
                }
            }

            return decompressed;
        }
    }
}
