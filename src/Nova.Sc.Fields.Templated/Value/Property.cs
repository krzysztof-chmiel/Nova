using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Nova.Sc.Fields.Templated.Value
{
    [DataContract]
    public class Property
    {
        [DataMember]
        public string key;
        [DataMember]
        public string value;
    }
}
