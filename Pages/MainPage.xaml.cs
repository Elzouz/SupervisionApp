using Microsoft.Win32;
using SupervisionApp.Classes;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace SupervisionApp
{
    public partial class MainPage : Page
    {
        //On utilisera cette variable pour vérifier qu'un fichier d'audit a bien été sélectionné
        private string auditFile = "";
        public MainPage()
        {
            InitializeComponent();
        }

        // *** Partie Audit *** //
        private async void ImportAuditBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            //On ne sélectionne que des fichiers CSV pour la simplicité de lecture de la taxonomie
            openFileDialog.Filter = "Fichiers CSV (*.csv)|*.csv";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    auditFile = openFileDialog.FileName;

                    //On vérifie qu'il s'agit bien d'un fichier d'audit en lisant sa première ligne 
                    var reader = new StreamReader(auditFile);
                    string firstLine = await reader.ReadLineAsync();
                    if (firstLine != "\"Survey - Name\"\t\"Feedback - Comment\"\t\"Feedback - Comment - English (EN)\"\t\"Feedback - Tags\"")
                    {
                        MessageBox.Show("Il ne s'agit pas d'un fichier d'audit", "Fichier incorrect");
                        return;
                    }

                    List<Commentaire> comments = await AuditLines(auditFile);

                    //On passe les variables comments et auditFile à la MainWindow
                    var mainWindow = (MainWindow)Application.Current.MainWindow;
                    mainWindow.ReceiveComments(comments);
                    Audit currentAudit = new Audit(auditFile, comments);
                    mainWindow.ReceiveAudit(auditFile);
                }
                catch (System.IO.IOException)
                {
                    //Si le fichier est déjà ouvert dans une autre application, on ne peut pas y accéder, on affiche une erreur
                    MessageBox.Show("Ce fichier est déjà ouvert dans une autre application, veuillez la fermer et réessayer.", "Impossible d'ouvrir le fichier");
                }
            }
        }

        //Récupère le fichier d'audit et crée une liste des commentaires
        public async Task<List<Commentaire>> AuditLines(string filename)
        {
            progressBar.Visibility = Visibility.Visible;
            progressBar.Value = 0;
            int totalLines = File.ReadLines(filename).Count();
            List<Commentaire> comments = new List<Commentaire>();
            var currentLineBuilder = new StringBuilder();
            using (var reader = new StreamReader(filename))
            {
                int currentLine = 0;
                int tabCount = 0;
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    currentLine++;

                    //On évite une potentielle erreur de formation d'un objet Commentaire en ignorant les lignes nulles s'il y en a
                    if (line == null) continue;

                    //On passe la première ligne avec le nom des colonnes qui est inutile
                    if (line == "\"Survey - Name\"\t\"Feedback - Comment\"\t\"Feedback - Comment - English (EN)\"\t\"Feedback - Tags\"") continue;

                    currentLineBuilder.Append(line);

                    //Si la ligne a un commentaire complet (des retours à la ligne dans les commentaires du csv provoquaient des bugs)
                    //On compte donc le nombre de séparateurs de colonnes pour être sûr qu'on a 4 colonnes
                    tabCount += line.Count(c => c == '\t');
                    if (tabCount == 3) // Si on a au moins trois tabulations, on a quatre colonnes
                    {
                        var completeLine = currentLineBuilder.ToString();
                        comments.Add(new Commentaire(completeLine));
                        currentLineBuilder.Clear();
                        tabCount = 0;
                    }
                    else
                    {
                        currentLineBuilder.Append(" ");
                    }

                    //On charge la barre de progression 
                    Dispatcher.Invoke(() => progressBar.Value = (double)currentLine / totalLines * 100);
                }
            }
            return comments;
        }


        // *** Partie Taxonomie *** //
        private async void ImportTaxonomyBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            //On ne sélectionne que des fichiers CSV pour la simplicité de lecture de la taxonomie
            openFileDialog.Filter = "Fichiers CSV (*.csv)|*.csv";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string filePath = openFileDialog.FileName;

                    //On vérifie qu'il s'agit bien d'un fichier de taxonomies en lisant sa première ligne 
                    var reader = new StreamReader(filePath);
                    string firstLine = await reader.ReadLineAsync();
                    if (firstLine != "name;type")
                    {
                        MessageBox.Show("Il ne s'agit pas d'un fichier de taxonomies", "Fichier incorrect");
                        return;
                    }
                    taxonomyProgressBar.Visibility = Visibility.Visible;
                    taxonomyProgressBar.Value = 0;

                    string[] allLines = await File.ReadAllLinesAsync(filePath);

                    taxonomyProgressBar.Value = 50;

                    //On remplace le contenu de l'ancien fichier des taxonomies par celui du nouveau
                    await File.WriteAllLinesAsync("LVMH_Taxonomy.csv", allLines);

                    taxonomyProgressBar.Value = 100;
                }
                catch (System.IO.IOException)
                {
                    //Si le fichier est déjà ouvert dans une autre application, on ne peut pas y accéder, on affiche une erreur
                    MessageBox.Show("Ce fichier est déjà ouvert dans une autre application, veuillez la fermer et réessayer.", "Impossible d'ouvrir le fichier");
                }
            }
        }
    }
}
