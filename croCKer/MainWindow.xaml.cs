using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace croCKer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        String[] FilePaths = Array.Empty<string>();
        String[] FileNames = Array.Empty<string>();

        private void Button_Original_File_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.FileName = "";
            ofd.Multiselect = true;
            ofd.Filter = "Scene file (*.srp)|*.srp|Variables file (*.var)|*.var|All files (*.*)|*.*";
            ofd.FilterIndex = ofd.Filter.Length;
            Nullable<bool> result = ofd.ShowDialog();

            if (result == true)
            {
                try
                {
                    //Copy the values for the selected files to an array in order to manage the
                    //files later on according to their extension
                    FilePaths = (string[])ofd.FileNames.Clone();
                    FileNames = new string[FilePaths.Length];
                    for (int CurrentFile = 0; CurrentFile < ofd.FileNames.Length; CurrentFile++)
                    {
                        FileNames[CurrentFile] = System.IO.Path.GetFileNameWithoutExtension(ofd.FileNames[CurrentFile]);
                    }

                    Button_Convert.IsEnabled = false;

                    for (int CurrentFile = 0; CurrentFile < FilePaths.Length; CurrentFile++)
                    {
                        string OriginalFileExtension = System.IO.Path.GetExtension(FilePaths[CurrentFile]);

                        //Check to see if the file is one that is not compatible with the program
                        if (OriginalFileExtension != ".txt" && OriginalFileExtension != ".srp" && OriginalFileExtension != ".var")
                        {
                            MessageBox.Show($"At least one of the selected files is not designed to be handled by this program, and thus" +
                                $" the conflicting files will not be processed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            break;
                        }

                        //Activate the button to convert the selected files because we know that there's
                        //already one that has a compatible extension
                        Button_Convert.IsEnabled = true;
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Button_Convert_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog ofd = new OpenFolderDialog();
            Nullable<bool> result = ofd.ShowDialog();

            if (result == true)
            {
                try
                {
                    for (int CurrentFile = 0; CurrentFile < FilePaths.Length; CurrentFile++)
                    {
                        //Check what extension the file has, in order to choose its corresponding class
                        if (System.IO.Path.GetExtension(FilePaths[CurrentFile]) == ".var")
                        {
                            byte[] Data = File.ReadAllBytes(FilePaths[CurrentFile]);
                            Var var = new Var(FilePaths[CurrentFile], 0);
                            var.Decompile(ofd.FolderName, FileNames[CurrentFile]);
                        }
                        else if (System.IO.Path.GetExtension(FilePaths[CurrentFile]) == ".txt")
                        {
                            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                            string[] Data = File.ReadAllLines(FilePaths[CurrentFile], Encoding.GetEncoding("shift-jis"));
                            if (Data[Data.Length - 1] == "[END]")
                            {
                                Var var = new Var(FilePaths[CurrentFile], 1);
                                var.Compile(ofd.FolderName, FileNames[CurrentFile]);
                            }
                            if (Data[Data.Length - 1] == "Data: " && Data[Data.Length - 2] == "Flags: 3" && Data[Data.Length - 3] == "Type: 16")
                            {
                                Srp srp = new Srp(FilePaths[CurrentFile], 1);
                                srp.Compile(ofd.FolderName, FileNames[CurrentFile]);
                            }
                        }
                        else if (System.IO.Path.GetExtension(FilePaths[CurrentFile]) == ".srp")
                        {
                            byte[] Data = File.ReadAllBytes(FilePaths[CurrentFile]);
                            Srp srp = new Srp(FilePaths[CurrentFile], 0);
                            srp.Decompile(ofd.FolderName, FileNames[CurrentFile]);
                        }
                    }
                    Button_Convert.IsEnabled = false;
                    MessageBox.Show($"Process completed successfully.", "Conversion completed.", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}