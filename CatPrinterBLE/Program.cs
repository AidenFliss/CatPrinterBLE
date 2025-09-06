using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using static CatPrinterBLE.ImageProcessor;

namespace CatPrinterBLE;

internal class Program
{
    static async Task Main(string[] args)
    {
        if (!ShouldNotShowHeader()) ShowHeader();

        if (args.Length < 1)
        {
            ShowUsage();
            return;
        }

        switch (args[0])
        {
            case "-p":
            case "--print":
            {
                if (args.Length < 5)
                {
                    ShowUsage();
                    return;
                }

                if (!byte.TryParse(args[1], out byte intensity))
                {
                    Logger.LogLine($"Error: Invalid intensity ({args[1]}).");
                    return;
                }

                CatPrinter.PrintModes printMode;
                switch (args[2])
                {
                    case "1bpp": printMode = CatPrinter.PrintModes.Monochrome; break;
                    case "4bpp": printMode = CatPrinter.PrintModes.Grayscale; break;
                    default: Logger.LogLine($"Error: Invalid print mode ({args[2]})."); return;
                }

                DitheringMethods ditheringMethod;
                switch (args[3])
                {
                    case "None": ditheringMethod = DitheringMethods.None; break;
                    case "Bayer2x2": ditheringMethod = DitheringMethods.Bayer2x2; break;
                    case "Bayer4x4": ditheringMethod = DitheringMethods.Bayer4x4; break;
                    case "Bayer8x8": ditheringMethod = DitheringMethods.Bayer8x8; break;
                    case "Bayer16x16": ditheringMethod = DitheringMethods.Bayer16x16; break;
                    case "FloydSteinberg": ditheringMethod = DitheringMethods.FloydSteinberg; break;
                    default: Logger.LogLine($"Error: Invalid dithering method ({args[3]})."); return;
                }

                string imagePath = args[4];

                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success)
                    {
                        await ble.Print(imagePath, intensity, printMode, ditheringMethod);
                    }
                }

                break;
            }
            case "-ep":
            case "--ejectPaper":
            {
                if (!ushort.TryParse(args[1], out ushort lineCount))
                {
                    Logger.LogLine($"Error: Invalid line count ({args[1]}).");
                    return;
                }

                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.EjectPaper(lineCount);
                }

