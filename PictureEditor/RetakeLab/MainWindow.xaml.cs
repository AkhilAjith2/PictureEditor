using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RetakeLab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string imagePath;
        private List<Image> stickers;
        public MainWindow()
        {
            InitializeComponent();
            stickers = new List<Image>();
            LoadStickersFromResources();
        }
        //https://www.codeproject.com/Articles/104929/Bitmap-to-BitmapSource

        private void LoadStickersFromResources()
        {
            var resourceManager = Properties.Resources.ResourceManager;
            var resourceSet = resourceManager.GetResourceSet(System.Globalization.CultureInfo.CurrentCulture, true, true);

            foreach (System.Collections.DictionaryEntry entry in resourceSet)
            {
                if (entry.Value is System.Drawing.Bitmap bitmap)
                {
                    ImageSource imageSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    ImageListView.Items.Add(imageSource);
                }
            }
        }

        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.jpg, *.jpeg, *.png, *.gif)|*.jpg;*.jpeg;*.png;*.gif";

            if (openFileDialog.ShowDialog() == true)
            {
                imagePath = openFileDialog.FileName;
                StickerCanvas.Children.Clear();
                LoadImage(imagePath);
            }
        }

        private void LoadImage(string path)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();

            Image image = new Image();
            image.Source = bitmap;
            image.MouseLeftButtonDown += Image_MouseLeftButtonDown;

            ImageContainer.Children.Clear();
            ImageContainer.Children.Add(image);
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (stickers.Count == 0 || ImageListView.SelectedIndex == -1)
                return;

            Image image = stickers[0];
            double size = StickerSize.Value;
            double x = e.GetPosition(ImageContainer).X - (size / 2);
            double y = e.GetPosition(ImageContainer).Y - (size / 2);

            Image sticker = new Image
            {
                Source = image.Source.Clone(),
                Width = size,
                Height = size,
                Stretch = Stretch.Fill
            };

            Canvas.SetLeft(sticker, x);
            Canvas.SetTop(sticker, y);

            StickerCanvas.Children.Add(sticker);
        }

        private void ImageListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            stickers.Clear();
            double size = StickerSize.Value;

            if (ImageListView.SelectedItem is ImageSource selectedImage)
            {
                Image selectedSticker = new Image();
                selectedSticker.Source = selectedImage;
                selectedSticker.Width = size;
                selectedSticker.Height = size;
                selectedSticker.Stretch = Stretch.Fill;
                stickers.Add(selectedSticker);
            }
        }

        private void StickerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            stickers.Clear();
            double size = StickerSize.Value;

            if (ImageListView.SelectedItem is Image selectedImage)
            {
                Image selectedSticker = new Image();
                selectedSticker.Source = selectedImage.Source;
                selectedSticker.Width = size;
                selectedSticker.Height = size;
                selectedSticker.Stretch = Stretch.Fill;
                stickers.Add(selectedSticker);
            }
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (ImageContainer.Children.Count == 0)
                return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image (*.png)|*.png";

            if (saveFileDialog.ShowDialog() == true)
            {
                string savePath = saveFileDialog.FileName;
                SaveImageToFile(savePath);
            }
        }

        //https://stackoverflow.com/questions/10470841/easiest-way-of-saving-wpf-image-control-to-a-file

        private void SaveImageToFile(string path)
        {
            if (ImageContainer.Children.Count == 0)
                return;

            FrameworkElement imageControl = ImageContainer.Children[0] as FrameworkElement;
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)imageControl.ActualWidth, (int)imageControl.ActualHeight, 96, 96, PixelFormats.Pbgra32);

            renderBitmap.Render(imageControl);

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                VisualBrush visualBrush = new VisualBrush(imageControl);
                context.DrawRectangle(visualBrush, null, new Rect(new Point(0, 0), new Point(imageControl.ActualWidth, imageControl.ActualHeight)));

                foreach (Image sticker in StickerCanvas.Children)
                {
                    Point stickerPosition = new Point(Canvas.GetLeft(sticker), Canvas.GetTop(sticker));
                    context.DrawImage(sticker.Source, new Rect(stickerPosition, new Size(sticker.Width, sticker.Height)));
                }
            }

            renderBitmap.Render(visual);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using (FileStream file = File.Open(path, FileMode.Create))
            {
                encoder.Save(file);
            }
        }

    }
}