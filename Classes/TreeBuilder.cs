using System.IO;


namespace SupervisionApp.Classes
{
    public class TreeBuilder
    {
        public Tag root { get; set; }

        public TreeBuilder(string taxonomy)
        {
            root = new Tag(taxonomy);
        }

        public async Task TaxonomyTreeFromCSV(string selectedTaxonomy)
        {
            using (var reader = new StreamReader("LVMH_Taxonomy.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var type = line.Split(';');
                    //On s'assure que la ligne possède 2 colonnes
                    if (type.Length < 2) continue;

                    type[0] = type[0].Trim();
                    var levels = type[0].Trim().Split('>');

                    //On s'assure que la catégorie possède une sous catégorie (sauf pour N/A) avant de former l'arbre
                    if (levels.Length < 2) continue;
                    if (levels.Length < 3 && levels[1].Trim() != "N/A") continue;

                    string taxonomy = type[1].Trim();
                    Tag current = root;
                    if (taxonomy == selectedTaxonomy.Trim())
                    {
                        foreach (var level in levels)
                        {
                            var trimmedLevel = level.Trim();
                            if (!current.Children.ContainsKey(trimmedLevel))
                            {
                                current.Children[trimmedLevel] = new Tag(trimmedLevel);
                            }
                            current = current.Children[trimmedLevel];
                        }
                    }
                }
                AddPosNeg(root);
            }
        }

        //On ajoute récursivement "Positive" et "Negative" aux tags
        public void AddPosNeg(Tag currentTag)
        {
            //S'il s'agit du dernier niveau de l'arbre (et pas de N/A), on ajoute Positive et Negative
            if (currentTag.Children.Count == 0 && currentTag.name != "N/A")
            {
                currentTag.Children["Positive"] = new Tag("Positive");
                currentTag.Children["Neutral"] = new Tag("Neutral");
                currentTag.Children["Negative"] = new Tag("Negative");
            }
            else
            {
                //Sinon, on continue à parcourir l'arbre
                foreach (var child in currentTag.Children.Values)
                {
                    AddPosNeg(child);
                }
            }
        }
    }
}
