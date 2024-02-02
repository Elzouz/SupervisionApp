using Microsoft.Win32;
using SupervisionApp.Classes;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace SupervisionApp
{
    public partial class AuditPage : Page
    {
        public int nbComments { get; set; }
        private List<Commentaire> commentsList; //Tous les commentaires du fichier d'audit
        private Commentaire[] commentsToAudit;  //Commentaires qui vont être audités
        private List<TreeBuilder> trees = new List<TreeBuilder>();  //Arbres des différentes taxonomies
        private int currentComment = 0;        
        private int currentLevel;  //Indicateur du niveau du tag dans la ComboBox
        private string tagBuilder = "";

        //Serviront a calculer le recall et la précision
        private List<string> deletedTagsCurrent = new List<string>(); //Liste les tags supprimés pour un commentaire
        private List<List<string>> deletedTags = new List<List<string>>(); //Faire une liste de listes pour le CSV final
        private List<string> addedTagsCurrent = new List<string>(); //Liste les tags ajoutés pour un commentaire
        private List<List<string>> addedTags = new List<List<string>>(); //Faire une liste de listes pour le CSV final

        private List<int> recall = new List<int>(); //mémorise si un tag a été ajouté pour chaque commentaire
        private List<int> precision = new List<int>(); //mémorise si un tag a été enlevé pour chaque commentaire

        //On utilisera cet évènement pour rendre le bouton "Suivant" visible lorsque l'on retourne au menu.
        public event EventHandler SuivantMainWindow;

        public AuditPage(List<Commentaire> comments)
        {
            InitializeComponent();
            commentsList = comments;
            FilterComments();
            BuildPossibleTags();
            DataContext = this;
        }


        //On élimine les commentaires à ne pas auditer (<50 ou >150 caractère)
        private List<Commentaire> FilterComments() 
        {
            List<Commentaire> auditableComments = new List<Commentaire>();
            for (int i = commentsList.Count - 1; i >= 0; i--)
            {
                int longueur = Math.Max(commentsList[i].Comment.Length, commentsList[i].TranslatedComment.Length);
                if (commentsList[i].Tags[0] != "" && longueur < 150 && longueur > 50 && !commentsList[i].Tags[0].Contains("Customer Experience"))
                {
                    auditableComments.Add(commentsList[i]);
                }
            }
            nbComments = auditableComments.Count;
            return auditableComments;
        }

        //On charge le contenu de la page avec les commentaires sélectionnés
        private void LoadContent(int commentsLeft)
        {
            if (commentsToAudit != null)
            {
                TagsStckP.Children.Clear();
                Commentaire current = commentsToAudit[currentComment];
                TranslatedCommentTxt.Visibility = Visibility.Visible;
                SurveyNameTxt.Text = current.SurveyName + " (" + commentsLeft + ")";
                TranslatedCommentTxt.Text = current.TranslatedComment;
                if(TranslatedCommentTxt.Text == "")
                {
                    TranslatedCommentTxt.Visibility = Visibility.Collapsed;
                }
                CommentTxt.Text = current.Comment;

                Style roundedButton = (Style)Application.Current.Resources["RoundedButton"];

                //On ajoute des boutons pour les tags
                for (int i = 0; i < current.Tags.Length; i++)
                {
                    //On crée le bouton pour le tag
                    Button newTag = new Button();
                    newTag.Style = roundedButton;
                    newTag.Content = " " + current.Tags[i] + " x ";
                    newTag.BorderBrush = new SolidColorBrush(Colors.LightSeaGreen) ;
                    newTag.Background = new SolidColorBrush(Colors.Azure) ;
                    newTag.Foreground = new SolidColorBrush(Colors.LightSeaGreen);
                    newTag.Margin = new Thickness(0, 5, 0, 5);
                    
                    //On met en forme la string à ajouter à la liste des tags enlevés
                    string tagContent = newTag.Content.ToString();
                    tagContent = tagContent.Trim();
                    tagContent = tagContent.Trim('x');
                    tagContent = tagContent.Trim();                  
                    
                    //On ajoute un gestionnaire d'événements Click pour faire disparaitre chaque bouton lorque l'on clique dessus
                    newTag.Click += (sender, e) =>
                    {
                        //On ajoute le tag à la liste des tags supprimés
                        deletedTagsCurrent.Add(tagContent);
                        ((Button)sender).Visibility = Visibility.Collapsed; 
                    };                   
                    TagsStckP.Children.Add(newTag);
                }
            }
        }
        private void NumberInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //On empêche l'utilisateur de rentrer d'autres caractères que des nombres
            e.Handled = !e.Text.All(char.IsDigit);
        }


        //Validation du nombre de commentaires à auditer
        private async void ValidationBtn_Click(object sender, RoutedEventArgs e)
        {
            //On récupère le nombre de commentaires à tagger et on vérifie s'il est valide
            if(string.IsNullOrEmpty(numberInput.Text))
            {
                MessageBox.Show("Veuillez entrer une valeur");
                return;
            }
            int nbAudit = Convert.ToInt32(numberInput.Text);
            if(nbAudit < 0 || nbAudit > nbComments)
            {
                MessageBox.Show("Veuillez entrer une valeur entre 0 et " + nbComments.ToString());
                numberInput.Focus();
                return;
            }

            selectCommentsBlock.Visibility = Visibility.Collapsed;
            nbCommentTextBlock.Visibility = Visibility.Collapsed;
            validationBtn.Visibility = Visibility.Collapsed;
            numberInput.Visibility = Visibility.Collapsed;

            //On sélectionne aléatoirement nbAudit commentaires dans un autre thread pour ne pas bloquer l'UI
            List<Commentaire> auditableComments = FilterComments();
            commentsToAudit = new Commentaire[nbAudit];
            await Task.Run(() =>
            {
                Random rand = new Random();
                HashSet<int> selectedIndices = new HashSet<int>();
                while (selectedIndices.Count < nbAudit)
                {
                    int index = rand.Next(auditableComments.Count);
                    selectedIndices.Add(index);
                }

                int i = 0;
                foreach (int index in selectedIndices)
                {
                    commentsToAudit[i] = auditableComments[index];
                    i++;
                }
            });

            currentComment = 0;
            SurveyNameTxt.Visibility = Visibility.Visible;
            CommentBorder.Visibility = Visibility.Visible;
            CommentTxt.Visibility = Visibility.Visible;
            ButtonsStckP.Visibility = Visibility.Visible;
            TagsTxt.Visibility = Visibility.Visible;
            TagsStckP.Visibility = Visibility.Visible;
            AddTagBtn.Visibility = Visibility.Visible;
            LoadContent(commentsToAudit.Length);
        }

        //Valide l'audit d'un commentaire et passe au commentaire suivant
        private void ValidateBtn_Click(object sender, RoutedEventArgs e)
        {
            currentLevel = 0;
            UndoBtn.Visibility = Visibility.Visible;
            TagsCbBox.Visibility = Visibility.Collapsed;
            AddTagBtn.Visibility = Visibility.Visible;

            //On empêche de valider un commentaire sans tag
            if(deletedTagsCurrent.Count >= commentsToAudit[currentComment].Tags.Length + addedTagsCurrent.Count)
            {
                MessageBox.Show("Ce commentaire ne contient aucun tag", "Erreur : aucun tag");

                //On empêche l'utilisateur de revenir en arrière tant que le problème n'est pas résolu
                LoadContent(commentsToAudit.Length - currentComment + 1);
                deletedTagsCurrent.Clear();
                addedTagsCurrent.Clear();

                //En rechargeant la page, on fait comme si aucun tag n'avait été retiré
                List<string> nothingToAdd = new List<string> { "" };
                deletedTags.Add(nothingToAdd);
                return;
            }

            //On ajoute les tags ajoutés à addedTags
            if (addedTagsCurrent.Count > 0)
            {
                recall.Add(1);
                addedTags.Add(new List<string>(addedTagsCurrent));
            }

            if (addedTagsCurrent.Count == 0)
            {
                recall.Add(0);

                //On ajoute une liste vide si aucun tag n'a été ajouté
                List<string> nothingToAdd = new List<string> { "" };
                addedTags.Add(nothingToAdd);
            }

            //On ajoute les tags retirés à deletedTags
            if (deletedTagsCurrent.Count > 0)
            {
                precision.Add(1);
                deletedTags.Add(new List<string>(deletedTagsCurrent)); // Ajoutez cleanedDeletedTags ici
            }
            if (deletedTagsCurrent.Count == 0)
            {
                precision.Add(0);
                //On ajoute une liste vide si aucun tag n'a été enlevé
                List<string> nothingToAdd = new List<string> { "" };
                deletedTags.Add(nothingToAdd);
            }

            //On vide les listes de tags ajoutés et enlevés avant de passer au commentaire suivant
            addedTagsCurrent.Clear();
            deletedTagsCurrent.Clear();

            if (currentComment < commentsToAudit.Length - 1)
            {
                currentComment++;               
                LoadContent(commentsToAudit.Length - currentComment);
            }
            else
            {
                double totalPrecision = 100 - ((precision.Sum() * 100) / commentsToAudit.Length);
                double totalRecall = (recall.Sum() * 100) / commentsToAudit.Length; 
                ValidateBtn.Visibility = Visibility.Collapsed;
                TagsStckP.Visibility = Visibility.Collapsed;
                CommentBorder.Visibility = Visibility.Collapsed;
                AddTagBtn.Visibility = Visibility.Collapsed;
                TagsTxt.Visibility = Visibility.Collapsed;
                SurveyNameTxt.Text = "0 - Audit terminé";
                TranslatedCommentTxt.Visibility = Visibility.Visible;
                TranslatedCommentTxt.Text = ("recall : " + totalRecall + "%\nprecision : " + totalPrecision + "%");
                endStack.Visibility = Visibility.Visible;
            }
        }

        private void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            if(currentComment > 0 && SurveyNameTxt.Text != "0 - Audit terminé") 
            {
                currentComment--;

                //On réinitialise la précision et le recall pour ce commentaire
                precision.RemoveAt(precision.Count - 1);
                recall.Remove(precision.Count - 1);

                //On enlève les listes de tags finaux et ajoutés/enlevés pour la réinitialiser
                deletedTags.RemoveAt(deletedTags.Count - 1);
                addedTags.RemoveAt(addedTags.Count - 1);

                LoadContent(commentsToAudit.Length - currentComment);
            }
            if (SurveyNameTxt.Text == "0 - Audit terminé")
            {
                //On réinitialise la précision et le recall pour ce commentaire
                precision.RemoveAt(precision.Count - 1);
                recall.RemoveAt(precision.Count - 1);

                //On enlève les listes de tags finaux et ajoutés/enlevés pour la réinitialiser
                deletedTags.RemoveAt(deletedTags.Count - 1);
                addedTags.RemoveAt(addedTags.Count - 1);

                ValidateBtn.Visibility = Visibility.Visible;
                UndoBtn.Visibility = Visibility.Visible;
                TagsStckP.Visibility = Visibility.Visible;
                CommentBorder.Visibility = Visibility.Visible;
                AddTagBtn.Visibility = Visibility.Visible;
                TagsTxt.Visibility = Visibility.Visible;
                ValidateBtn.Visibility = Visibility.Visible;
                endStack.Visibility = Visibility.Collapsed;
                LoadContent(commentsToAudit.Length - currentComment);
            }
        }

        private async Task<List<string>> ReadTaxonomies()
        {
            List<string> taxonomyNames = new List<string>();
            try
            {
                //On récupère le nom des taxonomies
                using (var reader = new StreamReader("LVMH_Taxonomy.csv"))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();

                        //On évite une potentielle erreur en ignorant les lignes nulles s'il y en a
                        if (line == null) continue;

                        //On passe la première ligne qui est inutile
                        if (line == "name;type") continue;
                        var type = line.Split(';');

                        //On vérifie que la taxonomie n'existe pas déjà, puis on l'ajoute à la liste des taxonomies existantes
                        if (!taxonomyNames.Contains(type[1]))
                        {
                            taxonomyNames.Add(type[1]);
                        }
                    }
                }
            }
            catch (System.IO.IOException)
            {
                //Si le fichier est déjà ouvert dans une autre application, on ne peut pas y accéder, on affiche une erreur
                MessageBox.Show("Le fichier des taxonomies est déjà ouvert dans une autre application, veuillez la fermer et réessayer.", "Impossible d'ouvrir le fichier");
            }
            return taxonomyNames;
        }

        //On liste les taxonomies pour construire les arbres des tags possibles
        private async void BuildPossibleTags()
        {
            List<string> taxonomies = await ReadTaxonomies();

            foreach(string taxonomy in taxonomies)
            {
                TreeBuilder tree = new TreeBuilder(taxonomy);
                await tree.TaxonomyTreeFromCSV(taxonomy);
                trees.Add(tree);
            }
        }

        private void AddTagBtn_Click(object sender, RoutedEventArgs e)
        {
            //On lit la taxonomie du premier tag déjà assigné pour savoir quelle taxonomie utiliser
            Commentaire current = commentsToAudit[currentComment];
            string[] categories = current.Tags[0].Split('>');
            string taxonomy = categories[0];
            taxonomy = taxonomy.Replace("LVMH ", "").Trim();

            //On utilise l'arbre avec cette taxonomie pour avoir les tags possibles
            var selectedTree = trees.FirstOrDefault(tree => tree.root.name.Trim() == taxonomy);
            
            if(selectedTree == null) 
            {
                MessageBox.Show("La taxonomie n'a pas été trouvée.", "Erreur de taxonomie");
                return;
            }
            ChangeComboBox(selectedTree.root, 0);
            TagsCbBox.Visibility = Visibility.Visible;
            AddTagBtn.Visibility = Visibility.Collapsed;
        }

        //Modifie le contenu de la ComboBox pour afficher les sous catégories de tag
        private async void ChangeComboBox(Tag tag, int level) 
        {
            TagsCbBox.IsDropDownOpen = false;

            //On attends pour permettre à l'UI de se rafraîchir
            await Task.Delay(100); //délai de 100 millisecondes

            currentLevel = level;
            TagsCbBox.ItemsSource = tag.Children.Values;
            TagsCbBox.DisplayMemberPath = "name";

            await Task.Delay(100);
            TagsCbBox.IsDropDownOpen = true;
        }

        private void TagsCbBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;           
            Tag selectedTag = comboBox.SelectedItem as Tag;

            //On modifie la ComboBox pour les enfants du Tag sélectionné
            if(selectedTag != null && currentLevel < 4 && !tagBuilder.Contains("N/A"))
            {
                ChangeComboBox(selectedTag, currentLevel + 1);
                tagBuilder += " " + selectedTag.name + " >";
            } 
            if(currentLevel > 3 || tagBuilder.Contains("N/A"))
            {
                Style roundedButton = (Style)Application.Current.Resources["RoundedButton"];

                //On ajoute un bouton pour le nouveau tag
                Button newTag = new Button();
                newTag.Style = roundedButton;
                tagBuilder = tagBuilder.Trim('>');
                newTag.Content = tagBuilder.Trim() + " x ";
                newTag.BorderBrush = new SolidColorBrush(Colors.LightSeaGreen);
                newTag.Background = new SolidColorBrush(Colors.Azure);
                newTag.Foreground = new SolidColorBrush(Colors.LightSeaGreen);
                newTag.Margin = new Thickness(0, 5, 0, 5);

                //On ajoute un gestionnaire d'événements Click pour faire disparaitre chaque bouton lorque l'on clique dessus
                newTag.Click += (sender, e) =>
                {
                    //Si le tag a été ajouté puis enlevé, on l'enlève de la liste des tags enlevés et ajoutés
                    if (addedTagsCurrent.Contains(tagBuilder.Trim()))
                    {
                        deletedTagsCurrent.Remove(tagBuilder.Trim());
                        addedTagsCurrent.Remove(tagBuilder.Trim());
                    }
                    //On retire le tag de la liste des tags ajoutés
                    deletedTagsCurrent.Remove(tagBuilder.Trim());

                    ((Button)sender).Visibility = Visibility.Collapsed;
                };
               
                for(int i=0; i<commentsToAudit[currentComment].Tags.Length; i++)
                {
                    commentsToAudit[currentComment].Tags[i] = commentsToAudit[currentComment].Tags[i].Trim();
                }

                //Si le tag existe déjà, on ne le rajoute pas
                bool exist = commentsToAudit[currentComment].Tags.Contains(tagBuilder.Trim());
                if ((exist && !deletedTagsCurrent.Contains(tagBuilder.Trim())) || addedTagsCurrent.Contains(tagBuilder.Trim()))
                {
                    tagBuilder = "";
                    currentLevel = 0;
                    AddTagBtn_Click(sender, e);
                    return;
                }

                //Si le tag a été enlevé puis remis, on l'enlève de la liste des tags enlevés et ajoutés
                if(deletedTagsCurrent.Contains(tagBuilder.Trim()))
                {
                    addedTagsCurrent.Remove(tagBuilder.Trim());
                    deletedTagsCurrent.Remove(tagBuilder.Trim());                  
                }
                else
                {
                    //On ajoute le tag à la liste des tags ajoutés
                    addedTagsCurrent.Add(tagBuilder.Trim());
                }

                //On ajoute le bouton du nouveau tag et on réinitialise tagBuilder et currentLevel
                TagsStckP.Children.Add(newTag);
                tagBuilder = "";
                currentLevel = 0;
                AddTagBtn_Click(sender, new RoutedEventArgs());
            }
        }

        //Faire une colonne tags ajoutés et une tags supprimés
        private void exportCSV()
        {
            string csvBuilder = "Maison;Commentaire;Commentaire traduit;Tags initiaux;Tags faux;Tags manquants\n";
            for(int i=0; i < commentsToAudit.Length; i++)
            {
                Commentaire comment = commentsToAudit[i];
                csvBuilder += comment.SurveyName + ";" + comment.Comment + ";" + comment.TranslatedComment + ";";

                //Tags initiaux
                for (int j = 0; j < commentsToAudit[i].Tags.Length-1; j++)
                {
                    csvBuilder += commentsToAudit[i].Tags[j] + ", ";
                }
                csvBuilder += commentsToAudit[i].Tags[commentsToAudit[i].Tags.Length-1] + ";";

                //Tags supprimés (tags faux)
                for (int j=0; j < deletedTags[i].Count-1; j++)
                {
                    csvBuilder += deletedTags[i][j] + ", ";
                }
                csvBuilder += deletedTags[i][deletedTags[i].Count-1] + ";";

                //Tags ajoutés (tags manquants)
                for (int j = 0; j < addedTags[i].Count - 1; j++)
                {
                    csvBuilder += addedTags[i][j] + ", ";
                }
                csvBuilder += addedTags[i][addedTags[i].Count - 1] + "\n";
            }
            
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = "csv";
            saveFileDialog.AddExtension = true;
            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                try
                {
                    File.WriteAllText(filePath, csvBuilder);
                }
                catch(System.IO.IOException) 
                { 
                    MessageBox.Show("Erreur, le fichier est déjà ouvert dans une autre application, veuillez la fermer et réessayer.");
                }
            }
        }

        private void SaveFileBtn_Click(object sender, RoutedEventArgs e)
        {
            exportCSV();
        }

        private void ReturnMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            //On réinitialise toutes les variables de l'audit
            addedTags.Clear();
            addedTagsCurrent.Clear();
            deletedTags.Clear();
            deletedTagsCurrent.Clear();
            nbComments = 0;
            commentsList.Clear();
            trees.Clear();
            currentComment = 0;
            currentLevel = 0;
            tagBuilder = "";
            recall.Clear();
            precision.Clear();

            //On déclenche l'évènement pour que le bouton Suivant de la MainWindow soit visible
            SuivantMainWindow?.Invoke(this, EventArgs.Empty);

            //Navigation vers la MainPage
            this.NavigationService.Navigate(new MainPage());
        }
    }
}


