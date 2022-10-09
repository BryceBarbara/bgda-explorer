/*  Copyright (C) 2012 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using JetBlackEngineLib.Data.Models;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JetBlackEngineLib.Data.Textures;

public static class TexDecoder
{
    private const int BITBLTBUF = 0x50;
    private const int TRXPOS = 0x51;
    private const int TRXREG = 0x52;

    private const int PSMCT32 = 0x00;
    private const int PSMT4 = 0x14;

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    private struct TexHeader
    {
        public ushort Width;
        public ushort Height;
        public ushort U1;
        public ushort Length;
        public int U2;
        public int U3;
        public int GifOffset;
    }

    public static WriteableBitmap? Decode(ReadOnlySpan<byte> data)
    {
        var header = DataUtil.CastTo<TexHeader>(data);

        if (header.Width <= 0)
        {
            return null;
        }

        if (header.GifOffset <= 0)
        {
            // PC version of stuff
            return Read8BitPaletteTexture(data, header);
        }

        if (header.GifOffset >= data.Length)
            throw new InvalidDataException("Offset is past the end of the data");
        
        return ReadGifTexture(data, header);
    }

    private static Color ToColorFromAbgr(int argb)
    {
        var a = (byte)((argb & -16777216) >> 0x18);
        var r = (byte)(argb & 0xff);
        var g = (byte)((argb & 0xff00) >> 8);
        var b = (byte)((argb & 0xff0000) >> 0x10);
        return Color.FromArgb(a, r, g, b);
    }

    private static WriteableBitmap Read8BitPaletteTexture(ReadOnlySpan<byte> data, TexHeader header)
    {
        var PaletteLength = 256;
        var paletteColors = new List<Color>(PaletteLength);

        var paletteOffset = header.U2;

        for (var i = 0; i < PaletteLength; i++)
        {
            var argb = DataUtil.GetLeInt(data, paletteOffset + (i * 4));
            paletteColors.Add(ToColorFromAbgr(argb));
        }
        
        var palette = new BitmapPalette(paletteColors);
        
        var image = new WriteableBitmap(
            header.Width, header.Height,
            96, 96,
            PixelFormats.Indexed8,
            palette);
        
        image.Lock();
        var imageDataOffset = paletteOffset + PaletteLength * 4;
        var stride = header.Width;
        for (var y = 0; y < header.Height && y < header.Height; ++y)
        for (var x = 0; x < header.Width && x < header.Width; ++x)
        {
            var pixel = data[imageDataOffset + ((y * stride) + x)];
            if (x < header.Width && y < header.Height)
            {
                var p = image.BackBuffer + (y * image.BackBufferStride) + (x);
                Marshal.WriteByte(p, pixel);
            }
        }
        
        // Specify the area of the bitmap that changed.
        image.AddDirtyRect(new Int32Rect(0, 0, header.Width, header.Height));

        // Release the back buffer and make it available for display.
        image.Unlock();

        return image;
    } 

    private static WriteableBitmap ReadGifTexture(ReadOnlySpan<byte> data, TexHeader header)
    {
        GsMemory gsMem = new();
        
        var sourceW = header.Width;
        var sourceH = header.Height;
        PalEntry[]? pixels = null;
        byte[]? bytes = null;

        var curIdx = header.GifOffset;
        var endIndex = curIdx + header.Length;

        GIFTag gifTag = new();
        gifTag.Parse(data[curIdx..]);

        // This is basically heuristics. Writing a full GIF parser is complex and as the texture files are written by a tool,
        // we can safely make some assumptions about their structure.
        if (gifTag.nloop == 4)
        {
            int palW = DataUtil.GetLeShort(data, curIdx + 0x30);
            int palH = DataUtil.GetLeShort(data, curIdx + 0x34);

            curIdx += gifTag.Length;
            GIFTag gifTag2 = new();
            gifTag2.Parse(data[curIdx..]);

            // 8 bit palletised
            var palette = PalEntry.ReadPalette(data[(curIdx + GIFTag.Size)..], palW, palH);

            palette = PalEntry.UnswizzlePalette(palette);

            curIdx += gifTag2.Length;
            var destWBytes = (ushort)((header.Width + 0x0f) & ~0x0f);
            var destHBytes = (ushort)((header.Height + 0x0f) & ~0x0f);

            var dpsm = PSMCT32;
            var dbw = 0;
            var dbp = 0;
            var rrw = 0;
            var rrh = 0;
            var startX = 0;
            var startY = 0;

            while (curIdx < endIndex - GIFTag.Size)
            {
                GIFTag gifTag3 = new();
                gifTag3.Parse(data[curIdx..]);
                while (!gifTag3.IsImage)
                {
                    var trxRegOffset = FindAdEntry(data, curIdx + GIFTag.Size, gifTag3.nloop, TRXREG);
                    if (trxRegOffset != 0)
                    {
                        rrw = DataUtil.GetLeShort(data, trxRegOffset);
                        rrh = DataUtil.GetLeShort(data, trxRegOffset + 4);
                    }

                    var trxPosOffset = FindAdEntry(data, curIdx + GIFTag.Size, gifTag3.nloop, TRXPOS);
                    if (trxPosOffset != 0)
                    {
                        startX = DataUtil.GetLeShort(data, trxPosOffset + 0x04) & 0x07FF;
                        startY = DataUtil.GetLeShort(data, trxPosOffset + 0x06) & 0x07FF;
                    }

                    var bitbltOffset = FindAdEntry(data, curIdx + GIFTag.Size, gifTag3.nloop, BITBLTBUF);
                    if (bitbltOffset != 0)
                    {
                        //int sbw = fileData[bitbltOffset + 0x02] & 0x3F;
                        dbp = data[bitbltOffset + 0x04] & 0x3FFF;
                        dbw = data[bitbltOffset + 0x06] & 0x3F;
                        dpsm = data[bitbltOffset + 0x07] & 0x3F;
                    }

                    curIdx += gifTag3.Length;
                    if (curIdx + GIFTag.Size >= endIndex)
                    {
                        break;
                    }

                    gifTag3.Parse(data[curIdx..]);
                }

                curIdx += GIFTag.Size; // image gif tag
                var bytesToTransfer = gifTag3.nloop * 16;

                if (palette.Length == 16)
                {
                    // source is PSMT4. Dest can be PSMT4 or PSMCT32
                    if (dpsm == PSMCT32)
                    {
                        var imageData = data.ToArray();
                        var imageDataIdx = curIdx;
                        // check for multiple IMAGE entries.
                        var nextTagInd = bytesToTransfer + curIdx;
                        if (nextTagInd < endIndex - GIFTag.Size)
                        {
                            GIFTag imageTag2 = new();
                            imageTag2.Parse(data[nextTagInd..]);
                            if (imageTag2.flg == 2)
                            {
                                // IMAGE
                                var bytesToTransfer2 = imageTag2.nloop * 16;
                                imageDataIdx = 0;
                                imageData = new byte[bytesToTransfer + bytesToTransfer2];
                                var j = curIdx;
                                for (var i = 0; i < bytesToTransfer; ++i)
                                {
                                    imageData[i] = data[j];
                                }

                                j = nextTagInd + GIFTag.Size;
                                for (var i = bytesToTransfer; i < bytesToTransfer + bytesToTransfer2; ++i)
                                {
                                    imageData[i] = data[j];
                                }

                                bytesToTransfer += imageTag2.Length;
                            }
                        }

                        gsMem.WriteTexPSMCT32(dbp, dbw, startX, startY, rrw, rrh, imageData, imageDataIdx);

                        destWBytes = (ushort)((header.Width + 0x3f) & ~0x3f);
                        bytes = gsMem.ReadTexPSMT4(dbp, destWBytes / 0x40, startX, startY, destWBytes, destHBytes);
                        bytes = Expand4Bit(bytes);
                    }
                    else
                    {
                        // dest and source are the same and so image isn't swizzled
                        bytes = TransferPSMT4(bytes, data, curIdx, startX, startY, rrw, rrh, destWBytes,
                            destHBytes);
                    }
                }
                else
                {
                    // source is PSMT8. Dest is always PSMCT32.
                    gsMem.WriteTexPSMCT32(dbp, dbw, startX, startY, rrw, rrh, data, curIdx);
                }

                curIdx += bytesToTransfer;
            }

            if (palette.Length == 256)
            {
                destWBytes = (ushort)((header.Width + 0x7f) & ~0x7f);
                dbw = destWBytes / 0x40;
                bytes = gsMem.ReadTexPSMT8(dbp, dbw, 0, 0, destWBytes, header.Height);
            }

            // THIS IS A HACK
            if (palette.Length == 1024)
            {
                destWBytes = (ushort)((header.Width + 0x3f) & ~0x3f);
                dbw = destWBytes / 0x40;
                bytes = gsMem.ReadTexPSMT8(dbp, dbw, 0, 0, destWBytes, header.Height);
            }

            if (bytes != null)
            {
                pixels = ApplyPalette(palette, bytes);
            }

            sourceW = destWBytes;
            sourceH = destHBytes;
        }
        else if (gifTag.nloop == 3)
        {
            GIFTag gifTag2 = new();
            gifTag2.Parse(data[0xC0..]);

            if (gifTag2.flg == 2)
                // image mode
            {
                pixels = ReadPixels32(data[0xD0..], header.Width, header.Height);
            }
        }

        var image = new WriteableBitmap(
            header.Width, header.Height,
            96, 96,
            PixelFormats.Bgr32,
            null);
        image.Lock();
        if (pixels != null)
        {
            var pBackBuffer = image.BackBuffer;
            for (var y = 0; y < sourceH && y < header.Height; ++y)
            for (var x = 0; x < sourceW && x < header.Width; ++x)
            {
                var pixel = pixels[(y * sourceW) + x];
                if (x < header.Width && y < header.Height)
                {
                    var p = pBackBuffer + (y * image.BackBufferStride) + (x * 4);
                    Marshal.WriteInt32(p, pixel.Argb());
                }
            }
        }

        // Specify the area of the bitmap that changed.
        image.AddDirtyRect(new Int32Rect(0, 0, header.Width, header.Height));

        // Release the back buffer and make it available for display.
        image.Unlock();

        return image;
    }


    // Take an image where the pixels are packed and expand them to one byte per pixel.
    private static byte[] Expand4Bit(byte[] bytes)
    {
        var outBytes = new byte[bytes.Length * 2];
        var j = 0;
        foreach (int val in bytes)
        {
            outBytes[j++] = (byte)(val & 0x0f);
            outBytes[j++] = (byte)((val >> 4) & 0x0f);
        }

        return outBytes;
    }

    private static PalEntry[] ApplyPalette(PalEntry[] palette, byte[] bytes)
    {
        var pixels = new PalEntry[bytes.Length];
        for (var i = 0; i < bytes.Length; ++i)
        {
            pixels[i] = palette[bytes[i] & 0xFF];
        }

        return pixels;
    }

    private static int FindAdEntry(ReadOnlySpan<byte> fileData, int dataStartIdx, int nLoop, int registerId)
    {
        var result = 0;
        for (var i = 0; i < nLoop; ++i)
        {
            var reg = DataUtil.GetLeInt(fileData, dataStartIdx + (i * 0x10) + 0x08);
            if (reg == registerId)
            {
                result = dataStartIdx + (i * 0x10);
                break;
            }
        }

        return result;
    }

    private static byte[] TransferPSMT4(byte[]? pixels, ReadOnlySpan<byte> fileData, int startOffset, int startX,
        int startY,
        int rrw, int rrh, int destWBytes, int destHBytes)
    {
        pixels ??= new byte[destWBytes * destHBytes];

        var nybble = 2;
        var nybbles = new byte[2];
        var idx = startOffset;
        for (var y = 0; y < rrh && y + startY < destHBytes; ++y)
        for (var x = 0; x < rrw; ++x)
        {
            if (nybble > 1)
            {
                var twoPix = fileData[idx++];
                nybbles[0] = (byte)(twoPix & 0x0f);
                nybbles[1] = (byte)((twoPix >> 4) & 0x0f);
                nybble = 0;
            }

            var destIdx = ((y + startY) * destWBytes) + x + startX;
            pixels[destIdx] = nybbles[nybble];
            ++nybble;
        }

        return pixels;
    }

    private static PalEntry[] ReadPixels32(ReadOnlySpan<byte> fileData, int w, int h)
    {
        var numPixels = w * h;
        var pixels = new PalEntry[numPixels];
        var destIdx = 0;
        var endOffset = numPixels * 4;
        for (var idx = 0; idx < endOffset;)
        {
            PalEntry pe = new()
            {
                R = fileData[idx++], G = fileData[idx++], B = fileData[idx++], A = fileData[idx++]
            };

            pixels[destIdx++] = pe;
        }

        return pixels;
    }
}