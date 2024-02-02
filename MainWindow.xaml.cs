using SupervisionApp.Classes;
using System.Windows;

namespace SupervisionApp
{
    public partial class MainWindow : Window
    {
        private string auditFile = "";
        private List<Commentaire> commentaires = new List<Commentaire>();
        public MainWindow()
        {
            InitializeComponent();
            Main.Content = new MainPage();

            AuditPage auditPage = new AuditPage(commentaires);
            auditPage.SuivantMainWindow += AuditPage_SuivantMainWindow;
        }


        //Reçoit les variables auditFile et comments depuis MainPage
        public void ReceiveAudit(string audit)
        {
            auditFile = audit;
        }

        public void ReceiveComments(List<Commentaire> comments)
        {
            commentaires = comments;
        }

        private void SuivantBtn_Click(object sender, RoutedEventArgs e)
        {
            //On vérifie d'abord si une taxonomie à été sélectionnée
            if (auditFile == "")
            {
                MessageBox.Show("Veuillez charger un fichier d'audit avant de passer à la page suivante.", "Aucun fichier d'audit");
                return;
            }
            AuditPage auditPage = new AuditPage(commentaires);
            auditPage.SuivantMainWindow += AuditPage_SuivantMainWindow; // Attachez l'événement ici
            Main.Content = auditPage;
            SuivantBtn.Visibility = Visibility.Hidden;
            UnloadPage();
        }

        //Si on revient au menu après avoir fait un audit, on rend le bouton Suivant à nouveau visible
        private void AuditPage_SuivantMainWindow(object sender, EventArgs e)
        {
            // Modifier la visibilité du bouton
            SuivantBtn.Visibility = Visibility.Visible;
        }

        //On se désabonne de l'évènement
        private void UnloadPage()
        {
            if (Main.Content is AuditPage auditPage)
            {
                // Se désabonner de l'événement
                auditPage.SuivantMainWindow -= AuditPage_SuivantMainWindow;
            }
        }

    }   
}