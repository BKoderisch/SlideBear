using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Windows;
using Application = Microsoft.Office.Interop.PowerPoint.Application;
using MessageBox = System.Windows.MessageBox;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace SlideBear
{
    public partial class MainWindow : Window
    {
        private string sourceFolder;
        private string targetFolder;

        public ObservableCollection<PresentationModel> Presentations { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();
            FilesListView.ItemsSource = Presentations;

            // Lade gespeicherte Ordnerpfade
            sourceFolder = ConfigurationManager.AppSettings["SourceFolder"] ?? string.Empty;
            targetFolder = ConfigurationManager.AppSettings["TargetFolder"] ?? string.Empty;


            string fullPath = "C:\\Users\\bkode\\source\\repos\\SlideBear\\Resources\\paw2.cur";
            this.Cursor = new System.Windows.Input.Cursor(fullPath);

            // Initialisiere mit vorhandenen Daten
            if (!string.IsNullOrEmpty(sourceFolder))
            {
                LoadPresentations();
            }
        }

        private void SelectSourceFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                sourceFolder = dialog.SelectedPath;
                SaveSetting("SourceFolder", sourceFolder);
                LoadPresentations();
            }
        }

        private void SelectTargetFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                targetFolder = dialog.SelectedPath;
                SaveSetting("TargetFolder", targetFolder);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPresentations();
        }

        private void LoadPresentations()
        {
            if (string.IsNullOrEmpty(sourceFolder)) return;

            Presentations.Clear();
            foreach (var file in Directory.GetFiles(sourceFolder, "*.pptx"))
            {
                Presentations.Add(new PresentationModel
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    Date = DateTime.Now.ToString("dd.MM.yyyy")
                });
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var presentation in Presentations)
            {
                presentation.IsSelected = true;
            }
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var presentation in Presentations)
            {
                presentation.IsSelected = false;
            }
        }


        private void GeneratePresentationsButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(targetFolder))
            {
                MessageBox.Show("Bitte wählen Sie einen Zielordner aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedPresentations = Presentations.Where(p => p.IsSelected).ToList();

            if (!selectedPresentations.Any())
            {
                MessageBox.Show("Bitte wählen Sie mindestens eine Präsentation aus.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Leere den Zielordner
            try
            {
                DirectoryInfo directoryInfo = new(targetFolder);
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Leeren des Zielordners: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Generiere die ausgewählten Präsentationen
            foreach (var model in selectedPresentations)
            {
                GeneratePresentation(model);
            }

            MessageBox.Show("Ausgewählte Präsentationen wurden mit hilfe der Eisbären erfolgreich generiert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
        }



        private void GeneratePresentation(PresentationModel model)
        {
            var app = new Application();
            var presentation = app.Presentations.Open(model.FilePath, ReadOnly: MsoTriState.msoFalse);

            try
            {
                foreach (Slide slide in presentation.Slides)
                {
                    foreach (Microsoft.Office.Interop.PowerPoint.Shape shape in slide.Shapes)
                    {
                        if (shape.TextFrame?.TextRange?.Text.Contains("<Datum>") == true)
                        {
                            shape.TextFrame.TextRange.Text = shape.TextFrame.TextRange.Text.Replace("<Datum>", model.Date);
                        }
                    }
                }

                var exportPath = Path.Combine(targetFolder, $"{Path.GetFileNameWithoutExtension(model.FileName)}.png");
                presentation.Slides[1].Export(exportPath, "PNG");
            }
            finally
            {
                presentation.Close();
                app.Quit();
            }
        }

        private void SaveSetting(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] != null)
            {
                config.AppSettings.Settings[key].Value = value;
            }
            else
            {
                config.AppSettings.Settings.Add(key, value);
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }



    public class PresentationModel : INotifyPropertyChanged
    {
        private bool isSelected;

        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Date { get; set; }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SubtractMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double originalHeight && double.TryParse(parameter?.ToString(), out double margin))
            {
                return originalHeight - margin;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
