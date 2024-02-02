using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupervisionApp.Classes
{
    public class Audit
    {
        private List<Commentaire> comments = new List<Commentaire>();
        public Audit(string auditFile, List<Commentaire> comments)
        {
            this.comments = comments;
        }

        public List<Commentaire> Comments
        {
            get { return this.comments; }
        }
    }
}
