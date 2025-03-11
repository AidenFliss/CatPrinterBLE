# Cat Printer BLE

<p align="center">
  <img title="Cat Printer MXW01 in action!" src="/Photo.jpg">
</p>

This is a command line program that provides basic functionality to use one of the most recent models (as of March 2025) of Cat Printers: model `MXW01`. It can load any image, it will resize it to the proper resolution and apply a dithering pattern to smooth the gradients after the color reduction.

It requires the [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) and it supports only Windows for now.

## Why

I recently acquired one of these thermal printers to be able to print small labels quickly. The main problem is that these kind of printers require using a proprietary and shady Chinese phone application that requires creating an account and who knows what else they do under the hood.

I searched for an open source alternative, and even though there are multiple projects that have successfully reverse-engineered similar printers, my specific model, the `MXW01`, seems to be fairly recent, works very different to other models, and it is barely understood. The only project I've found that has been able to implement some functionality to make this printer work (https://github.com/eerimoq/moblin), is iOS specific, and doesn't implement all the features.

So I wanted to try to do my own program as a fun challenge. I've never worked with Bluetooth or BLE before, so it's been a learning experience. The aforementioned project served me as a starting point, but then I was able to do my own discoveries by further reverse-engineering the Chinese app.

## Features

This program does not aim to be an editor where you can easily add images and text that can be printed. This will load an existing image that you can create in any image editing software, resize it to the resolution this printer uses, convert it to monochrome or grayscale and apply some dithering to smooth the gradients.

- Many image formats supported, like `BMP`, `GIF`, `JPEG`, `PNG`, `TGA` and `WebP`.
- Print in two modes: Black and white monochrome (1 bit per pixel) and grayscale (4 bits per pixel).
- It supports several [dithering](https://en.wikipedia.org/wiki/Dither) algorithms: `Bayer2x2`, `Bayer4x4`, `Bayer8x8`, `Bayer16x16` and `FloydSteinberg`.
- Get the status of the printer, which provides information about the battery level, if it has paper, its temperature, and its current state (Standby, Printing, Feeding paper, Ejecting paper, No paper, Overheated, Low battery).

## Usage

### Print an image

```
CatPrinterBLE (-p  | --print) <intensity> <print_mode> <dithering_method> <image_path>
```

#### Example

```
CatPrinterBLE -p 100 1bpp FloydSteinberg "C:\CoolCat.png"
```

#### Parameters
- `intensity`: How dark the printing will be. Values from 0 to 100.
- `print_mode`: The amount of colors that will be used for printing. Possible values:
  - `1bpp`: Monochrome, pure black and white. Faster printing, lower quality.
  - `4bpp`: 16bit grayscale. Slower printing, higher quality.
- `dithering_method`: The dithering pattern that will be used for the color reduction. Possible values:
  - `Bayer2x2`
  - `Bayer4x4`
  - `Bayer8x8`
  - `Bayer16x16`
  - `FloydSteinberg`
- image_path: The path to the image to print.

### Get the status of the printer

```
CatPrinterBLE (-ps | --printerStatus)
```

It provides information about the battery level, if it has paper, its temperature, and its current state.

### Get the battery level

```
CatPrinterBLE (-bl | --batteryLevel)
```

It provides the current battery level. This can also be shown with the `-ps` command.

### Show the device info

```
CatPrinterBLE (-di | --deviceInfo)
```

Prints some device information. Useful for testing purposes.

### Get the print type

```
CatPrinterBLE (-pt | --printType)
```

This returns some information abou the "print type" or maybe "printer type". I still haven't figured out what this means exactly. Types are decompiled phonetically written Chinese. This can also be obtained with the `-ps` command.

### Get the "Query count"

```
CatPrinterBLE (-qc | --queryCount)
```

No idea what this is, but the printer supports this command that returns some FF values. The name query count comes from machine translating some Chinese comments in the decompiled app code. I probably shouldn't expose this...

## Changelog

You can check the changelog [here](https://github.com/MaikelChan/CatPrinterBLE/blob/main/CHANGELOG.md).

## Roadmap

There are some commands / features that I still haven't figured out, so it would be nice to reverse-engineer them so the program is more feature complete. But they're not high priority as the most important stuff is already implemented.

I don't plan on supporting other printers for now, as I don't have any other printer to be able to make tests.