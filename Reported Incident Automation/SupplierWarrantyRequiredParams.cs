
namespace Reported_Incident_Automation
{
    class SupplierWarrantyRequiredParams
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
        
        public class InputParameters
        {
            public int P_SUPPLIER_ID { get; set; }
            public string P_VIN { get; set; }
            public string P_PART { get; set; }
            public int P_MILES { get; set; }
            public string P_FAILDATE { get; set; }
        }

        public class ISPARTWARRANTABLEInput
        {
            public string xmlns { get; set; }
            public RESTHeader RESTHeader { get; set; }
            public InputParameters InputParameters { get; set; }
        }

        public class RootObject
        {
            public ISPARTWARRANTABLEInput ISPARTWARRANTABLE_Input { get; set; }
        }
    }
}
    

