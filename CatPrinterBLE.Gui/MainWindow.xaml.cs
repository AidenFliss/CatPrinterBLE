using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Dithering;
using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static CatPrinterBLE.ImageProcessor;

namespace CatPrinterBLE.Gui
{
    public partial class MainWindow : Window
    {
        private System.Windows.Point _scrollStart;
        private bool _isPanning = false;

        private string imagePath = string.Empty;
        private int intensity = 50;
        private CatPrinter.PrintModes printMode = CatPrinter.PrintModes.Monochrome;
        private DitheringMethods ditheringMethod = DitheringMethods.FloydSteinberg;

        public MainWindow()
        {
            InitializeComponent();

            PrintButton.Click += PrintButton_Click;
            SelectImageButton.Click += SelectImageButton_Click;
            ThemeToggle.Checked += (s, e) => ApplyTheme(true);
            ThemeToggle.Unchecked += (s, e) => ApplyTheme(false);

            IntensitySlider.ValueChanged += IntensitySlider_ValueChanged;

            PrintModeCombo.SelectionChanged += (s, e) =>
            {
                printMode = PrintModeCombo.SelectedIndex switch
                {
                    0 => CatPrinter.PrintModes.Monochrome,
                    1 => CatPrinter.PrintModes.Grayscale,
                    _ => CatPrinter.PrintModes.Monochrome,
                };
                UpdatePreview();
            };

            DitheringCombo.SelectionChanged += (s, e) =>
            {
                ditheringMethod = DitheringCombo.SelectedIndex switch
                {
                    0 => DitheringMethods.Bayer2x2,
                    1 => DitheringMethods.Bayer4x4,
                    2 => DitheringMethods.Bayer8x8,
                    3 => DitheringMethods.Bayer16x16,
                    4 => DitheringMethods.FloydSteinberg,
                    _ => DitheringMethods.FloydSteinberg,
                };
                UpdatePreview();
            };

            AccuratePreview.Checked += (s, e) => UpdatePreview();
            AccuratePreview.Unchecked += (s, e) => UpdatePreview();

            PreviewScroll.PreviewMouseWheel += PreviewScroll_PreviewMouseWheel;

            PreviewScroll.PreviewMouseDown += (s, e) =>
            {
                if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    _scrollStart = e.GetPosition(PreviewScroll);
                    _isPanning = true;
                    PreviewScroll.Cursor = Cursors.SizeAll;
                    PreviewScroll.CaptureMouse();
                    e.Handled = true;
                }
            };

            PreviewScroll.PreviewMouseUp += (s, e) =>
            {
                if (_isPanning && e.MiddleButton == MouseButtonState.Released)
                {
                    _isPanning = false;
                    PreviewScroll.Cursor = Cursors.Arrow;
                    PreviewScroll.ReleaseMouseCapture();
                    e.Handled = true;
                }
            };

            PreviewScroll.PreviewMouseMove += (s, e) =>
            {
                if (_isPanning)
                {
                    System.Windows.Point pos = e.GetPosition(PreviewScroll);
                    double dx = pos.X - _scrollStart.X;
                    double dy = pos.Y - _scrollStart.Y;

                    PreviewScroll.ScrollToHorizontalOffset(PreviewScroll.HorizontalOffset - dx);
                    PreviewScroll.ScrollToVerticalOffset(PreviewScroll.VerticalOffset - dy);

                    _scrollStart = pos;
                    e.Handled = true;
                }
            };

            PrintModeCombo.SelectionChanged += (s, e) => UpdatePreview();
            DitheringCombo.SelectionChanged += (s, e) => UpdatePreview();
            IntensitySlider.ValueChanged += (s, e) => { intensity = (int)e.NewValue; UpdatePreview(); };

            AccuratePreview.Checked += (s, e) => UpdatePreview();
            AccuratePreview.Unchecked += (s, e) => UpdatePreview();

            FlipXCheckBox.Checked += (s, e) => UpdatePreview();
            FlipXCheckBox.Unchecked += (s, e) => UpdatePreview();

            FlipYCheckBox.Checked += (s, e) => UpdatePreview();
            FlipYCheckBox.Unchecked += (s, e) => UpdatePreview();

            RotateComboBox.SelectionChanged += (s, e) => UpdatePreview();
            CropTextBox.TextChanged += (s, e) => UpdatePreview();
            BackgroundColorCombo.SelectionChanged += (s, e) => UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;

