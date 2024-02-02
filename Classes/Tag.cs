namespace SupervisionApp.Classes
{
    public class Tag
    {
        public string name { get; set; }

        //Ce dictionnaire contient tous les enfants pour un noeud parent donné
        public Dictionary<string, Tag> Children { get; set; } = new Dictionary<string, Tag>();

        public Tag(string name)
        {
            this.name = name;
        }
    }
}
