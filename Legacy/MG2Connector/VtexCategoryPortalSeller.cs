using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG2Connector
{
    public class VtexCategoryPortalSeller
    {
        public List<ValueCategory> value { get; set; }
        public List<VtexCategoryPortalSeller> children { get; set; }


    }

    public class ValueCategory
    {
        public string name { get; set; }    
        public string id { get; set; }
        public Boolean isActive { get; set; }   

    }
    public class ChildrenCategory
    {
        public string name { get; set; } = string.Empty;
        public string id { get; set; }
    }


}