            try
            {
                int printWidth = 384;
                bool accurate = AccuratePreview.IsChecked ?? false;
                BitmapSource previewBitmap;

                if (accurate)
                {
                    ColorModes colorMode = (PrintModeCombo.SelectedIndex == 0) ? ColorModes.Mode_1bpp : ColorModes.Mode_4bpp;
                    DitheringMethods ditherMethod = ditheringMethod;

                    using Image<Rgba32> src = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePath);

                    ApplyTransformations(src);

                    Rgba32 bgColor = BackgroundColorCombo.SelectedIndex switch
                    {
                        0 => new Rgba32(255, 255, 255, 255),
                        1 => new Rgba32(0, 0, 0, 255),
                        2 => new Rgba32(128, 128, 128, 255),
                        3 => new Rgba32(255, 0, 0, 255),
                        4 => new Rgba32(0, 255, 0, 255),
                        5 => new Rgba32(0, 0, 255, 255),
                        _ => new Rgba32(255, 255, 255, 255)
                    };

                    src.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < accessor.Height; y++)
                        {
                            Span<Rgba32> row = accessor.GetRowSpan(y);
                            for (int x = 0; x < row.Length; x++)
                            {
                                if (row[x].A == 0)
                                    row[x] = bgColor;
                            }
                        }
                    });

                    float aspect = (float)src.Width / src.Height;
                    int height = Math.Max(1, (int)(printWidth / aspect));
                    src.Mutate(x => x.Resize(printWidth, height, KnownResamplers.Lanczos3));

                    using Image<L8> gray = src.CloneAs<L8>();

                    float factor = (float)IntensitySlider.Value / 100f;
                    gray.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < accessor.Height; y++)
                        {
                            Span<L8> row = accessor.GetRowSpan(y);
                            for (int x = 0; x < row.Length; x++)
                            {
                                float v = row[x].PackedValue / 255f;
                                v *= factor;
                                v = MathF.Pow(Math.Clamp(v, 0f, 1f), 2.2f);
                                row[x].PackedValue = (byte)MathF.Round(Math.Clamp(v * 255f, 0f, 255f));
                            }
                        }
                    });

                    IDither dither = ditherMethod switch
                    {
                        DitheringMethods.Bayer2x2 => KnownDitherings.Bayer2x2,
                        DitheringMethods.Bayer4x4 => KnownDitherings.Bayer4x4,
                        DitheringMethods.Bayer8x8 => KnownDitherings.Bayer8x8,
                        DitheringMethods.Bayer16x16 => KnownDitherings.Bayer16x16,
                        _ => KnownDitherings.FloydSteinberg
                    };

                    if (colorMode == ColorModes.Mode_1bpp)
                    {
                        gray.Mutate(g => g.BinaryDither(dither));
                    }
                    else
                    {
                        Color[] palette = new Color[16];
                        for (int i = 0; i < palette.Length; i++)
                        {
                            float c = (float)i / (palette.Length - 1);
                            palette[i] = new Color(new Vector4(c, c, c, 1f));
                        }
                        float scale = (ditherMethod == DitheringMethods.FloydSteinberg) ? 1f : 0.2f;
                        gray.Mutate(g => g.Dither(dither, scale, palette));
                    }

                    using Image<Rgba32> final = gray.CloneAs<Rgba32>();
                    using var ms = new MemoryStream();
                    final.SaveAsPng(ms, new PngEncoder());
                    ms.Seek(0, SeekOrigin.Begin);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                    previewBitmap = bmp;
                }
                else
                {
                    using Image<Rgba32> img = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePath);

                    ApplyTransformations(img);

                    Rgba32 bgColor = BackgroundColorCombo.SelectedIndex switch
                    {
                        0 => new Rgba32(255, 255, 255, 255),
                        1 => new Rgba32(0, 0, 0, 255),
                        2 => new Rgba32(128, 128, 128, 255),
                        3 => new Rgba32(255, 0, 0, 255),
                        4 => new Rgba32(0, 255, 0, 255),
                        5 => new Rgba32(0, 0, 255, 255),
                        _ => new Rgba32(255, 255, 255, 255)
                    };

                    img.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < accessor.Height; y++)
                        {
                            Span<Rgba32> row = accessor.GetRowSpan(y);
                            for (int x = 0; x < row.Length; x++)
                            {
                                if (row[x].A == 0)
                                    row[x] = bgColor;
                            }
                        }
                    });

                    float aspect = (float)img.Width / img.Height;
                    int height = Math.Max(1, (int)(printWidth / aspect));
                    img.Mutate(x => x.Resize(printWidth, height, KnownResamplers.Lanczos3));

                    float factor = (float)IntensitySlider.Value / 100f;
                    img.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < accessor.Height; y++)
                        {
                            Span<Rgba32> row = accessor.GetRowSpan(y);
                            for (int x = 0; x < row.Length; x++)
                            {
                                if (row[x].A > 0)
                                {
                                    row[x].R = (byte)Math.Clamp(row[x].R * factor, 0, 255);
                                    row[x].G = (byte)Math.Clamp(row[x].G * factor, 0, 255);
                                    row[x].B = (byte)Math.Clamp(row[x].B * factor, 0, 255);
                                }
                            }
                        }
                    });

                    using var ms = new MemoryStream();
                    img.SaveAsPng(ms, new PngEncoder());
                    ms.Seek(0, SeekOrigin.Begin);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                    previewBitmap = bmp;
                }

                PreviewImage.Source = previewBitmap;
            }
            catch (Exception ex)
            {
                Log($"Preview generation error: {ex.Message}");
            }
        }

        private void ApplyTransformations<TPixel>(Image<TPixel> img) where TPixel : unmanaged, IPixel<TPixel>
        {
            if (FlipXCheckBox.IsChecked == true) img.Mutate(x => x.Flip(FlipMode.Horizontal));
            if (FlipYCheckBox.IsChecked == true) img.Mutate(x => x.Flip(FlipMode.Vertical));

            if (RotateComboBox.SelectedItem is ComboBoxItem sel)
            {
                if (int.TryParse(sel.Content?.ToString(), out int angle) && angle != 0)
                    img.Mutate(x => x.Rotate((float)angle));
            }

            var parts = CropTextBox.Text.Split(',');
            if (parts.Length == 4
                && int.TryParse(parts[0], out int left)
                && int.TryParse(parts[1], out int top)
                && int.TryParse(parts[2], out int right)
                && int.TryParse(parts[3], out int bottom))
            {
                int w = Math.Max(0, img.Width - left - right);
                int h = Math.Max(0, img.Height - top - bottom);
                if (w > 0 && h > 0)
                    img.Mutate(x => x.Crop(new SixLabors.ImageSharp.Rectangle(left, top, w, h)));
            }
        }

        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var img = new BitmapImage(new Uri(dlg.FileName));
                    imagePath = dlg.FileName;
                    PreviewImage.Source = img;
                    PreviewImage.Visibility = Visibility.Visible;
                    PreviewPlaceholder.Visibility = Visibility.Collapsed;
                    Log($"Image loaded: {dlg.FileName}");
                }
                catch (Exception ex)
                {
                    Log($"Error loading image: {ex.Message}");
                }
            }
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            Log("Started printing...");
            await using (CatPrinter ble = new CatPrinter())
            {
                bool success = await ble.ConnectAsync();
                if (success)
                {
                    await ble.Print(imagePath, (byte)intensity, printMode, ditheringMethod);
                }
                else
                {
                    Log("Failed to connect to printer.");
                }
            }
            Log("Printing finished.");
        }

        private void ApplyTheme(bool dark)
        {
            var theme = dark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";
            var dict = new ResourceDictionary { Source = new Uri(theme, UriKind.Relative) };

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        private void IntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            intensity = (int)e.NewValue;
            UpdatePreview();
        }

        private void PreviewScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomDelta = e.Delta > 0 ? 0.1 : -0.1;
            double newZoom = PreviewZoom.ScaleX + zoomDelta;

            if (newZoom < 0.2) newZoom = 0.2;
            if (newZoom > 5.0) newZoom = 5.0;

            PreviewZoom.ScaleX = newZoom;
            PreviewZoom.ScaleY = newZoom;

            Log($"Zoom: {newZoom:P0}");
        }

        private void Log(string message)
        {
            LogBox.AppendText($"{message}\n");
            LogBox.ScrollToEnd();
        }

        static BitmapSource GeneratePreview(string imagePath, int printWidth,
        ColorModes colorMode, DitheringMethods ditheringMethod, float intensity)
        {
            byte[] processedBytes = CatPrinterBLE.ImageProcessor.LoadAndProcess(
                imagePath, printWidth, colorMode, ditheringMethod
            ) ?? throw new InvalidOperationException("Failed to process image");

            using Image<L8> image = SixLabors.ImageSharp.Image.Load<L8>(imagePath);
            float aspect = (float)image.Width / image.Height;
            int height = (int)(printWidth / aspect);
            image.Mutate(i => i.Resize(printWidth, height, KnownResamplers.Lanczos3));

            float factor = intensity / 100f;
            image.Mutate(x => x.ProcessPixelRowsAsVector4(row =>
            {
                for (int px = 0; px < row.Length; px++)
                {
                    float c = MathF.Pow(row[px].X * factor, 2.2f);
                    row[px] = new Vector4(c, c, c, row[px].W);
                }
            }));

            IDither dither = ditheringMethod switch
            {
                DitheringMethods.Bayer2x2 => KnownDitherings.Bayer2x2,
                DitheringMethods.Bayer4x4 => KnownDitherings.Bayer4x4,
                DitheringMethods.Bayer8x8 => KnownDitherings.Bayer8x8,
                DitheringMethods.Bayer16x16 => KnownDitherings.Bayer16x16,
                _ => KnownDitherings.FloydSteinberg
            };
            if (colorMode == ColorModes.Mode_1bpp)
                image.Mutate(i => i.BinaryDither(dither));

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}