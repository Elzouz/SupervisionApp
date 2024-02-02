using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupervisionApp.Classes
{
    public class Commentaire
    {
        private string surveyName;
        private string comment;
        private string translatedComment;
        private string[] tags;

        public Commentaire(string line)
        {
            var type = line.Split("\t");

            //On enlève les guillemets
            type[0] = type[0].Replace("\"", "");
            type[1] = type[1].Replace("\"", "");
            type[2] = type[2].Replace("\"", "");
            type[3] = type[3].Replace("\"", "");

            this.surveyName = type[0].Trim();
            this.comment = type[1].Trim();
            this.translatedComment = type[2].Trim();
            this.tags = type[3].Split(",");

            foreach (string tag in tags)
            {
                tag.Trim();
            }
        }

        //On utilise des getters pour pouvoir accéder aux données des commentaires audités depuis les autres classes
        public string SurveyName
        {
            get { return this.surveyName; }
        }

        public string Comment
        {
            get { return this.comment; }
        }

        public string TranslatedComment
        {
            get { return this.translatedComment; }
        }

        public string[] Tags
        {
            get { return this.tags; }
        }
    }
}
