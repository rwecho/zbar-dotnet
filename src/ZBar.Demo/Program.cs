// See https://aka.ms/new-console-template for more information
using System.Drawing;

Console.WriteLine("Hello, World!");

Console.WriteLine(ZBar.ZBar.Version);
var bitmap = (Bitmap)Image.FromFile("barcode.bmp");

//var zbarImage = new ZBar.Image(bitmap);

//foreach (var item in zbarImage.Symbols)
//{
//    Console.WriteLine(item.Data);
//}
var scanner = new ZBar.ImageScanner();

var symbols = scanner.Scan(bitmap);
foreach (var symbol in symbols)
{
    Console.WriteLine(symbol.Data);
}

Console.ReadKey();