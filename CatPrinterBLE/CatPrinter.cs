using InTheHand.Bluetooth;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static CatPrinterBLE.ImageProcessor;

namespace CatPrinterBLE;

class CatPrinter : IAsyncDisposable
{
    static readonly BluetoothUuid mainServiceId = BluetoothUuid.FromShortId(0xAE30);
    static readonly BluetoothUuid printCharacteristicId = BluetoothUuid.FromShortId(0xAE01);
    static readonly BluetoothUuid notifyCharacteristicId = BluetoothUuid.FromShortId(0xAE02);
    static readonly BluetoothUuid dataCharacteristicId = BluetoothUuid.FromShortId(0xAE03);

    BluetoothDevice? printerDevice = null;

    GattCharacteristic? printCharacteristic = null;
    GattCharacteristic? notifyCharacteristic = null;
    GattCharacteristic? dataCharacteristic = null;

    enum CommandIds : byte
    {
        GetStatus = 0xA1,
        PrintIntensity = 0xA2,
        EjectPaper = 0xA3,
        RetractPaper = 0xA4,
        QueryCount = 0xA7,
        Print = 0xA9,
        PrintComplete = 0xAA,
        BatteryLevel = 0xAB,
        CancelPrint = 0xAC,
        PrintDataFlush = 0xAD,
        UnknownAE = 0xAE,
        GetPrintType = 0xB0,
        GetVersion = 0xB1,
        UnknownB2 = 0xB2,  // Something about "learn"? print_cmd("2221B2000100000000", "V5X") Also related to A3?
        UnknownB3 = 0xB3   // Something about sign and encryption?
    }

    public enum PrintModes : byte
    {
        Monochrome = 0x0,
        Unknown01 = 0x1, // Similar to monochrome but doesn't eject as much paper after finishing printing?
        Grayscale = 0x2
    }

    const int LINE_PIXELS_COUNT = 384;

    public CatPrinter()
    {

    }

    public async ValueTask DisposeAsync()
    {
        if (printCharacteristic != null) printCharacteristic = null;

        if (notifyCharacteristic != null)
        {
            await notifyCharacteristic.StopNotificationsAsync();
            notifyCharacteristic.CharacteristicValueChanged -= NotifyCharacteristic_CharacteristicValueChanged;
            notifyCharacteristic = null;
        }

        if (dataCharacteristic != null) dataCharacteristic = null;

        if (printerDevice != null)
        {
            if (printerDevice.Gatt.IsConnected)
            {
                printerDevice.Gatt.Disconnect();
            }

            printerDevice = null;
        }
    }

    public async Task<bool> ConnectAsync()
    {
        if (printerDevice != null)
        {
            return true;
        }

        Console.WriteLine("Trying to connect to MXW01...");

        //RequestDeviceOptions options = new RequestDeviceOptions();
        //options.Filters.Add(new BluetoothLEScanFilter() { Name = "MXW01" });
        //BluetoothDevice? device = await Bluetooth.RequestDeviceAsync(options);

        BluetoothDevice? device = null;

        void Bluetooth_AdvertisementReceived(object? sender, BluetoothAdvertisingEvent e)
        {
            device = e.Device;
        }

        Bluetooth.AdvertisementReceived += Bluetooth_AdvertisementReceived;

        BluetoothLEScanOptions options = new BluetoothLEScanOptions();
        options.Filters.Add(new BluetoothLEScanFilter() { Name = "MXW01" });
        BluetoothLEScan scan = await Bluetooth.RequestLEScanAsync(options);

        const int TIMEOUT_MS = 10000;
        const int CHECK_INTERVAL_MS = 200;

        for (int i = 0; i < TIMEOUT_MS / CHECK_INTERVAL_MS; i++)
        {
            await Task.Delay(CHECK_INTERVAL_MS);

            if (device != null) break;
        }

        scan.Stop();
        Bluetooth.AdvertisementReceived -= Bluetooth_AdvertisementReceived;

        if (device == null)
        {
            Console.WriteLine("No MXW01 device found.");
            return false;
        }

        printerDevice = device;

        Console.WriteLine($"Found device - Name: {printerDevice.Name}, ID: {printerDevice.Id}.");
        Console.WriteLine();

        await RequestGattConnectionAsync();

        return true;
    }

