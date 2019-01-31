using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reported_Incident_Automation
{
    class MultiVinWarrantyReqParam
    {
        public class RESTHeader
        {
            public string xmlns { get; set; }
            public string Responsibility { get; set; }
            public string RespApplication { get; set; }
            public string SecurityGroup { get; set; }
            public string NLSLanguage { get; set; }
            public string Org_Id { get; set; }
        }
        public class VINREC
        {
            public string VIN { get; set; }
        }

        public class PVINTBL
        {
            public List<VINREC> VIN_REC { get; set; }
        }

        public class InputParameters
        {
            public string P_SR { get; set; }
            public PVINTBL P_VIN_TBL { get; set; }
            public string P_PART { get; set; }
        }

        public class GETVINWARRANTYPERIODSInput
        {
            public string xmlns { get; set; }
            public RESTHeader RESTHeader { get; set; }
            public InputParameters InputParameters { get; set; }
        }

        public class RootObject
        {
            public GETVINWARRANTYPERIODSInput GETVINWARRANTYPERIODS_Input { get; set; }
        }
    }
}
