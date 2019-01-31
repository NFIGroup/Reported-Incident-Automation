using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reported_Incident_Automation
{
    class SingleWarrantyReqParam
    {
        public class RESTHeader
        {
            public string @xmlns { get; set; }
            public string Responsibility { get; set; }
            public string RespApplication { get; set; }
            public string SecurityGroup { get; set; }
            public string NLSLanguage { get; set; }
            public string Org_id { get; set; }
        }

        public class InputParameters
        {
            public string P_SR { get; set; }
            public string P_VIN { get; set; }
            public string P_PART { get; set; }
            public string P_ODOMETER { get; set; }
            public string P_FAILDATE { get; set; }
        }

        public class ISPARTWARRANTABLEInput
        {
            public string @xmlns { get; set; }
            public RESTHeader RESTHeader { get; set; }
            public InputParameters InputParameters { get; set; }
        }

        public class ContentObject
        {
            public ISPARTWARRANTABLEInput ISPARTWARRANTABLE_Input { get; set; }
        }
    }
}