    /// <summary>
    /// Prints some information about the services and characteristics of the device.
    /// </summary>
    public async Task PrintDeviceInfoAsync()
    {
        if (!await RequestGattConnectionAsync()) return;

        List<GattService> services = await printerDevice!.Gatt.GetPrimaryServicesAsync();

        Console.WriteLine("#### Print Info ------------------------------------------------------------------------------------");
        Console.WriteLine();

        Console.WriteLine($"Services found: {services.Count}.");
        Console.WriteLine();

        foreach (var service in services)
        {
            string name = GattServiceUuids.GetServiceName(service.Uuid);
            Console.WriteLine($"  * UUID: {service.Uuid.Value}, Name: {name}");

            IReadOnlyList<GattCharacteristic> characteristics = await service.GetCharacteristicsAsync();
            Console.WriteLine($"      Characteristics found: {characteristics.Count}.");

            foreach (GattCharacteristic characteristic in characteristics)
            {
                GattCharacteristicProperties properties = characteristic.Properties;
                if ((properties & GattCharacteristicProperties.Read) != 0)
                {
                    byte[] value = await characteristic.ReadValueAsync();
                    Console.WriteLine($"        - UUID: {characteristic.Uuid.Value}, Properties: {properties}, Value: {ByteArrayToString(value)}");
                }
                else
                {
                    Console.WriteLine($"        - UUID: {characteristic.Uuid.Value}, Properties: {properties}");
                }
            }

            Console.WriteLine();
        }

        Console.WriteLine("#### --------------------------------------------------------------------------------------------------");
        Console.WriteLine();
    }

    public async Task GetPrinterStatusAsync()
    {
        if (!await FindRequiredCharacteristicsAsync()) return;

        await SendCommand(CommandIds.GetVersion, new byte[] { 0x0 }, true);
        await SendCommand(CommandIds.GetStatus, new byte[] { 0x0 }, true);
    }

    public async Task GetBatteryLevelAsync()
    {
        if (!await FindRequiredCharacteristicsAsync()) return;

        await SendCommand(CommandIds.BatteryLevel, new byte[] { 0x0 }, true);
    }

    public async Task GetQueryCount()
    {
        if (!await FindRequiredCharacteristicsAsync()) return;

        await SendCommand(CommandIds.QueryCount, new byte[] { 0x0 }, true);
    }

    public async Task GetPrintType()
    {
        if (!await FindRequiredCharacteristicsAsync()) return;

        await SendCommand(CommandIds.GetPrintType, new byte[] { 0x0 }, true);
    }

    public async Task EjectPaper(ushort lineCount)
    {
        if (!await FindRequiredCharacteristicsAsync()) return;

        byte[] commandData = new byte[2];
        commandData[0] = (byte)((lineCount >> 0) & 0xFF);
        commandData[1] = (byte)((lineCount >> 8) & 0xFF);

        await SendCommand(CommandIds.EjectPaper, commandData, false);
    }

    public async Task RetractPaper(ushort lineCount)
    {
        if (!await FindRequiredCharacteristicsAsync()) return;

        byte[] commandData = new byte[2];
        commandData[0] = (byte)((lineCount >> 0) & 0xFF);
        commandData[1] = (byte)((lineCount >> 8) & 0xFF);

        await SendCommand(CommandIds.RetractPaper, commandData, false);
    }

    public async Task Print(string imagePath, byte intensity, PrintModes printMode = PrintModes.Monochrome, DitheringMethods ditheringMethod = DitheringMethods.None)
    {
        if (!await FindRequiredCharacteristicsAsync()) return;

        if (intensity > 100) intensity = 100;

        int bytesPerLine;
        ColorModes colorMode;
        if (printMode == PrintModes.Grayscale)
        {
            bytesPerLine = LINE_PIXELS_COUNT >> 1;
            colorMode = ColorModes.Mode_4bpp;
        }
        else
        {
            bytesPerLine = LINE_PIXELS_COUNT >> 3;
            colorMode = ColorModes.Mode_1bpp;
        }

        await SendCommand(CommandIds.PrintIntensity, new byte[] { intensity }, false);

        byte[]? pixels = LoadAndProcess(imagePath, LINE_PIXELS_COUNT, colorMode, ditheringMethod);
        if (pixels == null) return;

        int lineCount = pixels.Length / bytesPerLine;

        byte[] printCommandData = new byte[4];
        printCommandData[0] = (byte)((lineCount >> 0) & 0xFF); // Little endian
        printCommandData[1] = (byte)((lineCount >> 8) & 0xFF);
        printCommandData[2] = 0x30;
        printCommandData[3] = (byte)printMode;

        await SendCommand(CommandIds.Print, printCommandData, true);

        byte[] line = new byte[bytesPerLine];

        for (int l = 0; l < lineCount; l++)
        {
            Array.Copy(pixels, l * bytesPerLine, line, 0, bytesPerLine);
            await dataCharacteristic!.WriteValueWithoutResponseAsync(line);
        }

        await SendCommand(CommandIds.PrintDataFlush, new byte[] { 0x0 }, false);

        await WaitForResponseAsync(CommandIds.PrintComplete);
    }

