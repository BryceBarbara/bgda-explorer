using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JetBlackEngineLib.Data.Textures;

public class FntFile
{
    public FntFile(WriteableBitmap texture)
    {
        Texture = texture;
    }

    public WriteableBitmap Texture { get; }
}

public static class FntDecoder
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    private struct FntHeader
    {
        public ushort Short00;
        public ushort Short02;
        public int Int04;
        public int Offset08;
        public int Int0C;
        public int ImgOffset;
    }

    
    public static FntFile Decode(ReadOnlySpan<byte> data)
    {
        var header = DataUtil.CastTo<FntHeader>(data);

        var texture = TexDecoder.Decode(data.Slice(header.ImgOffset)) ?? throw new InvalidOperationException("Failed to parse texture from FNT file");

        return new FntFile(texture);
    }
}