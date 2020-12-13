using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;
using Scarlet.IO;

namespace Scarlet.IO.ImageFormats
{
    [MagicNumber("SHTXFf", 0x00)]
    public class SHTXFf : ImageFormat
    {
        public string Tag { get; private set; }
        public ushort Width { get; private set; }
        public int Height { get; private set; }
        public byte Unknown0x0A { get; private set; }
        public byte Unknown0x0B { get; private set; }

        public static bool PCAE = false;

        public byte[] PaletteData { get; private set; }
        public byte[] PixelData { get; private set; }

        private ImageBinary imageBinary;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            Tag = Encoding.ASCII.GetString(reader.ReadBytes(6));
            Width = reader.ReadUInt16();
            Height = ((int)reader.BaseStream.Length - 6) / Width / 4;
            reader.ReadUInt16();
            Unknown0x0A = reader.ReadByte();
            Unknown0x0B = reader.ReadByte();

            PaletteData = null;
            PixelData = reader.ReadBytes((int)reader.BaseStream.Length);


            /* Initialize ImageBinary */
            imageBinary = new ImageBinary();
            imageBinary.Width = Width;
            imageBinary.Height = Height;
            imageBinary.InputEndianness = Endian.LittleEndian;

            if (PCAE == false)
            {
                imageBinary.InputPixelFormat = PixelDataFormat.FormatAbgr8888;
                imageBinary.InputPaletteFormat = PixelDataFormat.FormatAbgr8888;
            }
            else
            {
                imageBinary.InputPixelFormat = PixelDataFormat.FormatArgb8888;
                imageBinary.InputPaletteFormat = PixelDataFormat.FormatArgb8888;
            }

            imageBinary.AddInputPalette(PaletteData);
            imageBinary.AddInputPixels(PixelData);
        }

        public override int GetImageCount()
        {
            return 1;
        }

        public override int GetPaletteCount()
        {
            return 0;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            return imageBinary.GetBitmap(imageIndex, paletteIndex);
        }
    }
}
