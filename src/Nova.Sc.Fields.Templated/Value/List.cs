using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Nova.Sc.Fields.Templated.Value
{
    [DataContract]
    public class List
    {
        [DataMember]
        public List<Item> items = new List<Item>();
    }
}