                break;
            }
            case "-rp":
            case "--retractPaper":
            {
                if (!ushort.TryParse(args[1], out ushort lineCount))
                {
                    Logger.LogLine($"Error: Invalid line count ({args[1]}).");
                    return;
                }

                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.RetractPaper(lineCount);
                }

                break;
            }
            case "-ps":
            case "--printerStatus":
            {
                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.GetPrinterStatusAsync();
                }

                break;
            }
            case "-bl":
            case "--batteryLevel":
            {
                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.GetBatteryLevelAsync();
                }

                break;
            }
            case "-di":
            case "--deviceInfo":
            {
                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.PrintDeviceInfoAsync();
                }

                break;
            }
            case "-pt":
            case "--printType":
            {
                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.GetPrintType();
                }

                break;
            }
            case "-qc":
            case "--queryCount":
            {
                await using (CatPrinter ble = new CatPrinter())
                {
                    bool success = await ble.ConnectAsync();
                    if (success) await ble.GetQueryCount();
                }

                break;
            }
            default:
            {
                ShowUsage();
                break;
            }
        }
    }

    static void ShowHeader()
    {
        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        string v = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "?.?.?";

        Console.ForegroundColor = ConsoleColor.Yellow;
        Logger.LogLine();
        Logger.LogLine("        #----------------------------------------------------------------#");

        Logger.LogLine("        #                 Cat Printer BLE - Version " + v + "                #");
        Logger.Log("        #           ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Logger.Log("https://github.com/MaikelChan/CatPrinterBLE");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Logger.LogLine("          #");

        Logger.LogLine("        #                                                                #");
        Logger.LogLine("        #                    By MaikelChan / PacoChan                    #");
        Logger.LogLine("        #----------------------------------------------------------------#\n");

        Console.ForegroundColor = ConsoleColor.Gray;
        Logger.LogLine("        This program provides basic functionality to use one of the most");
        Logger.LogLine("        recent models (as of March 2025) of Cat Printers: Model MXW01.\n");

        Logger.LogLine("        It can load any image, and it will resize it to the proper resolution");
        Logger.LogLine("        and apply a dithering patten to smooth the gradients after the color reduction.\n\n");
    }

    static void ShowUsage()
    {
        Console.ForegroundColor = ConsoleColor.Gray;

        Logger.LogLine("Usage:\n");

        Console.ForegroundColor = ConsoleColor.White;
        Logger.LogLine("  CatPrinterBLE (-p  | --print) <intensity> <print_mode> <dithering_method> <image_path>\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Logger.LogLine("    Prints the specified image.\n");
        Logger.LogLine("    Example:");
        Logger.LogLine("      CatPrinterBLE -p 100 1bpp FloydSteinberg \"C:\\CoolCat.png\"\n");
        Logger.LogLine("    Parameters:");
        Logger.LogLine("      - intensity        : How dark the printing will be. Values from 0 to 100.");
        Logger.LogLine("      - print_mode       : The amount of colors that will be used for printing.");
        Logger.LogLine("                           Possible values:");
        Logger.LogLine("                             - 1bpp: Monochrome, pure black and white. Faster printing, lower quality.");
        Logger.LogLine("                             - 4bpp: 16bit grayscale. Slower printing, higher quality.");
        Logger.LogLine("      - dithering_method : The dithering pattern that will be used for the color reduction.");
        Logger.LogLine("                           Possible values:");
        Logger.LogLine("                             - Bayer2x2");
        Logger.LogLine("                             - Bayer4x4");
        Logger.LogLine("                             - Bayer8x8");
        Logger.LogLine("                             - Bayer16x16");
        Logger.LogLine("                             - FloydSteinberg");
        Logger.LogLine("      - image_path       : The path to the image to print.\n");

        Console.ForegroundColor = ConsoleColor.White;
        Logger.LogLine("  CatPrinterBLE (-ep | --ejectPaper) <line_count>\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Logger.LogLine("    Ejects the paper a specific amount of lines.\n");
        Logger.LogLine("    Example:");
        Logger.LogLine("      CatPrinterBLE -ep 20\n");
        Logger.LogLine("    Parameters:");
        Logger.LogLine("      - line_count       : The amount of lines to eject.\n");

        Console.ForegroundColor = ConsoleColor.White;
        Logger.LogLine("  CatPrinterBLE (-rp | --retractPaper) <line_count>\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Logger.LogLine("    Retracts the paper a specific amount of lines.\n");
        Logger.LogLine("    Example:");
        Logger.LogLine("      CatPrinterBLE -rp 20\n");
        Logger.LogLine("    Parameters:");
        Logger.LogLine("      - line_count       : The amount of lines to retract.\n");

        Console.ForegroundColor = ConsoleColor.White;
        Logger.LogLine("  CatPrinterBLE (-ps | --printerStatus)\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Logger.LogLine("    Prints the current status of the printer, for example, if it has paper, current temperature and battery level.\n");

        Console.ForegroundColor = ConsoleColor.White;
        Logger.LogLine("  CatPrinterBLE (-bl | --batteryLevel)\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Logger.LogLine("    Shows the current battery level. This can also be shown with the -ps command.\n");

        Console.ForegroundColor = ConsoleColor.White;
        Logger.LogLine("  CatPrinterBLE (-di | --deviceInfo)\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Logger.LogLine("    Prints some device information. Useful for debugging.\n");

        Console.ForegroundColor = ConsoleColor.White;
        Logger.LogLine("  CatPrinterBLE (-pt | --printType)\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Logger.LogLine("    This returns some information abou the \"print type\" or maybe \"printer type\".");
        Logger.LogLine("    I still haven't figured out what this means exactly. Types are decompiled phonetically written Chinese.");
        Logger.LogLine("    This can also be obtained with the -ps command.\n");

        Console.ForegroundColor = ConsoleColor.White;
        Logger.LogLine("  CatPrinterBLE (-qc | --queryCount)\n");
        Console.ForegroundColor = ConsoleColor.Gray;
        Logger.LogLine("    No idea what is this, but the printer supports this command that returns some FF values.\n");
    }

    static bool ShouldNotShowHeader()
    {
        string path = "showed_header";
        if (File.Exists(path))
        {
            return true;
        }
        else
        {
            using (File.Create(path)) { }
            return false;
        }
    }
}