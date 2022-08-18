// See https://aka.ms/new-console-template for more information
using Accord.Imaging;
using Accord.Imaging.Filters;
using System.Drawing;

Console.WriteLine("Hello, World!");

Console.WriteLine(ZBar.ZBar.Version);

foreach (var item in Directory.GetFiles("D:\\zbar-test\\samples\\达安细胞片\\二维码\\no","*.jpg"))
{
    Console.WriteLine($"{item} result: {Scan(item)}");
}

Console.ReadKey();

static string Scan(string fileName)
{
    using var originalBitmap = new Bitmap(fileName);
    using var unmanagedOriginalBitmap = UnmanagedImage.FromManagedImage(originalBitmap);
    using var grayscaleBitmap = Grayscale.CommonAlgorithms.Y.Apply(unmanagedOriginalBitmap);
    var data = grayscaleBitmap.ToByteArray();

    var scanner = new ZBar.ImageScanner();

    scanner.SetConfiguration(ZBar.SymbolType.None, ZBar.Config.Enable, 1);
    scanner.SetConfiguration(ZBar.SymbolType.EAN13, ZBar.Config.Enable, 1);
    scanner.SetConfiguration(ZBar.SymbolType.CODE39, ZBar.Config.Enable, 1);
    scanner.SetConfiguration(ZBar.SymbolType.CODE128, ZBar.Config.Enable, 1);

    //mat.GetArray<byte>(out var data);
    ZBar.Image zimage = new ZBar.Image();
    zimage.Width = (uint)originalBitmap.Width;
    zimage.Height = (uint)originalBitmap.Height;
    zimage.Data = data;
    zimage.Format = ZBar.Image.FourCC('Y', '8', '0', '0');
    var symbols = scanner.Scan(zimage);
    foreach (var symbol in zimage.Symbols)
    {
        return symbol.Data;
    }

    return "";
}