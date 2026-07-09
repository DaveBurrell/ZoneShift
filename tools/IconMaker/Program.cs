using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

var assets = args.Length > 0 ? args[0] : throw new Exception("assets path required");
var sourcePath = Path.Combine(assets, "icon-source.jpg");
var icoPath = Path.Combine(assets, "app.ico");
int[] sizes = [16, 24, 32, 48, 64, 128, 256];

using var src = Image.FromFile(sourcePath);
using var ms = new MemoryStream();
using var bw = new BinaryWriter(ms);

bw.Write((ushort)0);
bw.Write((ushort)1);
bw.Write((ushort)sizes.Length);

var images = new List<byte[]>();
var offset = 6 + 16 * sizes.Length;

foreach (var size in sizes)
{
    using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(bmp))
    {
        g.Clear(Color.Transparent);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(src, new Rectangle(0, 0, size, size));
    }

    using var pngMs = new MemoryStream();
    bmp.Save(pngMs, ImageFormat.Png);
    var png = pngMs.ToArray();
    images.Add(png);

    bw.Write((byte)(size >= 256 ? 0 : size));
    bw.Write((byte)(size >= 256 ? 0 : size));
    bw.Write((byte)0);
    bw.Write((byte)0);
    bw.Write((ushort)1);
    bw.Write((ushort)32);
    bw.Write(png.Length);
    bw.Write(offset);
    offset += png.Length;
}

foreach (var png in images)
    bw.Write(png);

bw.Flush();
File.WriteAllBytes(icoPath, ms.ToArray());
Console.WriteLine($"Wrote {icoPath} ({ms.Length} bytes)");
