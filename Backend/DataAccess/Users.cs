using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Backend.DataAccess
{
    [DataContract]
    public class User
    {
        [Key]
        [DataMember]
        public virtual int USID { get; set; }

        [DataMember]
        public virtual string Username { get; set; }

        // [NotMapped] --> Ritorna null come valore nel campo
        // [JsonIgnore] --> Non ritorna nemmeno il campo
    }
}