    async Task<bool> RequestGattConnectionAsync()
    {
        if (printerDevice == null)
        {
            Console.WriteLine("Can't connect to GATT server due to the device not being found.");
            return false;
        }

        if (printerDevice.Gatt.IsConnected)
        {
            return true;
        }

        Console.Write("Connecting to GATT server... ");
        await printerDevice.Gatt.ConnectAsync();

        if (!printerDevice.Gatt.IsConnected)
        {
            Console.WriteLine("Fail.");
            return false;
        }

        Console.WriteLine("Success.");
        Console.WriteLine();

        return true;
    }

    async Task<bool> FindRequiredCharacteristicsAsync()
    {
        if (printCharacteristic != null && notifyCharacteristic != null && dataCharacteristic != null)
        {
            return true;
        }

        if (!await RequestGattConnectionAsync()) return false;

        GattService? service = await printerDevice!.Gatt.GetPrimaryServiceAsync(mainServiceId);
        if (service == null)
        {
            Console.WriteLine($"Required service with ID {mainServiceId} is not found.");
            return false;
        }

        GattCharacteristic? printChr = await service.GetCharacteristicAsync(printCharacteristicId);
        if (printChr == null)
        {
            Console.WriteLine($"Required characteristic with ID {printCharacteristicId} is not found.");
            return false;
        }

        GattCharacteristic? notifyChr = await service.GetCharacteristicAsync(notifyCharacteristicId);
        if (notifyChr == null)
        {
            Console.WriteLine($"Required characteristic with ID {notifyCharacteristicId} is not found.");
            return false;
        }

        GattCharacteristic? dataChr = await service.GetCharacteristicAsync(dataCharacteristicId);
        if (dataChr == null)
        {
            Console.WriteLine($"Required characteristic with ID {dataCharacteristicId} is not found.");
            return false;
        }

        printCharacteristic = printChr;
        notifyCharacteristic = notifyChr;
        dataCharacteristic = dataChr;

        notifyCharacteristic.CharacteristicValueChanged += NotifyCharacteristic_CharacteristicValueChanged;
        await notifyCharacteristic.StartNotificationsAsync();

        Console.WriteLine("Found required characteristics and subscribed to notifications.");
        Console.WriteLine();

        return true;
    }

    void NotifyCharacteristic_CharacteristicValueChanged(object? sender, GattCharacteristicValueChangedEventArgs e)
    {
        if (e.Value == null) return;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Received notification: {ByteArrayToString(e.Value)}");
        Console.ForegroundColor = ConsoleColor.Gray;

        if (e.Value[0] != 0x22 || e.Value[1] != 0x21)
        {
            Console.WriteLine("Notification with wrong signature.");
            return;
        }

        ushort dataLength = (ushort)(e.Value[4] | (e.Value[5] << 8));

        CommandIds commandId = (CommandIds)e.Value[2];

        switch (commandId)
        {
            case CommandIds.GetStatus:
            {
                byte batteryLevel = e.Value[9];
                byte temperature = e.Value[10];

                bool statusOk = e.Value[12] == 0;
                string statusDetails = "";
                if (statusOk)
                {
                    switch (e.Value[6])
                    {
                        case 0x0: statusDetails = "Standby"; break;
                        case 0x1: statusDetails = "Printing"; break;
                        case 0x2: statusDetails = "Feeding paper"; break;
                        case 0x3: statusDetails = "Ejecting paper"; break;
                    }
                }
                else
                {
                    switch (e.Value[13])
                    {
                        case 0x1:
                        case 0x9: statusDetails = "No paper"; break;
                        case 0x4: statusDetails = "Overheated"; break;
                        case 0x8: statusDetails = "Low battery"; break;
                    }
                }

                Console.WriteLine($"Status: {(statusOk ? "Ok" : "Error")} ({statusDetails}), Battery: {batteryLevel}, Temperature: {temperature}");
                break;
            }
            case CommandIds.EjectPaper:
            {
                Console.WriteLine("Ejecting paper...");
                break;
            }
            case CommandIds.RetractPaper:
            {
                Console.WriteLine("Retracting paper...");
                break;
            }
            case CommandIds.QueryCount:
            {
                Console.WriteLine($"Query count: {ByteArrayToString(e.Value, 6, 6)}");
                break;
            }
            case CommandIds.Print:
            {
                bool printStatusOk = e.Value[6] == 0;
                Console.WriteLine($"Print status: {(printStatusOk ? "Ok" : "Failure")}");
                break;
            }
            case CommandIds.PrintComplete:
            {
                Console.WriteLine("Printing finished.");
                break;
            }
            case CommandIds.BatteryLevel:
            {
                Console.WriteLine($"Battery level: {e.Value[6]}");
                break;
            }
            case CommandIds.GetPrintType:
            {
                string type;
                switch (e.Value[6])
                {
                    case 0x01: type = "\"gaoya\" (High pressure / voltage / density?)"; break;
                    case 0xFF: type = "\"weishibie\" (???)"; break;
                    default: type = "\"diya\" (Low pressure / voltage / density?)"; break;
                }
                Console.WriteLine($"Print type: {type}");
                break;
            }
            case CommandIds.GetVersion:
            {
                string version = Encoding.UTF8.GetString(e.Value, 6, dataLength);
                string type;
                switch (e.Value[14])
                {
                    case 0x32: type = "\"gaoya\" (High pressure / voltage / density?)"; break;
                    case 0x31: type = "\"diya\" (Low pressure / voltage / density?)"; break;
                    default: type = "\"weishibie\" (???)"; break;
                }
                Console.WriteLine($"Version: {version}, Print type: {type}");
                break;
            }
            default:
                Console.WriteLine($"Unexpected command with ID {commandId}.");
                break;
        }

        Console.WriteLine();

        ResponseReceived(commandId);
    }

    static byte[] CreateCommand(CommandIds commandId, byte[] commandData)
    {
        byte[] command = new byte[8 + commandData.Length];

        command[0] = 0x22;
        command[1] = 0x21;
        command[2] = (byte)commandId;
        command[3] = 0x00;
        command[4] = (byte)((commandData.Length >> 0) & 0xFF); // Little endian
        command[5] = (byte)((commandData.Length >> 8) & 0xFF);
        Array.Copy(commandData, 0, command, 6, commandData.Length);
        command[6 + commandData.Length] = Crc8.Calculate(commandData);
        command[7 + commandData.Length] = 0xFF;

        return command;
    }

    async Task SendCommand(CommandIds commandId, byte[] commandData, bool waitForResponse)
    {
        byte[] command = CreateCommand(commandId, commandData);
        Console.Write($"Sending {commandId} command... ");
        await printCharacteristic!.WriteValueWithoutResponseAsync(command);
        Console.WriteLine("Finished\n");

        if (waitForResponse) await WaitForResponseAsync(commandId);
    }

    #region Task Completion Sources

    readonly Dictionary<CommandIds, TaskCompletionSource?> commandsTcs = new Dictionary<CommandIds, TaskCompletionSource?>();

    async Task WaitForResponseAsync(CommandIds commandId)
    {
        if (commandsTcs.TryGetValue(commandId, out TaskCompletionSource? currentTcs))
        {
            if (currentTcs != null)
            {
                Console.WriteLine($"There's a pending command with ID {commandId}");
                return;
            }
        }

        TaskCompletionSource tcs = new TaskCompletionSource();
        commandsTcs[commandId] = tcs;
        await tcs.Task;
    }

    void ResponseReceived(CommandIds commandId)
    {
        if (commandsTcs.TryGetValue(commandId, out TaskCompletionSource? currentTcs))
        {
            if (currentTcs != null)
            {
                currentTcs.TrySetResult();
                commandsTcs[commandId] = null;
            }
        }
    }

    #endregion

    #region Utils

    static string ByteArrayToString(byte[] bytes, int start = 0, int count = -1)
    {
        StringBuilder sb = new StringBuilder(bytes.Length * 3);

        if (count < 0) count = bytes.Length;

        for (int b = start; b < start + count; b++)
        {
            sb.Append($"{bytes[b]:X2} ");
        }

        return sb.ToString();
    }

    #endregion
}